using UnityEngine;

public class Upgrade : MonoBehaviour, IInteractable
{
    [SerializeField] private string interactText = "F";
    [SerializeField] public UpgradeType upgradeType;
    private float price = 20f;
    public string InteractionText => $"Upgrade {upgradeType} (${price})";
    public AudioSource audioSource;
    public AudioClip audio;
    [SerializeField, Range(0f, 1f)] private float soundVolume = 0.75f;
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
        if (interactor.TryGetComponent(out PlayerController player))
        {
            if (player.Money >= price)
            {
                player.Money -= price;
                ApplyUpgrade(player);
                audioSource.PlayOneShot(audio, soundVolume);
            }
            else
            {
                Debug.Log("Not enough money to purchase upgrade.");
            }
        }
    }

    private void ApplyUpgrade(PlayerController player)
    {
        switch (upgradeType)
        {
            case UpgradeType.Health:
                player.MaxHealth += 10f;
                player.UpdateHealthUI();
                break;
            case UpgradeType.Speed:
                player.walkSpeed += 1f;
                break;
            case UpgradeType.Stamina:
                player.maxStamina += 10f;
                player.UpdateStaminaUI();
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
                    player.Heal(20f);
                }
                break;
        }
        price += price * 0.2f;
        price = Mathf.Round(price * 100f) / 100f;
    }
}



