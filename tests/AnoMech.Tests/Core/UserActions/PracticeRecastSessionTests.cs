#pragma warning disable CA1416
using AnoMech.Core.UserActions;

namespace AnoMech.Tests;

public class PracticeRecastSessionTests
{
    private sealed class Store : IPracticeRecastStore
    {
        public readonly Dictionary<int, LocalRecast> Values = new();
        public readonly List<int> Writes = new();
        public readonly Dictionary<uint,int> Groups = new();
        public int Group(uint action) => Groups.GetValueOrDefault(action,(int)action);
        public LocalRecast? Read(int group) => Values.TryGetValue(group, out var value) ? value : null;
        public void Write(int group, LocalRecast value) { Values[group] = value; Writes.Add(group); }
    }

    [TestCase(36920u)] [TestCase(36923u)] [TestCase(36927u)] [TestCase(36935u)]
    [TestCase(3540u)] [TestCase(7388u)] [TestCase(16471u)] [TestCase(16160u)]
    [TestCase(7533u)] [TestCase(3u)] [TestCase(30u)] [TestCase(43u)] [TestCase(3638u)] [TestCase(16152u)] [TestCase(7548u)] [TestCase(7559u)]
    public void ResetIsLocalAndRestoresPreexistingCooldownWithWallTimeElapsed(uint action)
    {
        var store = new Store();
        store.Values[(int)action] = new(true, action, 20, 120);
        store.Values[999] = new(true, 999, 2, 20);
        double now = 0;
        var session = new PracticeRecastSession(store, () => true, () => now);
        session.Capture();
        Assert.That(session.Reset(action), Is.True);
        Assert.That(store.Values[(int)action].Active, Is.False);
        // The native client starts a fresh local timer when the hotbar is pressed.
        store.Values[(int)action] = new(true, action, 0, 120);
        now = 10;
        Assert.That(session.Reset(action), Is.True);
        now = 15;
        session.Restore();
        Assert.That(store.Values[(int)action], Is.EqualTo(new LocalRecast(true, action, 35, 120)));
        Assert.That(store.Values[999], Is.EqualTo(new LocalRecast(true, 999, 2, 20)));
        Assert.That(store.Writes, Has.All.EqualTo((int)action));
        var writes = store.Writes.Count;
        session.Restore();
        Assert.That(store.Writes, Has.Count.EqualTo(writes));
        Assert.That(session.Reset(action), Is.False);
    }

    [Test]
    public void LateAvailableChargedGroupIsCapturedBeforeResetAndRestoredWithItsOwnElapsedTime()
    {
        var store = new Store();
        double now = 0;
        var session = new PracticeRecastSession(store, () => true, () => now);
        session.Capture();
        Assert.That(session.Reset(25754), Is.False);
        now = 10;
        store.Values[25754] = new(true, 25754, 5, 120);
        Assert.That(session.Reset(25754), Is.True);
        Assert.That(store.Values[25754], Is.EqualTo(new LocalRecast(false, 25754, 120, 120)));
        now = 20;
        session.Restore();
        Assert.That(store.Values[25754], Is.EqualTo(new LocalRecast(true, 25754, 15, 120)));
    }

    [TestCase(19u, 30u)] [TestCase(21u, 43u)] [TestCase(32u, 3638u)] [TestCase(37u, 16152u)]
    [TestCase(24u, 7559u)] [TestCase(23u, 7551u)]
    public void RegisteredPracticeActionsRestoreOnRetryButNotBetweenSegments(uint job, uint expected)
    {
        var store = new Store();
        store.Values[(int)expected] = new(true, expected, 5, 120);
        var session = new PracticeRecastSession(store, () => true, () => 0);
        session.Capture();
        var actions = PracticeDefensiveActions.RecastsForJob(job);
        Assert.That(actions, Does.Contain(expected));
        foreach (var action in actions) session.Reset(action);
        store.Values[(int)expected] = new(true, expected, 0, 120);
        session.Track(expected);
        Assert.That(store.Values[(int)expected].Active, Is.True);
        foreach (var action in actions) session.Reset(action);
        Assert.That(store.Values[(int)expected].Active, Is.False);
        session.Restore();
        Assert.That(store.Values[(int)expected], Is.EqualTo(new LocalRecast(true, expected, 5, 120)));
    }

    [Test]
    public void UnprotectedOrUncapturedSessionsNeverWriteAndCannotRestore()
    {
        var store = new Store();
        store.Values[43] = new(true, 43, 0, 240);
        bool isolated = false;
        var session = new PracticeRecastSession(store, () => isolated, () => 0);
        session.Capture();
        Assert.That(session.Reset(43), Is.False);
        Assert.That(store.Writes, Is.Empty);
        isolated = true;
        session.Capture(); session.Reset(43);
        isolated = false;
        Assert.That(session.Reset(43), Is.False);
        Assert.Throws<InvalidOperationException>(session.Restore);
        isolated = true;
        session.Restore();
        Assert.That(store.Values[43].Active, Is.True);
    }

    [Test]
    public void ActionUseWithoutAnInitialResetIsStillRestoredAndExpiredOriginalCooldownStaysExpired()
    {
        var store = new Store();
        store.Values[43] = new(true, 43, 230, 240);
        double now = 0;
        var session = new PracticeRecastSession(store, () => true, () => now);
        session.Capture(); session.Track(43);
        store.Values[43] = new(true, 43, 0, 240);
        now = 30; session.Restore();
        Assert.That(store.Values[43], Is.EqualTo(new LocalRecast(false,43,240,240)));
    }

    [Test]
    public void UnrelatedActionsAndForeignSharedGroupOwnersCannotBeReset()
    {
        var store = new Store();
        store.Values[43] = new(true, 999, 5, 120);
        store.Values[999] = new(true, 999, 3, 120);
        var session = new PracticeRecastSession(store, () => true, () => 0);
        session.Capture();
        Assert.That(session.Reset(999), Is.False);
        Assert.That(session.Reset(43), Is.False);
        Assert.That(session.Reset(30), Is.False);
        Assert.That(store.Writes, Is.Empty);
    }

    [Test]
    public void SharedTankAndUtilityGroupsRestoreOnceWithTheOriginalActionIdentity()
    {
        var store = new Store();
        foreach (var action in new uint[] {30,43,3638,16152}) store.Groups[action] = 24;
        store.Groups[7548] = store.Groups[7559] = 48;
        store.Values[24] = new(true,43,20,240);
        store.Values[48] = new(true,7548,30,120);
        var session = new PracticeRecastSession(store, () => true, () => 0);
        session.Capture(); session.Reset(43); session.Reset(7548); session.Reset(7559);
        store.Writes.Clear(); session.Restore();
        Assert.That(store.Writes, Is.EquivalentTo(new[] {24,48}));
        Assert.That(store.Values[24], Is.EqualTo(new LocalRecast(true,43,20,240)));
        Assert.That(store.Values[48], Is.EqualTo(new LocalRecast(true,7548,30,120)));
    }

    [Test]
    public void RepeatedPauseRestoresOwnedRecastsWithoutRefreshingThemOrFreezingTheRealBaseline()
    {
        var store = new Store(); store.Values[43] = new(true,43,20,240); store.Values[7548] = new(true,7548,5,120);
        double now = 0;
        var session = new PracticeRecastSession(store, () => true, () => now);
        session.Capture(); session.Reset(43);
        store.Values[43] = new(true,43,7,240);
        for (var cycle = 0; cycle < 3; cycle++)
        {
            session.SetPaused(true); session.SetPaused(true);
            now += 60;
            store.Values[43] = new(true,43,67,240);
            session.SetPaused(false); session.SetPaused(false);
            Assert.That(store.Values[43], Is.EqualTo(new LocalRecast(true,43,7,240)));
        }
        Assert.That(store.Writes.Contains(7548), Is.False);
        session.Restore();
        Assert.That(store.Values[43], Is.EqualTo(new LocalRecast(true,43,200,240)));
    }

    [Test]
    public void LeaveWhilePausedRestoresOriginalStateRatherThanThePausedPracticeTimer()
    {
        var store = new Store(); store.Values[43] = new(false,43,240,240);
        double now = 0;
        var session = new PracticeRecastSession(store, () => true, () => now);
        session.Capture(); session.Reset(43); session.SetPaused(true);
        now = 500; session.Restore(); session.SetPaused(false);
        Assert.That(store.Values[43], Is.EqualTo(new LocalRecast(false,43,240,240)));
    }

    [Test]
    public void EachNewSessionGetsANewBaselineRatherThanReusingAnOldSnapshot()
    {
        var store = new Store(); store.Values[43] = new(false, 43, 0, 240);
        var session = new PracticeRecastSession(store, () => true, () => 0);
        session.Capture(); session.Reset(43); session.Restore();
        store.Values[43] = new(true,43,100,240);
        session.Capture(); session.Reset(43); session.Restore();
        Assert.That(store.Values[43].Elapsed, Is.EqualTo(100));
    }
}
