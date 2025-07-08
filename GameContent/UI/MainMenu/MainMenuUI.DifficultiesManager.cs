using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.ID;
using TanksRebirth.GameContent.RebirthUtils;
using TanksRebirth.GameContent.Systems;
using TanksRebirth.Internals.Common;
using TanksRebirth.Internals.Common.GameUI;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Net;

namespace TanksRebirth.GameContent.UI.MainMenu;

#pragma warning disable
public static partial class MainMenuUI {
    private static bool _diffButtonsInitialized;
    public static UITextButton TanksAreCalculators; // make them calculate shots abnormally
    public static UITextButton PieFactory;
    public static UITextButton UltraMines;
    public static UITextButton BulletHell;
    public static UITextButton AllInvisible;
    public static UITextButton AllStationary;
    public static UITextButton Armored;
    public static UITextButton AllHoming;
    public static UITextButton BumpUp;
    public static UITextButton Monochrome;
    public static UITextButton InfiniteLives;

    public static UITextButton MasterMode;
    public static UITextButton TacticalPlanes;
    public static UITextButton MachineGuns;
    public static UITextButton RandomizedTanks;
    public static UITextButton ThunderMode;
    public static UITextButton POVMode;
    public static UITextButton AiCompanion;
    public static UITextButton Shotguns;
    public static UITextButton Predictions;

    public static UITextButton RandomizedPlayer;
    public static UITextButton BulletBlocking;

    public static UITextButton FFA;

    public static UITextButton LanternMode;

    public static UITextButton DisguiseMode;

    // TODO: UI Layers. This is fucking ugly.
    internal static void SetDifficultiesButtonsVisibility(bool visible) {
        TanksAreCalculators.IsVisible = visible;
        PieFactory.IsVisible = visible;
        UltraMines.IsVisible = visible;
        BulletHell.IsVisible = visible;
        AllInvisible.IsVisible = visible;
        AllStationary.IsVisible = visible;
        Armored.IsVisible = visible;
        AllHoming.IsVisible = visible;
        BumpUp.IsVisible = visible;
        Monochrome.IsVisible = visible;
        InfiniteLives.IsVisible = visible;
        MasterMode.IsVisible = visible;
        TacticalPlanes.IsVisible = visible;
        MachineGuns.IsVisible = visible;
        RandomizedTanks.IsVisible = visible;
        ThunderMode.IsVisible = visible;
        POVMode.IsVisible = visible;
        AiCompanion.IsVisible = visible;
        Shotguns.IsVisible = visible;
        Predictions.IsVisible = visible;
        RandomizedPlayer.IsVisible = visible;
        BulletBlocking.IsVisible = visible;
        FFA.IsVisible = visible;
        LanternMode.IsVisible = visible;
        DisguiseMode.IsVisible = visible;
    }

    public static void UpdateDifficulties() {
        DisguiseMode.Text = "Disguise: " + TankID.Collection.GetKey(Difficulties.DisguiseValue);
        Monochrome.Text = "Monochrome: " + TankID.Collection.GetKey(Difficulties.MonochromeValue);
        RandomizedTanks.Text = $"Randomized Tanks\nLower: {TankID.Collection.GetKey(Difficulties.RandomTanksLower)} | Upper: {TankID.Collection.GetKey(Difficulties.RandomTanksUpper)}";
        if (MenuState == UIState.Mulitplayer) {
            if (DebugManager.DebuggingEnabled) {
                if (InputUtils.AreKeysJustPressed(Keys.Q, Keys.W)) {
                    IPInput.Text = "localhost";
                    PortInput.Text = "7777";
                    ServerNameInput.Text = "TestServer";
                    UsernameInput.Text = Client.ClientRandom.Next(0, ushort.MaxValue).ToString();
                }
            }
        }

        // me in march 2024: what the fuck is this code.
        TanksAreCalculators.Color = Difficulties.Types["TanksAreCalculators"] ? Color.Lime : Color.Red;
        PieFactory.Color = Difficulties.Types["PieFactory"] ? Color.Lime : Color.Red;
        UltraMines.Color = Difficulties.Types["UltraMines"] ? Color.Lime : Color.Red;
        BulletHell.Color = Difficulties.Types["BulletHell"] ? Color.Lime : Color.Red;
        AllInvisible.Color = Difficulties.Types["AllInvisible"] ? Color.Lime : Color.Red;
        AllStationary.Color = Difficulties.Types["AllStationary"] ? Color.Lime : Color.Red;
        AllHoming.Color = Difficulties.Types["AllHoming"] ? Color.Lime : Color.Red;
        Armored.Color = Difficulties.Types["Armored"] ? Color.Lime : Color.Red;
        BumpUp.Color = Difficulties.Types["BumpUp"] ? Color.Lime : Color.Red;
        Monochrome.Color = Difficulties.Types["Monochrome"] ? Color.Lime : Color.Red;
        InfiniteLives.Color = Difficulties.Types["InfiniteLives"] ? Color.Lime : Color.Red;
        MasterMode.Color = Difficulties.Types["MasterModBuff"] ? Color.Lime : Color.Red;
        TacticalPlanes.Color = Difficulties.Types["TacticalPlanes"] ? Color.Lime : Color.Red;
        MachineGuns.Color = Difficulties.Types["MachineGuns"] ? Color.Lime : Color.Red;
        RandomizedTanks.Color = Difficulties.Types["RandomizedTanks"] ? Color.Lime : Color.Red;
        ThunderMode.Color = Difficulties.Types["ThunderMode"] ? Color.Lime : Color.Red;
        POVMode.Color = Difficulties.Types["POV"] ? Color.Lime : Color.Red;
        AiCompanion.Color = Difficulties.Types["AiCompanion"] ? Color.Lime : Color.Red;
        Shotguns.Color = Difficulties.Types["Shotguns"] ? Color.Lime : Color.Red;
        Predictions.Color = Difficulties.Types["Predictions"] ? Color.Lime : Color.Red;
        RandomizedPlayer.Color = Difficulties.Types["RandomPlayer"] ? Color.Lime : Color.Red;
        BulletBlocking.Color = Difficulties.Types["BulletBlocking"] ? Color.Lime : Color.Red;
        FFA.Color = Difficulties.Types["FFA"] ? Color.Lime : Color.Red;
        LanternMode.Color = Difficulties.Types["LanternMode"] ? Color.Lime : Color.Red;
        DisguiseMode.Color = Difficulties.Types["Disguise"] ? Color.Lime : Color.Red;

        if (Active && Client.IsConnected() && Client.IsHost())
            Client.SendDiffiulties();
    }
    public static void RenderDifficultiesMenu() {
        if (MenuState == UIState.Difficulties) {
            DrawUtils.DrawTextWithBorder(TankGame.SpriteRenderer, FontGlobals.RebirthFont,
                "Ideas are welcome! Let us know in our DISCORD server!",
                new Vector2(WindowUtils.WindowWidth / 2, WindowUtils.WindowHeight / 6), Color.White, Color.Black, new Vector2(1f), 0f, Anchor.Center, 0.8f);
        }
    }
    private static void InitializeDifficultyButtons() {
        _diffButtonsInitialized = true;
        SpriteFontBase font = FontGlobals.RebirthFont;
        TanksAreCalculators = new("Tanks are Calculators", font, Color.White) {
            IsVisible = false,
            Tooltip = "ALL tanks will begin to look for angles" +
            "\non you (and other enemies) outside of their immediate aim." +
            "\nDo note that this uses significantly more CPU power.",
            OnLeftClick = (elem) => Difficulties.Types["TanksAreCalculators"] = !Difficulties.Types["TanksAreCalculators"]
        };
        TanksAreCalculators.SetDimensions(100, 300, 300, 40);

        PieFactory = new("Lemon Pie Factory", font, Color.White) {
            IsVisible = false,
            Tooltip = "Makes yellow tanks absurdly more dangerous by" +
            "\nturning them into mine-laying machines." +
            "\nOh, yeah. They're immune to explosions now too.",
            OnLeftClick = (elem) => Difficulties.Types["PieFactory"] = !Difficulties.Types["PieFactory"]
        };
        PieFactory.SetDimensions(100, 350, 300, 40);

        UltraMines = new("Ultra Mines", font, Color.White) {
            IsVisible = false,
            Tooltip = "Mines are now 2x as deadly!" +
            "\nTheir explosion radii are now 2x as big!",
            OnLeftClick = (elem) => Difficulties.Types["UltraMines"] = !Difficulties.Types["UltraMines"]
        };
        UltraMines.SetDimensions(100, 400, 300, 40);

        BulletHell = new("Bullet Hell", font, Color.White) {
            IsVisible = false,
            Tooltip = "Bullets now ricochet thrice as much as before!",
            OnLeftClick = (elem) => Difficulties.Types["BulletHell"] = !Difficulties.Types["BulletHell"]
        };
        BulletHell.SetDimensions(100, 450, 300, 40);

        AllInvisible = new("All Invisible", font, Color.White) {
            IsVisible = false,
            Tooltip = "Every single non-player tank is now invisible and no longer lay tracks!",
            OnLeftClick = (elem) => Difficulties.Types["AllInvisible"] = !Difficulties.Types["AllInvisible"]
        };
        AllInvisible.SetDimensions(100, 500, 300, 40);

        AllStationary = new("All Stationary", font, Color.White) {
            IsVisible = false,
            Tooltip = "Every single non-player tank is now stationary." +
            "\nThis should REDUCE difficulty.",
            OnLeftClick = (elem) => Difficulties.Types["AllStationary"] = !Difficulties.Types["AllStationary"]
        };
        AllStationary.SetDimensions(100, 550, 300, 40);

        AllHoming = new("Seekers", font, Color.White) {
            IsVisible = false,
            Tooltip = "Every enemy tank now has homing bullets.",
            OnLeftClick = (elem) => Difficulties.Types["AllHoming"] = !Difficulties.Types["AllHoming"]
        };
        AllHoming.SetDimensions(100, 600, 300, 40);

        Armored = new("Armored", font, Color.White) {
            IsVisible = false,
            Tooltip = "Every single non-player tank has 3 armor points added to it.",
            OnLeftClick = (elem) => Difficulties.Types["Armored"] = !Difficulties.Types["Armored"]
        };
        Armored.SetDimensions(100, 650, 300, 40);

        BumpUp = new("Bump Up", font, Color.White) {
            IsVisible = false,
            Tooltip = "Makes the game a bit harder by \"Bumping up\" each tank, giving them one extra tier.",
            OnLeftClick = (elem) => Difficulties.Types["BumpUp"] = !Difficulties.Types["BumpUp"]
        };
        BumpUp.SetDimensions(100, 700, 300, 40);

        Monochrome = new("Monochrome", font, Color.White) {
            IsVisible = false,
            Tooltip = "Makes every tank the tank of your choice." +
            "\n\"Bump Up\" effects are ignored.",
            OnLeftClick = (elem) => {
                if (Difficulties.MonochromeValue + 1 >= TankID.Collection.Count)
                    Difficulties.MonochromeValue = TankID.None;
                else
                    Difficulties.MonochromeValue++;
                Difficulties.Types["Monochrome"] = Difficulties.MonochromeValue != TankID.None;
            },
            OnRightClick = (elem) => {
                if (Difficulties.MonochromeValue - 1 < TankID.None)
                    Difficulties.MonochromeValue = TankID.Collection.Count - 1;
                else
                    Difficulties.MonochromeValue--;
                Difficulties.Types["Monochrome"] = Difficulties.MonochromeValue != TankID.None;
            }
        };
        Monochrome.SetDimensions(100, 750, 300, 40);

        InfiniteLives = new("Infinite Lives", font, Color.White) {
            IsVisible = false,
            Tooltip = "You now have infinite lives. Have fun!",
            OnLeftClick = (elem) => Difficulties.Types["InfiniteLives"] = !Difficulties.Types["InfiniteLives"]
        };
        InfiniteLives.SetDimensions(450, 300, 300, 40);

        MasterMode = new("Master Mode", font, Color.White) {
            IsVisible = false,
            Tooltip = "Original tanks will become much more difficult." +
            "\nNew music, mechanics, and more!",
            OnLeftClick = (elem) => Difficulties.Types["MasterModBuff"] = !Difficulties.Types["MasterModBuff"]
        };
        MasterMode.SetDimensions(450, 350, 300, 40);

        TacticalPlanes = new("Tactical Planes", font, Color.White) {
            IsVisible = false,
            Tooltip = "Airplanes will occasionally come through the sky" +
            "\nand drop smoke grenades to block your vision!",
            OnLeftClick = (elem) => Difficulties.Types["TacticalPlanes"] = !Difficulties.Types["TacticalPlanes"]
        };
        TacticalPlanes.SetDimensions(450, 400, 300, 40);

        MachineGuns = new("Machine Guns", font, Color.White) {
            IsVisible = false,
            Tooltip = "Every tank (including the player) now has the ability to fire as fast as they want.",
            OnLeftClick = (elem) => Difficulties.Types["MachineGuns"] = !Difficulties.Types["MachineGuns"]
        };
        MachineGuns.SetDimensions(450, 450, 300, 40);

        RandomizedTanks = new("Randomized Tanks", font, Color.White, 0.5f) {
            IsVisible = false,
            Tooltip = "Every tank is now randomized." +
            "\nA black tank could appear where a brown tank would be!" +
            "\n\nLeft click to increase the upper limit." +
            "\nRight click to increase the lower limit." +
            "\nMiddle click to reset both to 'None'.",
            OnLeftClick = (elem) => {
                if (Difficulties.RandomTanksUpper + 1 >= TankID.Collection.Count)
                    Difficulties.RandomTanksUpper = TankID.None;
                else
                    Difficulties.RandomTanksUpper++;
                Difficulties.Types["RandomizedTanks"] = Difficulties.RandomTanksLower != TankID.None && Difficulties.RandomTanksUpper != TankID.None;
            },
            OnRightClick = (elem) => {
                if (Difficulties.RandomTanksLower + 1 >= TankID.Collection.Count)
                    Difficulties.RandomTanksLower = TankID.None;
                else
                    Difficulties.RandomTanksLower++;
                Difficulties.Types["RandomizedTanks"] = Difficulties.RandomTanksLower != TankID.None && Difficulties.RandomTanksUpper != TankID.None;
            },
            OnMiddleClick = (elem) => {
                Difficulties.RandomTanksLower = TankID.None;
                Difficulties.RandomTanksUpper = TankID.None;
            }
        };
        RandomizedTanks.SetDimensions(450, 500, 300, 40);

        ThunderMode = new("Thunder Mode", font, Color.White) {
            IsVisible = false,
            Tooltip = "The scene is much darker, and thunder is your only source of decent light.",
            OnLeftClick = (elem) => Difficulties.Types["ThunderMode"] = !Difficulties.Types["ThunderMode"]
        };
        ThunderMode.SetDimensions(450, 550, 300, 40);

        POVMode = new("POV Mode", font, Color.White) {
            IsVisible = false,
            Tooltip = "Play the game in the POV of your tank!" +
            "\nYou can move around inter-directionally with WASD, and aim by dragging the mouse.",
            OnLeftClick = (elem) => Difficulties.Types["POV"] = !Difficulties.Types["POV"]
        };
        POVMode.SetDimensions(450, 600, 300, 40);

        AiCompanion = new("AI Companion", font, Color.White) {
            IsVisible = false,
            Tooltip = "A random tank will spawn at your location and help you throughout every mission.",
            OnLeftClick = (elem) => Difficulties.Types["AiCompanion"] = !Difficulties.Types["AiCompanion"]
        };
        AiCompanion.SetDimensions(450, 650, 300, 40);

        Shotguns = new("Shotguns", font, Color.White) {
            IsVisible = false,
            Tooltip = "Every tank now fires a spread of bullets.",
            OnLeftClick = (elem) => Difficulties.Types["Shotguns"] = !Difficulties.Types["Shotguns"]
        };
        Shotguns.SetDimensions(450, 700, 300, 40);

        //init predictions
        Predictions = new("Predictions", font, Color.White) {
            IsVisible = false,
            Tooltip = "Every tank predicts your future position.",
            OnLeftClick = (elem) => Difficulties.Types["Predictions"] = !Difficulties.Types["Predictions"]
        };
        Predictions.SetDimensions(450, 750, 300, 40);

        RandomizedPlayer = new("Randomized Player", font, Color.White) {
            IsVisible = false,
            Tooltip = "You become a random enemy tank every life.",
            OnLeftClick = (elem) => Difficulties.Types["RandomPlayer"] = !Difficulties.Types["RandomPlayer"]
        };
        RandomizedPlayer.SetDimensions(800, 300, 300, 40);

        BulletBlocking = new("Bullet Blocking", font, Color.White) {
            IsVisible = false,
            Tooltip = "Enemies *attempt* to block your bullets." +
            "\nIt doesn't always work, sometimes even killing teammates.\nHigh fire-rate enemies are mostly affected.",
            OnLeftClick = (elem) => Difficulties.Types["BulletBlocking"] = !Difficulties.Types["BulletBlocking"]
        };
        BulletBlocking.SetDimensions(800, 350, 300, 40);

        FFA = new("Free-for-all", font, Color.White) {
            IsVisible = false,
            Tooltip = "Every tank is on their own!",
            OnLeftClick = (elem) => Difficulties.Types["FFA"] = !Difficulties.Types["FFA"]
        };
        FFA.SetDimensions(800, 400, 300, 40);

        LanternMode = new("Lantern Mode", font, Color.White) {
            IsVisible = false,
            Tooltip = "Everything is dark. Only you and your lantern can save you now.",
            OnLeftClick = (elem) => {
                Difficulties.Types["LanternMode"] = !Difficulties.Types["LanternMode"];
                GameShaders.LanternMode = Difficulties.Types["LanternMode"];
            }
        };
        LanternMode.SetDimensions(800, 450, 300, 40);
        DisguiseMode = new("Disguise", font, Color.White) {
            IsVisible = false,
            Tooltip = "You become a tank of your choosing during gameplay.",
            OnLeftClick = (elem) => {
                if (Difficulties.DisguiseValue + 1 >= TankID.Collection.Count)
                    Difficulties.DisguiseValue = TankID.None;
                else
                    Difficulties.DisguiseValue++;
                Difficulties.Types["Disguise"] = Difficulties.DisguiseValue != TankID.None;
            },
            OnRightClick = (elem) => {
                if (Difficulties.DisguiseValue - 1 < TankID.None)
                    Difficulties.DisguiseValue = TankID.Collection.Count - 1;
                else
                    Difficulties.DisguiseValue--;
                Difficulties.Types["Disguise"] = Difficulties.DisguiseValue != TankID.None;
            }
        };
        DisguiseMode.SetDimensions(800, 500, 300, 40);
        // make all buttons not-interactable for non-host clients.
    }
}
