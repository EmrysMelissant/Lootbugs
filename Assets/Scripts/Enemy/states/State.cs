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

        PlayerController controller = target.GetComponentInParent<PlayerController>();
        if (controller == null || !controller.IsAlive || !controller.gameObject.activeInHierarchy) return false;

        return true;
    }

    public bool HasLineOfSight(Transform target)
    {
        if (target == null || npc == null) return false;

        float eyeH = ai != null ? ai.EyeHeight : 1.0f;
        Vector3 eyePos = npc.transform.position + Vector3.up * eyeH;
        Vector3 targetPos = target.position + Vector3.up * 1.0f;

        Vector3 dir = targetPos - eyePos;
        float dist = dir.magnitude;
        if (dist <= 0.05f) return true;

        LayerMask mask = ai != null ? ai.SightObstacleLayers : ~0;

        if (Physics.Raycast(eyePos, dir.normalized, out RaycastHit hit, dist, mask, QueryTriggerInteraction.Ignore))
        {
            // If the ray hits the target or a child/parent of the target, line of sight is clear
            if (hit.transform == target || hit.transform.IsChildOf(target) || target.IsChildOf(hit.transform))
            {
                return true;
            }

            // Check if it hit the player's PlayerController component
            PlayerController hitPlayer = hit.collider.GetComponentInParent<PlayerController>();
            PlayerController targetPlayer = target.GetComponentInParent<PlayerController>();
            if (hitPlayer != null && targetPlayer != null && hitPlayer == targetPlayer)
            {
                return true;
            }

            // Line of sight is blocked by a wall, door, or obstacle
            return false;
        }

        // Ray reached target without hitting any blocking obstacle
        return true;
    }

    public bool CanSeePlayer(out Transform visiblePlayer)
    {
        visiblePlayer = null;
        PlayerController[] allPlayers = Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None);

        float maxVisionDist = ai != null ? ai.VisionDistance : visDistance;
        float maxVisionAngle = ai != null ? ai.VisionAngle : visAngle;
        float closestDistance = maxVisionDist;

        for (int i = 0; i < allPlayers.Length; i++)
        {
            PlayerController player = allPlayers[i];
            if (player == null || !player.gameObject.activeInHierarchy || !player.IsAlive) continue;

            Vector3 direction = player.transform.position - npc.transform.position;
            float distance = direction.magnitude;
            float angle = Vector3.Angle(direction, npc.transform.forward);

            if (distance < closestDistance && angle < maxVisionAngle)
            {
                // Verify line of sight raycast
                if (HasLineOfSight(player.transform))
                {
                    closestDistance = distance;
                    visiblePlayer = player.transform;
                }
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
