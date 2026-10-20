using System;
using System.Numerics;

namespace AnoMech.Scenarios.Dsr;

public static class DsrP5Constants
{
    public static Vector3 AtBearing(float degrees, float radius) => new(radius * MathF.Sin(degrees * MathF.PI / 180), 0, -radius * MathF.Cos(degrees * MathF.PI / 180));
    public static class BNpcBaseId
    {
        public const uint Dummy = 9020;
        public const uint Grinnaux = 12602;
        public const uint Charibert = 12603;
        public const uint Zephirin = 12592;
        public const uint Vellguine = 12633;
        public const uint Paulecrain = 12634;
        public const uint Ignasse = 12635;
        public const uint Guerrique = 12637;
        public const uint Noudenet = 12639;
        public const uint DarkThordan = 12611;
        public const uint Vedrfolnir = 12646;
        public const uint Darkscale = 12647;
        public const uint Vidofnir = 12648;
        public const uint DarkscaleDiver = 12630;
        public const uint VidofnirDiver = 12631;
        public const uint MeteorCircle = 13920;
    }
    public static class BNpcNameId
    {
        public const uint Grinnaux = 3639;
        public const uint Charibert = 3642;
        public const uint Thordan = 3632;
        public const uint Zephirin = 3633;
        public const uint Vellguine = 3636;
        public const uint Paulecrain = 3637;
        public const uint Ignasse = 3638;
        public const uint Guerrique = 3641;
        public const uint Noudenet = 3644;
        public const uint Darkscale = 3983;
        public const uint Vedrfolnir = 3984;
        public const uint Vidofnir = 11314;
        public const uint MeteorCircle = 4386;
    }
    public static class ActionId
    {
        public const uint EmptyDimension = 0x62DA;
        public const uint FaithUnmoving = 0x62DC;
        public const uint Heavensflame = 0x62DE;
        public const uint HeavensflameHit = 0x62DF;
        public const uint HolyChain = 0x62E0;
        public const uint ThordanAttack = 0x63BB;
        public const uint ThordanLeap = 0x63C4;
        public const uint AscalonsMight = 0x63C5;
        public const uint AncientQuaga = 0x63C6;
        public const uint HeavenlyHeel = 0x63C7;
        public const uint LightningStorm = 0x63CC;
        public const uint LightningStormHit = 0x63CD;
        public const uint HeavyImpactWindup = 0x63D5;
        public const uint HeavyImpact = 0x63D6;
        public const uint HeavyImpactRing1 = 0x63D7;
        public const uint HeavyImpactRing2 = 0x63D8;
        public const uint HeavyImpactRing3 = 0x63D9;
        public const uint HeavyImpactRing4 = 0x63DA;
        public const uint DragonsGaze = 0x63D0;
        public const uint DragonsGazeHit = 0x63D1;
        public const uint DragonsGlory = 0x63D2;
        public const uint KnightsOfTheRound = 0x63ED;
        public const uint AscalonsMercyRevealed = 0x63CA;
        public const uint AscalonsMercyRevealedCone = 0x63CB;
        public const uint AltarFlare = 0x63E4;
        public const uint AltarFlareEruption = 0x63E5;
        public const uint Incarnation = 0x6B86;
        public const uint DragonsEye = 0x6B87;
        public const uint WrathOfTheHeavens = 0x6B89;
        public const uint SpiralPierce = 0x6B8A;
        public const uint TwistingDive = 0x6B8B;
        public const uint Twister = 0x6B8C;
        public const uint CauterizeDarkscale = 0x6B8D;
        public const uint CauterizeVidofnir = 0x6B8E;
        public const uint ChainLightning = 0x6B8F;
        public const uint ChainLightningHit = 0x6B90;
        public const uint LiquidHeaven = 0x6B91;
        public const uint DeathOfTheHeavens = 0x6B92;
        public const uint WrathSkywardLeap = 0x72A2;
        public const uint DeathSpearOfTheFury = 0x6B93;
        public const uint Deathstorm = 0x6B94;
        public const uint WingsOfSalvation = 0x6B95;
        public const uint WingsOfSalvationPuddle = 0x6B96;
        public const uint HolyMeteor = 0x6B97;
    }
    public static class StatusId
    {
        public const ushort BurningChains = 0x301;
        public const ushort FireResistanceDownII = 0xB56;
        public const ushort PhysicalVulnerabilityUp = 0xB7C;
        public const ushort MagicVulnerabilityUp = 0xB7D;
        public const ushort DamageDown = 0xC5E;
        public const ushort SlashingResistanceDown = 0xC3A;
        public const ushort Thunderstruck = 0xB11;
        public const ushort Doom = 0xBA0;
    }
    public static class TetherId
    {
        public const ushort BurningChains = 0x09;
        public const ushort SpiralPierce = 0x05;
        public const ushort DragonToThordan = 0xA5;
    }
    public static class EObjId
    {
        public const uint Twister = 0x1E8910;
        public const uint LiquidHeaven = 0x1EB684;
        public const uint WingsOfSalvation = 0x1EB685;
    }
    public static class TimelineId
    {
        public const ushort WarpEnd = 0x1E43;
        public const ushort DragonAppear = 0x1E44;
    }
    public static class LockonId
    {
        public const uint Circle = 0x119;
        public const uint Triangle = 0x11A;
        public const uint Square = 0x11B;
        public const uint Cross = 0x11C;
        public const uint WrathDefamation = 0x0E;
        public const uint Divebomb = 0x14;
    }
}
