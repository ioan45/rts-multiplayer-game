//
// The commented code here corresponds to the usage of the A* Pathfinding Project made by Aron Granberg (https://arongranberg.com/astar/)
//

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
// using Pathfinding;

public class CombatUnitBehaviour : NetworkBehaviour
{
    [field: SerializeField] 
    public CombatUnitBasicData UnitBasicData { get; private set; }
    public CombatUnitBasicStats CurrentBasicStats { get; set; }
    [HideInInspector]
    public uint ownerPlayerNumber;
    public event System.Action<CombatUnitBehaviour> onNetworkSpawn;   // Gives the object itself as argument. 

    protected int enemyTeamLayer;

    private int maxNearbyEnemies;
    private Collider[] nearbyEnemies;

    public virtual void Awake()
    {
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            maxNearbyEnemies = 10;
            nearbyEnemies = new Collider[maxNearbyEnemies];
        }

        onNetworkSpawn?.Invoke(this);
    }

    protected Collider TryToFindNearbyTarget()
    {
        Collider targetCollider = null;
        const int arrayCapIncreaseAmount = 10;
        int sphereLayerMask = 1 << enemyTeamLayer;
        int totalEnemiesFound = Physics.OverlapSphereNonAlloc(transform.position, CurrentBasicStats.TargetSearchRange.Value, nearbyEnemies, sphereLayerMask);
        while (totalEnemiesFound == maxNearbyEnemies)
        {
            maxNearbyEnemies += arrayCapIncreaseAmount;
            nearbyEnemies = new Collider[maxNearbyEnemies];
            Physics.OverlapSphereNonAlloc(transform.position, CurrentBasicStats.TargetSearchRange.Value, nearbyEnemies, sphereLayerMask);
        }
        if (totalEnemiesFound != 0)
        {
            float currentMinDistance = float.MaxValue;
            for (int i = 0; i < totalEnemiesFound; ++i)
            {
                float distanceToEnemy = Vector3.Distance(transform.position, nearbyEnemies[i].transform.position);
                if (distanceToEnemy < currentMinDistance)
                {
                    var targetMbComp = nearbyEnemies[i].GetComponent<MinionBehaviour>();
                    if (targetMbComp == null || targetMbComp.GetCurrentState() != MinionState.State.DIE)
                    {
                        targetCollider = nearbyEnemies[i];
                        currentMinDistance = distanceToEnemy;
                    }
                }
            }
        }
        return targetCollider;
    }
}
