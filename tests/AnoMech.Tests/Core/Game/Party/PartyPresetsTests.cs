using AnoMech.Core.Game.Party;

namespace AnoMech.Tests;

public class PartyPresetsTests
{
    private const byte FaceByte = 5, HairstyleByte = 6, EyebrowsByte = 14, EyeShapeByte = 16, NoseByte = 17, JawByte = 18, MouthByte = 19, TailByte = 22;
    private const int PaletteColors = 192;

    private static IEnumerable<PartyRole> Seats => Enum.GetValues<PartyRole>();

    private static PartyMemberLook Look(PartyRole seat) => PartyPresets.Standard[(int)seat].Look;

    private static IReadOnlyDictionary<byte, uint[]> Options(PartyMemberLook look)
        => DataminingGameData.CharaMakeOptions(PartyMemberLook.RaceMiqote, look.Tribe, PartyMemberLook.SexFemale);

    [Test]
    public void EverySeatHasAPreset() => Assert.That(PartyPresets.Standard, Has.Length.EqualTo(Seats.Count()));

    [TestCaseSource(nameof(Seats))]
    public void SeatIsAMiqoteTribe(PartyRole seat)
        => Assert.That(Look(seat).Tribe, Is.AnyOf(PartyMemberLook.TribeSeekerOfTheSun, PartyMemberLook.TribeKeeperOfTheMoon));

    [TestCaseSource(nameof(Seats))]
    public void SeatHairstyleIsOfferedToItsTribe(PartyRole seat)
    {
        var look = Look(seat);
        var offered = Options(look)[HairstyleByte].Select(DataminingGameData.CharaMakeFeatureId);
        Assert.That(offered, Does.Contain(look.Hairstyle));
    }

    [TestCaseSource(nameof(Seats))]
    public void SeatFeaturesAreWithinTheCharacterCreatorsOptions(PartyRole seat)
    {
        var look = Look(seat);
        var options = Options(look);
        Assert.Multiple(() =>
        {
            Assert.That(look.Face, Is.InRange(1, options[FaceByte].Length));
            Assert.That(look.TailShape, Is.InRange(1, options[TailByte].Length));
            Assert.That(look.Eyebrows, Is.LessThan(options[EyebrowsByte].Length));
            Assert.That(look.EyeShape, Is.LessThan(options[EyeShapeByte].Length));
            Assert.That(look.Nose, Is.LessThan(options[NoseByte].Length));
            Assert.That(look.Jaw, Is.LessThan(options[JawByte].Length));
            Assert.That(look.Mouth, Is.LessThan(options[MouthByte].Length));
        });
    }

    [TestCaseSource(nameof(Seats))]
    public void SeatColorsAndHeightAreInRange(PartyRole seat)
    {
        var look = Look(seat);
        Assert.Multiple(() =>
        {
            Assert.That(look.Height, Is.InRange(0, 100));
            Assert.That(new[] { look.SkinColor, look.EyeColor, look.HairColor, look.HighlightsColor }, Is.All.LessThan(PaletteColors));
        });
    }

    [Test]
    public void NoTwoSeatsShareAHairstyle()
        => Assert.That(Seats.Select(seat => Look(seat).Hairstyle), Is.Unique);

    [Test]
    public void ARemotePlayersSeatKeepsItsLook()
    {
        var preset = PartyPresets.Standard[(int)PartyRole.MainTank];
        Assert.That(new NetworkSeat("Remote Player", 21).Dress(preset).Look, Is.EqualTo(preset.Look));
    }
}
