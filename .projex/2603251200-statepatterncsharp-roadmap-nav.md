# statepatterncsharp Library Roadmap

> **Created:** 2026-03-25 | **Last Revised:** 2026-04-16 (2)
> **Author:** Claude (agent)
> **Scope:** Entire statepatterncsharp library — core, multi-track, Unity integration

---

## Vision

A C# state pattern library that works cleanly in both pure .NET and Unity contexts. States are first-class interfaces rather than nested abstract classes, transitions are resolver-driven to support serializable and ScriptableObject states, and the Unity layer is a thin opinionated wrapper that plugs into MonoBehaviour lifecycles without touching the core.

---

## Current Position

**As of 2026-04-16:**

Phases 1–5 complete and closed. `StateMachine<T>` implements `IState<T>` (machines nestable as states), `Link()`/`Unlink()` provide symmetric peer rings with lateral event broadcast, `MultiTrackStateMachine` is deprecated, and sub-machines receive parent components via entry-time snapshot (`IComponentUser`). Phase 6 (Parallel Regions) is now Current — though the Link/Unlink peer ring already covers the attach/detach proposal; the remaining milestones (multitrack validity eval, trackless parallel regions) need re-evaluation in light of Phase 5's scope.

### Recent Progress
- `ObserverTransitionState` added — observer-pattern hook on transitions
- `"Context"/"Target"` renamed to `"Subject"` across the codebase — 2026-03 (01b4fae)
- Phase 1 IState extraction executed, audited, and closed — 2026-03-25 (39ada27)
- Phase 2 resolver executed and closed — 2026-03-27 (d5cef05)
- Phase 3 MonoBehaviour wrapper executed and closed — 2026-03-28 (45f4db7)
- Phase 4 IStateMachine facade + IState<T>/IPopupState<T> extraction executed and closed — 2026-03-29 (7aae3a7)
- Phase 4 addendum: T1/T2 members + SendEvent<S,E> promoted to IStateMachine<T> — 2026-04-16 (3cbf91e)
- Phase 5 hierarchical machines + peer ring executed and closed — 2026-04-01 (27d5a22)

### Active Work
- Phase 6 (Parallel Regions) — re-evaluation needed; Link/Unlink may already satisfy the attach/detach milestone.
- `2603301500-sourcegen-state-resolver-plan.md` — untracked active plan; status unknown.
- `2604161215-mb-statemachine-direct-impl-plan.md` — untracked active plan; status unknown.

### Known Blockers
- None.

---

## Roadmap

### Phase 1: IState Foundation — **Done**

**Goal:** Replace the nested abstract class `StateMachine<T>.State` with a top-level `IState` interface, enabling the machine to eventually implement its own state contract (prerequisite for all downstream work).

**Milestones:**
- [x] `State` → `IState` extraction complete — rename nested abstract class to top-level interface across all source files
  - Ideation: `20260311-state-to-istate-interface-proposal.md`
  - Execution: `20260312-istate-extraction-plan.md`, `20260312-istate-extraction-log.md`
  - Closed: `20260325-istate-extraction-walkthrough.md`, `20260325-istate-extraction-audit.md`

**Exit Criteria:** `StateMachine.State.cs` replaced by `StateMachine.IState.cs`; `dotnet build` clean; no remaining `abstract class State` references in production code.

---

### Phase 2: State Resolution — **Done**

**Goal:** Introduce `IStateResolver` so `ChangeState<S>()` can accept externally-created state instances (ScriptableObject, DI, serialized) rather than only activator-constructed ones. Achieves parity between cache-hit and cache-miss transition paths.

**Milestones:**
- [x] `IStateResolver` interface defined and wired into `StateMachine<T>` and `MultiTrackStateMachine`
  - Ideation: `20260311-serializable-states-and-changestate-parity-proposal.md`
  - Review: `2603261200-istate-resolver-plan-review.md`
  - Execution: `20260312-istate-resolver-plan.md`
  - Closed: `2603261130-istate-resolver-plan-walkthrough.md`

**Exit Criteria:** `IStateResolver.cs` exists; `StateMachine<T>` and `MultiTrackStateMachine` accept an optional resolver; `ObserverTransitionState` constraints relaxed to `IState`.

---

### Phase 3: Unity Integration — **Done**

**Goal:** Ship `MonoBehaviourStateMachine<T>` — a Unity MonoBehaviour wrapper that auto-discovers `IState` components, registers serialized ScriptableObject states via the resolver, and drives machine updates from Unity's lifecycle.

**Milestones:**
- [x] `MonoBehaviourStateMachine<T>` implemented in `unity/`
  - Ideation: `20260311-monobehaviour-statemachine-proposal.md`
  - Red team: `2603272256-monobehaviour-statemachine-plan-redteam.md`
  - Execution: `20260312-monobehaviour-statemachine-plan.md` (45f4db7)

**Exit Criteria:** `unity/MonoBehaviourStateMachine.cs` exists; compiles under Unity's .NET Standard 2.1 profile; auto-component discovery and ScriptableObject registration work end-to-end. ✓

---

### Phase 4: IStateMachine Facade — **Done**

**Goal:** Extract `StateMachine<T>.IState` and `StateMachine<T>.IPopupState` to top-level `IState<T>` and `IPopupState<T>` interfaces. Introduce `IStateMachine`/`IStateMachine<T>` facade interfaces with clean mutual references. `StateMachine<T>` implements `IStateMachine<T>`. All state lifecycle methods receive `IStateMachine<T>` instead of the concrete class.

**Milestones:**
- [x] `IStateMachine<T>` facade + `IState<T>` + `IPopupState<T>` extracted and wired
  - Ideation: `2603281430-statemachine-as-interface-eval.md`, `2603281600-statemachine-driver-interface-proposal.md`
  - Red team: `2603281700-istatemachine-and-istate-extraction-plan-redteam.md`
  - Execution: `2603281630-istatemachine-and-istate-extraction-plan.md` → `2603281630-istatemachine-and-istate-extraction-walkthrough.md` (7aae3a7)
- [x] Interface gap closed — T1/T2 setup members + `SendEvent<S,E>` promoted from `StateMachine<T>` to `IStateMachine<T>`
  - Patch: `2604161200-istatemachine-interface-gap-patch.md` (3cbf91e)

**Exit Criteria:** `IStateMachine.cs`, `IState.cs`, `IPopupState.cs` exist at namespace level; nested interfaces deleted; `StateMachine<T> : IStateMachine<T>`; all state method signatures use `IStateMachine<T>`. ✓

---

### Phase 5: Hierarchical Machines + Peer Ring — **Done**

**Goal:** Enable `StateMachine<T>` to implement `IState<T>`, making machines nestable as states within parent machines. Unlocks hierarchical state trees with single-tick cascade. Also unified parallel composition via symmetric `Link()`/`Unlink()` peer ring, replacing `MultiTrackStateMachine`.

**Milestones:**
- [x] Unified hierarchical + parallel machine composition
  - Ideation: `2603201530-statemachines-as-states-imagine.md`, `2603251600-attach-detach-parallel-machines-proposal.md`
  - Execution: `2603291500-machines-as-states-plan.md` → `2603291500-machines-as-states-walkthrough.md` (27d5a22, 2026-04-01)

**Exit Criteria:** `StateMachine<T>` implements `IState<T>`; `Update()` cascades through nested machines; parameterless ctor + deferred subject; `Link()`/`Unlink()` peer ring with lateral event broadcast; `MultiTrackStateMachine` deprecated. ✓

---

### Phase 6: Parallel Regions — **Current**

**Goal:** Evaluate remaining parallel-region milestones in light of Phase 5's peer ring. The attach/detach proposal is already superseded by `Link()`/`Unlink()`. Remaining open questions: multitrack validity (is `MultiTrackStateMachine` still needed at all?) and trackless parallel regions (any gaps not covered by the peer ring?).

**Milestones:**
- [ ] Evaluate multitrack validity in context of machines-as-states + peer ring
  - Ideation: `2603251530-multitrack-validity-with-machines-as-states-eval.md`
  - Execution: *(re-evaluation needed — Phase 5 may have superseded this)*
- [ ] Design trackless parallel regions (if any gap remains after Phase 5 review)
  - Ideation: `2603251545-trackless-parallel-regions-imagine.md`
  - Execution: *(pending milestone 1 eval)*
- [x] Attach/detach parallel machine API — **superseded** by `Link()`/`Unlink()` (Phase 5)
  - Ideation: `2603251600-attach-detach-parallel-machines-proposal.md`

**Exit Criteria:** *(to be defined after multitrack validity eval)*

---

## Priorities

**Current focus:** Phase 6 (Parallel Regions) — re-evaluation. Phase 5's peer ring supersedes the attach/detach milestone. First step: eval whether `MultiTrackStateMachine` has any remaining validity, and whether trackless parallel regions represent a gap not covered by the peer ring.

**Deferred:** Trackless parallel regions design — pending multitrack validity eval.

**Closed:** Phase 5 milestones (hierarchical machines + peer ring) — fully executed and closed 2026-04-01.

---

## Open Questions

- [ ] Should proposals be updated to `Accepted` status to reflect that plans have been derived from them? (Housekeeping — no functional impact)

---

## Revision Log

| Date | Summary of Changes |
|------|--------------------|
| 2026-03-25 | Initial roadmap created |
| 2026-03-26 | Phase 1 marked Done (walkthrough + audit linked); Phase 2 promoted to Current; Phase 5 Parallel Regions stub added for three new ideation seeds |
| 2026-03-27 | Phase 2 marked Done (walkthrough linked, d5cef05); Phase 3 promoted to Current; priorities updated to Phase 3 execution |
| 2026-03-29 | Phase 3 marked Done (45f4db7); Phase 4 (IStateMachine Facade) inserted as Done (7aae3a7); old Phase 4→5 (Hierarchical Machines, Current), old Phase 5→6 (Parallel Regions, Future); priorities updated to Phase 5 |
| 2026-04-16 | Phase 4 addendum: interface gap patch (3cbf91e) — Cache<S>, SetComponent, StateResolver, DeliverOnlyOnceForCachedStates, SendEvent<S,E> promoted to IStateMachine<T> |
| 2026-04-16 | Phase 5 marked Done (27d5a22, 2026-04-01) — IState<T> on StateMachine<T>, Link/Unlink peer ring, ForwardedEvent/PeerStateChangedEvent, IComponentUser snapshot, MultiTrack deprecated; Phase 6 promoted to Current with attach/detach milestone marked superseded |
