using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.AI;
public class Attack : State
{
    float rotationSpeed = 2.0f;
    AudioSource shoot;
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
    }

    public override void Update()
    {
        Vector3 direction = currentTarget.position - npc.transform.position;
        float angle = Vector3.Angle(direction, npc.transform.forward);
        direction.y = 0;
        npc.transform.rotation = Quaternion.Slerp(npc.transform.rotation, Quaternion.LookRotation(direction), rotationSpeed * Time.deltaTime);

        if(!CanAttackPlayer())
        {
            nextState = new Idle(npc, agent, animator, currentTarget);
            stage = Event.Exit;
        }
    }

    public override void Exit()
    {
        //animator.ResetTrigger("Attack");
        base.Exit();
    }
}
