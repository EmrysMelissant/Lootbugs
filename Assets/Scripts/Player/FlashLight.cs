using UnityEngine;
using Unity.Netcode;
public class FlashLight : NetworkBehaviour
{
    public GameObject flashLight;
    public KeyCode lightKey = KeyCode.F;
    void Start()
    {
        if (flashLight != null)
        {
            flashLight.SetActive(false);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsOwner) return;

        bool toggleInput = Input.GetKeyDown(lightKey);
        if (MobileInputManager.Instance != null && MobileInputManager.Instance.ConsumeFlashlight())
        {
            toggleInput = true;
        }

        if (toggleInput)
        {
            ToggleLight();
        }
    }

    public void ToggleLight()
    {
        if (flashLight != null)
        {
            flashLight.SetActive(!flashLight.activeSelf);
        }
    }
}

