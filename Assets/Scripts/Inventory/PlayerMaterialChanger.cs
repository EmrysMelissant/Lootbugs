using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PlayerMaterialChanger : NetworkBehaviour, IInteractable
{
    [Header("Interaction Settings")]
    [Tooltip("Prompt displayed on screen when looking at this object.")]
    [SerializeField] private string interactionPrompt = "Change Material";

    [Header("Material Source")]
    [Tooltip("Optional: Explicit material to apply. If null, the material will be grabbed from this GameObject's Renderer.")]
    [SerializeField] private Material customMaterial;

    [Tooltip("Target renderer on this object to fetch the material from if customMaterial is not set. If null, uses GetComponent<Renderer>().")]
    [SerializeField] private Renderer sourceRenderer;

    [Header("Filtering Settings")]
    [Tooltip("If true, all MeshRenderers and SkinnedMeshRenderers on the interactor and its children will be changed.")]
    [SerializeField] private bool applyToAllChildRenderers = true;

    [Tooltip("Optional tag filter on child objects of the player to only modify matching renderers (leave empty to modify all).")]
    [SerializeField] private string targetChildTag = "";

    [Header("Feedback (Optional)")]
    [Tooltip("Audio clip played when player interacts.")]
    [SerializeField] private AudioClip interactionSound;

    [Tooltip("Particle effect played when player interacts.")]
    [SerializeField] private ParticleSystem interactionParticles;

    public string InteractionText => interactionPrompt;
    public Material CustomMaterial
    {
        get => customMaterial;
        set => customMaterial = value;
    }

    private void Awake()
    {
        if (sourceRenderer == null)
        {
            sourceRenderer = GetComponent<Renderer>();
            if (sourceRenderer == null)
            {
                sourceRenderer = GetComponentInChildren<Renderer>();
            }
        }
    }

    public void Interact(GameObject interactor)
    {
        if (interactor == null) return;

        Material matToApply = GetSourceMaterial();
        if (matToApply == null)
        {
            Debug.LogWarning($"[PlayerMaterialChanger] No material found to apply on {gameObject.name}!");
            return;
        }

        // Apply locally
        ApplyMaterialToPlayer(interactor, matToApply);

        // Play local feedback
        PlayFeedback();

        // Network synchronization if running inside a Netcode session
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && IsSpawned)
        {
            if (interactor.TryGetComponent<NetworkObject>(out NetworkObject netObj))
            {
                if (netObj != null)
                {
                    ChangeMaterialServerRpc(netObj.NetworkObjectId);
                }
            }
        }
    }

    public Material GetSourceMaterial()
    {
        if (customMaterial != null)
        {
            return customMaterial;
        }

        if (sourceRenderer != null && sourceRenderer.sharedMaterial != null)
        {
            return sourceRenderer.sharedMaterial;
        }

        return null;
    }

    private void ApplyMaterialToPlayer(GameObject player, Material mat)
    {
        if (player == null || mat == null) return;

        if (applyToAllChildRenderers)
        {
            Renderer[] renderers = player.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer rend = renderers[i];
                if (rend == null) continue;

                // Ignore UI, particles, lines, trails
                if (rend is ParticleSystemRenderer || rend is TrailRenderer || rend is LineRenderer)
                {
                    continue;
                }

                // Check tag filter if specified
                if (!string.IsNullOrEmpty(targetChildTag))
                {
                    try
                    {
                        if (!rend.CompareTag(targetChildTag)) continue;
                    }
                    catch (UnityException)
                    {
                        continue;
                    }
                }

                rend.material = mat;
            }
        }
        else
        {
            Renderer rend = player.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material = mat;
            }
        }
    }

    private void PlayFeedback()
    {
        if (interactionSound != null)
        {
            AudioSource.PlayClipAtPoint(interactionSound, transform.position);
        }

        if (interactionParticles != null)
        {
            interactionParticles.Play();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void ChangeMaterialServerRpc(ulong playerNetworkObjectId)
    {
        ChangeMaterialClientRpc(playerNetworkObjectId);
    }

    [ClientRpc]
    private void ChangeMaterialClientRpc(ulong playerNetworkObjectId)
    {
        if (NetworkManager.Singleton != null &&
            NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(playerNetworkObjectId, out NetworkObject targetNetObj))
        {
            if (targetNetObj != null && targetNetObj.gameObject != null)
            {
                Material mat = GetSourceMaterial();
                if (mat != null)
                {
                    ApplyMaterialToPlayer(targetNetObj.gameObject, mat);
                }
            }
        }
    }
}
