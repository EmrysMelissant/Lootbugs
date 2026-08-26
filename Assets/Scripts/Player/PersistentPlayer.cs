using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class PersistentPlayer : NetworkBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("The tag assigned to the default SpawnPoint GameObject in each scene.")]
    [SerializeField] private string spawnPointTag = "SpawnPoint";

    [Tooltip("The GameObject name of the default SpawnPoint in each scene.")]
    [SerializeField] private string spawnPointName = "SpawnPoint";

    [Tooltip("The tag assigned to the RespawnPoint GameObject in the hub scene.")]
    [SerializeField] private string respawnPointTag = "RespawnPoint";

    [Tooltip("The GameObject name of the RespawnPoint in the hub scene.")]
    [SerializeField] private string respawnPointName = "RespawnPoint";

    [Header("Scene Settings")]
    [Tooltip("The name of the Hub scene.")]
    [SerializeField] private string hubSceneName = "MainHub";

    [Header("Gravity & Teleport Settings")]
    [Tooltip("Whether to disable Rigidbody and movement gravity until spawning and teleport are finished.")]
    [SerializeField] private bool disableGravityUntilSpawned = true;

    [Tooltip("Buffer duration in seconds to keep gravity disabled after teleport to let physics and geometry settle.")]
    [SerializeField] private float gravityDisableDuration = 0.2f;

    private bool hasCompletedInitialSpawn = false;
    private Coroutine activeTeleportCoroutine;

    private void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        if (disableGravityUntilSpawned)
        {
            DisablePlayerGravity();
        }
    }

    public override void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        base.OnDestroy();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (disableGravityUntilSpawned)
        {
            DisablePlayerGravity();
        }
        TeleportToTargetLocation(SceneManager.GetActiveScene(), isCreationOrConnection: true);
        hasCompletedInitialSpawn = true;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (disableGravityUntilSpawned)
        {
            DisablePlayerGravity();
        }

        bool isInitial = !hasCompletedInitialSpawn;
        TeleportToTargetLocation(scene, isCreationOrConnection: isInitial);
        if (isInitial && IsSpawned)
        {
            hasCompletedInitialSpawn = true;
        }
    }

    private void TeleportToTargetLocation(Scene scene, bool isCreationOrConnection = false)
    {
        // Only teleport the local player owner
        if (!IsOwner) return;

        if (disableGravityUntilSpawned)
        {
            DisablePlayerGravity();
        }

        bool isHub = IsHubScene(scene);
        bool isDead = IsPlayerDead();

        GameObject targetPointObj = null;

        // Route to RespawnPoint on initial creation/connection OR when dead player enters the Hub
        if (isCreationOrConnection || (isHub && isDead))
        {
            targetPointObj = FindRespawnPoint();
            if (targetPointObj == null)
            {
                Debug.LogWarning($"[PersistentPlayer] RespawnPoint not found in '{scene.name}'. Falling back to SpawnPoint.");
                targetPointObj = FindSpawnPoint();
            }

            if (targetPointObj != null)
            {
                if (isDead)
                {
                    ReviveDeadPlayerInHub(targetPointObj.transform.position);
                }
                ExecuteTeleport(targetPointObj.transform);
            }
        }
        else
        {
            targetPointObj = FindSpawnPoint();
            if (targetPointObj != null)
            {
                ExecuteTeleport(targetPointObj.transform);
            }
            else
            {
                Debug.LogWarning($"[PersistentPlayer] No GameObject with tag '{spawnPointTag}' or name '{spawnPointName}' found in scene '{scene.name}'.");
            }
        }
    }

    private void ExecuteTeleport(Transform targetTransform)
    {
        if (targetTransform == null) return;

        // Ensure gravity is disabled while positioning
        if (disableGravityUntilSpawned)
        {
            DisablePlayerGravity();
        }

        // Temporarily disable CharacterController or Rigidbody to prevent position resets and physics glitches
        if (TryGetComponent<CharacterController>(out CharacterController cc))
        {
            cc.enabled = false;
            transform.position = targetTransform.position;
            transform.rotation = targetTransform.rotation;
            cc.enabled = true;
        }
        else if (TryGetComponent<Rigidbody>(out Rigidbody rb))
        {
            rb.position = targetTransform.position;
            rb.rotation = targetTransform.rotation;
            transform.position = targetTransform.position;
            transform.rotation = targetTransform.rotation;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        else
        {
            transform.position = targetTransform.position;
            transform.rotation = targetTransform.rotation;
        }

        if (gameObject.activeInHierarchy)
        {
            if (activeTeleportCoroutine != null)
            {
                StopCoroutine(activeTeleportCoroutine);
            }
            activeTeleportCoroutine = StartCoroutine(CompleteTeleportRoutine());
        }
        else
        {
            EnablePlayerGravity();
        }
    }

    private IEnumerator CompleteTeleportRoutine()
    {
        if (disableGravityUntilSpawned)
        {
            DisablePlayerGravity();
        }

        if (gravityDisableDuration > 0f)
        {
            yield return new WaitForSeconds(gravityDisableDuration);
        }
        else
        {
            yield return new WaitForFixedUpdate();
        }

        // Spawning and teleportation are finished -> re-enable gravity
        EnablePlayerGravity();
        activeTeleportCoroutine = null;
    }

    public void DisablePlayerGravity()
    {
        if (TryGetComponent<Rigidbody>(out Rigidbody rb))
        {
            rb.useGravity = false;
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (TryGetComponent<NewClimbing>(out NewClimbing climbing))
        {
            climbing.SetGravityEnabled(false);
        }
    }

    public void EnablePlayerGravity()
    {
        if (TryGetComponent<Rigidbody>(out Rigidbody rb))
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            // If not using NewClimbing custom gravity calculation, restore native useGravity
            if (!TryGetComponent<NewClimbing>(out _))
            {
                rb.useGravity = true;
            }
        }

        if (TryGetComponent<NewClimbing>(out NewClimbing climbing))
        {
            climbing.SetGravityEnabled(true);
        }
    }

    private void ReviveDeadPlayerInHub(Vector3 respawnPosition)
    {
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        if (TryGetComponent<NewClimbing>(out NewClimbing climbing))
        {
            climbing.OnRevived(respawnPosition);
            climbing.Health = climbing.MaxHealth;
            climbing.stamina = climbing.maxStamina;
        }
    }

    private bool IsPlayerDead()
    {
        if (TryGetComponent<NewClimbing>(out NewClimbing climbing))
        {
            return !climbing.IsAlive || climbing.Health <= 0f;
        }
        return false;
    }

    private bool IsHubScene(Scene scene)
    {
        if (string.IsNullOrEmpty(scene.name))
        {
            scene = SceneManager.GetActiveScene();
        }

        if (string.IsNullOrEmpty(scene.name)) return false;

        return scene.name.Equals(hubSceneName, StringComparison.OrdinalIgnoreCase)
            || scene.name.IndexOf("Hub", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private GameObject FindSpawnPoint()
    {
        // 1. Check tagged objects
        try
        {
            GameObject[] tagged = GameObject.FindGameObjectsWithTag(spawnPointTag);
            if (tagged != null && tagged.Length > 0)
            {
                // If multiple objects share the spawn tag, prefer the one NOT named Respawn
                foreach (GameObject obj in tagged)
                {
                    if (obj != null && obj.name.IndexOf("Respawn", StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        return obj;
                    }
                }
                return tagged[0];
            }
        }
        catch (UnityException)
        {
            // Tag might not be registered
        }

        // 2. Search by GameObject name
        GameObject namedObj = GameObject.Find(spawnPointName);
        if (namedObj != null)
        {
            return namedObj;
        }

        return null;
    }

    private GameObject FindRespawnPoint()
    {
        // 1. Check respawn tagged objects
        try
        {
            GameObject[] tagged = GameObject.FindGameObjectsWithTag(respawnPointTag);
            if (tagged != null && tagged.Length > 0)
            {
                return tagged[0];
            }
        }
        catch (UnityException)
        {
            // Tag might not be registered
        }

        // 2. Search by GameObject name
        GameObject namedObj = GameObject.Find(respawnPointName);
        if (namedObj != null)
        {
            return namedObj;
        }

        // 3. Search among spawnPointTag objects for any named "Respawn"
        try
        {
            GameObject[] spawnTagged = GameObject.FindGameObjectsWithTag(spawnPointTag);
            if (spawnTagged != null)
            {
                foreach (GameObject obj in spawnTagged)
                {
                    if (obj != null && obj.name.IndexOf("Respawn", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return obj;
                    }
                }
            }
        }
        catch (UnityException)
        {
        }

        return null;
    }
}