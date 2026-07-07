using System;
using System.Collections.Generic;
using UnityEngine;
using Echo.Core.Echo;
using Echo.Core.Replay;

namespace Echo.Unity
{
    /// <summary>
    /// Data-driven level (docs/06 §6). Designers author levels as assets — tiles, spawn, plates, doors,
    /// the allowed evolution-gate mask, Echo cap — with NO code. New levels = new assets.
    /// </summary>
    [CreateAssetMenu(menuName = "Echo/Level Definition", fileName = "Level")]
    public sealed class LevelDefinition : ScriptableObject
    {
        [Serializable] public struct SolidRect { public int X, Y, W, H; }
        [Serializable] public struct FloorGapDef { public int X, W; } // carves a pit into the floor row (e.g. Phase Platform #13)
        [Serializable] public struct PlateDef { public int LinkId; public Vector2 Min, Max; }
        [Serializable] public struct DoorDef { public int LinkId; public Vector2 Center, HalfExtents; public bool Invert; } // Invert = phase platform (#13)
        [Serializable] public struct QuorumDef { public int LinkId; public Vector2 Min, Max; public int Threshold; }
        [Serializable] public struct CrateDef { public Vector2 Position; public Vector2 HalfExtents; }
        [Serializable] public struct HazardDef { public Vector2 Min, Max; public bool Consumable; }
        [Serializable] public struct SwitchDef { public int LinkId; public Vector2 Min, Max; }   // same-tick switch (#5)
        [Serializable] public struct BouncePadDef { public Vector2 Center, HalfExtents; }          // bounce pad (#15)
        [Serializable] public struct PortalPairDef { public Vector2 AMin, AMax, APoint, BMin, BMax, BPoint; } // anchored portals (#34)
        [Serializable] public struct TimeFieldDef { public Vector2 Min, Max; public float Scale; }  // slow (<1) / fast (>1) field
        [Serializable] public struct MagnetDef { public Vector2 Position; public float Radius; public float Strength; } // polarity (#20): +attract, -repel
        [Serializable] public struct MetalDef { public Vector2 Position; public Vector2 HalfExtents; }                  // body a magnet can move
        [Serializable] public struct CheckpointDef { public Vector2 Min, Max; }                     // arrival order (#24): required in list order
        [Serializable] public struct ArrivalDoorDef { public int LinkId; public Vector2 Center, HalfExtents; }
        [Serializable] public struct TorchDef { public Vector2 Position; public float Radius; }     // torch sequence (#28): required in list order
        [Serializable] public struct TorchDoorDef { public int LinkId; public Vector2 Center, HalfExtents; }
        [Serializable] public struct AntiEchoFieldDef { public Vector2 Min, Max; }                  // anti-echo field (#45): deletes Echoes, spares the player
        [Serializable] public struct WindZoneDef { public int LinkId; public Vector2 Min, Max; public Vector2 Force; }
        [Serializable] public struct WindSwitchDef { public int LinkId; public Vector2 Min, Max; }
        [Serializable] public struct LightPlatformDef { public Vector2 Center, HalfExtents; }       // light-solid (#40): solid only near a held lantern
        [Serializable] public struct PressurePairDef { public int LinkId; public Vector2 AMin, AMax, BMin, BMax; }
        [Serializable] public struct PressureDoorDef { public int LinkId; public Vector2 Center, HalfExtents; }
        [Serializable] public struct GravityZoneDef { public Vector2 Min, Max; }                    // gravity memory (#37): reverses gravity inside
        [Serializable] public struct DelayedChargeDef { public Vector2 ArmMin, ArmMax, BlastMin, BlastMax; public int FuseTicks; } // delayed charge (#11)
        [Serializable] public struct ElevatorDef { public Vector2 StartCenter, HalfExtents, ZoneMin, ZoneMax; public int MaxWeightForFullRise; public float MaxRise; } // counterweight self (#4)
        [Serializable] public struct MassScaleDef { public int LinkId; public Vector2 Min, Max; public int Target; }            // mass scale (#18)
        [Serializable] public struct MassScaleDoorDef { public int LinkId; public Vector2 Center, HalfExtents; }
        [Serializable] public struct PulleyDef { public int LinkId; public Vector2 ZoneMin, ZoneMax; public int Threshold; public float RiseRate, SlipRate; } // pulley crew (#21)
        [Serializable] public struct PulleyDoorDef { public int LinkId; public Vector2 Center, HalfExtents; }
        [Serializable] public struct NoTouchZoneDef { public Vector2 Min, Max; }                                                // no-touch paradox (#25)
        [Serializable] public struct CrankZoneDef { public int LinkId; public Vector2 Min, Max; }                               // generator crank (#31)
        [Serializable] public struct CrankDoorDef { public int LinkId; public Vector2 Center, HalfExtents; }
        [Serializable] public struct EchoLadderZoneDef { public Vector2 Min, Max; }                         // echo ladder (#2)
        [Serializable] public struct ColoredItemDef { public int CrateIndex; public int Color; }            // color carry (#35): refers to Crates[CrateIndex]
        [Serializable] public struct KeyholeDef { public int LinkId; public int RequiredColor; public Vector2 Min, Max; }
        [Serializable] public struct ColorDoorDef { public int LinkId; public Vector2 Center, HalfExtents; }
        [Serializable] public struct MemoryCheckpointDef { public Vector2 Min, Max; }                       // memory-lock door (#43): required in list order, per-body
        [Serializable] public struct MemoryDoorDef { public int LinkId; public Vector2 Center, HalfExtents; }
        [Serializable] public struct CumulativeLeverDef { public int LinkId; public Vector2 ZoneMin, ZoneMax; public float ChargePerActivation, Threshold; } // #49
        [Serializable] public struct CumulativeLeverDoorDef { public int LinkId; public Vector2 Center, HalfExtents; }
        [Serializable] public struct EchoPasswordDef { public Vector2 EntryMin, EntryMax; public InputButtons[] TargetSequence; } // #36
        [Serializable] public struct EchoPasswordDoorDef { public int LinkId; public Vector2 Center, HalfExtents; }
        [Serializable] public struct TwoBodyLockDef { public int LinkId; public Vector2 AMin, AMax, BMin, BMax; public int ToleranceTicks; } // #41
        [Serializable] public struct TwoBodyLockDoorDef { public int LinkId; public Vector2 Center, HalfExtents; }
        [Serializable] public struct CrumbleTileDef { public Vector2 Center, HalfExtents; public int FuseTicks; }            // #30
        [Serializable] public struct DrainDef { public Vector2 Min, Max; }                                                  // #32 Flood Control
        [Serializable] public struct FloodConfigDef { public float StartY, MaxY; }
        [Serializable] public struct NegotiationPlateDef { public int LinkId; public Vector2 Min, Max; public float TrustThreshold; } // #50
        [Serializable] public struct NegotiationDoorDef { public int LinkId; public Vector2 Center, HalfExtents; }
        [Serializable] public struct TurretEmitterDef { public Vector2 ControlMin, ControlMax, BlastMin, BlastMax; }         // #22, reused for #48 Echo Crossfire (2 staggered emitters)
        [Serializable] public struct DominoDef { public Vector2 ZoneMin, ZoneMax; public int FallTicks; }                   // #39: in chain order, domino 0 needs the zone
        [Serializable] public struct DominoDoorDef { public int LinkId; public Vector2 Center, HalfExtents; }
        [Serializable] public struct PendulumDef { public Vector2 Center, HalfExtents; public float SwingHalfWidth; public int PeriodTicks; } // #47
        [Serializable] public struct DecoyHazardDef { public Vector2 Start, HalfExtents; public float ChaseSpeed; }         // #9
        [Serializable] public struct BodyShieldBeamDef { public Vector2 Min, Max; public int RechargeTicks; }               // #12 (recharge 0 = fires every tick)
        [Serializable] public struct MirrorPointDef { public Vector2 Min, Max; }                                            // #6: all must be simultaneously occupied
        [Serializable] public struct MirrorRelayDoorDef { public int LinkId; public Vector2 Center, HalfExtents; }
        [Serializable] public struct MirroredCheckpointDef { public Vector2 Min, Max; }                                     // #17: player track, required in order
        [Serializable] public struct MirrorCheckpointDef { public Vector2 Min, Max; }                                       // #17: mirror track (any Echo), required in order
        [Serializable] public struct MirroredPathDoorDef { public int LinkId; public Vector2 Center, HalfExtents; }
        [Serializable] public struct CrusherPistonDef { public Vector2 Min, Max; public int ExtendedTicks, RetractedTicks; } // Content Bible D2
        [Serializable] public struct CaretakerDroneDef { public Vector2 Start, HalfExtents; public float ChaseSpeed; }      // Content Bible D19
        [Serializable] public struct SecretDef { public int Id; public Vector2 Min, Max; }                                  // Truth (#docs/02 §8): campaign-unique Id, found once per profile

        [Header("Identity")]
        public string LevelId = "W1_L1";
        public long SaveSeed = 0x5EED;

        [Header("Grid (tiles)")]
        public int Width = 48;
        public int Height = 24;
        public int FloorRow = 0;
        public List<SolidRect> Solids = new List<SolidRect>();
        public List<FloorGapDef> FloorGaps = new List<FloorGapDef>();

        [Header("Play")]
        public Vector2 Spawn = new Vector2(10, 3);
        public int MaxEchoes = 6;
        public GateMask EnabledGates = GateMask.None; // tutorials keep Echoes obedient; later worlds open gates

        [Header("Exit")]
        // Reaching this zone completes the level. The default (the far-right corridor) sits past the exit
        // door of every authored level, so levels only override it for unusual layouts.
        public Vector2 ExitMin = new Vector2(44, 0);
        public Vector2 ExitMax = new Vector2(47, 24);

        [Header("Mechanics")]
        public List<PlateDef> Plates = new List<PlateDef>();
        public List<DoorDef> Doors = new List<DoorDef>();
        public List<QuorumDef> Quorums = new List<QuorumDef>();
        public List<CrateDef> Crates = new List<CrateDef>();
        public List<HazardDef> Hazards = new List<HazardDef>();
        public List<SwitchDef> Switches = new List<SwitchDef>();
        public List<DoorDef> SwitchDoors = new List<DoorDef>();   // doors latched by same-tick switches
        public List<BouncePadDef> BouncePads = new List<BouncePadDef>();
        public List<PortalPairDef> Portals = new List<PortalPairDef>();
        public List<TimeFieldDef> TimeFields = new List<TimeFieldDef>();
        public List<MagnetDef> Magnets = new List<MagnetDef>();
        public List<MetalDef> Metals = new List<MetalDef>();
        public List<CheckpointDef> ArrivalCheckpoints = new List<CheckpointDef>();
        public List<ArrivalDoorDef> ArrivalDoors = new List<ArrivalDoorDef>();
        public List<TorchDef> Torches = new List<TorchDef>();
        public List<TorchDoorDef> TorchDoors = new List<TorchDoorDef>();
        public List<AntiEchoFieldDef> AntiEchoFields = new List<AntiEchoFieldDef>();
        public List<WindZoneDef> WindZones = new List<WindZoneDef>();
        public List<WindSwitchDef> WindSwitches = new List<WindSwitchDef>();
        public List<LightPlatformDef> LightPlatforms = new List<LightPlatformDef>();
        public List<PressurePairDef> PressurePairs = new List<PressurePairDef>();
        public List<PressureDoorDef> PressureDoors = new List<PressureDoorDef>();
        public List<GravityZoneDef> GravityZones = new List<GravityZoneDef>();
        public List<DelayedChargeDef> DelayedCharges = new List<DelayedChargeDef>();
        public List<ElevatorDef> Elevators = new List<ElevatorDef>();
        public List<MassScaleDef> MassScales = new List<MassScaleDef>();
        public List<MassScaleDoorDef> MassScaleDoors = new List<MassScaleDoorDef>();
        public List<PulleyDef> Pulleys = new List<PulleyDef>();
        public List<PulleyDoorDef> PulleyDoors = new List<PulleyDoorDef>();
        public List<NoTouchZoneDef> NoTouchZones = new List<NoTouchZoneDef>();
        public List<CrankZoneDef> CrankZones = new List<CrankZoneDef>();
        public List<CrankDoorDef> CrankDoors = new List<CrankDoorDef>();
        public List<EchoLadderZoneDef> EchoLadderZones = new List<EchoLadderZoneDef>();
        public List<ColoredItemDef> ColoredItems = new List<ColoredItemDef>();
        public List<KeyholeDef> Keyholes = new List<KeyholeDef>();
        public List<ColorDoorDef> ColorDoors = new List<ColorDoorDef>();
        public List<MemoryCheckpointDef> MemoryCheckpoints = new List<MemoryCheckpointDef>();
        public List<MemoryDoorDef> MemoryDoors = new List<MemoryDoorDef>();
        public List<CumulativeLeverDef> CumulativeLevers = new List<CumulativeLeverDef>();
        public List<CumulativeLeverDoorDef> CumulativeLeverDoors = new List<CumulativeLeverDoorDef>();
        public EchoPasswordDef EchoPassword; // single instance; leave TargetSequence null/empty to disable
        public List<EchoPasswordDoorDef> EchoPasswordDoors = new List<EchoPasswordDoorDef>();
        public List<TwoBodyLockDef> TwoBodyLocks = new List<TwoBodyLockDef>();
        public List<TwoBodyLockDoorDef> TwoBodyLockDoors = new List<TwoBodyLockDoorDef>();
        public List<CrumbleTileDef> CrumbleTiles = new List<CrumbleTileDef>();
        public List<DrainDef> Drains = new List<DrainDef>();
        public FloodConfigDef Flood; // leave MaxY == 0 to disable
        public bool EnableEchoConveyor; // #14: any character resting on a moving Echo rides along
        public List<NegotiationPlateDef> NegotiationPlates = new List<NegotiationPlateDef>();
        public List<NegotiationDoorDef> NegotiationDoors = new List<NegotiationDoorDef>();
        public List<TurretEmitterDef> TurretEmitters = new List<TurretEmitterDef>();
        public List<DominoDef> Dominoes = new List<DominoDef>();
        public List<DominoDoorDef> DominoDoors = new List<DominoDoorDef>();
        public List<PendulumDef> Pendulums = new List<PendulumDef>();
        public List<DecoyHazardDef> DecoyHazards = new List<DecoyHazardDef>();
        public List<BodyShieldBeamDef> BodyShieldBeams = new List<BodyShieldBeamDef>();
        public List<MirrorPointDef> MirrorPoints = new List<MirrorPointDef>();
        public List<MirrorRelayDoorDef> MirrorRelayDoors = new List<MirrorRelayDoorDef>();
        public List<MirroredCheckpointDef> MirroredPlayerCheckpoints = new List<MirroredCheckpointDef>();
        public List<MirrorCheckpointDef> MirrorPathCheckpoints = new List<MirrorCheckpointDef>();
        public List<MirroredPathDoorDef> MirroredPathDoors = new List<MirroredPathDoorDef>();
        public List<CrusherPistonDef> CrusherPistons = new List<CrusherPistonDef>();
        public List<CaretakerDroneDef> CaretakerDrones = new List<CaretakerDroneDef>();
        public List<SecretDef> Secrets = new List<SecretDef>(); // presentation-side (SimRunner), never a sim module
    }
}
