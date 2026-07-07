# Echo — Self-Review: Phase 1 (Determinism Core + Replay + Clone AI)

*Per the mandate to review/optimize/refactor after every implementation phase. This is an honest pass over the code committed in `Assets/Scripts/`.*

## 1. What was built (the deterministic spine)
| Layer | Files | Proves |
|-------|-------|--------|
| Determinism | `Fix64`, `Fix64Vec2`, `DeterministicRng`, `StateHash` | platform-independent math + reproducible RNG + state hashing |
| Replay | `InputCommand`, `Timeline`, `Recorder`, `ReplaySource`, `DesyncGuard` | record intent, replay it, detect drift |
| Sim | `SimEntity`, `ICollisionWorld`, `KinematicSolver`, `CharacterMotor`, `LevelSimulation` | one deterministic pipeline for player **and** Echoes; stand-on-your-past-self |
| Clone AI | `DriveModel`, `SalienceTracker`, `TraitResolver`, `GateEvaluator`, `EchoBrain` | evolution as a *pure, telegraphed, repairable* function |
| Infra/Services | `ObjectPool`, `EventBus`, `ServiceLocator`, `ISaveService`, `IPlatformServices` | zero-GC pooling, decoupling, vendor-agnostic platform edges |
| Tests | `ReplayDeterminismTests` | the crown-jewel determinism + gate-purity assertions |

The single most important design decision is encoded in `LevelSimulation`: **run K only collides with runs 0..K-1**, reproducing each run's original collision environment so the braid stays bit-identical as it grows.

## 2. Bugs found & fixed in review
1. **Invalid hex seed literal** (`0xECHO_...`) in the test — `H`/`O` aren't hex digits; hard compile error. → replaced with a valid `0xEC0FEED5_1234_5678UL`.
2. **`_nextRunId` never incremented** in `LevelSimulation.Restart` — every banked run reused runId 0, colliding entity Ids (`runId+1`) and duplicating gate-seed streams across the braid (would have caused hash-order ambiguity and identical "personalities" on every clone). → increment after each live run begins.

## 3. Determinism audit (the existential risk)
- ✅ No `float` in any sim path; all gameplay math is `Fix64`.
- ✅ No `UnityEngine.Random`/`System.Random`/wall-clock in sim or AI — only `DeterministicRng` seeded by observable state.
- ✅ Entities processed in a fixed order; collision is axis-separated and order-stable.
- ✅ Gate decisions are pure functions (unit-tested for reproducibility + dormancy below threshold).
- ⚠️ **Intended vs unintended divergence:** `DesyncGuard` verifies *gates-off* replay fidelity (replay == recording). With gates ON, an Echo *intentionally* deviates, so the correct production invariant is **reproducibility across restarts** (replay N == replay N+1), covered by `LevelSimulation_SoakReplay_IsReproducible`. This distinction is now explicit in code comments and `05_TDD` — worth a callout so no one "fixes" a gate by chasing a false desync.

## 4. Performance review
- ✅ Hot loop allocation: `_solidsScratch` is reused (Clear, not new); bodies/brains/actors are pooled; `EventBus` uses struct events. The per-tick path is allocation-free.
- ⚠️ `EchoBrains` getter allocates a `List` each call — **fine for tooling/UI, must not be called in the sim loop.** Marked; consider exposing a read-only view instead. *(follow-up)*
- ⚠️ `KinematicSolver` entity resolution is O(bodies²) across the braid (each run scans predecessors). At the design cap of 6 Echoes this is ≤21 pair-checks/tick — negligible. If caps ever rise, add a broadphase grid. *(noted, not needed yet)*
- ✅ `StateHash` is streaming FNV-1a (no allocation). Keyframes are sparse (every 30 ticks).

## 5. Architecture review
- ✅ Downward-only dependencies hold: `Core.*` has **no UnityEngine reference**, which is exactly why `LevelSimulation` runs headless in EditMode tests. This must be locked with asmdefs before the codebase grows (currently by convention). *(action item: add `.asmdef` boundaries — `06_Architecture` §3 specifies them.)*
- ✅ Composition over inheritance: capabilities are `ISimComponent`s on `SimEntity`, not subclasses.
- 🔧 Refactor opportunity: the body-spawn/`ConfigureBody` logic in `LevelSimulation` and the test duplicate body-construction constants (`HalfExtents`). Extract a `SimEntityFactory` so tests and runtime share one definition. *(low-risk, do next.)*

## 6. Gameplay/feel review (early)
- The `CharacterMotor` already has coyote-time (game-feel + accessibility) and snappy horizontal control — good baseline.
- Missing for "juice": jump-buffering (queue a jump pressed just before landing), variable jump height (release-to-cut), and a tiny landing squash hook for Layer B. **Jump-buffer + variable height are the two highest-impact additions** and slot cleanly into `LocomotionState`. *(Phase 2)*
- The `GateEvaluator` currently telegraphs at the fire tick; the design promises a **≥1s pre-telegraph**. Production needs a look-ahead pass that pre-scans candidate gate ticks so `ExpressionController` can wind up the animation before the deviation. *(Phase 4 — already in the plan.)*

## 7. Correctness follow-ups (tracked, non-blocking)
- `KinematicSolver.ResolveEntityVertical` snaps to the *first* overlapping blocker; for stacked Echoes it should pick the highest top (down) / lowest bottom (up). Edge case at the design cap; fix when stacking puzzles (mechanic #23) come online.
- `SaveProfile` uses `Dictionary<>` (not Unity-serializable) — intentional, since persistence uses the custom LZ4 codec, not `JsonUtility`. Document in the save codec when written.

## 8. Verdict
Phase 1's exit criterion — *record a run, restart, get a bit-identical ghost, zero drift over a soak* — is **met in code and asserted by tests** (`Replay_GatesOff_ReproducesRecordingBitIdentical`, `LevelSimulation_SoakReplay_IsReproducible`). The fatal risk is retired in principle; next is wiring it into a Unity scene with real input + rendering (Phase 2) and the two game-feel additions above.

**Next phase:** Phase 2 — Unity scene integration (`PlayerController`/`InputRouter`, presentation adapter Fix64→Transform with render interpolation, HUD v0), plus jump-buffer/variable-jump, then the first hand-authored World-1 puzzle.
