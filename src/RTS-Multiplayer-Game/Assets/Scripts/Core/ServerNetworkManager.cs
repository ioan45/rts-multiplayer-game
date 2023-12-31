using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

public class ServerNetworkManager : MonoBehaviour
{
    public enum DisconnectReason
    {
        SERVER_ACCESS_DENIED,
        NOT_ACCEPTING_CONNECTIONS,
        WAITING_FOR_PLAYERS_TIMEOUT,
        GAME_ENDED,
        SERVER_SHUTDOWN
    }

    public static ServerNetworkManager Instance { get; private set; }

    // Indicates if the server, currently, can accept clients or not.
    public bool CanAcceptConnections { get; set; }
    // Indicates the number of clients that are approved, connected and synchronized.
    public ushort PlayersCount { get => playersCount; set => SetPlayersCount(value); }
    // The server can have at most 2 clients connected. This is the data of the first client.
    public ClientData Client1Data { get; private set; }
    // The server can have at most 2 clients connected. This is the data of the second client.
    public ClientData Client2Data { get; private set; }
    
    // Called when the value of the PlayersCount property changes.
    public event System.Action<ushort> onPlayersCountChange;
    
    // The duration (in seconds) for which the server will wait for players to connect before shutting down. Once both players
    // are connected for the first time, the server will begin preparing the game and it won't shut down until the game ends.
    private const int waitingPlayersDuration = 60;
    // The size of the server password (in bytes) which is generated by the web server.
    private const int serverPasswordSize = 64;
    
    // The coroutine used to shut down the server once the waiting time elapsed without both players being connected.
    private IEnumerator waitingTimeoutCoroutine;
    private ushort playersCount;
    private NetworkManager networkManager;

    private void Awake()
    {
        if (!IsSingletonInstance())
            return;
        
        CanAcceptConnections = false;
        Client1Data = null;
        Client2Data = null;
        waitingTimeoutCoroutine = null;
        playersCount = 0;
    }

    private void Start()
    {
        networkManager = NetworkManager.Singleton;
        networkManager.ConnectionApprovalCallback = ClientConnectionApprovalCheck;
        networkManager.OnClientConnectedCallback += OnClientConnected;
        networkManager.OnClientDisconnectCallback += OnClientDisconnect;
        ServerStateMachine.Instance.AddOnEnterCallback(ServerStateMachine.State.GAME_OVER, OnEnterGameOverState);
        
        CoreManager.Instance.SignalComponentInitialized();
    }

    public void StartTheServer()
    {
        networkManager.StartServer();
        networkManager.SceneManager.SetClientSynchronizationMode(LoadSceneMode.Additive);
    }

    public void SetServerConnectionData(string ipv4Address, ushort port)
    {
        var unityTransport = networkManager.GetComponent<UnityTransport>();
        unityTransport.SetConnectionData(ipv4Address, port);
    }

    private void ClientConnectionApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)                                     
    {
        if (!CanAcceptConnections || PlayersCount == 2 || ServerMultiplayManager.Instance.ServerPassword == null)
        {
            response.Approved = false;
            response.Reason = DisconnectReason.NOT_ACCEPTING_CONNECTIONS.ToString();
            return;
        }

        ClientConnectionPayload clientPayload = ClientConnectionPayload.FromBytesArray(request.Payload);
        if (string.IsNullOrEmpty(clientPayload.username) 
            || string.IsNullOrEmpty(clientPayload.playerName)
            || string.IsNullOrEmpty(clientPayload.serverPassword))
        {
            response.Approved = false;
            response.Reason = DisconnectReason.SERVER_ACCESS_DENIED.ToString();
            return;
        }
        if (!clientPayload.serverPassword.Equals(ServerMultiplayManager.Instance.ServerPassword))
        {
            response.Approved = false;
            response.Reason = DisconnectReason.SERVER_ACCESS_DENIED.ToString();
            return;
        }
        if (Client1Data != null && Client2Data != null)
        {
            if (clientPayload.username != Client1Data.username && clientPayload.username != Client2Data.username)
            {
                response.Approved = false;
                response.Reason = DisconnectReason.SERVER_ACCESS_DENIED.ToString();
                return;
            }
            if (clientPayload.playerName != Client1Data.playerName && clientPayload.playerName != Client2Data.playerName)
            {
                response.Approved = false;
                response.Reason = DisconnectReason.SERVER_ACCESS_DENIED.ToString();
                return;
            }
        }

        var serverState = ServerStateMachine.Instance.GetCurrentState();
        if (serverState == ServerStateMachine.State.WAITING_PLAYERS_TO_CONNECT)
        {
            // At this point, the client is approved.
            
            if (Client1Data != null && Client1Data.username == clientPayload.username)
                Client1Data.networkId = request.ClientNetworkId;
            else if (Client2Data != null && Client2Data.username == clientPayload.username)
                Client2Data.networkId = request.ClientNetworkId;
            else
            {
                var newPlayerData = new ClientData();
                newPlayerData.username = clientPayload.username;
                newPlayerData.playerName = clientPayload.playerName;
                newPlayerData.networkId = request.ClientNetworkId;
                if (Client1Data == null)
                    Client1Data = newPlayerData;
                else
                    Client2Data = newPlayerData;
                MarkPlayerAsInMatch(newPlayerData.username);
            }
            
            response.CreatePlayerObject = false;
            response.Approved = true;
        }
        else if (serverState == ServerStateMachine.State.PREPARING_GAME || serverState == ServerStateMachine.State.IN_GAME)
        {
            // At this point, the client is approved.

            if (clientPayload.username == Client1Data.username)
                Client1Data.networkId = request.ClientNetworkId;
            else
                Client2Data.networkId = request.ClientNetworkId;
            
            response.CreatePlayerObject = false;
            response.Approved = true;
        }
        else
        {
            response.Approved = false;
            response.Reason = DisconnectReason.NOT_ACCEPTING_CONNECTIONS.ToString();
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        PlayersCount++;
        if (clientId == Client1Data.networkId)
            Client1Data.isConnected = true;
        else
            Client2Data.isConnected = true;
    }

    private void OnClientDisconnect(ulong clientId)
    {
        PlayersCount--;
        if (clientId == Client1Data.networkId)
            Client1Data.isConnected = false;
        else
            Client2Data.isConnected = false;
    }

    private async void MarkPlayerAsInMatch(string playerUsername)
    {
        // Async method is better than coroutine here (and for some similar requests) because it will surely call req.Dispose().
        // If the request coroutine is stopped while waiting for web response, then the req.Dispose() will not be called.
        // As a possible downside, the request method cannot be stopped from completing execution.
        
#if USING_LOCAL_SERVERS
        const string requestURL = "http://localhost/mark_user_as_in_match";
#else
        const string requestURL = "https:// - /mark_user_as_in_match";
#endif
        WWWForm form = new WWWForm();
        form.AddField("Username", playerUsername);
        form.AddField("Ip", ServerMultiplayManager.Instance.ServerIp);
        form.AddField("Port", ServerMultiplayManager.Instance.ServerPort);

        UnityWebRequest req = UnityWebRequest.Post(requestURL, form);
        req.disposeUploadHandlerOnDispose = true;
        req.disposeDownloadHandlerOnDispose = true;
        AsyncOperation asyncOp = req.SendWebRequest();
        while (!asyncOp.isDone)
            await Task.Delay(100);
        
        if (req.result != UnityWebRequest.Result.Success)
            Debug.Log("MarkPlayerAsInMatch: Web request failed! " + req.error);
        else if (req.downloadHandler.text != "1")
            Debug.Log("MarkPlayerAsInMatch: Operation failed!");
        
        req.Dispose();
    }

    private void OnEnterGameOverState()
        => UnmarkPlayersAsInMatch();

    public async void UnmarkPlayersAsInMatch()
    {
        // Async method is better than coroutine here (and for some similar requests) because it will surely call req.Dispose().
        // If the request coroutine is stopped while waiting for web response, then the req.Dispose() will not be called.
        // As a possible downside, the request method cannot be stopped from completing execution.

#if USING_LOCAL_SERVERS
        const string requestURL = "http://localhost/unmark_users_as_in_match";
#else
        const string requestURL = "https:// - /unmark_users_as_in_match";
#endif
        WWWForm form = new WWWForm();
        form.AddField("Ip", ServerMultiplayManager.Instance.ServerIp);
        form.AddField("Port", ServerMultiplayManager.Instance.ServerPort);

        UnityWebRequest req = UnityWebRequest.Post(requestURL, form);
        req.disposeUploadHandlerOnDispose = true;
        req.disposeDownloadHandlerOnDispose = true;
        AsyncOperation asyncOp = req.SendWebRequest();
        while (!asyncOp.isDone)
            await Task.Delay(100);
        
        if (req.result != UnityWebRequest.Result.Success)
            Debug.Log("UnmarkPlayersAsInMatch: Web request failed! " + req.error);
        else if (req.downloadHandler.text != "1")
            Debug.Log("UnmarkPlayersAsInMatch: Operation failed!");
        
        req.Dispose();
    }

    public void StartWaitingPlayersCountdown()
    {
        if (waitingTimeoutCoroutine != null)
            StopCoroutine(waitingTimeoutCoroutine);
        waitingTimeoutCoroutine = WaitingPlayersTimeout();
        StartCoroutine(waitingTimeoutCoroutine);
    }

    public void StopWaitingPlayersCountdown()
    {
        if (waitingTimeoutCoroutine != null)
        {
            StopCoroutine(waitingTimeoutCoroutine);
            waitingTimeoutCoroutine = null;
        }
    }

    private IEnumerator WaitingPlayersTimeout()
    {
        yield return new WaitForSeconds(waitingPlayersDuration);
        DisconnectAllClients(DisconnectReason.WAITING_FOR_PLAYERS_TIMEOUT);
        ServerStateMachine.Instance.SetCurrentState(ServerStateMachine.State.SHUTTING_DOWN);
    }

    public void DisconnectAllClients(DisconnectReason reason)
    {
        // Verify if the netcode server is running.
        if (!networkManager.IsListening || !networkManager.IsServer)
            return;
        
        var clientsIdsList = networkManager.ConnectedClientsIds;
        var reasonString = reason.ToString();
        foreach (ulong clientId in clientsIdsList)
            networkManager.DisconnectClient(clientId, reasonString);
    }

    public void OnServerShutdown()
    {
        // At this point, if there are clients that were not disconnected for
        // another reason, they will be disconnected for SERVER_SHUTDOWN reason.
        DisconnectAllClients(DisconnectReason.SERVER_SHUTDOWN);
        
        networkManager.ConnectionApprovalCallback = null;
        networkManager.OnClientConnectedCallback -= OnClientConnected;   
        networkManager.OnClientDisconnectCallback -= OnClientDisconnect;
        if (networkManager.IsListening)
            networkManager.Shutdown();
    }

    private void SetPlayersCount(ushort count)
    {
        if (count != playersCount)
        {
            playersCount = count;
            onPlayersCountChange?.Invoke(count);
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
