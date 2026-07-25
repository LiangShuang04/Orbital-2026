using System;
using System.Collections.Generic;
using DontDiePlease.Central.Combat;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// The enemy "body": a finite state machine that drives a NavMeshAgent to chase
/// and attack the player. All tuning comes from an EnemyStats asset so behaviour
/// is data-driven. The LLM "brain" (M3) will sit on top of this and only choose
/// high-level intent; this script keeps running every frame regardless
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(EnemyHealth))]
public class EnemyController : MonoBehaviour
{
    public static event Action<EnemyController> AnyEnemyDetected;

    enum State { Patrol, Chase, Attack, Dead }

    [SerializeField] private EnemyStats stats;
    [Tooltip("Animator on the robot model. (Optional, leave empty to skip animation)")]
    [SerializeField] private Animator animator;

    private NavMeshAgent agent;
    private EnemyHealth health;
    private CentralEnemyVisualDriver visuals;
    private Transform player;
    private PlayerStats playerStats;

    private State state = State.Patrol;
    private float lastAttackTime = -999f;

    // Cached animator parameter IDs (faster than string lookups each frame).
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int DeadHash = Animator.StringToHash("Dead");

    // Which of the above parameters actually exist on the assigned Animator, so we
    // never set a parameter the controller doesn't have (which logs an error).
    private readonly HashSet<int> animParams = new HashSet<int>();

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        health = GetComponent<EnemyHealth>();
        visuals = GetComponent<CentralEnemyVisualDriver>();

        // Apply data-driven tuning to the agent.
        if (stats != null)
        {
            agent.speed = stats.moveSpeed;
            agent.stoppingDistance = stats.stoppingDistance;
        }

        // Find the player (tag your player GameObject "Player" in the Inspector).
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerStats = playerObj.GetComponent<PlayerStats>();
        }
        else
        {
            Debug.LogWarning($"{name}: no GameObject tagged 'Player' found: enemy will idle", this);
        }

        // Record which animator parameters exist so we only set valid ones.
        if (animator != null)
            foreach (var p in animator.parameters)
                animParams.Add(p.nameHash);

        health.OnDied += Die;
    }

    void OnDestroy()
    {
        // Always unsubscribe to avoid calling Die() on a destroyed object.
        if (health != null) health.OnDied -= Die;
    }

    void Update()
    {
        if (state == State.Dead || player == null || stats == null) return;

        float dist = Vector3.Distance(transform.position, player.position);

        switch (state)
        {
            case State.Patrol:
                // Idle for now; transition to Chase when the player is spotted.
                // (Later: wander between patrol points, or use a line-of-sight check.)
                if (dist <= stats.detectionRange)
                {
                    state = State.Chase;
                    AnyEnemyDetected?.Invoke(this);
                }
                break;

            case State.Chase:
                agent.isStopped = false;
                agent.SetDestination(player.position);
                if (dist <= stats.attackRange) state = State.Attack;
                else if (dist > stats.detectionRange) state = State.Patrol;
                break;

            case State.Attack:
                agent.isStopped = true;       // stop moving while attacking
                FacePlayer();
                if (Time.time >= lastAttackTime + stats.attackCooldown)
                {
                    lastAttackTime = Time.time;
                    if (animator != null && animParams.Contains(AttackHash)) animator.SetTrigger(AttackHash);
                    visuals?.PlayAttack();
                    if (playerStats != null) playerStats.TakeDamage(stats.attackDamage);
                }
                if (dist > stats.attackRange) state = State.Chase;
                break;
        }

        UpdateAnimator();
    }

    /// <summary>Rotate to look at the player on the horizontal plane only.</summary>
    private void FacePlayer()
    {
        Vector3 dir = player.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;
        Quaternion target = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, target, 10f * Time.deltaTime);
    }

    /// <summary>Drive the walk/idle blend from how fast the agent is actually moving.</summary>
    private void UpdateAnimator()
    {
        if (animator != null && animParams.Contains(SpeedHash)) animator.SetFloat(SpeedHash, agent.velocity.magnitude);
        visuals?.SetMoving(agent.velocity.sqrMagnitude > 0.01f);
    }

    /// <summary>Called by EnemyHealth.OnDied. Stops the agent and ends behaviour.</summary>
    private void Die()
    {
        state = State.Dead;
        if (agent != null) agent.isStopped = true;
        if (animator != null && animParams.Contains(DeadHash)) animator.SetTrigger(DeadHash);
        visuals?.PlayDeath();

        // Stop colliding/blocking the player. Destroy after a delay so the death
        // animation can play (tune the 3s to your clip length).
        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
        Destroy(gameObject, 3f);
    }
}
