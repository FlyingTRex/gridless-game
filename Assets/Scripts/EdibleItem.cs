using UnityEngine;

[CreateAssetMenu(menuName = "Gridless/Edible Item", fileName = "NewEdible")]
public class EdibleItem : ScriptableObject
{
    public ItemDefinition item;
    public int consumeCount = 1;
    public VitalType vital;
    public float restoreAmount = 20f;
    public string verb = "Eat";

    // Optional — for a consumable that leaves behind a reusable container
    // instead of being consumed entirely. Null means fully consumed.
    public ItemDefinition returnItem;
}
