using UnityEngine;

[CreateAssetMenu(menuName = "Gridless/Edible Item", fileName = "NewEdible")]
public class EdibleItem : ScriptableObject
{
    public ItemDefinition item;
    public int consumeCount = 1;

    // Every EdibleItem restores Hunger unconditionally, via this tier
    // (see FoodTier.cs) rather than through vital/restoreAmount below —
    // keeps every food item's baseline nutrition on one shared, tunable
    // scale instead of a hand-picked number per item.
    public FoodTier foodTier = FoodTier.Snack;

    // A secondary effect beyond the Hunger restore above — e.g. MRE
    // Ration's Health boost. Zero restoreAmount (Berry's case) means no
    // secondary effect, just the FoodTier Hunger restore.
    public VitalType vital;
    public float restoreAmount = 0f;
    public string verb = "Eat";

    // Optional — a Health heal-over-time component layered on top of the
    // instant restoreAmount above (e.g. MRE Ration: 25 instant + 15 more
    // over 60s). Zero (the default) means no over-time component, matching
    // every existing edible's plain instant restore. Reuses
    // PlayerVitals.StartHealOverTime, the same mechanism Medicine/Heal-Self
    // magic already use — note it overwrites any other in-progress
    // heal-over-time rather than stacking, same as those callers.
    public float healOverTimeAmount = 0f;
    public float healOverTimeDuration = 0f;

    // Optional — for a consumable that leaves behind a reusable container
    // instead of being consumed entirely. Null means fully consumed.
    public ItemDefinition returnItem;
}
