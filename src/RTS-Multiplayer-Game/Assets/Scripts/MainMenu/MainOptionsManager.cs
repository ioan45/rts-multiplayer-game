using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class MainOptionsManager : MonoBehaviour, IMainMenuPanel
{
    public static MainOptionsManager Instance { get; private set; }

    [SerializeField]
    private GameObject mainOptionsPanel;
    [SerializeField]
    private Button playButton;
    [SerializeField]
    private Button deckButton;
    [SerializeField]
    private Button optionsButton;
    [SerializeField]
    private Button signOutButton;
    [SerializeField]
    private Button quitButton;
    private IMainMenuPanel activePanel;
    private Animator showPanelController;

    private void Awake()
    {
        if (!IsSingletonInstance())
            return;
        
        playButton.onClick.AddListener(OnPlayButtonPress);
        playButton.onClick.AddListener(() => CoreUi.Instance.PlaySoundEffect(CoreUi.SoundEffect.BUTTON_CLICK));
        deckButton.onClick.AddListener(OnDeckButtonPress);
        deckButton.onClick.AddListener(() => CoreUi.Instance.PlaySoundEffect(CoreUi.SoundEffect.BUTTON_CLICK));
        optionsButton.onClick.AddListener(OnOptionsButtonPress);
        optionsButton.onClick.AddListener(() => CoreUi.Instance.PlaySoundEffect(CoreUi.SoundEffect.BUTTON_CLICK));
        signOutButton.onClick.AddListener(OnSignOutButtonPress);
        signOutButton.onClick.AddListener(() => CoreUi.Instance.PlaySoundEffect(CoreUi.SoundEffect.BUTTON_CLICK));
        quitButton.onClick.AddListener(OnQuitButtonPress);
        quitButton.onClick.AddListener(() => CoreUi.Instance.PlaySoundEffect(CoreUi.SoundEffect.BUTTON_CLICK));
        activePanel = null;
        showPanelController = mainOptionsPanel.GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        MainMenuManager.Instance.SignalComponentInitialized();
    }

    public void ShowPanel()
    {
        mainOptionsPanel.SetActive(true);
        showPanelController.SetTrigger("ShowPanel");
    }

    public void HidePanel()
    {
        mainOptionsPanel.SetActive(false);
    }

    public void OnPlayButtonPress()
    {
        if (activePanel != null)
            activePanel.HidePanel();
        PlayPanelManager.Instance.ShowPanel();
        activePanel = PlayPanelManager.Instance;
    }

    public void OnDeckButtonPress()
    {
        if (activePanel != null)
            activePanel.HidePanel();
        DeckPanelsManager.Instance.ShowPanel();
        activePanel = DeckPanelsManager.Instance;
    }

    public void OnOptionsButtonPress()
    {
        if (activePanel != null)
            activePanel.HidePanel();
        OptionsPanelManager.Instance.ShowPanel();
        activePanel = OptionsPanelManager.Instance;
    }

    public async void OnSignOutButtonPress()
    {
        // Cannot sign out while matchmaking.
        if (PlayPanelManager.Instance.IsFindingMatch)
            return;

        CoreUi.Instance.LoadingScreen.SetActive(true);

        await DeleteUserSession(UserData.SignedInUserData.username, UserData.SignedInUserData.loginSessionToken);
        PlayerPrefs.DeleteKey("UserSessionToken");
        UserData.SignedInUserData = null;
        
        ClientSceneManager.Instance.ChangeSceneLocallyAsync("Login", true);
    }

    public void OnQuitButtonPress()
    {
        if (activePanel != null)
            activePanel.HidePanel();
        QuitPanelManager.Instance.ShowPanel();
        activePanel = QuitPanelManager.Instance;
    }

    private async Task DeleteUserSession(string username, string sessionToken)
    {
#if USING_LOCAL_SERVERS
        const string requestURL = "http://localhost/delete_user_session";
#else
        const string requestURL = "https:// - /delete_user_session";
#endif
        WWWForm form = new WWWForm();
        form.AddField("Username", username);
        form.AddField("SessionToken", sessionToken);

        UnityWebRequest req = UnityWebRequest.Post(requestURL, form);
        req.disposeUploadHandlerOnDispose = true;
        req.disposeDownloadHandlerOnDispose = true;
        AsyncOperation asyncOp = req.SendWebRequest();
        while (!asyncOp.isDone)
            await Task.Delay(100);

        if (req.result != UnityWebRequest.Result.Success)
            Debug.Log("OnSignOut: DeleteUserSession: Web request failed! " + req.error);
        else if (req.downloadHandler.text != "1")
            Debug.Log("OnSignOut: DeleteUserSession: Operation failed!");
        
        req.Dispose();
    }

    private bool IsSingletonInstance()
    {
        if (Instance != null)
        {
            Destroy(this.gameObject);
            return false;
        }
        else
        {
            Instance = this;
            return true;
        }
    }
}
