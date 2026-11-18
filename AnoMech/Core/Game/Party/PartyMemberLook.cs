namespace AnoMech.Core.Game.Party;

// Character-creator choices for a female Miqo'te doppel. Colors index the tribe's palettes in
// human.cmp. Height never reaches the sim: mechanics resolve against positions, not the model.
public sealed record PartyMemberLook(
    byte Tribe,
    byte Height,
    byte Face,
    byte Hairstyle,
    byte SkinColor,
    byte EyeColor,
    byte HairColor,
    byte HighlightsColor,
    byte Eyebrows,
    byte EyeShape,
    byte Nose,
    byte Jaw,
    byte Mouth,
    byte TailShape)
{
    public const byte RaceMiqote = 4;
    public const byte TribeSeekerOfTheSun = 7;
    public const byte TribeKeeperOfTheMoon = 8;
    public const byte SexFemale = 1;
}
