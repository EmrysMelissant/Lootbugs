using Unity.Netcode;
using UnityEngine;

public class FlashLight : NetworkBehaviour
{
    [SerializeField] public GameObject flashLight;
    [SerializeField] public KeyCode lightKey = KeyCode.F;

    private readonly NetworkVariable<bool> isLightOn = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        isLightOn.OnValueChanged += OnLightStateChanged;
        ApplyLightState(isLightOn.Value);
    }

    public override void OnNetworkDespawn()
    {
        isLightOn.OnValueChanged -= OnLightStateChanged;
        base.OnNetworkDespawn();
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (Input.GetKeyDown(lightKey))
        {
            ToggleFlashlight();
        }
    }

    public void ToggleFlashlight()
    {
        if (!IsOwner) return;
        isLightOn.Value = !isLightOn.Value;
    }

    private void OnLightStateChanged(bool previousValue, bool newValue)
    {
        ApplyLightState(newValue);
    }

    private void ApplyLightState(bool state)
    {
        if (flashLight != null)
        {
            flashLight.SetActive(state);
        }
    }
}
