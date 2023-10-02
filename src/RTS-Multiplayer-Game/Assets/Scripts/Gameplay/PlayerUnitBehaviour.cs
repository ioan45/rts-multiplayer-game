using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerUnitBehaviour : CombatUnitBehaviour
{
    [SerializeField]
    private uint playerNumber;

    public NetworkVariable<float> HealthPoints { get; private set; }
    public Collider CurrentTargetColl { get; set; }

    public override void Awake()
    {
        CurrentBasicStats = new CombatUnitBasicStats(1, this.UnitBasicData);
        ownerPlayerNumber = playerNumber;
        this.HealthPoints = new NetworkVariable<float>(CurrentBasicStats.HealthPoints.Value);
        CurrentBasicStats.HealthPoints.onValueChanged += (_, newValue) => this.HealthPoints.Value = newValue;
    }

    private void Start()
    {
        this.CurrentBasicStats.HealthPoints.onValueChanged += OnDeath;

        if (NetworkManager.Singleton.IsServer)
        {
            PlayersManager pm = PlayersManager.Instance;
            GameplayData pd = (playerNumber == 1) ? pm.Player1Data.gameplayData : pm.Player2Data.gameplayData; 
            this.gameObject.layer = pd.alliesLayer;
            transform.forward = pd.unitFacingDirOnSpawn;
            enemyTeamLayer = pd.enemiesLayer;
        }
    }

    void FixedUpdate()
    {
        if (IsServer)
        {
            if (CurrentTargetColl != null)
            {
                Vector3 closestPointOnTarget = CurrentTargetColl.ClosestPointOnBounds(transform.position);
                if (Vector3.Distance(transform.position, closestPointOnTarget) > CurrentBasicStats.TargetSearchRange.Value)
                    CurrentTargetColl = null;
            }

            if (CurrentTargetColl == null)
                CurrentTargetColl = TryToFindNearbyTarget();
        }
    }

    public void OnDeath(float prevHp, float newHp)
    {
        if (newHp <= 0)
            print($"Player dead: Its layer is {LayerMask.LayerToName(this.gameObject.layer)}.");
    }
}
