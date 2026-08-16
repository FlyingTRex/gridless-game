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

    // Health/Stamina ceilings, added 2026-08-14 for Constitution
    // (DEXTERITY_CONSTITUTION_PLANNING.md) — unlike maxWill's discrete
    // per-event GrowMaxWill increments, these are pushed every frame by
    // PlayerConstitution as a pure function of the current Constitution
    // value (same "continuously recomputed" shape as PlayerEncumbrance
    // .Capacity), via SetMaxHealth/SetMaxStamina below rather than a
    // Grow-style additive call.
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float maxStamina = 100f;

    // Slowed 3x (2026-08-16, Ben's call, live-testing) -- the original
    // 20min/12min-to-empty rates meant eating/drinking almost constantly
    // (a Meal-tier food's 40 Hunger only bought back 8 real minutes).
    // 60min/36min keeps the same relative Hunger:Thirst pace (~1.67x)
    // while turning it into an occasional task instead of a constant one.
    [SerializeField] private float hungerDrainPerSecond = 100f / (60f * 60f);
    [SerializeField] private float thirstDrainPerSecond = 100f / (36f * 60f);
    [SerializeField] private float starvationDamagePerSecond = 2f;
    // Slowed 2026-08-10 (Ben's call, live-testing Combat): the old 1/s
    // rate healed a full 0-100 in under 2 minutes with zero player
    // action, which made taking real damage (the new Wolf bite) feel
    // consequence-free. 0.05/s is a genuine slow crawl — ~33 minutes for
    // a full passive heal — deliberately punishing until a real first-aid
    // system exists to counter it (Basic Combat's other still-open half).
    [SerializeField] private float healthRegenPerSecond = 0.05f;
    [SerializeField] private float staminaDrainPerSecond = 10f;
    [SerializeField] private float walkStaminaDrainPerSecond = 2f;
    [SerializeField] private float staminaRegenPerSecond = 6f;
    [SerializeField] private float bodyTemperatureNeutral = 50f;
    [SerializeField] private float bodyTemperatureDriftPerSecond = 2f;
    [SerializeField] private float overdrinkSicknessThreshold = 125f;
    [SerializeField] private float overdrinkSicknessDamagePerSecond = 5f;
    [SerializeField] private float overdrinkRecoveryThreshold = 50f;
    [SerializeField] private float overdrinkThirstRecoveryPerSecond = 10f;
    // 1 point every 5 seconds, per Ben's call.
    [SerializeField] private float willRegenPerSecond = 0.2f;

    // Heal-over-time state (Restoration's Heal Self wish, 2026-08-08) — a
    // flat rate computed once at StartHealOverTime and ticked down each
    // frame, same shape as bodyTemperature's drift. Re-casting while one is
    // already active replaces it outright (new rate/duration) rather than
    // stacking or extending — simplest behavior absent any spec otherwise.
    private float healOverTimeRatePerSecond;
    private float healOverTimeSecondsLeft;

    private bool isOverdrunkSick;

    public float Health => health;
    public float Hunger => hunger;
    public float Thirst => thirst;
    public float Stamina => stamina;
    public float BodyTemperature => bodyTemperature;
    public float Will => will;
    public float MaxWill => maxWill;
    public float MaxHealth => maxHealth;
    public float MaxStamina => maxStamina;
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
            health = Mathf.Min(maxHealth, health + healthRegenPerSecond * dt);

        if (IsSprinting)
            stamina = Mathf.Max(0f, stamina - staminaDrainPerSecond * dt);
        else if (IsWalking)
            stamina = Mathf.Max(0f, stamina - walkStaminaDrainPerSecond * dt);
        else if (CanRegenStamina)
            stamina = Mathf.Min(maxStamina, stamina + staminaRegenPerSecond * dt);

        bodyTemperature = Mathf.MoveTowards(bodyTemperature, bodyTemperatureNeutral,
            bodyTemperatureDriftPerSecond * dt);

        will = Mathf.Min(maxWill, will + willRegenPerSecond * dt);

        if (healOverTimeSecondsLeft > 0f)
        {
            health = Mathf.Min(maxHealth, health + healOverTimeRatePerSecond * dt);
            healOverTimeSecondsLeft -= dt;
        }
    }

    public void ConsumeStamina(float amount)
    {
        stamina = Mathf.Clamp(stamina - amount, 0f, maxStamina);
    }

    // Called every frame by PlayerConstitution — Max Health/Max Stamina are
    // a pure function of the current Constitution value, recomputed
    // continuously rather than accumulated, so this replaces the value
    // outright rather than adding to it. Clamps current health/stamina down
    // too, defensively — never actually triggers in practice since
    // Constitution has no decay mechanism, so these caps only ever rise.
    public void SetMaxHealth(float value)
    {
        maxHealth = value;
        health = Mathf.Min(health, maxHealth);
    }

    public void SetMaxStamina(float value)
    {
        maxStamina = value;
        stamina = Mathf.Min(stamina, maxStamina);
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

    // Called by PlayerInteraction on a successful Heal Self wish. Duration
    // <= 0 just applies the amount instantly instead of dividing by zero.
    public void StartHealOverTime(float amount, float duration)
    {
        if (duration <= 0f)
        {
            Restore(VitalType.Health, amount);
            return;
        }

        healOverTimeRatePerSecond = amount / duration;
        healOverTimeSecondsLeft = duration;
    }

    // Called every frame by a nearby lit heat source (Campfire, 2026-08-12
    // — its first real gameplay effect, previously 100% decorative) —
    // nudges bodyTemperature toward target at ratePerSecond, competing
    // each frame against the passive drift-to-neutral in Update() above.
    // Multiple heat sources in range each call this independently; no
    // special stacking/aggregation, whichever pulls hardest dominates over
    // time the same way any two competing MoveTowards calls would.
    public void WarmNear(float target, float ratePerSecond)
    {
        bodyTemperature = Mathf.MoveTowards(bodyTemperature, target, ratePerSecond * Time.deltaTime);
    }

    // Direct health loss (e.g. a spectacular crafting failure) — distinct
    // from the passive starvation/overdrink damage in Update(), which
    // that own logic applies via a lower-bound clamp; this uses the same
    // clamp so callers can't push health negative.
    public void Damage(float amount)
    {
        health = Mathf.Max(0f, health - amount);
    }

    // Written by SaveManager on load — sets every vital directly from save
    // data rather than through Restore(VitalType, amount)'s clamped-add
    // semantics, since a save file's stored value is already the correct
    // final number, not a delta to add.
    public void RestoreVitals(float health, float hunger, float thirst, float stamina,
        float bodyTemperature, float will, float maxWill, float maxHealth, float maxStamina)
    {
        this.health = health;
        this.hunger = hunger;
        this.thirst = thirst;
        this.stamina = stamina;
        this.bodyTemperature = bodyTemperature;
        this.will = will;
        this.maxWill = maxWill;
        this.maxHealth = maxHealth;
        this.maxStamina = maxStamina;
    }

    public void Restore(VitalType vital, float amount)
    {
        switch (vital)
        {
            case VitalType.Health: health = Mathf.Min(maxHealth, health + amount); break;
            case VitalType.Hunger: hunger = Mathf.Min(100f, hunger + amount); break;
            // 125 is the safe ceiling; the extra headroom to 150 is what
            // lets a drink past 125 register as overdrinking at all —
            // without it, thirst could never actually cross
            // overdrinkSicknessThreshold through drinking.
            case VitalType.Thirst: thirst = Mathf.Min(150f, thirst + amount); break;
            case VitalType.Stamina: stamina = Mathf.Min(maxStamina, stamina + amount); break;
        }
    }
}
