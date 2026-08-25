using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class PersistentPlayer : NetworkBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("The tag assigned to the SpawnPoint GameObject in each scene.")]
    [SerializeField] private string spawnPointTag = "SpawnPoint";

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        TeleportToSpawnPoint();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TeleportToSpawnPoint();
    }

    private void TeleportToSpawnPoint()
    {
        // Only teleport the local player owner
        if (!IsOwner) return;

        GameObject spawnPointObj = GameObject.FindWithTag(spawnPointTag);

        if (spawnPointObj != null)
        {
            // Temporarily disable CharacterController or Rigidbody to prevent position resets
            if (TryGetComponent<CharacterController>(out CharacterController cc))
            {
                cc.enabled = false;
                transform.position = spawnPointObj.transform.position;
                transform.rotation = spawnPointObj.transform.rotation;
                cc.enabled = true;
            }
            else if (TryGetComponent<Rigidbody>(out Rigidbody rb))
            {
                rb.position = spawnPointObj.transform.position;
                rb.rotation = spawnPointObj.transform.rotation;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            else
            {
                transform.position = spawnPointObj.transform.position;
                transform.rotation = spawnPointObj.transform.rotation;
            }
        }
        else
        {
            Debug.LogWarning($"[PersistentPlayer] No GameObject with tag '{spawnPointTag}' found in scene '{SceneManager.GetActiveScene().name}'.");
        }
    }
}