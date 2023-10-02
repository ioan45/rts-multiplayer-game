using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MainUi : MonoBehaviour
{
    [field:SerializeField]
    public GameObject MessageText { get; private set; }

    [SerializeField]
    private GameObject signInUi;
    [SerializeField]
    private GameObject signUpUi;
    [SerializeField]
    private GameObject creditsUi;
    [SerializeField]
    private GameObject quitUi;
    [SerializeField]
    private Button signInButton;
    [SerializeField]
    private Button signUpButton;
    [SerializeField]
    private Button creditsButton;
    [SerializeField]
    private Button quitButton;
    private Animator showPanelAnimator;

    private void Awake()
    {
        MessageText.GetComponentInChildren<TMP_Text>().text = "";
        signInButton.onClick.AddListener(OnSignInButtonPress);
        signInButton.onClick.AddListener(() => CoreUi.Instance.PlaySoundEffect(CoreUi.SoundEffect.BUTTON_CLICK));
        signUpButton.onClick.AddListener(OnSignUpButtonPress);
        signUpButton.onClick.AddListener(() => CoreUi.Instance.PlaySoundEffect(CoreUi.SoundEffect.BUTTON_CLICK));
        creditsButton.onClick.AddListener(OnCreditsButtonPress);
        creditsButton.onClick.AddListener(() => CoreUi.Instance.PlaySoundEffect(CoreUi.SoundEffect.BUTTON_CLICK));
        quitButton.onClick.AddListener(OnQuitButtonPress);
        quitButton.onClick.AddListener(() => CoreUi.Instance.PlaySoundEffect(CoreUi.SoundEffect.BUTTON_CLICK));
        showPanelAnimator = transform.parent.GetComponentInChildren<Animator>();
    }

    private void OnEnable()
    {
        if (string.IsNullOrEmpty(MessageText.GetComponentInChildren<TMP_Text>().text))
            MessageText.SetActive(false);
        else
            MessageText.SetActive(true);
        showPanelAnimator.SetTrigger("ShowPanel");
    }

    private void OnDisable()
    {
        MessageText.GetComponentInChildren<TMP_Text>().text = "";
    }

    private void OnSignInButtonPress()
    {
        transform.parent.gameObject.SetActive(false);
        signInUi.SetActive(true);
    }

    private void OnSignUpButtonPress()
    {
        transform.parent.gameObject.SetActive(false);
        signUpUi.SetActive(true);
    }

    private void OnCreditsButtonPress()
    {
        transform.parent.gameObject.SetActive(false);
        creditsUi.SetActive(true);
    }

    private void OnQuitButtonPress()
    {
        transform.parent.gameObject.SetActive(false);
        quitUi.SetActive(true);
    }
}
