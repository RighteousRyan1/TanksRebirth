namespace TanksRebirth.Enums;

public enum MenuMode : byte {
    MainMenu,
    PauseMenu,
    IngameMenu,
    LevelEditorMenu
}
public enum MissionEndContext {
    Lose,
    Win,
    GameOver,
    CampaignCompleteMajor,
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
    Up,
    Down,
    Left,
    Right
}