
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class MainMenu1Controller : MonoBehaviour
{
    [Header("菜单样式")]
    [SerializeField] private StyleSheet menuStyle;
    [SerializeField] private ShooterRoom room;

    private VisualElement homePage;
    private VisualElement multiplayerPage;
    private VisualElement infoPage;

    private Label infoTitle;

    private Button soloButton;
    private Button multiButton;
    private Button codexButton;
    private Button optionsButton;
    private Button quitButton;
    private Button multiBackButton;
    private Button infoBackButton;
    private Button createButton;
    private Button joinButton;
    private Button roomStartButton;
    private TextField roomAddress;
    private Label roomStatus;

    private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        // 强制关联 USS，避免样式丢失
        if (menuStyle != null &&
            !root.styleSheets.Contains(menuStyle))
        {
            root.styleSheets.Add(menuStyle);
        }
        else if (menuStyle == null)
        {
            Debug.LogError("请在 Inspector 中指定 MainMenu.uss", this);
        }

        homePage = root.Q<VisualElement>("HomePage");
        multiplayerPage = root.Q<VisualElement>("MultiplayerPage");
        infoPage = root.Q<VisualElement>("InfoPage");
        infoTitle = root.Q<Label>("InfoTitle");

        soloButton = root.Q<Button>("SoloButton");
        multiButton = root.Q<Button>("MultiButton");
        codexButton = root.Q<Button>("CodexButton");
        optionsButton = root.Q<Button>("OptionsButton");
        quitButton = root.Q<Button>("QuitButton");
        multiBackButton = root.Q<Button>("MultiBackButton");
        infoBackButton = root.Q<Button>("InfoBackButton");
        createButton = root.Q<Button>("CreateButton");
        joinButton = root.Q<Button>("JoinButton");
        roomStartButton = root.Q<Button>("RoomStartButton");
        roomAddress = root.Q<TextField>("RoomAddress");
        roomStatus = root.Q<Label>("RoomStatus");

        if (soloButton == null || multiButton == null || codexButton == null ||
            optionsButton == null || quitButton == null || multiBackButton == null ||
            infoBackButton == null || createButton == null || joinButton == null)
        {
            Debug.LogError("主菜单缺少按钮，请检查 UIDocument 的 Source Asset。", this);
            return;
        }

        soloButton.clicked += StartSolo;
        multiButton.clicked += OpenMultiplayer;
        codexButton.clicked += OpenCodex;
        optionsButton.clicked += OpenOptions;
        quitButton.clicked += QuitGame;
        multiBackButton.clicked += BackHome;
        infoBackButton.clicked += BackHome;

        createButton.clicked += CreateRoom;
        joinButton.clicked += JoinRoom;
        if (roomStartButton != null)
            roomStartButton.clicked += StartRoomGame;
        if (room != null)
            room.Changed += RefreshRoom;

        BackHome();
    }

    private void OnDisable()
    {
        if (soloButton != null)
            soloButton.clicked -= StartSolo;
        if (multiButton != null)
            multiButton.clicked -= OpenMultiplayer;
        if (codexButton != null)
            codexButton.clicked -= OpenCodex;
        if (optionsButton != null)
            optionsButton.clicked -= OpenOptions;
        if (quitButton != null)
            quitButton.clicked -= QuitGame;
        if (multiBackButton != null)
            multiBackButton.clicked -= BackHome;
        if (infoBackButton != null)
            infoBackButton.clicked -= BackHome;
        if (createButton != null)
            createButton.clicked -= CreateRoom;
        if (joinButton != null)
            joinButton.clicked -= JoinRoom;
        if (roomStartButton != null)
            roomStartButton.clicked -= StartRoomGame;
        if (room != null)
            room.Changed -= RefreshRoom;
    }

    private void ShowPage(VisualElement page)
    {
        homePage.style.display = DisplayStyle.None;
        multiplayerPage.style.display = DisplayStyle.None;
        infoPage.style.display = DisplayStyle.None;

        page.style.display = DisplayStyle.Flex;
    }

    private void StartSolo()
    {
        if (room != null && room.IsBusy)
            return;
        if (!Application.CanStreamedLevelBeLoaded(ShooterRoom.GameScene))
        {
            Debug.LogError("请把 Game_01 加入 Build Settings 并启用。", this);
            return;
        }
        room?.DiscardIdleManager();
        Time.timeScale = 1f;
        SceneManager.LoadScene(ShooterRoom.GameScene);
    }

    private void OpenMultiplayer()
    {
        ShowPage(multiplayerPage);
        RefreshRoom();
    }

    private void CreateRoom()
    {
        room?.CreateRoom();
    }

    private void JoinRoom()
    {
        room?.JoinRoom(roomAddress?.value);
    }

    private void StartRoomGame()
    {
        room?.StartMatch();
    }

    private void RefreshRoom()
    {
        if (createButton == null || joinButton == null)
            return;

        bool configured = room != null && roomStartButton != null && roomAddress != null;
        bool idle = configured && !room.IsBusy;
        createButton.SetEnabled(idle);
        joinButton.SetEnabled(idle);
        soloButton?.SetEnabled(room == null || !room.IsBusy);
        roomAddress?.SetEnabled(idle);
        if (roomStartButton != null)
        {
            roomStartButton.style.display = configured && room.IsHosting ? DisplayStyle.Flex : DisplayStyle.None;
            roomStartButton.SetEnabled(configured && room.CanStartMatch);
        }
        if (roomStatus != null)
            roomStatus.text = configured ? room.Status : "请配置房间组件、地址框和开局按钮。";
        multiBackButton?.SetEnabled(room == null || !room.IsChangingScene);
    }

    private void OpenCodex()
    {
        OpenInfo("道具图谱");
    }

    private void OpenOptions()
    {
        OpenInfo("选项");
    }

    private void OpenInfo(string title)
    {
        infoTitle.text = title;
        ShowPage(infoPage);
    }

    private void BackHome()
    {
        if (room != null && room.IsBusy)
            room.CloseLobby();
        ShowPage(homePage);
        RefreshRoom();
    }

    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
