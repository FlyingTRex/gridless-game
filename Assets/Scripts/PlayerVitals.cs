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
    [SerializeField] private float staminaRegenPerSecond = 6f;
    [SerializeField] private float staminaExhaustionRecoveryThreshold = 25f;
    [SerializeField] private float bodyTemperatureNeutral = 50f;
    [SerializeField] private float bodyTemperatureDriftPerSecond = 2f;
    [SerializeField] private float overdrinkSicknessThreshold = 100f;
    [SerializeField] private float overdrinkSicknessDamagePerSecond = 5f;
    [SerializeField] private float overdrinkRecoveryThreshold = 50f;

    private bool isExhausted;
    private bool isOverdrunkSick;

    public float Health => health;
    public float Hunger => hunger;
    public float Thirst => thirst;
    public float Stamina => stamina;
    public float BodyTemperature => bodyTemperature;

    public bool IsSprinting { get; set; }

    public bool CanSprint => !isExhausted && stamina > 0f;

    private void Update()
    {
        float dt = Time.deltaTime;

        hunger = Mathf.Max(0f, hunger - hungerDrainPerSecond * dt);
        thirst = Mathf.Max(0f, thirst - thirstDrainPerSecond * dt);

        if (thirst > overdrinkSicknessThreshold)
        {
            isOverdrunkSick = true;
            health = Mathf.Max(0f, health - overdrinkSicknessDamagePerSecond * dt);
            if (thirst <= overdrinkRecoveryThreshold)
                isOverdrunkSick = false;
        }
        else if (hunger <= 0f || thirst <= 0f)
            health = Mathf.Max(0f, health - starvationDamagePerSecond * dt);
        else if (hunger > 50f && thirst > 50f)
            health = Mathf.Min(100f, health + healthRegenPerSecond * dt);

        stamina = IsSprinting
            ? Mathf.Max(0f, stamina - staminaDrainPerSecond * dt)
            : Mathf.Min(100f, stamina + staminaRegenPerSecond * dt);

        if (stamina <= 0f)
            isExhausted = true;
        else if (stamina >= staminaExhaustionRecoveryThreshold)
            isExhausted = false;

        bodyTemperature = Mathf.MoveTowards(bodyTemperature, bodyTemperatureNeutral,
            bodyTemperatureDriftPerSecond * dt);
    }

    public void ConsumeStamina(float amount)
    {
        stamina = Mathf.Clamp(stamina - amount, 0f, 100f);
        if (stamina <= 0f)
            isExhausted = true;
    }

    public void Restore(VitalType vital, float amount)
    {
        switch (vital)
        {
            case VitalType.Health: health = Mathf.Min(100f, health + amount); break;
            case VitalType.Hunger: hunger = Mathf.Min(100f, hunger + amount); break;
            case VitalType.Thirst: thirst = Mathf.Min(125f, thirst + amount); break;
            case VitalType.Stamina: stamina = Mathf.Min(100f, stamina + amount); break;
        }
    }
}
