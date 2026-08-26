using UnityEngine;
using UnityEngine.AI;

public class Attack : State
{
    private float rotationSpeed = 5.0f;
    private float lastAttackTime = -999f;

    public Attack(GameObject _npc, NavMeshAgent _agent, Animator _animator, Transform _player) 
        : base(_npc, _agent, _animator, _player)
    {
        name = STATE.Attack;
    }

    public override void Enter()
    {
        base.Enter();
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
        }
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
        direction.y = 0;
        if (direction.sqrMagnitude > 0.001f)
        {
            npc.transform.rotation = Quaternion.Slerp(npc.transform.rotation, Quaternion.LookRotation(direction), rotationSpeed * Time.deltaTime);
        }

        float attackRate = ai != null ? ai.AttackRate : 1.0f;

        // Perform attack with cooldown timer
        if (Time.time >= lastAttackTime + attackRate)
        {
            PerformAttack();
            lastAttackTime = Time.time;
        }

        // If player stepped out of attack range, transition back to Chase immediately
        if (!CanAttackPlayer())
        {
            nextState = new Chase(npc, agent, animator, currentTarget);
            stage = Event.Exit;
        }
    }

    private void PerformAttack()
    {
        if (currentTarget == null) return;

        // Deal damage and knockback to the player component
        NewClimbing player = currentTarget.GetComponentInParent<NewClimbing>();
        if (player != null && player.IsAlive)
        {
            float damage = ai != null ? ai.AttackDamage : 15f;
            float knockback = ai != null ? ai.KnockbackForce : 12f;

            // Direction from enemy to player with a slight upward lift
            Vector3 knockbackDir = (currentTarget.position - npc.transform.position).normalized;
            knockbackDir.y = 0.35f;
            knockbackDir.Normalize();

            Vector3 knockbackVector = knockbackDir * knockback;

            player.TakeDamage(damage, knockbackVector);
        }
    }

    public override void Exit()
    {
        base.Exit();
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
        }
    }
}