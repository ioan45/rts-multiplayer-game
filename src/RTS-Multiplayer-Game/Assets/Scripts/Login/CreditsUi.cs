using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CreditsUi : MonoBehaviour
{
    [SerializeField]
    private GameObject mainUi;
    [SerializeField]
    private Button backButton;
    private Animator showPanelAnimator;

    private void Awake()
    {
        backButton.onClick.AddListener(OnBackButtonPress);
        backButton.onClick.AddListener(() => CoreUi.Instance.PlaySoundEffect(CoreUi.SoundEffect.BUTTON_CLICK));
        showPanelAnimator = transform.parent.GetComponentInChildren<Animator>();
    }

    private void OnEnable()
    {
        showPanelAnimator.SetTrigger("ShowPanel");
    }

    private void OnBackButtonPress()
    {
        transform.parent.gameObject.SetActive(false);
        mainUi.SetActive(true);
    }
}
