//
// The commented code here corresponds to the usage of the A* Pathfinding Project made by Aron Granberg (https://arongranberg.com/astar/)
//

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
// using Pathfinding;

public class MinionBehaviour : CombatUnitBehaviour
{
    public NetworkVariable<FixedString32Bytes> MainTargetObjName { get; private set; }
    public Collider MainTargetColl { get; set; }
    public Collider CurrentTargetColl { get; set; }
    public float SpawnDuration { get; set; }

    private MinionState[] states;
    private MinionState.State currentState;
    // private AIPath aipathComp;
    // private AIDestinationSetter aiTargetComp;
    private bool playedSpawnSoundEffect;

    public override void Awake()
    {
        base.Awake();

        MainTargetObjName = new NetworkVariable<FixedString32Bytes>();

        if (NetworkManager.Singleton.IsClient)
        {
            // Destroy(GetComponent<AIPath>());
            // Destroy(GetComponent<Seeker>());
            // Destroy(GetComponent<AIDestinationSetter>());

            playedSpawnSoundEffect = false;
        }
        else
        {
            // aipathComp = GetComponent<AIPath>();
            // aiTargetComp = GetComponent<AIDestinationSetter>();
            // aipathComp.canMove = false;
            // aipathComp.canSearch = false;
            SpawnDuration = 0.7f;

            states = new MinionState[System.Enum.GetNames(typeof(MinionState.State)).Length];
            for (int i = states.Length - 1; i >= 0; --i)
                states[i] = new MinionState();

            MinionState moveState = GetStateObject(MinionState.State.MOVE);
            moveState.onEnterState += OnEnterMoveState;
            moveState.onExitState += OnExitMoveState;
            MinionState spawningState = GetStateObject(MinionState.State.SPAWNING);
            spawningState.onEnterState += OnEnterSpawningState;
            
            MainTargetObjName.OnValueChanged += OnChangeMainTarget;
        }

        this.enabled = false;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            var pm = PlayersManager.Instance;
            GameplayData ownerData = (this.ownerPlayerNumber == 1) ? pm.Player1Data.gameplayData : pm.Player2Data.gameplayData;
            transform.forward = ownerData.unitFacingDirOnSpawn;
            MainTargetObjName.Value = ownerData.mainTargetObjName;
            this.gameObject.layer = ownerData.alliesLayer;
            enemyTeamLayer = ownerData.enemiesLayer;

            // aipathComp = GetComponent<AIPath>();
            // aipathComp.maxSpeed = CurrentBasicStats.MovementSpeed.Value;
            CurrentBasicStats.HealthPoints.onValueChanged += OnMinionDeath;

            this.enabled = true;

            currentState = MinionState.State.SPAWNING;
            states[(int)currentState].InvokeOnEnterState();
        }
        else
        {
            if (!playedSpawnSoundEffect)
            {
                CoreUi.Instance.PlaySoundEffect(CoreUi.SoundEffect.BATTLE_UNIT_SPAWN);
                playedSpawnSoundEffect = true;
            }
        }
    }

    private void FixedUpdate()
    {
        // Codul prezent presupune ca attackRange <= targetSearchRange

        if (IsServer)
        {
            Vector3 closestPointOnTarget;

            if (CurrentTargetColl != null && CurrentTargetColl != MainTargetColl)
            {
                closestPointOnTarget = CurrentTargetColl.ClosestPointOnBounds(transform.position);
                if (Vector3.Distance(transform.position, closestPointOnTarget) > CurrentBasicStats.TargetSearchRange.Value)
                    CurrentTargetColl = null;
            }

            if (CurrentTargetColl != null)
            {
                var targetMbComp = CurrentTargetColl.GetComponent<MinionBehaviour>();
                if (targetMbComp != null && targetMbComp.GetCurrentState() == MinionState.State.DIE)
                    CurrentTargetColl = null;
            }

            if (CurrentTargetColl == null || currentState != MinionState.State.ATTACK)
            {
                Collider nearbyTarget = TryToFindNearbyTarget();
                if (nearbyTarget != null)
                {
                    CurrentTargetColl = nearbyTarget;
                    // aiTargetComp.target = nearbyTarget.transform;
                }
                else if (CurrentTargetColl == null)
                {
                    CurrentTargetColl = MainTargetColl;
                    // aiTargetComp.target = MainTargetColl.transform;
                }
            }

            closestPointOnTarget = CurrentTargetColl.ClosestPointOnBounds(transform.position);
            if (currentState == MinionState.State.MOVE || currentState == MinionState.State.IDLE)
            {
                if (Vector3.Distance(transform.position, closestPointOnTarget) <= CurrentBasicStats.AttackRange.Value)
                    SetCurrentState(MinionState.State.ATTACK);
            }
            if (currentState == MinionState.State.ATTACK || currentState == MinionState.State.IDLE)
            {
                if (Vector3.Distance(transform.position, closestPointOnTarget) > CurrentBasicStats.AttackRange.Value)
                    SetCurrentState(MinionState.State.MOVE);
            }
        }
    }

    public MinionState GetStateObject(MinionState.State forState)
    {
        return states[(int)forState];
    }

    public MinionState.State GetCurrentState()
    {
        return currentState;
    }

    public void SetCurrentState(MinionState.State newState)
    {
        states[(int)currentState].InvokeOnExitState();
        currentState = newState;
        states[(int)currentState].InvokeOnEnterState();
    }

    public void PlaySoundEffectOnClients(CoreUi.SoundEffect soundEffect)
        => PlaySoundEffectClientRpc(soundEffect);

    [ClientRpc]
    private void PlaySoundEffectClientRpc(CoreUi.SoundEffect soundEffect)
    {
        CoreUi.Instance.PlaySoundEffect(soundEffect);
    }

    private void OnChangeMainTarget(FixedString32Bytes previousValue, FixedString32Bytes newValue)
    {
        MainTargetColl = GameObject.Find(newValue.ToString()).GetComponent<Collider>();
        if (CurrentTargetColl == null)
        {
            CurrentTargetColl = MainTargetColl;
            // aiTargetComp.target = CurrentTargetColl.transform;
        }
    }

    private void OnEnterSpawningState()
    {
        StartCoroutine(DelaySpawnCoroutine());
    }

    private IEnumerator DelaySpawnCoroutine()
    {
        yield return new WaitForSeconds(SpawnDuration);
        SetCurrentState(MinionState.State.IDLE);
    }

    private void OnEnterMoveState()
    {
        // aipathComp.canMove = true;
        // aipathComp.canSearch = true;
        // aipathComp.SearchPath();
    }

    private void OnExitMoveState()
    {
        // aipathComp.canMove = false;
        // aipathComp.canSearch = false;
    }

    private void OnMinionDeath(float prevHp, float newHp)
    {
        if (newHp <= 0)
            SetCurrentState(MinionState.State.DIE);
    }
}
