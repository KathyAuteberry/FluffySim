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
    }

    public static class StatusId
    {
        public const ushort MagicVulnerabilityUp = 0xB7D;
        public const ushort DoubleTroubleTrap = 0x13D6;
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
    }

    public const ushort GravenTether = 0x2D;

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
    }

    public const uint StatueDirector = 0x800375D2;

    // Local to the arena centre; Y is height. The spawn packets' own values.
    public sealed record Statue(uint EObjId, uint LayoutId, Vector3 Position, uint Arg2);

    public static class Statues
    {
        public static readonly Statue Centre = new(2015156, 0xBC7C91, new(0f, 0f, 0f), 0x400003);
        public static readonly Statue PulseWave = new(2015163, 0xBC178D, new(0f, 18.5f, -44f), 0x420003);
        public static readonly Statue WaveCannon = new(2015163, 0xBC178E, new(0f, 0f, -35f), 0x410003);
        public static readonly Statue Gravitas = new(2015164, 0xBC178F, new(2.5f, 22.5f, -73f), 0x450003);
        public static readonly Statue Vitrophyre = new(2015165, 0xBC1790, new(26f, 7f, -58.5f), 0x460003);
        public static readonly Statue EastCleave = new(2015165, 0xBC1792, new(16f, 6.5f, -57f), 0x440003);
        public static readonly Statue WestCleave = new(2015164, 0xBC1791, new(-8f, 15f, -73f), 0x430003);
        public static readonly Statue Confusion = new(2015167, 0xBC1795, new(-5f, 27f, -75f), 0x490003);
        public static readonly Statue Sleep = new(2015166, 0xBC1793, new(7f, 8.5f, -57f), 0x4A0003);
        public static readonly Statue AveMaria = new(2015167, 0xBC1796, new(-5f, 12.5f, -75f), 0x470003);
        public static readonly Statue IndolentWill = new(2015166, 0xBC1794, new(5.25f, 13.5f, -66f), 0x480003);

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
