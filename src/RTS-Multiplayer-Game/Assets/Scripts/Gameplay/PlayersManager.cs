using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class PlayerData
{
    public ClientData clientData;
    public GameplayData gameplayData;
}

public class PlayersManager : NetworkBehaviour
{
    public static PlayersManager Instance { get; private set; }

    [field:SerializeField]
    public GameplayData Player1GameplayData { get; private set; }
    [field:SerializeField]
    public GameplayData Player2GameplayData { get; private set; }
    [field:SerializeField]
    public PlayerUnitBehaviour Player1CombatUnitObj;
    [field:SerializeField]
    public PlayerUnitBehaviour Player2CombatUnitObj;

    public GameplayData OwnGameplayData { get; private set; }        // For each player (client)
    public GameplayData EnemyGameplayData { get; private set; }      //
    public string EnemyPlayerName { get; private set; }
    // Callbacks should not be added in Start methods because        //
    // this might be already called until then (or a check for       //
    // OwnGameplayData != null can be done).                         // 
    public event System.Action onDataReceived;                       //

    public PlayerData Player1Data { get; private set; }              // For server
    public PlayerData Player2Data { get; private set; }              //

    [SerializeField]
    private Material allyMainColor;
    [SerializeField]
    private Material enemyMainColor;

    private void Awake()
    {
        if (!IsSingletonInstance())
            return;

        if (NetworkManager.Singleton.IsServer)
        {
            var svNetManager = ServerNetworkManager.Instance;
            Player1Data = new PlayerData();
            Player1Data.clientData = svNetManager.Client1Data;
            Player1Data.gameplayData = Player1GameplayData;
            Player2Data = new PlayerData();
            Player2Data.clientData = svNetManager.Client2Data;
            Player2Data.gameplayData = Player2GameplayData;

            var netManager = NetworkManager.Singleton;
            netManager.OnClientConnectedCallback += OnPlayerConnected;
            netManager.SceneManager.OnLoadComplete += OnGameplaySceneLoadedForPlayer;
            netManager.SceneManager.OnLoadEventCompleted += OnGameplaySceneLoadedForAll;

            ServerStateMachine.Instance.AddOnEnterCallback(ServerStateMachine.State.GAME_OVER, OnServerEnterGameOverState);
        }
        else
        {
            OwnGameplayData = null;
            EnemyGameplayData = null;
            onDataReceived += OnPlayerGameplayDataReceived;
        }
    }

    private void OnGameplaySceneLoadedForPlayer(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
    {
        if (sceneName == "Gameplay")
        {
            if (clientId == Player1Data.clientData.networkId && Player1Data.clientData.isConnected)
                AssignDataToClient(Player1Data, Player2Data);
            else if (clientId == Player2Data.clientData.networkId && Player2Data.clientData.isConnected)
                AssignDataToClient(Player2Data, Player1Data);
        }
    }

    private void OnGameplaySceneLoadedForAll(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnGameplaySceneLoadedForAll;
        NetworkManager.Singleton.SceneManager.OnLoadComplete -= OnGameplaySceneLoadedForPlayer;
    }

    private void OnPlayerConnected(ulong clientId)
    {
        if (IsSpawned)
        {
            if (clientId == Player1Data.clientData.networkId)
                AssignDataToClient(Player1Data, Player2Data);
            else if (clientId == Player2Data.clientData.networkId)
                AssignDataToClient(Player2Data, Player1Data);
        }
    }

    private void AssignDataToClient(PlayerData clientPlayerData, PlayerData enemyPlayerData)
    {
        ClientRpcParams rpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[]{ clientPlayerData.clientData.networkId }
            }
        };
        AssignDataClientRpc(clientPlayerData.gameplayData.playerNumber, enemyPlayerData.clientData.playerName, rpcParams);
    }

    [ClientRpc]
    private void AssignDataClientRpc(uint playerNumber, string enemyPlayerName, ClientRpcParams clientRpcParams)
    {
        OwnGameplayData = playerNumber == 1 ? Player1GameplayData : Player2GameplayData;
        EnemyGameplayData = playerNumber == 1 ? Player2GameplayData : Player1GameplayData;
        EnemyPlayerName = enemyPlayerName;
        onDataReceived?.Invoke();
    }

    private void OnPlayerGameplayDataReceived()
    {
        // Init castles colors.
        var castle1MR = Player1CombatUnitObj.GetComponent<MeshRenderer>();
        var castle2MR = Player2CombatUnitObj.GetComponent<MeshRenderer>();
        if (OwnGameplayData.playerNumber == 1)
        {
            Material[] materials;
            materials = castle1MR.materials;
            materials[3] = allyMainColor;
            castle1MR.materials = materials;
            materials = castle2MR.materials;
            materials[3] = enemyMainColor;
            castle2MR.materials = materials;
        }
        else
        {
            Material[] materials;
            materials = castle1MR.materials;
            materials[3] = enemyMainColor;
            castle1MR.materials = materials;
            materials = castle2MR.materials;
            materials[3] = allyMainColor;
            castle2MR.materials = materials;
        }
    }

    private void OnServerEnterGameOverState()
    {
        Player1CombatUnitObj.CurrentBasicStats.CanTakeDamage = false;
        Player2CombatUnitObj.CurrentBasicStats.CanTakeDamage = false;
    }

    public override void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientConnectedCallback -= OnPlayerConnected;
        base.OnDestroy();
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
