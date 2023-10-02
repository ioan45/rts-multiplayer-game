using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class ServerSceneManager : MonoBehaviour
{
    public static ServerSceneManager Instance { get; private set; }

    private void Awake()
    {
        if (!IsSingletonInstance())
            return;
    }

    private void Start()
    {
        ServerStateMachine.Instance.AddOnEnterCallback(ServerStateMachine.State.WAITING_PLAYERS_TO_CONNECT, OnEnterWaitingPlayersState);
        ServerStateMachine.Instance.AddOnExitCallback(ServerStateMachine.State.WAITING_PLAYERS_TO_CONNECT, OnExitWaitingPlayersState); 
        ServerStateMachine.Instance.AddOnEnterCallback(ServerStateMachine.State.PREPARING_GAME, OnEnterPreparingGameState);

        CoreManager.Instance.SignalComponentInitialized();
    }

    private void OnEnterWaitingPlayersState()
        => ServerNetworkManager.Instance.onPlayersCountChange += OnAllPlayersConnected;
    
    private void OnExitWaitingPlayersState()
        => ServerNetworkManager.Instance.onPlayersCountChange -= OnAllPlayersConnected;

    private void OnAllPlayersConnected(ushort playersCount)
    {
        if (playersCount == 2)
        {
            // Players are connected, start preparing the game. From this point, the server will prepare and
            // simulate the game (even if both players disconnected), and shutdown just after the game ends.
            ServerStateMachine.Instance.SetCurrentState(ServerStateMachine.State.PREPARING_GAME);
        }
    }

    private void OnEnterPreparingGameState()
        => NetworkManager.Singleton.SceneManager.LoadScene("Gameplay", LoadSceneMode.Additive);

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
