using UnityEngine;
using UnityEngine.SceneManagement;

public class PersistentPlayer : MonoBehaviour
{
    private static PersistentPlayer instance;

    [Header("Spawn Settings")]
    [Tooltip("The tag assigned to the SpawnPoint GameObject in each scene.")]
    [SerializeField] private string spawnPointTag = "SpawnPoint";

    private void Awake()
    {
        // Singleton pattern: ensure only one Player survives scene transitions
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
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