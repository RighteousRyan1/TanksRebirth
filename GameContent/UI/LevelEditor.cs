using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TanksRebirth.Enums;
using TanksRebirth.GameContent.Properties;
using TanksRebirth.GameContent.Systems;
using TanksRebirth.Internals.Common.Framework.Audio;
using TanksRebirth.Internals.Common.GameUI;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Internals.UI;

namespace TanksRebirth.GameContent.UI
{
    public static class LevelEditor
    {
        public static bool Active { get; private set; }
        public static OggMusic Theme = new("Level Editor Theme", "Content/Assets/mainmenu/editor", 0.7f);

        public static UITextButton TestLevel;
        public static UITextButton Perspective;
        public static UITextButton BlocksCategory;
        public static UITextButton TanksCategory;

        public static UITextButton ReturnToEditor;

        private static Category _category;

        private static TankTier SelectedTankTier;
        public static Block.BlockType SelectedBlockType;
        public static bool Editing { get; internal set; }

        private enum Category
        {
            Tanks,
            Blocks
        }
        private static Mission _cachedMission;
        public static void Initialize()
        {
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

            BlocksCategory = new("Blocks", TankGame.TextFont, Color.White);
            BlocksCategory.SetDimensions(() =>
            {
                return new(GameUtils.WindowWidth * 0.75f, GameUtils.WindowHeight * 0.725f);
            }, () =>
            {
                return new(200, 50);
            });
            BlocksCategory.OnLeftClick = (l) => {
                _category = Category.Blocks;
            };

            TanksCategory = new("Tanks", TankGame.TextFont, Color.White);
            TanksCategory.SetDimensions(() =>
            {
                return new(GameUtils.WindowWidth * 0.875f, GameUtils.WindowHeight * 0.725f);
            }, () =>
            {
                return new(200, 50);
            });
            TanksCategory.OnLeftClick = (l) => {
                _category = Category.Tanks;
            };

            TanksCategory.IsVisible = false;
            BlocksCategory.IsVisible = false;
            TestLevel.IsVisible = false;
            Perspective.IsVisible = false;
            ReturnToEditor.IsVisible = false;

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
                    TanksCategory.IsVisible = true;
                    BlocksCategory.IsVisible = true;
                    TestLevel.IsVisible = true;
                    Perspective.IsVisible = true;
                });
            }
            else
            {
                Theme.Play();
                Active = true;
                TanksCategory.IsVisible = true;
                BlocksCategory.IsVisible = true;
                TestLevel.IsVisible = true;
                Perspective.IsVisible = true;
            }
            Editing = true;
        }
        public static void Close()
        {
            Active = false;
            Theme.SetVolume(0);
            Theme.Stop();
            TanksCategory.IsVisible = false;
            BlocksCategory.IsVisible = false;
            TestLevel.IsVisible = false;
            Perspective.IsVisible = false;
        }

        public static void Render()
        {
            TankGame.SpriteRenderer.Draw(TankGame.WhitePixel, new Vector2(0, GameUtils.WindowBottom.Y * 0.8f), null, Color.White, 0f, Vector2.Zero, new Vector2(GameUtils.WindowWidth, GameUtils.WindowHeight * 0.2f), default, 0f);
        }

        public static void Update()
        {
            switch (_category) {
                case Category.Tanks:
                    TanksCategory.Color = Color.DeepSkyBlue;
                    BlocksCategory.Color = Color.White;
                    break;
                case Category.Blocks:
                    TanksCategory.Color = Color.White;
                    BlocksCategory.Color = Color.DeepSkyBlue;
                    break;
            }
            if (Active)
                Theme.SetVolume(0.4f * TankGame.Settings.MusicVolume);
            else if (Editing && !Active && _cachedMission != default && GameProperties.InMission)
                if (GameHandler.NothingCanHappenAnymore(_cachedMission, out bool victory))
                    ReturnToEditor?.OnLeftClick?.Invoke(null);
            if (ReturnToEditor != null)
                ReturnToEditor.IsVisible = Editing && !Active && !MainMenu.Active;
        }
    }
}
