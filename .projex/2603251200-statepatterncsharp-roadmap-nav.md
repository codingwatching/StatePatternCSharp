# statepatterncsharp Library Roadmap

> **Created:** 2026-03-25 | **Last Revised:** 2026-03-27
> **Author:** Claude (agent)
> **Scope:** Entire statepatterncsharp library — core, multi-track, Unity integration

---

## Vision

A C# state pattern library that works cleanly in both pure .NET and Unity contexts. States are first-class interfaces rather than nested abstract classes, transitions are resolver-driven to support serializable and ScriptableObject states, and the Unity layer is a thin opinionated wrapper that plugs into MonoBehaviour lifecycles without touching the core.

---

## Current Position

**As of 2026-03-27:**

Phases 1 and 2 are complete and closed. The core now uses a top-level `IState` interface with a pluggable resolver layer — `ChangeState<S>()` no longer requires `new()` and accepts externally-created states. Phase 3 (MonoBehaviour wrapper) has a plan ready to execute.

### Recent Progress
- `ObserverTransitionState` added — observer-pattern hook on transitions
- `"Context"/"Target"` renamed to `"Subject"` across the codebase — 2026-03 (01b4fae)
- Three proposals drafted, three plans derived — 2026-03-12
- Phase 1 IState extraction executed, audited, and closed — 2026-03-25 (39ada27)
- Phase 2 resolver executed and closed — 2026-03-27 (d5cef05)

### Active Work
- Phase 3 plan ready to execute: `20260312-monobehaviour-statemachine-plan.md`

### Known Blockers
- None. Execution order must be respected: resolver second, MonoBehaviour wrapper third.

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

### Phase 3: Unity Integration — **Current**

**Goal:** Ship `MonoBehaviourStateMachine<T>` — a Unity MonoBehaviour wrapper that auto-discovers `IState` components, registers serialized ScriptableObject states via the resolver, and drives machine updates from Unity's lifecycle.

**Milestones:**
- [ ] `MonoBehaviourStateMachine<T>` implemented in `unity/`
  - Ideation: `20260311-monobehaviour-statemachine-proposal.md`
  - Execution: `20260312-monobehaviour-statemachine-plan.md`

**Exit Criteria:** `unity/MonoBehaviourStateMachine.cs` exists; compiles under Unity's .NET Standard 2.1 profile; auto-component discovery and ScriptableObject registration work end-to-end.

---

### Phase 4: Hierarchical Machines — **Future / Speculative**

**Goal:** Enable `StateMachine<T>` to implement `IState`, making machines nestable as states within parent machines. Unlocks hierarchical state trees with single-tick cascade.

**Milestones:**
- [ ] Design and proposal for machine-as-state capability
  - Ideation: `2603201530-statemachines-as-states-imagine.md` (imagination — not yet a proposal)
  - Execution: *(no plan yet — depends on Phase 1-3 landing)*

**Exit Criteria:** A machine can be used as a state in a parent machine; `Update()` cascades through the tree; constructor problem resolved.

---

### Phase 5: Parallel Regions — **Future / Speculative**

**Goal:** Extend the machine model with parallel and trackless region support — machines that run multiple concurrent state tracks, with dynamic attach/detach of sub-machines.

**Milestones:**
- [ ] Evaluate multitrack validity in context of machines-as-states
  - Ideation: `2603251530-multitrack-validity-with-machines-as-states-eval.md`
  - Execution: *(no plan yet)*
- [ ] Design trackless parallel regions
  - Ideation: `2603251545-trackless-parallel-regions-imagine.md` (imagination — not yet a proposal)
  - Execution: *(no plan yet)*
- [ ] Attach/detach parallel machine API
  - Ideation: `2603251600-attach-detach-parallel-machines-proposal.md` (proposal — not yet a plan)
  - Execution: *(no plan yet)*

**Exit Criteria:** *(to be defined when proposal matures into a plan)*

---

## Priorities

**Current focus:** Execute Phase 3 (`20260312-monobehaviour-statemachine-plan.md`) — plan is ready, resolver prerequisite is now met.

**Next up:** Phase 4 hierarchical machines — depends on Phase 3 landing.

**Deferred:** Phases 4 and 5 — seeds only; no plans exist. Phase 5 ideation is active but not ready for execution.

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
