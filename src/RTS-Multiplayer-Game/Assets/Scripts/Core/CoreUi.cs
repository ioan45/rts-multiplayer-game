using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class CoreUi : MonoBehaviour
{
    public enum SoundEffect
    {
        BUTTON_CLICK = 0,
        MATCH_WON = 1,
        MATCH_LOST = 2,
        MATCH_FOUND = 3,
        BATTLE_UNIT_PHYSICAL_ATTACK = 4,
        BATTLE_UNIT_MAGIC_ATTACK = 5,
        BATTLE_UNIT_SPAWN = 6,
        BATTLE_UNIT_DEATH = 7
    }

    public static CoreUi Instance { get; private set; }
    [field:SerializeField]
    public GameObject LoadingScreen { get; private set; }
    [field:SerializeField]
    public GameObject MessageScreen { get; private set; }
    [HideInInspector]
    public Camera mainCamera;
    public int SoundVolume { get; set; }

    [SerializeField]
    private AudioClip buttonClickSound;
    [SerializeField]
    private AudioClip matchWonSound;
    [SerializeField]
    private AudioClip matchLostSound;
    [SerializeField]
    private AudioClip matchFoundSound;
    [SerializeField]
    private AudioClip unitPhysicalAttackSound;
    [SerializeField]
    private AudioClip unitMagicAttackSound;
    [SerializeField]
    private AudioClip unitSpawnSound;
    [SerializeField]
    private AudioClip unitDeathSound;
    private TMP_Text messageTextObj;
    private AudioSource soundEffectSource;
    private AudioClip[] soundEffectsList;

    private void Awake()
    {
        if (!IsSingletonInstance())
            return;

        mainCamera = Camera.main;
        messageTextObj = MessageScreen.transform.Find("Text").GetComponent<TMP_Text>();

        soundEffectSource = GetComponent<AudioSource>();
        SoundVolume = PlayerPrefs.GetInt("SoundVolume", 100);
        soundEffectsList = new AudioClip[System.Enum.GetNames(typeof(SoundEffect)).Length];
        soundEffectsList[0] = buttonClickSound;
        soundEffectsList[1] = matchWonSound;
        soundEffectsList[2] = matchLostSound;
        soundEffectsList[3] = matchFoundSound;
        soundEffectsList[4] = unitPhysicalAttackSound;
        soundEffectsList[5] = unitMagicAttackSound;
        soundEffectsList[6] = unitSpawnSound;
        soundEffectsList[7] = unitDeathSound;

        LoadingScreen.SetActive(false);
        MessageScreen.SetActive(false);

        // TODO: CoreUi is irrelevant on the server application.
        // Checks if this is a server runtime.
        if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
        {
            // The server doesn't receive input.
            Destroy(GetComponent<PlayerInput>());
        }
    }

    private void Start()
    {
        // If it's a client runtime, enable the OnWebSvConnectionChange callback once the core is initialized.
        if (CoreManager.Instance.CoreInitialized && ClientNetworkManager.Instance != null)
            ClientNetworkManager.Instance.OnWebServerConnectionChange += OnWebSvConnectionChange;
        else
            CoreManager.Instance.onCoreInitialized += () => {
                if (ClientNetworkManager.Instance != null)
                    ClientNetworkManager.Instance.OnWebServerConnectionChange += OnWebSvConnectionChange;
            };
    }

    public void ShowMessage(string message)
    {
        messageTextObj.text = message;
        MessageScreen.SetActive(true);
    }

    public void PlaySoundEffect(SoundEffect effect)
    {
        soundEffectSource.PlayOneShot(soundEffectsList[(int)effect], SoundVolume / 100.0f);
    }

    private void OnWebSvConnectionChange()
    {
        if (!ClientNetworkManager.Instance.IsConnectedToWebServer)
            ShowMessage("Connection lost.\nTrying reconnection...");
        else
            MessageScreen.SetActive(false);
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