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
using TanksRebirth.GameContent.ID;
using TanksRebirth.Internals.Common.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace TanksRebirth.GameContent.UI
{
    public static class LevelEditor
    {
        public static readonly byte[] LevelFileHeader = { 84, 65, 78, 75 };
        public const int LevelEditorVersion = 2;

        // TODO: allow the moving of missions up and down in the level editor order -- done... i think.

        public static bool Active { get; private set; }
        public static OggMusic Theme = new("Level Editor Theme", "Content/Assets/mainmenu/editor", 0.7f);

        public static UITextButton TestLevel;
        public static UITextButton Perspective;

        public static UITextButton BlocksCategory;
        public static UITextButton EnemyTanksCategory;
        public static UITextButton PlayerTanksCategory;

        public static UITextButton Properties;
        public static UITextButton LoadLevel;

        public static UITextButton ReturnToEditor;

        #region ConfirmLevelContents
        // finish confirmation stuff later.
        public static Rectangle LevelContentsPanel;
        public static UITextInput MissionName;
        public static UITextInput CampaignName;
        public static UITextInput CampaignDescription;
        public static UITextInput CampaignAuthor;
        public static UITextInput CampaignVersion;
        public static UITextInput CampaignTags;
        public static UITextInput CampaignExtraLives;
        public static UITextInput CampaignLoadingStripColor;
        public static UITextInput CampaignLoadingBGColor;
        public static UITextButton SaveMenuReturn;
        public static UITextButton SaveLevelConfirm;
        public static UITextButton CampaignMajorVictory;

        public static UITextButton SwapMenu;
        #endregion
        #region Variables / Properties
        public static Category CurCategory { get; private set; }
        public static int SelectedTankTier { get; private set; }
        public static int SelectedTankTeam { get; private set; }
        public static int SelectedPlayerType { get; private set; }
        public static int SelectedBlockType { get; private set; }
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

        private static bool _initialized;

        private static Campaign _loadedCampaign;

        private static bool _viewMissionDetails = true;
        private static bool _hasMajorVictory;

        private static readonly List<UITextInput> _campaignElems = new();

        public static Color SelectedColor = Color.SkyBlue;
        public static Color UnselectedColor = Color.White;

        // reduce hardcode -- make a variable that tracks height.
        public static void InitializeSaveMenu()
        {
            LevelContentsPanel = new Rectangle(WindowUtils.WindowWidth / 4, (int)(WindowUtils.WindowHeight * 0.1f), WindowUtils.WindowWidth / 2, (int)(WindowUtils.WindowHeight * 0.625f));

            MissionName = new(TankGame.TextFont, Color.White, 1f, 20);

            MissionName.SetDimensions(() => new(LevelContentsPanel.X + 20.ToResolutionX(),
                LevelContentsPanel.Y + 60.ToResolutionY()),
                () => new(LevelContentsPanel.Width - 40.ToResolutionX(),
                50.ToResolutionY()));
            MissionName.DefaultString = "Name";

            SaveMenuReturn = new("Return", TankGame.TextFont, Color.White);
            SaveMenuReturn.SetDimensions(() => new Vector2(LevelContentsPanel.X + 20.ToResolutionX(),
                LevelContentsPanel.Y + LevelContentsPanel.Height - 60.ToResolutionY()),
                () => new(200.ToResolutionX(),
                50.ToResolutionY()));

            SaveMenuReturn.OnLeftClick = (l) => {
                GUICategory = UICategory.LevelEditor;
                if (MissionName.GetRealText() != string.Empty)
                    _loadedCampaign.CachedMissions[_loadedCampaign.CurrentMissionId].Name = MissionName.GetRealText();
                SetupMissionsBar(_loadedCampaign, false);
            };

            SaveLevelConfirm = new("Save", TankGame.TextFont, Color.White);
            SaveLevelConfirm.SetDimensions(() => new Vector2(LevelContentsPanel.X + 40.ToResolutionX() + SaveMenuReturn.Size.X,
                LevelContentsPanel.Y + LevelContentsPanel.Height - 60.ToResolutionY()),
                () => new(200.ToResolutionX(),
                50.ToResolutionY()));
            SaveLevelConfirm.OnLeftClick = (l) => {
                // Mission.Save(LevelName.GetRealText(), )
                var res = Dialog.FileSave(_viewMissionDetails ? "mission" : "campaign", TankGame.SaveDirectory);
                if (res.Path != null && res.IsOk)
                {
                    try {
                        var name = _viewMissionDetails ? MissionName.Text : CampaignName.Text;
                        var realName = Path.HasExtension(res.Path) ? Path.GetFileNameWithoutExtension(res.Path) : Path.GetFileName(res.Path);
                        var path = res.Path.Replace(realName, string.Empty);

                        var misName = _loadedCampaign.CachedMissions[_loadedCampaign.CurrentMissionId].Name;
                        _loadedCampaign.CachedMissions[_loadedCampaign.CurrentMissionId] = Mission.GetCurrent(misName);
                        if (_viewMissionDetails)
                            Mission.Save(name, path, false, realName);
                        else
                        {
                            _loadedCampaign.MetaData.Name = CampaignName.GetRealText();
                            _loadedCampaign.MetaData.Description = CampaignDescription.GetRealText();
                            _loadedCampaign.MetaData.Author = CampaignAuthor.GetRealText();
                            var split = CampaignTags.GetRealText().Split(',');
                            _loadedCampaign.MetaData.Tags = split;
                            _loadedCampaign.MetaData.ExtraLivesMissions = ArrayUtils.SequenceToInt32Array(CampaignExtraLives.GetRealText());
                            _loadedCampaign.MetaData.Version = CampaignVersion.GetRealText();
                            _loadedCampaign.MetaData.BackgroundColor = UnpackedColor.FromStringFormat(CampaignLoadingBGColor.GetRealText());
                            _loadedCampaign.MetaData.MissionStripColor = UnpackedColor.FromStringFormat(CampaignLoadingStripColor.GetRealText());
                            _loadedCampaign.MetaData.HasMajorVictory = _hasMajorVictory;
                            Campaign.Save(Path.Combine(path, realName), _loadedCampaign);
                        }
                    }
                    catch {
                        // guh...
                    }
                }

                // GUICategory = UICategory.LevelEditor;
            };

            float width = 300;
            float height = 50;

            SwapMenu = new("Campaign Details", TankGame.TextFont, Color.White);
            SwapMenu.SetDimensions(() => new Vector2(LevelContentsPanel.X + LevelContentsPanel.Width - width.ToResolutionX() - 20.ToResolutionX(),
                LevelContentsPanel.Y + LevelContentsPanel.Height - height.ToResolutionY() - 10.ToResolutionY()),
                () => new(width.ToResolutionX(),
                height.ToResolutionY()));

            SwapMenu.OnLeftClick = (l) => {
                _viewMissionDetails = !_viewMissionDetails;
                if (MissionName.GetRealText() != string.Empty)
                    _loadedCampaign.CachedMissions[_loadedCampaign.CurrentMissionId].Name = MissionName.GetRealText();
            };

            CampaignName = new(TankGame.TextFont, Color.White, 1f, 30);
            CampaignName.SetDimensions(() => new Vector2(LevelContentsPanel.X + 20.ToResolutionX(),
                LevelContentsPanel.Y + 60.ToResolutionY()),
                () => new(LevelContentsPanel.Width - 40.ToResolutionX(),
                50.ToResolutionY()));
            CampaignName.DefaultString = "Campaign Name";
            CampaignName.Tooltip = "The campaign this mission belongs to.";

            CampaignDescription = new(TankGame.TextFont, Color.White, 1f, 100);
            CampaignDescription.SetDimensions(() => new Vector2(LevelContentsPanel.X + 20.ToResolutionX(), LevelContentsPanel.Y + 120.ToResolutionY()), () => new(LevelContentsPanel.Width - 40.ToResolutionX(), 50.ToResolutionY()));
            CampaignDescription.DefaultString = "Description";
            CampaignDescription.Tooltip = "Sum up this campaign into your own words.";

            CampaignAuthor = new(TankGame.TextFont, Color.White, 1f, 25);
            CampaignAuthor.SetDimensions(() => new Vector2(LevelContentsPanel.X + 20.ToResolutionX(), LevelContentsPanel.Y + 180.ToResolutionY()), () => new(LevelContentsPanel.Width - 40.ToResolutionX(), 50.ToResolutionY()));
            CampaignAuthor.DefaultString = "Author";
            CampaignAuthor.Tooltip = "You! The one who created this campaign. Sign it!";

            CampaignTags = new(TankGame.TextFont, Color.White, 1f, 35);
            CampaignTags.SetDimensions(() => new Vector2(LevelContentsPanel.X + 20.ToResolutionX(), LevelContentsPanel.Y + 240.ToResolutionY()), () => new(LevelContentsPanel.Width - 40.ToResolutionX(), 50.ToResolutionY()));
            CampaignTags.DefaultString = "Tags (split with ',')";
            CampaignTags.Tooltip = "The tags that describe this campaign well. Be sure to split with ','";

            CampaignExtraLives = new(TankGame.TextFont, Color.White, 1f, 100);
            CampaignExtraLives.SetDimensions(() => new Vector2(LevelContentsPanel.X + 20.ToResolutionX(), LevelContentsPanel.Y + 300.ToResolutionY()), () => new(LevelContentsPanel.Width - 40.ToResolutionX(), 50.ToResolutionY()));
            CampaignExtraLives.DefaultString = "Extra Life Missions (split with ',')";
            CampaignExtraLives.Tooltip = "The level indexes of missions to provide extra lives. Be sure to split with ','";

            CampaignVersion = new(TankGame.TextFont, Color.White, 1f, 10);
            CampaignVersion.SetDimensions(() => new Vector2(LevelContentsPanel.X + 20.ToResolutionX(), LevelContentsPanel.Y + 360.ToResolutionY()), () => new(LevelContentsPanel.Width - 40.ToResolutionX(), 50.ToResolutionY()));
            CampaignVersion.DefaultString = "Level Version";
            CampaignVersion.Tooltip = "The version of THIS CAMPAIGN, not the game.";

            CampaignLoadingBGColor = new(TankGame.TextFont, Color.White, 1f, 11);
            CampaignLoadingBGColor.SetDimensions(() => new Vector2(LevelContentsPanel.X + 20.ToResolutionX(), LevelContentsPanel.Y + 420.ToResolutionY()), () => new(LevelContentsPanel.Width - 40.ToResolutionX(), 50.ToResolutionY()));
            CampaignLoadingBGColor.DefaultString = "Mission Loading: BG Color";
            CampaignLoadingBGColor.Tooltip = "Changes the color of the background when transitioning missions.\nBe sure there are no spaces between commas.";

            CampaignLoadingStripColor = new(TankGame.TextFont, Color.White, 1f, 11);
            CampaignLoadingStripColor.SetDimensions(() => new Vector2(LevelContentsPanel.X + 20.ToResolutionX(), LevelContentsPanel.Y + 480.ToResolutionY()), () => new(LevelContentsPanel.Width - 40.ToResolutionX(), 50.ToResolutionY()));
            CampaignLoadingStripColor.DefaultString = "Mission Loading: Strip Color";
            CampaignLoadingStripColor.Tooltip = "Changes the color of the strip that appears across\n the screen when transitioning missions.\nBe sure there are no spaces between commas.";

            CampaignMajorVictory = new("", TankGame.TextFont, Color.White, () => Vector2.One.ToResolution());
            CampaignMajorVictory.SetDimensions(() => new Vector2(LevelContentsPanel.X + 20.ToResolutionX(), LevelContentsPanel.Y + 540.ToResolutionY()), () => new(LevelContentsPanel.Width - 40.ToResolutionX(), 50.ToResolutionY()));
            CampaignMajorVictory.OnLeftClick = (a) => _hasMajorVictory = !_hasMajorVictory;
            CampaignMajorVictory.Tooltip = "If yes, at the end of this campaign it will play a\ndifferent, more accomplishing soundtrack at the end.";
            SetSaveMenuVisibility(false);

            _campaignElems.Add(MissionName);
            _campaignElems.Add(CampaignName);
            _campaignElems.Add(CampaignVersion);
            _campaignElems.Add(CampaignLoadingBGColor);
            _campaignElems.Add(CampaignLoadingStripColor);
            _campaignElems.Add(CampaignTags);
            _campaignElems.Add(CampaignAuthor);
            _campaignElems.Add(CampaignExtraLives);
            _campaignElems.Add(CampaignDescription);
        }
        public enum UICategory {
            LevelEditor,
            SavingThings,
        }

        private static bool _sdbui;
        public static bool ShouldDrawBarUI {
            get => _sdbui;
            set {
                if (GUICategory == UICategory.SavingThings)
                    SetSaveMenuVisibility(value);
                SetBarUIVisibility(value);
                _sdbui = value;
            }
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

        private static List<UITextButton> _missionButtons = new();
        private static Rectangle _missionTab = new(0, 150, 350, 535);
        private static Rectangle _missionButtonScissor;
        private static float _missionsOffset;
        private static float _missionsMaxOff;

        private static bool _saveMenuOpen;

        /// <summary>
        /// Moves the currently loaded mission on the loaded levels tab. Will throw a <see cref="IndexOutOfRangeException"/> if the mission is too high or low.
        /// </summary>
        /// <param name="up">Whether to move it up (a mission BACK) or down (a mission FORWARD)</param>
        private static void MoveMission(bool up)
        {
            if (up)
            {
                if (_loadedCampaign.CurrentMissionId == 0)
                {
                    SoundPlayer.SoundError();
                    ChatSystem.SendMessage("No mission above this one!", Color.Red);
                    return;
                }
            }
            else
            {
                var count = _loadedCampaign.CachedMissions.Count(x => x != default);
                if (_loadedCampaign.CurrentMissionId >= count - 1)
                {
                    SoundPlayer.SoundError();
                    ChatSystem.SendMessage("No mission below this one!", Color.Red);
                    return;
                }
            }


            var thisMission = _loadedCampaign.CurrentMission;
            var targetMission = _loadedCampaign.CachedMissions[_loadedCampaign.CurrentMissionId + (up ? -1 : 1)];

            // CHECKME: works?
            _loadedCampaign.CachedMissions[_loadedCampaign.CurrentMissionId] = targetMission;
            _loadedCampaign.CachedMissions[_loadedCampaign.CurrentMissionId + (up ? -1 : 1)] = thisMission;

            _loadedCampaign.LoadMission(_loadedCampaign.CurrentMissionId + (up ? -1 : 1));

            // _campaignElems.First(x => x.Text == _loadedCampaign.CurrentMission.Name).Color = SelectedColor;

            SetupMissionsBar(_loadedCampaign);

            _missionButtons[_loadedCampaign.CurrentMissionId].Color = SelectedColor;
        }
        private static void SetBarUIVisibility(bool visible)
        {
            PlayerTanksCategory.IsVisible =
                EnemyTanksCategory.IsVisible =
                BlocksCategory.IsVisible =
                Perspective.IsVisible =
                Properties.IsVisible = 
                LoadLevel.IsVisible = 
                TestLevel.IsVisible = visible;
            _missionButtons.ForEach(x => x.IsVisible = visible);
        }
        private static void SetSaveMenuVisibility(bool visible)
        {
            _saveMenuOpen = visible;
            MissionName.IsVisible = visible && _viewMissionDetails;
            SaveLevelConfirm.IsVisible = visible;
            SaveMenuReturn.IsVisible = visible;
            SwapMenu.IsVisible = visible;
            SaveMenuReturn.IsVisible = visible;

            CampaignName.IsVisible = visible && !_viewMissionDetails;
            CampaignDescription.IsVisible = visible && !_viewMissionDetails;
            CampaignAuthor.IsVisible = visible && !_viewMissionDetails;
            CampaignTags.IsVisible = visible && !_viewMissionDetails;
            CampaignVersion.IsVisible = visible && !_viewMissionDetails;
            CampaignExtraLives.IsVisible = visible && !_viewMissionDetails;
            CampaignLoadingBGColor.IsVisible = visible && !_viewMissionDetails;
            CampaignLoadingStripColor.IsVisible = visible && !_viewMissionDetails;
            CampaignMajorVictory.IsVisible = visible && !_viewMissionDetails;
        }
        private static void SetLevelEditorVisibility(bool visible)
        {
            EnemyTanksCategory.IsVisible = visible;
            BlocksCategory.IsVisible = visible;
            TestLevel.IsVisible = visible;
            Perspective.IsVisible = visible;
            PlayerTanksCategory.IsVisible = visible;
            Properties.IsVisible = visible;
            LoadLevel.IsVisible = visible;
        }
        public static void Initialize()
        {
            if (_initialized)
                return;
            _initialized = true;

            #region Enumerable Init
            foreach (var file in Directory.GetFiles(Path.Combine("Content", "Assets", "textures", "ui", "leveledit")))
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                var assetName = file;
                RenderTextures.Add(fileName, GameResources.GetGameResource<Texture2D>(assetName, false, false));
            }
            var names = TankID.Collection.Keys;
            for (int i = 0; i < names.Length; i++) {
                var nTL = names[i];

                if (RenderTextures.ContainsKey(nTL))
                    _renderNamesTanks.Add(nTL);
            }
            names = BlockID.Collection.Keys;
            for (int i = 0; i < names.Length; i++) {
                var nTL = names[i];

                if (RenderTextures.ContainsKey(nTL))
                    _renderNamesBlocks.Add(nTL);
            }
            names = PlayerID.Collection.Keys;
            for (int i = 0; i < names.Length; i++) {
                var nTL = names[i];

                if (RenderTextures.ContainsKey(nTL))
                    _renderNamesPlayers.Add(nTL);
            }
            #endregion

            TestLevel = new("Test Level", TankGame.TextFont, Color.White);
            TestLevel.SetDimensions(() => new(WindowUtils.WindowWidth * 0.01f, WindowUtils.WindowHeight * 0.725f), () => new Vector2(200, 50).ToResolution());

            TestLevel.OnLeftClick = (l) => {
                Close(false);
                TankGame.OverheadView = false;

                var name = _loadedCampaign.CachedMissions[_loadedCampaign.CurrentMissionId].Name;
                _cachedMission = Mission.GetCurrent(name);
            };

            ReturnToEditor = new("Return to Editor", TankGame.TextFont, Color.White);
            ReturnToEditor.SetDimensions(() => new(WindowUtils.WindowWidth * 0.01f, WindowUtils.WindowHeight * 0.02f), () => new Vector2(250, 50).ToResolution());

            ReturnToEditor.OnLeftClick = (l) => {
                Open(false);
                TankGame.OverheadView = true;
                GameProperties.InMission = false;
                GameHandler.CleanupScene();
                Mission.LoadDirectly(_cachedMission);
                SetupMissionsBar(_loadedCampaign);
            };

            Perspective = new("Perspective", TankGame.TextFont, Color.White);
            Perspective.SetDimensions(() => new(WindowUtils.WindowWidth * 0.125f, WindowUtils.WindowHeight * 0.725f), () => new Vector2(200, 50).ToResolution());
            Perspective.Tooltip = "View your mission at the perspective of the player.";
            Perspective.OnLeftClick = (l) => {
                TankGame.OverheadView = !TankGame.OverheadView;
            };

            BlocksCategory = new("Blocks", TankGame.TextFont, Color.White);
            BlocksCategory.SetDimensions(() => new(WindowUtils.WindowWidth * 0.75f, WindowUtils.WindowHeight * 0.725f), () => new Vector2(200, 50).ToResolution());
            BlocksCategory.OnLeftClick = (l) => {
                CurCategory = Category.Blocks;
            };

            EnemyTanksCategory = new("Enemies", TankGame.TextFont, Color.White);
            EnemyTanksCategory.SetDimensions(() => new(WindowUtils.WindowWidth * 0.875f, WindowUtils.WindowHeight * 0.725f), () => new Vector2(200, 50).ToResolution());
            EnemyTanksCategory.OnLeftClick = (l) => {
                CurCategory = Category.EnemyTanks;
            };
            PlayerTanksCategory = new("Players", TankGame.TextFont, Color.White);
            PlayerTanksCategory.SetDimensions(() => new(WindowUtils.WindowWidth * 0.875f, WindowUtils.WindowHeight * 0.65f), () => new Vector2(200, 50).ToResolution());
            PlayerTanksCategory.OnLeftClick = (l) => {
                CurCategory = Category.PlayerTanks;
            };

            Properties = new("Properties", TankGame.TextFont, Color.White);

            float width = 200;

            Properties.SetDimensions(() => new(WindowUtils.WindowWidth * 0.425f - (width / 2).ToResolutionX(), 10.ToResolutionY()), () => new Vector2(width, 50).ToResolution());
            Properties.OnLeftClick = (a) => {
                if (!_saveMenuOpen)
                    GUICategory = UICategory.SavingThings;
                else
                    GUICategory = UICategory.LevelEditor;
            };

            LoadLevel = new("Load", TankGame.TextFont, Color.White);

            LoadLevel.SetDimensions(() => new(WindowUtils.WindowWidth * 0.575f - (width / 2).ToResolutionX(), 10.ToResolutionY()), () => new Vector2(width, 50).ToResolution());
            LoadLevel.OnLeftClick = (a) => {
                var res = Dialog.FileOpen("mission,campaign,bin", TankGame.SaveDirectory);
                if (res.Path != null && res.IsOk)
                {
                    try {
                        var ext = Path.GetExtension(res.Path);

                        if (ext == ".mission") {
                            //GameProperties.LoadedCampaign.LoadMission(Mission.Load(res.Path, null));
                            //GameProperties.LoadedCampaign.SetupLoadedMission(true);
                            Mission.LoadDirectly(Mission.Load(res.Path, null));
                            //_loadedCampaign = null;
                        }
                        else if (ext == ".campaign") {
                            _loadedCampaign = Campaign.Load(res.Path);
                            _loadedCampaign.LoadMission(0);
                            _loadedCampaign.SetupLoadedMission(true);
                            MissionName.Text = _loadedCampaign.CachedMissions[0].Name;
                            SetupMissionsBar(_loadedCampaign);
                        }
                        else if (ext == ".bin")
                        {
                            var map = new WiiMap(res.Path);
                            ChatSystem.SendMessage($"(Width, Height): ({map.Width}, {map.Height})", Color.White);
                            ChatSystem.SendMessage($"(QVal, PValue): ({map.QValue}, {map.PValue})", Color.White);

                            WiiMap.ApplyToGameWorld(map);
                        }

                        ChatSystem.SendMessage($"Loaded '{Path.GetFileName(res.Path)}'.", Color.White);
                    } catch (Exception e) {
                        ChatSystem.SendMessage("Failed to load.", Color.Red);
                        ChatSystem.SendMessage(e.Message, Color.Red);
                    }
                }
                return;
            };
            // TODO: non-windows support. i am lazy. fuck this.
            LoadLevel.Tooltip = "Will open a file dialog for\nyou to choose what mission/campaign to load.";

            InitializeSaveMenu();

            SetLevelEditorVisibility(false);

            UIElement.CunoSucks();
        }
        public static void SetupMissionsBar(Campaign campaign, bool setCampaignData = true)
        {
            RemoveMissionButtons();

            // TODO: scissor, etc
            // offset
            // campaign metadata editing
            // go to bed ryan

            if (setCampaignData)
            {
                CampaignName.Text = campaign.MetaData.Name;
                CampaignDescription.Text = campaign.MetaData.Description;
                CampaignAuthor.Text = campaign.MetaData.Author;
                CampaignTags.Text = string.Join(',', campaign.MetaData.Tags);
                CampaignVersion.Text = campaign.MetaData.Version;
                CampaignExtraLives.Text = string.Join(',', campaign.MetaData.ExtraLivesMissions);
                CampaignVersion.Text = campaign.MetaData.Version;
                CampaignLoadingBGColor.Text = campaign.MetaData.BackgroundColor.ToString();
                CampaignLoadingStripColor.Text = campaign.MetaData.MissionStripColor.ToString();
                _hasMajorVictory = campaign.MetaData.HasMajorVictory;
            }

            float totalOff = 0;

            if (_loadedCampaign != null) {
                for (int i = 0; i < campaign.CachedMissions.Length; i++) {
                    var mission = campaign.CachedMissions[i];
                    if (mission == default)
                        break;
                    var btn = new UITextButton(mission.Name, TankGame.TextFont, Color.White, () => Vector2.One.ToResolution());
                    btn.SetDimensions(() => new Vector2(_missionButtonScissor.X + 15.ToResolutionX(), _missionButtonScissor.Y + _missionsOffset), () => new Vector2(_missionButtonScissor.Width - 30.ToResolutionX(), 25.ToResolutionY()));

                    btn.Offset = new(0, i * 30);
                    totalOff += btn.Offset.Y;

                    btn.HasScissor = true;
                    btn.Scissor = () => _missionButtonScissor;

                    int index = i;

                    btn.OnLeftClick = (a) =>
                    {
                        _missionButtons.ForEach(x => x.Color = UnselectedColor);
                        btn.Color = SelectedColor;

                        var mission = _loadedCampaign.CachedMissions[_loadedCampaign.CurrentMissionId];

                        _loadedCampaign.CachedMissions[_loadedCampaign.CurrentMissionId] = Mission.GetCurrent(mission.Name);

                        _loadedCampaign.LoadMission(index);
                        _loadedCampaign.SetupLoadedMission(true);

                        MissionName.Text = _loadedCampaign.CachedMissions[index].Name;
                    };
                    _missionButtons.Add(btn);
                }
            }
            var addMission = new UITextButton("+", TankGame.TextFont, Color.White, () => Vector2.One.ToResolution()) {
                Tooltip = "Insert a blank mission after the currently selected mission."
            };
            addMission.SetDimensions(() => new Vector2(_missionButtonScissor.X + 15.ToResolutionX(), _missionButtonScissor.Y + _missionButtonScissor.Height + 5.ToResolutionY()), () => new Vector2(_missionButtonScissor.Width / 2, 25.ToResolutionY()));
            addMission.OnLeftClick = (a) =>
            {
                _missionButtons.ForEach(x => x.Color = Color.White);
                addMission.Color = Color.SkyBlue;

                // Array.Resize(ref _loadedCampaign.CachedMissions, _loadedCampaign.CachedMissions.Length + 1);
                var count = _loadedCampaign.CachedMissions.Count(c => c != default);
                var id = _loadedCampaign.CurrentMissionId;
                _loadedCampaign.CachedMissions[id] = Mission.GetCurrent(_loadedCampaign.CachedMissions[id].Name);

                // move every mission up by 1 in the array.
                for (int i = count; i > id + 1; i--) {
                    if (i + 1 >= _loadedCampaign.CachedMissions.Length)
                        Array.Resize(ref _loadedCampaign.CachedMissions, _loadedCampaign.CachedMissions.Length + 1);
                    _loadedCampaign.CachedMissions[i] = _loadedCampaign.CachedMissions[i - 1];
                }
                if (id + 1 >= _loadedCampaign.CachedMissions.Length)
                    Array.Resize(ref _loadedCampaign.CachedMissions, _loadedCampaign.CachedMissions.Length + 1);
                _loadedCampaign.CachedMissions[id + 1] = new(Array.Empty<TankTemplate>(), Array.Empty<BlockTemplate>());
                _loadedCampaign.LoadMission(id + 1);
                _loadedCampaign.SetupLoadedMission(true);

                MissionName.Text = _loadedCampaign.CachedMissions[id].Name;

                SetupMissionsBar(_loadedCampaign, false);

                _missionButtons[id + 1].Color = SelectedColor;
            };
            _missionButtons.Add(addMission);

            var moveMissionUp = new UITextButton("v", TankGame.TextFont, Color.White, () => Vector2.One.ToResolution()) {
                Tooltip = "Swap the currently selected mission with the one above it.",
                TextRotation = MathHelper.Pi
            };
            moveMissionUp.SetDimensions(() => new(addMission.Position.X + addMission.Size.X, addMission.Position.Y), () => new Vector2(_missionButtonScissor.Width / 5, addMission.Size.Y));
            moveMissionUp.OnLeftClick = (a) => {
                MoveMission(true);
            };
            _missionButtons.Add(moveMissionUp);

            var moveMissionDown = new UITextButton("v", TankGame.TextFont, Color.White, () => Vector2.One.ToResolution())
            {
                Tooltip = "Swap the currently selected mission with the one below it.",
            };
            moveMissionDown.SetDimensions(() => new(addMission.Position.X + addMission.Size.X + moveMissionUp.Size.X, addMission.Position.Y), () => new Vector2(moveMissionUp.Size.X, addMission.Size.Y));
            moveMissionDown.OnLeftClick = (a) => {
                MoveMission(false);
            };
            _missionButtons.Add(moveMissionDown);
            UIElement.CunoSucks();
        }
        private static void RemoveMissionButtons()
        {
            for (int i = 0; i < _missionButtons.Count; i++)
                _missionButtons[i].Remove();
            _missionButtons.Clear();
        }
        public static void Open(bool fromMainMenu = true)
        {
            if (fromMainMenu)
            {

                IntermissionSystem.TimeBlack = 180;
                GameProperties.ShouldMissionsProgress = false;
                Task.Run(async () =>
                {
                    while (IntermissionSystem.BlackAlpha > 0.8f || MainMenu.Active)
                        await Task.Delay(TankGame.LogicTime).ConfigureAwait(false);

                    Active = true;
                    TankGame.OverheadView = true;
                    Theme.Play();
                    SetLevelEditorVisibility(true);
                    _loadedCampaign = new();
                    _loadedCampaign.CachedMissions[0] = new(Array.Empty<TankTemplate>(), Array.Empty<BlockTemplate>()) {
                        Name = "No Name"
                    };
                    SetupMissionsBar(_loadedCampaign);
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
        public static void Close(bool toMainMenu)
        {
            Active = false;
            RemoveMissionButtons();

            Theme.SetVolume(0);
            Theme.Stop();
            SetLevelEditorVisibility(false);
            SetSaveMenuVisibility(false);
            // SetMissionsVisibility(false);
            if (toMainMenu)
                _loadedCampaign = null;
            PlacementSquare.ResetSquares();
        }

        private static Rectangle _clickRect;

        private static readonly Dictionary<Rectangle, (int, string)> ClickEventsPerItem = new(); // hover zone, id, description

        private static string _curDescription = string.Empty;
        private static Rectangle _curHoverRect;

        public static Color SelectionColor = Color.NavajoWhite;
        public static Color HoverBoxColor = Color.SkyBlue;

        public static void Render()
        {
            if (!_initialized)
                return;

            ShouldDrawBarUI = !GameUI.Paused;
            SwapMenu.Text = _viewMissionDetails ? "Campaign Details" : "Mission Details";

            if (!ShouldDrawBarUI)
                return;

            #region Main UI
            int xOff = 0;
            _clickRect = new(0, (int)(WindowUtils.WindowBottom.Y * 0.8f), WindowUtils.WindowWidth, (int)(WindowUtils.WindowHeight * 0.2f));
            TankGame.SpriteRenderer.Draw(TankGame.WhitePixel, _clickRect, null, Color.White, 0f, Vector2.Zero, default, 0f);

            var measure = TankGame.TextFont.MeasureString(_curDescription);

            if (_curDescription != null && _curDescription != string.Empty)
            {
                int padding = 20;
                var orig = new Vector2(0, TankGame.WhitePixel.Size().Y);
                TankGame.SpriteRenderer.Draw(TankGame.WhitePixel,
                    new Rectangle((int)(WindowUtils.WindowWidth / 2 - (measure.X / 2 + padding).ToResolutionX()), (int)(WindowUtils.WindowHeight * 0.8f), (int)(measure.X + padding * 2).ToResolutionX(), (int)(measure.Y + 20).ToResolutionY()), null, Color.White, 0f, orig, default, 0f);
            }

            // i feel like i could turn these panels into their own method.
            // but whatever.

            TankGame.SpriteRenderer.DrawString(TankGame.TextFont, _curDescription, new Vector2(WindowUtils.WindowWidth / 2, WindowUtils.WindowHeight * 0.78f), Color.Black, Vector2.One.ToResolution(), 0f, new Vector2(measure.X / 2, measure.Y));
            // level info
            TankGame.SpriteRenderer.Draw(TankGame.WhitePixel, new Rectangle(0, 0, 350, 125).ToResolution(), null, Color.Gray, 0f, Vector2.Zero, default, 0f);
            TankGame.SpriteRenderer.Draw(TankGame.WhitePixel, new Rectangle(0, 0, 350, 40).ToResolution(), null, Color.White, 0f, Vector2.Zero, default, 0f);
            TankGame.SpriteRenderer.DrawString(TankGame.TextFont, "Level Information", new Vector2(175, 3).ToResolution(), Color.Black, Vector2.One.ToResolution(), 0f, GameUtils.GetAnchor(Anchor.TopCenter, TankGame.TextFont.MeasureString("Level Information")));
            TankGame.SpriteRenderer.DrawString(TankGame.TextFont, $"Total Enemy Tanks: {AITank.CountAll()}", new Vector2(10, 40).ToResolution(), Color.White, Vector2.One.ToResolution(), 0f, Vector2.Zero);
            TankGame.SpriteRenderer.DrawString(TankGame.TextFont, $"Difficulty Rating: {DifficultyAlgorithm.GetDifficulty(Mission.GetCurrent()):0.00}", new Vector2(10, 80).ToResolution(), Color.White, Vector2.One.ToResolution(), 0f, Vector2.Zero);


            // render campaign missions ui 

            if (_loadedCampaign != null) {
                var heightDiff = 40;
                _missionButtonScissor = new Rectangle(_missionTab.X, _missionTab.Y + heightDiff, _missionTab.Width, _missionTab.Height - heightDiff * 2).ToResolution();
                TankGame.SpriteRenderer.Draw(TankGame.WhitePixel, _missionTab.ToResolution(), null, Color.Gray, 0f, Vector2.Zero, default, 0f);
                TankGame.SpriteRenderer.Draw(TankGame.WhitePixel, new Rectangle(_missionTab.X, _missionTab.Y, _missionTab.Width, heightDiff).ToResolution(), null, Color.White, 0f, Vector2.Zero, default, 0f);
                TankGame.SpriteRenderer.DrawString(TankGame.TextFont, "Mission List", new Vector2(175, 153).ToResolution(), Color.Black, Vector2.One.ToResolution(), 0f, GameUtils.GetAnchor(Anchor.TopCenter, TankGame.TextFont.MeasureString("Mission List")));
            }
            // render teams

            // placement information
            TankGame.SpriteRenderer.Draw(TankGame.WhitePixel, new Rectangle(WindowUtils.WindowWidth - (int)350.ToResolutionX(), 0, (int)350.ToResolutionX(), (int)500.ToResolutionY()), null, Color.Gray, 0f, Vector2.Zero, default, 0f);
            TankGame.SpriteRenderer.Draw(TankGame.WhitePixel, new Rectangle(WindowUtils.WindowWidth - (int)350.ToResolutionX(), 0, (int)350.ToResolutionX(), (int)40.ToResolutionY()), null, Color.White, 0f, Vector2.Zero, default, 0f);
            TankGame.SpriteRenderer.DrawString(TankGame.TextFont, "Placement Information", new Vector2(WindowUtils.WindowWidth - 325.ToResolutionX(), 3.ToResolutionY()), Color.Black, Vector2.One.ToResolution(), 0f, Vector2.Zero);
            if (CurCategory == Category.Blocks)
                TankGame.SpriteRenderer.DrawString(TankGame.TextFont, $"Block Stack: {BlockHeight}", new Vector2(WindowUtils.WindowWidth - 335.ToResolutionX(), 40.ToResolutionY()), Color.White, Vector2.One.ToResolution(), 0f, Vector2.Zero);
            var helpText = "";
            Vector2 start = new();
            if (CurCategory == Category.EnemyTanks || CurCategory == Category.PlayerTanks)
            {
                helpText = "UP and DOWN to change teams.";
                start = new(WindowUtils.WindowWidth - 250.ToResolutionX(), 140.ToResolutionY());
                // TODO: should be optimised. do later.
                TankGame.SpriteRenderer.DrawString(TankGame.TextFont, "Tank Teams", new Vector2(start.X + 45.ToResolutionX(), start.Y - 80.ToResolutionY()), Color.White, Vector2.One.ToResolution(), 0f, TankGame.TextFont.MeasureString("Tank Teams") / 2);

                TankGame.SpriteRenderer.Draw(TankGame.WhitePixel, new Rectangle((int)start.X, (int)(start.Y - 40.ToResolutionY()), (int)40.ToResolutionX(), (int)40.ToResolutionY()), null, Color.Black, 0f, Vector2.Zero, default, 0f);
                TankGame.SpriteRenderer.DrawString(TankGame.TextFont, "No Team", new Vector2(start.X + 45.ToResolutionX(), start.Y - 40.ToResolutionY()), Color.Black, Vector2.One.ToResolution(), 0f, Vector2.Zero);
                for (int i = 0; i < TeamID.Collection.Count - 1; i++)
                {
                    var color = TeamID.TeamColors[i + 1];
                    TankGame.SpriteRenderer.DrawString(TankGame.TextFont, TeamID.Collection.GetKey(i + 1), new Vector2(start.X + 45.ToResolutionX(), start.Y + (i * 40).ToResolutionY()), color, Vector2.One.ToResolution(), 0f, Vector2.Zero); ;
                    TankGame.SpriteRenderer.Draw(TankGame.WhitePixel, new Rectangle((int)start.X, (int)(start.Y + (i * 40).ToResolutionY()), (int)40.ToResolutionX(), (int)40.ToResolutionY()), null, color, 0f, Vector2.Zero, default, 0f);
                }
                TankGame.SpriteRenderer.DrawString(TankGame.TextFontLarge, ">", new Vector2(start.X - 25.ToResolutionX(), start.Y + ((int)(SelectedTankTeam - 1) * 40).ToResolutionY()), Color.White, Vector2.One.ToResolution(), 0f, TankGame.TextFontLarge.MeasureString(">") / 2);

                if (SelectedTankTeam != TeamID.Magenta)
                    TankGame.SpriteRenderer.DrawString(TankGame.TextFont, "v", new Vector2(start.X - 25.ToResolutionX(), start.Y + ((int)(SelectedTankTeam - 1) * 40 + 50).ToResolutionY()), Color.White, Vector2.One.ToResolution(), 0f, TankGame.TextFont.MeasureString("v") / 2);
                if (SelectedTankTeam != TeamID.NoTeam)
                    TankGame.SpriteRenderer.DrawString(TankGame.TextFont, "v", new Vector2(start.X - 25.ToResolutionX(), start.Y + ((int)(SelectedTankTeam - 1) * 40 - 10).ToResolutionY()), Color.White, Vector2.One.ToResolution(), MathHelper.Pi, TankGame.TextFont.MeasureString("v") / 2);
            }
            else if (CurCategory == Category.Blocks)
            {
                helpText = "UP and DOWN to change stack.";
                // TODO: add static dict for specific types?
                var tex = SelectedBlockType != BlockID.Hole ? $"{BlockID.Collection.GetKey(SelectedBlockType)}_{BlockHeight}" : $"{BlockID.Collection.GetKey(SelectedBlockType)}";
                var size = RenderTextures[tex].Size();
                start = new Vector2(WindowUtils.WindowWidth - 175.ToResolutionX(), 450.ToResolutionY());
                TankGame.SpriteRenderer.Draw(RenderTextures[tex], start, null, Color.White, 0f, new Vector2(size.X / 2, size.Y), Vector2.One.ToResolution(), default, 0f);
                // TODO: reduce the hardcode for modders, yeah
                if (SelectedBlockType != BlockID.Teleporter && SelectedBlockType != BlockID.Hole)
                {
                    TankGame.SpriteRenderer.DrawString(TankGame.TextFontLarge, "v", new Vector2(start.X + 100.ToResolutionX(), start.Y - 75.ToResolutionY()), Color.White, Vector2.One.ToResolution(), 0f, TankGame.TextFontLarge.MeasureString("v") / 2);
                    TankGame.SpriteRenderer.DrawString(TankGame.TextFontLarge, "v", new Vector2(start.X - 100.ToResolutionX(), start.Y - 25.ToResolutionY()), Color.White, Vector2.One.ToResolution(), MathHelper.Pi, TankGame.TextFontLarge.MeasureString("v") / 2);
                }
            }
            TankGame.SpriteRenderer.DrawString(TankGame.TextFont, helpText, new Vector2(WindowUtils.WindowWidth - 175.ToResolutionX(), WindowUtils.WindowHeight / 2 - 70.ToResolutionY()), Color.White, new Vector2(0.5f).ToResolution(), 0f, TankGame.TextFont.MeasureString(helpText) / 2);
            TankGame.SpriteRenderer.Draw(TankGame.WhitePixel, _curHoverRect, null, HoverBoxColor * 0.5f, 0f, Vector2.Zero, default, 0f);

            if (CurCategory == Category.EnemyTanks)
            {
                for (int i = 0; i < _renderNamesTanks.Count; i++)
                {
                    ClickEventsPerItem[new Rectangle((int)(34.ToResolutionX() + xOff + _barOffset), (int)(WindowUtils.WindowBottom.Y * 0.8f), (int)234.ToResolutionX(), (int)(WindowUtils.WindowHeight * 0.2f))] =
                        (i + 2, (i + 2) switch
                        {
                            TankID.Brown => "An easy to defeat, stationary, slow firing enemy.",
                            TankID.Ash => "Similar to the Brown tank, but can move slowly and fire more often.",
                            TankID.Marine => "A slow-moving, passive and methodical enemy.\nShoots off rockets instead of standard shells.",
                            TankID.Yellow => "Not incredibly dangerous. Lays mines often and move fast.",
                            TankID.Pink => "A slow, but incredibly persistent and aggressive tank.\nCan fire multiple bullets at once.",
                            TankID.Green => "A highly-dangerous but stationary tank.\nShoots multiple rockets that can bounce twice.",
                            TankID.Violet => "A fast-moving, intelligent tank that can spray\nup to 5 bullets at once and can lay mines.",
                            TankID.White => "A slow-moving, powerful tank that turns invisible\nat the start of the mission and can fire multiple bullets and lay mines.",
                            TankID.Black => "A tank that moves faster than the player, fires rockets often,\nis aggressive, and can dodge well. Can lay mines.",
                            TankID.Bronze => "A simple, stationary tank that can fire multiple bullets\nand aims directly at its target.",
                            TankID.Silver => "An advanced and difficult mobile tank that can fire up\nto 8 bullets at fast rates and can dodge well. Can lay mines",
                            TankID.Sapphire => "A deadly tank that can rapidly fire up to 3 rockets\nin a single volley. Can lay mines.",
                            TankID.Ruby => "A slow, but very aggressive tank that constantly fires shells.",
                            TankID.Citrine => "An insanely fast tank that shoots very fast shells at its target.\nLays mines frequently.",
                            TankID.Amethyst => "A fast, very agile, and dodgy tank that fires a spread\nof shells at its target. Can lay mines.",
                            TankID.Emerald => "A stationary tank that turns invisible at the start of the round.\nFires multiple double-bounce rockets at its target.",
                            TankID.Gold => "A slow and mobile tank that turns invisible at the start of the round\nand lays no tracks for players to see. Can lay mines.",
                            TankID.Obsidian => "A very fast, but very unintelligent tank that fires\nfast rockets frequently with 2 bounces. Can lay mines.",
                            TankID.Granite => "A very slow, mobile tank that becomes stunned\n for a while upon firing. Shoots faster-than-normal shells.",
                            TankID.Bubblegum => "A medium-speed, fast-firing dodgy tank that can lay mines.",
                            TankID.Water => "A medium-speed tank that moves linearly.\nFires rockets that bounce one time, and can also lay mines.",
                            TankID.Crimson => "A slow tank that fires in a burst of 5 shells. Can lay mines.",
                            TankID.Tiger => "A very wary tank that strafes very often and dodges very often.\nFires shells fast and lays mines often.",
                            TankID.Fade => "A tank that is mainly focused on dodging threats.\nFires often and can lay mines.",
                            TankID.Creeper => "A highly dangerous tank that fires very fast rockets\n that bounce 3 times. Moves very slowly.",
                            TankID.Gamma => "A stationary tank that fires insanely fast bullets at rapid frequency.",
                            TankID.Marble => "An apex predator tank that is good at dodging, good at\ncalculating shots, is fast, and fires fast rockets rapidly.",
                            _ => "What?"
                        }); // TODO: localize this. i hate english.

                    TankGame.SpriteRenderer.Draw(RenderTextures[_renderNamesTanks[i]], new Vector2(24.ToResolutionX() + xOff + _barOffset, WindowUtils.WindowBottom.Y * 0.75f), null, (int)SelectedTankTier - 2 == i ? SelectionColor : Color.White, 0f, Vector2.Zero, Vector2.One.ToResolution(), default, 0f);
                    xOff += (int)234.ToResolutionX();
                }
                _maxScroll = xOff;
            }
            else if (CurCategory == Category.Blocks)
            {
                for (int i = 0; i < _renderNamesBlocks.Count; i++)
                {
                    ClickEventsPerItem[new Rectangle((int)(34.ToResolutionX() + xOff + _barOffset), (int)(WindowUtils.WindowBottom.Y * 0.8f), (int)234.ToResolutionX(), (int)(WindowUtils.WindowHeight * 0.2f))] =
                        (i, i switch
                        {
                            BlockID.Wood => "An indestructible obstacle.",
                            BlockID.Cork => "An obstacle that can be destroyed by mines.",
                            BlockID.Hole => "An obstacle that tanks cannot travel through, but shells can.",
                            _ => "What?"
                        });

                    TankGame.SpriteRenderer.Draw(RenderTextures[_renderNamesBlocks[i]], new Vector2(24.ToResolutionX() + xOff + _barOffset, WindowUtils.WindowBottom.Y * 0.75f), null, (int)SelectedBlockType == i ? SelectionColor : Color.White, 0f, Vector2.Zero, Vector2.One.ToResolution(), default, 0f);
                    xOff += (int)234.ToResolutionX();
                }
                _maxScroll = xOff;
            }
            else if (CurCategory == Category.PlayerTanks)
            {
                for (int i = 0; i < _renderNamesPlayers.Count; i++)
                {
                    ClickEventsPerItem[new Rectangle((int)(34.ToResolutionX() + xOff + _barOffset), (int)(WindowUtils.WindowBottom.Y * 0.8f), (int)234.ToResolutionX(), (int)(WindowUtils.WindowHeight * 0.2f))] =
                        (i, i switch
                        {
                            PlayerID.Blue => "The blue player tank (P1)",
                            PlayerID.Red => "The red player tank. (P2)",
                            PlayerID.GreenPlr => "The green player tank (P3)",
                            PlayerID.YellowPlr => "The yellow player tank (P4)",
                            _ => "What?"
                        });

                    TankGame.SpriteRenderer.Draw(RenderTextures[_renderNamesPlayers[i]], new Vector2(24.ToResolutionX() + xOff + _barOffset, WindowUtils.WindowBottom.Y * 0.75f), null, (int)SelectedPlayerType == i ? SelectionColor : Color.White, 0f, Vector2.Zero, Vector2.One.ToResolution(), default, 0f);
                    xOff += (int)234.ToResolutionX();
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
                    var text = thing.Key.Contains(MouseUtils.MousePosition.ToPoint()) ? $"{thing.Key} ---- {thing.Value.Item1} (HOVERED)" : $"{thing.Key} ---- {thing.Value.Item1}";
                    DebugUtils.DrawDebugString(TankGame.SpriteRenderer, text, new Vector2(500, 20 + a), 3);
                    a += 20;
                }
            }
            #endregion
            // TankGame.SpriteRenderer.Draw(TankGame.WhitePixel, _missionButtonScissor, null, Color.Red * 0.5f, 0f, Vector2.Zero, default, 0f);

            if (Active && TankGame.HoveringAnyTank)
            {
                var tex = GameResources.GetGameResource<Texture2D>("Assets/textures/ui/leveledit/rotate");
                TankGame.SpriteRenderer.Draw(tex,
                   MouseUtils.MousePosition + new Vector2(20, -20).ToResolution(), null, Color.White, 0f, new Vector2(0, tex.Size().Y), 0.2f.ToResolution(), default, 0f);
            }
            if (Active && GUICategory == UICategory.SavingThings)
                TankGame.SpriteRenderer.Draw(TankGame.WhitePixel,
                   LevelContentsPanel, null, Color.Gray, 0f, Vector2.Zero, default, 0f);
            var txt = !_viewMissionDetails ? "Campaign Details" : "Mission Details";

            if (GUICategory == UICategory.SavingThings)
                TankGame.SpriteRenderer.DrawString(TankGame.TextFont, txt, new Vector2(LevelContentsPanel.X + LevelContentsPanel.Width / 2, LevelContentsPanel.Y + 10.ToResolutionY()), Color.White, Vector2.One.ToResolution(), 0f, new Vector2(TankGame.TextFont.MeasureString(txt).X / 2, 0));

            _campaignElems.ForEach(elem =>
            {
                if (elem is UITextInput)
                    elem.DrawText = false;
                elem.UniqueDraw = (a, b) =>
                {
                    if (!elem.IsVisible)
                        return;
                    // fix why this isn't drawing???
                    var pos = new Vector2(elem.Position.X + 10.ToResolutionX(), elem.Position.Y + elem.Size.Y / 2);
                    //var pos = Vector2.Zero;
                    string text = elem.DefaultString + ": " + elem.GetRealText();
                    var msr1 = TankGame.TextFontLarge.MeasureString(text);
                    var msr2 = TankGame.TextFontLarge.MeasureString(elem.DefaultString);
                    float constScale = 0.4f.ToResolutionX();
                    float scale = /*elem.DefaultString.Length < 30 ? 
                    msr2.X / msr1.X * 0.5f :
                    msr2.X / msr1.X * 0.3f;*/ msr1.X * constScale > elem.Size.X ? msr2.X / (msr1.X + msr2.X) : constScale;
                    b.DrawString(TankGame.TextFontLarge, text, pos, Color.Black, new Vector2(scale).ToResolution(), 0f, new Vector2(0, msr1.Y / 2));
                };
            });
        }

        public static void Update()
        {
            if (!_initialized)
                return;

            if (_missionButtonScissor.Contains(MouseUtils.MousePosition))
                _missionsOffset += InputUtils.GetScrollWheelChange() * 30;

            _missionsMaxOff = _missionButtons.Count * 30.ToResolutionY();
            SaveLevelConfirm.Tooltip = _viewMissionDetails ? "Save your mission directly to a file." : "Save your campaign directly to a file.\nIt will need to be in your Campaigns folder to be detected.";
            CampaignMajorVictory.Text = "Has Major Victory Theme: " + (_hasMajorVictory ? "Yes" : "No");

            if (_missionsOffset > 0)
                _missionsOffset = 0;
            else if (-_missionsMaxOff < -_missionButtonScissor.Height && _missionsOffset < -_missionsMaxOff + _missionButtonScissor.Height)
                _missionsOffset = -_missionsMaxOff + _missionButtonScissor.Height;

            LevelContentsPanel = new Rectangle(WindowUtils.WindowWidth / 4, (int)(WindowUtils.WindowHeight * 0.1f), WindowUtils.WindowWidth / 2, (int)(WindowUtils.WindowHeight * 0.625f));
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
                    if (thing.Key.Contains(MouseUtils.MousePosition.ToPoint()))
                    {
                        _curHoverRect = thing.Key;
                        if (thing.Value.Item2 != null)
                            _curDescription = thing.Value.Item2;
                    }
                }

                if (InputUtils.CanDetectClick())
                {
                    _origClick = MouseUtils.MousePosition - new Vector2(_barOffset, 0);

                    for (int i = 0; i < ClickEventsPerItem.Count; i++)
                    {
                        var evt = ClickEventsPerItem.ElementAt(i);
                        if (evt.Key.Contains(MouseUtils.MousePosition.ToPoint()))
                        {
                            if (CurCategory == Category.EnemyTanks)
                                SelectedTankTier = evt.Value.Item1;
                            else if (CurCategory == Category.Blocks)
                                SelectedBlockType = evt.Value.Item1;
                            else if (CurCategory == Category.PlayerTanks)
                                SelectedPlayerType = evt.Value.Item1;
                        }
                    }
                }
                if (InputUtils.MouseLeft && _clickRect.Contains(MouseUtils.MousePosition.ToPoint()))
                {
                    _barOffset = MouseUtils.MousePosition.X - _origClick.X;
                    if (_barOffset < -_maxScroll + WindowUtils.WindowWidth - 60.ToResolutionX())
                        _barOffset = -_maxScroll + WindowUtils.WindowWidth - 60.ToResolutionX();
                    if (_barOffset > 0)
                    {
                        _barOffset = 0;
                        _origClick = MouseUtils.MousePosition - new Vector2(_barOffset, 0);
                    }
                }

                BlockHeight = MathHelper.Clamp(BlockHeight, 1, 7);

                if (CurCategory == Category.EnemyTanks || CurCategory == Category.PlayerTanks)
                {
                    // tank place handling, etc
                    if (InputUtils.KeyJustPressed(Microsoft.Xna.Framework.Input.Keys.Up))
                        SelectedTankTeam--;
                    if (InputUtils.KeyJustPressed(Microsoft.Xna.Framework.Input.Keys.Down))
                        SelectedTankTeam++;
                    if (SelectedTankTeam > TeamID.Magenta)
                        SelectedTankTeam = TeamID.Magenta;
                    if (SelectedTankTeam < TeamID.NoTeam)
                        SelectedTankTeam = TeamID.NoTeam;
                }
                else if (CurCategory == Category.Blocks)
                {
                    if (InputUtils.KeyJustPressed(Microsoft.Xna.Framework.Input.Keys.Up))
                        BlockHeight++;
                    if (InputUtils.KeyJustPressed(Microsoft.Xna.Framework.Input.Keys.Down))
                        BlockHeight--;
                    if (SelectedBlockType == BlockID.Hole || SelectedBlockType == BlockID.Teleporter)
                        BlockHeight = 1;

                    BlockHeight = MathHelper.Clamp(BlockHeight, 1, 7);
                }
                ClickEventsPerItem.Clear();
            }
            else if (Editing && !Active && _cachedMission != default && GameProperties.InMission)
                if (GameHandler.NothingCanHappenAnymore(_cachedMission, out bool victory))
                    QueueEditorReEntry(120f);
            if (ReturnToEditor != null)
                ReturnToEditor.IsVisible = Editing && !Active && !MainMenu.Active;
        }

        private static float _waitTime;
        private static bool _isWaiting;
        private static void QueueEditorReEntry(float delay)
        {
            if (!_isWaiting)
                _waitTime = delay;
            _isWaiting = true;

            _waitTime -= TankGame.DeltaTime;
            if (_waitTime < 0)
            {
                ReturnToEditor?.OnLeftClick?.Invoke(null);
                _waitTime = 0;
                _isWaiting = false;
            }
        }
    }
}
