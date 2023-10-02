using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class ClientSceneManager : MonoBehaviour
{
    public static ClientSceneManager Instance { get; private set; }

    public bool ReloadSceneOnWebReconnected { get; set; }
    // The loading screen is shown when the scene changing process starts and hidden after the scene is loaded. If this is false,
    // the loading screen is hidden by the specific scene controller after the specific scene initialization is completed.
    // It resets (becomes true) when a scene completes loading (useful for network scene loading signals, no need to manually set to true).
    // It may not function properly if multiple scenes are loaded at the same time.
    public bool AutoHideLoadingScreen { get; set; }

    private string networkSceneCurrentlyLoading;
    private bool lastUsedAutoHideSetting;
    private CoreUi coreUi;
    
    private void Awake()
    {
        if (!IsSingletonInstance())
            return;
        
        AutoHideLoadingScreen = true;
        networkSceneCurrentlyLoading = null;
        lastUsedAutoHideSetting = true;
    }

    private void Start()
    {
        ReloadSceneOnWebReconnected = true;
        coreUi = CoreUi.Instance;
        var clientNetManager = ClientNetworkManager.Instance;
        clientNetManager.OnWebServerConnectionChange += OnWebSvConnectionChange;
        clientNetManager.OnWebServerConnectionChange += OnFirstWebServerCheckPassed;
        clientNetManager.onClientStart += OnNetworkClientStart;
        clientNetManager.onGameDisconnectionAccepted += OnGameDisconnectionAccepted;
        SceneManager.sceneLoaded += OnLocalSceneLoaded;
        SceneManager.sceneLoaded += OnEverySceneLoaded;
        CoreManager.Instance.onCoreInitialized += OnAppCoreInitialized;

        CoreManager.Instance.SignalComponentInitialized();
    }

    private void OnAppCoreInitialized()
        => coreUi.LoadingScreen.SetActive(true);

    private void OnFirstWebServerCheckPassed()
    {
        if (ClientNetworkManager.Instance.IsConnectedToWebServer)
        {
            ClientNetworkManager.Instance.OnWebServerConnectionChange -= OnFirstWebServerCheckPassed;
            // Load the first scene.
            ChangeSceneLocallyAsync("Login", false);
        }
    }

    public void ChangeSceneLocallyAsync(string toScene, bool autoHideLoadingScreen)
    {
        if (toScene == "Core")
            return;
        AutoHideLoadingScreen = autoHideLoadingScreen;
        lastUsedAutoHideSetting = autoHideLoadingScreen;
        Scene activeScene = SceneManager.GetActiveScene();
        coreUi.LoadingScreen.SetActive(true);
        if (activeScene.name == "Core")
            SceneManager.LoadSceneAsync(toScene, LoadSceneMode.Additive);
        else
        {
            AsyncOperation asyncOp = SceneManager.UnloadSceneAsync(activeScene);
            asyncOp.completed += (_) => { SceneManager.LoadSceneAsync(toScene, LoadSceneMode.Additive); };
        }
    }

    public void ReloadActiveSceneLocallyAsync()
    {
        string activeSceneName = SceneManager.GetActiveScene().name;
        ChangeSceneLocallyAsync(activeSceneName, lastUsedAutoHideSetting);
    }

    private void OnEverySceneLoaded(Scene loadedScene, LoadSceneMode loadSceneMode)
    {
        // OnLoadComplete registered callbacks are called after the in-scene objects Awake and OnEnable methods
        // but before the Start methods (same goes for UnityEngine.SceneManagement). So the GameObject instantiating
        // operations triggered by in-scene placed scripts should happen in Start() instead of Awake() or OnEnable(). 
        SceneManager.SetActiveScene(loadedScene);
    }

    private void OnLocalSceneLoaded(Scene loadedScene, LoadSceneMode loadSceneMode)
    {
        // The NetworkSceneManager of Netcode uses the engine's SceneManager, so this method will also be called by scenes loaded that way.
        // This method should ignore scenes which were loaded because of the game server signal.
        if (loadedScene.name == networkSceneCurrentlyLoading)
            return;
        
        if (AutoHideLoadingScreen)
            coreUi.LoadingScreen.SetActive(false);
        AutoHideLoadingScreen = true;
    }

    private void OnNetworkClientStart()
    {
        // NetworkManager.SceneManager object exists once the client is started.
        var netSceneManager = NetworkManager.Singleton.SceneManager;
        netSceneManager.VerifySceneBeforeLoading += CanNetworkSceneBeLoaded;
        netSceneManager.DisableValidationWarnings(true);
        netSceneManager.OnLoad += OnNetworkSceneLoadLocally;
        netSceneManager.OnLoadComplete += OnNetworkSceneLoadedLocally;
    }

    private bool CanNetworkSceneBeLoaded(int sceneIndex, string sceneName, LoadSceneMode loadSceneMode)
        => sceneName != "Core" && sceneName != "MainMenu" && sceneName != "Login";
    
    private void OnNetworkSceneLoadLocally(ulong clientId, string sceneName, LoadSceneMode loadSceneMode, AsyncOperation asyncOperation)
    {
        coreUi.LoadingScreen.SetActive(true);
        networkSceneCurrentlyLoading = sceneName;
    }

    private void OnNetworkSceneLoadedLocally(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
    {
        if (AutoHideLoadingScreen)
            coreUi.LoadingScreen.SetActive(false);
        AutoHideLoadingScreen = true;
        networkSceneCurrentlyLoading = null;
    }

    private void OnGameDisconnectionAccepted()
    {
        ChangeSceneLocallyAsync("MainMenu", false);
    }

    private void OnWebSvConnectionChange()
    {
        if (ClientNetworkManager.Instance.IsConnectedToWebServer && ReloadSceneOnWebReconnected)
            ReloadActiveSceneLocallyAsync();
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
