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

    [Header("Perception Settings")]
    [Tooltip("Maximum vision distance to spot players.")]
    [SerializeField] private float visionDistance = 10f;

    [Tooltip("Vision cone half-angle in degrees.")]
    [SerializeField] private float visionAngle = 35f;

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
}
