using AnoMech.Core.Game;

namespace AnoMech.Windows;

public readonly record struct ScenarioPauseControls(bool Visible, bool Enabled, string Label)
{
    public static ScenarioPauseControls Evaluate(Game game, bool inSession)
        => new(game.CanPause || game.Paused, !inSession && (game.CanPause || game.Paused), game.Paused ? "Resume" : "Pause");

    public static bool Press(Game game, bool inSession)
    {
        if (!Evaluate(game, inSession).Enabled) return false;
        game.Paused = !game.Paused;
        return true;
    }
}
