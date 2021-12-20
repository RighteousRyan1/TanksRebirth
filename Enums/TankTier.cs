namespace WiiPlayTanksRemake.Enums
{
    public enum TankTier : byte
    {
        None,
        Brown,
        Ash,
        Marine,
        Yellow,
        Pink,
        Purple,
        Green,
        White,
        Black,

        // here separates the vanilla tanks from the master mod tanks

        Bronze,
        Silver,
        Sapphire,
        Ruby,
        Citrine,
        Amethyst,
        Emerald,
        Gold,
        Obsidian,

        // here separates the master mod tanks from the advanced mod tanks

        Granite,
        Bubblegum,
        Water,
        Crimson,
        Tiger,
        Creeper,
        Fade,
        Gamma,
        Marble
    }

    public enum PlayerType : byte
    {
        Blue,
        Red
    }

    public enum ShellTier : byte
    {
        Regular,
        Rocket,
        RicochetRocket
    }

    public enum MenuMode : byte
    {
        MainMenu,
        PauseMenu,
        IngameMenu,
        LevelEditorMenu
    }
}