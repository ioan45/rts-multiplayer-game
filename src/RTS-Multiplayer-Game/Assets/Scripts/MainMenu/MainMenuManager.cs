using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class MainMenuManager : MonoBehaviour
{
    public static MainMenuManager Instance { get; private set; }
    public bool MainMenuInitialized { get; private set; }
    public event System.Action onMainMenuInitialized;

    private const float cameraRotationSpeed = 3;  // angles per second

    [SerializeField]
    private TMP_Text playerNameLabel;
    private int awaitedInitsCount;
    private int completedInitsCount;
    private Camera mainCamera;
    private bool canRotateCamera;

    private void Awake()
    {
        if (!IsSingletonInstance())
            return;

        MainMenuInitialized = false;
        awaitedInitsCount = 6;
        completedInitsCount = 0;
        mainCamera = CoreUi.Instance.mainCamera;
        canRotateCamera = true;
        onMainMenuInitialized += OnMainMenuInitialized;

        playerNameLabel.text = UserData.SignedInUserData.playerName;
    }

    private void Start()
    {
        InitBackground();
        ClientMatchmakingManager.Instance.onMatchStarted += OnMatchStarted;
        MainOptionsManager.Instance.HidePanel();

        var audioComp = GetComponent<AudioSource>();
        audioComp.loop = true;
        audioComp.volume = CoreUi.Instance.SoundVolume / 100.0f;
        audioComp.Play();

        SignalComponentInitialized();
    }

    private void FixedUpdate()
    {
        if (canRotateCamera)
            mainCamera.transform.Rotate(new Vector3(0, cameraRotationSpeed * Time.deltaTime, 0), Space.World);
    }

    public void SignalComponentInitialized()
    {
        ++completedInitsCount;
        if (completedInitsCount == awaitedInitsCount)
        {
            MainMenuInitialized = true;
            onMainMenuInitialized?.Invoke();
        }
    }

    private void InitBackground()
    {
        Vector3 lightPosition = new Vector3(92, 156, -93);
        Vector3 lightRotationAngles = new Vector3(60, 0, 0);
        Vector3 cameraPos = new Vector3(-32, 267, -56);
        Vector3 cameraInitRotation = new Vector3(39, 0, 0);

        Light light = CoreManager.Instance.LightComponent;
        light.transform.position = lightPosition;
        light.transform.rotation = Quaternion.identity;
        light.transform.Rotate(lightRotationAngles);

        mainCamera.transform.position = cameraPos;
        mainCamera.transform.rotation = Quaternion.identity;
        mainCamera.transform.Rotate(cameraInitRotation);
    }

    private void OnMainMenuInitialized()
    {
        CoreUi.Instance.LoadingScreen.SetActive(false);
        MainOptionsManager.Instance.ShowPanel();
    }

    private void OnMatchStarted()
    {
        canRotateCamera = false;
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
