using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class PlayPanelManager : MonoBehaviour, IMainMenuPanel
{
    public static PlayPanelManager Instance { get; private set; }
    public bool IsFindingMatch { get; set; }

    [SerializeField]
    private GameObject playPanel;
    [SerializeField]
    private GameObject entryPanel;
    [SerializeField]
    private GameObject findingMatchPanel;
    [SerializeField]
    private Button findMatchButton;
    [SerializeField]
    private Button cancelMatchmakingButton;
    [SerializeField]
    private TMP_Text matchmakingDurationText;
    [SerializeField]
    private TMP_Text trophiesIndicator;
    private float matchmakingDuration;  // in seconds
    private Animator showPanelController;

    private void Awake()
    {
        if (!IsSingletonInstance())
            return;

        findingMatchPanel.SetActive(false);
        entryPanel.SetActive(true);
        playPanel.SetActive(false);

        findMatchButton.interactable = false;
        findMatchButton.onClick.AddListener(OnFindMatchButtonPress);
        findMatchButton.onClick.AddListener(() => CoreUi.Instance.PlaySoundEffect(CoreUi.SoundEffect.BUTTON_CLICK));
        cancelMatchmakingButton.onClick.AddListener(OnCancelMatchmakingButtonPress);
        cancelMatchmakingButton.onClick.AddListener(() => CoreUi.Instance.PlaySoundEffect(CoreUi.SoundEffect.BUTTON_CLICK));

        trophiesIndicator.text = $"Trophies : {UserData.SignedInUserData.trophies}";
        IsFindingMatch = false;
        matchmakingDuration = 0.0f;
        showPanelController = playPanel.GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        var matchmakingManager = ClientMatchmakingManager.Instance;
        matchmakingManager.CallWhenInitialized(OnMatchmakingManagerInitialized);
        matchmakingManager.onMatchStarted += OnMatchStarted;
        matchmakingManager.onMatchmakingCancelled += OnMatchmakingCancelled;   
        MainMenuManager.Instance.SignalComponentInitialized();
    }

    private void Update()
    {
        // Update elapsed matchmaking time.
        if (IsFindingMatch)
        {
            matchmakingDuration += Time.deltaTime;
            int duration = (int)matchmakingDuration;
            if (duration != (int)(matchmakingDuration - Time.deltaTime))
            {
                int minutes = duration / 60;
                int seconds = duration % 60;
                matchmakingDurationText.text = $"{(minutes < 10 ? "0" : "")}{minutes}:{(seconds < 10 ? "0" : "")}{seconds}";
            }
        }
    }

    public void ShowPanel()
    {
        playPanel.SetActive(true);
        showPanelController.SetTrigger("ShowPanel");
    }

    public void HidePanel()
    {
        playPanel.SetActive(false);
    }

    private void OnMatchmakingManagerInitialized()
        // Enables matchmaking.
        => findMatchButton.interactable = true;

    public void OnFindMatchButtonPress()
    {
        entryPanel.SetActive(false);
        findingMatchPanel.SetActive(true);
        matchmakingDuration = 0.0f;
        matchmakingDurationText.text = "00:00";
        IsFindingMatch = true;
        ClientMatchmakingManager.Instance.FindMatch();
    }

    public void OnCancelMatchmakingButtonPress()
        => ClientMatchmakingManager.Instance.CancelMatchmaking();

    private void OnMatchStarted()
    {
        // Web server connection testing stops when the game starts. From that point,
        // the disconnection to the game server is handled by the automatic reconnection procedure.
        // The tesing resumes once the disconnection from the game server is accepted.
        ClientNetworkManager.Instance.WebServerConnectionTesting.StopRequestLoop();
        ClientNetworkManager.Instance.EnableAutomaticReconnection();
        SceneManager.UnloadSceneAsync("MainMenu");
        CoreUi.Instance.PlaySoundEffect(CoreUi.SoundEffect.MATCH_FOUND);
    }

    private void OnMatchmakingCancelled()
    {
        findingMatchPanel.SetActive(false);
        entryPanel.SetActive(true);
        IsFindingMatch = false;
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
