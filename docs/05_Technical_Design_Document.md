# Echo — Technical Design Document (TDD)
*Engine: Unity 2022 LTS · C# · URP · New Input System · Addressables*

## 1. Determinism Strategy (the foundation)
**Decision: we do NOT rely on deterministic re-simulation of Unity PhysX.** Cross-platform PhysX is not bit-deterministic, and floating-point drift across CPUs would desync replays. Instead:

### 1.1 Hybrid model
- **Gameplay-critical entities** (Echo-Prime, Echoes, blocks, plates, doors) run on a **custom fixed-point deterministic kinematic simulation** at a fixed tick (Δ = 1/60 s). This sim is integer/fixed-point math (`Fix64`-style), platform-independent, fully reproducible.
- **Cosmetic entities** (particles, debris, parallax, cloth) use normal Unity physics/animation — they never affect outcomes, so non-determinism there is harmless.

### 1.2 What we record
For each tick we record the **InputCommand** (a quantized struct) plus an **event log** of discrete interactions, plus a periodic **keyframe** (full deterministic state snapshot, every K ticks) used for:
- fast scrubbing,
- resync/verification (a replay must reproduce the keyframe → desync detector),
- partial-load (jump into a timeline without replaying from 0).

> Recording *input + fixed-point sim* (not transforms) keeps files tiny and replays exact, and is the in-fiction "record intent, not video" (Lore §4).

### 1.3 Desync guard
Each keyframe stores a hash of deterministic state. On replay, the engine re-hashes and asserts equality. Any mismatch is logged with a repro seed → a determinism bug becomes a failing test, not a player-facing glitch. In release, a mismatch silently falls back to the stored keyframe (graceful).

## 2. Replay Format
```
Timeline (one run)
  header: { schemaVersion, levelId, saveSeed, runId, tickRate, tickCount, traitSeed }
  inputStream:  delta-encoded InputCommand[]  (RLE for held/no-op ticks)
  eventLog:     [tick, entityId, eventType, payload]   (grabs, throws, presses)
  keyframes:    [tick, stateHash, packedState]  every K ticks
  meta:         { salience, dominantTrait, driveVector, playerStats }
```
- **InputCommand** (≈ 2–4 bytes/tick before compression): bitfield of buttons + quantized analog (move axis to 8 levels, etc.).
- **Compression:** RLE over idle ticks + LZ4 block compression at save. A typical 90-second run compresses to a few KB. A level's full braid (6 runs) is tens of KB.
- **Versioning:** `schemaVersion` + migration shims; never break old saves.

## 3. Simulation Loop
```
FixedTick (60 Hz):
  1. GatherInput      → InputCommand (live) ; Echoes pull from ReplaySource
  2. EchoBrain.Step   → Layer A deltas via Divergence Gates (deterministic)
  3. KinematicSolve   → fixed-point integration, collision, interactions (order-stable)
  4. ResolveEvents    → plates/doors/objects state machine
  5. Record           → append live InputCommand + events ; emit keyframe if tick%K==0
  6. Snapshot for Undo (bounded ring buffer)
RenderTick (display Hz):
  - interpolate fixed-point state → smooth visuals (decoupled from sim rate)
  - Layer B expression, particles, camera, parallax
```
- **Order stability:** entities processed in a stable, id-sorted order so interactions are deterministic regardless of spawn timing.
- **Render interpolation** decouples sim (60 Hz) from refresh (up to 144+), so high-refresh PCs stay smooth and mobile can sim at 60 while rendering 60/30.

## 4. Component Architecture (ECS-lite, composition over inheritance)
We use **MonoBehaviour + composition**, not deep inheritance. (Full DOTS is overkill for the entity counts; a lightweight component model is cleaner and faster to author.)
- An entity = a `SimEntity` (id, fixed-point transform) + capability components: `Grabbable`, `Pressable`, `Carriable`, `Hazard`, `PlateTrigger`, `DoorActuator`, `Lantern`, etc.
- Systems iterate component lists each tick. See `06_Architecture…` for the module map and the actual scripts in `Assets/Scripts/`.

## 5. Object Pooling
- All transient sim objects (Echoes, projectiles, particles, audio one-shots, UI toasts, VFX) come from typed **pools** (`ObjectPool<T>`), pre-warmed at level load from a manifest.
- Echoes are the marquee pool: spawning 6 Echoes per restart must allocate **zero** at runtime. Pool sizing comes from per-level `maxEchoes`.
- Pools are scene-scoped, released on level unload, and report high-water marks to analytics for tuning.

## 6. Save System
- **Format:** versioned binary (LZ4) with a JSON debug fallback in dev builds.
- **Model:** `Profile → WorldProgress → LevelRecord{ bestSolution, bankedTimelines[], stars, salienceMeta }` + `Settings` + `Stats`.
- **Autosave:** debounced writes on key events (GDD §8); double-buffered (write temp → fsync → atomic rename) so a crash never corrupts the live file.
- **Integrity:** CRC + schema version; on failure → previous good autosave; never hard-block.
- **Undo:** in-level bounded command stack (ring buffer of sim snapshots/keyframes); `Ctrl+Z` re-derives prior state deterministically. Distinct from save.

## 7. Cloud Save
- Abstraction `ICloudSaveProvider` with implementations: **Steam Cloud**, **Google Play Saved Games**, **iCloud/Game Center**.
- Strategy: local is source of truth during play; sync on app pause/quit + level complete. **Conflict** = compare `(deviceClock, playTimeMonotonic, progressScore)` → auto-pick higher progress, else prompt "keep which?".
- Saves are small (KB), so full-blob sync is fine; no partial-merge complexity.

## 8. Rendering / Tech Art
- **URP**, 2.5D: sprites/flat meshes on parallax depth layers; 2D lights (Echo glow, lanterns, lasers).
- **Echo rendering:** a single instanced material with per-Echo params (salience glow, faction tint, ghost alpha) via `MaterialPropertyBlock` → no per-Echo material, GPU-instanced.
- **Batching:** SRP Batcher on; static environment batched; Echoes instanced. Target ≤ ~150 draw calls/frame on mobile.
- **VFX:** pooled, LOD'd, all "reduce-motion"-aware.
- **Determinism note:** rendering reads interpolated sim state read-only; nothing in render feeds back into sim.

## 9. Audio
- FMOD (or Unity audio + a thin bus abstraction) with: music stems that layer per active-Echo count (the "Choir" grows as your braid grows), captioned puzzle-critical SFX, haptic-paired cues on mobile. Audio is pooled and never on the sim critical path.

## 10. Hint System (technical)
- Per-puzzle `HintGraph` ScriptableObject: tiered nodes (Nudge/Frame/Choreography). Choreography tier replays a *canned* partial timeline as a translucent preview using the same ReplaySource pipeline (reuse, not bespoke).
- Auto-surface trigger: `failedRuns >= threshold && timeSinceProgress > T` → offer (never force).

## 11. Achievements
- `IAchievementProvider` (Steam / Google Play / Game Center). Local achievement state mirrored in save; granted via an event bus so gameplay code only raises semantic events (`OnEchoSacrificed`, `OnSolvedNoEchoes`, `OnAllShards`). Idempotent unlock.

## 12. Analytics (privacy-first)
- `IAnalyticsSink` (no-op by default; opt-in per platform/region). Events: funnel (level start/complete/abandon), `runsToSolve`, hint usage, deaths-by-hazard, Echo high-water, trait distribution, FPS buckets, crash breadcrumbs.
- **No PII.** Aggregated, opt-in, GDPR/CCPA compliant, clear toggle in settings, documented data map. Used to tune difficulty curve and perf.

## 13. Input Abstraction
- New Input System → a single `InputCommand` per tick regardless of device (KBM/gamepad/touch). The recorder consumes `InputCommand`, so **all platforms record identically** and a PC-recorded timeline replays bit-identically on mobile.

## 14. Threading & Frame Budget
- Sim is single-threaded by design (determinism); it's cheap (small entity counts). Heavy non-sim work (LZ4 compress, save I/O, addressable loads, audio) is off-thread via jobs/async. Frame budget table in `08_Performance…`.

## 15. Build/Tooling
- Addressables for content (stream levels/worlds). CI builds (Win/Mac/Linux/Android/iOS). Automated determinism + save-migration test suites in CI. Feature flags via remote config (kill-switch for analytics, etc.).
