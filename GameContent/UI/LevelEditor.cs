using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
using TanksRebirth.Localization;
using Microsoft.Xna.Framework.Input;
using TanksRebirth.GameContent.RebirthUtils;

namespace TanksRebirth.GameContent.UI;

// TODO: bugs to fix
/* Rotation seemingly becomes negative upon level editor return
 * Horizontally facing tanks get reversed, PiOver2 becomes -PiOver2.
 */
public static class LevelEditor {
    public static readonly byte[] LevelFileHeader = { 84, 65, 78, 75 };
    public const int LevelEditorVersion = 2;

    public static string AlertText;
    private static float _alertTime;
    public static float DefaultAlertDuration { get; set; } = 120;

    /// <summary>Displays an alert to the screen.</summary>
    /// <param name="alert">The text to show in the alert.</param>
    /// <param name="timeOverride">The amount of time to display the alert for. Defaults to <see cref="DefaultAlertDuration"/>.</param>
    public static void Alert(string alert, float timeOverride = 0f) {
        _alertTime = timeOverride != 0f ? timeOverride : DefaultAlertDuration;
        AlertText = alert;
        SoundPlayer.SoundError();
    }

    // TODO: allow the moving of missions up and down in the level editor order -- done... i think.

    public static bool Active { get; private set; }
    public static OggMusic Theme = new("Level Editor Theme", "Content/Assets/mainmenu/editor.ogg", 0.7f);

    public static UITextButton TestLevel;
    public static UITextButton Perspective;

    public static UITextButton TerrainCategory;
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
        Terrain,
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

    internal static Campaign? loadedCampaign;

    private static bool _viewMissionDetails = true;
    private static bool _hasMajorVictory;

    private static readonly List<UITextInput> _campaignElems = new();

    public static Color SelectedColor = Color.SkyBlue;
    public static Color UnselectedColor = Color.White;

    private static Task? _loadTask;
    private static int _oldelta;

    public static void HandleLevelEditorModifications() {
        var cur = PlacementSquare.CurrentlyHovered;

        if (cur is not null && cur.HasItem && cur.HasBlock && cur.BlockId > -1 && cur.BlockId < Block.AllBlocks.Length) {
            if (Block.AllBlocks[cur.BlockId] != null) {
                if (Block.AllBlocks[cur.BlockId].Type == BlockID.Teleporter) {
                    // ChatSystem.SendMessage($"{Input.DeltaScrollWheel}", Color.White);

                    if (InputUtils.DeltaScrollWheel != _oldelta)
                        Block.AllBlocks[cur.BlockId].TpLink += (sbyte)(InputUtils.DeltaScrollWheel - _oldelta);
                }
            }
        }

        _oldelta = InputUtils.DeltaScrollWheel;
    }

    // reduce hardcode -- make a variable that tracks height.
    public static void InitializeSaveMenu() {
        LevelContentsPanel = new Rectangle(WindowUtils.WindowWidth / 4, (int)(WindowUtils.WindowHeight * 0.1f), WindowUtils.WindowWidth / 2, (int)(WindowUtils.WindowHeight * 0.625f));

        MissionName = new(TankGame.TextFont, Color.White, 1f, 30);

        MissionName.SetDimensions(() => new(LevelContentsPanel.X + 20.ToResolutionX(),
                LevelContentsPanel.Y + 60.ToResolutionY()),
            () => new(LevelContentsPanel.Width - 40.ToResolutionX(),
                50.ToResolutionY()));
        MissionName.DefaultString = TankGame.GameLanguage.Name;

        SaveMenuReturn = new(TankGame.GameLanguage.Return, TankGame.TextFont, Color.White);
        SaveMenuReturn.SetDimensions(() => new Vector2(LevelContentsPanel.X + 20.ToResolutionX(),
                LevelContentsPanel.Y + LevelContentsPanel.Height - 60.ToResolutionY()),
            () => new(200.ToResolutionX(),
                50.ToResolutionY()));

        SaveMenuReturn.OnLeftClick = (l) => {
            GUICategory = UICategory.LevelEditor;
            if (MissionName.GetRealText() != string.Empty)
                loadedCampaign.CachedMissions[loadedCampaign.CurrentMissionId].Name = MissionName.GetRealText();
            SetupMissionsBar(loadedCampaign, false);
        };

        SaveLevelConfirm = new(TankGame.GameLanguage.Save, TankGame.TextFont, Color.White);
        SaveLevelConfirm.SetDimensions(() => new Vector2(LevelContentsPanel.X + 40.ToResolutionX() + SaveMenuReturn.Size.X,
                LevelContentsPanel.Y + LevelContentsPanel.Height - 60.ToResolutionY()),
            () => new(200.ToResolutionX(),
                50.ToResolutionY()));
        SaveLevelConfirm.OnLeftClick = (l) => {
            // Mission.Save(LevelName.GetRealText(), )
            var res = Dialog.FileSave(_viewMissionDetails ? "mission,bin" : "campaign", TankGame.SaveDirectory);
            if (res.Path != null && res.IsOk) {
                try {
                    var name = _viewMissionDetails ? MissionName.Text : CampaignName.Text;
                    var realName = Path.HasExtension(res.Path) ? Path.GetFileNameWithoutExtension(res.Path) : Path.GetFileName(res.Path);
                    var path = res.Path.Replace(realName, string.Empty);

                    var misName = loadedCampaign.CachedMissions[loadedCampaign.CurrentMissionId].Name;
                    loadedCampaign.CachedMissions[loadedCampaign.CurrentMissionId] = Mission.GetCurrent(misName);
                    if (_viewMissionDetails) {
                        var ext = Path.GetExtension(res.Path);
                        if (ext == ".mission" || ext == string.Empty)
                            Mission.Save(res.Path, name);
                        else if (ext == ".bin")
                            WiiMap.SaveToTanksBinFile(res.Path, true);
                    }
                    else {
                        loadedCampaign.MetaData.Name = CampaignName.GetRealText();
                        loadedCampaign.MetaData.Description = CampaignDescription.GetRealText();
                        loadedCampaign.MetaData.Author = CampaignAuthor.GetRealText();
                        var split = CampaignTags.GetRealText().Split(',');
                        loadedCampaign.MetaData.Tags = split;
                        loadedCampaign.MetaData.ExtraLivesMissions = ArrayUtils.SequenceToInt32Array(CampaignExtraLives.GetRealText());
                        loadedCampaign.MetaData.Version = CampaignVersion.GetRealText();
                        loadedCampaign.MetaData.BackgroundColor = UnpackedColor.FromStringFormat(CampaignLoadingBGColor.GetRealText());
                        loadedCampaign.MetaData.MissionStripColor = UnpackedColor.FromStringFormat(CampaignLoadingStripColor.GetRealText());
                        loadedCampaign.MetaData.HasMajorVictory = _hasMajorVictory;
                        Campaign.Save(Path.Combine(path, realName), loadedCampaign);
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

        SwapMenu = new(TankGame.GameLanguage.CampaignDetails, TankGame.TextFont, Color.White);
        SwapMenu.SetDimensions(() => new Vector2(LevelContentsPanel.X + LevelContentsPanel.Width - width.ToResolutionX() - 20.ToResolutionX(),
                LevelContentsPanel.Y + LevelContentsPanel.Height - height.ToResolutionY() - 10.ToResolutionY()),
            () => new(width.ToResolutionX(),
                height.ToResolutionY()));

        SwapMenu.OnLeftClick = (l) => {
            _viewMissionDetails = !_viewMissionDetails;
            if (MissionName.GetRealText() != string.Empty)
                loadedCampaign.CachedMissions[loadedCampaign.CurrentMissionId].Name = MissionName.GetRealText();
        };
        CampaignName = new(TankGame.TextFont, Color.White, 1f, 30);
        CampaignName.SetDimensions(() => new Vector2(LevelContentsPanel.X + 20.ToResolutionX(),
                LevelContentsPanel.Y + 60.ToResolutionY()),
            () => new(LevelContentsPanel.Width - 40.ToResolutionX(),
                50.ToResolutionY()));
        CampaignName.DefaultString = TankGame.GameLanguage.Name;
        CampaignName.Tooltip = TankGame.GameLanguage.CampaignNameFlavor;

        CampaignDescription = new(TankGame.TextFont, Color.White, 1f, 100);
        CampaignDescription.SetDimensions(() => new Vector2(LevelContentsPanel.X + 20.ToResolutionX(), LevelContentsPanel.Y + 120.ToResolutionY()), () => new(LevelContentsPanel.Width - 40.ToResolutionX(), 50.ToResolutionY()));
        CampaignDescription.DefaultString = TankGame.GameLanguage.Description;
        CampaignDescription.Tooltip = TankGame.GameLanguage.DescriptionFlavor;

        CampaignAuthor = new(TankGame.TextFont, Color.White, 1f, 25);
        CampaignAuthor.SetDimensions(() => new Vector2(LevelContentsPanel.X + 20.ToResolutionX(), LevelContentsPanel.Y + 180.ToResolutionY()), () => new(LevelContentsPanel.Width - 40.ToResolutionX(), 50.ToResolutionY()));
        CampaignAuthor.DefaultString = TankGame.GameLanguage.Author;
        CampaignAuthor.Tooltip = TankGame.GameLanguage.AuthorFlavor;

        CampaignTags = new(TankGame.TextFont, Color.White, 1f, 35);
        CampaignTags.SetDimensions(() => new Vector2(LevelContentsPanel.X + 20.ToResolutionX(), LevelContentsPanel.Y + 240.ToResolutionY()), () => new(LevelContentsPanel.Width - 40.ToResolutionX(), 50.ToResolutionY()));
        CampaignTags.DefaultString = TankGame.GameLanguage.Tags;
        CampaignTags.Tooltip = TankGame.GameLanguage.TagsFlavor;

        CampaignExtraLives = new(TankGame.TextFont, Color.White, 1f, 100);
        CampaignExtraLives.SetDimensions(() => new Vector2(LevelContentsPanel.X + 20.ToResolutionX(), LevelContentsPanel.Y + 300.ToResolutionY()), () => new(LevelContentsPanel.Width - 40.ToResolutionX(), 50.ToResolutionY()));
        CampaignExtraLives.DefaultString = TankGame.GameLanguage.ExtraLifeMissions;
        CampaignExtraLives.Tooltip = TankGame.GameLanguage.ExtraLifeMissionsFlavor;

        CampaignVersion = new(TankGame.TextFont, Color.White, 1f, 10);
        CampaignVersion.SetDimensions(() => new Vector2(LevelContentsPanel.X + 20.ToResolutionX(), LevelContentsPanel.Y + 360.ToResolutionY()), () => new(LevelContentsPanel.Width - 40.ToResolutionX(), 50.ToResolutionY()));
        CampaignVersion.DefaultString = TankGame.GameLanguage.Version;
        CampaignVersion.Tooltip = TankGame.GameLanguage.VersionFlavor;

        CampaignLoadingBGColor = new(TankGame.TextFont, Color.White, 1f, 11);
        CampaignLoadingBGColor.SetDimensions(() => new Vector2(LevelContentsPanel.X + 20.ToResolutionX(), LevelContentsPanel.Y + 420.ToResolutionY()), () => new(LevelContentsPanel.Width - 40.ToResolutionX(), 50.ToResolutionY()));
        CampaignLoadingBGColor.DefaultString = TankGame.GameLanguage.BGColor;
        CampaignLoadingBGColor.Tooltip = TankGame.GameLanguage.BGColorFlavor;

        CampaignLoadingStripColor = new(TankGame.TextFont, Color.White, 1f, 11);
        CampaignLoadingStripColor.SetDimensions(() => new Vector2(LevelContentsPanel.X + 20.ToResolutionX(), LevelContentsPanel.Y + 480.ToResolutionY()), () => new(LevelContentsPanel.Width - 40.ToResolutionX(), 50.ToResolutionY()));
        CampaignLoadingStripColor.DefaultString = TankGame.GameLanguage.StripColor;
        CampaignLoadingStripColor.Tooltip = TankGame.GameLanguage.StripColorFlavor;

        CampaignMajorVictory = new("", TankGame.TextFont, Color.White, () => Vector2.One.ToResolution());
        CampaignMajorVictory.SetDimensions(() => new Vector2(LevelContentsPanel.X + 20.ToResolutionX(), LevelContentsPanel.Y + 540.ToResolutionY()), () => new(LevelContentsPanel.Width - 40.ToResolutionX(), 50.ToResolutionY()));
        CampaignMajorVictory.OnLeftClick = (a) => _hasMajorVictory = !_hasMajorVictory;
        CampaignMajorVictory.Tooltip = TankGame.GameLanguage.HasMajorVictoryThemeFlavor;
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

    internal static Mission missionToRate = new(Array.Empty<TankTemplate>(), Array.Empty<BlockTemplate>());

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

            if (_category == UICategory.SavingThings) {
                SetSaveMenuVisibility(true);
                SetLevelEditorVisibility(false);
            }
            else {
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

    public static List<string> TeamColorsLocalized = new();

    /// <summary>
    /// Moves the currently loaded mission on the loaded levels tab. Will throw a <see cref="IndexOutOfRangeException"/> if the mission is too high or low.
    /// </summary>
    /// <param name="up">Whether to move it up (a mission BACK) or down (a mission FORWARD)</param>
    private static void MoveMission(bool up) {
        if (up) {
            if (loadedCampaign.CurrentMissionId == 0) {
                SoundPlayer.SoundError();
                ChatSystem.SendMessage("No mission above this one!", Color.Red);
                return;
            }
        }
        else {
            var count = loadedCampaign.CachedMissions.Count(x => x != default);
            if (loadedCampaign.CurrentMissionId >= count - 1) {
                SoundPlayer.SoundError();
                ChatSystem.SendMessage("No mission below this one!", Color.Red);
                return;
            }
        }


        var thisMission = loadedCampaign.CurrentMission;
        var targetMission = loadedCampaign.CachedMissions[loadedCampaign.CurrentMissionId + (up ? -1 : 1)];

        // CHECKME: works?
        loadedCampaign.CachedMissions[loadedCampaign.CurrentMissionId] = targetMission;
        loadedCampaign.CachedMissions[loadedCampaign.CurrentMissionId + (up ? -1 : 1)] = thisMission;

        loadedCampaign.LoadMission(loadedCampaign.CurrentMissionId + (up ? -1 : 1));

        // _campaignElems.First(x => x.Text == _loadedCampaign.CurrentMission.Name).Color = SelectedColor;

        SetupMissionsBar(loadedCampaign);

        _missionButtons[loadedCampaign.CurrentMissionId].Color = SelectedColor;
    }

    private static void SetBarUIVisibility(bool visible) {
        PlayerTanksCategory.IsVisible =
            EnemyTanksCategory.IsVisible =
                TerrainCategory.IsVisible =
                    Perspective.IsVisible =
                        Properties.IsVisible =
                            LoadLevel.IsVisible =
                                TestLevel.IsVisible = visible;
        _missionButtons.ForEach(x => x.IsVisible = visible);
    }

    private static void SetSaveMenuVisibility(bool visible) {
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

    private static void SetLevelEditorVisibility(bool visible) {
        EnemyTanksCategory.IsVisible = visible;
        TerrainCategory.IsVisible = visible;
        TestLevel.IsVisible = visible;
        Perspective.IsVisible = visible;
        PlayerTanksCategory.IsVisible = visible;
        Properties.IsVisible = visible;
        LoadLevel.IsVisible = visible;
    }

    public static void Initialize() {
        if (_initialized) {
            foreach (var field in typeof(LevelEditor).GetFields()) {
                if (field.GetValue(null) is UIElement) {
                    ((UIElement)field.GetValue(null)).Remove();
                    field.SetValue(null, null);
                }
            }
        }
        TeamColorsLocalized.Clear();
        for (int i = 0; i < TeamID.Collection.Count; i++) {
            var name = TeamID.Collection.GetKey(i);
            TeamColorsLocalized.Add((string)typeof(Language).GetProperty(name).GetValue(TankGame.GameLanguage));
        }

        #region Enumerable Init

        if (!_initialized) {

            foreach (var file in Directory.GetFiles(Path.Combine("Content", "Assets", "textures", "ui", "leveledit"))) {
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
        }

        #endregion

        _initialized = true;

        TestLevel = new(TankGame.GameLanguage.TestLevel, TankGame.TextFont, Color.White);
        TestLevel.SetDimensions(() => new(WindowUtils.WindowWidth * 0.01f, WindowUtils.WindowHeight * 0.725f), () => new Vector2(200, 50).ToResolution());

        TestLevel.OnLeftClick = (l) => {
            Close(false);
            TankGame.OverheadView = false;

            var name = loadedCampaign?.CachedMissions[loadedCampaign.CurrentMissionId].Name;
            _cachedMission = Mission.GetCurrent(name);
        };

        ReturnToEditor = new(TankGame.GameLanguage.Return, TankGame.TextFont, Color.White);
        ReturnToEditor.SetDimensions(() => new(WindowUtils.WindowWidth * 0.01f, WindowUtils.WindowHeight * 0.02f), () => new Vector2(250, 50).ToResolution());

        ReturnToEditor.OnLeftClick = (l) => {
            Open(false);
            TankGame.OverheadView = true;
            GameProperties.InMission = false;
            // GameHandler.CleanupScene();
            if (_cachedMission is { Tanks: not null, Blocks: not null, Name: not null, Note: not null })
                Mission.LoadDirectly(_cachedMission);
            if (loadedCampaign is { })
                SetupMissionsBar(loadedCampaign);
        };

        Perspective = new(TankGame.GameLanguage.Perspective, TankGame.TextFont, Color.White);
        Perspective.SetDimensions(() => new(WindowUtils.WindowWidth * 0.125f, WindowUtils.WindowHeight * 0.725f), () => new Vector2(200, 50).ToResolution());
        Perspective.Tooltip = TankGame.GameLanguage.PerspectiveFlavor;
        Perspective.OnLeftClick = (l) => { TankGame.OverheadView = !TankGame.OverheadView; };

        TerrainCategory = new(TankGame.GameLanguage.Terrain, TankGame.TextFont, Color.White);
        TerrainCategory.SetDimensions(() => new(WindowUtils.WindowWidth * 0.75f, WindowUtils.WindowHeight * 0.725f), () => new Vector2(200, 50).ToResolution());
        TerrainCategory.OnLeftClick = (l) => { CurCategory = Category.Terrain; };

        EnemyTanksCategory = new(TankGame.GameLanguage.AIControlled, TankGame.TextFont, Color.White);
        EnemyTanksCategory.SetDimensions(() => new(WindowUtils.WindowWidth * 0.875f, WindowUtils.WindowHeight * 0.725f), () => new Vector2(200, 50).ToResolution());
        EnemyTanksCategory.OnLeftClick = (l) => { CurCategory = Category.EnemyTanks; };
        PlayerTanksCategory = new(TankGame.GameLanguage.Players, TankGame.TextFont, Color.White);
        PlayerTanksCategory.SetDimensions(() => new(WindowUtils.WindowWidth * 0.875f, WindowUtils.WindowHeight * 0.65f), () => new Vector2(200, 50).ToResolution());
        PlayerTanksCategory.OnLeftClick = (l) => { CurCategory = Category.PlayerTanks; };

        Properties = new(TankGame.GameLanguage.Properties, TankGame.TextFont, Color.White);

        float width = 200;

        Properties.SetDimensions(() => new(WindowUtils.WindowWidth * 0.425f - (width / 2).ToResolutionX(), 10.ToResolutionY()), () => new Vector2(width, 50).ToResolution());
        Properties.OnLeftClick = (a) => {
            if (!_saveMenuOpen)
                GUICategory = UICategory.SavingThings;
            else
                GUICategory = UICategory.LevelEditor;
        };

        LoadLevel = new(TankGame.GameLanguage.Load, TankGame.TextFont, Color.White);

        LoadLevel.SetDimensions(() => new(WindowUtils.WindowWidth * 0.575f - (width / 2).ToResolutionX(), 10.ToResolutionY()), () => new Vector2(width, 50).ToResolution());
        LoadLevel.OnLeftClick = (a) => {
            var res = Dialog.FileOpen("mission,campaign,bin", TankGame.SaveDirectory);
            if (res.Path != null && res.IsOk) {
                try {
                    var ext = Path.GetExtension(res.Path);

                    if (ext == ".mission") {
                        //GameProperties.LoadedCampaign.LoadMission(Mission.Load(res.Path, null));
                        //GameProperties.LoadedCampaign.SetupLoadedMission(true);
                        Mission.LoadDirectly(Mission.Load(res.Path, null));
                        //_loadedCampaign = null;
                    }
                    else if (ext == ".campaign") {
                        loadedCampaign = Campaign.Load(res.Path);
                        loadedCampaign.LoadMission(0);
                        loadedCampaign.SetupLoadedMission(true);
                        MissionName.Text = loadedCampaign.CachedMissions[0].Name;
                        SetupMissionsBar(loadedCampaign);
                    }
                    else if (ext == ".bin") {
                        var map = new WiiMap(res.Path);
                        ChatSystem.SendMessage($"(Width, Height): ({map.Width}, {map.Height})", Color.White);

                        WiiMap.ApplyToGameWorld(map);
                    }

                    ChatSystem.SendMessage($"Loaded '{Path.GetFileName(res.Path)}'.", Color.White);
                }
                catch (Exception e) {
                    ChatSystem.SendMessage("Failed to load.", Color.Red);
                    ChatSystem.SendMessage(e.Message, Color.Red);
                }
            }
            return;
        };
        // TODO: non-windows support. i am lazy. fuck this. also localize bozo
        LoadLevel.Tooltip = "Will open a file dialog for\nyou to choose what mission/campaign to load.";

        InitializeSaveMenu();

        SetLevelEditorVisibility(false);
    }

    public static void SetupMissionsBar(Campaign campaign, bool setCampaignData = true) {
        RemoveMissionButtons();

        // TODO: scissor, etc
        // offset
        // campaign metadata editing
        // go to bed ryan

        if (setCampaignData) {
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

        if (loadedCampaign != null) {
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
                var len = btn.Text.Length;
                btn.TextScale = () => Vector2.One * (len > 20 ? 1f - ((len - 20) * 0.03f) : 1f);

                btn.OnLeftClick = (a) => {
                    _missionButtons.ForEach(x => x.Color = UnselectedColor);
                    btn.Color = SelectedColor;

                    var mission = loadedCampaign.CachedMissions[loadedCampaign.CurrentMissionId];

                    loadedCampaign.CachedMissions[loadedCampaign.CurrentMissionId] = Mission.GetCurrent(mission.Name);

                    loadedCampaign.LoadMission(index);
                    loadedCampaign.SetupLoadedMission(true);

                    MissionName.Text = loadedCampaign.CachedMissions[index].Name;
                };
                _missionButtons.Add(btn);
            }
        }
        var addMission = new UITextButton("+", TankGame.TextFont, Color.White, () => Vector2.One.ToResolution()) {
            Tooltip = "Insert a blank mission after the currently selected mission."
        };
        addMission.SetDimensions(() => new Vector2(_missionButtonScissor.X + 15.ToResolutionX(), _missionButtonScissor.Y + _missionButtonScissor.Height + 5.ToResolutionY()), () => new Vector2(_missionButtonScissor.Width / 2, 25.ToResolutionY()));
        addMission.OnLeftClick = (a) => {
            _missionButtons.ForEach(x => x.Color = Color.White);
            addMission.Color = Color.SkyBlue;

            // Array.Resize(ref _loadedCampaign.CachedMissions, _loadedCampaign.CachedMissions.Length + 1);
            var count = loadedCampaign.CachedMissions.Count(c => c != default);
            var id = loadedCampaign.CurrentMissionId;
            loadedCampaign.CachedMissions[id] = Mission.GetCurrent(loadedCampaign.CachedMissions[id].Name);

            // move every mission up by 1 in the array.
            for (int i = count; i > id + 1; i--) {
                if (i + 1 >= loadedCampaign.CachedMissions.Length)
                    Array.Resize(ref loadedCampaign.CachedMissions, loadedCampaign.CachedMissions.Length + 1);
                loadedCampaign.CachedMissions[i] = loadedCampaign.CachedMissions[i - 1];
            }
            if (id + 1 >= loadedCampaign.CachedMissions.Length)
                Array.Resize(ref loadedCampaign.CachedMissions, loadedCampaign.CachedMissions.Length + 1);
            loadedCampaign.CachedMissions[id + 1] = new(Array.Empty<TankTemplate>(), Array.Empty<BlockTemplate>());
            loadedCampaign.LoadMission(id + 1);
            loadedCampaign.SetupLoadedMission(true);

            MissionName.Text = loadedCampaign.CachedMissions[id].Name;

            SetupMissionsBar(loadedCampaign, false);

            _missionButtons[id + 1].Color = SelectedColor;
        };
        _missionButtons.Add(addMission);

        var moveMissionUp = new UITextButton("v", TankGame.TextFont, Color.White, () => Vector2.One.ToResolution()) {
            Tooltip = "Swap the currently selected mission with the one above it.",
            TextRotation = MathHelper.Pi
        };
        moveMissionUp.SetDimensions(() => new(addMission.Position.X + addMission.Size.X, addMission.Position.Y), () => new Vector2(_missionButtonScissor.Width / 5, addMission.Size.Y));
        moveMissionUp.OnLeftClick = (a) => { MoveMission(true); };
        _missionButtons.Add(moveMissionUp);

        var moveMissionDown = new UITextButton("v", TankGame.TextFont, Color.White, () => Vector2.One.ToResolution()) {
            Tooltip = "Swap the currently selected mission with the one below it.",
        };
        moveMissionDown.SetDimensions(() => new(addMission.Position.X + addMission.Size.X + moveMissionUp.Size.X, addMission.Position.Y), () => new Vector2(moveMissionUp.Size.X, addMission.Size.Y));
        moveMissionDown.OnLeftClick = (a) => { MoveMission(false); };
        _missionButtons.Add(moveMissionDown);
    }

    private static void RemoveMissionButtons() {
        for (int i = 0; i < _missionButtons.Count; i++)
            _missionButtons[i].Remove();
        _missionButtons.Clear();
    }

    public static void Open(bool fromMainMenu = true) {
        if (fromMainMenu) {

            IntermissionSystem.TimeBlack = 180;
            GameProperties.ShouldMissionsProgress = false;
            if (_loadTask == null)
                _loadTask = Task.Run(async () => {
                    await Task.Delay(TankGame.LogicTime).ConfigureAwait(false);
                    while (IntermissionSystem.BlackAlpha > 0.8f || MainMenu.Active) {
                        if (IntermissionSystem.BlackAlpha == 0) {
                            // The user has exited without completing the transition by our greatly hardcoded poorly parallelized code.
                            // why did we design this like this?
                            // - Dottik
                            _loadTask = null;
                            return;
                        }
                        await Task.Delay(TankGame.LogicTime).ConfigureAwait(false);
                    }

                    Active = true;
                    TankGame.OverheadView = true;
                    Theme.Play();
                    SetLevelEditorVisibility(true);
                    loadedCampaign = new();
                    loadedCampaign.CachedMissions[0] = new(Array.Empty<TankTemplate>(), Array.Empty<BlockTemplate>()) {
                        Name = "No Name"
                    };
                    SetupMissionsBar(loadedCampaign);
                    _loadTask = null;
                });
        }
        else {
            Theme.Play();
            Active = true;
            SetLevelEditorVisibility(true);
        }
        Editing = true;
    }

    public static void Close(bool toMainMenu) {
        Active = false;
        RemoveMissionButtons();

        Theme.SetVolume(0);
        Theme.Stop();
        SetLevelEditorVisibility(false);
        SetSaveMenuVisibility(false);
        // SetMissionsVisibility(false);
        if (toMainMenu)
            loadedCampaign = null;
        PlacementSquare.ResetSquares();
    }

    private static Rectangle _clickRect;

    private static readonly Dictionary<Rectangle, (int, string)> ClickEventsPerItem = new(); // hover zone, id, description

    private static string _curDescription = string.Empty;
    private static Rectangle _curHoverRect;

    public static Color SelectionColor = Color.NavajoWhite;
    public static Color HoverBoxColor = Color.SkyBlue;

    public static void Render() {
        if (!_initialized)
            return;

        ShouldDrawBarUI = !GameUI.Paused;
        SwapMenu.Text = _viewMissionDetails ? "Campaign Details" : "Mission Details";

        var measure = TankGame.TextFont.MeasureString(AlertText);

        if (_alertTime > 0) {
            var scale = 0.5f;
            TankGame.SpriteRenderer.Draw(ChatSystem.ChatAlert,
                new Vector2(WindowUtils.WindowWidth / 2, (WindowUtils.WindowHeight * 0.625f) - (ChatSystem.ChatAlert.Size().Y.ToResolutionY() * scale)),
                null,
                Color.White,
                0f,
                ChatSystem.ChatAlert.Size() / 2,
                new Vector2(scale).ToResolution(),
                default,
                default);
            SpriteFontUtils.DrawBorderedText(TankGame.SpriteRenderer, TankGame.TextFontLarge, AlertText, new Vector2(WindowUtils.WindowWidth / 2, WindowUtils.WindowHeight * 0.625f), Color.Red, Color.White, new Vector2(0.4f).ToResolution(), 0f, Anchor.Center);
            _alertTime -= TankGame.DeltaTime;
        }
        if (!ShouldDrawBarUI)
            return;
        var info = TankGame.GameLanguage.BinDisclaimer;
        TankGame.SpriteRenderer.DrawString(TankGame.TextFont, info, new Vector2(WindowUtils.WindowWidth - 175.ToResolutionX(), WindowUtils.WindowHeight / 2 - 20.ToResolutionY()), Color.White, new Vector2(0.425f).ToResolution(), 0f, TankGame.TextFont.MeasureString(info) / 2);

        #region Main UI

        int xOff = 0;
        _clickRect = new(0, (int)(WindowUtils.WindowBottom.Y * 0.8f), WindowUtils.WindowWidth, (int)(WindowUtils.WindowHeight * 0.2f));
        TankGame.SpriteRenderer.Draw(TankGame.WhitePixel, _clickRect, null, Color.White, 0f, Vector2.Zero, default, 0f);

        measure = TankGame.TextFont.MeasureString(_curDescription);

        if (_curDescription != null && _curDescription != string.Empty) {
            int padding = 20;
            var orig = new Vector2(0, TankGame.WhitePixel.Size().Y);
            TankGame.SpriteRenderer.Draw(TankGame.WhitePixel,
                new Rectangle((int)(WindowUtils.WindowWidth / 2 - (measure.X / 2 + padding).ToResolutionX()), (int)(WindowUtils.WindowHeight * 0.8f), (int)(measure.X + padding * 2).ToResolutionX(), (int)(measure.Y + 20).ToResolutionY()),
                null,
                Color.White,
                0f,
                orig,
                default,
                0f);
        }

        // i feel like i could turn these panels into their own method.
        // but whatever.

        TankGame.SpriteRenderer.DrawString(TankGame.TextFont, _curDescription, new Vector2(WindowUtils.WindowWidth / 2, WindowUtils.WindowHeight * 0.78f), Color.Black, Vector2.One.ToResolution(), 0f, new Vector2(measure.X / 2, measure.Y));
        // level info
        TankGame.SpriteRenderer.Draw(TankGame.WhitePixel, new Rectangle(0, 0, 350, 125).ToResolution(), null, Color.Gray, 0f, Vector2.Zero, default, 0f);
        TankGame.SpriteRenderer.Draw(TankGame.WhitePixel, new Rectangle(0, 0, 350, 40).ToResolution(), null, Color.White, 0f, Vector2.Zero, default, 0f);
        TankGame.SpriteRenderer.DrawString(TankGame.TextFont, TankGame.GameLanguage.LevelInfo, new Vector2(175, 3).ToResolution(), Color.Black, Vector2.One.ToResolution(), 0f, GameUtils.GetAnchor(Anchor.TopCenter, TankGame.TextFont.MeasureString(TankGame.GameLanguage.LevelInfo)));
        TankGame.SpriteRenderer.DrawString(TankGame.TextFont, $"{TankGame.GameLanguage.EnemyTankTotal}: {AIManager.CountAll()}", new Vector2(10, 40).ToResolution(), Color.White, Vector2.One.ToResolution(), 0f, Vector2.Zero);
        TankGame.SpriteRenderer.DrawString(TankGame.TextFont, $"{TankGame.GameLanguage.DifficultyRating}: {DifficultyAlgorithm.GetDifficulty(missionToRate):0.00}", new Vector2(10, 80).ToResolution(), Color.White, Vector2.One.ToResolution(), 0f, Vector2.Zero);


        // render campaign missions ui 

        if (loadedCampaign != null) {
            var heightDiff = 40;
            _missionButtonScissor = new Rectangle(_missionTab.X, _missionTab.Y + heightDiff, _missionTab.Width, _missionTab.Height - heightDiff * 2).ToResolution();
            TankGame.SpriteRenderer.Draw(TankGame.WhitePixel, _missionTab.ToResolution(), null, Color.Gray, 0f, Vector2.Zero, default, 0f);
            TankGame.SpriteRenderer.Draw(TankGame.WhitePixel, new Rectangle(_missionTab.X, _missionTab.Y, _missionTab.Width, heightDiff).ToResolution(), null, Color.White, 0f, Vector2.Zero, default, 0f);
            TankGame.SpriteRenderer.DrawString(TankGame.TextFont,
                TankGame.GameLanguage.MissionList,
                new Vector2(175, 153).ToResolution(),
                Color.Black,
                Vector2.One.ToResolution(),
                0f,
                GameUtils.GetAnchor(Anchor.TopCenter, TankGame.TextFont.MeasureString(TankGame.GameLanguage.MissionList)));
        }
        // render teams

        // placement information
        TankGame.SpriteRenderer.Draw(TankGame.WhitePixel, new Rectangle(WindowUtils.WindowWidth - (int)350.ToResolutionX(), 0, (int)350.ToResolutionX(), (int)500.ToResolutionY()), null, Color.Gray, 0f, Vector2.Zero, default, 0f);
        TankGame.SpriteRenderer.Draw(TankGame.WhitePixel, new Rectangle(WindowUtils.WindowWidth - (int)350.ToResolutionX(), 0, (int)350.ToResolutionX(), (int)40.ToResolutionY()), null, Color.White, 0f, Vector2.Zero, default, 0f);
        TankGame.SpriteRenderer.DrawString(TankGame.TextFont,
            TankGame.GameLanguage.PlaceInfo,
            new Vector2(WindowUtils.WindowWidth - 175.ToResolutionX(), 3.ToResolutionY()),
            Color.Black,
            Vector2.One.ToResolution(),
            0f,
            GameUtils.GetAnchor(Anchor.TopCenter, TankGame.TextFont.MeasureString(TankGame.GameLanguage.PlaceInfo)));
        //if (CurCategory == Category.Blocks)
        //TankGame.SpriteRenderer.DrawString(TankGame.TextFont, $"Block Stack: {BlockHeight}", new Vector2(WindowUtils.WindowWidth - 335.ToResolutionX(), 40.ToResolutionY()), Color.White, Vector2.One.ToResolution(), 0f, Vector2.Zero);
        var helpText = "";
        Vector2 start = new();
        if (CurCategory == Category.EnemyTanks || CurCategory == Category.PlayerTanks) {
            helpText = TankGame.GameLanguage.PlacementTeamInfo;
            start = new(WindowUtils.WindowWidth - 250.ToResolutionX(), 140.ToResolutionY());
            // TODO: should be optimised. do later.
            TankGame.SpriteRenderer.DrawString(TankGame.TextFont, TankGame.GameLanguage.TankTeams, new Vector2(start.X + 45.ToResolutionX(), start.Y - 80.ToResolutionY()), Color.White, Vector2.One.ToResolution(), 0f, TankGame.TextFont.MeasureString("Tank Teams") / 2);

            TankGame.SpriteRenderer.Draw(TankGame.WhitePixel, new Rectangle((int)start.X, (int)(start.Y - 40.ToResolutionY()), (int)40.ToResolutionX(), (int)40.ToResolutionY()), null, Color.Black, 0f, Vector2.Zero, default, 0f);
            TankGame.SpriteRenderer.DrawString(TankGame.TextFont, TankGame.GameLanguage.NoTeam, new Vector2(start.X + 45.ToResolutionX(), start.Y - 40.ToResolutionY()), Color.Black, Vector2.One.ToResolution(), 0f, Vector2.Zero);
            for (int i = 0; i < TeamID.Collection.Count - 1; i++) {
                var color = TeamID.TeamColors[i + 1];

                TankGame.SpriteRenderer.DrawString(TankGame.TextFont, TeamColorsLocalized[i + 1], new Vector2(start.X + 45.ToResolutionX(), start.Y + (i * 40).ToResolutionY()), color, Vector2.One.ToResolution(), 0f, Vector2.Zero);
                TankGame.SpriteRenderer.Draw(TankGame.WhitePixel, new Rectangle((int)start.X, (int)(start.Y + (i * 40).ToResolutionY()), (int)40.ToResolutionX(), (int)40.ToResolutionY()), null, color, 0f, Vector2.Zero, default, 0f);
            }
            TankGame.SpriteRenderer.DrawString(TankGame.TextFontLarge, ">", new Vector2(start.X - 25.ToResolutionX(), start.Y + ((int)(SelectedTankTeam - 1) * 40).ToResolutionY()), Color.White, Vector2.One.ToResolution(), 0f, TankGame.TextFontLarge.MeasureString(">") / 2);

            if (SelectedTankTeam != TeamID.Magenta)
                TankGame.SpriteRenderer.DrawString(TankGame.TextFont, "v", new Vector2(start.X - 25.ToResolutionX(), start.Y + ((int)(SelectedTankTeam - 1) * 40 + 50).ToResolutionY()), Color.White, Vector2.One.ToResolution(), 0f, TankGame.TextFont.MeasureString("v") / 2);
            if (SelectedTankTeam != TeamID.NoTeam)
                TankGame.SpriteRenderer.DrawString(TankGame.TextFont,
                    "v",
                    new Vector2(start.X - 25.ToResolutionX(), start.Y + ((int)(SelectedTankTeam - 1) * 40 - 10).ToResolutionY()),
                    Color.White,
                    Vector2.One.ToResolution(),
                    MathHelper.Pi,
                    TankGame.TextFont.MeasureString("v") / 2);
        }
        else if (CurCategory == Category.Terrain) {
            helpText = "UP and DOWN to change stack.";
            // TODO: add static dict for specific types?
            var tex = SelectedBlockType != BlockID.Hole ? $"{BlockID.Collection.GetKey(SelectedBlockType)}_{BlockHeight}" : $"{BlockID.Collection.GetKey(SelectedBlockType)}";
            var size = RenderTextures[tex].Size();
            start = new Vector2(WindowUtils.WindowWidth - 175.ToResolutionX(), 450.ToResolutionY());
            TankGame.SpriteRenderer.Draw(RenderTextures[tex], start, null, Color.White, 0f, new Vector2(size.X / 2, size.Y), Vector2.One.ToResolution(), default, 0f);
            // TODO: reduce the hardcode for modders, yeah
            if (SelectedBlockType != BlockID.Teleporter && SelectedBlockType != BlockID.Hole) {
                TankGame.SpriteRenderer.DrawString(TankGame.TextFontLarge, "v", new Vector2(start.X + 100.ToResolutionX(), start.Y - 75.ToResolutionY()), Color.White, Vector2.One.ToResolution(), 0f, TankGame.TextFontLarge.MeasureString("v") / 2);
                TankGame.SpriteRenderer.DrawString(TankGame.TextFontLarge, "v", new Vector2(start.X - 100.ToResolutionX(), start.Y - 25.ToResolutionY()), Color.White, Vector2.One.ToResolution(), MathHelper.Pi, TankGame.TextFontLarge.MeasureString("v") / 2);
            }
        }
        TankGame.SpriteRenderer.DrawString(TankGame.TextFont, helpText, new Vector2(WindowUtils.WindowWidth - 175.ToResolutionX(), WindowUtils.WindowHeight / 2 - 70.ToResolutionY()), Color.White, new Vector2(0.5f).ToResolution(), 0f, TankGame.TextFont.MeasureString(helpText) / 2);
        TankGame.SpriteRenderer.Draw(TankGame.WhitePixel, _curHoverRect, null, HoverBoxColor * 0.5f, 0f, Vector2.Zero, default, 0f);

        if (CurCategory == Category.EnemyTanks) {
            for (int i = 0; i < _renderNamesTanks.Count; i++) {
                // TODO: i come back to this code and i think "what kind of drugs was ryan on?" to my surprise i have no clue.
                // the magic numbers here hurt my brain.
                ClickEventsPerItem[new Rectangle((int)(34.ToResolutionX() + xOff + _barOffset), (int)(WindowUtils.WindowBottom.Y * 0.8f), (int)234.ToResolutionX(), (int)(WindowUtils.WindowHeight * 0.2f))] =
                    (i + 2, (i + 2) switch {
                        TankID.Brown => TankGame.GameLanguage.BrownFlavor,
                        TankID.Ash => TankGame.GameLanguage.AshFlavor,
                        TankID.Marine => TankGame.GameLanguage.MarineFlavor,
                        TankID.Yellow => TankGame.GameLanguage.YellowFlavor,
                        TankID.Pink => TankGame.GameLanguage.PinkFlavor,
                        TankID.Green => TankGame.GameLanguage.GreenFlavor,
                        TankID.Violet => TankGame.GameLanguage.VioletFlavor,
                        TankID.White => TankGame.GameLanguage.WhiteFlavor,
                        TankID.Black => TankGame.GameLanguage.BlackFlavor,
                        TankID.Bronze => TankGame.GameLanguage.BronzeFlavor,
                        TankID.Silver => TankGame.GameLanguage.SilverFlavor,
                        TankID.Sapphire => TankGame.GameLanguage.SapphireFlavor,
                        TankID.Ruby => TankGame.GameLanguage.RubyFlavor,
                        TankID.Citrine => TankGame.GameLanguage.CitrineFlavor,
                        TankID.Amethyst => TankGame.GameLanguage.AmethystFlavor,
                        TankID.Emerald => TankGame.GameLanguage.EmeraldFlavor,
                        TankID.Gold => TankGame.GameLanguage.GoldFlavor,
                        TankID.Obsidian => TankGame.GameLanguage.ObsidianFlavor,
                        TankID.Granite => TankGame.GameLanguage.GraniteFlavor,
                        TankID.Bubblegum => TankGame.GameLanguage.BubblegumFlavor,
                        TankID.Water => TankGame.GameLanguage.WaterFlavor,
                        TankID.Crimson => TankGame.GameLanguage.CrimsonFlavor,
                        TankID.Tiger => TankGame.GameLanguage.TigerFlavor,
                        TankID.Fade => TankGame.GameLanguage.FadeFlavor,
                        TankID.Creeper => TankGame.GameLanguage.CreeperFlavor,
                        TankID.Gamma => TankGame.GameLanguage.GammaFlavor,
                        TankID.Marble => TankGame.GameLanguage.MarbleFlavor,
                        _ => "Did Not Load (DNL)"
                    }); // TODO: localize this. i hate english.

                TankGame.SpriteRenderer.Draw(RenderTextures[_renderNamesTanks[i]],
                    new Vector2(24.ToResolutionX() + xOff + _barOffset, WindowUtils.WindowBottom.Y * 0.75f),
                    null,
                    (int)SelectedTankTier - 2 == i ? SelectionColor : Color.White,
                    0f,
                    Vector2.Zero,
                    Vector2.One.ToResolution(),
                    default,
                    0f);
                xOff += (int)234.ToResolutionX();
            }
            _maxScroll = xOff;
        }
        else if (CurCategory == Category.Terrain) {
            for (int i = 0; i < _renderNamesBlocks.Count; i++) {
                ClickEventsPerItem[new Rectangle((int)(34.ToResolutionX() + xOff + _barOffset), (int)(WindowUtils.WindowBottom.Y * 0.8f), (int)234.ToResolutionX(), (int)(WindowUtils.WindowHeight * 0.2f))] =
                    (i, i switch {
                        BlockID.Wood => TankGame.GameLanguage.WoodFlavor,
                        BlockID.Cork => TankGame.GameLanguage.CorkFlavor,
                        BlockID.Hole => TankGame.GameLanguage.HoleFlavor,
                        _ => "Did Not Load (DNL)"
                    });

                TankGame.SpriteRenderer.Draw(RenderTextures[_renderNamesBlocks[i]],
                    new Vector2(24.ToResolutionX() + xOff + _barOffset, WindowUtils.WindowBottom.Y * 0.75f),
                    null,
                    (int)SelectedBlockType == i ? SelectionColor : Color.White,
                    0f,
                    Vector2.Zero,
                    Vector2.One.ToResolution(),
                    default,
                    0f);
                xOff += (int)234.ToResolutionX();
            }
            _maxScroll = xOff;
        }
        else if (CurCategory == Category.PlayerTanks) {
            for (int i = 0; i < _renderNamesPlayers.Count; i++) {
                ClickEventsPerItem[new Rectangle((int)(34.ToResolutionX() + xOff + _barOffset), (int)(WindowUtils.WindowBottom.Y * 0.8f), (int)234.ToResolutionX(), (int)(WindowUtils.WindowHeight * 0.2f))] =
                    (i, i switch {
                        PlayerID.Blue => TankGame.GameLanguage.P1TankFlavor,
                        PlayerID.Red => TankGame.GameLanguage.P2TankFlavor,
                        PlayerID.GreenPlr => TankGame.GameLanguage.P3TankFlavor,
                        PlayerID.YellowPlr => TankGame.GameLanguage.P4TankFlavor,
                        _ => "Did Not Load (DNL)"
                    });

                TankGame.SpriteRenderer.Draw(RenderTextures[_renderNamesPlayers[i]],
                    new Vector2(24.ToResolutionX() + xOff + _barOffset, WindowUtils.WindowBottom.Y * 0.75f),
                    null,
                    (int)SelectedPlayerType == i ? SelectionColor : Color.White,
                    0f,
                    Vector2.Zero,
                    Vector2.One.ToResolution(),
                    default,
                    0f);
                xOff += (int)234.ToResolutionX();
            }
            _maxScroll = xOff;
        }
        if (DebugManager.DebuggingEnabled) {
            int a = 0;
            foreach (var thing in ClickEventsPerItem) {
                if (DebugManager.DebugLevel == 3)
                    TankGame.SpriteRenderer.Draw(TankGame.WhitePixel, thing.Key, null, Color.Red * 0.5f, 0f, Vector2.Zero, default, 0f);
                var text = thing.Key.Contains(MouseUtils.MousePosition.ToPoint()) ? $"{thing.Key} ---- {thing.Value.Item1} (HOVERED)" : $"{thing.Key} ---- {thing.Value.Item1}";
                DebugManager.DrawDebugString(TankGame.SpriteRenderer, text, new Vector2(500, 20 + a), 3);
                a += 20;
            }
        }

        #endregion

        // TankGame.SpriteRenderer.Draw(TankGame.WhitePixel, _missionButtonScissor, null, Color.Red * 0.5f, 0f, Vector2.Zero, default, 0f);

        if (Active && TankGame.HoveringAnyTank) {
            var tex = GameResources.GetGameResource<Texture2D>("Assets/textures/ui/leveledit/rotate");
            TankGame.SpriteRenderer.Draw(tex,
                MouseUtils.MousePosition + new Vector2(20, -20).ToResolution(),
                null,
                Color.White,
                0f,
                new Vector2(0, tex.Size().Y),
                0.2f.ToResolution(),
                default,
                0f);
        }
        if (Active && GUICategory == UICategory.SavingThings)
            TankGame.SpriteRenderer.Draw(TankGame.WhitePixel,
                LevelContentsPanel,
                null,
                Color.Gray,
                0f,
                Vector2.Zero,
                default,
                0f);
        var txt = !_viewMissionDetails ? TankGame.GameLanguage.CampaignDetails : TankGame.GameLanguage.MissionDetails;

        if (GUICategory == UICategory.SavingThings)
            TankGame.SpriteRenderer.DrawString(TankGame.TextFont,
                txt,
                new Vector2(LevelContentsPanel.X + LevelContentsPanel.Width / 2, LevelContentsPanel.Y + 10.ToResolutionY()),
                Color.White,
                Vector2.One.ToResolution(),
                0f,
                new Vector2(TankGame.TextFont.MeasureString(txt).X / 2, 0));

        _campaignElems.ForEach(elem => {
            if (elem is UITextInput)
                elem.DrawText = false;
            elem.UniqueDraw = (a, b) => {
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

    public static void Update() {
        if (!_initialized)
            return;

        if (Active) {
            IntermissionHandler.TankFunctionWait = 190;
            if (DebugManager.DebuggingEnabled)
                if (InputUtils.KeyJustPressed(Keys.T))
                    PlacementSquare.DrawStacks = !PlacementSquare.DrawStacks;
        }

        if (_missionButtonScissor.Contains(MouseUtils.MousePosition))
            _missionsOffset += InputUtils.GetScrollWheelChange() * 30;

        _missionsMaxOff = _missionButtons.Count * 30.ToResolutionY();
        SaveLevelConfirm.Tooltip = _viewMissionDetails ? TankGame.GameLanguage.MissionSaveFlavor : TankGame.GameLanguage.CampaignSaveFlavor;
        CampaignMajorVictory.Text = TankGame.GameLanguage.HasMajorVictoryTheme + ": " + (_hasMajorVictory ? TankGame.GameLanguage.Yes : TankGame.GameLanguage.No);

        if (_missionsOffset > 0)
            _missionsOffset = 0;
        else if (-_missionsMaxOff < -_missionButtonScissor.Height && _missionsOffset < -_missionsMaxOff + _missionButtonScissor.Height)
            _missionsOffset = -_missionsMaxOff + _missionButtonScissor.Height;

        LevelContentsPanel = new Rectangle(WindowUtils.WindowWidth / 4, (int)(WindowUtils.WindowHeight * 0.1f), WindowUtils.WindowWidth / 2, (int)(WindowUtils.WindowHeight * 0.625f));
        PlacementSquare.PlacesBlock = CurCategory == Category.Terrain;
        switch (CurCategory) {
            case Category.EnemyTanks:
                EnemyTanksCategory.Color = Color.DeepSkyBlue;
                TerrainCategory.Color = Color.White;
                PlayerTanksCategory.Color = Color.White;
                break;
            case Category.Terrain:
                EnemyTanksCategory.Color = Color.White;
                TerrainCategory.Color = Color.DeepSkyBlue;
                PlayerTanksCategory.Color = Color.White;
                break;
            case Category.PlayerTanks:
                EnemyTanksCategory.Color = Color.White;
                TerrainCategory.Color = Color.White;
                PlayerTanksCategory.Color = Color.DeepSkyBlue;
                break;
        }
        if (Active) {
            Theme.SetVolume(0.4f * TankGame.Settings.MusicVolume);

            _curDescription = string.Empty;

            _curHoverRect = new();
            foreach (var thing in ClickEventsPerItem) {
                if (thing.Key.Contains(MouseUtils.MousePosition.ToPoint())) {
                    _curHoverRect = thing.Key;
                    if (thing.Value.Item2 != null)
                        _curDescription = thing.Value.Item2;
                }
            }

            if (InputUtils.CanDetectClick()) {
                _origClick = MouseUtils.MousePosition - new Vector2(_barOffset, 0);

                for (int i = 0; i < ClickEventsPerItem.Count; i++) {
                    var evt = ClickEventsPerItem.ElementAt(i);
                    if (evt.Key.Contains(MouseUtils.MousePosition.ToPoint())) {
                        if (CurCategory == Category.EnemyTanks)
                            SelectedTankTier = evt.Value.Item1;
                        else if (CurCategory == Category.Terrain)
                            SelectedBlockType = evt.Value.Item1;
                        else if (CurCategory == Category.PlayerTanks)
                            SelectedPlayerType = evt.Value.Item1;
                    }
                }
            }
            if (InputUtils.MouseLeft && _clickRect.Contains(MouseUtils.MousePosition.ToPoint())) {
                _barOffset = MouseUtils.MousePosition.X - _origClick.X;
                if (_barOffset < -_maxScroll + WindowUtils.WindowWidth - 60.ToResolutionX())
                    _barOffset = -_maxScroll + WindowUtils.WindowWidth - 60.ToResolutionX();
                if (_barOffset > 0) {
                    _barOffset = 0;
                    _origClick = MouseUtils.MousePosition - new Vector2(_barOffset, 0);
                }
            }

            BlockHeight = MathHelper.Clamp(BlockHeight, 1, 7);

            if (CurCategory == Category.EnemyTanks || CurCategory == Category.PlayerTanks) {
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
            else if (CurCategory == Category.Terrain) {
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
            if (IntermissionHandler.NothingCanHappenAnymore(_cachedMission, out bool victory))
                QueueEditorReEntry(120f);
        // if (ReturnToEditor != null)
        ReturnToEditor.IsVisible = Editing && !Active && !MainMenu.Active;
    }

    private static float _waitTime;
    private static bool _isWaiting;

    private static void QueueEditorReEntry(float delay) {
        if (!_isWaiting)
            _waitTime = delay;
        _isWaiting = true;

        _waitTime -= TankGame.DeltaTime;
        if (_waitTime < 0) {
            ReturnToEditor?.OnLeftClick?.Invoke(null);
            _waitTime = 0;
            _isWaiting = false;
        }
    }
}