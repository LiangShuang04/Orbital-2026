using System;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    PlayerMovement movement;
    CameraController camController;

    [Header("Health")]
    public float maxHealth = 100f;
    public float currentHealth = 100f;

    [Header("Oxygen")]
    public float maxOxygen = 100f;
    public float currentOxygen = 100f;
    public float oxygenDepletionRate = 2f;
    public float oxygenRegenerationRate = 10f;
    public float suffocationDamage = 5f;

    [Header("Saturation")]
    public float maxSaturation = 100f;
    public float currentSaturation = 100f;
    public float saturationDepletionRate = 0.5f;
    public float starvationDamage = 10f;

    [Header("Toxicity")]
    public float maxToxicity = 100f;
    public float currentToxicity = 0f;
    public float toxicityBuildupRate = 0.1f;
    public float toxicityDecayRate = 0f;

    [Header("Environment")]
    public bool isInsideShip = true;

    [Header("On Death")]
    [Tooltip("Components disabled when the player dies (drag in the Akila FirstPersonController, CharacterInput, and any weapon/look scripts)")]
    public Behaviour[] disableOnDeath;
    [Tooltip("Where the player is placed on respawn. Leave empty to respawn on the spot")]
    public Transform respawnPoint;

    public bool IsDead => isDead;
    public event Action OnDied;

    bool isDead;

    void Start()
    {
        isDead = false;
        movement = GetComponent<PlayerMovement>();
        camController = GetComponentInChildren<CameraController>();
    }

    void Update()
    {
        if (isDead) return;
        var dt = Time.deltaTime;

        if (isInsideShip)
        {
            currentOxygen = Mathf.Min(maxOxygen,  currentOxygen   + oxygenRegenerationRate * dt);
            currentToxicity = Mathf.Max(0f, currentToxicity - toxicityDecayRate * dt);
        }
        else
        {
            currentOxygen = Mathf.Max(0f, currentOxygen - oxygenDepletionRate * dt);
            currentToxicity = Mathf.Min(maxToxicity,currentToxicity + toxicityBuildupRate * dt);
        }

        currentSaturation = Mathf.Max(0f, currentSaturation - saturationDepletionRate * dt);

        if (currentOxygen <= 0f) currentHealth -= suffocationDamage * dt;
        if (currentSaturation <= 0f) currentHealth -= starvationDamage * dt;
        if (currentToxicity >= maxToxicity) currentHealth = 0f;

        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        if (currentHealth <= 0f) Die();
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        foreach (var b in disableOnDeath)
            if (b != null) b.enabled = false;

        // legacy references, only used if this player has the old controller scripts
        if (movement != null) movement.enabled = false;
        if (camController != null) camController.enabled = false;

        // freeze the Akila controller automatically (matched by name, no hard dependency)
        SetMovementEnabled(false);

        OnDied?.Invoke();
        Debug.Log("Player died");
    }

    public void Respawn()
    {
        isDead = false;
        currentHealth = maxHealth;
        currentOxygen = maxOxygen;
        currentSaturation = maxSaturation;
        currentToxicity = 0f;

        foreach (var b in disableOnDeath)
            if (b != null) b.enabled = true;

        if (movement != null) movement.enabled = true;
        if (camController != null) camController.enabled = true;

        SetMovementEnabled(true);

        if (respawnPoint != null)
        {
            transform.position = respawnPoint.position;
            transform.rotation = respawnPoint.rotation;
        }
    }

    // enables/disables the Akila movement + input by type name, so PlayerStats
    // does not need a compile-time reference to the Akila package
    void SetMovementEnabled(bool enabled)
    {
        foreach (var mb in GetComponents<MonoBehaviour>())
        {
            var n = mb.GetType().Name;
            if (n == "FirstPersonController" || n == "CharacterInput")
                mb.enabled = enabled;
        }
    }

    public void Heal(float amount) => currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
    public void RestoreOxygen(float amount) => currentOxygen = Mathf.Min(maxOxygen, currentOxygen + amount);
    public void RestoreSaturation(float amount) => currentSaturation = Mathf.Min(maxSaturation, currentSaturation + amount);
    public void ReduceToxicity(float amount) => currentToxicity = Mathf.Max(0f, currentToxicity - amount);

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        if (currentHealth <= 0f) Die();
    }
}
