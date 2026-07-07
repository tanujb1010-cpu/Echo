# Echo — Self-Review: Phase 2 (Playable Core Loop)

*Reviewed after implementation, per the standing mandate.*

## 1. What was built
| Area | Files | Verified by |
|------|-------|-------------|
| Game feel | `CharacterMotor` (+`MotorTuning`,`LocomotionState`): jump-buffer, coyote, **variable jump height** | headless tests (apex full 3.62 vs tap 2.12) |
| First mechanic (#1 Held Plate) | `PressurePlate`, `Door`, `InteractionSystem`, `PlateDoorModule` | headless puzzle solve + control |
| Pluggable mechanics | `ILevelModule` (Core hook) so Gameplay mechanics register without Core depending on them | architecture review |
| Sim integration | `LevelSimulation`: module ticking, module solids, `CollectRender` snapshot | soak still green |
| Input | `IInputProvider` + `InputRouter` (pure), `LegacyInputProvider` (Unity) | pure & device-agnostic |
| Presentation | `SimView` (interpolated, pooled), `SimRunner` (fixed-step driver + restart), `HudV0` | Unity (see §4) |
| Content pipeline | `LevelDefinition` (SO), `LevelBuilder`, `SampleLevels` (W1-L1) | builds the tested puzzle |
| Composition root | `GameBootstrap` (services behind interfaces) | architecture review |

## 2. Test results — 67/67 passing (headless, real code)
New since Phase 1:
- **Game feel:** buffered pre-landing jump fires; full-hold clearly out-climbs a short tap.
- **Held-Plate puzzle (#1):** a lone player is blocked by the closed door (control); a **past self holding the plate opens it** so the live self reaches the exit; **solution is reproducible**.
- All Phase-1 determinism/soak/gate/pool tests still green after the integration changes.

## 3. Bugs / issues found & fixed during Phase 2
1. **Architecture violation caught mid-implementation:** my first cut made `Core.Sim.LevelSimulation` `using Echo.Gameplay.*` — an *upward* dependency that breaks the downward-only rule and would have prevented the core from compiling without Gameplay. **Refactored to `ILevelModule`**: Core hosts abstract modules; Gameplay supplies the concrete `PlateDoorModule`. Core stays Unity- and Gameplay-free (still compiles + tests headless — proof the boundary holds).
2. **Spawn-overlap pop** (carried from Phase-1 analysis): fixed in the solver and now regression-guarded by a dedicated test.
3. **Harness bug** (not game code): `maxY` initialized to the drop height masked the no-jump case. Fixed; unrelated to engine correctness.

## 4. Unity layer — review status (honest)
The `Assets/Scripts/Unity/` files reference `UnityEngine`, so they are **not** exercised by the headless harness (they're excluded from its build). They are written to compile against Unity 2022 LTS and reviewed by inspection. **They still need in-editor verification** — that is the one thing I cannot do without the editor installed. Specific things to check on first Play:
- `SimRunner` accumulator: confirm no input lag at high refresh; confirm interpolation `alpha` looks smooth (it reads `_acc/SimDt`).
- View pooling across restart: `SyncViews(firstFrame:true)` should teleport (no smear) on restart; verify echo views recycle (no leak) as the braid grows/shrinks.
- Fallback view/sprite path renders when no prefab is assigned.
- Restart hold-to-confirm timing feels right (0.4 s default).

To run: Unity 2022 LTS, 2D (URP), drop in `Assets/`, scene with a Camera + one GameObject holding `GameBootstrap` + `SimRunner` + `HudV0`. `SimRunner` self-builds W1-L1 and a fallback sprite, so it plays immediately.

## 5. Performance review
- `SimRunner`/`SyncViews` reuse `_render`, `_seen`, `_toRemove` lists → no per-frame GC in the steady state. View create/release goes through `ObjectPool<SimView>`.
- `LevelSimulation.Step` still allocation-free in the steady state; module ticking reuses `_allBodies`.
- ⚠️ `EchoBrains` getter still allocates (tooling-only; keep out of hot paths) — unchanged from Phase 1.
- Door/plate counts are tiny; `InteractionSystem` is O(plates×bodies + doors×plates), negligible at design scale.

## 6. Gameplay review
- The Held-Plate solve *works and is legible*: blocked → cooperate with a past self → through. This is the game's core loop proven end-to-end in a real (if minimal) puzzle.
- Game feel is now "forgiving platformer" grade (buffer + coyote + variable height), which doubles as accessibility.
- Next feel addition worth queuing: **landing squash + dust** (Layer B only) and **Echo intent arcs** (Assist) — both presentation-only, zero sim risk.

## 7. Refactor opportunities (tracked)
- Extract a shared `SimEntityFactory` (body construction constants are duplicated between `LevelSimulation`, `PlateDoorModule` door bodies, and tests).
- Add `.asmdef` boundaries now that Gameplay + Unity layers exist — the `ILevelModule` split makes the Core/Gameplay/Unity seams crisp and enforceable.
- `PlateDoorModule.AddDoor` hard-codes body ids from 900000; centralize prop-id allocation when more prop types arrive.

## 8. Verdict
Phase 2's exit criterion — *solve a real multi-Echo puzzle end-to-end* — is **met and headless-verified**. The deterministic core, the first mechanic, game feel, and a complete (if minimal) Unity presentation/driver stack are in place. The only unverified surface is the Unity-editor rendering glue, flagged above for first-Play QA.

**Next phase (3 — Vertical Slice):** author 8–10 World-1 levels as `LevelDefinition` assets, add grab/carry (mechanic #3) + a second mechanic, build the timeline scrubber + prune UI, wire autosave, and stand up the NUnit `EditMode` suite in Unity CI alongside this harness.
