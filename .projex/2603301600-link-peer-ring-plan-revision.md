# Link/Unlink Peer Ring -- Plan Revision for Parallel Composition

> **Status:** Incorporated into `2603291500-machines-as-states-plan.md` (2026-03-31)
> **Created:** 2026-03-30
> **Author:** Claude (agent)
> **Revises:** `2603291500-machines-as-states-plan.md` (Steps 2, 4, 5, 6)
> **Related:** `2603251600-attach-detach-parallel-machines-proposal.md` (superseded by this revision)

---

## Summary

Revise the parallel composition model in the machines-as-states plan. Replace `Attach()/Detach()` with `Link()/Unlink()` -- a symmetric peer ring. `Host` remains exclusively for hierarchical nesting (machines-as-states). Peers are equals: no ownership, no automatic ticking, no component sharing, direct lateral event broadcast.

**Scope:** Steps 2, 4, 5, 6 of `2603291500-machines-as-states-plan.md`. Steps 1 and 3 are unchanged.

---

## Objective

### Problem

The existing plan unified hierarchy and parallelism under a single `Host` property with `Attach()/Detach()`. This conflates two distinct relationships:

- **Hierarchical:** Parent owns child. Parent ticks child. Events flow up/down. `Host` is correct here.
- **Parallel:** Peers are equals. No ownership. Events broadcast laterally. `Host` is wrong here.

"Host" implies ownership. Attached machines are guests of a host -- that's hierarchy, not parallelism. The attach model smuggles Phase 5 semantics into Phase 6.

### Success Criteria

- [ ] `Host` is set ONLY by machines-as-states nesting (Step 3's `IState<T>.OnEntered`)
- [ ] `Link(a, b)` / `Unlink(a, b)` -- static, symmetric, bidirectional peer ring
- [ ] Peers broadcast events laterally -- no hub, no "up then redistribute"
- [ ] Peers do NOT tick each other -- caller ticks all machines independently
- [ ] Peers do NOT share components -- each machine has its own
- [ ] `PeerStateChangedEvent` replaces `AttachedStateChangedEvent`
- [ ] `ForwardedEvent<E>` envelope unchanged
- [ ] Re-entry guard prevents loops between peers
- [ ] `MultiTrackStateMachine` marked `[Obsolete]` with Link migration message
- [ ] All other success criteria from the original plan remain

### Out of Scope

- `LinkAll(params)` convenience method (noted as future)
- Transitive link propagation (links are pairwise by design)
- Shared component registry for peers

---

## Context

### What Stays from the Original Plan

| Step | Content | Status |
|------|---------|--------|
| Step 1 | Foundation -- Subject settable, parameterless ctor | **Unchanged** |
| Step 3 | `IState<T>` on `StateMachine<T>` | **Unchanged** (Host set here only) |

### What Changes

| Step | Original | Revised |
|------|----------|---------|
| Step 2 | `Host` + `Attach()/Detach()` unified in `StateMachine.Attach.cs` | `Host` moves to `AsState.cs` (hierarchy-only). New `StateMachine.Link.cs` with `Link()/Unlink()` |
| Step 4 | Hub-and-spoke event bridging via Host | Direct lateral broadcast between peers |
| Step 5 | Component prototype chain via Host for both hierarchy and parallel | Hierarchy only. Peers are independent. |
| Step 6 | `UpdateAttachedMachines()` in `Update()` | No peer ticking. `Update()` unchanged from current code. |

### Key Files

| File | Role | Change |
|------|------|--------|
| `StateMachine.cs` | Core | Modify `SendEvent` (+peers), `PostStateChange` (+peers). Subject setter, parameterless ctor (Step 1). Component chain (Step 5). |
| `StateMachine.Link.cs` | **New** | Replaces `Attach.cs`. `peers` field, `Link()`, `Unlink()`, `SendEventToPeers()`, `NotifyPeersOfStateChange()`, `ReceiveFromPeer()`, guards. |
| `StateMachine.AsState.cs` | **New** | `Host` property lives here (was in `Attach.cs`). `IState<T>` implementation. |
| `StateMachine.PeerStateChangedEvent.cs` | **New** | Replaces `AttachedStateChangedEvent`. `{ source, from, to }` |
| `StateMachine.ForwardedEvent.cs` | **New** | Generic envelope. Unchanged from original plan. |
| `MultiTrackStateMachine.cs` | Deprecate | `[Obsolete("Use StateMachine<T>.Link()")]` |
| `MultiTrackStateMachine.SideTrackStateChangedEvent.cs` | Deprecate | `[Obsolete("Use PeerStateChangedEvent")]` |

### Constraints

- Links are pairwise, not transitive. `Link(A,B)` + `Link(B,C)` does NOT mean A sees C.
- `Unlink()` does NOT exit either machine's state. Peers don't own each other's lifecycle.
- `Link()/Unlink()` throw if called during peer event dispatch (mutation guard).

---

## Implementation

### Overview

Four revised steps (2, 4, 5, 6). Steps 1 and 3 unchanged from `2603291500-machines-as-states-plan.md`.

---

### Step 2 (Revised): Host (Hierarchy-Only) + Link/Unlink Peer Ring

**Objective:** Split the unified `Host`+`Attach` into two distinct primitives.
**Confidence:** High
**Depends on:** None (parallel with Step 1)

**Files:**
- `StateMachine.Link.cs` **(new)**

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

**Host property** moves to `StateMachine.AsState.cs` (Step 3). It is set exclusively by `IState<T>.OnEntered` and cleared by `IState<T>.OnLeaving`. Peers never touch it.

**Rationale:**
- Static `Link(a, b)` -- symmetric by construction. No `a.Link(b)` that implies `a` owns the action.
- `Unlink` does not interfere with state lifecycle -- peers are independent.
- Null cleanup on empty lists keeps fast-path `if (peers == null) return;` free.
- Mutation guard (`isPeerDispatching`) prevents list modification during event iteration.

**Verification:**
- `Link(a, b)` -- `a.Peers` contains `b`, `b.Peers` contains `a`
- `Unlink(a, b)` -- both removed, lists nullified
- `Link(a, a)` -- throws
- `Link(a, b)` twice -- throws
- `Unlink` when not linked -- throws
- `Link` during dispatch -- throws

---

### Step 4 (Revised): Event Bridging -- Direct Lateral Broadcast

**Objective:** Symmetric event communication between peers. No hub-and-spoke.
**Confidence:** Medium -- re-entry loop risk, but simpler than the original hub model.
**Depends on:** Step 2 (Link)

**Files:**
- `StateMachine.cs` (modify `SendEvent`, `PostStateChange`)
- `StateMachine.Link.cs` (add event forwarding methods + re-entry guard)
- `StateMachine.PeerStateChangedEvent.cs` **(new)**
- `StateMachine.ForwardedEvent.cs` **(new)**

**Key design: NO re-broadcast in `ReceiveFromPeer`.**

The old plan's `ReceiveFromAttached` re-broadcast to siblings (skip source). In the peer ring, the source machine broadcasts to ALL its own peers directly. Re-broadcast would cause duplication in fully-connected graphs and loops in chains.

**Changes to `StateMachine.cs`:**

`SendEvent<E>` (after line ~394):
```csharp
// Add after SendEventToPopupStates(ev):
SendEventToPeers(ev);
```

`PostStateChange` (after line ~245):
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

**Event flow -- peer A changes state (A-B-C fully linked):**

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

**New file `StateMachine.ForwardedEvent.cs`** (unchanged from original plan):

```csharp
namespace BAStudio.StatePattern
{
    public partial class StateMachine<T>
    {
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

**Opt-in source discrimination (same as original proposal):**

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

---

### Step 5 (Revised): Component Chain -- Hierarchy Only

**Objective:** Component prototype chain walks `Host`. Peers are independent.
**Confidence:** High
**Depends on:** Step 3 (Host)

**Code changes identical to original plan.** `TryResolveComponent` walks `Host`:

```csharp
for (var m = this; m != null; m = m.Host as StateMachine<T>)
```

Since peers never set `Host`, the chain stops at the peer machine itself. No code change needed to exclude peers -- the separation is structural.

Shared components across peers = user registers on each:
```csharp
var shared = new SharedService();
machineA.SetComponent(shared);
machineB.SetComponent(shared);
```

---

### Step 6 (Revised): No Peer Ticking + Deprecation

**Objective:** Peers tick independently. MultiTrack deprecated.
**Confidence:** High
**Depends on:** Step 2

**`Update()` is UNCHANGED from current code.** No `UpdateAttachedMachines()` added.

User code:
```csharp
// BEFORE (MultiTrack -- host ticks side tracks automatically)
machine.Update(); // ticks main + all side tracks

// AFTER (Link -- each machine ticked independently)
combat.Update();
animation.Update();
buffs.Update();
```

Hierarchy still cascades via `IState<T>.Update` delegation (Step 3).

**MultiTrack deprecation:**
```csharp
[Obsolete("Use StateMachine<T> with Link() for parallel composition.")]
public partial class MultiTrackStateMachine<T, TRACK> : StateMachine<T> ...

[Obsolete("Use StateMachine<T>.PeerStateChangedEvent instead.")]
public struct SideTrackStateChangedEvent ...
```

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
| Side track state change -> main + others | `NotifyPeersOfStateChange` -> `PeerStateChangedEvent` to all peers | Direct broadcast, no hub |
| `SideTrackStateChangedEvent` with TRACK | `PeerStateChangedEvent` with `source` reference | Machine reference replaces enum |
| `DeliverComponents` to tracks | Each peer has own components | No sharing |
| `OnSideTrackStateChanged` delegate | Per-machine `OnStateChanged` | Already exists on every machine |

---

## Risks

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Incomplete peer graphs surprise users | Medium | Medium | Document: links are pairwise, not transitive. Future `LinkAll(params)`. |
| User forgets to tick a peer | Medium | Medium | Document: peers are independent. Intentional trade-off. |
| Mutation during dispatch | Medium | High | `isPeerDispatching` guard throws with clear message. |
| Re-entry ping-pong between peers | Medium | High | `peerEventForwardingDepth` caps at 8, logs warning. |
| `Unlink` doesn't exit states (unlike old `Detach`) | Low | Low | Intentional: peers don't own lifecycle. Document the pattern. |

---

## Open Questions

- [ ] Should `NotifyPeersOfStateChange` be called before or after `stateChangingDepth--`? (Current: after, matching original plan)
- [ ] Should `ForwardedEvent<E>` carry both source and the receiving machine's own identity? (Current: source only)
- [ ] Future: should `LinkAll(params StateMachine<T>[])` be included in this plan or deferred?

---

## Verification Plan

- [ ] `Link(a, b)` -- both see each other in Peers; `Unlink` -- both removed
- [ ] A sends event -- B receives `ForwardedEvent<E>` with source=A
- [ ] A changes state -- B receives `PeerStateChangedEvent` with source=A
- [ ] A-B-C fully linked, A sends event -- B and C each receive once (no duplication)
- [ ] `Link()` during dispatch throws
- [ ] Re-entry caps at depth 8
- [ ] `a.Update()` does NOT tick `b`
- [ ] Nested child inherits components from Host; peers do not share
- [ ] Existing root machine behavior unchanged, zero overhead
- [ ] MultiTrack compiles with `[Obsolete]` warning
- [ ] `dotnet build` clean

---

## Rollback Plan

Steps 2, 4, 5, 6 are independently revertible in reverse:

1. **Step 6:** Remove `[Obsolete]` attributes. Update unchanged.
2. **Step 5:** Revert `DeliverComponents` changes. (Same as original plan.)
3. **Step 4:** Revert `SendEvent`/`PostStateChange` modifications. Delete event struct files.
4. **Step 2:** Delete `StateMachine.Link.cs`.

Full rollback: revert all, delete new files. No migrations, no external state.
