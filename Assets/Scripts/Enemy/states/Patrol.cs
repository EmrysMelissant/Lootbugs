using UnityEngine;
using UnityEngine.AI;

public class Patrol : State
{
    int currentWaypointIndex = -1;

    public Patrol(GameObject _npc, NavMeshAgent _agent, Animator _animator, Transform _target) 
        : base(_npc, _agent, _animator, _target)
    {
        name = STATE.Patrol;
        if (agent != null)
        {
            agent.speed = ai != null ? ai.PatrolSpeed : 2.0f;
            agent.isStopped = false;
        }
    }

    public override void Enter()
    {
        // animator.SetTrigger("Walk");
        base.Enter();
    }

    public override void Update()
    {
        if (agent.remainingDistance < 1f)
        {
            if (GameEnviroment.Singleton.checkpoints.Count > 0)
            {
                if (currentWaypointIndex >= GameEnviroment.Singleton.checkpoints.Count - 1)
                {
                    currentWaypointIndex = 0;
                }
                else
                {
                    currentWaypointIndex++;
                }
                
                agent.SetDestination(GameEnviroment.Singleton.checkpoints[currentWaypointIndex].transform.position);
            }
        }

        if (CanSeePlayer(out Transform spottedPlayer))
        {
            nextState = new Chase(npc, agent, animator, spottedPlayer);
            stage = Event.Exit;
        }
    }

    public override void Exit()
    {
        // animator.ResetTrigger("Walk");
        base.Exit();
    }
}
