using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class MenuUiManager : MonoBehaviour
{
    [SerializeField]
    private GameObject mainGameUi;

    [SerializeField]
    private GameObject mainOptionsUi;
    [SerializeField]
    private GameObject quitUi;
    [SerializeField]
    private GameObject surrenderUi;

    [SerializeField]
    private Button resumeButton;
    [SerializeField]
    private Button surrenderButton;
    [SerializeField]
    private Button quitButton;

    [SerializeField]
    private Button quitYesButton;
    [SerializeField]
    private Button quitNoButton;

    [SerializeField]
    private Button surrYesButton;
    [SerializeField]
    private Button surrNoButton;

    private Animator showMainPanelController;
    private Animator showSurrPanelController;
    private Animator showQuitPanelController;

    private void Awake()
    {
        resumeButton.onClick.AddListener(OnResumeButtonPress);
        resumeButton.onClick.AddListener(() => CoreUi.Instance.PlaySoundEffect(CoreUi.SoundEffect.BUTTON_CLICK));
        surrenderButton.onClick.AddListener(OnSurrenderButtonPress);
        surrenderButton.onClick.AddListener(() => CoreUi.Instance.PlaySoundEffect(CoreUi.SoundEffect.BUTTON_CLICK));
        quitButton.onClick.AddListener(OnQuitButtonPress);
        quitButton.onClick.AddListener(() => CoreUi.Instance.PlaySoundEffect(CoreUi.SoundEffect.BUTTON_CLICK));

        quitYesButton.onClick.AddListener(OnQuitYesButtonPress);
        quitYesButton.onClick.AddListener(() => CoreUi.Instance.PlaySoundEffect(CoreUi.SoundEffect.BUTTON_CLICK));
        quitNoButton.onClick.AddListener(OnQuitNoButtonPress);
        quitNoButton.onClick.AddListener(() => CoreUi.Instance.PlaySoundEffect(CoreUi.SoundEffect.BUTTON_CLICK));

        surrYesButton.onClick.AddListener(OnSurrYesButtonPress);
        surrYesButton.onClick.AddListener(() => CoreUi.Instance.PlaySoundEffect(CoreUi.SoundEffect.BUTTON_CLICK));
        surrNoButton.onClick.AddListener(OnSurrNoButtonPress);
        surrNoButton.onClick.AddListener(() => CoreUi.Instance.PlaySoundEffect(CoreUi.SoundEffect.BUTTON_CLICK));

        showMainPanelController = mainOptionsUi.GetComponentInChildren<Animator>();
        showQuitPanelController = quitUi.GetComponentInChildren<Animator>();
        showSurrPanelController = surrenderUi.GetComponentInChildren<Animator>();
    }

    private void OnEnable()
    {
        mainGameUi.SetActive(false);
        quitUi.SetActive(false);
        surrenderUi.SetActive(false);
        mainOptionsUi.SetActive(true);

        showMainPanelController.SetTrigger("ShowPanel");
    }

    public void OnResumeButtonPress()
    {
        transform.parent.gameObject.SetActive(false);
        mainGameUi.SetActive(true);
    }

    public void OnQuitButtonPress()
    {
        mainOptionsUi.SetActive(false);
        quitUi.SetActive(true);
        showQuitPanelController.SetTrigger("ShowPanel");
    }

    public void OnQuitYesButtonPress()
    {
        // This call will make the AsyncCleanupManager execute it's subscribed functions.
        Application.Quit();
    }

    public void OnQuitNoButtonPress()
    {
        quitUi.SetActive(false);
        mainOptionsUi.SetActive(true);
        showMainPanelController.SetTrigger("ShowPanel");
    }

    public void OnSurrenderButtonPress()
    {
        mainOptionsUi.SetActive(false);
        surrenderUi.SetActive(true);
        showSurrPanelController.SetTrigger("ShowPanel");
    }

    public void OnSurrYesButtonPress()
    {
        GameOverManager.Instance.OnPlayerSurrenderServerRpc(new ServerRpcParams());
    }

    public void OnSurrNoButtonPress()
    {
        surrenderUi.SetActive(false);
        mainOptionsUi.SetActive(true);
        showMainPanelController.SetTrigger("ShowPanel");
    }
}
