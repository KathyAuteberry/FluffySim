using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AnoMech.Core.EnemyActions;
using AnoMech.Core.Game;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.SimObjects;
using AnoMech.Scenarios.Fru.Upstream.P5FulgentBlade;
using AnoMech.Scenarios.Fru.Upstream.P5ParadiseRegained;
using Dalamud.Bindings.ImGui;
using static AnoMech.Core.EnemyActions.EnemyActionEffects;
using Constants = AnoMech.Scenarios.Fru.Upstream.FruConstants;

namespace AnoMech.Scenarios.Fru.Pandora;

public sealed class FruPandoraSequenceScenario : IScenario
{
    public bool SupportsMultiplayer => true;
    public string Name => "Pandora sequence";
    public IPhase Phase => FruZone.P5;
    public IReadOnlyList<IScenarioAi> AiStrats => [new FruPandoraSequenceAi()];
    public object SettingsOverrides => Overrides;
    public FruPandoraSequenceOverrides Overrides { get; } = new();
    public const float EnrageCastAt = 246.6f;
    public const float EnrageHitAt = 268.1f;

    private SimWorld world = null!;
    private SimEnemy? pandora;
    private IScenario? stage;
    private float stageStarted;
    private HashSet<ISimObject> retained = [];

    private static readonly EnemyAction Enrage = new(40328)
    {
        Effects = [Resolve(ctx =>
        {
            foreach (var member in ctx.Hits) ctx.Kill(member, "Paradise Lost enrage");
        })],
        Timing = new() { DamageDelay = 0.3f },
    };

    public void DrawSettings()
    {
        var invulnerability = Overrides.RequireInvulnerability;
        if (ImGui.Checkbox("Require invulnerability (tank practice)", ref invulnerability)) Overrides.RequireInvulnerability = invulnerability;
        var resolve = Overrides.ResolveTerminalEnrage;
        if (ImGui.Checkbox("Resolve terminal enrage", ref resolve)) Overrides.ResolveTerminalEnrage = resolve;
        ImGui.TextWrapped("Practice ends before the lethal strike unless enabled. Boss HP and the final DPS race are not simulated.");
    }

    public void Run(SimWorld worldParam, int? selectedAi)
    {
        world = worldParam;
        stage = null;
        retained.Clear();
        world.Events.Add(0f, () =>
        {
            pandora = world.SpawnEnemy(new EnemySpawnConfig(
                BNpcBaseId: Constants.BNpcBaseId.Pandora, NameId: Constants.BNpcNameId.Pandora,
                Level: 100, Targetable: true, EnemyList: EnemyListMode.Always,
                Placement: new Placement(Vector3.Zero, MathF.PI), LayoutId: Constants.LayoutId.PandoraP5));
            Begin(new FruP5FulgentBladeScenario { SharedPandora = pandora }, selectedAi);
        });
        world.Events.Add(37.35f, () => Begin(Paradise(), selectedAi));
        world.Events.Add(95.45f, () => Begin(new FruPandorasBoxScenario { SharedPandora = pandora }, selectedAi));
        world.Events.Add(113.15f, () => Begin(new FruP5FulgentBladeScenario { SharedPandora = pandora }, selectedAi));
        world.Events.Add(154.7f, () => Begin(Paradise(-5.35f), selectedAi));
        world.Events.Add(203.75f, () => Begin(new FruP5FulgentBladeScenario { SharedPandora = pandora }, selectedAi));
        world.Events.Add(EnrageCastAt, BeginEnrage);
    }

    public void Tick(float delta, float elapsed) => stage?.Tick(delta, elapsed - stageStarted);

    private FruP5ParadiseRegainedScenario Paradise(float offset = 0f)
    {
        var scenario = new FruP5ParadiseRegainedScenario { SharedPandora = pandora, PolarizingOffset = offset };
        ((FruP5ParadiseRegainedStateOverrides)scenario.SettingsOverrides).RequireInvulnerability = Overrides.RequireInvulnerability;
        return scenario;
    }

    private void Begin(IScenario next, int? selectedAi)
    {
        ClearStage();
        pandora?.KeepFacing(null);
        pandora?.SetPosition(new Placement(Vector3.Zero, MathF.PI));
        pandora?.SetModelState(0);
        retained = world.Children.ToHashSet();
        stage = next;
        stageStarted = world.Events.Elapsed;
        next.Run(world, selectedAi);
    }

    private void ClearStage()
    {
        if (stage is null) return;
        var children = world.Children.Where(child => !retained.Contains(child) && !ReferenceEquals(child, pandora)).ToArray();
        void Despawn()
        {
            foreach (var child in children) child.Despawn();
        }
        // Fulgent's remaining strips keep rolling after the first Paradise cast begins.
        var remaining = stage is FruP5FulgentBladeScenario
            ? stageStarted + FruP5FulgentBladeScenario.ExalinesCompleteAt - world.Events.Elapsed
            : 0f;
        if (remaining > 0f) world.Events.Add(remaining, Despawn);
        else Despawn();
        stage = null;
    }

    private void BeginEnrage()
    {
        ClearStage();
        pandora?.KeepFacing(null);
        pandora?.Cast(40327, animationLock: 9.5f);
        if (!Overrides.ResolveTerminalEnrage) return;
        var helper = world.SpawnEnemy(new EnemySpawnConfig(
            BNpcBaseId: Constants.BNpcBaseId.Helper, NameId: Constants.BNpcNameId.Pandora,
            Level: 1, EnemyList: EnemyListMode.Never, Visibility: SpawnVisibility.InvisibleHelper,
            Placement: new Placement(Vector3.Zero, 0f)));
        helper?.Cast(Enrage);
    }
}

public sealed class FruPandoraSequenceOverrides
{
    public bool ResolveTerminalEnrage { get; set; }
    public bool RequireInvulnerability { get; set; }
}
