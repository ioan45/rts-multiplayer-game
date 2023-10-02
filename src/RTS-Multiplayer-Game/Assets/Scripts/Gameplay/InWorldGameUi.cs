using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class InWorldGameUi : NetworkBehaviour
{
    public event System.Action onSpawn;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        onSpawn?.Invoke(); 
    }
}
