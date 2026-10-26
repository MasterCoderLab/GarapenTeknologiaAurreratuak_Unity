using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Etsaien portaera: ausazko patruila, ikusmena, entzumena eta jazarpena.
/// NavMesh erabiliz hormak saihesten ditu; ez du waypoint finkorik behar.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public sealed class HorrorEnemyAI : MonoBehaviour
{
    public event System.Action AttackPerformed;
    public Transform player;
    public Vector3 sectorCenter;
    public Vector2 halfSectorSize = new Vector2(8.5f, 6f);
    public float viewDistance = 11f;
    public float hearingDistance = 3.5f;
    public float viewAngle = 120f;
    public float chaseSpeed = 3.2f;
    public float patrolSpeed = 1.4f;
    public float lostTargetSeconds = 4f;
    public float attackDistance = 1.35f;
    public float attackCooldown = 2.5f;

    private NavMeshAgent agent;
    private Vector3 lastSeenPosition;
    private float lastSeenAt = -100f;
    private float nextSearch;
    private float nextAttack;
    private float nextPatrol;
    private bool investigating;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (player == null || !agent.isOnNavMesh)
        {
            Debug.LogError("HORROR_AI_ERROR: jokalaria edo NavMesh falta da: " + name);
            enabled = false;
            return;
        }
        ChoosePatrolPoint();
    }

    private void Update()
    {
        if (agent == null || !agent.isOnNavMesh || player == null) return;
        if (Time.time < nextSearch) return;
        nextSearch = Time.time + 0.20f;

        float distance = Vector3.Distance(transform.position, player.position);
        bool near = distance <= hearingDistance;
        bool seen = distance <= viewDistance &&
            Vector3.Angle(transform.forward, player.position - transform.position) <= viewAngle * 0.5f &&
            HasLineOfSight();

        if (near || seen)
        {
            lastSeenAt = Time.time;
            lastSeenPosition = player.position;
            investigating = true;
            agent.speed = chaseSpeed;
            agent.stoppingDistance = 1.0f;
            if (NavMesh.SamplePosition(lastSeenPosition, out NavMeshHit target, 1.6f, NavMesh.AllAreas))
                agent.SetDestination(target.position);

            if (distance <= attackDistance && HasLineOfSight() && Time.time >= nextAttack)
            {
                nextAttack = Time.time + attackCooldown;
                // Une honetan PerderVida() metodoak Debug.Log bakarrik egiten du.
                player.gameObject.SendMessage("PerderVida", SendMessageOptions.DontRequireReceiver);
                AttackPerformed?.Invoke();
                Debug.Log("HORROR_AI_ATTACK: " + name);
            }
            return;
        }

        if (investigating)
        {
            agent.speed = chaseSpeed;
            agent.stoppingDistance = 0.25f;
            if (Time.time - lastSeenAt < lostTargetSeconds)
                return; // Azken posizio ezaguna aztertu.
            investigating = false;
            nextPatrol = Time.time + Random.Range(0.4f, 1.4f);
        }

        agent.speed = patrolSpeed;
        agent.stoppingDistance = 0.25f;
        if (!agent.pathPending && (agent.remainingDistance < 0.8f || !agent.hasPath) &&
            Time.time >= nextPatrol)
        {
            ChoosePatrolPoint();
            nextPatrol = Time.time + Random.Range(1f, 3f);
        }
    }

    private void ChoosePatrolPoint()
    {
        agent.speed = patrolSpeed;
        agent.stoppingDistance = 0.25f;
        var path = new NavMeshPath();
        for (int i = 0; i < 35; i++)
        {
            var candidate = new Vector3(
                sectorCenter.x + Random.Range(-halfSectorSize.x, halfSectorSize.x),
                0.6f,
                sectorCenter.z + Random.Range(-halfSectorSize.y, halfSectorSize.y));
            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 0.65f, NavMesh.AllAreas) &&
                NavMesh.CalculatePath(transform.position, hit.position, agent.areaMask, path) &&
                path.status == NavMeshPathStatus.PathComplete &&
                Vector3.Distance(transform.position, hit.position) >= 2f)
            {
                agent.SetDestination(hit.position);
                return;
            }
        }
        nextPatrol = Time.time + 2f;
    }

    private bool HasLineOfSight()
    {
        // Kapsula propioa alde batera utzi, eta lehen oztopo erreala aztertu.
        Vector3 from = transform.position + Vector3.up * 1.45f;
        Vector3 to = player.position + Vector3.up * 0.25f;
        Vector3 direction = to - from;
        float length = direction.magnitude;
        if (length < 0.01f) return true;
        RaycastHit[] hits = Physics.RaycastAll(from, direction.normalized, length,
            ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (RaycastHit hit in hits)
        {
            if (hit.transform.IsChildOf(transform)) continue;
            return hit.transform.IsChildOf(player);
        }
        return true;
    }
}
