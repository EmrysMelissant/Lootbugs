using Unity.Netcode;
using UnityEngine;

public class Item : NetworkBehaviour
{
    public enum Rarity { Common, Uncommon, Rare, Epic, Legendary }

    [Header("Network Data")]
    public NetworkVariable<Rarity> NetRarity = new NetworkVariable<Rarity>(Rarity.Common);
    public NetworkVariable<int> NetPoints = new NetworkVariable<int>(0);
    public NetworkVariable<float> NetHeavy = new NetworkVariable<float>(0f);

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            DetermineRarity();
        }
    }

    private void DetermineRarity()
    {
        int[] weights = { 60, 25, 10, 4, 1 };
        int totalWeight = 0;
        foreach (int w in weights) totalWeight += w;

        int roll = Random.Range(0, totalWeight);
        int cursor = 0;

        for (int i = 0; i < weights.Length; i++)
        {
            cursor += weights[i];
            if (roll < cursor)
            {
                Rarity selected = (Rarity)i;
                NetRarity.Value = selected;
                NetPoints.Value = CalculatePoints(selected);
                NetHeavy.Value = CalculateHeavy(selected);
                break;
            }
        }
    }

    private int CalculatePoints(Rarity rarity)
    {
        return rarity switch
        {
            Rarity.Common => 10,
            Rarity.Uncommon => 25,
            Rarity.Rare => 50,
            Rarity.Epic => 100,
            Rarity.Legendary => 500,
            _ => 0
        };
    }

    private float CalculateHeavy(Rarity rarity)
    {
        return rarity switch
        {
            Rarity.Common => 0.5f,
            Rarity.Uncommon => 1f,
            Rarity.Rare => 1.5f,
            Rarity.Epic => 2f,
            Rarity.Legendary => 2.5f,
            _ => 0f
        };
    }
}