using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    PlayerMovement movement;
    CameraController CamController;

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

    bool isDead = false;

    void Start()
    {
        isDead = false;
        movement = GetComponent<PlayerMovement>();
        CamController = GetComponentInChildren<CameraController>();
    }

    void Update()
    {
        if (isDead) return;
        float dt = Time.deltaTime;

        // Oxygen and toxicity react to environment
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

        // Saturation always drains
        currentSaturation = Mathf.Max(0f, currentSaturation - saturationDepletionRate * dt);

        // Damage from empty/maxed stats — all hit health
        if (currentOxygen <= 0f) currentHealth -= suffocationDamage * dt;
        if (currentSaturation <= 0f) currentHealth -= starvationDamage * dt;
        if (currentToxicity >= maxToxicity) currentHealth = 0f;  // insta-kill

        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        if (currentHealth <= 0f) Die();
    }   // <-- Update closes HERE, before the other methods

    public void Die()
    {
        isDead = true;
        movement.enabled = false;
        CamController.enabled = false;
        Debug.Log("Player died");
    }

    // --- Public methods for pickups, medkits, food, oxygen tanks ---
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