using System;
using System.Numerics;

namespace AnoMech.Scenarios.TopNative;

public static class TopNativeConstants
{
    public const byte Level = 90;
    public const float ArenaRadius = 20f;

    public static class BNpcBaseId
    {
        public const uint Helper = 9020;
        public const uint OmegaP1 = 15708;
        public const uint OmegaM = 15712;
        public const uint OmegaF = 15713;
        public const uint OmegaMClone = 15714;
        public const uint OmegaFClone = 15715;
        public const uint OpticalUnit = 15716;
        public const uint OmegaP3 = 15717;
        public const uint LeftArmUnit = 15718;
        public const uint RightArmUnit = 15719;
    }

    public static class BNpcNameId
    {
        public const uint Omega = 7695;
        public const uint OmegaM = 7633;
        public const uint OmegaF = 7634;
        public const uint OmegaP2 = 7635;
        public const uint OmegaP3 = 7636;
        public const uint LeftArmUnit = 7637;
        public const uint RightArmUnit = 7638;
        public const uint OpticalUnit = 7640;
    }

    public static class LayoutId
    {
        public const uint OmegaP1 = 0x91D1DF;
        public const uint OmegaM = 0x91D426;
        public const uint OmegaF = 0x91D427;
        public const uint OmegaMClone = 0x91D42A;
        public const uint OmegaFClone = 0x91D42B;
        public const uint OpticalUnit = 0x91D42D;
        public const uint OmegaP3 = 0x91E420;
        public const uint LeftArmUnit = 0x91E421;
        public const uint RightArmUnit = 0x91E422;
    }

    public static class Bgm
    {
        public const ushort P1 = 962;
        public const ushort P2 = 963;
        public const ushort P3 = 950;
    }

    public static class WeatherId
    {
        public const byte P1 = 77;
        public const byte P2 = 78;
        public const byte P3 = 79;
        public const byte P4 = 89;
        public const byte HelloWorld = 176;
    }

    public static class MapEffect
    {
        public const uint Show = 0x00020001;
        public const uint Hide = 0x00080004;
        public const ushort Shown = 1;
        public const ushort Hidden = 4;
        public const byte P1Floor = 9;
        public const byte P2Floor = 10;
        public const byte P4Arena = 20;
    }

    public static class ActionId
    {
        public const uint AutoAttackP1 = 31741;
        public const uint AtomicRay = 31480;
        public const uint ProgramLoop = 31491;
        public const uint StorageViolation = 31492;
        public const uint StorageViolationObliteration = 31494;
        public const uint Blaster = 31495;
        public const uint BlasterRepeat = 31496;
        public const uint BlasterLast = 31497;
        public const uint BlasterAoe = 31498;
        public const uint Pantokrator = 31499;
        public const uint BallisticImpact = 31500;
        public const uint FlameThrowerFirst = 31501;
        public const uint FlameThrowerRest = 32368;
        public const uint GuidedMissileKyrios = 31502;
        public const uint CondensedWaveCannonKyrios = 31503;
        public const uint DiffuseWaveCannonKyrios = 31504;
        public const uint WaveCannonKyrios = 31505;
        public const uint LimitlessSynergyM = 31544;
        public const uint LimitlessSynergyF = 31545;
        public const uint SyntheticShield = 32369;
        public const uint OptimizedPassageOfArms = 31556;
        public const uint LaserShower = 31557;
        public const uint TeleportM = 31554;
        public const uint TeleportF = 31555;
        public const uint OptimizedSagittariusArrow = 31539;
        public const uint OptimizedBladedanceM = 31540;
        public const uint OptimizedBladedanceF = 31541;
        public const uint OptimizedBladedanceAoe = 32629;
        public const uint BeyondDefense = 31527;
        public const uint BeyondDefenseAoe = 31528;
        public const uint PilePitch = 31529;
        public const uint OptimizedMeteor = 31537;
        public const uint OptimizedMeteorAoe = 31538;
        public const uint CosmoMemoryM = 31522;
        public const uint CosmoMemoryF = 31523;
        public const uint CosmoMemoryAoe = 31524;
        public const uint WaveRepeater1 = 31567;
        public const uint WaveRepeater2 = 31568;
        public const uint WaveRepeater3 = 31569;
        public const uint WaveRepeater4 = 31570;
        public const uint SniperCannon = 31571;
        public const uint HighPoweredSniperCannon = 31572;
        public const uint ColossalBlow = 31566;
        public const uint HelloWorld = 31573;
        public const uint LatentDefect = 31599;
        public const uint CriticalError = 31588;
        public const uint OversampledWaveCannonR = 31595;
        public const uint OversampledWaveCannonL = 31596;
        public const uint OversampledWaveCannonAoe = 31597;
        public const uint IonEfflux = 31560;
        public const uint BlueScreen = 31611;
        public const uint WaveCannonP4 = 31617;
        public const uint WaveCannonP4Protean = 31614;
        public const uint WaveCannonP4Stack = 31615;
        public const uint WaveCannonP4Line = 31616;
        public const uint WaveCannonP4StackMarker = 22393;
        public const uint CriticalSynchronizationBug = 31574;
        public const uint CriticalOverflowBug = 31575;
        public const uint LatentSynchronizationDefect = 31576;
        public const uint LatentDefectFail = 31577;
        public const uint CriticalUnderflowBug = 31578;
        public const uint CriticalPerformanceBug = 31579;
        public const uint RedTowerExpireWipe = 31581;
        public const uint BlueTowerExpireWipe = 31582;
        public const uint RedTower = 31583;
        public const uint BlueTower = 31584;
        public const uint RedTowerUnsoakedWipe = 31585;
        public const uint BlueTowerUnsoakedWipe = 31586;
        public const uint Patch = 31587;
        public const uint PatchFail = 32505;
    }

    public static class StatusId
    {
        public const ushort FirstInLine = 3004;
        public const ushort SecondInLine = 3005;
        public const ushort ThirdInLine = 3006;
        public const ushort FourthInLine = 3451;
        public const ushort Looper = 3456;
        public const ushort TwiceComeRuin = 2534;
        public const ushort ThriceComeRuin = 2530;
        public const ushort HpPenalty = 3401;
        public const ushort MagicVulnerabilityUp = 2941;
        public const ushort VulnerabilityUp = 3366;
        public const ushort SniperCannonFodder = 3425;
        public const ushort HighPoweredSniperCannonFodder = 3426;
        public const ushort MonitorRight = 3452;
        public const ushort MonitorLeft = 3453;
        public const ushort SynchronizationCodeSmell = 3436;
        public const ushort OverflowCodeSmell = 3437;
        public const ushort UnderflowCodeSmell = 3438;
        public const ushort PerformanceCodeSmell = 3439;
        public const ushort LocalCodeSmell = 3503;
        public const ushort RemoteCodeSmell = 3441;
        public const ushort CriticalSynchronizationBug = 3524;
        public const ushort CriticalOverflowBug = 3525;
        public const ushort CriticalUnderflowBug = 3526;
        public const ushort CriticalPerformanceBug = 3429;
        public const ushort LocalRegression = 3529;
        public const ushort RemoteRegression = 3530;
        public const ushort LatentDefect = 3527;
        public const ushort LatentSynchronizationBug = 3434;
        public const ushort CascadingLatentDefect = 3528;
        public const ushort LatentPerformanceDefect = 3435;
        public const ushort SynchronizationDebugger = 3430;
        public const ushort OverflowDebugger = 3431;
        public const ushort UnderflowDebugger = 3432;
        public const ushort PerformanceDebugger = 3433;

        public static readonly ushort[] InLine = [FirstInLine, SecondInLine, ThirdInLine, FourthInLine];
        // Indexed by number - 1; the countdown to that number's Guided Missile / Condensed Wave Cannon.
        public static readonly ushort[] GuidedMissileIncoming = [3424, 3495, 3496, 3497];
        public static readonly ushort[] CondensedWaveCannon = [3509, 3510, 3507, 3508];
    }

    public static class TetherId
    {
        public const ushort Blaster = 89;
        public const ushort OptimizedBladedance = 84;
        public const ushort LocalCodeSmell = 200;
        public const ushort RemoteCodeSmell = 201;
        public const ushort LocalRegression = 224;
        public const ushort RemoteRegression = 225;
    }

    public static class LockonId
    {
        public const uint WaveCannonKyrios = 23;
        public const uint OptimizedMeteor = 346;
    }

    public static class EObjId
    {
        public const uint TowerTimer = 2013244;
        public const uint TowerSolo = 2013245;
        public const uint TowerStateUnk = 0x800503FE;
        public const uint TowerEventId = 0x800375AC;
    }

    // Scenario-local: +X east, -Z north.
    public static Vector3 Polar(float bearingDegrees, float radius)
    {
        var b = bearingDegrees * MathF.PI / 180f;
        return new Vector3(MathF.Sin(b) * radius, 0f, -MathF.Cos(b) * radius);
    }

    public static float BearingDegrees(Vector3 position)
        => (MathF.Atan2(position.X, -position.Z) * 180f / MathF.PI + 360f) % 360f;

    // Facing that looks along `bearingDegrees` (Rotation 0 faces +Z, south).
    public static float FacingBearing(float bearingDegrees) => MathF.PI - bearingDegrees * MathF.PI / 180f;
}
