using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TanksRebirth.Enums;
using TanksRebirth.GameContent.Properties;
using TanksRebirth.GameContent.Systems;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common;
using TanksRebirth.Internals.Common.Framework.Audio;
using TanksRebirth.Internals.Common.GameUI;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Internals.UI;
using FontStashSharp;
using TanksRebirth.GameContent.Systems.Coordinates;
using NativeFileDialogSharp;

namespace TanksRebirth.GameContent.UI
{
    public static class LevelEditor
    {
        public static bool Active { get; private set; }
        public static OggMusic Theme = new("Level Editor Theme", "Content/Assets/mainmenu/editor", 0.7f);

        public static UITextButton TestLevel;
        public static UITextButton Perspective;

        public static UITextButton BlocksCategory;
        public static UITextButton EnemyTanksCategory;
        public static UITextButton PlayerTanksCategory;

        public static UITextButton SaveLevel;

        public static UITextButton ReturnToEditor;

        #region ConfirmLevelContents
        // finish confirmation stuff later.
        public static Rectangle LevelContentsPanel;
        public static UITextInput LevelName;
        public static UITextInput LevelCampaignName;
        public static UITextInput LevelDescription;
        public static UITextInput LevelAuthor;
        public static UITextInput LevelVersion;
        public static UITextInput LevelTags;
        public static UITextInput LevelExtraLifeMissions;
        public static UITextInput LevelLoadingStripColor;
        public static UITextInput LevelLoadingBGColor;
        public static UITextButton ExitConfirm;
        public static UITextButton SaveLevelConfirm;
        #endregion
        #region Variables / Properties
        public static Category CurCategory { get; private set; }
        public static TankTier SelectedTankTier { get; private set; }
        public static TankTeam SelectedTankTeam { get; private set; }
        public static PlayerType SelectedPlayerType { get; private set; }
        public static Block.BlockType SelectedBlockType { get; private set; }
        public static int BlockHeight { get; private set; }
        public static bool Editing { get; internal set; }

        private static Mission _cachedMission;
        public enum Category {
            EnemyTanks,
            Blocks,
            PlayerTanks
        }

        public static Dictionary<string, Texture2D> RenderTextures = new();

        private static float _barOffset;
        private static Vector2 _origClick;
        private static float _maxScroll;
        private static List<string> _renderNamesTanks = new();
        private static List<string> _renderNamesBlocks = new();
        private static List<string> _renderNamesPlayers = new();
        #endregion

        // reduce hardcode -- make a variable that tracks height.
        public static void InitializeSaveMenu()
        {
            LevelName = new(TankGame.TextFont, Color.White, 1f, 20);
            LevelName.SetDimensions(() => new Vector2(LevelContentsPanel.X + 20, LevelContentsPanel.Y + 60), () => new(LevelContentsPanel.Width - 40, 50 / (1080 / GameUtils.WindowHeight)));
            LevelName.DefaultString = "Name";

            LevelCampaignName = new(TankGame.TextFont, Color.White, 1f, 20);
            LevelCampaignName.SetDimensions(() => new Vector2(LevelContentsPanel.X + 20, LevelContentsPanel.Y + 120), () => new(LevelContentsPanel.Width - 40, 50 / (1080 / GameUtils.WindowHeight)));
            LevelCampaignName.DefaultString = "Campaign Name (Optional)";
            LevelCampaignName.Tooltip = "The campaign this mission belongs to.";

            LevelDescription = new(TankGame.TextFont, Color.White, 1f, 20);
            LevelDescription.SetDimensions(() => new Vector2(LevelContentsPanel.X + 20, LevelContentsPanel.Y + 180), () => new(LevelContentsPanel.Width - 40, 50 / (1080 / GameUtils.WindowHeight)));
            LevelDescription.DefaultString = "Description";

            LevelAuthor = new(TankGame.TextFont, Color.White, 1f, 20);
            LevelAuthor.SetDimensions(() => new Vector2(LevelContentsPanel.X + 20, LevelContentsPanel.Y + 240), () => new(LevelContentsPanel.Width - 40, 50 / (1080 / GameUtils.WindowHeight)));
            LevelAuthor.DefaultString = "Author";

            LevelTags = new(TankGame.TextFont, Color.White, 1f, 20);
            LevelTags.SetDimensions(() => new Vector2(LevelContentsPanel.X + 20, LevelContentsPanel.Y + 300), () => new(LevelContentsPanel.Width - 40, 50 / (1080 / GameUtils.WindowHeight)));
            LevelTags.DefaultString = "Tags (separate tags with a ',')";

            LevelExtraLifeMissions = new(TankGame.TextFont, Color.White, 1f, 20);
            LevelExtraLifeMissions.SetDimensions(() => new Vector2(LevelContentsPanel.X + 20, LevelContentsPanel.Y + 360), () => new(LevelContentsPanel.Width - 40, 50 / (1080 / GameUtils.WindowHeight)));
            LevelExtraLifeMissions.DefaultString = "Extra Life Missions (separate tags with a ',')";

            LevelVersion = new(TankGame.TextFont, Color.White, 1f, 20);
            LevelVersion.SetDimensions(() => new Vector2(LevelContentsPanel.X + 20, LevelContentsPanel.Y + 420), () => new(LevelContentsPanel.Width - 40, 50 / (1080 / GameUtils.WindowHeight)));
            LevelVersion.DefaultString = "Level Version";

            LevelLoadingBGColor = new(TankGame.TextFont, Color.White, 1f, 20);
            LevelLoadingBGColor.SetDimensions(() => new Vector2(LevelContentsPanel.X + 20, LevelContentsPanel.Y + 480), () => new(LevelContentsPanel.Width - 40, 50 / (1080 / GameUtils.WindowHeight)));
            LevelLoadingBGColor.DefaultString = "Mission Loading: BG Color";

            LevelLoadingStripColor = new(TankGame.TextFont, Color.White, 1f, 20);
            LevelLoadingStripColor.SetDimensions(() => new Vector2(LevelContentsPanel.X + 20, LevelContentsPanel.Y + 540), () => new(LevelContentsPanel.Width - 40, 50 / (1080 / GameUtils.WindowHeight)));
            LevelLoadingStripColor.DefaultString = "Mission Loading: Strip Color";

            ExitConfirm = new("Return", TankGame.TextFont, Color.White);
            ExitConfirm.SetDimensions(() => new Vector2(LevelContentsPanel.X + 20, LevelContentsPanel.Y + LevelContentsPanel.Height - 60), () => new(200, 50 / (1080 / GameUtils.WindowHeight)));

            ExitConfirm.OnLeftClick = (l) => {
                GUICategory = UICategory.LevelEditor;
            };

            SaveLevelConfirm = new("Save Level", TankGame.TextFont, Color.White);
            SaveLevelConfirm.SetDimensions(() => new Vector2(LevelContentsPanel.X + 40 + ExitConfirm.Size.X, LevelContentsPanel.Y + LevelContentsPanel.Height - 60), () => new(200, 50 / (1080 / GameUtils.WindowHeight)));
            SaveLevelConfirm.Tooltip = "Save your mission to a file.\nKeep in mind: you will need to name the missions in numerical order.";
            SaveLevelConfirm.OnLeftClick = (l) => {
                // Mission.Save(LevelName.GetRealText(), )

                if (TankGame.IsWindows)
                {
                    var res = Dialog.FileSave("mission", TankGame.SaveDirectory);
                    if (res.Path != null && res.IsOk)
                    {
                        try
                        {
                            var ingameName = LevelName.Text;
                            var realName = Path.HasExtension(res.Path) ? Path.GetFileNameWithoutExtension(res.Path) : Path.GetFileName(res.Path);
                            var path = res.Path.Replace(realName, string.Empty);
                            Mission.Save(ingameName, path, false, realName);
                        }
                        catch
                        {
                            // guh
                        }
                    }
                    return;
                }

                GUICategory = UICategory.LevelEditor;
            };


            SetSaveMenuVisibility(false);
        }
        public enum UICategory {
            LevelEditor,
            SavingThings,
        }

        private static UICategory _category;
        public static UICategory GUICategory {
            get => _category;
            set {
                _category = value;

                if (_category == UICategory.SavingThings)
                {
                    SetSaveMenuVisibility(true);
                    SetLevelEditorVisibility(false);
                }
                else
                {
                    SetSaveMenuVisibility(false);
                    SetLevelEditorVisibility(true);
                }
            }
        }

        private static bool _saveMenuOpen;
        private static void SetSaveMenuVisibility(bool visible)
        {
            _saveMenuOpen = visible;
            LevelName.IsVisible = visible;
            LevelCampaignName.IsVisible = visible;
            SaveLevelConfirm.IsVisible = visible;
            ExitConfirm.IsVisible = visible;
            /*LevelDescription.IsVisible = visible;
            LevelAuthor.IsVisible = visible;
            LevelTags.IsVisible = visible;
            LevelVersion.IsVisible = visible;
            LevelExtraLifeMissions.IsVisible = visible;
            ExitConfirm.IsVisible = visible;
            LevelLoadingBGColor.IsVisible = visible;
            LevelLoadingStripColor.IsVisible = visible;*/
            LevelDescription.IsVisible = 
            LevelAuthor.IsVisible = 
            LevelTags.IsVisible = 
            LevelVersion.IsVisible = 
            LevelExtraLifeMissions.IsVisible = 
            LevelLoadingBGColor.IsVisible = 
            LevelLoadingStripColor.IsVisible = false;
        }
        private static void SetLevelEditorVisibility(bool visible)
        {
            EnemyTanksCategory.IsVisible = visible;
            BlocksCategory.IsVisible = visible;
            TestLevel.IsVisible = visible;
            Perspective.IsVisible = visible;
            PlayerTanksCategory.IsVisible = visible;
            SaveLevel.IsVisible = visible;
        }
        public static void Initialize()
        {
            #region Enumerable Init
            for (int i = 1; i < Enum.GetNames<TankTeam>().Length; i++)
            {
                var team = (TankTeam)i;

                var colorToAdd = (Color)typeof(Color).GetProperty(team.ToString()).GetValue(null);
                TeamColors.Add(colorToAdd);
            }
            foreach (var file in Directory.GetFiles(Path.Combine("Content", "Assets", "textures", "ui", "leveledit")))
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                var assetName = file;
                RenderTextures.Add(fileName, GameResources.GetGameResource<Texture2D>(assetName, false, false));
            }
            var enumNames = Enum.GetNames<TankTier>();
            for (int i = 0; i < enumNames.Length; i++)
            {
                var nTL = enumNames[i];

                if (RenderTextures.ContainsKey(nTL))
                    _renderNamesTanks.Add(nTL);
            }
            enumNames = Enum.GetNames<Block.BlockType>();
            for (int i = 0; i < enumNames.Length; i++)
            {
                var nTL = enumNames[i];

                if (RenderTextures.ContainsKey(nTL))
                    _renderNamesBlocks.Add(nTL);
            }
            enumNames = Enum.GetNames<PlayerType>();
            for (int i = 0; i < enumNames.Length; i++)
            {
                var nTL = enumNames[i];

                if (RenderTextures.ContainsKey(nTL))
                    _renderNamesPlayers.Add(nTL);
            }
            #endregion

            TestLevel = new("Test Level", TankGame.TextFont, Color.White);
            TestLevel.SetDimensions(() => new(GameUtils.WindowWidth * 0.01f, GameUtils.WindowHeight * 0.725f), () => new(200, 50));

            TestLevel.OnLeftClick = (l) => {
                Close();
                TankGame.OverheadView = false;

                _cachedMission = Mission.GetCurrent();
            };

            ReturnToEditor = new("Return to Editor", TankGame.TextFont, Color.White);
            ReturnToEditor.SetDimensions(() =>new(GameUtils.WindowWidth * 0.01f, GameUtils.WindowHeight * 0.02f), () => new(250, 50));

            ReturnToEditor.OnLeftClick = (l) => {
                Open(false);
                TankGame.OverheadView = true;
                GameProperties.InMission = false;
                GameHandler.CleanupScene();
                Mission.LoadDirectly(_cachedMission);
            };

            Perspective = new("Perspective", TankGame.TextFont, Color.White);
            Perspective.SetDimensions(() => new(GameUtils.WindowWidth * 0.125f, GameUtils.WindowHeight * 0.725f), () =>new(200, 50));

            Perspective.OnLeftClick = (l) => {
                TankGame.OverheadView = !TankGame.OverheadView;
            };

            BlocksCategory = new("Blocks", TankGame.TextFont, Color.White);
            BlocksCategory.SetDimensions(() => new(GameUtils.WindowWidth * 0.75f, GameUtils.WindowHeight * 0.725f), () => new(200, 50));
            BlocksCategory.OnLeftClick = (l) => {
                CurCategory = Category.Blocks;
            };

            EnemyTanksCategory = new("Enemies", TankGame.TextFont, Color.White);
            EnemyTanksCategory.SetDimensions(() => new(GameUtils.WindowWidth * 0.875f, GameUtils.WindowHeight * 0.725f), () => new(200, 50));
            EnemyTanksCategory.OnLeftClick = (l) => {
                CurCategory = Category.EnemyTanks;
            };
            PlayerTanksCategory = new("Players", TankGame.TextFont, Color.White);
            PlayerTanksCategory.SetDimensions(() => new(GameUtils.WindowWidth * 0.875f, GameUtils.WindowHeight * 0.65f), () => new(200, 50));
            PlayerTanksCategory.OnLeftClick = (l) => {
                CurCategory = Category.PlayerTanks;
            };

            SaveLevel = new("Save Level", TankGame.TextFont, Color.White);

            float width = 200;

            SaveLevel.SetDimensions(() => new(GameUtils.WindowWidth * 0.5f - width / 2, 10), () => new(width, 50));
            SaveLevel.OnLeftClick = (a) => {
                if (!_saveMenuOpen)
                    GUICategory = UICategory.SavingThings;
                else
                    GUICategory = UICategory.LevelEditor;
            };

            InitializeSaveMenu();

            SetLevelEditorVisibility(false);

            UIElement.cunoSucksElement = new() { IsVisible = false };
            UIElement.cunoSucksElement.Remove();
            UIElement.cunoSucksElement = new();
            UIElement.cunoSucksElement.SetDimensions(-1000789342, -783218, 0, 0);
        }
        public static void Open(bool fromMainMenu = true)
        {
            if (fromMainMenu)
            {
                IntermissionSystem.TimeBlack = 180;
                GameProperties.ShouldMissionsProgress = false;
                Task.Run(() =>
                {
                    while (IntermissionSystem.BlackAlpha > 0.8f || MainMenu.Active)
                        Thread.Sleep(TankGame.LogicTime);

                    Active = true;
                    TankGame.OverheadView = true;
                    Theme.Play();
                    SetLevelEditorVisibility(true);
                });
            }
            else
            {
                Theme.Play();
                Active = true;
                SetLevelEditorVisibility(true);
            }
            Editing = true;
        }
        public static void Close()
        {
            Active = false;
            Theme.SetVolume(0);
            Theme.Stop();
            SetLevelEditorVisibility(false);
            SetSaveMenuVisibility(false);
        }

        private static Rectangle _clickRect;

        private static Dictionary<Rectangle, (int, string)> ClickEventsPerItem = new(); // hover zone, id, description

        private static string _curDescription = string.Empty;
        private static Rectangle _curHoverRect;

        private static List<Color> TeamColors = new();

        public static Color SelectionColor = Color.NavajoWhite;
        public static Color HoverBoxColor = Color.SkyBlue;

        public static void Render()
        {
            #region Main UI
            int xOff = 0;
            _clickRect = new(0, (int)(GameUtils.WindowBottom.Y * 0.8f), GameUtils.WindowWidth, (int)(GameUtils.WindowHeight * 0.2f));
            TankGame.SpriteRenderer.Draw(TankGame.WhitePixel, _clickRect, null, Color.White, 0f, Vector2.Zero, default, 0f);

            var measure = TankGame.TextFont.MeasureString(_curDescription);

            if (_curDescription != null && _curDescription != string.Empty)
            {
                int padding = 20;
                var orig = new Vector2(0, TankGame.WhitePixel.Size().Y);
                TankGame.SpriteRenderer.Draw(TankGame.WhitePixel, 
                    new Rectangle((int)(GameUtils.WindowWidth / 2 - measure.X / 2 - padding), (int)(GameUtils.WindowHeight * 0.8f), (int)(measure.X + padding * 2), (int)(measure.Y + 20)), null, Color.White, 0f, orig, default, 0f);
            }

            TankGame.SpriteRenderer.DrawString(TankGame.TextFont, _curDescription, new Vector2(GameUtils.WindowWidth / 2, GameUtils.WindowHeight * 0.78f), Color.Black, Vector2.One, 0f, new Vector2(measure.X / 2, measure.Y));
            // level info
            TankGame.SpriteRenderer.Draw(TankGame.WhitePixel, new Rectangle(0, 0, 350, 500), null, Color.Gray, 0f, Vector2.Zero, default, 0f);
            TankGame.SpriteRenderer.Draw(TankGame.WhitePixel, new Rectangle(0, 0, 350, 40), null, Color.White, 0f, Vector2.Zero, default, 0f);
            // render teams

            // placement information
            TankGame.SpriteRenderer.Draw(TankGame.WhitePixel, new Rectangle(GameUtils.WindowWidth - 350, 0, 350, 500), null, Color.Gray, 0f, Vector2.Zero, default, 0f);
            TankGame.SpriteRenderer.Draw(TankGame.WhitePixel, new Rectangle(GameUtils.WindowWidth - 350, 0, 350, 40), null, Color.White, 0f, Vector2.Zero, default, 0f);
            TankGame.SpriteRenderer.DrawString(TankGame.TextFont, "Placement Information", new Vector2(GameUtils.WindowWidth - 325, 3), Color.Black, Vector2.One, 0f, Vector2.Zero);
            if (CurCategory == Category.Blocks)
            {
                TankGame.SpriteRenderer.DrawString(TankGame.TextFont, $"Block Stack: {BlockHeight}", new Vector2(GameUtils.WindowWidth - 335, 40), Color.White, Vector2.One, 0f, Vector2.Zero);
                // TankGame.SpriteRenderer.DrawString(TankGame.TextFont, $"Block Stack: {BlockHeight}", new Vector2(GameUtils.WindowWidth - 335, 40), Color.White, Vector2.One, 0f, Vector2.Zero);
            }

            if (CurCategory == Category.EnemyTanks || CurCategory == Category.PlayerTanks)
            {
                Vector2 start = new(GameUtils.WindowWidth - 250, 140);

                TankGame.SpriteRenderer.DrawString(TankGame.TextFont, "Tank Teams", new Vector2(start.X + 45, start.Y - 80), Color.White, Vector2.One, 0f, TankGame.TextFont.MeasureString("Tank Teams") / 2);

                TankGame.SpriteRenderer.Draw(TankGame.WhitePixel, new Rectangle((int)start.X, (int)(start.Y - 40), 40, 40), null, Color.Black, 0f, Vector2.Zero, default, 0f);
                TankGame.SpriteRenderer.DrawString(TankGame.TextFont, "No Team", new Vector2(start.X + 45, start.Y - 40), Color.Black, Vector2.One, 0f, Vector2.Zero);
                for (int i = 0; i < Enum.GetNames<TankTeam>().Length - 1; i++)
                {
                    var color = TeamColors[i];
                    TankGame.SpriteRenderer.DrawString(TankGame.TextFont, ((TankTeam)i + 1).ToString(), new Vector2(start.X + 45, start.Y + i * 40), color, Vector2.One, 0f, Vector2.Zero); ;
                    TankGame.SpriteRenderer.Draw(TankGame.WhitePixel, new Rectangle((int)start.X, (int)(start.Y + i * 40), 40, 40), null, color, 0f, Vector2.Zero, default, 0f);
                }
                TankGame.SpriteRenderer.DrawString(TankGame.TextFontLarge, ">", new Vector2(start.X - 25, start.Y + ((int)(SelectedTankTeam - 1) * 40)), Color.White, new(1f), 0f, TankGame.TextFontLarge.MeasureString(">") / 2);

                if (SelectedTankTeam != TankTeam.Magenta)
                    TankGame.SpriteRenderer.DrawString(TankGame.TextFont, "v", new Vector2(start.X - 25, start.Y + ((int)(SelectedTankTeam - 1) * 40 + 50)), Color.White, new(1f), 0f, TankGame.TextFont.MeasureString("v") / 2);
                if (SelectedTankTeam != TankTeam.NoTeam)
                    TankGame.SpriteRenderer.DrawString(TankGame.TextFont, "v", new Vector2(start.X - 25, start.Y + ((int)(SelectedTankTeam - 1) * 40 - 10)), Color.White, new(1f), MathHelper.Pi, TankGame.TextFont.MeasureString("v") / 2);
            }
            else if (CurCategory == Category.Blocks)
            {
                var tex = SelectedBlockType != Block.BlockType.Hole ? $"{SelectedBlockType}_{BlockHeight}" : $"{SelectedBlockType}";
                var size = RenderTextures[tex].Size();
                Vector2 start = new(GameUtils.WindowWidth - 175, 450);
                TankGame.SpriteRenderer.Draw(RenderTextures[tex], start, null, Color.White, 0f, new(size.X / 2, size.Y), /*new Vector2(1f, (float)GameUtils.WindowHeight / 1080)*/ Vector2.One, default, 0f);
                // TODO: reduce the hardcode for modders, yeah
                if (SelectedBlockType != Block.BlockType.Teleporter && SelectedBlockType != Block.BlockType.Hole)
                {
                    TankGame.SpriteRenderer.DrawString(TankGame.TextFontLarge, "v", new Vector2(start.X + 100, start.Y - 75), Color.White, new(1f), 0f, TankGame.TextFontLarge.MeasureString("v") / 2);
                    TankGame.SpriteRenderer.DrawString(TankGame.TextFontLarge, "v", new Vector2(start.X - 100, start.Y - 25), Color.White, new(1f), MathHelper.Pi, TankGame.TextFontLarge.MeasureString("v") / 2);
                }
            }

            TankGame.SpriteRenderer.Draw(TankGame.WhitePixel, _curHoverRect, null, HoverBoxColor * 0.5f, 0f, Vector2.Zero, default, 0f);

            TankGame.SpriteRenderer.DrawString(TankGame.TextFont, "Level Information", new Vector2(55, 3), Color.Black, Vector2.One, 0f, Vector2.Zero);
            TankGame.SpriteRenderer.DrawString(TankGame.TextFont, $"Total Enemy Tanks: {AITank.CountAll()}", new Vector2(10, 40), Color.White, Vector2.One, 0f, Vector2.Zero);
            TankGame.SpriteRenderer.DrawString(TankGame.TextFont, $"Difficulty Rating: {DifficultyAlgorithm.GetDifficulty(Mission.GetCurrent()):0.00}", new Vector2(10, 80), Color.White, Vector2.One, 0f, Vector2.Zero);

            if (CurCategory == Category.EnemyTanks)
            {
                for (int i = 0; i < _renderNamesTanks.Count; i++)
                {
                    ClickEventsPerItem[new Rectangle((int)(34 + xOff + _barOffset), (int)(GameUtils.WindowBottom.Y * 0.8f), 234, 256)] =
                        (i + 2, (TankTier)(i + 2) switch
                        {
                            TankTier.Brown => "An easy to defeat, stationary, slow firing enemy.",
                            TankTier.Ash => "Similar to the Brown tank, but can move slowly and fire more often.",
                            TankTier.Marine => "A slow-moving, passive and methodical enemy.\nShoots off rockets instead of standard shells.",
                            TankTier.Yellow => "Not incredibly dangerous. Lays mines often and move fast.",
                            TankTier.Pink => "A slow, but incredibly persistent and aggressive tank.\nCan fire multiple bullets at once.",
                            TankTier.Green => "A highly-dangerous but stationary tank.\nShoots multiple rockets that can bounce twice.",
                            TankTier.Violet => "A fast-moving, intelligent tank that can spray\nup to 5 bullets at once and can lay mines.",
                            TankTier.White => "A slow-moving, powerful tank that turns invisible\nat the start of the mission and can fire multiple bullets and lay mines.",
                            TankTier.Black => "A tank that moves faster than the player, fires rockets often,\nis aggressive, and can dodge well. Can lay mines.",
                            TankTier.Bronze => "A simple, stationary tank that can fire multiple bullets\nand aims directly at its target.",
                            TankTier.Silver => "An advanced and difficult mobile tank that can fire up\nto 8 bullets at fast rates and can dodge well. Can lay mines",
                            TankTier.Sapphire => "A deadly tank that can rapidly fire up to 3 rockets\nin a single volley. Can lay mines.",
                            TankTier.Ruby => "A slow, but very aggressive tank that constantly fires shells.",
                            TankTier.Citrine => "An insanely fast tank that shoots very fast shells at its target.\nLays mines frequently.",
                            TankTier.Amethyst => "A fast, very agile, and dodgy tank that fires a spread\nof shells at its target. Can lay mines.",
                            TankTier.Emerald => "A stationary tank that turns invisible at the start of the round.\nFires multiple double-bounce rockets at its target.",
                            TankTier.Gold => "A slow and mobile tank that turns invisible at the start of the round\nand lays no tracks for players to see. Can lay mines.",
                            TankTier.Obsidian => "A very fast, but very unintelligent tank that fires\nfast rockets frequently with 2 bounces. Can lay mines.",
                            TankTier.Granite => "A very slow, mobile tank that becomes stunned\n for a while upon firing. Shoots faster-than-normal shells.",
                            TankTier.Bubblegum => "A medium-speed, fast-firing dodgy tank that can lay mines.",
                            TankTier.Water => "A medium-speed tank that moves linearly.\nFires rockets that bounce one time, and can also lay mines.",
                            TankTier.Crimson => "A slow tank that fires in a burst of 5 shells. Can lay mines.",
                            TankTier.Tiger => "A very wary tank that strafes very often and dodges very often.\nFires shells fast and lays mines often.",
                            TankTier.Fade => "A tank that is mainly focused on dodging threats.\nFires often and can lay mines.",
                            TankTier.Creeper => "A highly dangerous tank that fires very fast rockets\n that bounce 3 times. Moves very slowly.",
                            TankTier.Gamma => "A stationary tank that fires insanely fast bullets at rapid frequency.",
                            TankTier.Marble => "An apex predator tank that is good at dodging, good at\ncalculating shots, is fast, and fires fast rockets rapidly.",
                            _ => "What?"
                        }); // TODO: localize this. i hate english.

                    TankGame.SpriteRenderer.Draw(RenderTextures[_renderNamesTanks[i]], new Vector2(24 + xOff + _barOffset, GameUtils.WindowBottom.Y * 0.75f), null, (int)SelectedTankTier - 2 == i ? SelectionColor : Color.White, 0f, Vector2.Zero, new Vector2(1f, (float)GameUtils.WindowHeight / 1080), default, 0f);
                    xOff += 234;
                }
                _maxScroll = xOff;
            }
            else if (CurCategory == Category.Blocks)
            {
                for (int i = 0; i < _renderNamesBlocks.Count; i++)
                {
                    ClickEventsPerItem[new Rectangle((int)(34 + xOff + _barOffset), (int)(GameUtils.WindowBottom.Y * 0.8f), 234, 256)] =
                        (i, (Block.BlockType)i switch
                        {
                            Block.BlockType.Wood => "An indestructible obstacle.",
                            Block.BlockType.Cork => "An obstacle that can be destroyed by mines.",
                            Block.BlockType.Hole => "An obstacle that tanks cannot travel through, but shells can.",
                            _ => "What?"
                        });

                    TankGame.SpriteRenderer.Draw(RenderTextures[_renderNamesBlocks[i]], new Vector2(24 + xOff + _barOffset, GameUtils.WindowBottom.Y * 0.75f), null, (int)SelectedBlockType == i ? SelectionColor : Color.White, 0f, Vector2.Zero, new Vector2(1f, (float)GameUtils.WindowHeight / 1080), default, 0f);
                    xOff += 234;
                }
                _maxScroll = xOff;
            }
            else if (CurCategory == Category.PlayerTanks)
            {
                for (int i = 0; i < _renderNamesPlayers.Count; i++)
                {
                    ClickEventsPerItem[new Rectangle((int)(34 + xOff + _barOffset), (int)(GameUtils.WindowBottom.Y * 0.8f), 234, 256)] =
                        (i, (PlayerType)i switch
                        {
                            PlayerType.Blue => "The blue player tank (P1)",
                            PlayerType.Red => "The red player tank. (P2)",
                            _ => "What?"
                        });

                    TankGame.SpriteRenderer.Draw(RenderTextures[_renderNamesPlayers[i]], new Vector2(24 + xOff + _barOffset, GameUtils.WindowBottom.Y * 0.75f), null, (int)SelectedPlayerType == i ? SelectionColor : Color.White, 0f, Vector2.Zero, new Vector2(1f, (float)GameUtils.WindowHeight / 1080), default, 0f);
                    xOff += 234;
                }
                _maxScroll = xOff;
            }
            if (DebugUtils.DebuggingEnabled)
            {
                int a = 0;
                foreach (var thing in ClickEventsPerItem)
                {
                    if (DebugUtils.DebugLevel == 3)
                        TankGame.SpriteRenderer.Draw(TankGame.WhitePixel, thing.Key, null, Color.Red * 0.5f, 0f, Vector2.Zero, default, 0f);
                    var text = thing.Key.Contains(GameUtils.MousePosition.ToPoint()) ? $"{thing.Key} ---- {(TankTier)thing.Value.Item1} (HOVERED)" : $"{thing.Key} ---- {(TankTier)thing.Value.Item1}";
                    DebugUtils.DrawDebugString(TankGame.SpriteRenderer, text, new Vector2(500, 20 + a), 3);
                    a += 20;
                }
            }
            #endregion

            if (Active && TankGame.HoveringAnyTank)
            {
                var tex = GameResources.GetGameResource<Texture2D>("Assets/textures/ui/leveledit/rotate");
                TankGame.SpriteRenderer.Draw(tex,
                   GameUtils.MousePosition + new Vector2(20, -20), null, Color.White, 0f, new Vector2(0, tex.Size().Y), 0.2f, default, 0f);
            }
            if (Active && GUICategory == UICategory.SavingThings)
                TankGame.SpriteRenderer.Draw(TankGame.WhitePixel,
                   LevelContentsPanel, null, Color.Gray, 0f, Vector2.Zero, default, 0f);
        }

        public static void Update()
        {
            LevelContentsPanel = new Rectangle(GameUtils.WindowWidth / 4, (int)(GameUtils.WindowHeight * 0.1f), GameUtils.WindowWidth / 2, (int)(GameUtils.WindowHeight * 0.625f));
            PlacementSquare.PlacesBlock = CurCategory == Category.Blocks;
            switch (CurCategory) {
                case Category.EnemyTanks:
                    EnemyTanksCategory.Color = Color.DeepSkyBlue;
                    BlocksCategory.Color = Color.White;
                    PlayerTanksCategory.Color = Color.White;
                    break;
                case Category.Blocks:
                    EnemyTanksCategory.Color = Color.White;
                    BlocksCategory.Color = Color.DeepSkyBlue;
                    PlayerTanksCategory.Color = Color.White;
                    break;
                case Category.PlayerTanks:
                    EnemyTanksCategory.Color = Color.White;
                    BlocksCategory.Color = Color.White;
                    PlayerTanksCategory.Color = Color.DeepSkyBlue;
                    break;
            }
            if (Active)
            {
                Theme.SetVolume(0.4f * TankGame.Settings.MusicVolume);

                _curDescription = string.Empty;

                _curHoverRect = new();
                foreach (var thing in ClickEventsPerItem)
                {
                    if (thing.Key.Contains(GameUtils.MousePosition.ToPoint()))
                    {
                        _curHoverRect = thing.Key;
                        if (thing.Value.Item2 != null)
                            _curDescription = thing.Value.Item2;
                    }
                }

                if (Input.CanDetectClick())
                {
                    _origClick = GameUtils.MousePosition - new Vector2(_barOffset, 0);

                    for (int i = 0; i < ClickEventsPerItem.Count; i++)
                    {
                        var evt = ClickEventsPerItem.ElementAt(i);
                        if (evt.Key.Contains(GameUtils.MousePosition.ToPoint())) {
                            if (CurCategory == Category.EnemyTanks) {
                                SelectedTankTier = (TankTier)evt.Value.Item1;
                            }
                            else if (CurCategory == Category.Blocks) {
                                SelectedBlockType = (Block.BlockType)evt.Value.Item1;
                            }
                            else if (CurCategory == Category.PlayerTanks) {
                                SelectedPlayerType = (PlayerType)evt.Value.Item1;
                            }
                        }
                    }
                }
                if (Input.MouseLeft && _clickRect.Contains(GameUtils.MousePosition.ToPoint()))
                {
                    _barOffset = GameUtils.MousePosition.X - _origClick.X;
                    if (_barOffset < -_maxScroll)
                        _barOffset = -_maxScroll;
                    if (_barOffset > 0)
                    {
                        _barOffset = 0;
                        _origClick = GameUtils.MousePosition - new Vector2(_barOffset, 0);
                    }
                }

                BlockHeight = MathHelper.Clamp(BlockHeight, 1, 7);

                if (CurCategory == Category.EnemyTanks || CurCategory == Category.PlayerTanks) {
                    // tank place handling, etc
                    if (Input.KeyJustPressed(Microsoft.Xna.Framework.Input.Keys.Up))
                        SelectedTankTeam--;
                    if (Input.KeyJustPressed(Microsoft.Xna.Framework.Input.Keys.Down))
                        SelectedTankTeam++;
                    if (SelectedTankTeam > TankTeam.Magenta)
                        SelectedTankTeam = TankTeam.Magenta;
                    if (SelectedTankTeam < TankTeam.NoTeam)
                        SelectedTankTeam = TankTeam.NoTeam;
                }
                else if (CurCategory == Category.Blocks) {
                    if (Input.KeyJustPressed(Microsoft.Xna.Framework.Input.Keys.Up))
                        BlockHeight++;
                    if (Input.KeyJustPressed(Microsoft.Xna.Framework.Input.Keys.Down))
                        BlockHeight--;
                    if (SelectedBlockType == Block.BlockType.Hole || SelectedBlockType == Block.BlockType.Teleporter)
                        BlockHeight = 1;

                    BlockHeight = MathHelper.Clamp(BlockHeight, 1, 7);
                }
                ClickEventsPerItem.Clear();
            }
            else if (Editing && !Active && _cachedMission != default && GameProperties.InMission)
                if (GameHandler.NothingCanHappenAnymore(_cachedMission, out bool victory))
                    ReturnToEditor?.OnLeftClick?.Invoke(null);
            if (ReturnToEditor != null)
                ReturnToEditor.IsVisible = Editing && !Active && !MainMenu.Active;
        }
    }
}
