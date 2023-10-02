using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using TMPro;

public class GameOverUiManager : MonoBehaviour
{
    public event System.Action onQuitButtonPress;

    [SerializeField]
    private TMP_Text winText;
    [SerializeField]
    private TMP_Text trophiesGainedText;
    [SerializeField]
    private TMP_Text goldGainedText;
    [SerializeField]
    private TMP_Text unitGainedText;
    [SerializeField]
    private TMP_Text autoQuittingText;
    [SerializeField]
    private Button quitButton;
    private uint autoQuittingTimeShown;
    private Animator showPanelController;

    private void Awake()
    {
        autoQuittingTimeShown = 0;
        showPanelController = transform.parent.GetComponentInChildren<Animator>();
        winText.gameObject.SetActive(true);
        trophiesGainedText.gameObject.SetActive(true);
        goldGainedText.gameObject.SetActive(true);
        unitGainedText.gameObject.SetActive(true);
        autoQuittingText.gameObject.SetActive(true);
        quitButton.gameObject.SetActive(true);

        quitButton.onClick.AddListener(() => onQuitButtonPress?.Invoke());
        quitButton.onClick.AddListener(() => CoreUi.Instance.PlaySoundEffect(CoreUi.SoundEffect.BUTTON_CLICK));
    }

    public void SetDataDisplayed(bool winner, int trophiesGained, int goldGained, string unitGainedName, uint initialQuittingTime)
    {
        autoQuittingText.text = $"Quitting in {initialQuittingTime}s";
        goldGainedText.text = $"+{goldGained} Gold";
        if (trophiesGained < 0)
            trophiesGainedText.text = $"{trophiesGained} Trophies";
        else
            trophiesGainedText.text = $"+{trophiesGained} Trophies";
        if (string.IsNullOrEmpty(unitGainedName))
            unitGainedText.text = "Sorry, you didn't get a new minion.";
        else
            unitGainedText.text = $"New minion gained: {unitGainedName}";
        if (winner)
            winText.text = "You won!";
        else
            winText.text = "You lost!";
    }

    public void UpdateQuittingTime(uint seconds)
    {
        if (seconds != autoQuittingTimeShown)
        {
            autoQuittingText.text = $"Quitting in {seconds}s";
            autoQuittingTimeShown = seconds;
        }
    }

    public void ShowGameOverUi(bool showUi)
    {
        this.transform.parent.gameObject.SetActive(showUi);
        if (showUi)
            showPanelController.SetTrigger("ShowPanel");
    }
}
