using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using TMPro;

public class SignInManager : MonoBehaviour
{
    public class ServerConnectionData
    {
        public string ip;
        public ushort port;
        public string password;
    }
    
    public static SignInManager Instance { get; private set; }
    public Dictionary<uint, CombatUnitBasicData> gameUnits;  // units IDs (key) and units data (value)
    
    private const int connectionAttemptDelay = 1;  // in seconds

    [SerializeField]
    private GameObject mainUi;
    [SerializeField]
    private TMP_InputField usernameInput;
    [SerializeField]
    private TMP_InputField passwordInput;
    [SerializeField]
    private GameObject messageText;
    [SerializeField]
    private Button submitButton;
    [SerializeField]
    private List<CombatUnitBasicData> gameUnitsList;
    private int currentConnectionAttempts;

    private void Awake()
    {
        if (!IsSingletonInstance())
            return;

        submitButton.onClick.AddListener(OnSubmitButtonPress);
        submitButton.onClick.AddListener(() => CoreUi.Instance.PlaySoundEffect(CoreUi.SoundEffect.BUTTON_CLICK));
        currentConnectionAttempts = 0;

        gameUnits = new Dictionary<uint, CombatUnitBasicData>();
        foreach (var unit in gameUnitsList)
            gameUnits.Add(unit.unitId, unit);
    }

    private async void OnSubmitButtonPress()
    {
        submitButton.interactable = false;
        TMP_Text messageTextObj = messageText.GetComponentInChildren<TMP_Text>();
        messageTextObj.text = "";
        messageText.SetActive(false);
        
        if (IsInputValid())
        {
            UserData userData = await CheckIfUserExists();
            if (userData != null)
            {
                SignInUser(userData);
                return;
            }
        }
        
        if (messageTextObj.text == "")
            messageTextObj.text = "Invalid username or password";
        messageText.SetActive(true);
        submitButton.interactable = true;
    }

    private async Task<UserData> CheckIfUserExists()
    {
#if USING_LOCAL_SERVERS
        const string requestURL = "http://localhost/sign_in_user";
#else
        const string requestURL = "https:// - /sign_in_user";
#endif
        WWWForm form = new WWWForm();
        form.AddField("Username", usernameInput.text);
        form.AddField("Password", passwordInput.text);

        UnityWebRequest req = UnityWebRequest.Post(requestURL, form);
        req.disposeUploadHandlerOnDispose = true;
        req.disposeDownloadHandlerOnDispose = true;
        AsyncOperation asyncOp = req.SendWebRequest();
        while (!asyncOp.isDone)
            await Task.Delay(100);

        UserData data = null;
        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.Log("SignIn: CheckIfUserExists: Web request failed! " + req.error);
            messageText.GetComponentInChildren<TMP_Text>().text = "Oops, something went wrong. Try again later.";
        }
        else
        {
            string[] reqResponse = req.downloadHandler.text.Split('\t');
            if (reqResponse[0] == "2")
                messageText.GetComponentInChildren<TMP_Text>().text = "Invalid username or password";
            else if (reqResponse[0] == "1")
            {
                if (reqResponse.Length < 8)
                    messageText.GetComponentInChildren<TMP_Text>().text = "Oops, something went wrong. Try again later.";
                else
                {
                    string generatedSessionToken = reqResponse[1];
                    PlayerPrefs.SetString("UserSessionToken", generatedSessionToken);

                    data = new UserData();
                    data.username = usernameInput.text;
                    data.playerName = reqResponse[2];
                    Int32.TryParse(reqResponse[3], out data.gold);
                    Int32.TryParse(reqResponse[4], out data.trophies);
                    string[] ownedUnitsIds = reqResponse[5].Trim().Split('&');
                    string[] ownedUnitsLevels = reqResponse[6].Trim().Split('&');
                    string[] deckUnitsIds = reqResponse[7].Trim().Split('&');
                    FillWithOwnedUnits(ref data, ownedUnitsIds, ownedUnitsLevels, deckUnitsIds);
                }
            }
            else
            {
                string error = (reqResponse[0] == "0" && reqResponse.Length > 1 ? reqResponse[1] : reqResponse[0]);
                Debug.Log($"SignIn: CheckIfUserExists error: {error}");
                messageText.GetComponentInChildren<TMP_Text>().text = "Oops, something went wrong. Try again later.";
            }
        }

        req.Dispose();
        return data;
    }

    public void FillWithOwnedUnits(ref UserData data, string[] ownedUnitsIds, string[] ownedUnitsLevels, string[] deckUnitsIds)
    {
        var deckUnitsOrderDict = new Dictionary<uint, int>();  // units ids (key) and deck positions (value)
        for (int i = deckUnitsIds.Length - 1; i >= 0; --i)
        {
            uint unitId;
            UInt32.TryParse(deckUnitsIds[i], out unitId);
            deckUnitsOrderDict.Add(unitId, i + 1);
        }

        data.ownedUnitsData = new Dictionary<uint, OwnedCombatUnitData>();
        data.deckUnitsData = new Dictionary<uint, uint>();
        int deckUnitsToFind = 8;
        for (int i = ownedUnitsIds.Length - 1; i >= 0; --i)
        {
            uint unitId, unitLevel;
            UInt32.TryParse(ownedUnitsIds[i], out unitId);
            UInt32.TryParse(ownedUnitsLevels[i], out unitLevel);
            CombatUnitBasicData unitData;
            if (gameUnits.TryGetValue(unitId, out unitData))
            {
                var ownedUnitData = new OwnedCombatUnitData(unitData, unitLevel);
                data.ownedUnitsData.Add(unitId, ownedUnitData);
                int deckPosition;
                if (deckUnitsOrderDict.TryGetValue(unitId, out deckPosition))
                {
                    data.deckUnitsData.Add(unitId, (uint)deckPosition);
                    --deckUnitsToFind;
                }
            }
        }

        if (deckUnitsToFind != 0)
        {
            messageText.GetComponentInChildren<TMP_Text>().text = "Oops, something went wrong. Try again later.";
            data = null;
        }
    }

    private bool IsInputValid()
    {
        Regex alphanumericRegex = new Regex("^[a-zA-Z0-9]*$");
        if (!alphanumericRegex.IsMatch(usernameInput.text)
            || usernameInput.text.Length > 25
            || usernameInput.text.Length < 3
            || Encoding.UTF8.GetByteCount(passwordInput.text) != passwordInput.text.Length  // password must contain only one byte chars
            || passwordInput.text.Length < 10
            || passwordInput.text.Length > 40)
            return false;
        return true;
    }

    public async void SignInUser(UserData userData)
    {
        CoreUi.Instance.LoadingScreen.SetActive(true);

        userData.loginSessionToken = PlayerPrefs.GetString("UserSessionToken");
        UserData.SignedInUserData = userData;

        ServerConnectionData matchServer = await CheckIfUserInMatch();
        if (matchServer != null)
        {
            ClientNetworkManager.Instance.SetConnectionData(matchServer.ip, matchServer.port);
            ClientNetworkManager.Instance.PrepareConnectionPayload(userData.username, userData.playerName, matchServer.password);
            NetworkManager.Singleton.OnClientDisconnectCallback += OnConnectionAttemptFail;
            NetworkManager.Singleton.OnClientConnectedCallback += OnGameServerConnectionSucceeded;
            ClientNetworkManager.Instance.ClientInMatchTesting.StartRequestLoop();
            StartCoroutine(AttemptConnectionCoroutine());
        }
        else
            ClientSceneManager.Instance.ChangeSceneLocallyAsync("MainMenu", false);
    }

    private async Task<ServerConnectionData> CheckIfUserInMatch()
    {
#if USING_LOCAL_SERVERS
        const string requestURL = "http://localhost/check_if_user_in_match";
#else
        const string requestURL = "https:// - /check_if_user_in_match";
#endif
        WWWForm form = new WWWForm();
        form.AddField("Username", UserData.SignedInUserData.username);
        form.AddField("ReturnServer", "Yes");
        form.AddField("SessionToken", UserData.SignedInUserData.loginSessionToken);

        UnityWebRequest req = UnityWebRequest.Post(requestURL, form);
        req.disposeUploadHandlerOnDispose = true;
        req.disposeDownloadHandlerOnDispose = true;
        AsyncOperation asyncOp = req.SendWebRequest();
        while (!asyncOp.isDone)
            await Task.Delay(100);

        ServerConnectionData data = null;
        if (req.result != UnityWebRequest.Result.Success)
            Debug.Log("OnStartupCheckIfUserIsInMatch: Web request failed! " + req.error);
        else
        {
            string[] reqResponse = req.downloadHandler.text.Split('\t');
            if (reqResponse[0] == "1" && reqResponse.Length == 4)
            {
                ushort svPort;
                if (ushort.TryParse(reqResponse[2], out svPort))
                {
                    data = new ServerConnectionData();
                    data.ip = reqResponse[1];
                    data.port = svPort;
                    data.password = reqResponse[3];
                }
            }
        }

        req.Dispose();
        return data;
    }

    private IEnumerator AttemptConnectionCoroutine()
    {
        if (currentConnectionAttempts > 0)
            yield return new WaitForSeconds(connectionAttemptDelay);

        NetworkManager.Singleton.Shutdown();
        yield return new WaitWhile(() => NetworkManager.Singleton.ShutdownInProgress);
        Debug.Log($"OnLogin: Server connection attempt #{currentConnectionAttempts}");
        ++currentConnectionAttempts;
        ClientNetworkManager.Instance.StartClient();
    }

    private void OnConnectionAttemptFail(ulong clientId)
    {
        string disconnectReason = NetworkManager.Singleton.DisconnectReason;
        if (ClientNetworkManager.Instance.ClientRegisteredInMatch)
        {
            if (disconnectReason != ServerNetworkManager.DisconnectReason.WAITING_FOR_PLAYERS_TIMEOUT.ToString()
                && disconnectReason != ServerNetworkManager.DisconnectReason.GAME_ENDED.ToString()
                && disconnectReason != ServerNetworkManager.DisconnectReason.SERVER_SHUTDOWN.ToString())   
            {
                StartCoroutine(AttemptConnectionCoroutine());
                return;
            }
        }

        Debug.Log("OnSignIn: Connection attempts stopped. Entering Main Menu...");
        ClientSceneManager.Instance.ChangeSceneLocallyAsync("MainMenu", false);
        OnConnectionAttemptsDone();
    }

    private void OnGameServerConnectionSucceeded(ulong clientId)
    {
        ClientNetworkManager.Instance.WebServerConnectionTesting.StopRequestLoop();
        ClientNetworkManager.Instance.EnableAutomaticReconnection();
        SceneManager.UnloadSceneAsync("Login");
        OnConnectionAttemptsDone();
    }

    private void OnConnectionAttemptsDone()
    {
        NetworkManager.Singleton.OnClientConnectedCallback -= OnGameServerConnectionSucceeded;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnConnectionAttemptFail;
        ClientNetworkManager.Instance.ClientInMatchTesting.StopRequestLoop();
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
