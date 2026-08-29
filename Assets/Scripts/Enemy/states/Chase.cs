using UnityEngine;
using UnityEngine.AI;

public class Chase : State
{
    private Vector3 lastKnownPosition;
    private float lostSightTimer = 0f;
    private float unreachableTimer = 0f;
    private float recheckDestinationTimer = 0f;
    private bool isPursuingLastKnown = false;

    public Chase(GameObject _npc, NavMeshAgent _agent, Animator _animator, Transform _target) 
        : base(_npc, _agent, _animator, _target)
    {
        name = STATE.Chase;
        if (agent != null)
        {
            agent.speed = ai != null ? ai.ChaseSpeed : 4.0f;
            agent.isStopped = false;
        }
        if (currentTarget != null)
        {
            lastKnownPosition = currentTarget.position;
        }
    }

    public override void Enter()
    {
        base.Enter();
        if (currentTarget != null)
        {
            lastKnownPosition = currentTarget.position;
        }
    }

    public override void Update()
    {
        // 1. If current target disconnected, died, disabled, or went missing
        if (!IsTargetValid(currentTarget))
        {
            currentTarget = null;
            if (CanSeePlayer(out Transform visiblePlayer))
            {
                currentTarget = visiblePlayer;
                lastKnownPosition = currentTarget.position;
                lostSightTimer = 0f;
                unreachableTimer = 0f;
                isPursuingLastKnown = false;
            }
            else
            {
                GiveUpChase();
                return;
            }
        }

        float maxVisionDist = ai != null ? ai.VisionDistance : visDistance;
        float maxChaseDistance = maxVisionDist * 1.5f;
        float distanceToTarget = Vector3.Distance(npc.transform.position, currentTarget.position);

        // 2. Check if player escaped beyond max chase distance
        if (distanceToTarget > maxChaseDistance)
        {
            GiveUpChase();
            return;
        }

        // 3. Line of Sight Check
        bool hasLoS = HasLineOfSight(currentTarget);
        float loseSightDuration = ai != null ? ai.LoseSightDuration : 2.0f;

        if (hasLoS)
        {
            // Player is in sight: refresh last known position and reset lost sight timer
            lastKnownPosition = currentTarget.position;
            lostSightTimer = 0f;
            isPursuingLastKnown = false;
        }
        else
        {
            // Line of sight is blocked by a wall, closed door, or obstacle
            lostSightTimer += Time.deltaTime;
            isPursuingLastKnown = true;

            if (lostSightTimer >= loseSightDuration)
            {
                // Lost sight of player for too long, give up chase
                GiveUpChase();
                return;
            }
        }

        // 4. Reachability Check: Check if player climbed out of vertical reach or NavMesh path is blocked
        float maxVerticalDist = ai != null ? ai.MaxReachVerticalDistance : 3.0f;
        float verticalDifference = Mathf.Abs(currentTarget.position.y - npc.transform.position.y);
        bool isVerticallyOutOfReach = verticalDifference > maxVerticalDist;

        bool isNavMeshPathBlocked = false;
        if (agent != null && agent.isOnNavMesh)
        {
            if (!agent.pathPending && (agent.pathStatus == NavMeshPathStatus.PathPartial || agent.pathStatus == NavMeshPathStatus.PathInvalid))
            {
                isNavMeshPathBlocked = true;
            }
        }

        if (isVerticallyOutOfReach || isNavMeshPathBlocked)
        {
            unreachableTimer += Time.deltaTime;
            float maxUnreachableTime = ai != null ? ai.UnreachableDuration : 1.5f;

            if (unreachableTimer >= maxUnreachableTime)
            {
                // Player is out of reach on a wall/ceiling/ledge, give up chase
                GiveUpChase();
                return;
            }
        }
        else
        {
            unreachableTimer = Mathf.Max(0f, unreachableTimer - Time.deltaTime);
        }

        // 5. Drive NavMeshAgent toward target or last known seen position
        if (agent != null && agent.isOnNavMesh)
        {
            recheckDestinationTimer += Time.deltaTime;
            if (recheckDestinationTimer >= 0.1f)
            {
                recheckDestinationTimer = 0f;
                Vector3 destination = isPursuingLastKnown ? lastKnownPosition : currentTarget.position;
                agent.SetDestination(destination);
            }

            // If pursuing last known position and arrived there without finding the player, stop chase
            if (isPursuingLastKnown && !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.6f)
            {
                GiveUpChase();
                return;
            }
        }

        // 6. Check state transitions: Attack if in attack range and line of sight is clear
        if (CanAttackPlayer() && hasLoS)
        {
            nextState = new Attack(npc, agent, animator, currentTarget);
            stage = Event.Exit;
            return;
        }

        // 7. Retarget if a closer visible player comes into field of view
        if (CanSeePlayer(out Transform closerPlayer) && closerPlayer != currentTarget)
        {
            currentTarget = closerPlayer;
            lastKnownPosition = currentTarget.position;
            lostSightTimer = 0f;
            unreachableTimer = 0f;
            isPursuingLastKnown = false;
        }
    }

    private void GiveUpChase()
    {
        nextState = new Patrol(npc, agent, animator, null);
        stage = Event.Exit;
    }
}