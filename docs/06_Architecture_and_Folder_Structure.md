# Echo — Project Architecture & Folder Structure

## 1. Architectural Principles
1. **Composition over inheritance** — entities are bags of capability components.
2. **Deterministic core, expressive shell** — a pure fixed-point sim wrapped by a non-authoritative presentation layer.
3. **Dependency inversion at the edges** — platform services (save, cloud, achievements, analytics) behind interfaces; gameplay never references a vendor SDK directly.
4. **Data-driven content** — levels, puzzles, hints, traits, tuning are ScriptableObjects/Addressables, not code.
5. **Zero runtime allocations in the hot loop** — pooling everywhere; the 60 Hz sim must not generate GC.

## 2. Module Map
```
┌──────────────────────────────────────────────────────────────┐
│ App (bootstrap, scene flow, service locator, feature flags)   │
├──────────────────────────────────────────────────────────────┤
│ Core.Determinism   Fix64, DeterministicRng, TickClock, Hash   │
│ Core.Sim           SimEntity, KinematicSolver, SystemRunner    │
│ Core.Replay        InputCommand, Recorder, ReplaySource,       │
│                    Timeline, KeyframeCodec, DesyncGuard         │
│ Core.Echo          EchoBrain, Salience, DriveModel, Traits,     │
│                    GateEvaluator, ExpressionController          │
├──────────────────────────────────────────────────────────────┤
│ Gameplay.Components  Grabbable, Pressable, Carriable, Plate,    │
│                      Door, Hazard, Lantern, Portal, …           │
│ Gameplay.Systems     GrabSystem, PressureSystem, HazardSystem,  │
│                      CarrySystem, TimeFieldSystem, …            │
│ Gameplay.Player      PlayerController, InputRouter              │
├──────────────────────────────────────────────────────────────┤
│ Services (interfaces)  ISaveService, ICloudSaveProvider,        │
│                        IAchievementProvider, IAnalyticsSink,    │
│                        IAudioService, IHapticService            │
│   Platform impls       Steam/, GooglePlay/, AppleGameKit/       │
├──────────────────────────────────────────────────────────────┤
│ UI  HUD, TimelineScrubber, PruneMenu, HintUI, Accessibility,    │
│     Pause, Settings (UI Toolkit)                                │
├──────────────────────────────────────────────────────────────┤
│ Infra  ObjectPool<T>, EventBus, ServiceLocator, Logger,         │
│        AddressableLoader, SaveCodec(LZ4)                         │
└──────────────────────────────────────────────────────────────┘
```
**Dependency rule:** arrows point downward only. `Gameplay` may use `Core` + `Infra`; `Core` depends on nothing but `Infra` + `Core.Determinism`. UI/Services never reach into `Core.Sim` internals — they talk through `EventBus`/interfaces. Enforced with **assembly definitions** (asmdef) so violations are compile errors.

## 3. Assembly Definitions (compile-time boundaries)
`Echo.Core.Determinism` → `Echo.Core.Sim` → `Echo.Core.Replay` → `Echo.Core.Echo` → `Echo.Gameplay` → `Echo.UI` / `Echo.Services` → `Echo.App`. Tests get `*.Tests` asmdefs referencing only their target. This keeps the deterministic core *physically unable* to depend on Unity-specific or platform code.

## 4. Folder Structure (Unity project)
```
Echo/
├─ README.md
├─ docs/                         (this design package)
├─ Assets/
│  ├─ Scripts/
│  │  ├─ Core/
│  │  │  ├─ Determinism/   Fix64.cs, DeterministicRng.cs, TickClock.cs, StateHash.cs
│  │  │  ├─ Sim/           SimEntity.cs, KinematicSolver.cs, SystemRunner.cs, ISimSystem.cs
│  │  │  ├─ Replay/        InputCommand.cs, Recorder.cs, ReplaySource.cs, Timeline.cs,
│  │  │  │                 KeyframeCodec.cs, DesyncGuard.cs
│  │  │  └─ Echo/          EchoBrain.cs, SalienceTracker.cs, DriveModel.cs,
│  │  │                    TraitResolver.cs, GateEvaluator.cs, ExpressionController.cs
│  │  ├─ Gameplay/
│  │  │  ├─ Components/     Grabbable.cs, Pressable.cs, Carriable.cs, PlateTrigger.cs,
│  │  │  │                  DoorActuator.cs, Hazard.cs, Lantern.cs, Portal.cs
│  │  │  ├─ Systems/        GrabSystem.cs, PressureSystem.cs, HazardSystem.cs, …
│  │  │  └─ Player/         PlayerController.cs, InputRouter.cs
│  │  ├─ Services/          ISaveService.cs, SaveService.cs, ICloudSaveProvider.cs,
│  │  │                     IAchievementProvider.cs, IAnalyticsSink.cs, Platform/{Steam,GooglePlay,Apple}
│  │  ├─ UI/                Hud/, Timeline/, Prune/, Hint/, Accessibility/, Settings/
│  │  ├─ Infra/             ObjectPool.cs, EventBus.cs, ServiceLocator.cs, Logger.cs,
│  │  │                     AddressableLoader.cs, SaveCodec.cs
│  │  └─ App/               GameBootstrap.cs, SceneFlow.cs, GameConfig.cs, FeatureFlags.cs
│  ├─ ScriptableObjects/    Levels/, Puzzles/, Hints/, Traits/, Tuning/, Pools/
│  ├─ Prefabs/              Echo/, Player/, Props/, UI/, VFX/
│  ├─ Art/                  Sprites/, Materials/, Shaders/, Animations/, Tilesets/
│  ├─ Audio/                Music/, SFX/, Buses/
│  ├─ Scenes/               Boot, MainMenu, World1…World6, Sandbox
│  ├─ Settings/             URP/, Input/, Addressables/
│  └─ Tests/
│     ├─ EditMode/          Determinism, Replay, Save, Gates (pure unit tests)
│     └─ PlayMode/          Integration, soak/regression, perf
├─ Packages/                manifest.json (Input System, Addressables, Test Framework, …)
└─ ProjectSettings/
```

## 5. Key Cross-Cutting Systems
- **ServiceLocator** (tiny, explicit, set at bootstrap) — avoids singletons-everywhere; testable by swapping fakes.
- **EventBus** — typed, allocation-free (struct events), for decoupling gameplay → UI/achievements/analytics.
- **ObjectPool<T>** — generic, prewarm + high-water reporting.
- **GameBootstrap** — composition root: wires services (real on device, fakes in tests), loads boot config, hands off to `SceneFlow`.

## 6. Data-Driven Content Pipeline
- `LevelDefinition` (SO): layout addressable, `maxEchoes`, allowed Gate mask, hazard set, hint graph ref, par solution.
- `TuningProfile` (SO) per world: salience curves, drive rates, trait thresholds.
- Designers ship new levels/puzzles **without code** by authoring SOs + prefabs. Code provides systems; data provides content.

## 7. Why this scales to the full content list
50 mechanics don't mean 50 bespoke systems. Most are **compositions** of a dozen core systems (pressure, grab/carry, hazard, time-field, light, portal) over data. New mechanics are usually a new component + a small system or just a new SO config. This is how an indie team ships 50 levels without 50× the code.
