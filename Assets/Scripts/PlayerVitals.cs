using UnityEngine;

public enum VitalType
{
    Health,
    Hunger,
    Thirst,
    Stamina
}

[DisallowMultipleComponent]
public class PlayerVitals : MonoBehaviour
{
    // Stamina must be at or above this percentage to get the sprint speed
    // bonus at all — below it, FirstPersonController caps movement to
    // normal walk speed regardless of whether sprint is held.
    public const float SprintStaminaThreshold = 85f;

    [SerializeField] private float health = 100f;
    [SerializeField] private float hunger = 100f;
    [SerializeField] private float thirst = 100f;
    [SerializeField] private float stamina = 100f;
    [SerializeField] private float bodyTemperature = 50f;

    [SerializeField] private float hungerDrainPerSecond = 100f / (20f * 60f);
    [SerializeField] private float thirstDrainPerSecond = 100f / (12f * 60f);
    [SerializeField] private float starvationDamagePerSecond = 2f;
    [SerializeField] private float healthRegenPerSecond = 1f;
    [SerializeField] private float staminaDrainPerSecond = 10f;
    [SerializeField] private float walkStaminaDrainPerSecond = 2f;
    [SerializeField] private float staminaRegenPerSecond = 6f;
    [SerializeField] private float bodyTemperatureNeutral = 50f;
    [SerializeField] private float bodyTemperatureDriftPerSecond = 2f;

    public float Health => health;
    public float Hunger => hunger;
    public float Thirst => thirst;
    public float Stamina => stamina;
    public float BodyTemperature => bodyTemperature;

    public bool IsSprinting { get; set; }

    // Set every frame by FirstPersonController — true while moving
    // normally (standing, not getting the sprint bonus), including
    // attempted sprints below SprintStaminaThreshold. Drains stamina at a
    // slower rate than sprinting.
    public bool IsWalking { get; set; }

    // Set every frame by FirstPersonController based on movement/stance —
    // stamina only climbs back up while stopped, kneeling, crawling, or
    // prone.
    public bool CanRegenStamina { get; set; } = true;

    public bool CanSprint => stamina >= SprintStaminaThreshold;

    private void Update()
    {
        float dt = Time.deltaTime;

        hunger = Mathf.Max(0f, hunger - hungerDrainPerSecond * dt);
        thirst = Mathf.Max(0f, thirst - thirstDrainPerSecond * dt);

        if (hunger <= 0f || thirst <= 0f)
            health = Mathf.Max(0f, health - starvationDamagePerSecond * dt);
        else if (hunger > 50f && thirst > 50f)
            health = Mathf.Min(100f, health + healthRegenPerSecond * dt);

        if (IsSprinting)
            stamina = Mathf.Max(0f, stamina - staminaDrainPerSecond * dt);
        else if (IsWalking)
            stamina = Mathf.Max(0f, stamina - walkStaminaDrainPerSecond * dt);
        else if (CanRegenStamina)
            stamina = Mathf.Min(100f, stamina + staminaRegenPerSecond * dt);

        bodyTemperature = Mathf.MoveTowards(bodyTemperature, bodyTemperatureNeutral,
            bodyTemperatureDriftPerSecond * dt);
    }

    public void ConsumeStamina(float amount)
    {
        stamina = Mathf.Clamp(stamina - amount, 0f, 100f);
    }

    public void Restore(VitalType vital, float amount)
    {
        switch (vital)
        {
            case VitalType.Health: health = Mathf.Min(100f, health + amount); break;
            case VitalType.Hunger: hunger = Mathf.Min(100f, hunger + amount); break;
            case VitalType.Thirst: thirst = Mathf.Min(100f, thirst + amount); break;
            case VitalType.Stamina: stamina = Mathf.Min(100f, stamina + amount); break;
        }
    }
}
