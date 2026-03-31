# Machines as States + Link/Unlink Peer Ring — Unified Machine Composition

> **Status:** Ready
> **Created:** 2026-03-29
> **Revised:** 2026-03-30 — integrated `2603301600-link-peer-ring-plan-revision.md` (Steps 2, 4, 5, 6 replaced with Link/Unlink peer ring model)
> **Revised:** 2026-03-31 — removed `Host`, replaced component prototype chain with entry-time snapshot via `IComponentUser` per `2603311500-remove-host-component-snapshot-proposal.md`
> **Author:** Claude (agent)
> **Source:** `2603201530-statemachines-as-states-imagine.md` (Direction 1), `2603251600-attach-detach-parallel-machines-proposal.md` (superseded by Link/Unlink revision)
> **Related Projex:** `2603251530-multitrack-validity-with-machines-as-states-eval.md`, `2603251545-trackless-parallel-regions-imagine.md`, `2603251200-statepatterncsharp-roadmap-nav.md`, `2603311500-remove-host-component-snapshot-proposal.md`
> **Worktree:** Yes

---

## Summary

Unify hierarchical and parallel machine composition under `StateMachine<T>`. `StateMachine<T>` implements `IState<T>` for vertical nesting (machines as states) — sub-machines receive parent components as a snapshot at entry time via `IComponentUser`, with no parent reference. `Link()`/`Unlink()` enable horizontal composition — a symmetric peer ring with direct lateral event broadcast. Peers are equals with no ownership, no automatic ticking, and no component sharing. `MultiTrackStateMachine` becomes obsolete.

**Scope:** `IState<T>` on `StateMachine<T>`, `IComponentUser` on `StateMachine<T>` for entry-time component snapshot, `Link`/`Unlink` peer ring with event bridging, MultiTrack deprecation.
**Estimated Changes:** 1 file modified, 4 files created, 2 files deprecated

---

## Objective

### Problem / Gap / Need

Two composition patterns are needed and currently missing or incomplete:

1. **Hierarchical (vertical):** A machine inside a machine — e.g., `Alive > Exploration > Walking`. Currently requires manual plumbing: wrapping a sub-machine in a custom `IState<T>`, forwarding lifecycle calls, threading subject and components. The imagination (`2603201530`) showed this should be free if `StateMachine<T>` implements `IState<T>`.

2. **Parallel (horizontal):** Multiple machines running concurrently on the same subject — e.g., `Combat` + `Animation` + `Buffs`. Currently served by `MultiTrackStateMachine<T, TRACK>`, which requires a compile-time enum, static array sizing, and no dynamic add/remove. The attach/detach proposal (`2603251600`) showed this should be dynamic.

These two relationships are distinct and must not be conflated:

- **Hierarchical:** Parent owns child. Parent ticks child. Sub-machine receives parent components as snapshot at entry. Events flow through nesting.
- **Parallel:** Peers are equals. No ownership. Events broadcast laterally. No component sharing.

`Link`/`Unlink` serves parallelism as a symmetric peer ring. Hierarchy uses `IState<T>` nesting with component snapshot delivery — no parent reference needed.

### Success Criteria

- [ ] `StateMachine<T>` implements `IState<T>` — any machine subclass usable as a state via `ChangeState<SubMachine>()`
- [ ] `StateMachine<T>` implements `IComponentUser` — receives parent components as snapshot at entry via `OnComponentSupplied` → `SetComponent`
- [ ] `Link(a, b)` / `Unlink(a, b)` — static, symmetric, bidirectional peer ring
- [ ] Peers broadcast events laterally — no hub, no "up then redistribute"
- [ ] Peers do NOT tick each other — caller ticks all machines independently
- [ ] Peers do NOT share components — each machine has its own
- [ ] Root `Update()` cascades through nested machines (via `CurrentState`); peers are independent
- [ ] `ForwardedEvent<E>` envelope for cross-boundary event source identification
- [ ] `PeerStateChangedEvent` for cross-peer state change notification
- [ ] Event re-entry guard prevents infinite loops between peers
- [ ] Sub-machine states access parent components via entry-time snapshot; peers do not share components
- [ ] `GetStatePath()` returns full nested path for debug output
- [ ] `MultiTrackStateMachine` marked `[Obsolete]` with Link migration message
- [ ] Constructor problem resolved — parameterless construction with deferred subject
- [ ] Existing root machine usage unchanged

### Out of Scope

- Subject polymorphism / projection (Direction 2 — different `T` per nesting level)
- Event bubbling through nested hierarchy (events stay scoped to the immediate machine; peers get lateral bridging)
- History state variants (deep history, configurable resume)
- Serialization of nested/linked machine trees
- Visual debugging / tree inspector tooling
- `MultiTrackStateMachine` deletion (deprecated, not removed)
- `MultiTrackStateMachine` nesting as IState (needs its own parameterless ctor + deferred `SideTracks` init — separate plan)
- `LinkAll(params)` convenience method (noted as future)
- Transitive link propagation (links are pairwise by design)
- Shared component registry for peers

---

## Context

### Current State

`StateMachine<T>` implements `IStateMachine<T>`. Single constructor `StateMachine(T subject)` with get-only `Subject`. `Update()` ticks `UpdateMainState()` + `UpdatePopupStates()`. `SendEvent<E>()` dispatches to current state + popup states. Components delivered via `SetComponent`/`DeliverComponents` with `[AutoComponent]` reflection.

`MultiTrackStateMachine<T, TRACK>` extends `StateMachine<T>` with a compile-time enum-indexed `IState<T>[]` array for parallel regions. Ticks side tracks in `Update()`, dispatches events to them, fires `OnSideTrackStateChanging/Changed`.

`IState<T>` is a standalone interface. `IStateMachine<T>` is a typed facade. `IStateResolver` + `ActivatorStateResolver` handle state creation.

### Key Files

| File | Role | Change Summary |
|------|------|----------------|
| `StateMachine.cs` | Core machine class | Parameterless ctor, Subject settable, modify `SendEvent`/`PostStateChange` for peer broadcast, `IComponentUser` impl, `[DisableAutoComponents]` |
| `StateMachine.AsState.cs` | **New** partial | Explicit `IState<T>` implementation, virtual hooks, `GetStatePath()` |
| `StateMachine.Link.cs` | **New** partial | `peers` field, `Link()`, `Unlink()`, `SendEventToPeers()`, `NotifyPeersOfStateChange()`, `ReceiveFromPeer()`, dispatch guards |
| `StateMachine.PeerStateChangedEvent.cs` | **New** partial | Event struct for cross-peer state change notification `{ source, from, to }` |
| `StateMachine.ForwardedEvent.cs` | **New** partial | Generic envelope for cross-boundary events |
| `MultiTrackStateMachine.cs` | Multi-track extension | `[Obsolete]` attribute + migration message |
| `MultiTrackStateMachine.SideTrackStateChangedEvent.cs` | Side track event | `[Obsolete]` attribute |
| `IStateMachine.cs` | Machine facade | No changes |

### Dependencies

- **Requires:** Phase 4 (IStateMachine facade + IState extraction) — **Done** (7aae3a7)
- **Replaces:** `MultiTrackStateMachine<T, TRACK>` (deprecated, not deleted)

### Constraints

- `Subject { get; }` is currently get-only — `{ get; protected set; }` is the minimum. Cannot be public-settable.
- Parameterless constructor must be `protected` — user sub-machine classes define their own public parameterless ctor chaining to `base()`.
- No `Host` property — sub-machines have no parent reference. Components are delivered as a snapshot at entry time via existing `IComponentUser`/`DeliverComponents` contract.
- `StateMachine<T>` marked `[DisableAutoComponents]` — skips reflection, uses bulk `OnComponentSupplied` path for component snapshot delivery.
- Links are pairwise, not transitive. `Link(A,B)` + `Link(B,C)` does NOT mean A sees C.
- `Unlink()` does NOT exit either machine's state. Peers don't own each other's lifecycle.
- `Link()`/`Unlink()` throw if called during peer event dispatch (mutation guard).

### Assumptions

- C# 8 default interface methods are available (already used in `IState<T>`, `IPopupState<T>`)
- Sub-machine exit: current child exited, `CurrentState` reset to `NoOpState`, re-entry starts fresh (no shallow history in v1)
- Unlink: neither machine's state is exited — peers are independent

### Impact Analysis

- **Direct:** `StateMachine<T>` — modified (ctor, Subject, SendEvent, PostStateChange, IComponentUser). New partial files for IState, Link, events.
- **Adjacent:** `MultiTrackStateMachine<T, TRACK>` — deprecated. Continues to function but marked `[Obsolete]`.
- **Downstream:** `MonoBehaviourStateMachine<T>` — unaffected (wraps via composition). A machine with peers is still just a `StateMachine<T>`; the wrapper ticks it the same way.

---

## Implementation

### Overview

Six steps: (1) foundation — Subject settability + parameterless ctor, (2) Link/Unlink peer ring, (3) IState<T> implementation (no Host), (4) event bridging + re-entry guard, (5) component snapshot at entry via IComponentUser, (6) deprecation.

---

### Step 1: Foundation — Subject Settability + Parameterless Constructor

**Objective:** Enable sub-machines to receive their subject after construction.
**Confidence:** High
**Depends on:** None

**Files:**
- `StateMachine.cs`

**Changes:**

```csharp
// Before (line 28):
public T Subject { get; }

// After:
public T Subject { get; protected set; }
```

```csharp
// Before (lines 22-27 — only constructor):
public StateMachine(T subject)
{
    Subject = subject;
    CurrentState = new NoOpState();
    UpdatePaused = false;
}

// After (add parameterless constructor before existing one):
protected StateMachine()
{
    CurrentState = new NoOpState();
}

public StateMachine(T subject)
{
    Subject = subject;
    CurrentState = new NoOpState();
    UpdatePaused = false;
}
```

**Rationale:** Sub-machines created by `ActivatorStateResolver` (parameterless) receive their subject via `IState<T>.OnEntered`. Peer machines are created with `new StateMachine<T>(subject)` directly — they already have subject. The parameterless ctor is `protected`; user sub-machine classes define their own public ctor chaining to `base()`. `UpdatePaused` backing field defaults to `false`, so the parameterless ctor omits it.

**Verification:** `new StateMachine<T>(subject)` unchanged. Subclass `: base()` compiles. `Subject` is null after parameterless construction, set after `OnEntered`.

**If this fails:** Revert. No cascading impact.

---

### Step 2: Link/Unlink Peer Ring

**Objective:** Symmetric peer ring for parallel composition. No ownership, no hierarchy.
**Confidence:** High
**Depends on:** None (parallel with Step 1)

**Files:**
- `StateMachine.Link.cs` **(new file)**

**Changes:**

```csharp
using System;
using System.Collections.Generic;

namespace BAStudio.StatePattern
{
    public partial class StateMachine<T>
    {
        List<StateMachine<T>> peers;
        bool isPeerDispatching;

        /// <summary>
        /// Peer machines linked to this machine. Symmetric: if A sees B, B sees A.
        /// Null if no peers are linked.
        /// </summary>
        public IReadOnlyList<StateMachine<T>> Peers => peers?.AsReadOnly();

        /// <summary>
        /// Create a symmetric peer link. Static because the relationship is symmetric
        /// -- neither machine is privileged.
        /// </summary>
        public static void Link(StateMachine<T> a, StateMachine<T> b)
        {
            if (a == null) throw new ArgumentNullException(nameof(a));
            if (b == null) throw new ArgumentNullException(nameof(b));
            if (a == b)
                throw new InvalidOperationException("A machine cannot link to itself.");
            if (a.isPeerDispatching || b.isPeerDispatching)
                throw new InvalidOperationException(
                    "Cannot Link/Unlink while peer event dispatch is in progress.");

            a.peers ??= new List<StateMachine<T>>();
            b.peers ??= new List<StateMachine<T>>();

            if (a.peers.Contains(b))
                throw new InvalidOperationException("Machines are already linked.");

            a.peers.Add(b);
            b.peers.Add(a);
        }

        /// <summary>
        /// Remove a symmetric peer link. Does NOT exit either machine's state --
        /// peers don't own each other's lifecycle.
        /// </summary>
        public static void Unlink(StateMachine<T> a, StateMachine<T> b)
        {
            if (a == null) throw new ArgumentNullException(nameof(a));
            if (b == null) throw new ArgumentNullException(nameof(b));
            if (a.isPeerDispatching || b.isPeerDispatching)
                throw new InvalidOperationException(
                    "Cannot Link/Unlink while peer event dispatch is in progress.");

            bool removedFromA = a.peers != null && a.peers.Remove(b);
            bool removedFromB = b.peers != null && b.peers.Remove(a);

            if (!removedFromA || !removedFromB)
                throw new InvalidOperationException("Machines are not linked.");

            if (a.peers.Count == 0) a.peers = null;
            if (b.peers.Count == 0) b.peers = null;
        }
    }
}
```

**Rationale:**
- Static `Link(a, b)` — symmetric by construction. No `a.Link(b)` that implies `a` owns the action.
- `Unlink` does not interfere with state lifecycle — peers are independent.
- Null cleanup on empty lists keeps fast-path `if (peers == null) return;` free.
- Mutation guard (`isPeerDispatching`) prevents list modification during event iteration.

**Verification:**
- `Link(a, b)` — `a.Peers` contains `b`, `b.Peers` contains `a`
- `Unlink(a, b)` — both removed, lists nullified
- `Link(a, a)` — throws
- `Link(a, b)` twice — throws
- `Unlink` when not linked — throws
- `Link` during dispatch — throws

**If this fails:** Delete `StateMachine.Link.cs`. Purely additive.

---

### Step 3: Implement IState<T> on StateMachine<T>

**Objective:** Hierarchical composition — machines nestable as states inside parent machines. No parent reference — sub-machines are just states.
**Confidence:** High
**Depends on:** Step 1 (Subject settability)

**Files:**
- `StateMachine.AsState.cs` **(new file)**

**Changes:**

```csharp
namespace BAStudio.StatePattern
{
    public partial class StateMachine<T> : IState<T>
    {
        // --- IState<T> explicit implementation ---

        void IState<T>.OnEntered(IStateMachine<T> machine, IState<T> previous, T subject, object parameter)
        {
            Subject = subject;
            OnNestedEntry(previous, parameter);
        }

        void IState<T>.Update(IStateMachine<T> machine, T subject)
        {
            Update();
        }

        void IState<T>.OnLeaving(IStateMachine<T> machine, IState<T> next, T subject, object parameter)
        {
            if (CurrentState is not NoOpState && CurrentState != null)
                CurrentState.OnLeaving(this, null, subject, parameter);
            CurrentState = new NoOpState();
        }

        void IState<T>.Reset()
        {
            ResetInternalState();
        }

#if UNITY_2017_1_OR_NEWER
        void IState<T>.FixedUpdate(IStateMachine<T> machine, T subject)
        {
            FixedUpdate();
        }

        void IState<T>.LateUpdate(IStateMachine<T> machine, T subject)
        {
            LateUpdate();
        }
#endif

        // --- Virtual hooks ---

        /// <summary>
        /// Called when this machine is entered as a state inside a parent machine.
        /// Override to set the initial state (e.g., <c>ChangeState&lt;MyInitialState&gt;()</c>).
        /// Subject is already set when this is called.
        /// </summary>
        protected virtual void OnNestedEntry(IState<T> previous, object parameter) { }

        /// <summary>
        /// Called when IState&lt;T&gt;.Reset() is invoked (after parent transitions away).
        /// Default: no-op (AutoStateCache preserved for re-entry efficiency).
        /// Override to clear: <c>AutoStateCache?.Clear();</c>
        /// </summary>
        protected virtual void ResetInternalState() { }

        // --- Debug path ---

        /// <summary>
        /// Full nested state path from this machine downward.
        /// Example: "Alive > Exploration > Walking"
        /// </summary>
        public string GetStatePath()
        {
            if (CurrentState == null || CurrentState is NoOpState)
                return "(none)";

            string name = CurrentState.GetType().Name;

            if (CurrentState is StateMachine<T> sub)
                return name + " > " + sub.GetStatePath();

            return name;
        }
    }
}
```

**Rationale:** Explicit interface implementation keeps `IState<T>` methods invisible on root machines — only visible when cast to `IState<T>`, which `ChangeState` does internally. `OnLeaving` exits child state and resets to `NoOpState` for clean re-entry. `Update()` delegation is virtual — `MultiTrackStateMachine.Update()` override works correctly if a multi-track machine is nested in the future. No `Host` property — a nested machine is just a state; it receives what it needs at entry, not by reaching upward.

**Verification:** `parent.ChangeState<SubMachine>()` enters sub-machine, `sub.Subject == parent.Subject`. `parent.Update()` cascades to sub-machine. `parent.ChangeState<Other>()` exits sub-machine cleanly.

**If this fails:** Delete `StateMachine.AsState.cs`. Link/Unlink still works independently.

---

### Step 4: Event Bridging — Direct Lateral Broadcast

**Objective:** Symmetric event communication between peers. No hub-and-spoke.
**Confidence:** Medium — re-entry loop risk, but simpler than a hub model.
**Depends on:** Step 2 (Link)

**Files:**
- `StateMachine.cs` (modify `SendEvent`, `PostStateChange`)
- `StateMachine.Link.cs` (add event forwarding methods + re-entry guard)
- `StateMachine.PeerStateChangedEvent.cs` **(new file)**
- `StateMachine.ForwardedEvent.cs` **(new file)**

**Key design: NO re-broadcast in `ReceiveFromPeer`.**

The source machine broadcasts to ALL its own peers directly. `ReceiveFromPeer` delivers to the receiver's own states only — no re-broadcast. Re-broadcast would cause duplication in fully-connected graphs and loops in chains.

**Changes to `StateMachine.cs`:**

`SendEvent<E>` (after `SendEventToPopupStates`):
```csharp
// Add after SendEventToPopupStates(ev):
SendEventToPeers(ev);
```

`PostStateChange` (after `stateChangingDepth--`):
```csharp
// Add after stateChangingDepth--:
NotifyPeersOfStateChange(fromState, CurrentState);
```

**Additions to `StateMachine.Link.cs`:**

```csharp
int peerEventForwardingDepth;
const int MaxPeerEventForwardingDepth = 8;

[MethodImpl(MethodImplOptions.AggressiveInlining)]
void SendEventToPeers<E>(E ev)
{
    if (peers == null) return;
    if (peerEventForwardingDepth >= MaxPeerEventForwardingDepth)
    {
        LogFormat("Peer event forwarding depth limit reached ({0}).",
                  MaxPeerEventForwardingDepth);
        return;
    }
    isPeerDispatching = true;
    peerEventForwardingDepth++;
    try
    {
        var forwarded = new ForwardedEvent<E>(this, ev);
        for (int i = 0; i < peers.Count; i++)
            peers[i].ReceiveFromPeer(this, forwarded);
    }
    finally
    {
        peerEventForwardingDepth--;
        isPeerDispatching = false;
    }
}

[MethodImpl(MethodImplOptions.AggressiveInlining)]
void NotifyPeersOfStateChange(IState<T> from, IState<T> to)
{
    if (peers == null) return;
    var ev = new PeerStateChangedEvent(this, from, to);
    for (int i = 0; i < peers.Count; i++)
        peers[i].ReceiveFromPeer(this, ev);
}

/// <summary>
/// Receive from a peer. Delivers to own states only. NO re-broadcast.
/// </summary>
internal void ReceiveFromPeer<E>(StateMachine<T> source, E ev)
{
    SendEventToCurrentState(ev);
    SendEventToPopupStates(ev);
}
```

**Event flow — peer A changes state (A-B-C fully linked):**

```
A.PostStateChange
  -> A.SendEvent(MainStateChangedEvent)
       -> A's own CurrentState + Popups
       -> A.SendEventToPeers:
            B.ReceiveFromPeer(ForwardedEvent<MainStateChangedEvent>)
            C.ReceiveFromPeer(ForwardedEvent<MainStateChangedEvent>)
  -> A.NotifyPeersOfStateChange:
       B.ReceiveFromPeer(PeerStateChangedEvent{source=A})
       C.ReceiveFromPeer(PeerStateChangedEvent{source=A})
```

Peers receive both types. States implement handlers only for what they care about (type-matched dispatch).

**New file `StateMachine.PeerStateChangedEvent.cs`:**

```csharp
namespace BAStudio.StatePattern
{
    public partial class StateMachine<T>
    {
        public struct PeerStateChangedEvent
        {
            public StateMachine<T> source;
            public IState<T> from, to;

            public PeerStateChangedEvent(StateMachine<T> source, IState<T> from, IState<T> to)
            {
                this.source = source;
                this.from = from;
                this.to = to;
            }
        }
    }
}
```

**New file `StateMachine.ForwardedEvent.cs`:**

```csharp
namespace BAStudio.StatePattern
{
    public partial class StateMachine<T>
    {
        /// <summary>
        /// Envelope wrapping any event that crosses a peer machine boundary.
        /// States opt in to source discrimination per event type via
        /// <c>IEventReceiverState&lt;T, ForwardedEvent&lt;E&gt;&gt;</c>.
        /// </summary>
        public struct ForwardedEvent<E>
        {
            public StateMachine<T> source;
            public E inner;

            public ForwardedEvent(StateMachine<T> source, E inner)
            {
                this.source = source;
                this.inner = inner;
            }
        }
    }
}
```

**Opt-in source discrimination:**

```csharp
// Local events only:
class LocalState : IEventReceiverState<T, DamageTaken> { ... }

// Cross-peer events with source:
class PeerAwareState : IEventReceiverState<T, ForwardedEvent<DamageTaken>>
{
    void ReceiveEvent(IStateMachine<T> m, T s, ForwardedEvent<DamageTaken> ev)
    {
        ev.source; // which peer
        ev.inner;  // the original event
    }
}
```

**Rationale:** Events sent on a machine cascade to peers wrapped in `ForwardedEvent<E>` so receivers can identify the source. State changes notify peers via `PeerStateChangedEvent`. Re-entry guard (depth limit of 8 with logging) prevents infinite loops — same class of protection as `stateChangingDepth`. NO re-broadcast in `ReceiveFromPeer` — the source broadcasts to all its own peers directly, preventing duplication and loops.

Nested machines (IState) do NOT get host events automatically — they're the CurrentState, so they already receive events via `SendEventToCurrentState` if they implement `IEventReceiverState`. Event bubbling through the nested hierarchy is a future enhancement, intentionally out of scope.

**Verification:**
1. A sends event → B receives `ForwardedEvent<E>` with source=A
2. A changes state → B receives `PeerStateChangedEvent` with source=A
3. A-B-C fully linked, A sends event → B and C each receive once (no duplication)
4. Re-entry caps at depth 8
5. Root machine with no peers → no behavioral change (null checks short-circuit)

**If this fails:** Revert `SendEvent`/`PostStateChange` modifications, delete event struct files. Link/Unlink degrades to structural-only (no event bridging) — still functional.

---

### Step 5: Component Snapshot at Entry — IComponentUser on StateMachine

**Objective:** Sub-machine receives parent components as a snapshot at entry time. No parent reference, no chain-walking. Peers are independent.
**Confidence:** High — reuses existing `DeliverComponents` + `IComponentUser` + `SetComponent` contracts with no new abstractions.
**Depends on:** Step 3 (IState<T> implementation)

**Files:**
- `StateMachine.cs` (add `IComponentUser` implementation, `[DisableAutoComponents]` attribute)

**Changes:**

`StateMachine<T>` implements `IComponentUser` and is marked `[DisableAutoComponents]`:

```csharp
[DisableAutoComponents]
public partial class StateMachine<T> : IStateMachine<T>, IComponentUser
```

Add `IComponentUser` implementation:

```csharp
/// <summary>
/// Receives components from the parent machine at entry time (snapshot).
/// Stores each component locally via SetComponent so the sub-machine's own
/// states receive them through normal DeliverComponents when they enter.
/// Virtual — sub-machines that want isolation override to no-op.
/// </summary>
public virtual void OnComponentSupplied(Type componentType, object component)
{
    SetComponent(componentType, component);
}
```

No changes to `DeliverComponents` internals. The existing flow already delivers components to `IComponentUser` states — `StateMachine<T>` as a state now receives them through this path:

```
Parent.ChangeState<SubMachine>()
  -> Parent.DeliverComponents(subMachine)   // existing flow
    -> subMachine.OnComponentSupplied(typeof(ILogger), logger)
      -> subMachine.SetComponent(typeof(ILogger), logger)  // stored locally
        -> later: subMachine.DeliverComponents(childState)  // normal delivery
```

`[DisableAutoComponents]` skips the reflection path on `StateMachine<T>` (no `[AutoComponent]` properties on the machine class) and uses the bulk `OnComponentSupplied` path instead.

Shared components across peers = user registers on each:
```csharp
var shared = new SharedService();
machineA.SetComponent(shared);
machineB.SetComponent(shared);
```

**Rationale:** Zero new machinery. The sub-machine is an `IComponentUser` like any other state, and the parent delivers components to it like any other state. This is the most consistent with "a machine is just a state." Snapshot semantics — component changes on the parent after entry don't propagate. Same constraint as any state: states don't get re-delivered after entry either. `OnComponentSupplied` is virtual — sub-machines that want component isolation override it to no-op.

**Verification:**
1. Parent machine: `SetComponent<ILogger, Logger>(logger)` → sub-machine stores it locally, sub-machine's child states with `[AutoComponent] ILogger` receive it
2. Sub-machine: `SetComponent<ILogger, OtherLogger>(other)` → local overwrites snapshot (later `SetComponent` wins)
3. Root machine → no behavioral change (never passed through `DeliverComponents` as a state)
4. Peer machine → no component sharing (peers don't deliver to each other)
5. Component changed on parent after entry → sub-machine retains snapshot (by design)

**If this fails:** Remove `IComponentUser` implementation and `[DisableAutoComponents]`. Sub-machine states just don't see parent components. Degraded but functional.

---

### Step 6: Deprecation

**Objective:** Deprecate MultiTrack with migration message. `Update()` is UNCHANGED — peers tick independently.
**Confidence:** High
**Depends on:** Step 2 (Link)

**Files:**
- `MultiTrackStateMachine.cs` (add `[Obsolete]`)
- `MultiTrackStateMachine.SideTrackStateChangedEvent.cs` (add `[Obsolete]`)

**`Update()` is UNCHANGED from current code.** No `UpdateAttachedMachines()` added. Peers tick independently:

```csharp
// BEFORE (MultiTrack — host ticks side tracks automatically)
machine.Update(); // ticks main + all side tracks

// AFTER (Link — each machine ticked independently)
combat.Update();
animation.Update();
buffs.Update();
```

Hierarchy still cascades via `IState<T>.Update` delegation (Step 3).

**MultiTrack deprecation:**

```csharp
// MultiTrackStateMachine.cs — add attribute to class:
[System.Obsolete("Use StateMachine<T> with Link() for parallel composition.")]
public partial class MultiTrackStateMachine<T, TRACK> : StateMachine<T> where TRACK : unmanaged, System.Enum
```

```csharp
// MultiTrackStateMachine.SideTrackStateChangedEvent.cs — add attribute:
[System.Obsolete("Use StateMachine<T>.PeerStateChangedEvent instead.")]
public struct SideTrackStateChangedEvent
```

**Verification:**
1. `MultiTrackStateMachine` usage produces compiler warning with migration message
2. Existing MultiTrack code still compiles and functions
3. `machine.Update()` does NOT tick any peers

**If this fails:** Remove `[Obsolete]` attributes. No functional changes to revert.

---

## User Code Comparison

```csharp
// BEFORE (MultiTrack)
enum EnemyTrack { Animation, Buffs }
var machine = new MultiTrackStateMachine<Enemy, EnemyTrack>(enemy);
machine.ChangeState<PatrolState>();
machine.ChangeSideTrackState<IdleAnim>(EnemyTrack.Animation);
machine.ChangeSideTrackState<NoBuffs>(EnemyTrack.Buffs);
machine.Update(); // ticks all
```

```csharp
// AFTER (Link)
var combat    = new StateMachine<Enemy>(enemy);
var animation = new StateMachine<Enemy>(enemy);
var buffs     = new StateMachine<Enemy>(enemy);

StateMachine<Enemy>.Link(combat, animation);
StateMachine<Enemy>.Link(combat, buffs);
StateMachine<Enemy>.Link(animation, buffs);

combat.ChangeState<PatrolState>();
animation.ChangeState<IdleAnim>();
buffs.ChangeState<NoBuffs>();

// Each ticked independently
combat.Update();
animation.Update();
buffs.Update();
```

---

## Communication Parity Matrix

| MultiTrack behavior | Link equivalent | Mechanism |
|---------------------|-----------------|-----------|
| Side tracks update after main | Caller ticks each machine independently | No auto-ticking |
| `SendEvent` cascades to all tracks | `SendEventToPeers` wraps as `ForwardedEvent<E>` | Direct lateral broadcast |
| Side track state change → main + others | `NotifyPeersOfStateChange` → `PeerStateChangedEvent` to all peers | Direct broadcast, no hub |
| `SideTrackStateChangedEvent` with TRACK | `PeerStateChangedEvent` with `source` reference | Machine reference replaces enum |
| `DeliverComponents` to tracks | Each peer has own components; nested sub-machines receive parent components as snapshot at entry via `IComponentUser` | No sharing (peers); snapshot at entry (hierarchy) |
| `OnSideTrackStateChanged` delegate | Per-machine `OnStateChanged` | Already exists on every machine |

---

## Verification Plan

### Automated Checks

- [ ] Project compiles without errors
- [ ] No warnings from new code (only from deprecated MultiTrack usage)

### Manual Verification

- [ ] **Hierarchical:** `ChangeState<SubMachine>()` → sub-machine entered, `Update()` cascades, `ChangeState<Other>()` exits cleanly
- [ ] **Parallel (Link):** `Link(a, b)` → both see each other in `Peers`; events bridged; `Unlink(a, b)` → both removed
- [ ] **Combined:** Nested sub-machine with linked parallel machines — all update, events flow correctly
- [ ] **Event bridging:** A sends event → B receives `ForwardedEvent<E>` with source=A; A changes state → B receives `PeerStateChangedEvent` with source=A
- [ ] **No duplication:** A-B-C fully linked, A sends event → B and C each receive once
- [ ] **Components:** Sub-machine receives parent components as snapshot at entry; local `SetComponent` overwrites snapshot; peers do NOT share
- [ ] **Re-entry guard:** Two peers sending events to each other → caps at depth 8, logs warning
- [ ] **No peer ticking:** `a.Update()` does NOT tick `b`
- [ ] **Debug path:** `GetStatePath()` returns `"SubMachine > LeafState"`
- [ ] **MultiTrack:** Existing MultiTrack code compiles with `[Obsolete]` warning
- [ ] **Root machines:** Unchanged behavior, no performance impact
- [ ] **Link guards:** `Link(a, a)` throws; `Link` during dispatch throws; `Unlink` when not linked throws
- [ ] `dotnet build` clean

### Acceptance Criteria Validation

| Criterion | How to Verify | Expected Result |
|-----------|---------------|-----------------|
| IState<T> implementation | `machine is IState<T>` | `true` |
| Update cascade (nested) | Breakpoint in nested state's Update | Hit every frame |
| No peer ticking | Breakpoint in peer state's Update | NOT hit from other peer's Update |
| Component snapshot (nested) | `[AutoComponent]` on sub-machine's child state | Receives parent's component via snapshot |
| Component isolation (peer) | `[AutoComponent]` on peer state | Does NOT receive other peer's component |
| Event bridging (peer) | Send event on A | B receives `ForwardedEvent<E>` |
| Component snapshot (nested) | `[AutoComponent]` on sub-machine's child state | Receives parent component from snapshot |
| Component snapshot staleness | Parent `SetComponent` after entry | Sub-machine retains original snapshot |
| Re-entry guard | Circular event forwarding | Caps at depth 8 |
| MultiTrack obsolete | Compile existing MultiTrack usage | Warning, not error |

---

## Rollback Plan

Steps are independently revertible in reverse order:

1. **Step 6:** Remove `[Obsolete]` attributes.
2. **Step 5:** Remove `IComponentUser` implementation and `[DisableAutoComponents]` from `StateMachine<T>`.
3. **Step 4:** Revert `SendEvent`/`PostStateChange` modifications, delete event struct files.
4. **Step 3:** Delete `StateMachine.AsState.cs`.
5. **Step 2:** Delete `StateMachine.Link.cs`.
6. **Step 1:** Revert Subject to `{ get; }`, remove parameterless ctor.

Full rollback: revert all, delete new files. No migrations, no external state.

---

## Risks

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Accidental nesting | Low | Medium | Explicit interface implementation — `IState<T>` methods only accessible via cast, which only `ChangeState` does internally |
| Incomplete peer graphs surprise users | Medium | Medium | Document: links are pairwise, not transitive. Future `LinkAll(params)`. |
| User forgets to tick a peer | Medium | Medium | Document: peers are independent. Intentional trade-off vs. auto-ticking. |
| Mutation during dispatch | Medium | High | `isPeerDispatching` guard throws with clear message. |
| Re-entry ping-pong between peers | Medium | High | `peerEventForwardingDepth` caps at 8, logs warning. |
| `Unlink` doesn't exit states | Low | Low | Intentional: peers don't own lifecycle. Document the pattern. |
| Stale components after parent `SetComponent` | Medium | Low | Snapshot semantics by design. Same constraint as any state — states don't get re-delivered after entry either. Document clearly. |
| Event forwarding overhead | Low | Low | `ForwardedEvent<E>` wrapping creates one struct per event per peer. Zero-cost when no peers (null check short-circuits). |

## Open Questions

- [ ] Should `NotifyPeersOfStateChange` be called before or after `stateChangingDepth--`? (Current: after, matching original plan)
- [ ] Should `ForwardedEvent<E>` carry both source and the receiving machine's own identity? (Current: source only)
- [ ] Future: should `LinkAll(params StateMachine<T>[])` be included in this plan or deferred?
