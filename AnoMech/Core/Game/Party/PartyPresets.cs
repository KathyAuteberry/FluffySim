using System.Collections.Generic;
using AnoMech.Core.UserActions;

namespace AnoMech.Core.Game.Party;

// Hardcoded preset for the standard 8-job party of female Miqo'te stand-ins
// dressed in their respective level-50 artifact (AF1) gear.
//
// Standard is laid out in PartyRole order — index N == (PartyRole)N.
public static class PartyPresets
{
    // Tanks default to skipping Warrior (PLD player swaps to skipping Paladin).
    // Melee defaults to skipping Monk (MNK player skips Dragoon instead).
    // Casters and unknown jobs fall through to skipping Black Mage.
    public static PartyRole SkipRoleForJob(uint job) => (JobId)job switch
    {
        JobId.Paladin => PartyRole.OffTank,
        JobId.Warrior or JobId.DarkKnight or JobId.Gunbreaker => PartyRole.MainTank,
        JobId.WhiteMage or JobId.Astrologian => PartyRole.RegenHealer,
        JobId.Scholar or JobId.Sage => PartyRole.ShieldHealer,
        JobId.Monk => PartyRole.MeleeDpsA,
        JobId.Dragoon or JobId.Ninja or JobId.Samurai or JobId.Reaper or JobId.Viper => PartyRole.MeleeDpsB,
        JobId.Bard or JobId.Machinist or JobId.Dancer => PartyRole.PhysRangedDps,
        _ => PartyRole.CasterDps,
    };

    public static readonly PartyMemberPreset[] Standard =
    [
        new("Warrior", ClassJob: (byte)JobId.Warrior,
            Head: 2899, Body: 3222, Hands: 3684, Legs: 3460, Feet: 3891, // Fighter's
            Look: new(PartyMemberLook.TribeSeekerOfTheSun, Height: 40, Face: 1, Hairstyle: 9, SkinColor: 8, EyeColor: 96, HairColor: 176, HighlightsColor: 104, Eyebrows: 1, EyeShape: 1, Nose: 1, Jaw: 1, Mouth: 1, TailShape: 3)),
        new("Paladin", ClassJob: (byte)JobId.Paladin,
            Head: 2897, Body: 3220, Hands: 3682, Legs: 3458, Feet: 3889, // Gallant
            Look: new(PartyMemberLook.TribeSeekerOfTheSun, Height: 55, Face: 3, Hairstyle: 153, SkinColor: 16, EyeColor: 152, HairColor: 16, HighlightsColor: 40, Eyebrows: 2, EyeShape: 2, Nose: 2, Jaw: 2, Mouth: 2, TailShape: 1)),
        new("White Mage", ClassJob: (byte)JobId.WhiteMage,
            Head: 2902, Body: 3225, Hands: 3687, Legs: 3463, Feet: 3894, // Healer's
            Look: new(PartyMemberLook.TribeSeekerOfTheSun, Height: 30, Face: 1, Hairstyle: 151, SkinColor: 0, EyeColor: 104, HairColor: 0, HighlightsColor: 96, Eyebrows: 1, EyeShape: 3, Nose: 1, Jaw: 1, Mouth: 1, TailShape: 3)),
        new("Scholar", ClassJob: (byte)JobId.Scholar,
            Head: 2905, Body: 3228, Hands: 3689, Legs: 3466, Feet: 3897, // Scholar's
            Look: new(PartyMemberLook.TribeKeeperOfTheMoon, Height: 45, Face: 4, Hairstyle: 3, SkinColor: 144, EyeColor: 120, HairColor: 177, HighlightsColor: 120, Eyebrows: 2, EyeShape: 1, Nose: 3, Jaw: 1, Mouth: 2, TailShape: 3)),
        new("Dragoon", ClassJob: (byte)JobId.Dragoon,
            Head: 2900, Body: 3223, Hands: 3685, Legs: 3461, Feet: 3892, // Drachen
            Look: new(PartyMemberLook.TribeSeekerOfTheSun, Height: 50, Face: 3, Hairstyle: 115, SkinColor: 8, EyeColor: 168, HairColor: 114, HighlightsColor: 16, Eyebrows: 3, EyeShape: 2, Nose: 2, Jaw: 2, Mouth: 1, TailShape: 2)),
        new("Monk", ClassJob: (byte)JobId.Monk,
            Head: 2898, Body: 3221, Hands: 3683, Legs: 3459, Feet: 3890, // Temple
            Look: new(PartyMemberLook.TribeSeekerOfTheSun, Height: 35, Face: 4, Hairstyle: 11, SkinColor: 24, EyeColor: 160, HairColor: 178, HighlightsColor: 168, Eyebrows: 1, EyeShape: 4, Nose: 1, Jaw: 3, Mouth: 3, TailShape: 3)),
        new("Bard", ClassJob: (byte)JobId.Bard,
            Head: 2901, Body: 3224, Hands: 3686, Legs: 3462, Feet: 3893, // Choral
            Look: new(PartyMemberLook.TribeSeekerOfTheSun, Height: 45, Face: 1, Hairstyle: 107, SkinColor: 16, EyeColor: 176, HairColor: 18, HighlightsColor: 168, Eyebrows: 2, EyeShape: 3, Nose: 4, Jaw: 1, Mouth: 2, TailShape: 1)),
        new("Black Mage", ClassJob: (byte)JobId.BlackMage,
            Head: 2903, Body: 3226, Hands: 3690, Legs: 3464, Feet: 3895, // Wizard's
            Look: new(PartyMemberLook.TribeKeeperOfTheMoon, Height: 25, Face: 3, Hairstyle: 146, SkinColor: 152, EyeColor: 136, HairColor: 185, HighlightsColor: 136, Eyebrows: 3, EyeShape: 1, Nose: 1, Jaw: 2, Mouth: 1, TailShape: 3)),
    ];
}

// A seat a real player holds over the network; both override the role preset's, and ClassJob 0
// leaves the preset's job.
public sealed record NetworkSeat(string Name, byte ClassJob)
{
    public PartyMemberPreset Dress(PartyMemberPreset preset) => preset with
    {
        Name = Name.Length > 0 ? Name : preset.Name,
        ClassJob = ClassJob != 0 ? ClassJob : preset.ClassJob,
    };
}

public sealed record PartyMemberPreset(
    string Name,
    byte ClassJob,
    uint Head,
    uint Body,
    uint Hands,
    uint Legs,
    uint Feet,
    PartyMemberLook Look);
