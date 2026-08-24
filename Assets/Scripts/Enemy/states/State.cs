using UnityEngine;
using UnityEngine.AI;

public class State
{
    public enum STATE
    {
        Idle,
        Patrol,
        Chase,
        Attack,
        Stunned,
        Dead
    }

    public enum Event
    {
        Enter,
        Update,
        Exit
    }

    public STATE name;
    protected Event stage;
    protected GameObject npc;
    protected Animator animator;
    protected Transform currentTarget; 
    protected NavMeshAgent agent;
    protected State nextState;

    protected float visDistance = 10.0f;
    protected float visAngle = 30.0f;
    protected float attackDistance = 2.0f;

    public State(GameObject _npc, NavMeshAgent _agent, Animator _animator, Transform _target)
    {
        npc = _npc;
        agent = _agent;
        animator = _animator;
        currentTarget = _target;
        stage = Event.Enter;
    }

    public virtual void Enter() { stage = Event.Update; }
    public virtual void Update() { stage = Event.Update; }
    public virtual void Exit() { stage = Event.Exit; }

    public State Process()
    {
        if (stage == Event.Enter) Enter();
        if (stage == Event.Update) Update();
        if (stage == Event.Exit)
        {
            Exit();
            return nextState;
        }
        return this;
    }
    public bool CanSeePlayer(out Transform visiblePlayer)
    {
        visiblePlayer = null;
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        float closestDistance = visDistance;

        foreach (GameObject p in players)
        {
            if (p == null) continue;

            Vector3 direction = p.transform.position - npc.transform.position;
            float distance = direction.magnitude;
            float angle = Vector3.Angle(direction, npc.transform.forward);

            if (distance < closestDistance && angle < visAngle)
            {
                closestDistance = distance;
                visiblePlayer = p.transform;
            }
        }

        return visiblePlayer != null;
    }
    public bool CanSeePlayer()
    {
        return CanSeePlayer(out _);
    }

    public bool CanAttackPlayer()
    {
        if (currentTarget == null) return false;

        Vector3 direction = currentTarget.position - npc.transform.position;
        return direction.magnitude < attackDistance;
    }
}
