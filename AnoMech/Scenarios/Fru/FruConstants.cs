namespace AnoMech.Scenarios.Fru;

public static class FruConstants
{
    // Sent by the content director (DirectorUpdate 0x80000001); 1 silences the music.
    public static class Bgm
    {
        public const ushort Fatebreaker = 20134;
        public const ushort Usurper = 20135;
        public const ushort Intermission = 20136;
        public const ushort Oracle = 20105;
        public const ushort OracleAndUsurper = 20137;
        public const ushort Pandora = 20099;
    }

    public static class WeatherId
    {
        public const byte Fatebreaker = 2;
        public const byte Usurper = 35;
        public const byte Oracle = 105;
        public const byte TimeStop = 106;
        public const byte Pandora = 108;
    }

    public static class MapEffect
    {
        public const ushort Hidden = 4;
        public const ushort Shown = 1;
        public const uint Show = 0x00020001;
        public const uint Hide = 0x00080004;
        public const byte P2Platform = 23;
        public const byte OracleStage = 40;
        public const uint OracleStageTimeStop = 0x00200010;
        public const uint OracleStageResume = 0x00400001;
        public const byte OracleFloor = 41;
        public const byte FragmentOfFate = 46;
        public const byte PandoraArena = 47;
        public const byte FirstRelativityHourglass = 26;
        public const uint HourglassFlowing = 0x00200010;
        public const uint HourglassDraining = 0x00800040;
        public const uint HourglassEmptying = 0x02000100;
        public const uint TowerBase = 0x00010004;
        public const uint TowerEmpty = 0x00400001;
        public const uint DarklitTowerEmpty = 0x01000001;
        public static readonly uint[] TowerOccupied = [0x00200010, 0x00800040, 0x02000100, 0x08000400];
    }

    public static class BNpcBaseId
    {
        public const uint UsurperOfFrost = 17823;
        public const uint OraclesReflection = 17824;
        public const uint HolyLight = 17826;
        public const uint Helper = 9020;
        public const uint OracleOfDarkness = 17831;
        public const uint DelightsHourglass = 17832;
        public const uint UsurperOfFrostP4 = 17833;
        public const uint OracleOfDarknessP4 = 17835;
        public const uint FragmentOfFate = 17841;
    }

    public static class BNpcNameId
    {
        public const uint UsurperOfFrost = 12809;
        public const uint OraclesReflection = 13554;
        public const uint HolyLight = 9318;
        public const uint OracleOfDarkness = 9832;
        public const uint DelightsHourglass = 9825;
        public const uint FragmentOfFate = 13559;
    }

    public static class ActionId
    {
        public const uint MirrorImage = 40180;
        public const uint DiamondDust = 40197;
        public const uint IcicleImpact = 40198;
        public const uint FrigidStone = 40199;
        public const uint FrigidNeedleCircle = 40200;
        public const uint FrigidNeedleCross = 40201;
        public const uint AxeKick = 40202;
        public const uint ScytheKick = 40203;
        public const uint DiamondDustHouseOfLight = 40206;
        public const uint HeavenlyStrike = 40207;
        public const uint LightRampant = 40212;
        public const uint BrightHunger = 40213;
        public const uint InescapableIllumination = 40214;
        public const uint RefulgentFate = 40215;
        public const uint Lightsteep = 40216;
        public const uint PowerfulLight = 40217;
        public const uint LuminousHammer = 40218;
        public const uint Burst = 40219;
        public const uint BanishPairs = 40220;
        public const uint BanishSpread = 40221;
        public const uint BanishPairsHit = 40222;
        public const uint BanishSpreadHit = 40223;
        public const uint HouseOfLight = 40189;
        public const uint HouseOfLightHit = 40188;
    }

    public static class StatusId
    {
        public const ushort Chains = 4157;
        public const ushort Curse = 4158;
        public const ushort WeightOfLight = 4159;
        public const ushort Lightsteeped = 2257;
        public const ushort MarkOfMortality = 4372;
    }

    public const uint PuddleEObjId = 0x1EBC4F;
    public const ushort PuddleLockon = 375;
    public const ushort ChainsTether = 110;
    public const ushort CurseTether = 111;
}
