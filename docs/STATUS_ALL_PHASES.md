# Echo — Consolidated Status Across All Phases

*Honest accounting of every roadmap phase. Legend: ✅ done + headless-tested · 🟦 code written, needs Unity editor to verify · 📋 process/external (needs engine, accounts, art, or calendar time — cannot be executed in this environment).*

## Headless test coverage: **311/311 passing**
Run: `dotnet run --project echoharness -c Release` (exits 0). The whole `Core`/`Infra`/`Services`/`Gameplay` layer is pure C# (no UnityEngine) and is compiled + executed for real.

| Phase | Item | State | Evidence |
|------|------|-------|----------|
| **1** | Deterministic Fix64 sim, RNG, hashing | ✅ | 29 math/RNG/hash tests |
| 1 | Recorder / Replay / DesyncGuard | ✅ | replay == recording bit-identical incl. 90 s run |
| 1 | Clone-evolution AI (drives/traits/gates) | ✅ | pure, dormant-below-threshold, reproducible divergence |
| 1 | Object pooling, EventBus, ServiceLocator | ✅ | pool zero-leak / zero-alloc reuse |
| 1 | LevelSimulation braid + predecessor invariant | ✅ | 8-run soak reproducible across sessions |
| **2** | Game feel (jump-buffer, coyote, variable height) | ✅ | buffered jump + apex tests |
| 2 | Mechanic #1 Held Plate (plates/doors) | ✅ | solve + control + reproducible |
| 2 | `ILevelModule` pluggable mechanics | ✅ | architecture boundary holds (core compiles Gameplay-free) |
| 2 | Unity presentation (SimRunner/SimView/HUD) | ✅ | compiles + runs against real Unity 6000.0.78f1; playtest scenes built for interactive verification |
| 2 | Content pipeline (LevelDefinition/Builder) | ✅ | builder logic tested via modules headlessly AND compiles/imports cleanly in real Unity |
| **3** | Mechanic #3 Carry (crates, grab/drop) | ✅ | carry-to-plate solve + reproducible |
| 3 | Mechanic #8 Quorum Door | ✅ | N-body zone open/close |
| 3 | Mechanic #42 Hazard + Echo Sacrifice | ✅ | sacrifice clears one-shot hazard + reproducible |
| 3 | Undo / prune banked runs | ✅ | `PruneBankedRun` API + prune UI 🟦 |
| 3 | Hint system (escalation + auto-surface) | ✅ | tier/cooldown/reset tests |
| 3 | Mechanic #5 Same-Tick Switch | ✅ | two-self simultaneous latch + control + reproducible |
| 3 | Mechanic #13 Phase Platform | ✅ | past-self bridge (choreographed) + control + reproducible |
| 3 | Mechanic #15 Bounce Pad | ✅ | launch apex 6.9 vs jump 3.6; reproducible |
| 3 | Mechanic #10 Momentum Bank (throw) | ✅ | thrown crate (X23.1) beats drop (X18.7); reproducible |
| 3 | Mechanic #34 Anchored Portals | ✅ | teleport past a wall + control + reproducible |
| 3 | Mechanic #44 Resonance Plates (run cloning) | ✅ | 3 cloned Echoes meet threshold; reproducible |
| 3 | Timeline scrubber / prune UI | 🟦 | `PruneTimelineHud` written |
| **Time** | Echo-Delay (#E1) | ✅ | offset replay unit test + delayed-solve reproducible |
| Time | Reverse-Replay (#E16) | ✅ | reversed source unit test + reproducible in-sim |
| Time | Slow-Field (§E #3) | ✅ | scaled displacement reduces distance; reproducible |
| Time | Fast-Field (§E #4) | ✅ | same module, scale>1; increases distance; reproducible |
| **3** | Mechanic #20 Polarity (magnets) | ✅ | attract pulls metal to magnet, repel pushes away; damped, reproducible |
| 3 | Mechanic #24 Arrival Order | ✅ | reversed-order checkpoints proven to require backtracking, not just spatial pass-through |
| 3 | Mechanic #28 Torch Sequence | ✅ | explicit-Interact beacons; wrong beacon snuffs all lit progress; reproducible |
| 3 | Mechanic #38 Throw-and-Ride | ✅ | crate landing now skids (decay, not hard-stop); a resting rider is carried by the skid |
| **Time** | Conductor / Phase-Shift | ✅ | mid-run playhead nudge ±k ticks; forward/backward proven against an idle-then-move recording |
| Time | Pause-Self | ✅ | frozen playhead proven vs an unpaused control over the same real-tick span |
| **Assist** | Pre-Echo (preview ghost) | ✅ | deterministic, non-authoritative projection; proven to match the real replay and never perturb live sim state |
| **Content** | Floor-gap authoring (pits) | ✅ | `LevelDefinition.FloorGaps` → `LevelBuilder`; fill-then-carve proven tile-identical to the trusted pit-world helper |
| **Content** | Worlds 1–2 authored (14 levels) | ✅/🟦 | `SampleLevels.cs`; each level's geometry ported from a passing headless test (see below) |
| **Editor** | Unity 6000.0.78f1 installed + project verified | ✅ | Whole codebase compiles with 0 errors against real UnityEngine; `.asmdef` boundaries added (Core/Infra/Gameplay/Services/Unity/Tests) |
| Editor | EditMode test suite (real Unity, not the harness) | ✅ | `Echo.Tests.EditMode`, 4/4 passing via `-runTests -testPlatform EditMode` |
| Editor | Playtest scenes for the two feel-dependent levels | 🟦 | `Assets/Scenes/W1_L6_Springboard.unity`, `W2_L6_Freight.unity` built and ready to Play; timing/drift still needs a human to actually judge feel |
| **Bug** | `ObjectPool.Prewarm()` skipped the despawn hook | ✅ fixed | Real bug, only visible by actually rendering the game: prewarmed-but-unclaimed pooled `SimView`s stayed active at `(0,0,0)`, showing as a phantom square. Fixed in `Assets/Scripts/Infra/ObjectPool.cs`; regression-guarded by a new `PoolableSpy` test in the harness (182/182). The headless-only harness could never have caught this — it has no concept of "visible." |
| **Note** | Automated real-time playtesting hit a tooling wall | — | Computer-use key-hold does not translate into sustained per-frame `Input.GetAxisRaw` polling the way a human keystroke does — 3 separate attempts (2s/3s/6s "hold right") landed the player at the exact same X, which isn't physically possible if the input were genuinely continuous. W1-L6's ledge was widened as a reasonable, low-risk improvement, but its exact feel is **unverified by this session** — needs an actual human on the keyboard. |
| **4** | Trust/Mercy wired into live gameplay | ✅ | `EchoBrain.ApplyTrust/ApplyGrievance` now actually get called: a sacrificed Echo's own Spite rises on death; landing on a specific Echo raises that Echo's own Attachment (edge-triggered once per fresh landing, not every resting tick). Persisted onto `Timeline.Trust/Grievance` so it survives the Echo being despawned/re-Init'd on the next restart — previously these hooks existed but nothing called them. `EchoDefiedEvent` also now actually publishes (was defined, never raised). |
| **Cleanup** | `SimEntityFactory` | ✅ | Centralized body-construction (character half-extents, static-body assembly) and entity-id ranges that were previously hand-copied across `LevelSimulation`/`CrateModule`/`PlateDoorModule`/`SameTickSwitchModule`/`BouncePadModule`/`MagnetModule`/`ArrivalOrderModule`/`TorchSequenceModule`/`PreEchoPreview`. Behavior-preserving — same pass count, identical numeric findings before/after. |
| **3** | Mechanic #45 Anti-Echo Field | ✅ | deletes Echoes on contact, live player passes through unharmed; reuses the generic sacrifice/grievance wiring for free since it's just another PrevActive→inactive transition |
| 3 | Mechanic #33 Wind Toggle | ✅ | held-switch-activated force field; found `CharacterMotor` can't take a velocity push on X (recomputed from input every tick) so wind nudges position directly, matching `TimeFieldModule`'s technique |
| 3 | Mechanic #40 Light-Solid | ✅ | platform solid only near a held Lantern; **caught and fixed a real ordering bug in my own first draft** — resetting the "lit" flag in `Tick()` erased the previous tick's result before `CollectSolids()` ever read it; fixed with a first-character-this-tick reset in `OnCharacterStep` instead |
| 3 | Mechanic #29 Pressure Balance | ✅ | door needs EQUAL nonzero occupancy on two plates (not just any press) — one-sided pressure provably does not open it |
| 4 | Mechanic #37 Gravity Memory | ✅ | zone reverses gravity via a post-motor velocity correction (`v.Y -= 2·Gravity·Dt`), same technique as Bounce Pad — no core `CharacterMotor` changes needed. Implemented by a parallel subagent, verified by hand before wiring in. |
| 4 | Mechanic #11 Delayed Charge | ✅ | arm/detonate decoupled by a fixed fuse. Confirmed `ILevelModule.ResetModule()` runs on every `Restart()` (checked the source, didn't assume) — the puzzle works because the *replaying Echo* re-arms and re-fires the fuse each run, same pattern as a consumable Hazard being re-spent by its own replay. Implemented by a parallel subagent. |
| 4 | Mechanic #23 Stack-to-Reach | ✅ | proved genuine 2-high Echo stacking (Echo on floor → 2nd Echo lands on the 1st → live player lands on the 2nd) using the existing reliance-jump search technique twice in sequence; caught and fixed a bug in my own first draft — calling `Restart()` inside a choreography-search loop banked half-finished attempts as spurious extra Echoes instead of retrying cleanly. |
| **4** | Clone AI live in-engine (telegraphs, Trust/Mercy) | ✅ core / 🟦 visuals | gates + `SimView` flash; Trust/Mercy hooks exist (`ApplyTrust/Grievance`) |
| 4 | **All 10 endings** (BranchState + EndingResolver) | ✅ | every ending reachable + deterministic; mutators tested |
| 4 | Narrative wiring (EventBus → BranchState) | ✅ | `NarrativeDirector` + live in-sim sacrifice → Mercy drop tested |
| **Save** | Timeline codec (RLE) + Deflate + CRC | ✅ | decoded timeline replays bit-identically; corruption detected |
| Save | SaveProfile round-trip | ✅ | profile + banked-timeline blob round-trip |
| Save | Autosave debounce + atomic file write | ✅ | coalesced-write test; `FileSaveBackend` atomic |
| **Services** | Achievements (idempotent), Analytics (opt-in) | ✅ | in-memory impls tested; platform impls behind interfaces |
| Services | Cloud save | 🟦 | `ICloudSaveProvider` interface; per-platform impls pending |
| **6 Mobile** | Touch controls (virtual stick + buttons) | 🟦 | `TouchInputProvider` written; needs device test |
| **5 Content** | Worlds 2–6, full 50/50/20/… | 📋 | designed (Content Bible); production is the long tail |
| **6 Ports** | Steam Deck / Android / iOS builds | 📋 | needs engine + devices; checklists in `09_…` |
| **7 Polish/Beta** | Juice, localization, closed beta | 📋 | needs assets + players |
| **8 Cert/Launch** | Store cert, pages, trailer | 📋 | needs store accounts + final assets; checklists ready in `09_…`, copy in `10_…` |

## What "all phases simultaneously" actually produced this pass
Driven in parallel and landed **tested**: 3 new mechanics (carry/quorum/hazard-sacrifice), the full **save/serialization stack**, **undo/hint** systems, **achievements/analytics** services — plus **mobile touch input**, **prune UI**, and **content-pipeline** wiring written for the editor. Test count grew **67 → 93**, still 0 failures.

## What genuinely cannot be done here (and why)
- **Ports & store certification** need platform SDKs (console/mobile), signing certs, and store accounts beyond what a local Unity install provides.
- **Content production** (40+ more levels, art, audio, VO) and **marketing/beta** need artists, writers, players, and calendar time.
- **Human feel-testing** — Unity 6000.0.78f1 is now installed and the whole codebase is verified compiling + passing an in-editor EditMode suite, closing the "no editor" gap. What's still un-verifiable by an agent: whether specific jump/throw timings (the Bounce Pad ledge gap in W1-L6, the jump-onto-a-moving-crate window in W2-L6) actually *feel* right — that requires a human playing it. Scenes are built and ready: `Assets/Scenes/W1_L6_Springboard.unity`, `Assets/Scenes/W2_L6_Freight.unity`.

These are tracked with concrete checklists already written: release (`09_Performance_Testing_Release.md`), marketing/store (`10_Marketing_Monetization_Store.md`), and the per-phase reviews.

## Mechanics implemented & headless-tested (48 of 50)
#1 Held Plate · #2 Echo Ladder · #3 Carry · #4 Counterweight Self · #5 Same-Tick Switch · #6 Mirror Relay · #7 Object Chain · #8 Quorum Door · #9 Decoy Self · #10 Momentum Bank · #11 Delayed Charge · #12 Body Shield · #13 Phase Platform · #14 Echo Conveyor · #15 Bounce Pad · #16 Key Across Time · #17 Mirrored Path · #18 Mass Scale · #19 Trapdoor Hold · #20 Polarity (magnets) · #21 Pulley Crew · #22 Self-Turret · #23 Stack-to-Reach · #24 Arrival Order · #25 No-Touch Paradox · #26 Echo Budget · #28 Torch Sequence · #29 Pressure Balance · #30 Split-Speed Floor · #31 Generator Crank · #32 Flood Control · #33 Wind Toggle · #34 Anchored Portals · #35 Color Carry · #36 Echo Password · #37 Gravity Memory · #38 Throw-and-Ride · #39 Domino Crew · #40 Light-Solid · #41 Two-Body Lock · #42 Hazard/Sacrifice · #43 Memory-Lock Door · #44 Resonance Plates · #45 Anti-Echo Field · #46 Conductor (built earlier as "Conductor/Phase-Shift") · #47 Weighted Pendulum · #48 Echo Crossfire · #49 Cumulative Lever · #50 Self-Negotiation.
Time mechanics (6): Echo-Delay, Reverse-Replay, Slow-Field, Fast-Field, Conductor/Phase-Shift, Pause-Self. Plus Pre-Echo (assist preview), Trust/Grievance (persisted per-Echo, live-wired), run-cloning, undo/prune, all 10 endings + live narrative wiring.
All 48 mechanics are authorable from a `LevelDefinition` asset via `LevelBuilder`, including a new floor-gap primitive needed for pits (discovered while authoring Phase Platform as real content — `LevelBuilder` previously had no way to leave a hole in the floor row at all).
**Only #27 Blind Record remains** from the 50 Unique Puzzle Mechanics — deliberately out of scope for this layer: it's "part of the room hidden while recording," a camera/fog-of-war *presentation* concern with no deterministic-sim analog, not something the headless harness (or any `ILevelModule`) can meaningfully own. It belongs in the Unity rendering/camera layer, not `Core`/`Gameplay`.

## Bonus: 2 Environmental Hazards from Section D (`docs/03_Content_Bible.md`)
With Section A essentially complete, picked up two more from the 20 Environmental Hazards list since they're genuinely new sim behaviors, not reskins of what's already built:
- **Crusher Pistons (D2)** — `CrusherPistonModule`: an autonomous rhythmic hazard (fixed extend/retract cycle, no Echo interaction needed to arm it, unlike Self-Turret #22). Player must read and time the piston's own rhythm.
- **Caretaker Drones (D19)** — `CaretakerDroneModule`: a roaming hazard that hunts whichever ACTIVE ECHO has the highest Salience, never the live player. Added `LevelSimulation.SalienceForBodyId` (mirroring the existing `TrustForBodyId` pattern) so the module can read live per-Echo Salience via `ISimAware` without owning `EchoBrain` internals.
- **Real discovery while testing Caretaker Drones**: assumed Salience would persist and accumulate across restarts the way Trust/Grievance do (since they were explicitly wired that way earlier this session) — it doesn't. `EchoBrain.Init()` re-seeds `Salience` from the banked Timeline on every `Restart()`, so naturally engineering a large, stable salience gap between two Echoes across multiple restarts isn't practical to script deterministically. Confirmed by direct inspection (debug output showing both Echoes at exactly `0.000000` right after a restart) rather than continuing to guess at timing fixes. Fixed by directly setting `Salience.Salience` for the test — a legitimate technique since it's a public mutable field, and the module itself doesn't care how Salience got to its value, only what it currently is.

### Notable engineering finds from the full 50-mechanic push
- **Real bugs caught by the harness, not assumed away**: `PendulumModule`'s swing formula treated phase=0 as the LEFT extreme, but the body spawned at CENTER — a genuine one-tick teleport bug, fixed at the source (not papered over in the test). `EchoLadderModule`'s first draft left a crouch-toggled Echo permanently non-solid once it exited its zone. `LightSolidModule`/`GeneratorCrankModule`/`TwoBodyLockModule` all hit the same tick-ordering trap (resetting a "this tick" flag in `Tick()` erases the previous tick's result before `CollectSolids()` ever reads it) — caught once, then written into every subsequent agent brief as a named pitfall.
- **`CharacterMotor` constraint discovered mid-session**: horizontal velocity is recomputed from input every tick (never accumulated), so `WindModule`/`FloodModule`/`GravityFieldModule` all push bodies via direct position/velocity nudges in `OnCharacterStep`, not by trying to set X velocity like Bounce Pad does for Y.
- **`ISimAware`** (`Core/Sim/ISimAware.cs`) added so a module (Self-Negotiation) can query live per-Echo Trust state without a constructor-time dependency — modules are built before the `LevelSimulation` exists, so `LevelSimulation.AddModule` auto-wires it after both exist.
- **Echo Budget (#26)** was a real gap: `MaxEchoes` was only ever used to size object pools, nothing enforced it as a hard cap. `LevelSimulation.Restart()` now refuses to bank a new run once at cap.
- Parallel subagents built roughly two-thirds of the later mechanics from detailed specs (including exact known pitfalls); all were reviewed by hand before integration, and the review caught real issues in at least two of them (Pendulum, Echo Ladder).

## Worlds 1–2 authored (14 levels, `Assets/Scripts/Unity/SampleLevels.cs`)
- **World 1** (8, one mechanic each, tutorial tone, `GateMask.None`): L1 Held Plate · L2 Carry · L3 Hazard/Sacrifice · L4 Quorum Door · L5 Same-Tick Switch · L6 Bounce Pad · L7 Phase Platform · L8 Momentum Bank.
- **World 2** (6, one newer/advanced mechanic each): L1 Anchored Portals · L2 Resonance Plates · L3 Polarity · L4 Arrival Order (reversed-order, forces backtracking) · L5 Torch Sequence · L6 Throw-and-Ride (practice room).
- Every level's numeric layout is ported directly from a passing headless test (traceable in each level's doc comment), except **W1-L6** (Bounce Pad ledge gap) and **W2-L6** (Throw-and-Ride jump-on timing), which depend on in-flight drift/timing feel that needs an actual Unity editor playtest pass to tune — called out explicitly rather than asserted as proven.
- `LevelDefinition`/`LevelBuilder` compile against `UnityEngine` so they're outside the *headless harness* by design — but Unity itself is now installed and both are verified compiling/importing cleanly in it (see Editor rows above). The `FloorGaps` mechanism they depend on is additionally unit-tested at the `TileCollisionWorld` level.

## Immediate next engineering steps
1. **#27 Blind Record** is the only mechanic left, and it needs a Unity-side camera/fog-of-war feature (hide part of the room during recording), not a sim module — implement it in the presentation layer, not `Core`/`Gameplay`.
2. **Author Worlds 3–6.** All 48 mechanics exist and are wired into `LevelDefinition`/`LevelBuilder`, but only Worlds 1–2 (14 levels) have actual authored content. This is now the biggest gap between "mechanics work" and "a finished game."
3. **Human playtest pass.** Every mechanic is proven correct in the headless harness (deterministic logic) and compiles clean in the real Unity editor, but NONE have been played by a human — feel, difficulty curve, and telegraphing (especially the newer hazard-based mechanics: Self-Turret, Decoy Self, Echo Crossfire, Body Shield) need eyes-on tuning that a logic test can't provide. `W1_L6_Springboard` / `W2_L6_Freight` were already flagged as unverified for this same reason.
4. Expand the NUnit `EditMode` suite (`Echo.Tests.EditMode`, currently 4 tests) to mirror more of the 306-test headless harness, now that the asmdef split makes adding test files straightforward.
5. Add UI/telegraphing for the newer timing-sensitive mechanics (Self-Turret's firing rhythm, Two-Body Lock's tolerance window, Domino Crew's cascade) — these are only "fair" to a player if the game visually communicates the countdown/rhythm, which no test can verify.
