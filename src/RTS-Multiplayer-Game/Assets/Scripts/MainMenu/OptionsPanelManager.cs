using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OptionsPanelManager : MonoBehaviour, IMainMenuPanel
{
    public static OptionsPanelManager Instance { get; private set; }

    [SerializeField]
    private GameObject optionsPanel;
    [SerializeField]
    private Slider fpsSlider;
    [SerializeField]
    private TMP_Text fpsValueText;
    [SerializeField]
    private Slider soundVolumeSlider;
    [SerializeField]
    private TMP_Text soundVolumeValueText;
    private Animator showPanelController;

    private void Awake()
    {
        if (!IsSingletonInstance())
            return;
        
        optionsPanel.SetActive(false);
        showPanelController = optionsPanel.GetComponentInChildren<Animator>();
        
        int[] fpsCapOptions = CoreManager.Instance.FpsCapOptions;
        int fpsCapIndex = PlayerPrefs.GetInt("FpsCap", fpsCapOptions.Length - 1);
        int savedFpsCap = fpsCapOptions[fpsCapIndex];
        fpsSlider.wholeNumbers = true;
        fpsSlider.minValue = 0;
        fpsSlider.maxValue = fpsCapOptions.Length - 1;
        fpsSlider.onValueChanged.AddListener(OnFpsCapChange);
        fpsSlider.value = fpsCapIndex;
        fpsValueText.text = $"{(savedFpsCap == -1 ? "UNLIMITED" : savedFpsCap)} FPS";
        
        int savedSoundVolume = CoreUi.Instance.SoundVolume;
        soundVolumeSlider.wholeNumbers = true;
        soundVolumeSlider.minValue = 0;
        soundVolumeSlider.maxValue = 100;
        soundVolumeSlider.onValueChanged.AddListener(OnSoundVolumeChange);
        soundVolumeSlider.value = savedSoundVolume;
        soundVolumeValueText.text = $"{savedSoundVolume}%";
    }

    private void Start()
    {
        MainMenuManager.Instance.SignalComponentInitialized();
    }

    public void ShowPanel()
    {
        optionsPanel.SetActive(true);
        showPanelController.SetTrigger("ShowPanel");
    }

    public void HidePanel()
    {
        optionsPanel.SetActive(false);
    }

    private void OnFpsCapChange(float newValue)
    {
        int newFpsCap = CoreManager.Instance.FpsCapOptions[(int)newValue];
        Application.targetFrameRate = newFpsCap;
        fpsValueText.text = $"{(newFpsCap == -1 ? "UNLIMITED" : newFpsCap)} FPS";
        PlayerPrefs.SetInt("FpsCap", (int)newValue);
    }

    private void OnSoundVolumeChange(float newValue)
    {
        int newVolume = (int)newValue;
        CoreUi.Instance.SoundVolume = newVolume;
        MainMenuManager.Instance.GetComponent<AudioSource>().volume = newValue / 100.0f;
        soundVolumeValueText.text = $"{newVolume}%";
        PlayerPrefs.SetInt("SoundVolume", newVolume);
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
