using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// FSM that moves the enemy with a NavMeshAgent to chase and attack the player
/// Tuning numbers all come from an EnemyStats asset
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(EnemyHealth))]
public class EnemyController : MonoBehaviour
{
    enum State { Patrol, Chase, Attack, Dead }

    [SerializeField] private EnemyStats stats;
    [Tooltip("Animator on the robot model. (Optional, leave empty to skip animation)")]
    [SerializeField] private Animator animator;

    private NavMeshAgent agent;
    private EnemyHealth health;
    private Transform player;
    private PlayerStats playerStats;

    private State state = State.Patrol;
    private float lastAttackTime = -999f;

    // hash the animator params for ease of lookup
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int DeadHash = Animator.StringToHash("Dead");

    // params that actually exist on the animator, setting a missing one gives errors
    private readonly HashSet<int> animParams = new HashSet<int>();

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        health = GetComponent<EnemyHealth>();

        // apply tuning from the stats asset
        if (stats != null)
        {
            agent.speed = stats.moveSpeed;
            agent.stoppingDistance = stats.stoppingDistance;
        }

        // find the player by tag
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

        // remember which animator params exist so we only ever set valid ones
        if (animator != null)
            foreach (var p in animator.parameters)
                animParams.Add(p.nameHash);

        health.OnDied += Die;
    }

    void OnDestroy()
    {
        // ensures Die() isn't called on a destroyed object
        if (health != null) health.OnDied -= Die;
    }

    void Update()
    {
        if (state == State.Dead) return;

        float dist = Vector3.Distance(transform.position, player.position);

        switch (state)
        {
            case State.Patrol:
                // just idle for now, could wander between patrol points later
                if (dist <= stats.detectionRange) state = State.Chase;
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
                    if (playerStats != null) playerStats.TakeDamage(stats.attackDamage);
                }
                if (dist > stats.attackRange) state = State.Chase;
                break;
        }

        UpdateAnimator();
    }

    /// <summary>Rotate to face the player, horizontal plane only</summary>
    private void FacePlayer()
    {
        Vector3 dir = player.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;
        Quaternion target = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, target, 10f * Time.deltaTime);
    }

    /// <summary>walk/idle blend follows how fast the agent is actually moving</summary>
    private void UpdateAnimator()
    {
        if (animator != null && animParams.Contains(SpeedHash)) animator.SetFloat(SpeedHash, agent.velocity.magnitude);
    }

    /// <summary>Called by EnemyHealth.OnDied</summary>
    private void Die()
    {
        state = State.Dead;
        if (agent != null) agent.isStopped = true;
        if (animator != null && animParams.Contains(DeadHash)) animator.SetTrigger(DeadHash);

        // stop blocking the player, destroy after the death anim has time to play
        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
        Destroy(gameObject, 3f);
    }
}
