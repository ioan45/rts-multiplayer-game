using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using Unity.Services.Core;

public class CoreManager : MonoBehaviour
{
    public static CoreManager Instance { get; private set; }
    public bool CoreInitialized { get; private set; }
    [field: SerializeField]
    public Light LightComponent { get; private set; }
    public int[] FpsCapOptions {get; private set; }

    public event System.Action onCoreInitialized;

    [SerializeField]
    private GameObject networkManager;
    private int awaitedInitsCount;
    private int completedInitsCount; 

#if BYPASS_UNITY_SERVICES

    private void Awake()
    {
        if (!IsSingletonInstance())
            return;

        CoreInitialized = false;
        completedInitsCount = 0;

        var buttonPrefab = Resources.Load<GameObject>("BypassUnityServices/SimpleButton");
        var canvas = GameObject.FindObjectOfType<Canvas>();
        Button serverButton = Instantiate(buttonPrefab, canvas.transform).GetComponent<Button>();
        Button clientButton = Instantiate(buttonPrefab, canvas.transform).GetComponent<Button>();
        
        serverButton.GetComponent<RectTransform>().anchoredPosition = new Vector3(0, 100);
        serverButton.GetComponentInChildren<TMP_Text>().text = "Start Server";
        serverButton.onClick.AddListener(() => {
            Destroy(serverButton.gameObject);
            Destroy(clientButton.gameObject);
            OnStartServerButtonPress();
        });

        clientButton.GetComponent<RectTransform>().anchoredPosition = new Vector3(0, -100);
        clientButton.GetComponentInChildren<TMP_Text>().text = "Start Client";
        clientButton.onClick.AddListener(() => {
            Destroy(serverButton.gameObject);
            Destroy(clientButton.gameObject);
            OnStartClientButtonPress(); 
        });
    }

    private void OnStartServerButtonPress()
    {
        Application.targetFrameRate = 60;

        awaitedInitsCount = 5;
        (new GameObject("ServerStateMachine")).AddComponent<ServerStateMachine>();
        networkManager.AddComponent<ServerNetworkManager>();
        (new GameObject("ServerMultiplayManager")).AddComponent<ServerMultiplayManager>();
        (new GameObject("ServerSceneManager")).AddComponent<ServerSceneManager>();
        (new GameObject("AsyncCleanupManager")).AddComponent<AsyncCleanupManager>();
    }

    private void OnStartClientButtonPress()
    {
        InitAppUserSettings();

        awaitedInitsCount = 3;
        networkManager.AddComponent<ClientNetworkManager>();
        (new GameObject("ClientSceneManager")).AddComponent<ClientSceneManager>();
        (new GameObject("AsyncCleanupManager")).AddComponent<AsyncCleanupManager>();
    }

#else

    private void Awake()
    {
        if (!IsSingletonInstance())
            return;
        
        InitUnityServices();

        CoreInitialized = false;
        completedInitsCount = 0;

        if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
        {
            // Server runtime

            Application.targetFrameRate = 60;

            awaitedInitsCount = 5;
            (new GameObject("ServerStateMachine")).AddComponent<ServerStateMachine>();
            networkManager.AddComponent<ServerNetworkManager>();
            (new GameObject("ServerMultiplayManager")).AddComponent<ServerMultiplayManager>();
            (new GameObject("ServerSceneManager")).AddComponent<ServerSceneManager>();
            (new GameObject("AsyncCleanupManager")).AddComponent<AsyncCleanupManager>();
        }
        else
        {
            // Client runtime
            
            InitAppUserSettings();

            awaitedInitsCount = 3;
            networkManager.AddComponent<ClientNetworkManager>();
            (new GameObject("ClientSceneManager")).AddComponent<ClientSceneManager>();
            (new GameObject("AsyncCleanupManager")).AddComponent<AsyncCleanupManager>();
        }
    }

#endif

    private void Start()
    {
        // NetworkConfig must be the same on server and all clients.
        NetworkManager.Singleton.NetworkConfig.ConnectionApproval = true;
    }

    public void SignalComponentInitialized()
    {
        ++completedInitsCount;
        if (completedInitsCount == awaitedInitsCount)
        {
            CoreInitialized = true;
            onCoreInitialized?.Invoke();
        }
    }

    private async void InitUnityServices()
    {
        try
        {
            await UnityServices.InitializeAsync();
        }
        catch (Exception e)
        {
            Debug.Log("Unity services initialization failed!");
            Debug.LogException(e);
        }
    }

    private void InitAppUserSettings()
    {
        // Init frame rate.
        FpsCapOptions = new int[]{ 30, 60, 120, 240, -1 };
        int savedFpsCap = FpsCapOptions[PlayerPrefs.GetInt("FpsCap", FpsCapOptions.Length - 1)];
        Application.targetFrameRate = savedFpsCap;
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
