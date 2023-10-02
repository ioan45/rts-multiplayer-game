using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class GameplayManager : NetworkBehaviour
{
    public class CameraConfig
    {
        public Vector3 position;
        public Vector3 rotation;
    }

    public static GameplayManager Instance { get; private set; }
    // The signal used by the server to tell the clients if the game is running or not.
    public NetworkVariable<bool> IsInGame { get; private set; }
    // The remaining time of the match (in seconds).
    public NetworkVariable<float> GameTimeRemained { get; private set; }
    public uint WinnerPlayerNumber { get; set; }

    [SerializeField]
    private GameObject inWorldGameUi;
    [SerializeField]
    private GameObject gameUi;
    [SerializeField]
    private GameObject gameMainUi;
    [SerializeField]
    private GameObject gameMenuUi;
    private List<CameraConfig> cameraConfigs;
    private int currentCamCfgIndex;

    private void Awake()
    {
        if (!IsSingletonInstance())
            return;
        
        IsInGame = new NetworkVariable<bool>(false);
        GameTimeRemained = new NetworkVariable<float>(150.0f);
        WinnerPlayerNumber = 0;
        inWorldGameUi.SetActive(true);  // Since the world UI contains network objects, it should be active on server too.

        if (NetworkManager.Singleton.IsServer)
        {
            gameUi.SetActive(false);  // The interactable UI is intended only for clients.
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnGameplaySceneLoadedForAll;
            NetworkManager.Singleton.SceneManager.OnLoadComplete += OnGameplaySceneLoadedOnServer;
            ServerStateMachine.Instance.AddOnEnterCallback(ServerStateMachine.State.IN_GAME, OnServerEnterInGameState);
            ServerStateMachine.Instance.AddOnEnterCallback(ServerStateMachine.State.GAME_OVER, OnServerEnterGameOverState);
        }
        else
        {
            cameraConfigs = new List<CameraConfig>();
            CameraConfig config1 = new CameraConfig();
            config1.position = new Vector3(-37.5f, 709.0f, 217.0f);
            config1.rotation = new Vector3(70.0f, 180.0f, 0.0f);
            CameraConfig config2 = new CameraConfig();
            config2.position = new Vector3(-37.5f, 709.0f, -329.0f);
            config2.rotation = new Vector3(70.0f, 0.0f, 0.0f);
            CameraConfig config3 = new CameraConfig();
            config3.position = new Vector3(-466.0f, 584.7f, -56.0f);
            config3.rotation = new Vector3(62.0f, 90.0f, 0.0f);
            CameraConfig config4 = new CameraConfig();
            config4.position = new Vector3(401.0f, 584.7f, -56.0f);
            config4.rotation = new Vector3(62.0f, 270.0f, 0.0f);
            cameraConfigs.Add(config1);
            cameraConfigs.Add(config2);
            cameraConfigs.Add(config3);
            cameraConfigs.Add(config4);

            gameMenuUi.SetActive(false);
            gameMainUi.SetActive(true);
            gameUi.SetActive(true);

            // After the gameplay scene completes loading, the loading screen will persist until this client
            // receives the player gameplay data and the game starts.
            ClientSceneManager.Instance.AutoHideLoadingScreen = false;

            NetworkManager.Singleton.SceneManager.OnLoadComplete += OnGameplaySceneLoadedOnClient;
        }
    }

    private void Start()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            PlayersManager.Instance.Player1CombatUnitObj.CurrentBasicStats.HealthPoints.onValueChanged += OnPlayer1TakingDamage;
            PlayersManager.Instance.Player2CombatUnitObj.CurrentBasicStats.HealthPoints.onValueChanged += OnPlayer2TakingDamage;
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsClient)
        {
            if (IsInGame.Value == true)
                OnInGameSignalChange(false, true);
            IsInGame.OnValueChanged += OnInGameSignalChange;
        }
    }

    private void Update()
    {
        if (NetworkManager.Singleton.IsServer && IsInGame.Value == true)
        {
            // Update remaining match time.
            GameTimeRemained.Value -= Time.deltaTime;
            if (GameTimeRemained.Value <= 0)
            {
                // Set Game Over.
                float player1Hp = PlayersManager.Instance.Player1CombatUnitObj.CurrentBasicStats.HealthPoints.Value;
                float player2Hp = PlayersManager.Instance.Player2CombatUnitObj.CurrentBasicStats.HealthPoints.Value;
                if (player1Hp > player2Hp)
                    WinnerPlayerNumber = 1;
                else if (player2Hp > player1Hp)
                    WinnerPlayerNumber = 2;
                else
                    WinnerPlayerNumber = (uint)(new System.Random()).Next(1, 3);
                ServerStateMachine.Instance.SetCurrentState(ServerStateMachine.State.GAME_OVER);
            }
        }
    }

    public void ApplyNextCameraConfig()
    {
        Transform cameraTranform = CoreUi.Instance.mainCamera.transform;
        currentCamCfgIndex = (currentCamCfgIndex + 1) % cameraConfigs.Count;
        cameraTranform.position = cameraConfigs[currentCamCfgIndex].position;
        cameraTranform.rotation = Quaternion.identity;
        cameraTranform.Rotate(cameraConfigs[currentCamCfgIndex].rotation);
    }

    private void OnGameplaySceneLoadedOnClient(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
    {
        if (sceneName == "Gameplay")
            PlayersManager.Instance.onDataReceived += OnPlayerGameplayDataReceived;
    }

    private void OnGameplaySceneLoadedOnServer(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
    {
        if (clientId == NetworkManager.ServerClientId && sceneName == "Gameplay")
        {
            NetworkManager.Singleton.SceneManager.OnLoadComplete -= OnGameplaySceneLoadedOnServer;
            // OnLoadComplete registered callbacks are called after the in-scene objects Awake and OnEnable methods
            // but before the Start methods (same goes for UnityEngine.SceneManagement). So the GameObject instantiating
            // operations triggered by in-scene placed scripts should happen in Start() instead of Awake() or OnEnable(). 
            SceneManager.SetActiveScene(SceneManager.GetSceneByName("Gameplay"));
        }
    }

    private void OnGameplaySceneLoadedForAll(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        if (sceneName == "Gameplay")
        {
            // Start the game.
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnGameplaySceneLoadedForAll;
            ServerStateMachine.Instance.SetCurrentState(ServerStateMachine.State.IN_GAME);
        }
    }

    private void OnServerEnterInGameState()
        => IsInGame.Value = true;

    private void OnServerEnterGameOverState()
        => IsInGame.Value = false;

    private void OnInGameSignalChange(bool previousValue, bool newValue)
    {
        // The loading screen becomes hidden if the game started and the player gameplay data has been received and processed.
        // Also, the background music is played.
        if (newValue == true && PlayersManager.Instance.OwnGameplayData != null)
        {
            var audioComp = GetComponent<AudioSource>();
            audioComp.loop = true;
            audioComp.volume = CoreUi.Instance.SoundVolume / 100.0f;
            audioComp.Play();
            CoreUi.Instance.LoadingScreen.SetActive(false);
        }
        else if (newValue == false)
        {
            var audioComp = GetComponent<AudioSource>();
            audioComp.Stop();
        }
    }

    private void OnPlayerGameplayDataReceived()
    {
        // Init camera.
        currentCamCfgIndex = PlayersManager.Instance.OwnGameplayData.initialCameraConfigIndex - 1;
        ApplyNextCameraConfig();

        // The loading screen becomes hidden if the game started and the player gameplay data has been received and processed.
        // Also, the background music is played.
        if (IsInGame.Value == true)
        {
            var audioComp = GetComponent<AudioSource>();
            audioComp.loop = true;
            audioComp.volume = CoreUi.Instance.SoundVolume / 100.0f;
            audioComp.Play();
            CoreUi.Instance.LoadingScreen.SetActive(false);
        }
    }

    private void OnPlayer1TakingDamage(float prevHp, float newHp)
    {
        if (newHp <= 0)
        {
            // Set Game Over.
            WinnerPlayerNumber = 2;
            ServerStateMachine.Instance.SetCurrentState(ServerStateMachine.State.GAME_OVER);
        }
    }

    private void OnPlayer2TakingDamage(float prevHp, float newHp)
    {
        if (newHp <= 0)
        {
            // Set Game Over.
            WinnerPlayerNumber = 1;
            ServerStateMachine.Instance.SetCurrentState(ServerStateMachine.State.GAME_OVER);
        }
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
