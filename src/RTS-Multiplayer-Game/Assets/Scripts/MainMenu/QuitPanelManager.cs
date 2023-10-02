using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class QuitPanelManager : MonoBehaviour, IMainMenuPanel
{
    public static QuitPanelManager Instance { get; private set; }

    [SerializeField]
    private GameObject quitPanel;
    [SerializeField]
    private Button quitButton;
    private Animator showPanelController;

    private void Awake()
    {
        if (!IsSingletonInstance())
            return;
        
        quitButton.onClick.AddListener(OnQuitButtonPress);
        quitButton.onClick.AddListener(() => CoreUi.Instance.PlaySoundEffect(CoreUi.SoundEffect.BUTTON_CLICK));
        quitPanel.SetActive(false);
        showPanelController = quitPanel.GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        MainMenuManager.Instance.SignalComponentInitialized();
    }

    public void ShowPanel()
    {
        quitPanel.SetActive(true);
        showPanelController.SetTrigger("ShowPanel");
    }

    public void HidePanel()
    {
        quitPanel.SetActive(false);
    }

    public void OnQuitButtonPress()
    {
        // This call will make the AsyncCleanupManager execute it's subscribed functions.
        Application.Quit();
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
