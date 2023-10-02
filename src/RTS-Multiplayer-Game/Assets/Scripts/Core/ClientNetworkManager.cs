using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using TMPro;

public class ClientNetworkManager : MonoBehaviour
{
    public static ClientNetworkManager Instance { get; private set; }
    public WebRequestLoop ClientInMatchTesting { get; private set; }
    public WebRequestLoop WebServerConnectionTesting { get; private set; }
    public bool IsConnectedToWebServer { get; private set; }
    public bool ClientRegisteredInMatch { get; set; }

    public event System.Action OnWebServerConnectionChange;
    public event System.Action onClientStart;
    public event System.Action onGameDisconnectionAccepted;

    private const int webServerTestingDelay1 = 15000;  // in milliseconds
    private const int webServerTestingDelay2 = 1000;  // in milliseconds
    private const int clientInMatchTestingDelay = 10000;  // in milliseconds
    private const int initialReconnectionAttemptDelay = 1;  // in seconds
    private const int maxReconnectionAttemptDelay = 10;  // in seconds

    private int currentReconnectionAttemptDelay;  // in seconds
    private int currentReconnectionAttempts;
    private int webServerTestingFails;
    private NetworkManager networkManager;

    private void Awake()
    {
        if (!IsSingletonInstance())
            return;
        
        ClientInMatchTesting = new WebRequestLoop(CheckIfStillInMatch, clientInMatchTestingDelay);
        WebServerConnectionTesting = new WebRequestLoop(CheckWebServerConnection, webServerTestingDelay1);
        IsConnectedToWebServer = false;
        ClientRegisteredInMatch = false;

        currentReconnectionAttemptDelay = initialReconnectionAttemptDelay;
        currentReconnectionAttempts = 0;
        webServerTestingFails = 0;

        CoreManager.Instance.onCoreInitialized += OnAppCoreInitialized;
    }

    private void Start()
    {
        networkManager = NetworkManager.Singleton;
        // Init the connection payload container with empty payload.
        PrepareConnectionPayload("", "", "");   
        
        CoreManager.Instance.SignalComponentInitialized();
    }

    private void OnAppCoreInitialized()
    {
        // Web server connection testing starts once the core is initialized.
        WebServerConnectionTesting.StartRequestLoop();
    }

    public void StartClient()
    {
        bool started = networkManager.StartClient();
        if (started)
            onClientStart?.Invoke();
        else
            Debug.Log("Client start failed!");
    }

    public void StopClient()
    {
        networkManager.Shutdown();
    }

    public void SetConnectionData(string ipv4Address, ushort port)
    {
        var unityTransport = networkManager.GetComponent<UnityTransport>();
        unityTransport.SetConnectionData(ipv4Address, port);
    }

    public void EnableAutomaticReconnection()
    {
        currentReconnectionAttemptDelay = initialReconnectionAttemptDelay;
        currentReconnectionAttempts = 0;
        networkManager.OnClientConnectedCallback += OnClientReconnected;
        networkManager.OnClientDisconnectCallback += OnClientDisconnected;
    }

    public void DisableAutomaticReconnection()
    {
        networkManager.OnClientConnectedCallback -= OnClientReconnected;
        networkManager.OnClientDisconnectCallback -= OnClientDisconnected;
    }

    private void OnClientReconnected(ulong clientId)
    {
        Debug.Log("Reconnection succeeded.");
        ClientInMatchTesting.StopRequestLoop();
        currentReconnectionAttemptDelay = 1;
        currentReconnectionAttempts = 0;
        CoreUi.Instance.MessageScreen.SetActive(false);
    }

    private void OnClientDisconnected(ulong clientId)
    {
        string disconnectReason = NetworkManager.Singleton.DisconnectReason;
        print($"OnClientDisconnected: DisconnectReason: {disconnectReason}");
        if (disconnectReason != ServerNetworkManager.DisconnectReason.WAITING_FOR_PLAYERS_TIMEOUT.ToString()
            && disconnectReason != ServerNetworkManager.DisconnectReason.GAME_ENDED.ToString()
            && disconnectReason != ServerNetworkManager.DisconnectReason.SERVER_SHUTDOWN.ToString())
        {
            if (currentReconnectionAttempts == 0)
            {
                Debug.Log("Lost connection to the game server.");
                CoreUi.Instance.MessageScreen.transform.Find("Text").GetComponent<TMP_Text>().text = "Connection lost.\nTrying reconnection...";
                CoreUi.Instance.MessageScreen.SetActive(true);
                ClientRegisteredInMatch = true;
                ClientInMatchTesting.StartRequestLoop();
            }
            if (ClientRegisteredInMatch)
            {
                print($"OnClientDisconnected: ClientRegisteredInMatch: {ClientRegisteredInMatch}");
                StartCoroutine(AttemptReconnection());
                return;
            }
        }
         
        // At this point, disconnection is accepted.

        Debug.Log("Disconnection from game server accepted.");
        DisableAutomaticReconnection();
        ClientInMatchTesting.StopRequestLoop();
        WebServerConnectionTesting.StartRequestLoop();
        networkManager.Shutdown();
        CoreUi.Instance.MessageScreen.SetActive(false);
        onGameDisconnectionAccepted?.Invoke();
    }

    private IEnumerator AttemptReconnection()
    {
        // Every 3 attempts, the delay between attempts is doubled until it reaches the max limit.
        // The first attempt is done without an additional delay, waiting just to restart the Netcode library.
        if (currentReconnectionAttempts > 0)
        {
            if (currentReconnectionAttempts % 3 == 0)
                currentReconnectionAttemptDelay = Mathf.Min(2 * currentReconnectionAttemptDelay, maxReconnectionAttemptDelay);
            yield return new WaitForSeconds(currentReconnectionAttemptDelay);
        }

        networkManager.Shutdown();
        yield return new WaitWhile(() => networkManager.ShutdownInProgress);
        ++currentReconnectionAttempts;
        Debug.Log($"Trying reconnection (attempt #{currentReconnectionAttempts})...");
        ClientNetworkManager.Instance.StartClient();
    }

    private async Task CheckIfStillInMatch()
    {
        print("CheckIfStillInMatch: Starting point.");

#if USING_LOCAL_SERVERS
        const string requestURL = "http://localhost/check_if_user_in_match";
#else
        const string requestURL = "https:// - /check_if_user_in_match";
#endif
        WWWForm form = new WWWForm();
        form.AddField("Username", UserData.SignedInUserData.username);
        form.AddField("ReturnServer", "No");
        form.AddField("SessionToken", UserData.SignedInUserData.loginSessionToken);

        UnityWebRequest req = UnityWebRequest.Post(requestURL, form);
        req.disposeUploadHandlerOnDispose = true;
        req.disposeDownloadHandlerOnDispose = true;
        AsyncOperation asyncOp = req.SendWebRequest();
        while (!asyncOp.isDone)
            await Task.Delay(100);

        if (req.result != UnityWebRequest.Result.Success)
            Debug.Log("ChechIfStillInMatch: Server request failed! " + req.error);
        else
        {
            print($"CheckIfStillInMatch: Web response: {req.downloadHandler.text}");
            string[] reqResponse = req.downloadHandler.text.Split('\t');
            if (reqResponse[0] == "1")
                ClientRegisteredInMatch = true;
            else if (reqResponse[0] == "2")
                ClientRegisteredInMatch = false;
            else
            {
                string error = (reqResponse.Length > 1 ? reqResponse[1] : reqResponse[0]);
                Debug.Log($"CheckIfStillInMatch error: {error}");
            }
        }

        req.Dispose();
    }

    private async Task CheckWebServerConnection()
    {
        System.Random rnd = new System.Random();
        const int tokenBufferSize = 8;
        byte[] tokenBuffer = new byte[tokenBufferSize];
        rnd.NextBytes(tokenBuffer);
        // This can be at most 3 times the size of the tokenBuffer because each "escaped" ascii char is converted
        // into 3 ascii chars ('%' + hex value of the ascii char). 
        string generatedToken = UnityWebRequest.EscapeURL(Encoding.ASCII.GetString(tokenBuffer, 0, tokenBufferSize));

#if USING_LOCAL_SERVERS
        string requestURL = $"http://localhost/server_connection_testing?token={generatedToken}";
#else
        string requestURL = $"https:// - /server_connection_testing?token={generatedToken}";
#endif
        UnityWebRequest req = UnityWebRequest.Get(requestURL);
        req.disposeUploadHandlerOnDispose = true;
        req.disposeDownloadHandlerOnDispose = true;
        AsyncOperation asyncOp = req.SendWebRequest();
        while (!asyncOp.isDone)
            await Task.Delay(100);

        string receivedToken = "";
        if (req.result == UnityWebRequest.Result.Success)
            receivedToken = UnityWebRequest.EscapeURL(Encoding.ASCII.GetString(req.downloadHandler.data));
        if (req.result != UnityWebRequest.Result.Success || receivedToken != generatedToken)
        {
            string failReason = (req.result != UnityWebRequest.Result.Success ? $"Web request failed. {req.error}" : "Different token received");
            Debug.Log($"Web server connection test failed! {failReason}.");
            ++webServerTestingFails;
            if (webServerTestingFails == 1)
                WebServerConnectionTesting.SetNewReqDelay(webServerTestingDelay2);
            else if (webServerTestingFails == 3)
            {
                // Consider disconnection only if multiple consecutive checks failed. 
                // This is to avoid some unexpected fails when the server is actually up (like some req. timeout responses). 
                IsConnectedToWebServer = false;
                OnWebServerConnectionChange?.Invoke();
            }
        }
        else
        {
            webServerTestingFails = 0;
            if (!IsConnectedToWebServer)
            {
                Debug.Log("Reconnected to web server.");
                IsConnectedToWebServer = true;
                OnWebServerConnectionChange?.Invoke();
                WebServerConnectionTesting.SetNewReqDelay(webServerTestingDelay1);
            }
        }
        
        req.Dispose();
    }

    public void PrepareConnectionPayload(string username, string playerName, string serverPassword)
    {
        // With the given data, prepares a ClientConnectionPayload object which will be sent over the network.
        var tmp = new ClientConnectionPayload();
        tmp.username = username;
        tmp.playerName = playerName;
        tmp.serverPassword = serverPassword;
        networkManager.NetworkConfig.ConnectionData = tmp.ToBytesArray();
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
