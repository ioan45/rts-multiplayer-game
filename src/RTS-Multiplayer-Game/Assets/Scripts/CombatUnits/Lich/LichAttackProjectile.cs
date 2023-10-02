using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class LichAttackProjectile : NetworkBehaviour
{
    public Transform Target { get; set; }
    public float Speed { get; set; } 
    public float Damage { get; set; }

    private float maxLifeSpan;
    private float currentLifeSpan;
    private Vector3 movementDir;

    void Awake()
    {
        maxLifeSpan = 10.0f;
        currentLifeSpan = 0.0f;
    }

    void Update()
    {
        if (IsServer)
        {
            currentLifeSpan += Time.deltaTime;
            if (currentLifeSpan >= maxLifeSpan)
                Destroy(this.gameObject);
            
            if (Target != null)
            {
                movementDir = (Target.transform.position - transform.position).normalized;
                transform.forward = movementDir;
            }
            Vector3 newPos = transform.position + (movementDir * Speed * Time.deltaTime);
            transform.position = newPos;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (IsServer)
        {
            if (Target != null && Object.ReferenceEquals(other.gameObject, Target.gameObject))
            {
                other.GetComponent<CombatUnitBehaviour>().CurrentBasicStats.TakeDamage(Damage);
                if (ServerStateMachine.Instance.GetCurrentState() == ServerStateMachine.State.IN_GAME)
                    PlaySoundOnEnemyHitClientRpc();
                Destroy(this.gameObject);
            }
        }   
    }

    [ClientRpc]
    private void PlaySoundOnEnemyHitClientRpc()
    {
        CoreUi.Instance.PlaySoundEffect(CoreUi.SoundEffect.BATTLE_UNIT_MAGIC_ATTACK);
    }
}
