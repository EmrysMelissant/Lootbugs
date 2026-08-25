using UnityEngine;

public class Upgrade : MonoBehaviour, IInteractable
{
    [SerializeField] private string interactText = "F";
    [SerializeField] public UpgradeType upgradeType;
    private float price = 20f;
    public string InteractionText => $"Upgrade {upgradeType} (${price})";
    public enum UpgradeType
    {
        Health,
        Speed,
        Stamina,
        StaminaRegen,
        Strength,
        Gains,
        Heal
    }

    public void Interact(GameObject interactor)
    {
        if (interactor.TryGetComponent(out NewClimbing player))
        {
            if (player.Money >= price)
            {
                player.Money -= price;
                ApplyUpgrade(player);
            }
            else
            {
                Debug.Log("Not enough money to purchase upgrade.");
            }
        }
    }

    private void ApplyUpgrade(NewClimbing player)
    {
        switch (upgradeType)
        {
            case UpgradeType.Health:
                player.MaxHealth += 10f;
                break;
            case UpgradeType.Speed:
                player.walkSpeed += 1f;
                break;
            case UpgradeType.Stamina:
                player.maxStamina += 10f;
                break;
            case UpgradeType.StaminaRegen:
                player.staminaRegenRate += 0.2f;
                break;
            case UpgradeType.Strength:
                player.Strength += 1f;
                break;
            case UpgradeType.Gains:
                player.gainMultiplier += 0.1f;
                break;
            case UpgradeType.Heal:
                if (player.Health < player.MaxHealth)
                {
                    player.Health += 20f;
                }

                break;
        }
        price += price * 0.2f;
        price = Mathf.Round(price * 100f) / 100f;
    }
}



