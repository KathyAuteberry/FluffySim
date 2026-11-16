using System.Globalization;
using System.Reflection;

namespace AnoMech.Tests;

public class ConstantIdsExistInSheetsTests
{
    private static readonly string[] SheetClasses = ["ActionId", "StatusId", "BNpcNameId"];

    private static readonly Dictionary<string, string> NotSheetRows = new()
    {
        ["Dsr.DsrP7Constants.StatusId.FlamesGlow"] = "param of status 0x808",
        ["Dsr.DsrP7Constants.StatusId.IceGlow"] = "param of status 0x808",
    };

    private static readonly Lazy<Dictionary<string, HashSet<uint>>> Sheets = new(() => new()
    {
        ["ActionId"] = RowIds("Action"),
        ["StatusId"] = RowIds("Status"),
        ["BNpcNameId"] = RowIds("BNpcName"),
    });

    private static HashSet<uint> RowIds(string sheet)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "datamining", sheet + ".csv");
        var ids = new HashSet<uint>();
        foreach (var line in File.ReadLines(path).Skip(1))
        {
            var comma = line.IndexOf(',');
            if (comma > 0 && uint.TryParse(line.AsSpan(0, comma), NumberStyles.None, CultureInfo.InvariantCulture, out var id))
                ids.Add(id);
        }
        return ids;
    }

    private static string DisplayName(FieldInfo field)
    {
        var holder = field.DeclaringType!;
        return $"{holder.DeclaringType!.FullName!["AnoMech.Scenarios.".Length..]}.{holder.Name}.{field.Name}";
    }

    public static IEnumerable<TestCaseData> Constants()
    {
        var holders = typeof(AnoMech.Plugin).Assembly.GetTypes()
            .Where(t => t.IsNestedPublic || t.IsNestedAssembly)
            .Where(t => t.Namespace?.StartsWith("AnoMech.Scenarios") == true && SheetClasses.Contains(t.Name));
        foreach (var holder in holders.OrderBy(t => t.FullName, StringComparer.Ordinal))
            foreach (var field in holder.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                         .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType != typeof(string)))
            {
                var value = Convert.ToInt64(field.GetRawConstantValue(), CultureInfo.InvariantCulture);
                yield return new TestCaseData(holder.Name, DisplayName(field), value).SetArgDisplayNames(DisplayName(field));
            }
    }

    [TestCaseSource(nameof(Constants))]
    public void ConstantIsARowOfItsSheet(string holder, string name, long value)
    {
        if (holder == "StatusId" && value <= ushort.MaxValue && AnoMech.Core.SimObjects.SimOnlyStatus.Is((ushort)value))
            Assert.Pass($"{name} is a sim-only status, deliberately past the Status sheet.");

        if (NotSheetRows.TryGetValue(name, out var reallyIs))
            Assert.Pass($"{name} is a {reallyIs}, not a row of the {holder} sheet.");

        Assert.That(value, Is.InRange(0, uint.MaxValue));
        Assert.That(RowExists(holder, (uint)value), Is.True, $"{name} = {value} (0x{value:X}) is not a row in the sheet for {holder}.");
    }

    [Test]
    public void AllowlistEntriesStillExistAsConstants()
    {
        var names = Constants().Select(c => (string)c.Arguments[1]!).ToHashSet();
        Assert.That(NotSheetRows.Keys.Where(k => !names.Contains(k)), Is.Empty, "stale allowlist entries");
    }

    [Test]
    public void LookupRejectsMissingRows()
    {
        Assert.That(RowExists("ActionId", 0x63C6), Is.True);
        Assert.That(RowExists("ActionId", uint.MaxValue - 1), Is.False);
        Assert.That(RowExists("StatusId", uint.MaxValue - 1), Is.False);
        Assert.That(RowExists("BNpcNameId", uint.MaxValue - 1), Is.False);
    }

    private static bool RowExists(string holder, uint id) => Sheets.Value[holder].Contains(id);
}
