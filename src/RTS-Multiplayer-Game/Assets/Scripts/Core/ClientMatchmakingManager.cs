using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Matchmaker;
using Unity.Services.Matchmaker.Models;

public class ClientMatchmakingManager : MonoBehaviour
{
    private enum MatchmakingResultStatus
    {
        SUCCESS,
        FAIL,
        TIMEOUT,
        CANCEL
    }

    private class MatchmakingResultData
    {
        public MatchmakingResultStatus status;
        public string message;
        public string ipv4Address;
        public int port;
    }

    public static ClientMatchmakingManager Instance { get; private set; }
    public bool Initialized { get; private set; }

    public event System.Action onMatchmakingCancelled;
    public event System.Action onMatchStarted;

    private const int findNewMatchDelay = 1000; // in milliseconds
    private const int pollingTicketResultDelay = 1000;  // in milliseconds, docs recommend it to be 1000ms
    private const int connectionAttemptDelay = 1;  // in seconds
    private const int maxConnectionAttempts = 10;
    private const int passwordGetAttemptDelay = 5000;  // in milliseconds
    private const int maxPasswordGetAttempts = 24;

    private event System.Action onInitialized;

    private bool isFindingMatch;
    private bool isMatchmakerRunning;
    private bool cancelMatchmakingRequested;
    private bool connectedPreviously;
    private int currentConnectionAttempts;
    private string ticketId;

    private void Awake()
    {
        if (!IsSingletonInstance())
            return;

        Initialized = false;
        isFindingMatch = false;
        isMatchmakerRunning = false;
        cancelMatchmakingRequested = false;
        connectedPreviously = false;
        currentConnectionAttempts = 0;

        onMatchmakingCancelled += OnMatchmakingCancelled;
    }

#if BYPASS_UNITY_SERVICES
    private void Start()
    {
        Initialized = true;
        onInitialized?.Invoke();
    }
#else
    private async void Start()
    {
        try
        {
            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Initialized = true;
            onInitialized?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }
#endif

    public void CallWhenInitialized(System.Action callback)
    {
        if (Initialized)
            callback();
        else
            onInitialized += callback;
    }

#if BYPASS_UNITY_SERVICES
    public async void FindMatch()
    {
        if (isFindingMatch)
            return;

        Debug.Log("Started matchmaking process.");
        isFindingMatch = true;
        cancelMatchmakingRequested = false;

        ClientSceneManager.Instance.ReloadSceneOnWebReconnected = false;

        // No need to set connection data for connecting to localhost.
        // Still, requires the server to be already listening to connections.
        Debug.Log("Getting server password...");
        string password = await GetServerPassword();

    #if UNITY_EDITOR
        if (!Application.isPlaying)
            return;
    #endif

        if (password != null)
        {
            Debug.Log($"Password received. Password: {password}");
            string tmpUsername = UserData.SignedInUserData.username;
            string tmpPlayerName = UserData.SignedInUserData.playerName;
            ClientNetworkManager.Instance.PrepareConnectionPayload(tmpUsername, tmpPlayerName, password);
            NetworkManager.Singleton.OnClientDisconnectCallback += OnDisconnectedWhileMatchmaking;
            ClientNetworkManager.Instance.onClientStart += OnNetworkClientStart;
            currentConnectionAttempts = 0;
            StartCoroutine(AttemptConnectionCoroutine());
        }
        else
        {
            Debug.Log("Couldn't get server password.");
            Debug.Log("Matchmaking process was cancelled.");
            onMatchmakingCancelled?.Invoke();
        }
    }
#else
    public void FindMatch()
    {
        if (isFindingMatch)
            return;
        isFindingMatch = true;

        cancelMatchmakingRequested = false;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnDisconnectedWhileMatchmaking;
        NetworkManager.Singleton.OnClientConnectedCallback += OnGameServerConnectionSucceeded;
        ClientNetworkManager.Instance.onClientStart += OnNetworkClientStart;
        ClientSceneManager.Instance.ReloadSceneOnWebReconnected = false;
        TryFindingMatch();
    }
#endif

    public void CancelMatchmaking()
    {
        if (cancelMatchmakingRequested || !isFindingMatch)
            return;
        cancelMatchmakingRequested = true;
    }

    private async void TryFindingMatch()
    {
        while (!cancelMatchmakingRequested)
        {
            MatchmakingResultData matchData = await RunMatchmaker();
            if (matchData.status == MatchmakingResultStatus.SUCCESS)
            {
                Debug.Log("Matchmaking process succeeded. Connecting to server...");
                ClientNetworkManager.Instance.SetConnectionData(matchData.ipv4Address, (ushort)matchData.port);
                Debug.Log("Getting server password...");
                string password = await GetServerPassword();
                if (password != null)
                {
                    Debug.Log($"Password received. Password: {password}");
                    string tmpUsername = UserData.SignedInUserData.username;
                    string tmpPlayerName = UserData.SignedInUserData.playerName;
                    ClientNetworkManager.Instance.PrepareConnectionPayload(tmpUsername, tmpPlayerName, password);
                    currentConnectionAttempts = 0;
                    connectedPreviously = false;
                    StartCoroutine(AttemptConnectionCoroutine());
                    return;
                }
                else
                    Debug.Log("An error appeared while getting the password. Finding another match...");
            }
            else if (matchData.status != MatchmakingResultStatus.CANCEL)
            {
                Debug.Log($"Matchmaking process failed.");
                Debug.Log($" - Process Result Status: {matchData.status.ToString()}");
                Debug.Log($" - Message: {matchData.message}");
                Debug.Log($" - Retrying...");
            }

            if (!cancelMatchmakingRequested)
                await Task.Delay(findNewMatchDelay);
#if UNITY_EDITOR
            // This will prevent the loop from continuing execution after exiting editor's Play Mode without cancelling it before.
            // Maybe that happens because Unity doesn't stop the running Tasks when exiting Play Mode. So, when the Task ends,
            // the execution after 'await' is resumed.
            // Should also make this check after every 'await' of this method in the call stack.
            if (!Application.isPlaying)
                return;
#endif
        }
        
        Debug.Log("Matchmaking process was cancelled.");
        onMatchmakingCancelled?.Invoke();
    }

    private async Task<MatchmakingResultData> RunMatchmaker()
    {
        if (isMatchmakerRunning)
            return null;
        isMatchmakerRunning = true;

        try
        {
            // Options regarding player's game preferences like map or game mode. Options are used to match him with
            // others having similar preferences. This game doesn't have multiple choices so the object created is empty.
            var options = new CreateTicketOptions();
            // List of players which represent a ticket in the matchmaking queue. Useful when there is a group of friends
            // who want to play together as a team (it's not the case for this game). Player object contains a UID and 
            // custom data for things like skill level so it can be matched with players having similar skill level.
            var players = new List<Player> { new Player(AuthenticationService.Instance.PlayerId) };

            CreateTicketResponse ticket = await MatchmakerService.Instance.CreateTicketAsync(players, options);
            ticketId = ticket.Id;
            
            while (!cancelMatchmakingRequested)
            {
                await Task.Delay(pollingTicketResultDelay);
#if UNITY_EDITOR
            if (!Application.isPlaying)
                break;
#endif
                // Each request can return either null or a MultiplayAssignment object.
                // Polling until the object returned is not null and it's status is not InProgress.
                var assignmentResponse = await MatchmakerService.Instance.GetTicketAsync(ticketId);
                if (assignmentResponse.Type == typeof(MultiplayAssignment))
                {
                    MultiplayAssignment matchAssignment = (MultiplayAssignment)assignmentResponse.Value;
                    if (matchAssignment.Status == MultiplayAssignment.StatusOptions.InProgress)
                        continue;
                    if (matchAssignment.Status == MultiplayAssignment.StatusOptions.Failed)
                        return CreateResultData(MatchmakingResultStatus.FAIL, matchAssignment.Message);
                    if (matchAssignment.Status == MultiplayAssignment.StatusOptions.Timeout)
                        return CreateResultData(MatchmakingResultStatus.TIMEOUT, matchAssignment.Message);
                    if (matchAssignment.Port == null)
                    {
                        string failMessage = $"The connection port was not received.\n - Message: {matchAssignment.Message}";
                        return CreateResultData(MatchmakingResultStatus.FAIL, failMessage);
                    }
                    return CreateResultData(MatchmakingResultStatus.SUCCESS, matchAssignment.Message, matchAssignment);
                }
            }

            return CreateResultData(MatchmakingResultStatus.CANCEL, "The matchmacking process was cancelled.");
        }
        catch (Exception e)
        {
            return CreateResultData(MatchmakingResultStatus.FAIL, e.ToString());
        }
    }

    private MatchmakingResultData CreateResultData(MatchmakingResultStatus status, string message, MultiplayAssignment assignment = null)
    {
        isMatchmakerRunning = false;
        ticketId = null; 

        MatchmakingResultData resultData = new MatchmakingResultData();
        resultData.status = status;
        resultData.message = message;
        if (status == MatchmakingResultStatus.SUCCESS)
        {
            resultData.ipv4Address = assignment.Ip;
            resultData.port = (int)assignment.Port;
        }
        return resultData;
    }

    private IEnumerator AttemptConnectionCoroutine()
    {
        if (currentConnectionAttempts > 0)
            yield return new WaitForSeconds(connectionAttemptDelay);

        NetworkManager.Singleton.Shutdown();
        yield return new WaitWhile(() => NetworkManager.Singleton.ShutdownInProgress);
        ++currentConnectionAttempts;
        Debug.Log($"Server connection attempt #{currentConnectionAttempts}");
        ClientNetworkManager.Instance.StartClient();
    }

    private void OnDisconnectedWhileMatchmaking(ulong clientId)
    {
#if BYPASS_UNITY_SERVICES
        string disconnectReason = NetworkManager.Singleton.DisconnectReason;
        if (string.IsNullOrEmpty(disconnectReason))
            Debug.Log("Disconnected while matchmaking. No reason received.");
        else
            Debug.Log($"Disconnected while matchmaking. Reason: {disconnectReason}");
        Debug.Log("Matchmaking process was cancelled.");
        onMatchmakingCancelled?.Invoke();
#else
        bool attemptReconnection = false;

        string disconnectReason = NetworkManager.Singleton.DisconnectReason;
        if (disconnectReason != ServerNetworkManager.DisconnectReason.WAITING_FOR_PLAYERS_TIMEOUT.ToString()
            && disconnectReason != ServerNetworkManager.DisconnectReason.GAME_ENDED.ToString()
            && disconnectReason != ServerNetworkManager.DisconnectReason.SERVER_SHUTDOWN.ToString())
        {
            if (connectedPreviously)
            {
                // Here, the client was connected at least once to the found game server. So it tries to reconnect.
                if (currentConnectionAttempts == 0)
                {
                    Debug.Log("Lost connection to the found game server. Trying reconnection...");
                    ClientNetworkManager.Instance.ClientInMatchTesting.StartRequestLoop();
                    // This is set here to true for trying reconnection at least once.
                    ClientNetworkManager.Instance.ClientRegisteredInMatch = true;
                }
                if (ClientNetworkManager.Instance.ClientRegisteredInMatch)
                    attemptReconnection = true;
            }
            else
            {
                // Here, the client didn't succeeded yet in connecting for the first time to the found game.
                if (!cancelMatchmakingRequested && currentConnectionAttempts < maxConnectionAttempts)
                    attemptReconnection = true;
            }
        }

        if (attemptReconnection)
            StartCoroutine(AttemptConnectionCoroutine());
        else
        {
            ClientNetworkManager.Instance.ClientInMatchTesting.StopRequestLoop();
            Debug.Log("Couldn't connect to the found game server. Trying to find a new match...");
            TryFindingMatch();
        }
#endif
    }

    private void OnGameServerConnectionSucceeded(ulong clientId)
    {   
        if (connectedPreviously)
            ClientNetworkManager.Instance.ClientInMatchTesting.StopRequestLoop();
        connectedPreviously = true;
        currentConnectionAttempts = 0;
    }

    private void OnNetworkClientStart()
        // NetworkManager.SceneManager object exists once the client is started.
        => NetworkManager.Singleton.SceneManager.OnLoad += OnNetworkGameplaySceneLoad;

    private void OnNetworkGameplaySceneLoad(ulong clientId, string sceneName, LoadSceneMode loadSceneMode, AsyncOperation asyncOperation)
    {
        // This is called when the server sends signal to load the gameplay scene (game starts).
        // This is one of the two points where the matchmaking process ends.

        CleanUp();
        onMatchStarted?.Invoke();
    }

#if BYPASS_UNITY_SERVICES
    private void OnMatchmakingCancelled()
        // This is one of the two points where the matchmaking process ends.
        => CleanUp();
#else
    private async void OnMatchmakingCancelled()
    {
        // This is one of the two points where the matchmaking process ends.
        if (!string.IsNullOrEmpty(ticketId))
        {
            await MatchmakerService.Instance.DeleteTicketAsync(ticketId);
            ticketId = null;
        }
        CleanUp();
    }
#endif

    private async Task<string> GetServerPassword()
    {
#if USING_LOCAL_SERVERS
        const string requestURL = "http://localhost/get_server_password";
#else
        const string requestURL = "https:// - /get_server_password";
#endif
        var unityTransport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        string serverIp = unityTransport.ConnectionData.Address;
        ushort serverPort = unityTransport.ConnectionData.Port;

        WWWForm form = new WWWForm();
        form.AddField("SessionToken", UserData.SignedInUserData.loginSessionToken);
        form.AddField("Username", UserData.SignedInUserData.username);
        form.AddField("Ip", serverIp);
        form.AddField("Port", serverPort);

        int attemptsMade = 0;
        while (!cancelMatchmakingRequested && attemptsMade < maxPasswordGetAttempts)
        {
            ++attemptsMade;

            UnityWebRequest req = UnityWebRequest.Post(requestURL, form);
            req.disposeUploadHandlerOnDispose = true;
            req.disposeDownloadHandlerOnDispose = true;
            AsyncOperation asyncOp = req.SendWebRequest();
            while (!asyncOp.isDone)
                await Task.Delay(100);

            if (req.result != UnityWebRequest.Result.Success)
                Debug.Log($"ClientGetServerPassword (Attempt #{attemptsMade}): Web request failed! " + req.error);
            else
            {
                string[] reqResponse = req.downloadHandler.text.Split('\t');
                if (reqResponse[0] == "1" && reqResponse.Length > 1)
                {
                    req.Dispose();
                    return reqResponse[1];
                }
                else
                {
                    string error = (reqResponse.Length > 1 ? reqResponse[1] : reqResponse[0]);
                    Debug.Log($"ClientGetServerPassword error (Attempt #{attemptsMade}): {error}");
                }
            }
            
            req.Dispose();
            await Task.Delay(passwordGetAttemptDelay);
#if UNITY_EDITOR
            if (!Application.isPlaying)
                break;
#endif
        }
        return null;
    }

    private void CleanUp()
    {
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnDisconnectedWhileMatchmaking;
        NetworkManager.Singleton.OnClientConnectedCallback -= OnGameServerConnectionSucceeded;
        ClientNetworkManager.Instance.onClientStart -= OnNetworkClientStart;
        ClientSceneManager.Instance.ReloadSceneOnWebReconnected = true;
        if (NetworkManager.Singleton.SceneManager != null)
            NetworkManager.Singleton.SceneManager.OnLoad -= OnNetworkGameplaySceneLoad;

        isFindingMatch = false;
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
