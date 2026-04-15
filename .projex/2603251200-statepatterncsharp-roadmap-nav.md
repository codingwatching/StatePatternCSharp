# statepatterncsharp Library Roadmap

> **Created:** 2026-03-25 | **Last Revised:** 2026-04-16
> **Author:** Claude (agent)
> **Scope:** Entire statepatterncsharp library — core, multi-track, Unity integration

---

## Vision

A C# state pattern library that works cleanly in both pure .NET and Unity contexts. States are first-class interfaces rather than nested abstract classes, transitions are resolver-driven to support serializable and ScriptableObject states, and the Unity layer is a thin opinionated wrapper that plugs into MonoBehaviour lifecycles without touching the core.

---

## Current Position

**As of 2026-03-29:**

Phases 1–3 and the new Phase 4 (IStateMachine Facade) are complete and closed. The core now has clean mutual-reference interfaces — `IStateMachine<T>`, `IState<T>`, and `IPopupState<T>` — with `StateMachine<T>` implementing `IStateMachine<T>`. States receive `IStateMachine<T>` in all lifecycle methods, decoupling them from the concrete class. Phase 5 (Hierarchical Machines) is now unblocked.

### Recent Progress
- `ObserverTransitionState` added — observer-pattern hook on transitions
- `"Context"/"Target"` renamed to `"Subject"` across the codebase — 2026-03 (01b4fae)
- Phase 1 IState extraction executed, audited, and closed — 2026-03-25 (39ada27)
- Phase 2 resolver executed and closed — 2026-03-27 (d5cef05)
- Phase 3 MonoBehaviour wrapper executed and closed — 2026-03-28 (45f4db7)
- Phase 4 IStateMachine facade + IState<T>/IPopupState<T> extraction executed and closed — 2026-03-29 (7aae3a7)
- Phase 4 addendum: T1/T2 members + SendEvent<S,E> promoted to IStateMachine<T> — 2026-04-16 (3cbf91e)

### Active Work
- None currently. Phase 5 (Hierarchical Machines) is the logical next step.
- `2603281200-noopstate-default-plan.md` — exists but execution status unknown; review before next cycle.

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

### Phase 5: Hierarchical Machines — **Current**

**Goal:** Enable `StateMachine<T>` to implement `IState<T>`, making machines nestable as states within parent machines. Unlocks hierarchical state trees with single-tick cascade.

**Milestones:**
- [ ] Design and proposal for machine-as-state capability
  - Ideation: `2603201530-statemachines-as-states-imagine.md`
  - Execution: *(no plan yet — Phase 4 prerequisite now met)*

**Exit Criteria:** A machine can be used as a state in a parent machine; `Update()` cascades through the tree; constructor problem resolved.

---

### Phase 6: Parallel Regions — **Future / Speculative**

**Goal:** Extend the machine model with parallel and trackless region support — machines that run multiple concurrent state tracks, with dynamic attach/detach of sub-machines.

**Milestones:**
- [ ] Evaluate multitrack validity in context of machines-as-states
  - Ideation: `2603251530-multitrack-validity-with-machines-as-states-eval.md`
  - Execution: *(no plan yet)*
- [ ] Design trackless parallel regions
  - Ideation: `2603251545-trackless-parallel-regions-imagine.md`
  - Execution: *(no plan yet)*
- [ ] Attach/detach parallel machine API
  - Ideation: `2603251600-attach-detach-parallel-machines-proposal.md` (proposal — not yet a plan)
  - Execution: *(no plan yet)*

**Exit Criteria:** *(to be defined when proposal matures into a plan)*

---

## Priorities

**Current focus:** Phase 5 (Hierarchical Machines) — all prerequisites met. Ideation seed exists (`2603201530-statemachines-as-states-imagine.md`); next step is a proposal or plan.

**Next up:** Phase 6 (Parallel Regions) — deferred until Phase 5 lands.

**Deferred:** Phase 6 — three ideation seeds exist but no plans. Multitrack validity eval and trackless parallel regions design are both unstarted.

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
