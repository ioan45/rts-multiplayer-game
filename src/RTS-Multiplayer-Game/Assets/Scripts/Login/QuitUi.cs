using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class QuitUi : MonoBehaviour
{
    [SerializeField]
    private GameObject mainUi;
    [SerializeField]
    private Button yesButton;
    [SerializeField]
    private Button noButton;
    private Animator showPanelAnimator;

    private void Awake()
    {
        yesButton.onClick.AddListener(OnYesButtonPress);
        yesButton.onClick.AddListener(() => CoreUi.Instance.PlaySoundEffect(CoreUi.SoundEffect.BUTTON_CLICK));
        noButton.onClick.AddListener(OnNoButtonPress);
        noButton.onClick.AddListener(() => CoreUi.Instance.PlaySoundEffect(CoreUi.SoundEffect.BUTTON_CLICK));
        showPanelAnimator = transform.parent.GetComponentInChildren<Animator>();
    }

    private void OnEnable()
    {
        showPanelAnimator.SetTrigger("ShowPanel");
    }

    public void OnYesButtonPress()
    {
        // This call will make the AsyncCleanupManager execute it's subscribed functions.
        Application.Quit();
    }

    public void OnNoButtonPress()
    {
        this.transform.parent.gameObject.SetActive(false);
        mainUi.SetActive(true);   
    }
}
