namespace TanksRebirth.Enums;

public enum MenuMode : byte {
    MainMenu,
    PauseMenu,
    IngameMenu,
    LevelEditorMenu
}
public enum MissionEndContext {
    /// <summary>The player has lost, but the game is not over.</summary>
    Lose,
    /// <summary>The player has won, but the game is not over.</summary>
    Win,
    /// <summary>The player has lost, and the game is over.</summary>
    GameOver,
    /// <summary>The player has won, and the game is over, with a big celebration.</summary>
    CampaignCompleteMajor,
    /// <summary>The player has won, and the game is over, with a small celebration.</summary>
    CampaignCompleteMinor
}
public enum Grade {
    APlus = 0,
    A = 1,
    AMinus = 2,

    BPlus = 3,
    B = 4,
    BMinus = 5,

    CPlus = 6,
    C = 7,
    CMinus = 8,

    DPlus = 9,
    D = 10,
    DMinus = 11,

    FPlus = 12,
    F = 13,
    FMinus = 14
}
public enum CollisionDirection {
    None,
    Down,
    Left,
    Up,
    Right
}