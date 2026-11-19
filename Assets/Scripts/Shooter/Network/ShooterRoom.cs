using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(NetworkManager), typeof(UnityTransport))]
public sealed class ShooterRoom : MonoBehaviour
{
    public const string MenuScene = "MainMenu_1";
    public const string GameScene = "Game_01";

    [SerializeField] private ushort port = 7778;
    [SerializeField, Range(2, 4)] private int roomCapacity = 4;

    private readonly HashSet<ulong> admittedClients = new();
    private readonly HashSet<ulong> readyClients = new();
    private NetworkManager manager;
    private UnityTransport transport;
    private NetworkSceneManager networkScenes;
    private bool hosting;
    private bool closing;

    public static ShooterRoom Instance { get; private set; }
    public bool IsBusy => closing || (manager != null && manager.IsListening);
    public bool IsHosting => hosting && IsBusy;
    public bool IsChangingScene { get; private set; }
    public bool CanStartMatch => IsHosting && !closing && !IsChangingScene &&
        readyClients.Count > 0 && readyClients.Count == admittedClients.Count;
    public string Status { get; private set; } = "局域网联机。加入时填写房主的 IPv4 地址。";
    public event Action Changed;

    private void Awake()
    {
        Instance = this;
        manager = GetComponent<NetworkManager>();
        transport = GetComponent<UnityTransport>();
        manager.OnClientConnectedCallback += OnConnected;
        manager.OnClientDisconnectCallback += OnDisconnected;
    }

    public void CreateRoom()
    {
        if (IsBusy || !CheckConfiguration())
            return;

        hosting = true;
        transport.SetConnectionData("127.0.0.1", port, "0.0.0.0");
        manager.ConnectionApprovalCallback = Approve;
        if (!manager.StartHost())
        {
            hosting = false;
            readyClients.Clear();
            admittedClients.Clear();
            SetStatus("创建房间失败，请检查端口和 Console。");
            return;
        }

        BindSceneEvents();
        UpdateRoomStatus();
    }

    public void JoinRoom(string address)
    {
        if (IsBusy || !CheckConfiguration())
            return;
        if (!IPAddress.TryParse(address?.Trim(), out IPAddress ip) ||
            ip.AddressFamily != AddressFamily.InterNetwork)
        {
            SetStatus("请输入房主的 IPv4 地址。同机测试填 127.0.0.1。");
            return;
        }

        transport.SetConnectionData(ip.ToString(), port);
        if (manager.StartClient())
        {
            BindSceneEvents();
            SetStatus("正在连接房主…");
        }
        else
        {
            SetStatus("无法启动客户端，请检查 Console。");
        }
    }

    private bool CheckConfiguration()
    {
        if (!Application.CanStreamedLevelBeLoaded(MenuScene))
        {
            SetStatus("请把 MainMenu_1 加入 Build Settings 并启用。");
            return false;
        }
        if (manager.NetworkConfig.NetworkTransport == transport &&
            manager.NetworkConfig.EnableSceneManagement &&
            manager.NetworkConfig.ConnectionApproval &&
            manager.NetworkConfig.PlayerPrefab == null)
            return true;

        SetStatus("请检查房间的 Transport、Connection Approval、Scene Management 和 Player Prefab 配置。");
        return false;
    }

    private void Approve(NetworkManager.ConnectionApprovalRequest request,
        NetworkManager.ConnectionApprovalResponse response)
    {
        response.CreatePlayerObject = false;
        if (closing || IsChangingScene || admittedClients.Count >= roomCapacity)
        {
            response.Approved = false;
            response.Reason = IsChangingScene ? "本局已开始，请等下一局。" : "房间已满或正在关闭。";
            return;
        }

        admittedClients.Add(request.ClientNetworkId);
        response.Approved = true;
        Changed?.Invoke();
    }

    private void OnConnected(ulong clientId)
    {
        if (hosting)
        {
            admittedClients.Add(clientId);
            readyClients.Add(clientId);
            UpdateRoomStatus();
        }
        else if (clientId == manager.LocalClientId)
        {
            SetStatus("已加入房间，等待房主开始游戏。");
        }
    }

    private void OnDisconnected(ulong clientId)
    {
        if (closing)
            return;
        if (hosting)
        {
            admittedClients.Remove(clientId);
            readyClients.Remove(clientId);
            if (!IsChangingScene)
                UpdateRoomStatus();
        }
        else if (clientId == manager.LocalClientId)
        {
            string reason = manager.DisconnectReason;
            if (IsChangingScene || SceneManager.GetActiveScene().name == GameScene)
                LeaveMatch();
            else
                StartCoroutine(Close(false, string.IsNullOrEmpty(reason) ? "连接已断开，请检查房主地址。" : reason));
        }
    }

    public void StartMatch()
    {
        if (!CanStartMatch)
            return;
        if (!Application.CanStreamedLevelBeLoaded(GameScene))
        {
            SetStatus("请把 Game_01 加入 Build Settings 并启用。");
            return;
        }

        IsChangingScene = true;
        SceneEventProgressStatus result = networkScenes.LoadScene(GameScene, LoadSceneMode.Single);
        if (result == SceneEventProgressStatus.Started)
        {
            SetStatus("正在进入游戏…");
            return;
        }

        IsChangingScene = false;
        SetStatus($"进入 Game_01 失败（{result}）");
    }

    private void OnSceneEvent(SceneEvent sceneEvent)
    {
        if (!hosting && sceneEvent.SceneEventType == SceneEventType.Load &&
            sceneEvent.SceneName == GameScene)
        {
            IsChangingScene = true;
            SetStatus("正在进入游戏…");
            return;
        }
        if (!hosting || closing || sceneEvent.SceneEventType != SceneEventType.LoadEventCompleted ||
            sceneEvent.SceneName != GameScene)
            return;

        foreach (ulong clientId in sceneEvent.ClientsThatTimedOut)
            if (clientId != NetworkManager.ServerClientId && manager.ConnectedClients.ContainsKey(clientId))
                manager.DisconnectClient(clientId, "加载 Game_01 超时。");

        ShooterCoopRun run = FindObjectOfType<ShooterCoopRun>();
        if (run != null && run.Begin(sceneEvent.ClientsThatCompleted))
            return;

        Debug.LogError("合作回合无法开局，请检查 Game_01 的 ShooterCoopRun 和网络 Prefab。", this);
        LeaveMatch();
    }

    public void CloseLobby()
    {
        if (IsBusy && !closing && !IsChangingScene)
            StartCoroutine(Close(false, "已退出房间。"));
    }

    public void LeaveMatch()
    {
        if (!closing)
            StartCoroutine(Close(true, "已退出房间。"));
    }

    public void DiscardIdleManager()
    {
        if (!IsBusy)
            Destroy(gameObject);
    }

    private IEnumerator Close(bool returnToMenu, string message)
    {
        closing = true;
        if (manager.IsListening && !manager.ShutdownInProgress)
            manager.Shutdown();
        while (manager.IsListening || manager.ShutdownInProgress)
            yield return null;
        hosting = false;
        IsChangingScene = false;
        admittedClients.Clear();
        readyClients.Clear();
        UnbindSceneEvents();
        closing = false;
        SetStatus(message);

        if (returnToMenu)
        {
            Time.timeScale = 1f;
            Destroy(gameObject);
            SceneManager.LoadScene(MenuScene);
        }
    }

    private void UpdateRoomStatus()
    {
        SetStatus($"房间人数 {readyClients.Count}/{roomCapacity} · 端口 {port}。准备好后开始合作。");
    }

    private void SetStatus(string status)
    {
        Status = status;
        Changed?.Invoke();
    }

    private void BindSceneEvents()
    {
        networkScenes = manager.SceneManager;
        networkScenes.OnSceneEvent += OnSceneEvent;
    }

    private void UnbindSceneEvents()
    {
        if (networkScenes != null)
            networkScenes.OnSceneEvent -= OnSceneEvent;
        networkScenes = null;
    }

    private void OnDestroy()
    {
        UnbindSceneEvents();
        if (manager != null)
        {
            manager.OnClientConnectedCallback -= OnConnected;
            manager.OnClientDisconnectCallback -= OnDisconnected;
        }
        if (Instance == this)
            Instance = null;
    }
}
