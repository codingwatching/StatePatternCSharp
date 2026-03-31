# MonoBehaviourStateMachine\<T\> Wrapper

> **Status:** Complete
> **Created:** 2026-03-12
> **Author:** Claude (agent)
> **Source:** `20260311-monobehaviour-statemachine-proposal.md` (Option A accepted)
> **Related Projex:** `20260312-istate-extraction-plan.md` (prerequisite), `20260312-istate-resolver-plan.md` (prerequisite), `2603272256-monobehaviour-statemachine-plan-redteam.md` (red team), `2603281200-noopstate-default-plan.md` (prerequisite — core NoOpState default)
> **Worktree:** No
> **Completed:** 2026-03-28
> **Walkthrough:** `20260312-monobehaviour-statemachine-walkthrough.md`

---

## Summary

Create `MonoBehaviourStateMachine<T>` — a Unity `MonoBehaviour` wrapper around `StateMachine<T>` that auto-discovers `IState` components, registers and clones serialized `ScriptableObject` states, drives machine updates from Unity's lifecycle, and validates setup contracts with explicit diagnostics. Additive change — one new file in `unity/`, no modifications to core library.

**Scope:** New file `unity/MonoBehaviourStateMachine.cs`. No changes to existing files.
**Estimated Changes:** 1 file created.

---

## Objective

### Problem / Gap / Need

After Plans 1 and 2, `IState` and `IStateResolver` exist — SO/MB types can implement the state contract and be resolved by type. But users must still manually:
- Construct `StateMachine<T>` in a MonoBehaviour
- Forward `Update()`/`FixedUpdate()`/`LateUpdate()`
- Discover and register MB states via `GetComponents` + `CacheByType` loops
- Clone SO states, track cloned instances, destroy them on teardown

This boilerplate is identical in every Unity project using the library. `MonoBehaviourStateMachine<T>` eliminates it.

### Success Criteria

- [ ] `MonoBehaviourStateMachine<T>` exists in `unity/MonoBehaviourStateMachine.cs`
- [ ] Wrapped `StateMachine<T>` accessible via `Machine` property
- [ ] `abstract ResolveSubject()` forces subclass to provide `T`
- [ ] MB `IState` components on the same GameObject auto-registered in `Awake`
- [ ] Serialized `ScriptableObject[]` field for Inspector-assigned SO states
- [ ] SO states always cloned per instance — cloned instances tracked and destroyed in `OnDestroy`
- [ ] `Update()`, `FixedUpdate()`, `LateUpdate()` forwarded to inner machine
- [ ] `ResolveSubject()` null return validated in `Awake()` with explicit diagnostic
- [ ] `Machine == null` guarded in `Update`/`FixedUpdate`/`LateUpdate` with diagnostic for missing `base.Awake()`
- [ ] Entire file guarded by `#if UNITY_2017_1_OR_NEWER`
- [ ] Compiles in Unity 2021.2+

### Out of Scope

- `MonoBehaviourMultiTrackStateMachine<T, TRACK>` — follow-on
- Custom Inspector / editor tooling — follow-on
- Shared (uncloned) SO mode — removed per red team F2; cross-instance contamination via `Reset()` and runtime field mutation is unsafe without stateless-only enforcement
- Per-state clone granularity (`StateEntry` struct) — refinement for later
- Unity-specific `IStateResolver` implementations — the MB machine handles registration directly; a separate resolver is unnecessary for the initial version

---

## Context

### Current State

After Plans 1 and 2, the library provides:

```csharp
// Core (pure C#)
StateMachine<T>.IState           // interface
StateMachine<T>.ChangeState<S>() // where S : IState (no new())
StateMachine<T>.CacheByType()    // runtime-typed registration
StateMachine<T>.StateResolver    // IStateResolver, defaults to ActivatorStateResolver
```

Unity-specific code exists only as:
- `#if UNITY_2017_1_OR_NEWER` guards in `IState` (default `FixedUpdate`/`LateUpdate`)
- `unity/StateMachine.TimedPopupState.cs` — a popup state utility

No Unity MonoBehaviour integration exists.

### Key Files

| File | Role | Change Summary |
|------|------|----------------|
| `unity/MonoBehaviourStateMachine.cs` (new) | MB wrapper class | Full implementation |

### Dependencies

- **Requires:** `20260312-istate-extraction-plan.md` (IState interface), `20260312-istate-resolver-plan.md` (CacheByType, resolver), `2603281200-noopstate-default-plan.md` (core NoOpState default — machines are safe before first `ChangeState`)
- **Blocks:** Nothing directly (follow-on: MultiTrack variant, editor tooling)

### Constraints

- `#if UNITY_2017_1_OR_NEWER` — entire file gated
- `MonoBehaviour` cannot be generic in Unity (generic MBs don't serialize). However, `abstract` generic MBs work as base classes — concrete subclasses with closed generic parameters serialize fine. This is the standard Unity pattern (e.g., `Singleton<T>`).
- `ScriptableObject[]` serializes as asset references in the Inspector. `IState[]` does not serialize (Unity can't serialize interface-typed fields). The `ScriptableObject[]` type is the correct serialization boundary; runtime `is IState` check filters.
- `[SerializeField]` on the SO array enables Inspector assignment on concrete subclasses.

### Assumptions

- Plans 1 and 2 have been executed
- Users will create concrete subclasses (e.g., `class EnemyMachine : MonoBehaviourStateMachine<Enemy>`) — the abstract base class is never placed on a GameObject directly
- `StateMachine<T>.FixedUpdate()` and `LateUpdate()` signatures are: `void FixedUpdate(IStateMachine<T> machine, T subject)` and `void LateUpdate(IStateMachine<T> machine, T subject)` (machine-level methods that call through to `CurrentState`)

### Impact Analysis

- **Direct:** One new file. Zero changes to existing code.
- **Adjacent:** None
- **Downstream:** Unity users gain a ready-made integration point instead of writing boilerplate

---

## Implementation

### Step 1: Create `unity/MonoBehaviourStateMachine.cs`

**Objective:** Implement the complete MB wrapper.
**Confidence:** High
**Depends on:** Plans 1 and 2 completed

**Files:**
- `unity/MonoBehaviourStateMachine.cs` (new)

**Changes:**

```csharp
#if UNITY_2017_1_OR_NEWER
using System.Collections.Generic;
using UnityEngine;

namespace BAStudio.StatePattern
{
    /// <summary>
    /// <para>MonoBehaviour wrapper around StateMachine&lt;T&gt;.</para>
    /// <para>Auto-discovers IState components on the GameObject and registers serialized ScriptableObject states.</para>
    /// <para>Forwards Unity lifecycle (Update, FixedUpdate, LateUpdate) to the inner machine.</para>
    /// <para>Subclass with a concrete T and override ResolveSubject() to use.</para>
    /// </summary>
    public abstract class MonoBehaviourStateMachine<T> : MonoBehaviour
    {
        [Header("State Configuration")]
        [Tooltip("ScriptableObject assets implementing IState. Assign in Inspector.")]
        [SerializeField] ScriptableObject[] scriptableObjectStates;

        List<Object> clonedInstances;

        /// <summary>
        /// The inner state machine. Access for advanced usage (events, popup states, SendEvent, etc.).
        /// </summary>
        public StateMachine<T> Machine { get; private set; }

        /// <summary>
        /// Provide the subject instance for the state machine.
        /// Common patterns: GetComponent&lt;T&gt;(), this as T, or a serialized reference.
        /// </summary>
        protected abstract T ResolveSubject();

        /// <summary>
        /// Called after the machine is constructed and all states are registered.
        /// Override to set the initial state or perform additional setup.
        /// Always call base.Initialize() if overriding.
        /// </summary>
        protected virtual void Initialize() { }

        protected virtual void Awake()
        {
            T subject = ResolveSubject();
            if (subject == null)
                throw new System.InvalidOperationException(
                    $"[{GetType().Name}] ResolveSubject() returned null. " +
                    "Override ResolveSubject() to return a valid subject instance.");

            Machine = new StateMachine<T>(subject);
            RegisterComponentStates();
            RegisterScriptableObjectStates();
            Initialize();
        }

        void RegisterComponentStates()
        {
            foreach (var component in GetComponents<StateMachine<T>.IState>())
            {
                // Skip self if this MonoBehaviour also implements IState (unlikely but safe)
                if (ReferenceEquals(component as Object, this)) continue;
                Machine.CacheByType(component);
            }
        }

        void RegisterScriptableObjectStates()
        {
            if (scriptableObjectStates == null) return;
            foreach (var so in scriptableObjectStates)
            {
                if (so == null) continue;
                if (so is not StateMachine<T>.IState)
                {
                    Debug.LogWarning(
                        $"[{GetType().Name}] ScriptableObject '{so.name}' ({so.GetType().Name}) " +
                        $"does not implement {typeof(IStateMachine<T>).Name}.IState. Skipping.",
                        this);
                    continue;
                }

                var clone = Instantiate(so);
                clone.name = so.name; // Preserve original name (Instantiate appends "(Clone)")
                clonedInstances ??= new List<Object>();
                clonedInstances.Add(clone);
                Machine.CacheByType((IStateMachine<T>.IState) clone);
            }
        }

        protected virtual void Update()
        {
            if (Machine == null)
                throw new System.InvalidOperationException(
                    $"[{GetType().Name}] Machine is null. Did you override Awake() without calling base.Awake()?");
            Machine.Update();
        }

        protected virtual void FixedUpdate()
        {
            if (Machine == null)
                throw new System.InvalidOperationException(
                    $"[{GetType().Name}] Machine is null. Did you override Awake() without calling base.Awake()?");
            Machine.FixedUpdate(Machine, Machine.Subject);
        }

        protected virtual void LateUpdate()
        {
            if (Machine == null)
                throw new System.InvalidOperationException(
                    $"[{GetType().Name}] Machine is null. Did you override Awake() without calling base.Awake()?");
            Machine.LateUpdate(Machine, Machine.Subject);
        }

        protected virtual void OnDestroy()
        {
            if (clonedInstances == null) return;
            for (int i = 0; i < clonedInstances.Count; i++)
                if (clonedInstances[i] != null) Destroy(clonedInstances[i]);
            clonedInstances = null;
        }
    }
}
#endif
```

**Design decisions:**

1. **`Awake` is `virtual`, not `sealed`** — follows standard Unity convention. `Initialize()` is provided as a post-setup hook, but users can also override `Awake()` with `base.Awake()`. Sealed Awake would be safer but unfamiliar to Unity developers.

2. **`GetComponents` (same object only)** — not `GetComponentsInChildren`. Same-object guarantees lifetime alignment. Users needing child scanning can override `Awake()` and call `Machine.CacheByType()` manually.

3. **`ScriptableObject[]` not `IState[]`** — Unity serialization requires concrete types. Runtime `is IState` check with warning handles misassignment.

4. **Clone name preservation** — `Instantiate` appends "(Clone)" to `name`. Resetting to original avoids confusion in debug output.

5. **Always clone SOs** — shared SO mode removed per red team F2 (`2603272256-monobehaviour-statemachine-plan-redteam.md`). Cloning gives each machine its own instance, so `Reset()` and runtime fields are fully isolated. Shared mode was cut because without it, multiple machines would mutate and reset the same SO instance — unsafe unless stateless-only semantics are enforced, which is out of scope for v1.

6. **`clonedInstances` list** — tracks clones for `OnDestroy` cleanup. Only allocated if SOs are registered.

7. **`ReferenceEquals` self-check** — prevents registering the machine itself if it somehow implements `IState` (defensive, unlikely).

8. **Explicit diagnostics** — added per red team F3. `ResolveSubject()` null and missing `base.Awake()` produce wrapper-specific `InvalidOperationException` with the concrete type name and fix guidance, rather than generic downstream null references.

**Rationale:** Composition over inheritance. The core `StateMachine<T>` stays pure C#. The wrapper handles Unity-specific concerns (serialization, lifecycle, cloning) without any core changes.

**Verification:**
- File compiles under Unity 2021.2+ with `UNITY_2017_1_OR_NEWER` defined
- A concrete subclass can be placed on a GameObject and assign SO states in Inspector
- MB states on the same GameObject are auto-discovered
- Cloned SOs are destroyed when the GameObject is destroyed

**If this fails:** Delete the file.

---

## Verification Plan

### Automated Checks

- [ ] `dotnet build` succeeds (non-Unity — file excluded by `#if` guard, no impact)
- [ ] Unity project compiles with the new file included

### Manual Verification

- [ ] Create a test `ScriptableObject` implementing `StateMachine<TestSubject>.IState`
- [ ] Create a test `MonoBehaviour` implementing `StateMachine<TestSubject>.IState`
- [ ] Create a concrete `TestMachine : MonoBehaviourStateMachine<TestSubject>`
- [ ] Place on a GameObject with the MB state component and the SO state assigned in Inspector
- [ ] Verify: `Machine.ChangeState<SOStateType>()` works (SO state resolved from cache)
- [ ] Verify: `Machine.ChangeState<MBStateType>()` works (MB state resolved from cache)
- [ ] Verify: destroying the GameObject calls `OnDestroy` and cleans up cloned SOs
- [ ] Verify: `ResolveSubject()` returning null throws `InvalidOperationException` with type name
- [ ] Verify: overriding `Awake()` without `base.Awake()` throws `InvalidOperationException` on first `Update()`

### Acceptance Criteria Validation

| Criterion | How to Verify | Expected Result |
|-----------|---------------|-----------------|
| MB states auto-registered | Add MB IState to same GO, check `Machine` cache | State found by type |
| SO states registered | Assign SO in Inspector, check `Machine` cache | State found by type |
| SO always cloned | Assign SO, modify clone's runtime field | Original asset unmodified |
| Cleanup on destroy | Destroy GO, check cloned SO | Cloned SO destroyed |
| Lifecycle forwarding | Run scene, verify state gets `Update` calls | State `Update` fires each frame |
| Null subject diagnostic | Return null from `ResolveSubject()` | `InvalidOperationException` with type name |
| Missing base.Awake diagnostic | Override `Awake()` without `base.Awake()` | `InvalidOperationException` on `Update()` |

---

## Rollback Plan

1. Delete `unity/MonoBehaviourStateMachine.cs`

(No existing files are modified.)

---

## Notes

### Risks

- **Generic MonoBehaviour serialization**: Unity doesn't serialize generic MonoBehaviours. This is fine — `MonoBehaviourStateMachine<T>` is abstract, always subclassed with a concrete `T`. The concrete subclass serializes normally. This is a well-established Unity pattern.
- **FixedUpdate/LateUpdate signature mismatch**: The machine's `FixedUpdate` and `LateUpdate` take `(IStateMachine<T> machine, T subject)` parameters (designed for state-level calls). The MB wrapper calls them with the inner machine's own references. If these signatures change in a future refactor, this file must update. Low risk — signatures are stable.

### Red Team Remediations Applied

- **F2 (shared SO mode):** Removed `cloneScriptableObjects` toggle. All SOs always cloned. Eliminates cross-instance contamination via `Reset()` and shared runtime fields.
- **F3 (weak guardrails):** Added `ResolveSubject()` null validation in `Awake()` and `Machine == null` guards in `Update`/`FixedUpdate`/`LateUpdate`. Misuse produces wrapper-specific diagnostics.
- **F1 (first-frame crash):** Addressed by prerequisite `2603281200-noopstate-default-plan.md` — core `StateMachine<T>` defaults to `NoOpState`, so uninitialized machines are safe.

### Open Questions

(None — deferred items from proposal are explicitly out of scope for this initial version: MultiTrack variant, shared SO mode, per-state clone control, editor tooling.)
