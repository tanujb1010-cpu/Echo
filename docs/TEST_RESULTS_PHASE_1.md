# Echo — Phase 1 Test Results (executed, headless)

The entire `Core`/`Infra`/`Services` layer is pure C# with **no UnityEngine dependency** (by design), so it was compiled with .NET 8 and run headless. A standalone harness compiles the **real game sources** and throws edge cases at them.

## Result: 59 / 59 passed
```
Fix64 ............... 18/18   exact arithmetic, neg-floor, sqrt, div-by-zero throw, clamp/sign
DeterministicRng .... 7/7     same-seed stream, seed-by-(run,tick,gate), range bounds, Chance(0/1)
StateHash ........... 4/4     deterministic, order- & value-sensitive
Replay fidelity ..... 8/8     replay == recording BIT-IDENTICAL (incl. 5,400-tick / 90s run, empty, 1-tick)
Collision edges ..... 4/4     rest stability (no jitter over 300 ticks), no wall tunneling
Stand-on-clone ...... 3/3     player lands on a solid clone's head from clear air
LevelSim soak ....... 2/2     8 runs × 2 sessions → identical hash streams; empty restarts safe
Spawn-overlap ....... 2/2     stacked-at-spawn braid passes through (no pop), settles correctly
Evolution gates ..... 5/5     pure, dormant below threshold, mask-suppressed, reproducible divergence
ObjectPool .......... 6/6     prewarm, high-water, zero-leak, reuse allocates nothing
```

## What this proves
- **The fatal risk (determinism drift) is retired in practice, not just in theory.** Record→replay is exact across long runs and across a growing braid.
- The **predecessor-collision invariant** (run K collides only with runs 0..K-1) holds: the soak test reconstructs an 8-run braid twice and gets identical state every tick.
- The **evolving clone AI is deterministic**: a high-salience Stubborn echo refused 6 times over 300 ticks, identically across two independent brains.

## Bug found & fixed during edge testing
- **Spawn-overlap pop:** on restart the whole braid spawns stacked on the entrance tile, so bodies overlap at tick 0. The naive solver shoved them apart (a 1-tick vertical "pop", peaking ~0.9 units above spawn). Fixed with the **start-of-tick overlap rule** in `KinematicSolver`: a solid only blocks you if you weren't already overlapping it when the tick began — stacked clones pass through until they separate, then re-solidify. Verified: `no spawn-overlap pop` now passes, and legitimate landing-on-a-clone (from clear air) still works.

## How to reproduce
```
# one-time, no admin (installs a local SDK):
#   pwsh dotnet-install.ps1 -Channel 8.0 -InstallDir C:\Users\<you>\dotnet8 -NoPath
dotnet run --project echoharness/EchoHarness.csproj -c Release
# exits 0 on success, non-zero on any failure
```
*(The harness lives outside the repo to keep it Unity-clean; it `<Compile Include>`s `Assets/Scripts/**/*.cs` directly. The same assertions are mirrored as NUnit `EditMode` tests for in-Unity CI.)*
