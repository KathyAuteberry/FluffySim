using System.Numerics;

namespace AnoMech.Scenarios.Dmu;

public static class DmuConstants
{
    public const byte Level = 100;

    public static class BNpcBaseId
    {
        public const uint Kefka = 19504;
        public const uint GravenImage = 19505;
        public const uint Helper = 9020;
        public const uint GodKefka = 19506;
        public const uint KefkaClone = 19513;
    }

    public static class BNpcNameId
    {
        public const uint Kefka = 7131;
        public const uint GravenImage = 7132;
    }

    public static class LayoutId
    {
        public const uint Kefka = 0xBC1726;
        public const uint GravenImage = 0xBC1727;
        public const uint CentreStatue = 0xBC7C91;
        public const uint PulseWaveStatue = 0xBC178D;
        public const uint WaveCannonStatue = 0xBC178E;
        public const uint GravitasStatue = 0xBC178F;
        public const uint VitrophyreStatue = 0xBC1790;
        public const uint WestCleaveStatue = 0xBC1791;
        public const uint EastCleaveStatue = 0xBC1792;
        public const uint SleepStatue = 0xBC1793;
        public const uint IndolentWillStatue = 0xBC1794;
        public const uint GodKefka = 0xBC1728;
        public const uint KefkaClone = 0xBC172F;
        public const uint ConfusionStatue = 0xBC1795;
        public const uint AveMariaStatue = 0xBC1796;
        // Trine telegraphs, clockwise from north, then the centre one.
        public const uint TrineNorth = 0xBC17E6;
        public const uint TrineEast = 0xBC17E3;
        public const uint TrineSoutheast = 0xBC17F1;
        public const uint TrineSouth = 0xBC17E0;
        public const uint TrineWest = 0xBC17ED;
        public const uint TrineNorthwest = 0xBC17F5;
        public const uint TrineCentre = 0xBC17E1;
    }

    // The multiplayer allowlist harvests these by class name.
    public static class EObjId
    {
        public const uint CentreStatue = 2015156;
        public const uint PlainStatue = 2015163;
        public const uint MiddleStatue = 2015164;
        public const uint EastStatue = 2015165;
        public const uint SleepStatue = 2015166;
        public const uint ConfusionStatue = 2015167;
        public const uint GravitasPuddle = 2015266;
        public const uint TelePortent = 2015267;
        public const uint TrinePointingEast = 2015154;
        public const uint TrinePointingWest = 2015155;
    }

    public static class TetherId
    {
        public const ushort GravenImage = 0x2D;
    }

    public static class EventId
    {
        public const uint StatueDirector = 0x800375D2;
    }

    public static class ActionId
    {
        public const uint AutoAttack = 0xC252;
        public const uint Teleport = 0xC3FD;
        public const uint RevoltingRuin = 0xC403;
        public const uint RevoltingRuinSecond = 0xC4E1;
        public const uint GravenImage = 0xBCF2;
        public const uint MysteryMagic = 0xBA94;
        public const uint BlizzardBoss = 0xBA95;
        public const uint BlizzardShown = 0xBA98;
        public const uint BlizzardFake = 0xBA9B;
        public const uint BlizzardHidden = 0xBA9E;
        public const uint ThunderShown = 0xBA9F;
        public const uint ThunderFake = 0xBAA0;
        public const uint ThunderHidden = 0xBAA1;
        public const uint FlagrantFireSpread = 0xBAA2;
        public const uint FlagrantFireStack = 0xBAA3;
        public const uint DoubleTroubleTrap = 0xBAA6;
        public const uint DoubleTroubleTrapBurst = 0xBAA7;
        public const uint WaveCannon = 0xBAA8;
        public const uint PulseWave = 0xBAA9;
        public const uint Explosion = 0xBAAA;
        public const uint UnmitigatedExplosion = 0xBAAB;
        public const uint Gravitas = 0xBAAC;
        public const uint GravitationalExplosion = 0xBAAD;
        public const uint GravityIII = 0xBAAF;
        public const uint Vitrophyre = 0xBAB0;
        public const uint GravitationalWave = 0xBAB1;
        public const uint IntemperateWill = 0xBAB2;
        public const uint LightOfJudgment = 0xC622;
        public const uint TeleTrouncing = 0xBAB9;
        public const uint TeleTrouncingDrop = 0xBABA;
        public const uint AveMaria = 0xBAB3;
        public const uint IndolentWill = 0xBAB4;
        public const uint IndulgentWill = 0xBAB5;
        public const uint IdyllicWill = 0xBAB6;
        public const uint StatueGlance = 0xC554;
        public const uint StatueGlanceEnd = 0xC555;
        public const uint Forsaken = 0xBABC;
        public const uint ForsakenLightOfJudgment = 0xBABD;
        public const uint ThePathOfLight = 0xBABE;
        public const uint TheRiverOfLight = 0xBABF;
        public const uint Spelldriver = 0xBAC0;
        public const uint Spellscatter = 0xBAC1;
        public const uint Spellwave = 0xBAC2;
        public const uint FuturesEnd = 0xBAD2;
        public const uint PastsEnd = 0xBAD3;
        public const uint FuturesEndResolve = 0xBAD6;
        public const uint PastsEndResolve = 0xBAD7;
        public const uint FuturesEndCloneResolve = 0xBAD8;
        public const uint PastsEndCloneResolve = 0xBAD9;
        public const uint AllThingsEndingFuture = 0xBADC;
        public const uint AllThingsEndingPast = 0xBADD;
        public const uint Trine = 0xBADF;
        public const uint TrineBurst = 0xBAE0;
        public const uint WingsOfDestructionLeft = 0xBACD;
        public const uint WingsOfDestructionRight = 0xBACE;
        public const uint WingsOfDestructionBuster = 0xBACF;
        public const uint WingsOfDestructionBusterCast = 0xC487;
        public const uint UltimateEmbrace = 0xC24C;
    }

    public static class StatusId
    {
        public const ushort MagicVulnerabilityUp = 0xB7D;
        public const ushort DoubleTroubleTrap = 0x13D6;
        public const ushort Confused = 0x503;
        public const ushort Sleep = 0x131E;
        public const ushort Bind = 0x9D6;
        public const ushort DamageDown = 0xB5F;
        // Tele-portent by arrow: N, S, E, W. The second set marks a player's other, turned arrow.
        public static readonly ushort[] TelePortent = [0x130C, 0x130D, 0x130E, 0x130F];
        public static readonly ushort[] TelePortentTurned = [0x13D7, 0x13D8, 0x13D9, 0x13DA];
        public const ushort SpellsTrouble = 0x13DB;
        // Hidden, one per Forsaken lockon: the shape a player's next tower casts.
        public const ushort ForsakenStackShape = 0x13DC;
        public const ushort ForsakenChariotShape = 0x13DD;
        public const ushort ForsakenConeShape = 0x13DE;
    }

    public static class LockonId
    {
        public const uint Tankbuster = 0xDA;
        public const uint Spread = 0x7F;
        public const uint Stack = 0x80;
        public const uint FireLie = 0x2A1;
        public const uint FireTruth = 0x2A2;
        public const uint IceLie = 0x2A3;
        public const uint IceTruth = 0x2A4;
        public const uint ThunderLie = 0x2A5;
        public const uint ThunderTruth = 0x2A6;
        public const uint ForsakenStack = 715;
        public const uint ForsakenChariot = 716;
        public const uint ForsakenCone = 717;
        public const uint UltimateEmbrace = 0x103;
    }

    public static class KnockbackId
    {
        public const uint PulseWave = 72;
        public const uint DoubleTroubleTrap = 240;
    }

    public static class Bgm
    {
        public const ushort BetweenPulls = 20293;
        public const ushort Opening = 20289;
        public const ushort AfterFirstJudgment = 20290;
        public const ushort AfterSecondJudgment = 20291;
        public const ushort Forsaken = 20292;
    }

    public const uint GravitasPuddleArg2Base = 0x500003;
    public const uint TelePortentArg2Base = 0x500003;

    // Director 0x800375D2, slot 0: Light of Judgment 1, then 2.
    public const uint FirstJudgmentArena = 0x00080004;
    public const uint SecondJudgmentArena = 0x00200010;

    // Local to the arena centre; Y is height. The spawn packets' own values.
    public sealed record Statue(uint EObjId, uint LayoutId, Vector3 Position, uint Arg2);

    public static class Statues
    {
        public static readonly Statue Centre = new(EObjId.CentreStatue, LayoutId.CentreStatue, new(0f, 0f, 0f), 0x400003);
        public static readonly Statue PulseWave = new(EObjId.PlainStatue, LayoutId.PulseWaveStatue, new(0f, 18.5f, -44f), 0x420003);
        public static readonly Statue WaveCannon = new(EObjId.PlainStatue, LayoutId.WaveCannonStatue, new(0f, 0f, -35f), 0x410003);
        public static readonly Statue Gravitas = new(EObjId.MiddleStatue, LayoutId.GravitasStatue, new(2.5f, 22.5f, -73f), 0x450003);
        public static readonly Statue Vitrophyre = new(EObjId.EastStatue, LayoutId.VitrophyreStatue, new(26f, 7f, -58.5f), 0x460003);
        public static readonly Statue EastCleave = new(EObjId.EastStatue, LayoutId.EastCleaveStatue, new(16f, 6.5f, -57f), 0x440003);
        public static readonly Statue WestCleave = new(EObjId.MiddleStatue, LayoutId.WestCleaveStatue, new(-8f, 15f, -73f), 0x430003);
        public static readonly Statue Confusion = new(EObjId.ConfusionStatue, LayoutId.ConfusionStatue, new(-5f, 27f, -75f), 0x490003);
        public static readonly Statue Sleep = new(EObjId.SleepStatue, LayoutId.SleepStatue, new(7f, 8.5f, -57f), 0x4A0003);
        public static readonly Statue AveMaria = new(EObjId.ConfusionStatue, LayoutId.AveMariaStatue, new(-5f, 12.5f, -75f), 0x470003);
        public static readonly Statue IndolentWill = new(EObjId.SleepStatue, LayoutId.IndolentWillStatue, new(5.25f, 13.5f, -66f), 0x480003);

        public static readonly Statue[] All = [Centre, PulseWave, WaveCannon, Gravitas, Vitrophyre, EastCleave, WestCleave, Confusion, Sleep, AveMaria, IndolentWill];
    }

    // The timeline beats every statue plays: tether or appear, resolve, wind-up, fire.
    public static class Beat
    {
        public static readonly (uint State, uint Mask) Activate = (0x1, 0x2);
        public static readonly (uint State, uint Mask) Resolve = (0x10, 0x20);
        public static readonly (uint State, uint Mask) WindUp = (0x40, 0x80);
        public static readonly (uint State, uint Mask) Fire = (0x100, 0x200);
    }

    public const ushort StatueRestState = 4;
}
