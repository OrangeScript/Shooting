
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

[RequireComponent(typeof(UIDocument))]
public class MainMenuController : MonoBehaviour
{

    private const ushort Port = 7777;

    private NetworkManager manager;

    private Button createButton;
    private Button joinButton;
    private Button quitButton;

    private VisualElement joinPanel;
    private TextField ipField;
    private Label statusLabel;

    private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        createButton = root.Q<Button>("StartGameButton");
        joinButton = root.Q<Button>("JoinButton");
        quitButton = root.Q<Button>("ExitButton");

        if (createButton == null ||
            joinButton == null ||
            quitButton == null)
        {
            Debug.LogError("请检查 UI Builder 中三个按钮的 Name");
            return;
        }

        

        // 动态创建加入游戏面板
        joinPanel = new VisualElement();
        joinPanel.style.display = DisplayStyle.None;
        joinPanel.style.alignSelf = Align.Center;
        joinPanel.style.width = 300;

        ipField = new TextField("主机 IP");
        ipField.value = "127.0.0.1";

        var confirmButton = new Button(ConnectToHost);
        confirmButton.text = "确认加入";
        confirmButton.style.height = 45;

        joinPanel.Add(ipField);
        joinPanel.Add(confirmButton);

        var parent = joinButton.parent;
        parent.Insert(parent.IndexOf(joinButton) + 1, joinPanel);

        statusLabel = new Label("");
        statusLabel.style.alignSelf = Align.Center;
        parent.Add(statusLabel);

        // 绑定三个按钮的回调
        createButton.clicked += CreateGame;
        joinButton.clicked += ShowJoinPanel;
        quitButton.clicked += QuitGame;

        
    }
    void Start()
    {
        manager = NetworkManager.Singleton;

        if (manager == null)
        {
            Debug.LogError("场景中缺少 NetworkManager");
            return;
        }

        // 保证切换到 Game 场景后网络连接不会消失
        DontDestroyOnLoad(manager.gameObject);
        manager.OnClientConnectedCallback += OnConnected;
        manager.OnClientDisconnectCallback += OnDisconnected;
    }

    private void OnDisable()
    {
        if (createButton != null)
            createButton.clicked -= CreateGame;

        if (joinButton != null)
            joinButton.clicked -= ShowJoinPanel;

        if (quitButton != null)
            quitButton.clicked -= QuitGame;

        if (manager != null)
        {
            manager.OnClientConnectedCallback -= OnConnected;
            manager.OnClientDisconnectCallback -= OnDisconnected;
        }
    }


    private void CreateGame()
    {
        if (manager.IsListening) return;

        var transport = manager.GetComponent<UnityTransport>();

        transport.SetConnectionData(
            "127.0.0.1",
            Port,
            "0.0.0.0"
        );

        if (!manager.StartHost())
        {
            statusLabel.text = "创建失败";
            return;
        }

        // 由 Host 统一切换所有玩家的场景
        var result = manager.SceneManager.LoadScene(
            "Scene_02",
            LoadSceneMode.Single
        );

        if (result != SceneEventProgressStatus.Started)
        {
            Debug.LogError($"场景加载失败: {result}");
        }
    }

    // 展开或收起 IP 输入框
    private void ShowJoinPanel()
    {
        joinPanel.style.display =
            joinPanel.style.display == DisplayStyle.None
                ? DisplayStyle.Flex
                : DisplayStyle.None;
    }

    // 连接到指定 IP 的 Host
    private void ConnectToHost()
    {
        if (manager.IsListening) return;

        string ip = ipField.value.Trim();

        if (string.IsNullOrEmpty(ip))
        {
            statusLabel.text = "请输入主机 IP";
            return;
        }

        var transport = manager.GetComponent<UnityTransport>();

        transport.SetConnectionData(ip, Port);

        if (manager.StartClient())
            statusLabel.text = "正在连接...";
        else
            statusLabel.text = "无法发起连接";
    }

    private void OnConnected(ulong clientId)
    {
        if (clientId == manager.LocalClientId)
            statusLabel.text = "连接成功";
    }

    private void OnDisconnected(ulong clientId)
    {
        if (clientId == manager.LocalClientId)
            statusLabel.text = "连接已断开";
    }

    private void QuitGame()
    {
        if (manager.IsListening)
            manager.Shutdown();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
