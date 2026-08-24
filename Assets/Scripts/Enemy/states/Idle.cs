using UnityEngine;
using UnityEngine.AI;

public class Idle : State
{
    public Idle(GameObject _npc, NavMeshAgent _agent, Animator _animator, Transform _target) 
        : base(_npc, _agent, _animator, _target)
    {
        name = STATE.Idle;
    }

    public override void Enter()
    {
        // animator.SetTrigger("Idle");
        base.Enter();
    }

    public override void Update()
    {
        if (CanSeePlayer(out Transform spottedPlayer))
        {
            nextState = new Chase(npc, agent, animator, spottedPlayer);
            stage = Event.Exit;
        }
        else if (Random.Range(0, 100) < 10)
        {
            nextState = new Patrol(npc, agent, animator, null);
            stage = Event.Exit;
        }
    }

    public override void Exit()
    {
        // animator.ResetTrigger("Idle");
        base.Exit();
    }
}
