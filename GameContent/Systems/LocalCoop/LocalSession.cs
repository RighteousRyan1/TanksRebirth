namespace TanksRebirth.GameContent.Systems.LocalCoop;

public enum LocalPlayMode {
    SinglePlayer,
    LocalCoop
}

public sealed class LocalSession {
    public LocalPlayMode Mode { get; private set; } = LocalPlayMode.SinglePlayer;
    public int PlayerCount => Mode == LocalPlayMode.LocalCoop ? 2 : 1;
    public bool IsLocalCoop => Mode == LocalPlayMode.LocalCoop;

    public void StartSinglePlayer() => Mode = LocalPlayMode.SinglePlayer;
    public void StartLocalCoop() => Mode = LocalPlayMode.LocalCoop;
    public bool IsActivePlayer(int playerId) => playerId >= 0 && playerId < PlayerCount;
}

public static class LocalGameSession {
    public static LocalSession Current { get; } = new();
}
