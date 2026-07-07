# Echo — Development Roadmap

*Assumes a small indie team (1–4 people) or a solo dev with contractors. Durations are calendar estimates for a part-to-full-time solo dev; scale down with more people. The ordering is what matters most.*

## Phase 0 — Pre-production (✅ this commit)
- Design package (GDD, TDD, Content Bible, AI system, architecture).
- Risk register (below). Tech spikes identified.
- **Exit criteria:** the core fantasy is provable on paper; biggest risk (determinism) has a plan.

## Phase 1 — Determinism Spike & Core Foundation  *(~3–4 weeks)*
- `Fix64`, `DeterministicRng`, `TickClock`, `StateHash`.
- `SimEntity`, `KinematicSolver` (move + collide + one-way platforms), `SystemRunner`.
- `InputCommand`, `Recorder`, `ReplaySource`, `Timeline`, `KeyframeCodec`, `DesyncGuard`.
- `ObjectPool<T>`, `EventBus`, `ServiceLocator`.
- **Exit criteria (the make-or-break test):** record a 60-second run, restart, watch a *bit-identical* ghost replay; `DesyncGuard` reports zero drift over a 1000× soak. **If this fails, the game doesn't exist — so it's first.**
- *(Most of these files are scaffolded in this commit; see `Assets/Scripts/Core/`.)*

## Phase 2 — Playable Core Loop  *(~3 weeks)*
- `PlayerController`, `InputRouter` (KBM/gamepad).
- Components: `Grabbable`, `Carriable`, `PlateTrigger`, `DoorActuator`. Systems: grab/carry/pressure.
- Restart → braid spawn from pool. HUD v0 (run count, restart, prune).
- **Exit criteria:** solve a real 3-Echo puzzle end-to-end. Fun gut-check.

## Phase 3 — Vertical Slice (World 1)  *(~4–5 weeks)*
- 8–10 World-1 levels (Content Bible B1–B8) + Boss "Doorwarden".
- Timeline scrubber, Prune menu, Undo, Hint system v1.
- Art pass for World 1, audio stems, core VFX, save/autosave.
- **Exit criteria:** a stranger plays World 1 unaided and "gets it." This is your **demo/trailer/Steam-page** build.

## Phase 4 — Clone Evolution AI  *(~4 weeks)*
- `SalienceTracker`, `DriveModel`, `TraitResolver`, `GateEvaluator`, `ExpressionController`.
- Layer-B expression + telegraphs; Trust/Mercy; first Refuse/Improvise gates in World 3–4 levels.
- **Exit criteria:** a high-salience Echo *visibly* refuses, is telegraphed, and is repairable. Determinism soak still green.

## Phase 5 — Content Production  *(~3–5 months, the long tail)*
- Worlds 2–6: ~40 more levels, 20 hazards, time mechanics, remaining bosses.
- Endings branching, secrets/shards, narrative beats, VESS/Caretaker.
- Continuous: difficulty tuning via analytics, accessibility passes per world.
- **Exit criteria:** content-complete ("alpha").

## Phase 6 — Mobile & Ports  *(~4 weeks, overlaps Phase 5)*
- Touch controls, layout editor, one-handed mode, haptics, perf for mid-tier phones.
- Cloud save providers per platform; achievements per platform.
- **Exit criteria:** full game completable on a mid-range Android phone at target FPS.

## Phase 7 — Polish & Beta  *(~6–8 weeks)*
- Juice, game-feel, audio mix, localization, settings completeness.
- Closed beta → analytics-driven difficulty re-tuning → bug burndown.
- **Exit criteria:** "beta" — feature-complete, content-complete, no known sev-1 bugs.

## Phase 8 — Certification & Launch  *(~4 weeks)*
- Store pages, assets, trailer, wishlpopulation campaign.
- Steam/Google Play/App Store cert checklists (`09_…`). Day-1 patch staged.
- **Exit criteria:** shipped.

## Phase 9 — Post-launch  *(ongoing)*
- Hotfixes, NG+ "Recursion", level editor / community runs (stretch), seasonal challenge runs.

---

## Risk Register
| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|-----------|
| Determinism drift across platforms | Med | **Fatal** | Fixed-point sim + DesyncGuard + soak tests; *Phase-1 gate* before any content. |
| Replays feel like watching, not playing | Med | High | Keep runs short; emphasize *planning* tension; Echo AI adds liveliness. |
| Cognitive overload (too many Echoes) | Med | High | Echo caps, intent overlays, color/shape coding, assist. |
| Content scope (50 levels) overruns | High | Med | Data-driven pipeline; cut to 30 strong levels if needed (quality > count). |
| Clone AI feels random/unfair | Med | High | Determinism + telegraph + repair-path guarantees (AI doc §4). |
| Mobile perf with full braid | Med | Med | Pooling, instancing, LOD'd expression, 30 Hz render fallback. |

## Definition of Done (per level)
Authored SO ✓ · solvable as intended ✓ · ≥1 emergent solution allowed ✓ · determinism soak green ✓ · hint graph ✓ · a11y pass ✓ · analytics events wired ✓.
