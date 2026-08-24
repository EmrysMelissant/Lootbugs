# Unity AI Agent Settings & Project Rules

> **Summary**: Universal configuration and development rules for all AI agents working on this Unity project. This file mirrors [`AGENTS.md`](file:///e:/github/Lootbugs/AGENTS.md) and provides the complete guideline suite for Unity 6, Netcode for GameObjects, HDRP, C# architecture, and AI safety.

---

## 1. Core Project Specifications
- **Unity Version**: **Unity 6** (`6000.3.8f1`)
- **Render Pipeline**: High Definition Render Pipeline (**HDRP** 17.3+)
- **Networking**: **Netcode for GameObjects (NGO)** (`2.11.2`)
- **Input System**: **Unity Input System** (`1.18.0`)
- **AI & Navigation**: **Unity AI Navigation** (`2.0.12`)
- **Multiplayer Voice**: **Vivox** (`16.10.0`)

---

## 2. Essential Rules for AI Agents

### 2.1 Meta Files & Asset Integrity
- **Do not delete or modify `.meta` files** manually unless performing a verified deletion or GUID repair.
- When creating a C# script, ensure the class name matches the file name exactly.

### 2.2 Unity 6 API Compliance
- `rb.linearVelocity` instead of `rb.velocity`
- `rb.linearDamping` / `rb.angularDamping` instead of `rb.drag` / `rb.angularDrag`
- `FindAnyObjectByType<T>()` / `FindFirstObjectByType<T>()` instead of `FindObjectOfType<T>()`
- `FindObjectsByType<T>(FindObjectsSortMode.None)` instead of `FindObjectsOfType<T>()`

### 2.3 The "Fake Null" Gotcha
- **Never** use `?.` or `??` operators on `UnityEngine.Object` subclasses (`MonoBehaviour`, `GameObject`, `Component`, `ScriptableObject`).
- **Always** use explicit `if (obj != null)` or `if (obj == null)` because Unity overrides `==` to check native C++ lifecycle state.

### 2.4 Lifecycle & Physics Rules
- **`Awake()`**: Cache local components (`GetComponent<T>()`) and initialize singletons.
- **`Start()`**: Cross-object references and manager handshakes.
- **`FixedUpdate()`**: All `Rigidbody` manipulation, forces, and physical movement. Use `Time.fixedDeltaTime`.
- **`Update()`**: Inputs, frame logic, timers. Use `Time.deltaTime`.
- **`LateUpdate()`**: Camera follow, procedural bone rotations.
- **Hot-path rule**: No `GetComponent`, `Find*`, `Camera.main`, LINQ, or heap allocations in `Update()` / `FixedUpdate()`.

---

## 3. Netcode for GameObjects (NGO) Standards

1. **Inheritance**: Inherit from `NetworkBehaviour` instead of `MonoBehaviour` for networked components.
2. **Authority Checks**:
   - `if (!IsOwner) return;` for local player input / camera.
   - `if (!IsServer) return;` for game-state mutation / spawning.
3. **RPCs**:
   - `[ServerRpc]` methods must end with `ServerRpc`.
   - `[ClientRpc]` methods must end with `ClientRpc`.
4. **NetworkVariables**:
   - Specify read/write permissions explicitly.
   - Subscribe in `OnNetworkSpawn()` and unsubscribe in `OnNetworkDespawn()`.

---

## 4. Performance & Memory Management
- Pre-allocate arrays for physics queries (`Physics.RaycastNonAlloc`, `Physics.OverlapSphereNonAlloc`).
- Cache parameter hashes: `Animator.StringToHash()` and `Shader.PropertyToID()`.
- Use object pooling (`UnityEngine.Pool.ObjectPool<T>`) for repeated spawns (bullets, loot, VFX).
- Use `[SerializeField] private` fields with public read-only properties for safe encapsulation.

---

## 5. Folder Hierarchy
```
Assets/
  Scripts/
    Enemy/            # AI navigation and state machines
    Inventory/        # Items, ScriptableObjects, Collection
    MapGeneration/    # Procedural level generators
    Multiplayer/      # Netcode, GameManagers, Lobby
    PlayerMovement/   # Character controllers, climbing, camera
    UI/               # Menus, HUD, score tracking
```
