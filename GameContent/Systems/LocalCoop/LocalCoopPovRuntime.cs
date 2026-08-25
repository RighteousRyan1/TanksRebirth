using TanksRebirth.GameContent.Systems.TankSystem;
using TanksRebirth.GameContent.UI.LevelEditor;
using TanksRebirth.GameContent.UI.MainMenu;
using TanksRebirth.Net;

namespace TanksRebirth.GameContent.Systems.LocalCoop;

public static class LocalCoopPovRuntime {
    public static bool IsActive => LocalCoopPovPolicy.ShouldUseSplitScreen(
        LocalGameSession.Current.IsLocalCoop,
        Difficulties.Types["POV"],
        MainMenuUI.IsActive,
        LevelEditorUI.IsActive,
        Client.IsConnected());

    public static PlayerTank? ResolveCameraTank(int requestedPlayerId) {
        var playerOne = GameHandler.AllPlayerTanks[0];
        var playerTwo = GameHandler.AllPlayerTanks[1];
        var resolvedPlayerId = LocalCoopPovPolicy.ResolveCameraPlayerId(
            requestedPlayerId,
            playerOne is not null,
            playerOne?.IsDestroyed ?? false,
            playerTwo is not null,
            playerTwo?.IsDestroyed ?? false);
        return resolvedPlayerId switch {
            0 => playerOne,
            1 => playerTwo,
            _ => null
        };
    }

    public static bool IsPlayerDown(int playerId) {
        if (playerId is < 0 or > 1)
            return true;
        var tank = GameHandler.AllPlayerTanks[playerId];
        return tank is null || tank.IsDestroyed;
    }
}
