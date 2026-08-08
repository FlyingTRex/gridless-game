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
    // Sixth vital, added 2026-08-08 for the Magic System (see
    // design-brief.md) — the resource wishes spend. Unlike the other five,
    // its ceiling isn't fixed: maxWill grows via GrowMaxWill (called by
    // PlayerMagic on a completed wish), so `will` is clamped against
    // maxWill, not a hardcoded 100. Regens passively like Stamina — no
    // "IsCasting" drain state, since Will is spent as one lump per
    // completed wish, not drained continuously.
    [SerializeField] private float will = 100f;
    [SerializeField] private float maxWill = 100f;

    [SerializeField] private float hungerDrainPerSecond = 100f / (20f * 60f);
    [SerializeField] private float thirstDrainPerSecond = 100f / (12f * 60f);
    [SerializeField] private float starvationDamagePerSecond = 2f;
    [SerializeField] private float healthRegenPerSecond = 1f;
    [SerializeField] private float staminaDrainPerSecond = 10f;
    [SerializeField] private float walkStaminaDrainPerSecond = 2f;
    [SerializeField] private float staminaRegenPerSecond = 6f;
    [SerializeField] private float bodyTemperatureNeutral = 50f;
    [SerializeField] private float bodyTemperatureDriftPerSecond = 2f;
    [SerializeField] private float overdrinkSicknessThreshold = 125f;
    [SerializeField] private float overdrinkSicknessDamagePerSecond = 5f;
    [SerializeField] private float overdrinkRecoveryThreshold = 50f;
    [SerializeField] private float overdrinkThirstRecoveryPerSecond = 10f;
    [SerializeField] private float willRegenPerSecond = 4f;

    private bool isOverdrunkSick;

    public float Health => health;
    public float Hunger => hunger;
    public float Thirst => thirst;
    public float Stamina => stamina;
    public float BodyTemperature => bodyTemperature;
    public float Will => will;
    public float MaxWill => maxWill;
    public bool IsOverdrunkSick => isOverdrunkSick;

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

        if (thirst > overdrinkSicknessThreshold)
            isOverdrunkSick = true;
        else if (thirst <= overdrinkRecoveryThreshold)
            isOverdrunkSick = false;

        if (isOverdrunkSick)
        {
            // Vomiting/sweating out the excess — much faster than the
            // ambient thirst drain above, so the player actually recovers
            // (crosses overdrinkRecoveryThreshold) instead of dying from
            // sickness damage before thirst ever comes back down.
            thirst = Mathf.Max(0f, thirst - overdrinkThirstRecoveryPerSecond * dt);
            health = Mathf.Max(0f, health - overdrinkSicknessDamagePerSecond * dt);
        }
        else if (hunger <= 0f || thirst <= 0f)
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

        will = Mathf.Min(maxWill, will + willRegenPerSecond * dt);
    }

    public void ConsumeStamina(float amount)
    {
        stamina = Mathf.Clamp(stamina - amount, 0f, 100f);
    }

    // False (no state change) if the player doesn't have enough Will —
    // callers (PlayerMagic) should check this before doing anything else a
    // failed wish shouldn't have side effects from.
    public bool ConsumeWill(float amount)
    {
        if (will < amount) return false;
        will -= amount;
        return true;
    }

    // Called by PlayerMagic on every successfully completed wish — Will's
    // ceiling grows through use, same skill-via-use spirit as everything
    // else, distinct from the other five vitals' fixed 100 cap. Tops up
    // current `will` by the same amount so growth reads as a real gain,
    // not just a cap raise that leaves you further from full.
    public void GrowMaxWill(float amount)
    {
        maxWill += amount;
        will += amount;
    }

    // Direct health loss (e.g. a spectacular crafting failure) — distinct
    // from the passive starvation/overdrink damage in Update(), which
    // that own logic applies via a lower-bound clamp; this uses the same
    // clamp so callers can't push health negative.
    public void Damage(float amount)
    {
        health = Mathf.Max(0f, health - amount);
    }

    public void Restore(VitalType vital, float amount)
    {
        switch (vital)
        {
            case VitalType.Health: health = Mathf.Min(100f, health + amount); break;
            case VitalType.Hunger: hunger = Mathf.Min(100f, hunger + amount); break;
            // 125 is the safe ceiling; the extra headroom to 150 is what
            // lets a drink past 125 register as overdrinking at all —
            // without it, thirst could never actually cross
            // overdrinkSicknessThreshold through drinking.
            case VitalType.Thirst: thirst = Mathf.Min(150f, thirst + amount); break;
            case VitalType.Stamina: stamina = Mathf.Min(100f, stamina + amount); break;
        }
    }
}
