using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.AI;

public class Attack : State
{
    float rotationSpeed = 2.0f;
    AudioSource shoot;

    // Attack parameters
    private float attackDamage = 15f;
    private float attackRate = 1.0f; // Seconds between attacks
    private float lastAttackTime;

    public Attack(GameObject _npc, NavMeshAgent _agent, Animator _animator, Transform _player) : base(_npc, _agent, _animator, _player)
    {
        name = STATE.Attack;
        //shoot = npc.GetComponent<AudioSource>();
    }

    public override void Enter()
    {
        //animator.SetTrigger("Attack");
        base.Enter();
        agent.isStopped = true;
        //shoot.Play();
        lastAttackTime = Time.time;
    }

    public override void Update()
    {
        if (!IsTargetValid(currentTarget))
        {
            nextState = new Idle(npc, agent, animator, null);
            stage = Event.Exit;
            return;
        }

        Vector3 direction = currentTarget.position - npc.transform.position;
        float angle = Vector3.Angle(direction, npc.transform.forward);
        direction.y = 0;
        if (direction.sqrMagnitude > 0.001f)
        {
            npc.transform.rotation = Quaternion.Slerp(npc.transform.rotation, Quaternion.LookRotation(direction), rotationSpeed * Time.deltaTime);
        }

        // Perform attack with cooldown timer
        if (Time.time >= lastAttackTime + attackRate)
        {
            PerformAttack();
            lastAttackTime = Time.time;
        }

        if (!CanAttackPlayer())
        {
            nextState = new Idle(npc, agent, animator, null);
            stage = Event.Exit;
        }
    }

    private void PerformAttack()
    {
        if (currentTarget == null) return;

        // Deal damage to the player component
        if (currentTarget.TryGetComponent(out NewClimbing player) && player.IsAlive)
        {
            player.TakeDamage(attackDamage);
        }
    }

    public override void Exit()
    {
        //animator.ResetTrigger("Attack");
        base.Exit();
    }
}