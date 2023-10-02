using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Multiplay;

public class ServerMultiplayManager : MonoBehaviour
{
    public static ServerMultiplayManager Instance { get; private set; }

    public long ServerId { get; private set; }
    public string ServerIp { get; private set; }
    public ushort ServerPort { get; private set; }
    public string ServerPassword { get; private set; }

    // Multiplay service specific type. Provides up-to-date server info to the Game Server Hosting (Multiplay) service.
    private IServerQueryHandler serverQueryHandler;
    private bool isServerAllocated;
    private bool allocProcedureTriggered;

    private void Awake()
    {
        if (!IsSingletonInstance())
            return;

        ServerId = 0;
        ServerIp = null;
        ServerPort = 0;
        ServerPassword = null;

        isServerAllocated = false;
        allocProcedureTriggered = false;
#if !BYPASS_UNITY_SERVICES
        // "-serverip $$ip$$" should be present as command line arguments. 
        string[] args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; ++i)
            if (args[i] == "-serverip")
            {
                ServerIp = args[i + 1];
                break;
            }
#endif
    }

#if BYPASS_UNITY_SERVICES
    private async void Start()
    {
        ServerStateMachine.Instance.AddOnEnterCallback(ServerStateMachine.State.WAITING_PLAYERS_TO_CONNECT, OnExitWaitingPlayersState);
        ServerStateMachine.Instance.AddOnEnterCallback(ServerStateMachine.State.SHUTTING_DOWN, OnEnterShuttingDownState);
        var connectionData = GameObject.FindObjectOfType<UnityTransport>().ConnectionData;
        ServerIp = connectionData.Address;
        ServerPort = connectionData.Port;

        isServerAllocated = await MarkServerAsAllocated();
        if (!isServerAllocated)
            Debug.LogError("Server start: MarkServerAsAllocated failed.");
        else
        {
            // Quit cleanup
            AsyncCleanupManager.Instance.AddAsyncCleanup("UnmarkServerAsAllocated", UnmarkServerAsAllocated);

            // Start the server
            var svNetManager = ServerNetworkManager.Instance;
            svNetManager.StartTheServer();
            svNetManager.CanAcceptConnections = true;
            ServerStateMachine.Instance.SetCurrentState(ServerStateMachine.State.WAITING_PLAYERS_TO_CONNECT);

            CoreManager.Instance.SignalComponentInitialized();
        }
    }
#else
    private async void Start()
    {
        ServerStateMachine.Instance.AddOnExitCallback(ServerStateMachine.State.WAITING_PLAYERS_TO_CONNECT, OnExitWaitingPlayersState);
        ServerStateMachine.Instance.AddOnEnterCallback(ServerStateMachine.State.SHUTTING_DOWN, OnEnterShuttingDownState);
        Task sqhTask = InitServerQueryHandler();
        Task mseTask = InitMultiplayServiceEvents();
        await Task.WhenAll(new List<Task> {sqhTask, mseTask});

        CoreManager.Instance.SignalComponentInitialized();

        var svConfig = MultiplayService.Instance.ServerConfig;
        if (!string.IsNullOrEmpty(svConfig.AllocationId) && !allocProcedureTriggered)
        {
            // The allocation event was triggered before subscribing to it, so the subscribed procedure is called manually.
            Debug.Log("OnMultiplayAllocateEvent: The event was already triggered. Manually calling the procedure...");
            OnMultiplayAllocateEvent(new MultiplayAllocation("", svConfig.ServerId, svConfig.AllocationId));
        }
    }
#endif

#if !BYPASS_UNITY_SERVICES
    private void Update()
        => serverQueryHandler.UpdateServerCheck();
#endif

    private async Task InitServerQueryHandler()
    {
        const ushort maxPlayers = 2;
        const string serverName = "RTSMultiplayerGameServer";
        const string gameType = "Normal";
        const string buildVersion = "1.0";
        const string gameMap = "MainGameMap";
        try
        {
            serverQueryHandler = await MultiplayService.Instance.StartServerQueryHandlerAsync(maxPlayers, serverName, gameType, buildVersion, gameMap);
        }
        catch (Exception e)
        {
            Debug.LogError($"StartServerQueryHandlerAsync exception: {e.Message}");
            return;
        }
        serverQueryHandler.CurrentPlayers = 0;
        ServerNetworkManager.Instance.onPlayersCountChange += UpdateSqpPlayersCount;
    }

    private async Task InitMultiplayServiceEvents()
    {
        var multiplayEventCallbacks = new MultiplayEventCallbacks();
		multiplayEventCallbacks.Allocate += OnMultiplayAllocateEvent;
		multiplayEventCallbacks.Error += OnMultiplayErrorEvent;
		await MultiplayService.Instance.SubscribeToServerEventsAsync(multiplayEventCallbacks);
    } 

    private async void OnMultiplayAllocateEvent(MultiplayAllocation allocation)
    {
        if (allocProcedureTriggered)
            return;
        allocProcedureTriggered = true;

        if (allocation == null)
        {
            Debug.LogError("OnMultiplayAllocationEvent: Allocation argument was null.");
            ServerStateMachine.Instance.SetCurrentState(ServerStateMachine.State.SHUTTING_DOWN);
        }
        else
        {
            // Mark the server (in database) as allocated.
            ServerId = allocation.ServerId;
            ServerPort =  MultiplayService.Instance.ServerConfig.Port;
            isServerAllocated = await MarkServerAsAllocated();
            if (!isServerAllocated)
            {
                Debug.LogError("OnMultiplayAllocationEvent: MarkServerAsAllocated failed.");
                ServerStateMachine.Instance.SetCurrentState(ServerStateMachine.State.SHUTTING_DOWN);
            }
            else
            {
                AsyncCleanupManager.Instance.AddAsyncCleanup("UnmarkServerAsAllocated", UnmarkServerAsAllocated);

                // Start the server.
                var svNetManager = ServerNetworkManager.Instance;
                svNetManager.SetServerConnectionData("0.0.0.0", ServerPort);
                svNetManager.StartTheServer();
                svNetManager.CanAcceptConnections = true;
                ServerStateMachine.Instance.SetCurrentState(ServerStateMachine.State.WAITING_PLAYERS_TO_CONNECT);
                await MultiplayService.Instance.ReadyServerForPlayersAsync();
                svNetManager.StartWaitingPlayersCountdown();
            }
        }
    }

    private async Task<bool> MarkServerAsAllocated()
    {
#if USING_LOCAL_SERVERS
        const string requestURL = "http://localhost/mark_server_as_allocated";
#else
        const string requestURL = "https:// - /mark_server_as_allocated";
#endif
        WWWForm form = new WWWForm();
        form.AddField("Id", ServerId.ToString());
        form.AddField("Ip", ServerIp);
        form.AddField("Port", ServerPort);

        UnityWebRequest req = UnityWebRequest.Post(requestURL, form);
        req.disposeUploadHandlerOnDispose = true;
        req.disposeDownloadHandlerOnDispose = true;
        AsyncOperation asyncOp = req.SendWebRequest();
        while (!asyncOp.isDone)
            await Task.Delay(100);

        bool isAllocated = false;
        if (req.result != UnityWebRequest.Result.Success)
            Debug.Log("MarkServerAsAllocated: Web request failed! " + req.error);
        else
        {
            string[] reqResponse = req.downloadHandler.text.Split('\t');
            if (reqResponse[0] == "1" && reqResponse.Length == 2)
            {
                ServerPassword = reqResponse[1];
                isAllocated = true;
            }
            else
            {
                string error = (reqResponse.Length > 1 ? reqResponse[1] : reqResponse[0]);
                Debug.Log($"MarkServerAsAllocated error: {error}");
            }
        }

        req.Dispose();
        return isAllocated;
    }

    private async Task UnmarkServerAsAllocated()
    {
#if USING_LOCAL_SERVERS
        const string requestURL = "http://localhost/unmark_server_as_allocated";
#else
        const string requestURL = "https:// - /unmark_server_as_allocated";
#endif
        WWWForm form = new WWWForm();
        form.AddField("Id", ServerId.ToString());

        UnityWebRequest req = UnityWebRequest.Post(requestURL, form);
        req.disposeUploadHandlerOnDispose = true;
        req.disposeDownloadHandlerOnDispose = true;
        AsyncOperation asyncOp = req.SendWebRequest();
        while (!asyncOp.isDone)
            await Task.Delay(100);

        if (req.result != UnityWebRequest.Result.Success)
            Debug.Log("UnmarkServerAsAllocated: Web request failed! " + req.error);
        else
        {
            string[] reqResponse = req.downloadHandler.text.Split('\t');
            if (reqResponse[0] != "1")
            {
                string error = (reqResponse.Length > 1 ? reqResponse[1] : reqResponse[0]);
                Debug.Log($"UnmarkServerAsAllocated error: {error}");
            }
        }

        req.Dispose();
    }

    private void OnExitWaitingPlayersState()
        => ServerNetworkManager.Instance.StopWaitingPlayersCountdown();

    private void OnMultiplayErrorEvent(MultiplayError error)
    {
        Debug.Log("Multiplay service error event received!");
        Debug.Log($"Error Reason: {error.Reason}");
        Debug.Log($"Error Details: {error.Detail}");
    }

    public void UpdateSqpPlayersCount(ushort count)
        => serverQueryHandler.CurrentPlayers = count;

    private void OnEnterShuttingDownState()
    {
        Debug.Log("Shutting down the server...");
        ShutDownTheServer();
    }

#if BYPASS_UNITY_SERVICES
    private void ShutDownTheServer()
    {
        ServerNetworkManager.Instance.CanAcceptConnections = false;
        ServerNetworkManager.Instance.OnServerShutdown();
        Application.Quit();  // This call will make the AsyncCleanupManager execute it's subscribed functions.
    }
#else
    private async void ShutDownTheServer()
    {
        ServerNetworkManager.Instance.CanAcceptConnections = false;
        await MultiplayService.Instance.UnreadyServerAsync();
        ServerNetworkManager.Instance.OnServerShutdown();
        
        // Since it is a clean quit with the exit code 0, the Multiplay service will automatically issue a server deallocation request.
        // This call will make the AsyncCleanupManager execute it's subscribed functions.
        Application.Quit();
    }
#endif

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
