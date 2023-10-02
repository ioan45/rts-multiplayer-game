using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Unity.Netcode;

public class GameOverManager : NetworkBehaviour
{
    public static GameOverManager Instance { get; private set; }

    [SerializeField]
    private GameOverUiManager gameOverUiManager;
    [SerializeField]
    private GameObject mainUi;
    [SerializeField]
    private GameObject menuUi;
    [SerializeField]
    private GameObject inWorldUi;
    [SerializeField]
    private List<CombatUnitBasicData> unitsList;
    private uint autoQuittingDelay;  // in seconds
    private IEnumerator runningCoroutine;
    private System.Random rng;

    private void Awake()
    {
        if (!IsSingletonInstance())
            return;

        runningCoroutine = null;
        autoQuittingDelay = 300;
        gameOverUiManager.ShowGameOverUi(false);

        if (NetworkManager.Singleton.IsClient)
        {
            gameOverUiManager.onQuitButtonPress += QuitMatch;
        }
        else
        {
            rng = new System.Random();
            ServerStateMachine.Instance.AddOnEnterCallback(ServerStateMachine.State.GAME_OVER, OnEnterGameOverState);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void OnPlayerSurrenderServerRpc(ServerRpcParams args)
    {
        if (ServerStateMachine.Instance.GetCurrentState() == ServerStateMachine.State.IN_GAME)
        {
            uint winnerPlayerNumber = (args.Receive.SenderClientId == PlayersManager.Instance.Player1Data.clientData.networkId ? 2U : 1U);
            GameplayManager.Instance.WinnerPlayerNumber = winnerPlayerNumber;
            ServerStateMachine.Instance.SetCurrentState(ServerStateMachine.State.GAME_OVER);
        }
    }

    public void OnEnterGameOverState()
    {
        // Executed by server.

        // Prepare winner.
        int winnerTrophiesGained = 30;
        int winnerGoldGained = 250 + rng.Next(30, 101);
        var unitGained = unitsList[rng.Next(0, unitsList.Count)];
        var winnerData = GameplayManager.Instance.WinnerPlayerNumber == 1 ? PlayersManager.Instance.Player1Data : PlayersManager.Instance.Player2Data;
        ClientRpcParams winnerRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[]{ winnerData.clientData.networkId }
            }
        };
        if (winnerData.clientData.isConnected)
            SetGameOverOnClientRpc(true, winnerTrophiesGained, winnerGoldGained, unitGained.unitId, winnerRpcParams);

        // Prepare loser.
        int loserTrophiesGained = -30;
        int loserGoldGained = 150 + rng.Next(30, 101);
        var loserData = GameplayManager.Instance.WinnerPlayerNumber == 1 ? PlayersManager.Instance.Player2Data : PlayersManager.Instance.Player1Data;
        ClientRpcParams loserRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[]{ loserData.clientData.networkId }
            }
        };
        if (loserData.clientData.isConnected)
            SetGameOverOnClientRpc(false, loserTrophiesGained, loserGoldGained, 0, loserRpcParams);

        // Update the data for both players in the database.
        PostMatchResultForUser(winnerData.clientData.username, winnerTrophiesGained, winnerGoldGained, unitGained.unitId);
        PostMatchResultForUser(loserData.clientData.username, loserTrophiesGained, loserGoldGained, 0);

        // Shutdown the server when both players are disconnected.
        if (ServerNetworkManager.Instance.PlayersCount == 0)
            OnBothPlayersDisconnected(0);
        else
            ServerNetworkManager.Instance.onPlayersCountChange += OnBothPlayersDisconnected;
    }

    [ClientRpc]
    private void SetGameOverOnClientRpc(bool winner, int trophiesGained, int goldGained, uint unitGainedId, ClientRpcParams clientRpcParams)
    {
        // Executed by client.
        
        string unitGainedName = null;
        if (winner && !UserData.SignedInUserData.ownedUnitsData.ContainsKey(unitGainedId))
            foreach (var unitData in unitsList)
                if (unitData.unitId == unitGainedId)
                {
                    UserData.SignedInUserData.ownedUnitsData.Add(unitGainedId, new OwnedCombatUnitData(unitData, 1));
                    unitGainedName = unitData.unitName;
                    break;
                }
        UserData.SignedInUserData.gold += goldGained;
        UserData.SignedInUserData.trophies = Mathf.Max(0, UserData.SignedInUserData.trophies + trophiesGained);

        mainUi.SetActive(false);
        menuUi.SetActive(false);
        inWorldUi.SetActive(false);
        gameOverUiManager.SetDataDisplayed(winner, trophiesGained, goldGained, unitGainedName, autoQuittingDelay);
        gameOverUiManager.ShowGameOverUi(true);
        if (winner)
            CoreUi.Instance.PlaySoundEffect(CoreUi.SoundEffect.MATCH_WON);
        else
            CoreUi.Instance.PlaySoundEffect(CoreUi.SoundEffect.MATCH_LOST);
        runningCoroutine = AutoQuittingCountdown();
        StartCoroutine(runningCoroutine);
    }

    private async void PostMatchResultForUser(string username, int trophiesGained, int goldGained, uint unitGainedId)
    {
#if USING_LOCAL_SERVERS
        const string requestURL = "http://localhost/post_match_result";
#else
        const string requestURL = "https:// - /post_match_result";
#endif
        WWWForm form = new WWWForm();
        form.AddField("Username", username);
        form.AddField("Trophies", trophiesGained);
        form.AddField("Gold", goldGained);
        form.AddField("Unit_id", (unitGainedId != 0 ? unitGainedId.ToString() : "NO_UNIT"));

        UnityWebRequest req = UnityWebRequest.Post(requestURL, form);
        req.disposeUploadHandlerOnDispose = true;
        req.disposeDownloadHandlerOnDispose = true;
        AsyncOperation asyncOp = req.SendWebRequest();
        while (!asyncOp.isDone)
            await Task.Delay(100);

        if (req.result != UnityWebRequest.Result.Success)
            Debug.Log("PostMatchResultForUser: Web request failed! " + req.error);
        else
        {
            string[] reqResponse = req.downloadHandler.text.Split('\t');
            if (reqResponse[0] != "1")
            {
                string error = (reqResponse.Length > 1 ? reqResponse[1] : reqResponse[0]);
                Debug.Log($"PostMatchResultForUser error: {error}");
            }
        }

        req.Dispose();
    }

    private IEnumerator AutoQuittingCountdown()
    {
        // Executed by client.

        float remainedTime = autoQuittingDelay;
        while (remainedTime > 0)
        {
            remainedTime -= Time.deltaTime;
            gameOverUiManager.UpdateQuittingTime((uint)remainedTime);
            yield return null;
        }
        QuitMatch();
    }

    private void QuitMatch()
    {
        // Executed by client.

        if (runningCoroutine != null)
            StopCoroutine(runningCoroutine);
        ClientNetworkManager.Instance.DisableAutomaticReconnection();
        ClientNetworkManager.Instance.StopClient();
        ClientNetworkManager.Instance.WebServerConnectionTesting.StartRequestLoop();
        ClientSceneManager.Instance.ChangeSceneLocallyAsync("MainMenu", false);
    }

    private void OnBothPlayersDisconnected(ushort playersCount)
    {
        // Executed by server.

        if (playersCount == 0)
            // Waiting for other operations to complete before shutdown (like updating the NetworkManager.ConnectedClientsIds list).
            StartCoroutine(ShutDownNextFrame());
    }

    private IEnumerator ShutDownNextFrame()
    {
        // Executed by server.

        yield return null;
        ServerStateMachine.Instance.SetCurrentState(ServerStateMachine.State.SHUTTING_DOWN);
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
