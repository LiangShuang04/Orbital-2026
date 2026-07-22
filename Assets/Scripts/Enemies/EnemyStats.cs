using UnityEngine;

/// <summary>
/// Tuning values for an enemy, kept as an asset like ItemData so we can
/// rebalance without touching code
/// </summary>
[CreateAssetMenu(fileName = "New Enemy", menuName = "Enemies/Enemy Stats")]
public class EnemyStats : ScriptableObject
{
    [Header("Health")]
    public float maxHealth = 50f;

    [Header("Movement")]
    public float moveSpeed = 3.5f;
    [Tooltip("How close the agent gets before it stops (keeps it from overlapping the target)")]
    public float stoppingDistance = 1.8f;

    [Header("Perception")]
    [Tooltip("Distance at which the enemy notices the player and starts chasing")]
    public float detectionRange = 12f;
    [Tooltip("Distance at which the enemy switches from chasing to attacking")]
    public float attackRange = 2f;

    [Header("Combat")]
    public float attackDamage = 10f;
    [Tooltip("Seconds between attacks")]
    public float attackCooldown = 1.5f;
}
