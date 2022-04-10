namespace TanksRebirth.Enums
{
    /// <summary>Serves the purpose of parsing tiers into strings. Note that only tiers less than 27 will be parsed.</summary>
    public enum TankTier : byte
    {
        None,
        Random,
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
        Citrine,
        Ruby,
        Amethyst,
        Emerald,
        Gold,
        Obsidian,

        // here separates the master mod tanks from the advanced mod tanks

        Granite,
        Bubblegum,
        Water,
        Tiger,
        Crimson,
        Fade,
        Creeper,
        Gamma,
        Marble,

        Explosive,
        Electro,
        RocketDefender,
        Assassin,
        Commando
    }

    public enum PlayerType : byte
    {
        Blue,
        Red
    }

    public enum ShellTier : byte
    {
        Player,
        Standard,
        Rocket,
        RicochetRocket,
        Supressed,
        Explosive
    }

    public enum MenuMode : byte
    {
        MainMenu,
        PauseMenu,
        IngameMenu,
        LevelEditorMenu
    }
}