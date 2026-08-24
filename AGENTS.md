# Unity AI Agent Guidelines & Project Instructions (Unity 6 / C#)

> **Role & Purpose**: This document serves as the master instructions and default system prompt for all AI coding agents working in this Unity project (`Lootbugs`). AI agents must strictly adhere to the guidelines, architectural patterns, safety rules, and coding conventions defined below.

---

## 1. Project Tech Stack & Environment

| Component | Specification / Package | Notes |
| :--- | :--- | :--- |
| **Engine Version** | **Unity 6** (`6000.3.8f1`+) | Use Unity 6 APIs; avoid deprecated APIs |
| **Scripting Backend / Language** | C# (Mono / IL2CPP), .NET Standard 2.1 | Modern C# idioms, zero-alloc hot paths |
| **Render Pipeline** | **HDRP** (`com.unity.render-pipelines.high-definition` 17.3+) | High-Fidelity shaders, Volume Profiles |
| **Multiplayer / Networking** | **Netcode for GameObjects (NGO)** (`2.11.2`) | Server-authoritative / Client-predicted architecture |
| **Multiplayer Services** | Unity Services Multiplayer (`2.2.2`), Vivox (`16.10.0`) | Lobby, Relay, Vivox voice integration |
| **AI & Navigation** | Unity AI Navigation (`com.unity.ai.navigation` 2.0.12) | NavMeshSurface, NavMeshAgent, NavMeshLink |
| **Input System** | **New Input System** (`com.unity.inputsystem` 1.18.0) | Prefer InputAction / PlayerInput over legacy `Input.*` |
| **UI** | TextMeshPro (`com.unity.modules.ui`, TMP) | Use `TMP_Text`, Canvas optimization |

---

## 2. Fundamental AI Safety Rules (Never Break)

1. **Meta Files (`.meta`) Integrity**:
   - Every file and folder under `Assets/` has an associated `.meta` file containing its unique GUID.
   - **NEVER** delete, corrupt, or leave `.meta` files orphaned.
   - When renaming or moving files, ensure `.meta` files are kept in sync.
   - When creating a new C# script, ensure Unity will generate or handle the corresponding `.meta` file without GUID collision.

2. **Class & File Naming Consistency**:
   - The C# script filename **MUST EXACTLY MATCH** the primary `MonoBehaviour` / `NetworkBehaviour` / `ScriptableObject` class name inside it (e.g., `ItemCollector.cs` -> `public class ItemCollector : MonoBehaviour`).
   - Mismatched class and file names cause Unity serialization failures and broken inspector scripts.

3. **No Phantom Assemblies**:
   - Check if an `Assembly Definition` (`.asmdef`) exists in the directory. If yes, respect internal visibility and ensure package references are properly wired.

---

## 3. Unity 6 & Modern C# Conventions

### 3.1 Unity 6 API Replacements
Always use modern Unity 6 APIs instead of deprecated legacy methods:

| Deprecated / Legacy API | Unity 6 Modern API | Reason |
| :--- | :--- | :--- |
| `rb.velocity` | `rb.linearVelocity` | Direct Unity 6 physics update |
| `rb.drag` | `rb.linearDamping` | Standardized damping API |
| `rb.angularDrag` | `rb.angularDamping` | Standardized damping API |
| `FindObjectOfType<T>()` | `FindAnyObjectByType<T>()` / `FindFirstObjectByType<T>()` | Explicit ordering & performance |
| `FindObjectsOfType<T>()` | `FindObjectsByType<T>(FindObjectsSortMode.None)` | Avoids unnecessary sorting overhead |
| `GameObject.Find()` | Cache references in `Awake()` / `[SerializeField]` | Prevents slow scene traversal |

### 3.2 The Unity "Fake Null" Trap (Critical)
`UnityEngine.Object` overloads `==` and `!=` to check if the underlying C++ native object has been destroyed.

- **DO NOT** use null-conditional (`?.`) or null-coalescing (`??`) operators on `UnityEngine.Object` derivatives (`MonoBehaviour`, `GameObject`, `Transform`, `Component`, `ScriptableObject`).
- **DO NOT**: `target?.DoSomething();` or `var comp = cachedComp ?? GetComponent<T>();`
- **DO**: Use explicit equality checks:
  ```csharp
  if (target != null)
  {
      target.DoSomething();
  }

  if (cachedComp == null)
  {
      cachedComp = GetComponent<T>();
  }
  ```

### 3.3 Inspector Serialization & Encapsulation
- Use `[SerializeField] private` for Inspector-configurable fields instead of `public`.
- Expose read-only properties or methods for external access.
- Use attributes to organize Inspector UI: `[Header("...")], [Tooltip("...")], [Range(min, max)], [RequireComponent(typeof(...))]`.

```csharp
// GOOD:
public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [Tooltip("Maximum health capacity.")]
    [SerializeField] private float maxHealth = 100f;
    
    [SerializeField] private float currentHealth;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
}
```

---

## 4. Unity Lifecycle & Execution Standards

```
Awake() -> OnEnable() -> Start() -> [FixedUpdate() (Physics)] -> [Update() (Input/Logic)] -> [LateUpdate() (Camera/Follow)] -> OnDisable() -> OnDestroy()
```

1. **`Awake()`**:
   - Initialize internal references, cache components on the same GameObject (`GetComponent<T>()`), set up singletons.
   - Do NOT assume other GameObjects have initialized their state.

2. **`Start()`**:
   - Connect to external objects, query manager singletons, initialize cross-object dependencies.

3. **`FixedUpdate()`**:
   - **Physics calculations only**: All `Rigidbody` forces (`AddForce`), velocity modifications (`linearVelocity`), and physics simulation steps.
   - Always multiply by `Time.fixedDeltaTime` when applying continuous frame-rate independent physics manual steps.

4. **`Update()`**:
   - Read user input, update animations, update timers, execute gameplay logic.
   - Always multiply frame-rate dependent values by `Time.deltaTime`.

5. **`LateUpdate()`**:
   - Camera movement, tracking transforms modified during `Update()`, procedural bone positioning.

6. **Hot Path Rule**:
   - **NEVER** call `GetComponent<T>()`, `Find*()`, `Camera.main`, `new List<T>()`, or LINQ inside `Update()` / `FixedUpdate()`. Cache references in `Awake()`.

---

## 5. Netcode for GameObjects (NGO) Multiplayer Guidelines

When writing or modifying networked gameplay scripts:

### 5.1 Base Class & Lifecycle
- Inherit from `NetworkBehaviour` instead of `MonoBehaviour`.
- Use `OnNetworkSpawn()` for network initialization instead of `Start()`.
- Use `OnNetworkDespawn()` for network cleanup and unhooking delegates instead of `OnDestroy()`.

### 5.2 Ownership & Authority Checks
Always guard execution based on network role:
```csharp
using Unity.Netcode;
using UnityEngine;

public class NetworkPlayerController : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            // Set up local camera, input listeners, HUD
        }
    }

    private void Update()
    {
        // Only local owner processes input
        if (!IsOwner) return;
        
        HandleInput();
    }

    private void FixedUpdate()
    {
        // Only owner or server computes movement depending on architecture
        if (!IsOwner) return;
        
        Move();
    }
}
```

### 5.3 Remote Procedure Calls (RPCs)
- Methods invoking Server execution must use `[ServerRpc]` attribute and end with the `ServerRpc` suffix.
- Methods invoking Client execution must use `[ClientRpc]` attribute and end with the `ClientRpc` suffix.
- Use `RequireOwnership = false` on ServerRpc only when clients other than the owner are legitimately permitted to trigger the call.

```csharp
[ServerRpc]
private void RequestItemPickupServerRpc(ulong networkObjectId, ServerRpcParams rpcParams = default)
{
    // Server validation and execution
    if (!IsServer) return;
    
    NotifyItemCollectedClientRpc(networkObjectId);
}

[ClientRpc]
private void NotifyItemCollectedClientRpc(ulong networkObjectId)
{
    // Client-side visual / audio feedback
}
```

### 5.4 NetworkVariables
- Use `NetworkVariable<T>` for synchronized state (health, score, team).
- Configure read/write permissions explicitly:
```csharp
private readonly NetworkVariable<int> score = new NetworkVariable<int>(
    0, 
    NetworkVariableReadPermission.Everyone, 
    NetworkVariableWritePermission.Server
);
```
- Subscribe to value change callbacks in `OnNetworkSpawn()` and unsubscribe in `OnNetworkDespawn()`:
```csharp
public override void OnNetworkSpawn()
{
    score.OnValueChanged += OnScoreChanged;
}

public override void OnNetworkDespawn()
{
    score.OnValueChanged -= OnScoreChanged;
}

private void OnScoreChanged(int previousValue, int newValue)
{
    // Update UI or visuals
}
```

---

## 6. AI & Navigation (`com.unity.ai.navigation`)

1. **State Machine / Clean AI Pattern**:
   - Structure AI with clean state machines or behavior logic (e.g. `IdleState`, `PatrolState`, `ChaseState`, `AttackState`).
   - Keep state logic decoupled from Monobehaviour rendering/physics.

2. **NavMeshAgent Best Practices**:
   - Check `agent.isOnNavMesh` before calling `agent.SetDestination()`, `agent.isStopped = true`, or `agent.Warp()`.
   - To check if path calculation is done: `if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)`.
   - Avoid calling `agent.SetDestination()` every single frame in `Update()`. Use an interval timer (e.g., every 0.1s - 0.2s) to reduce CPU load.

---

## 7. Performance & Memory Optimization

1. **Zero Garbage Collection (GC) in Hot Loops**:
   - **No LINQ** (`.Where()`, `.Select()`, `.ToList()`, etc.) in `Update()` / `FixedUpdate()`.
   - **No string concatenation** in hot loops (use cached strings or `StringBuilder`).
   - Use `NonAlloc` physics queries with pre-allocated buffer arrays:
     ```csharp
     private readonly Collider[] hitBuffer = new Collider[16];

     private void CheckArea()
     {
         int count = Physics.OverlapSphereNonAlloc(transform.position, 5f, hitBuffer, targetLayer);
         for (int i = 0; i < count; i++)
         {
             Collider col = hitBuffer[i];
             // Process hit
         }
     }
     ```

2. **String ID Caching**:
   - Cache animator parameter names and shader property names using static readonly integers:
     ```csharp
     private static readonly int SpeedParamHash = Animator.StringToHash("Speed");
     private static readonly int ColorPropertyId = Shader.PropertyToID("_BaseColor");

     animator.SetFloat(SpeedParamHash, moveSpeed);
     ```

3. **Object Pooling**:
   - Use `UnityEngine.Pool.ObjectPool<T>` or a custom pooling system for frequently spawned entities (projectiles, floating combat text, visual effects, loot drops).
   - Never repeatedly `Instantiate()` and `Destroy()` during active gameplay loops.

---

## 8. Input System Guidelines (`com.unity.inputsystem`)

- When implementing input, check if the project uses `InputActionReference`, `PlayerInput` component, or direct `Keyboard.current` / `Mouse.current` / `Gamepad.current`.
- Cleanly decouple input reading from gameplay physics:
  - Read input state into variables during `Update()`.
  - Apply physical movement based on those variables during `FixedUpdate()`.

---

## 9. Project Structure Conventions

Maintain clean directory organization under `Assets/Scripts/`:
```
Assets/
├── Scripts/
│   ├── Enemy/            # AI, NavMesh, State Machines, Enemy behaviors
│   ├── Inventory/        # Items, ItemSO, Tether, Collector, Spawners
│   ├── MapGeneration/    # Procedural map & dungeon generators
│   ├── Multiplayer/      # GameManager, Netcode synchronization, Network managers
│   ├── PlayerMovement/   # Player controllers, climbing, camera controllers
│   ├── UI/               # Menus, HUD, HighScoreManager, View controllers
│   └── Audio/            # Sound management, Vivox, SFX
├── Prefabs/              # Reusable GameObject prefabs
├── Settings/             # HDRP / URP / Input settings
└── Materials/            # Shaders and materials
```

---

## 10. AI Agent Workflow Checklist

Before delivering code or modifications:
- [ ] **Class name matches filename exactly.**
- [ ] **No deprecated Unity 5/2020-era physics APIs** (used `linearVelocity` / `linearDamping` where applicable).
- [ ] **No `?.` or `??` used on `UnityEngine.Object` subclasses.**
- [ ] **No `GetComponent()` or `Find*()` inside `Update()` / `FixedUpdate()`.**
- [ ] **Physics runs in `FixedUpdate()`, Input in `Update()`, Camera tracking in `LateUpdate()`.**
- [ ] **Netcode scripts inherit `NetworkBehaviour` and guard with `IsOwner` / `IsServer`.**
- [ ] **All new fields are `[SerializeField] private` with public getter properties where needed.**
- [ ] **NonAlloc physics methods used for multi-object queries in loops.**
- [ ] **`.meta` files are preserved and never deleted inadvertently.**
