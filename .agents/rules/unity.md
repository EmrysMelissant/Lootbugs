# Unity Development Rules for AI Agents

These rules apply across the Unity codebase:

1. **Unity 6 APIs**:
   - Use `rb.linearVelocity` (not `rb.velocity`).
   - Use `rb.linearDamping` and `rb.angularDamping` (not `rb.drag` / `rb.angularDrag`).
   - Use `FindAnyObjectByType<T>()` / `FindFirstObjectByType<T>()` instead of `FindObjectOfType<T>()`.
   - Use `FindObjectsByType<T>(FindObjectsSortMode.None)` instead of `FindObjectsOfType<T>()`.

2. **Null Checks on Unity Objects**:
   - Never use `?.` or `??` on `UnityEngine.Object` subclasses (`MonoBehaviour`, `GameObject`, `Component`, `ScriptableObject`).
   - Use explicit `if (obj != null)` or `if (obj == null)`.

3. **Lifecycle & Execution Flow**:
   - `Awake()`: Cache internal `GetComponent<T>()` references and setup.
   - `FixedUpdate()`: Rigidbody physics, forces, velocity updates (using `Time.fixedDeltaTime`).
   - `Update()`: Inputs, non-physics movement, timers (using `Time.deltaTime`).
   - `LateUpdate()`: Camera follow and transform syncing.
   - Never perform `GetComponent<T>()`, `Find*()`, `Camera.main`, or LINQ allocations in `Update()` / `FixedUpdate()`.

4. **Netcode for GameObjects (NGO)**:
   - Inherit from `NetworkBehaviour` when networking is required.
   - Check `if (!IsOwner) return;` for player input handling.
   - Check `if (!IsServer) return;` for authoritative game state changes.
   - Name RPCs with suffixes: `ServerRpc` and `ClientRpc`.
   - Setup `NetworkVariable` callbacks in `OnNetworkSpawn()` and remove in `OnNetworkDespawn()`.

5. **Serialization & Encapsulation**:
   - Use `[SerializeField] private` fields instead of `public` fields for inspector variables.
   - Expose public getter properties (e.g. `public float Speed => speed;`).
   - Script filenames MUST match the class name inside.
   - Never delete or corrupt `.meta` files.
