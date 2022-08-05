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

        public static UITextButton ReturnToEditor;

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
        public static void Initialize()
        {
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

            TestLevel = new("Test Level", TankGame.TextFont, Color.White);
            TestLevel.SetDimensions(() =>
            {
                // let's be goofy and set the volume of the track to the music volume.

                return new(GameUtils.WindowWidth * 0.01f, GameUtils.WindowHeight * 0.725f);
            }, () =>
            {
                return new(200, 50);
            });

            TestLevel.OnLeftClick = (l) =>
            {
                Close();
                TankGame.OverheadView = false;

                _cachedMission = Mission.GetCurrent();
            };

            ReturnToEditor = new("Return to Editor", TankGame.TextFont, Color.White);
            ReturnToEditor.SetDimensions(() =>
            {
                // let's be goofy and set the volume of the track to the music volume.

                return new(GameUtils.WindowWidth * 0.01f, GameUtils.WindowHeight * 0.02f);
            }, () =>
            {
                return new(250, 50);
            });

            ReturnToEditor.OnLeftClick = (l) =>
            {
                Open(false);
                TankGame.OverheadView = true;

                Mission.LoadDirectly(_cachedMission);
            };

            Perspective = new("Perspective", TankGame.TextFont, Color.White);
            Perspective.SetDimensions(() =>
            {
                return new(GameUtils.WindowWidth * 0.125f, GameUtils.WindowHeight * 0.725f);
            }, () =>
            {
                return new(200, 50);
            });
            Perspective.OnLeftClick = (l) =>
            {
                TankGame.OverheadView = !TankGame.OverheadView;
            };

            BlocksCategory = new("Blocks", TankGame.TextFont, Color.White);
            BlocksCategory.SetDimensions(() =>
            {
                return new(GameUtils.WindowWidth * 0.75f, GameUtils.WindowHeight * 0.725f);
            }, () =>
            {
                return new(200, 50);
            });
            BlocksCategory.OnLeftClick = (l) => {
                CurCategory = Category.Blocks;
            };

            EnemyTanksCategory = new("Enemies", TankGame.TextFont, Color.White);
            EnemyTanksCategory.SetDimensions(() =>
            {
                return new(GameUtils.WindowWidth * 0.875f, GameUtils.WindowHeight * 0.725f);
            }, () =>
            {
                return new(200, 50);
            });
            EnemyTanksCategory.OnLeftClick = (l) => {
                CurCategory = Category.EnemyTanks;
            };
            PlayerTanksCategory = new("Players", TankGame.TextFont, Color.White);
            PlayerTanksCategory.SetDimensions(() =>
            {
                return new(GameUtils.WindowWidth * 0.875f, GameUtils.WindowHeight * 0.65f);
            }, () =>
            {
                return new(200, 50);
            });
            PlayerTanksCategory.OnLeftClick = (l) => {
                CurCategory = Category.PlayerTanks;
            };

            EnemyTanksCategory.IsVisible = false;
            BlocksCategory.IsVisible = false;
            TestLevel.IsVisible = false;
            Perspective.IsVisible = false;
            ReturnToEditor.IsVisible = false;
            PlayerTanksCategory.IsVisible = false;

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
                    EnemyTanksCategory.IsVisible = true;
                    BlocksCategory.IsVisible = true;
                    TestLevel.IsVisible = true;
                    Perspective.IsVisible = true;
                    PlayerTanksCategory.IsVisible = true;
                });
            }
            else
            {
                Theme.Play();
                Active = true;
                EnemyTanksCategory.IsVisible = true;
                BlocksCategory.IsVisible = true;
                TestLevel.IsVisible = true;
                Perspective.IsVisible = true;
                PlayerTanksCategory.IsVisible = true;
            }
            Editing = true;
        }
        public static void Close()
        {
            Active = false;
            Theme.SetVolume(0);
            Theme.Stop();
            EnemyTanksCategory.IsVisible = false;
            BlocksCategory.IsVisible = false;
            TestLevel.IsVisible = false;
            Perspective.IsVisible = false;
            PlayerTanksCategory.IsVisible = false;
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
                var tex = BlockHeight != 1 ? $"{SelectedBlockType}_{BlockHeight}" : $"{SelectedBlockType}";
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
                            TankTier.White => "A slow-moving, powerful tank that turns invisible\nat the start of the mission and can fire multiple bullets and lay mines..",
                            TankTier.Black => "A tank that moves faster than the player, fires rockets often, \nis aggressive, and can dodge well. Can lay mines.",
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
        }

        public static void Update()
        {
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
