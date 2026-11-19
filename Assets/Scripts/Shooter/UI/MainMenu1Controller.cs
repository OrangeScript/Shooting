
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class MainMenu1Controller : MonoBehaviour
{
    [Header("菜单样式")]
    [SerializeField] private StyleSheet menuStyle;

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

        soloButton.clicked += StartSolo;
        multiButton.clicked += OpenMultiplayer;
        codexButton.clicked += OpenCodex;
        optionsButton.clicked += OpenOptions;
        quitButton.clicked += QuitGame;
        multiBackButton.clicked += BackHome;
        infoBackButton.clicked += BackHome;

        // 尚未接入网络功能，先避免无效点击
        root.Q<Button>("CreateButton").SetEnabled(false);
        root.Q<Button>("JoinButton").SetEnabled(false);

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
        SceneManager.LoadScene("Game01");
    }

    private void OpenMultiplayer()
    {
        ShowPage(multiplayerPage);
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
        ShowPage(homePage);
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
