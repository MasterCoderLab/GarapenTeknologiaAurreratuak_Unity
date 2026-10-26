using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// NavMesh-eko mugimenduaren eta erasoen arabera animazioak sinkronizatzen ditu.
/// Bi LODetako Animator osagaiak batera eguneratzen ditu.
/// </summary>
[RequireComponent(typeof(HorrorEnemyAI), typeof(NavMeshAgent))]
public sealed class HorrorEnemyAnimation : MonoBehaviour
{
    public Animator[] animators;
    public float attackDuration = 0.9f;

    private HorrorEnemyAI enemy;
    private NavMeshAgent agent;
    private string currentState = "";
    private float attackUntil;

    private void Awake()
    {
        enemy = GetComponent<HorrorEnemyAI>();
        agent = GetComponent<NavMeshAgent>();
    }

    private void OnEnable()
    {
        if (enemy == null) enemy = GetComponent<HorrorEnemyAI>();
        enemy.AttackPerformed += OnAttack;
    }

    private void Start()
    {
        SetAnimation("Idle");
    }

    private void Update()
    {
        if (Time.time < attackUntil || agent == null || !agent.isOnNavMesh) return;
        bool moving = agent.velocity.sqrMagnitude > 0.06f * 0.06f;
        bool chasing = agent.speed > (enemy.patrolSpeed + enemy.chaseSpeed) * 0.5f;
        SetAnimation(!moving ? "Idle" : chasing ? "Chase" : "Patrol");
    }

    private void OnAttack()
    {
        attackUntil = Time.time + Mathf.Max(0.4f, attackDuration);
        SetAnimation("Attack");
    }

    private void SetAnimation(string state)
    {
        if (currentState == state) return;
        currentState = state;
        if (animators == null) return;
        string fullName = "Base Layer." + state;
        foreach (Animator animator in animators)
        {
            if (animator == null || !animator.isActiveAndEnabled) continue;
            animator.CrossFadeInFixedTime(fullName, state == "Attack" ? 0.08f : 0.16f, 0);
        }
    }

    private void OnDisable()
    {
        if (enemy != null) enemy.AttackPerformed -= OnAttack;
    }
}
