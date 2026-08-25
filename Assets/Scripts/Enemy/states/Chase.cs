using UnityEngine;
using UnityEngine.AI;

public class Chase : State
{
    public Chase(GameObject _npc, NavMeshAgent _agent, Animator _animator, Transform _target) 
        : base(_npc, _agent, _animator, _target)
    {
        name = STATE.Chase;
        agent.speed = 4.0f;
        agent.isStopped = false;
    }

    public override void Enter()
    {
        // animator.SetTrigger("Run");
        base.Enter();
    }

    public override void Update()
    {
        // If current target disconnected, died, disabled, or went missing
        if (!IsTargetValid(currentTarget))
        {
            currentTarget = null;
            if (CanSeePlayer(out Transform visiblePlayer))
            {
                currentTarget = visiblePlayer;
            }
            else
            {
                nextState = new Patrol(npc, agent, animator, null);
                stage = Event.Exit;
                return;
            }
        }

        // Drive agent toward the current target
        agent.SetDestination(currentTarget.position);

        // Check state transitions
        if (CanAttackPlayer())
        {
            nextState = new Attack(npc, agent, animator, currentTarget);
            stage = Event.Exit;
        }
        else if (CanSeePlayer(out Transform visiblePlayer))
        {
            // Dynamically retarget if a closer player comes into vision
            currentTarget = visiblePlayer;
        }
        else
        {
            // Lost line of sight on all players
            nextState = new Patrol(npc, agent, animator, null);
            stage = Event.Exit;
        }
    }
}