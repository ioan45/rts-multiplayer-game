public class MinionState
{
    public enum State { SPAWNING, IDLE, MOVE, ATTACK, DIE }

    public event System.Action onEnterState;
    public event System.Action onExitState;

    public void InvokeOnEnterState() { onEnterState?.Invoke(); }
    public void InvokeOnExitState() { onExitState?.Invoke(); }
}
