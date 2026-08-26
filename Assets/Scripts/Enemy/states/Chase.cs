using UnityEngine;
using UnityEngine.AI;

public class Chase : State
{
    public Chase(GameObject _npc, NavMeshAgent _agent, Animator _animator, Transform _target) 
        : base(_npc, _agent, _animator, _target)
    {
        name = STATE.Chase;
        if (agent != null)
        {
            agent.speed = ai != null ? ai.ChaseSpeed : 4.0f;
            agent.isStopped = false;
        }
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
        if (agent != null && agent.isOnNavMesh)
        {
            agent.SetDestination(currentTarget.position);
        }

        // Check state transitions: Attack if in range
        if (CanAttackPlayer())
        {
            nextState = new Attack(npc, agent, animator, currentTarget);
            stage = Event.Exit;
            return;
        }

        // Check if player escaped beyond max chase distance
        float maxChaseDistance = (ai != null ? ai.VisionDistance : visDistance) * 1.5f;
        float distanceToTarget = Vector3.Distance(npc.transform.position, currentTarget.position);

        if (distanceToTarget > maxChaseDistance)
        {
            // Lost player, return to patrol
            nextState = new Patrol(npc, agent, animator, null);
            stage = Event.Exit;
        }
        else if (CanSeePlayer(out Transform closerPlayer) && closerPlayer != currentTarget)
        {
            // Dynamically retarget if a closer player comes into vision
            currentTarget = closerPlayer;
        }
    }
}