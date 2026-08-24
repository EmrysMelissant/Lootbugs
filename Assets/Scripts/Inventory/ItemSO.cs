using UnityEngine;

[CreateAssetMenu(fileName = "Item", menuName = "NewItem")]
public class ItemSO : ScriptableObject
{
    public string itemName;
    public Sprite itemIcon;
    //public int itemID;
    public GameObject itemPrefab;
    public enum Rarity
    {
        common,
        uncommon,
        rare,
        epic,
        legendary,
    }

}
