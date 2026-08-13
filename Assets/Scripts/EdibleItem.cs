using UnityEngine;

[CreateAssetMenu(menuName = "Gridless/Edible Item", fileName = "NewEdible")]
public class EdibleItem : ScriptableObject
{
    public ItemDefinition item;
    public int consumeCount = 1;
    public VitalType vital;
    public float restoreAmount = 20f;
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
