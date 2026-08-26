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
    protected AI ai;

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

        if (npc != null)
        {
            ai = npc.GetComponent<AI>();
        }
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

    public bool IsTargetValid(Transform target)
    {
        if (target == null || !target.gameObject.activeInHierarchy) return false;

        NewClimbing climbing = target.GetComponentInParent<NewClimbing>();
        if (climbing == null || !climbing.IsAlive || !climbing.gameObject.activeInHierarchy) return false;

        return true;
    }

    public bool CanSeePlayer(out Transform visiblePlayer)
    {
        visiblePlayer = null;
        NewClimbing[] allPlayers = Object.FindObjectsByType<NewClimbing>(FindObjectsSortMode.None);

        float maxVisionDist = ai != null ? ai.VisionDistance : visDistance;
        float maxVisionAngle = ai != null ? ai.VisionAngle : visAngle;
        float closestDistance = maxVisionDist;

        for (int i = 0; i < allPlayers.Length; i++)
        {
            NewClimbing player = allPlayers[i];
            if (player == null || !player.gameObject.activeInHierarchy || !player.IsAlive) continue;

            Vector3 direction = player.transform.position - npc.transform.position;
            float distance = direction.magnitude;
            float angle = Vector3.Angle(direction, npc.transform.forward);

            if (distance < closestDistance && angle < maxVisionAngle)
            {
                closestDistance = distance;
                visiblePlayer = player.transform;
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
        if (!IsTargetValid(currentTarget)) return false;

        float maxAttackDist = ai != null ? ai.AttackRange : attackDistance;
        Vector3 direction = currentTarget.position - npc.transform.position;
        return direction.magnitude <= maxAttackDist;
    }
}
