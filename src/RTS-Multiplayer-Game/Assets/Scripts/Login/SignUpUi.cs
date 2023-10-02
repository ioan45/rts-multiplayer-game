using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SignUpUi : MonoBehaviour
{
    [SerializeField]
    private GameObject mainUi;
    [SerializeField]
    private TMP_InputField usernameInput;
    [SerializeField]
    private TMP_InputField passwordInput;
    [SerializeField]
    private TMP_InputField playerNameInput;
    [SerializeField]
    private TMP_InputField emailInput;
    [SerializeField]
    private GameObject messageText;
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
        messageText.SetActive(false);
        usernameInput.text = "";
        passwordInput.text = "";
        playerNameInput.text = "";
        emailInput.text = "";
        showPanelAnimator.SetTrigger("ShowPanel");
    }

    private void OnBackButtonPress()
    {
        transform.parent.gameObject.SetActive(false);
        mainUi.SetActive(true);
    }
}
