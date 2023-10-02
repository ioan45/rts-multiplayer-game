using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

public class WizardBehaviour : MonoBehaviour
{
    [SerializeField]
    private GameObject projectilePrefab;
    [SerializeField]
    private Transform firePoint;
    private MinionBehaviour mb;
    private NetworkAnimator animator;
    private float projectileSpeed;
    float timeFromLastAttack;

    void Awake()
    {
        mb = GetComponent<MinionBehaviour>();
        animator = GetComponent<NetworkAnimator>();
        projectileSpeed = 85.0f;

        if (NetworkManager.Singleton.IsServer)
        {
            MinionState state;
            state = mb.GetStateObject(MinionState.State.MOVE);
            state.onEnterState += OnEnterMoveState;
            state = mb.GetStateObject(MinionState.State.ATTACK);
            state.onEnterState += OnEnterAttackState;
            state = mb.GetStateObject(MinionState.State.DIE);
            state.onEnterState += OnEnterDieState;
        }
    }

    void Update()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            if (mb.GetCurrentState() == MinionState.State.ATTACK)
            {
                timeFromLastAttack += Time.deltaTime;
                if (timeFromLastAttack >= 1.0f / mb.CurrentBasicStats.AttackSpeed.Value && mb.CurrentTargetColl != null) 
                {
                    PerformAttack();
                    timeFromLastAttack = 0.0f;
                }
            }
        }
    }

    private void PerformAttack()
    {
        transform.forward = (mb.CurrentTargetColl.transform.position - transform.position).normalized;
        animator.Animator.SetFloat("AttackSpeedMultiplier", Mathf.Max(1.0f, mb.CurrentBasicStats.AttackSpeed.Value));
        animator.SetTrigger("Attack");
    }

    // This method should be called by an animation event which is raised on a frame that shows a successfully performed attack.
    public void OnAttackSuccesAnimFrame()
    {
        if (NetworkManager.Singleton.IsServer && mb.CurrentTargetColl != null && mb.GetCurrentState() == MinionState.State.ATTACK)
        {
            WizardAttackProjectile projectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity).GetComponent<WizardAttackProjectile>();
            projectile.Target = mb.CurrentTargetColl.transform;
            projectile.transform.forward = this.transform.forward;
            projectile.Speed = projectileSpeed;
            projectile.Damage = mb.CurrentBasicStats.AttackDamage.Value;
            projectile.GetComponent<NetworkObject>().Spawn();
        }
    }

    private void OnEnterMoveState()
    {
        animator.SetTrigger("Move");
    }

    private void OnEnterAttackState()
    {
        timeFromLastAttack = float.MaxValue / 2;
    }

    private void OnEnterDieState()
    {
        animator.SetTrigger("Die");
        if (GameplayManager.Instance.IsInGame.Value)
            mb.PlaySoundEffectOnClients(CoreUi.SoundEffect.BATTLE_UNIT_DEATH);
    }

    // This method should be called by an animation event which is raised on the last frame of the die animation.
    public void OnDieAnimationComplete()
    {
        if (NetworkManager.Singleton.IsServer)
            Destroy(this.gameObject);
    }
}
