using Unity.Netcode;
using UnityEngine;
using TMPro;

public class PlayerHUDInteraction : NetworkBehaviour
{
    [Header("Detection")]
    public float maxDistance = 2f;
    public LayerMask interactableLayer;

    [Header("HUD References")]
    public GameObject hudPanel;       
    public TextMeshProUGUI hudText;   

    private Camera _cam;
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsOwner)
        {
            HideHUD();
            if (hudPanel != null)
            {
                hudPanel.SetActive(false);
            }
            this.enabled = false;
            return;
        }

        HideHUD();
    }

    private void Start()
    {
        if (!IsOwner) return;

        HideHUD();
        FindCamera();
    }

    void Update()
    {
        // 1. Only run for the local player
        if (!IsOwner) return;

        // 2. Ensure camera is found
        if (_cam == null)
        {
            FindCamera();
            if (_cam == null) return; 
        }

        HandleDetection();
    }

    private void FindCamera()
    {
        _cam = GetComponentInChildren<Camera>();
        if (_cam == null) _cam = Camera.main;
    }

    void HandleDetection()
    {
        if (_cam == null) return;

        Ray ray = _cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, maxDistance, interactableLayer))
        {
            Item item = hit.collider.GetComponentInParent<Item>();

            if (item != null)
            {
                ShowHUD(item.NetRarity.Value.ToString());
                return; 
            }
        }
        HideHUD();
    }

    void ShowHUD(string rarityName)
    {
        if (hudPanel != null && !hudPanel.activeSelf) 
            hudPanel.SetActive(true);
            
        if (hudText != null) 
            hudText.text = rarityName;
    }

    void HideHUD()
    {
        // Only call SetActive if it's currently on to save performance
        if (hudPanel != null && hudPanel.activeSelf) 
        {
            hudPanel.SetActive(false);
        }

        if (hudText != null)
        {
            hudText.text = "";
        }
    }
}
