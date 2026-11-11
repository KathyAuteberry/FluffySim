using System.Collections.Generic;
using System.Numerics;
using AnoMech.Core;
using AnoMech.Core.Game;
using AnoMech.Core.SimObjects;
using static AnoMech.Scenarios.Dmu.DmuConstants;

namespace AnoMech.Scenarios.Dmu;

// The P1 actors every Graven Image scenario stands up: Kefka, the background statues, the nine
// invisible Graven Images that act for them, and the invisible helpers that cast the rest.
public sealed class DmuArena
{
    private const int HelperCount = 16;
    private const int GravenImageCount = 9;

    public SimEnemy? Kefka { get; private set; }
    public IReadOnlyList<SimEnemy> GravenImages => gravenImages;

    private readonly SimWorld world;
    private readonly Dictionary<Statue, SimEventObject> statues = [];
    private readonly List<SimEnemy> gravenImages = [];
    private readonly List<SimEnemy> helpers = [];
    private int nextHelper;

    public DmuArena(SimWorld world) => this.world = world;

    // `kefkaAt` is where the fight has left him; he faces north, to his tank.
    public void Spawn(Vector3 kefkaAt, IReadOnlyDictionary<Statue, (uint State, uint Mask)>? statueBeats = null)
    {
        Kefka = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: BNpcBaseId.Kefka, NameId: BNpcNameId.Kefka, Level: Level,
            Targetable: true, EnemyList: EnemyListMode.Always, Visibility: SpawnVisibility.Visible,
            Placement: new Placement(kefkaAt, float.Pi), LayoutId: DmuConstants.LayoutId.Kefka));
        for (var i = 0; i < GravenImageCount; i++)
            if (world.SpawnEnemy(new EnemySpawnConfig(
                    BNpcBaseId: BNpcBaseId.GravenImage, NameId: BNpcNameId.GravenImage, Level: Level,
                    Targetable: false, EnemyList: EnemyListMode.Never, Visibility: SpawnVisibility.InvisibleHelper,
                    Placement: new Placement(Vector3.Zero, 0f), LayoutId: DmuConstants.LayoutId.GravenImage)) is { } image)
                gravenImages.Add(image);
        for (var i = 0; i < HelperCount; i++)
            if (world.SpawnEnemy(new EnemySpawnConfig(
                    BNpcBaseId: BNpcBaseId.Helper, NameId: BNpcNameId.Kefka, Level: 1,
                    Targetable: false, EnemyList: EnemyListMode.Never, Visibility: SpawnVisibility.InvisibleHelper,
                    Placement: new Placement(Vector3.Zero, 0f))) is { } helper)
                helpers.Add(helper);
        foreach (var statue in Statues.All)
            if (world.SpawnEventObject(new EventObjectSpawnConfig
                {
                    EObjId = statue.EObjId,
                    LayoutId = statue.LayoutId,
                    EventId = EventId.StatueDirector,
                    TargetableStatus = 5,
                    Arg2 = statue.Arg2,
                    Placement = new Placement(statue.Position, 0f),
                    TimelineState = StatueRestState,
                }) is { } prop)
                statues[statue] = prop;
        if (statueBeats is not null)
            foreach (var (statue, beat) in statueBeats) Beat(statue, beat);
    }

    public void Beat(Statue statue, (uint State, uint Mask) beat)
    {
        if (statues.TryGetValue(statue, out var prop)) prop.PlayBeat(beat.State, beat.Mask);
        else DiagnosticLog.Warn($"[DmuArena] Statue 0x{statue.LayoutId:X} is not spawned; beat 0x{beat.State:X} dropped.");
    }

    // An invisible helper placed and turned for its cast. Sixteen in rotation keep any one free
    // for longer than every P1 cast that overlaps it.
    public SimEnemy? Helper(Vector3 at, float rotation)
    {
        if (helpers.Count == 0) return null;
        var helper = helpers[nextHelper++ % helpers.Count];
        helper.SetPosition(new Placement(at, rotation));
        return helper;
    }

    // Graven Image `index` stood on its statue, as retail places them before they act.
    public SimEnemy? GravenImageAt(int index, Statue statue)
    {
        if (index >= gravenImages.Count) return null;
        var image = gravenImages[index];
        image.SetPosition(new Placement(statue.Position, 0f));
        return image;
    }

    public IReadOnlyList<SimEventObject> Puddles => puddles;
    private readonly List<SimEventObject> puddles = [];

    public SimEventObject? DropPuddle(Vector3 at)
    {
        var puddle = world.SpawnEventObject(new EventObjectSpawnConfig
        {
            EObjId = EObjId.GravitasPuddle,
            EventId = EventId.StatueDirector,
            TargetableStatus = 5,
            Arg2 = GravitasPuddleArg2Base + ((uint)puddles.Count << 16),
            Placement = new Placement(at with { Y = 0f }, 0f),
            TimelineState = 1,
        });
        if (puddle is not null) puddles.Add(puddle);
        return puddle;
    }

    public void ClearPuddles()
    {
        foreach (var puddle in puddles) puddle.Despawn();
        puddles.Clear();
    }

    public void TeleportKefkaToCentre()
    {
        if (Kefka is not { } kefka) return;
        kefka.Cast(ActionId.Teleport, animationLock: 0.6f);
        kefka.SetPosition(new Placement(Vector3.Zero, kefka.Rotation));
    }
}
