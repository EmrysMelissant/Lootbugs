using UnityEngine;
using UnityEngine.AI;

public class AI : MonoBehaviour
{
    [Header("Combat Settings")]
    [Tooltip("Damage dealt to the player per attack.")]
    [SerializeField] private float attackDamage = 15f;

    [Tooltip("Attack range / distance to target.")]
    [SerializeField] private float attackRange = 2f;

    [Tooltip("Seconds between attacks.")]
    [SerializeField] private float attackRate = 1.0f;

    [Tooltip("Knockback force applied to the player on hit.")]
    [SerializeField] private float knockbackForce = 12f;

    [Header("Perception & Line of Sight")]
    [Tooltip("Maximum vision distance to spot players.")]
    [SerializeField] private float visionDistance = 10f;

    [Tooltip("Vision cone half-angle in degrees.")]
    [SerializeField] private float visionAngle = 35f;

    [Tooltip("Layers that block line of sight (walls, doors, environment).")]
    [SerializeField] private LayerMask sightObstacleLayers = ~0;

    [Tooltip("Eye height offset above ground for line-of-sight raycasts.")]
    [SerializeField] private float eyeHeight = 1.0f;

    [Tooltip("Seconds the enemy continues pursuing the last seen position after line of sight is broken.")]
    [SerializeField] private float loseSightDuration = 2.0f;

    [Header("Reachability Settings")]
    [Tooltip("Maximum vertical height difference before the player is considered out of reach (e.g. climbing walls).")]
    [SerializeField] private float maxReachVerticalDistance = 3.0f;

    [Tooltip("Seconds the player can remain out of reach or path blocked before the enemy gives up chase.")]
    [SerializeField] private float unreachableDuration = 1.5f;

    [Header("Movement Settings")]
    [Tooltip("Movement speed while chasing a player.")]
    [SerializeField] private float chaseSpeed = 4f;

    [Tooltip("Movement speed while patrolling.")]
    [SerializeField] private float patrolSpeed = 2f;

    private NavMeshAgent agent;
    private Animator animator;
    private State currentState;

    public float AttackDamage => attackDamage;
    public float AttackRange => attackRange;
    public float AttackRate => attackRate;
    public float KnockbackForce => knockbackForce;
    public float VisionDistance => visionDistance;
    public float VisionAngle => visionAngle;
    public LayerMask SightObstacleLayers => sightObstacleLayers;
    public float EyeHeight => eyeHeight;
    public float LoseSightDuration => loseSightDuration;
    public float MaxReachVerticalDistance => maxReachVerticalDistance;
    public float UnreachableDuration => unreachableDuration;
    public float ChaseSpeed => chaseSpeed;
    public float PatrolSpeed => patrolSpeed;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        currentState = new Idle(gameObject, agent, animator, null);
    }

    void Update()
    {
        if (currentState != null)
        {
            currentState = currentState.Process();
        }
    }

    public void OnHeardNoise(Vector3 noisePosition)
    {
        if (currentState != null && (currentState.name == State.STATE.Idle || currentState.name == State.STATE.Patrol))
        {
            if (agent != null && agent.isOnNavMesh)
            {
                agent.SetDestination(noisePosition);
            }
        }
    }
}
