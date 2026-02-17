using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using System;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.ID;
using TanksRebirth.GameContent.RebirthUtils;
using TanksRebirth.GameContent.Systems;
using TanksRebirth.Internals.Common;
using TanksRebirth.Internals.Common.GameUI;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Net;
using TanksRebirth.Internals.UI;
using TanksRebirth.Internals.Common.Framework.Audio;

namespace TanksRebirth.GameContent.UI.MainMenu;

#pragma warning disable

// todo: remake for a good visual polish?
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

    public static List<UIElement> AllDifficultyButtons = [];

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
        if (MenuState != UIState.Difficulties) return;

        DisguiseMode.Text = "Disguise: " + TankID.Collection.GetKey(Modifiers.DisguiseValue);
        Monochrome.Text = "Monochrome: " + TankID.Collection.GetKey(Modifiers.MonochromeValue);
        RandomizedTanks.Text = $"Randomized Tanks\nLower: {TankID.Collection.GetKey(Modifiers.RandomTanksLower)} | Upper: {TankID.Collection.GetKey(Modifiers.RandomTanksUpper)}";
        Modifiers.Map[Modifiers.RANDOM_ENEMY] = Modifiers.RandomTanksLower > 0 && Modifiers.RandomTanksUpper > 0;

        // me in march 2024: what the fuck is this code.
        // also me in july 2025: what the FUCK is this code
        TanksAreCalculators.Color = Modifiers.Map[Modifiers.EXTRA_CALCS] ? Color.Lime : Color.Red;
        PieFactory.Color = Modifiers.Map[Modifiers.MINE_SPAM] ? Color.Lime : Color.Red;
        UltraMines.Color = Modifiers.Map[Modifiers.BIG_MINES] ? Color.Lime : Color.Red;
        BulletHell.Color = Modifiers.Map[Modifiers.TRIPLE_BOUNCE] ? Color.Lime : Color.Red;
        AllInvisible.Color = Modifiers.Map[Modifiers.INVIS] ? Color.Lime : Color.Red;
        AllStationary.Color = Modifiers.Map[Modifiers.STATIONARY] ? Color.Lime : Color.Red;
        AllHoming.Color = Modifiers.Map[Modifiers.HOMING] ? Color.Lime : Color.Red;
        Armored.Color = Modifiers.Map[Modifiers.ARMOR] ? Color.Lime : Color.Red;
        BumpUp.Color = Modifiers.Map[Modifiers.BUMP] ? Color.Lime : Color.Red;
        Monochrome.Color = Modifiers.MonochromeValue > 0 ? Color.Lime : Color.Red;
        InfiniteLives.Color = Modifiers.Map[Modifiers.INF_LIFE] ? Color.Lime : Color.Red;
        MasterMode.Color = Modifiers.Map[Modifiers.MASTER] ? Color.Lime : Color.Red;
        TacticalPlanes.Color = Modifiers.Map[Modifiers.PLANES] ? Color.Lime : Color.Red;
        MachineGuns.Color = Modifiers.Map[Modifiers.MACHINE_GUNS] ? Color.Lime : Color.Red;
        RandomizedTanks.Color = Modifiers.Map[Modifiers.RANDOM_ENEMY] ? Color.Lime : Color.Red;
        ThunderMode.Color = Modifiers.Map[Modifiers.THUNDER] ? Color.Lime : Color.Red;
        POVMode.Color = Modifiers.Map[Modifiers.POV] ? Color.Lime : Color.Red;
        AiCompanion.Color = Modifiers.Map[Modifiers.AI_COMPANION] ? Color.Lime : Color.Red;
        Shotguns.Color = Modifiers.Map[Modifiers.SHOTGUNS] ? Color.Lime : Color.Red;
        Predictions.Color = Modifiers.Map[Modifiers.PREDICTIONS] ? Color.Lime : Color.Red;
        RandomizedPlayer.Color = Modifiers.Map[Modifiers.RANDOM_PLAYER] ? Color.Lime : Color.Red;
        BulletBlocking.Color = Modifiers.Map[Modifiers.DEFLECT] ? Color.Lime : Color.Red;
        FFA.Color = Modifiers.Map[Modifiers.FFA] ? Color.Lime : Color.Red;
        LanternMode.Color = Modifiers.Map[Modifiers.LANTERN] ? Color.Lime : Color.Red;
        DisguiseMode.Color = Modifiers.Map[Modifiers.DISGUISE] ? Color.Lime : Color.Red;

        if (IsActive && Client.IsConnected() && Client.IsHost())
            Client.SendDiffiulties();
    }
    public static void RenderDifficultiesMenu() {
        if (MenuState == UIState.Difficulties) {
            DrawUtils.DrawStringWithBorder(TankGame.SpriteRenderer, FontGlobals.RebirthFont,
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
            OnLeftClick = (elem) => Modifiers.Map[Modifiers.EXTRA_CALCS] = !Modifiers.Map[Modifiers.EXTRA_CALCS]
        };
        PieFactory = new("Lemon Pie Factory", font, Color.White) {
            IsVisible = false,
            Tooltip = "Makes yellow tanks absurdly more dangerous by" +
            "\nturning them into mine-laying machines." +
            "\nOh, yeah. They're immune to explosions now too.",
            OnLeftClick = (elem) => Modifiers.Map[Modifiers.MINE_SPAM] = !Modifiers.Map[Modifiers.MINE_SPAM]
        };
        UltraMines = new("Ultra Mines", font, Color.White) {
            IsVisible = false,
            Tooltip = "Mines are now 2x as deadly!" +
            "\nTheir explosion radii are now 2x as big!",
            OnLeftClick = (elem) => Modifiers.Map[Modifiers.BIG_MINES] = !Modifiers.Map[Modifiers.BIG_MINES]
        };
        BulletHell = new("Bullet Hell", font, Color.White) {
            IsVisible = false,
            Tooltip = "Bullets now ricochet thrice as much as before!",
            OnLeftClick = (elem) => Modifiers.Map[Modifiers.TRIPLE_BOUNCE] = !Modifiers.Map[Modifiers.TRIPLE_BOUNCE]
        };
        AllInvisible = new("All Invisible", font, Color.White) {
            IsVisible = false,
            Tooltip = "Every single non-player tank is now invisible and no longer lay tracks!",
            OnLeftClick = (elem) => Modifiers.Map[Modifiers.INVIS] = !Modifiers.Map[Modifiers.INVIS]
        };
        AllStationary = new("All Stationary", font, Color.White) {
            IsVisible = false,
            Tooltip = "Every single non-player tank is now stationary." +
            "\nThis should REDUCE difficulty.",
            OnLeftClick = (elem) => Modifiers.Map[Modifiers.STATIONARY] = !Modifiers.Map[Modifiers.STATIONARY]
        };
        AllHoming = new("Seekers", font, Color.White) {
            IsVisible = false,
            Tooltip = "Every enemy tank now has homing bullets.",
            OnLeftClick = (elem) => Modifiers.Map[Modifiers.HOMING] = !Modifiers.Map[Modifiers.HOMING]
        };
        Armored = new("Armored", font, Color.White) {
            IsVisible = false,
            Tooltip = "Every single non-player tank has 3 armor points added to it.",
            OnLeftClick = (elem) => Modifiers.Map[Modifiers.ARMOR] = !Modifiers.Map[Modifiers.ARMOR]
        };
        BumpUp = new("Bump Up", font, Color.White) {
            IsVisible = false,
            Tooltip = "Makes the game a bit harder by \"Bumping up\" each tank, giving them one extra tier.",
            OnLeftClick = (elem) => Modifiers.Map[Modifiers.BUMP] = !Modifiers.Map[Modifiers.BUMP]
        };
        Monochrome = new("Monochrome", font, Color.White) {
            IsVisible = false,
            Tooltip = "Makes every tank the tank of your choice." +
            "\n\"Bump Up\" effects are ignored.",
            OnLeftClick = (elem) => {
                if (Modifiers.MonochromeValue + 1 >= TankID.Collection.Count)
                    Modifiers.MonochromeValue = TankID.None;
                else
                    Modifiers.MonochromeValue++;
                Modifiers.Map["Monochrome"] = Modifiers.MonochromeValue != TankID.None;
            },
            OnRightClick = (elem) => {
                if (Modifiers.MonochromeValue - 1 < TankID.None)
                    Modifiers.MonochromeValue = TankID.Collection.Count - 1;
                else
                    Modifiers.MonochromeValue--;
                Modifiers.Map["Monochrome"] = Modifiers.MonochromeValue != TankID.None;
            }
        };
        InfiniteLives = new("Infinite Lives", font, Color.White) {
            IsVisible = false,
            Tooltip = "You now have infinite lives. Have fun!",
            OnLeftClick = (elem) => Modifiers.Map[Modifiers.INF_LIFE] = !Modifiers.Map[Modifiers.INF_LIFE]
        };
        MasterMode = new("Master Mode", font, Color.White) {
            IsVisible = false,
            Tooltip = "Original tanks will become much more difficult." +
            "\nNew music, mechanics, and more!",
            OnLeftClick = (elem) => Modifiers.Map[Modifiers.MASTER] = !Modifiers.Map[Modifiers.MASTER]
        };
        TacticalPlanes = new("Tactical Planes", font, Color.White) {
            IsVisible = false,
            Tooltip = "Airplanes will occasionally come through the sky" +
            "\nand drop smoke grenades to block your vision!",
            OnLeftClick = (elem) => Modifiers.Map[Modifiers.PLANES] = !Modifiers.Map[Modifiers.PLANES]
        };
        MachineGuns = new("Machine Guns", font, Color.White) {
            IsVisible = false,
            Tooltip = "Every tank (including the player) now has the ability to fire as fast as they want.",
            OnLeftClick = (elem) => Modifiers.Map[Modifiers.MACHINE_GUNS] = !Modifiers.Map[Modifiers.MACHINE_GUNS]
        };
        RandomizedTanks = new("Randomized Tanks", font, Color.White, 0.5f) {
            IsVisible = false,
            Tooltip = "Every tank is now randomized." +
            "\nA black tank could appear where a brown tank would be!" +
            "\n\nLeft click to increase the lower limit." +
            "\nRight click to increase the upper limit." +
            "\nMiddle click to reset both to 'None'.",
            OnRightClick = (elem) => {
                if (Modifiers.RandomTanksUpper + 1 >= TankID.Collection.Count)
                    Modifiers.RandomTanksUpper = TankID.None;
                else
                    Modifiers.RandomTanksUpper++;
                Modifiers.Map["RandomizedTanks"] = Modifiers.RandomTanksLower != TankID.None && Modifiers.RandomTanksUpper != TankID.None;
            },
            OnLeftClick = (elem) => {
                if (Modifiers.RandomTanksLower + 1 >= TankID.Collection.Count)
                    Modifiers.RandomTanksLower = TankID.None;
                else
                    Modifiers.RandomTanksLower++;
                Modifiers.Map["RandomizedTanks"] = Modifiers.RandomTanksLower != TankID.None && Modifiers.RandomTanksUpper != TankID.None;
            },
            OnMiddleClick = (elem) => {
                Modifiers.RandomTanksLower = TankID.None;
                Modifiers.RandomTanksUpper = TankID.None;
            }
        };
        ThunderMode = new("Thunder Mode", font, Color.White) {
            IsVisible = false,
            Tooltip = "The scene is much darker, and thunder is your only source of decent light.",
            OnLeftClick = (elem) => Modifiers.Map[Modifiers.THUNDER] = !Modifiers.Map[Modifiers.THUNDER]
        };
        POVMode = new("POV Mode", font, Color.White) {
            IsVisible = false,
            Tooltip = "Play the game in the POV of your tank!" +
            "\nYou can move around inter-directionally with WASD, and aim by dragging the mouse.",
            OnLeftClick = (elem) => Modifiers.Map[Modifiers.POV] = !Modifiers.Map[Modifiers.POV]
        };
        AiCompanion = new("AI Companion", font, Color.White) {
            IsVisible = false,
            Tooltip = "A random tank will spawn at your location and help you throughout every mission.",
            OnLeftClick = (elem) => Modifiers.Map[Modifiers.AI_COMPANION] = !Modifiers.Map[Modifiers.AI_COMPANION]
        };
        Shotguns = new("Shotguns", font, Color.White) {
            IsVisible = false,
            Tooltip = "Every tank now fires a spread of bullets.",
            OnLeftClick = (elem) => Modifiers.Map[Modifiers.SHOTGUNS] = !Modifiers.Map[Modifiers.SHOTGUNS]
        };
        Predictions = new("Predictions", font, Color.White) {
            IsVisible = false,
            Tooltip = "Every tank predicts your future position.",
            OnLeftClick = (elem) => Modifiers.Map[Modifiers.PREDICTIONS] = !Modifiers.Map[Modifiers.PREDICTIONS]
        };
        RandomizedPlayer = new("Randomized Player", font, Color.White) {
            IsVisible = false,
            Tooltip = "You become a random enemy tank every life.",
            OnLeftClick = (elem) => Modifiers.Map[Modifiers.RANDOM_PLAYER] = !Modifiers.Map[Modifiers.RANDOM_PLAYER]
        };
        BulletBlocking = new("Bullet Blocking", font, Color.White) {
            IsVisible = false,
            Tooltip = "Enemies *attempt* to block your bullets." +
            "\nIt doesn't always work, sometimes even killing teammates.\nHigh fire-rate enemies are mostly affected.",
            OnLeftClick = (elem) => Modifiers.Map[Modifiers.DEFLECT] = !Modifiers.Map[Modifiers.DEFLECT]
        };
        FFA = new("Free-for-all", font, Color.White) {
            IsVisible = false,
            Tooltip = "Every tank is on their own!",
            OnLeftClick = (elem) => Modifiers.Map[Modifiers.FFA] = !Modifiers.Map[Modifiers.FFA]
        };
        LanternMode = new("Lantern Mode", font, Color.White) {
            IsVisible = false,
            Tooltip = "Everything is dark. Only you and your lantern can save you now.",
            OnLeftClick = (elem) => {
                Modifiers.Map[Modifiers.LANTERN] = !Modifiers.Map[Modifiers.LANTERN];
            }
        };
        DisguiseMode = new("Disguise", font, Color.White) {
            IsVisible = false,
            Tooltip = "You become a tank of your choosing during gameplay.",
            OnLeftClick = (elem) => {
                if (Modifiers.DisguiseValue + 1 >= TankID.Collection.Count)
                    Modifiers.DisguiseValue = TankID.None;
                else
                    Modifiers.DisguiseValue++;
                Modifiers.Map[Modifiers.DISGUISE] = Modifiers.DisguiseValue != TankID.None;
            },
            OnRightClick = (elem) => {
                if (Modifiers.DisguiseValue - 1 < TankID.None)
                    Modifiers.DisguiseValue = TankID.Collection.Count - 1;
                else
                    Modifiers.DisguiseValue--;
                Modifiers.Map[Modifiers.DISGUISE] = Modifiers.DisguiseValue != TankID.None;
            }
        };

        AllDifficultyButtons.AddRange(new UITextButton[] { TanksAreCalculators, PieFactory, UltraMines, BulletHell, AllInvisible, AllStationary, Armored, AllHoming, BumpUp, Monochrome,
        InfiniteLives, MasterMode, TacticalPlanes, MachineGuns, RandomizedTanks, ThunderMode, POVMode, AiCompanion, Shotguns, Predictions,
        RandomizedPlayer, BulletBlocking, FFA, LanternMode, DisguiseMode });

        // make all buttons not-interactable for non-host clients.
    }
    static void ArrangeDifficultyButtons() {
        const int maxRowsPerColumn = 12;
        Vector2 buttonSize = new Vector2(300, 40);
        float padding = 20f;
        int totalButtons = AllDifficultyButtons.Count;
        int columnCount = (int)Math.Ceiling(totalButtons / (float)maxRowsPerColumn);

        // gets the total width of all columns combined (scaled after ToResolutionX)
        //float totalWidth = (buttonSize.X * columnCount + padding * (columnCount - 1)).ToResolutionX();
        float totalWidth = columnCount * buttonSize.X + (columnCount + 1) * padding;
        float startX = totalWidth / 2f; //(WindowUtils.WindowWidth - totalWidth) / 2f;

        for (int i = 0; i < totalButtons; i++) {
            var button = AllDifficultyButtons[i];
            int col = i / maxRowsPerColumn;
            int row = i % maxRowsPerColumn;

            float offsetX = (buttonSize.X + padding) * col;
            float offsetY = (buttonSize.Y + padding) * row;

            Vector2 position = new Vector2(startX + offsetX, (WindowUtils.WindowHeight * 0.1f) + offsetY);
            button.SetDimensions(() => position.ToResolution(), () => buttonSize.ToResolution());
            button.OnMouseOver = (a) => SoundPlayer.PlaySoundInstance(TickSound, SoundContext.Effect);
        }
    }
}
