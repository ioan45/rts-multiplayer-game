using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ServerStateMachine : MonoBehaviour
{
    public enum State
    {
        INITIALIZING_APP,
        WAITING_PLAYERS_TO_CONNECT,
        PREPARING_GAME,
        IN_GAME,
        GAME_OVER,
        SHUTTING_DOWN
    }

    public class StateCallbacks
    {
        public event System.Action onEnterState;
        public event System.Action onExitState;

        public void InvokeOnEnterState() { onEnterState?.Invoke(); }
        public void InvokeOnExitState() { onExitState?.Invoke(); }
    }

    public static ServerStateMachine Instance { get; private set; }
    
    private State currentState;
    private StateCallbacks[] statesCallbacks;

    private void Awake()
    {
        if (!IsSingletonInstance())
            return;

        currentState = State.INITIALIZING_APP;

        statesCallbacks = new StateCallbacks[System.Enum.GetNames(typeof(State)).Length];
        for (int i = statesCallbacks.Length - 1; i >= 0; --i)
            statesCallbacks[i] = new StateCallbacks();
        
        CoreManager.Instance.SignalComponentInitialized();
    }

    public State GetCurrentState()
        => currentState;

    public void SetCurrentState(State newState)
    {
        statesCallbacks[(int)currentState].InvokeOnExitState();
        currentState = newState;
        statesCallbacks[(int)currentState].InvokeOnEnterState();
    }

    public void AddOnEnterCallback(State forState, System.Action callback)
        => statesCallbacks[(int)forState].onEnterState += callback;

    public void AddOnExitCallback(State forState, System.Action callback)
        => statesCallbacks[(int)forState].onExitState += callback;

    public void RemoveOnEnterCallback(State forState, System.Action callback)
        => statesCallbacks[(int)forState].onEnterState -= callback;

    public void RemoveOnExitCallback(State forState, System.Action callback)
        => statesCallbacks[(int)forState].onExitState -= callback;

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
