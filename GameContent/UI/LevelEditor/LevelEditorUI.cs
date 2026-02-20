using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TanksRebirth.GameContent.Globals;
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
using TanksRebirth.GameContent.UI.MainMenu;
using System.Diagnostics;
using TanksRebirth.GameContent.Systems.TankSystem;
using TanksRebirth.GameContent.Systems.ParticleSystem;
using TanksRebirth.Internals.Common.Framework.Collections;

namespace TanksRebirth.GameContent.UI.LevelEditor;

/* TODO:
 * Make more modular...
 * 1) Block stacks are essentially their own data structure, so more can be added
 * 2) LevelEditorTankElement - One for each enemy tank. Will support mods
 * 3) LevelEditorTerrainElement - One for each obstacle. Will support mods
 * 4) Make these elements not textures, but rather rendered as their appropriate models with text underneath, for modularity
 * 5) When in the level editor, the GC gains ~10MB per second. Find out why. (Collections are often)
 */
public static partial class LevelEditorUI {
    public enum LevelEditState {
        LevelEditor,
        SavingThings,
    }
    public static readonly byte[] LevelFileHeader = [84, 65, 78, 75]; // T, A, N, K
    public const int EDITOR_VERSION = 5;
    public const int MAX_MISSION_CHARS = 30;
    public const float BAR_WIDTH = 256.0f;
    public const float BAR_START_X = 34.0f;
    public static bool IsActive { get; private set; }
    public static OggMusic Theme = new("Level Editor Theme", "Content/Assets/music/mainmenu/editor.ogg", 0.7f);

    static EditorCategory _curc;
    public static EditorCategory CurCategory {
        get => _curc;
        set {
            _curc = value;

            _barOffset = 0;

            foreach (var pList in _categoryParticles) {
                var isCorrect = _curc == pList.Key;
                foreach (var p in pList.Value) {
                    if (isCorrect) p.Alpha = 1;
                    else {
                        p.Alpha = 0;
                        p.Scale = Vector3.Zero;
                    }
                }
            }
        }
    }
    public static int SelectedTankTier { get; private set; }
    public static int SelectedTankTeam { get; private set; }
    public static int SelectedPlayerType { get; private set; }
    public static int SelectedBlockType { get; private set; }
    public static int BlockStack { get; private set; }
    public static bool IsEditing { get; internal set; }
    public static bool IsTestingLevel { get; private set; }
    public static bool HoveringAnyTank;

    internal static Mission cachedMission;
    public enum EditorCategory {
        EnemyTanks,
        Terrain,
        PlayerTanks
    }

    static bool _initialized;

    internal static Campaign loadedCampaign;

    static bool _viewMissionDetails = true;
    static bool _hasMajorVictory;

    public static Color SelectedColor = Color.SkyBlue;
    public static Color UnselectedColor = Color.White;

    static Task? _loadTask;
    static int _oldelta;

    static readonly List<UITextInput> _campaignTextInputs = [];
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

    internal static float difficultyRating;
    // internal static float difficultyRating;

    static bool _sdbui;

    static bool _queueOpen;
    static float _openCountndown;
    public static bool ShouldDrawBarUI {
        get => _sdbui;
        set {
            if (EditState == LevelEditState.SavingThings)
                SetSaveMenuVisibility(value);
            SetBarUIVisibility(value);
            _sdbui = value;
        }
    }

    private static LevelEditState _category;
    public static LevelEditState EditState {
        get => _category;
        set {
            _category = value;

            if (_category == LevelEditState.SavingThings) {
                SetSaveMenuVisibility(true);
                SetLevelEditorVisibility(false);
            }
            else {
                SetSaveMenuVisibility(false);
                SetLevelEditorVisibility(true);
            }
        }
    }

    static readonly List<UITextButton> _missionButtons = [];
    static readonly List<UITextButton> _listModifyButtons = [];
    static Rectangle _missionTab = new(0, 150, 350, 535);
    static Rectangle _missionButtonScissor;
    static float _missionsOffset;
    static float _missionsMaxOff;

    private static bool _saveMenuOpen;

    public static List<string> TeamColorsLocalized = [];
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
        MissionGrantsLife.IsVisible = visible && _viewMissionDetails;

        SaveLevelConfirm.IsVisible = visible;
        SaveMenuReturn.IsVisible = visible;
        SwapMenu.IsVisible = visible;
        SaveMenuReturn.IsVisible = visible;

        CampaignName.IsVisible = visible && !_viewMissionDetails;
        CampaignDescription.IsVisible = visible && !_viewMissionDetails;
        CampaignAuthor.IsVisible = visible && !_viewMissionDetails;
        CampaignTags.IsVisible = visible && !_viewMissionDetails;
        CampaignStartingLives.IsVisible = visible && !_viewMissionDetails;
        CampaignVersion.IsVisible = visible && !_viewMissionDetails;
        CampaignLoadingBGColor.IsVisible = visible && !_viewMissionDetails;
        CampaignLoadingBannercolor.IsVisible = visible && !_viewMissionDetails;
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
        AutoOrientTanks.IsVisible = visible;
    }
    public static void Initialize() {
        if (_initialized) {
            foreach (var field in typeof(LevelEditorUI).GetFields()) {
                if (field.GetValue(null) is UIElement element) {
                    element.Remove();
                    field.SetValue(null, null);
                }
            }
        }
        TeamColorsLocalized.Clear();
        for (int i = 0; i < TeamID.Collection.Count; i++) {
            var name = TeamID.Collection.GetKey(i);
            TeamColorsLocalized.Add((string)typeof(Language).GetProperty(name!)!.GetValue(TankGame.GameLanguage)!);
        }

        // maybe init/de-init EditorParticleSystem?

        #region Enumerable Init

        if (!_initialized) {

            foreach (var file in Directory.GetFiles(Path.Combine("Content", "Assets", "textures", "ui", "leveledit"))) {
                var fileName = Path.GetFileNameWithoutExtension(file);
                RenderTextures.Add(fileName, GameResources.GetGameResource<Texture2D>(file, false, false));
            }
            var names = BlockID.Collection.Keys;
            for (int i = 0; i < names.Length; i++) {
                var nTL = names[i];

                if (RenderTextures.ContainsKey(nTL))
                    _renderNamesBlocks.Add(nTL);
            }
        }

        #endregion

        _initialized = true;

        AddMissionBtn = new UITextButton("+", FontGlobals.RebirthFont, Color.White, () => Vector2.One.ToResolution()) {
            Tooltip = TankGame.GameLanguage.AddMissionFlavor
        };
        AddMissionBtn.SetDimensions(
            () => new Vector2(_missionButtonScissor.X + 15.ToResolutionX(), _missionButtonScissor.Y + _missionButtonScissor.Height + 5.ToResolutionY()),
            () => new Vector2(_missionButtonScissor.Width / 4.5f, 25.ToResolutionY()));
        AddMissionBtn.OnLeftClick = (a) => {
            AddMission();
        };
        _listModifyButtons.Add(AddMissionBtn);

        RemoveMissionBtn = new UITextButton("-", FontGlobals.RebirthFont, Color.White, () => Vector2.One.ToResolution()) {
            Tooltip = TankGame.GameLanguage.RemoveMissionFlavor
        };
        RemoveMissionBtn.SetDimensions(
            () => new Vector2(AddMissionBtn.Position.X + AddMissionBtn.Size.X, AddMissionBtn.Position.Y),
            () => new Vector2(AddMissionBtn.Size.X, AddMissionBtn.Size.Y));
        RemoveMissionBtn.OnLeftClick = (a) => {
            RemoveMission();
        };
        _listModifyButtons.Add(RemoveMissionBtn);

        MoveMissionUp = new UITextButton("v", FontGlobals.RebirthFont, Color.White, () => Vector2.One.ToResolution()) {
            Tooltip = TankGame.GameLanguage.MoveMissionUpFlavor,
            TextRotation = MathHelper.Pi
        };
        MoveMissionUp.SetDimensions(
            () => new(RemoveMissionBtn.Position.X + RemoveMissionBtn.Size.X, RemoveMissionBtn.Position.Y),
            () => new Vector2(AddMissionBtn.Size.X, RemoveMissionBtn.Size.Y));
        MoveMissionUp.OnLeftClick = (a) => { MoveMission(true); };
        _listModifyButtons.Add(MoveMissionUp);

        MoveMissionDown = new UITextButton("v", FontGlobals.RebirthFont, Color.White, () => Vector2.One.ToResolution()) {
            Tooltip = TankGame.GameLanguage.MoveMissionDownFlavor
        };
        MoveMissionDown.SetDimensions(
            () => new(MoveMissionUp.Position.X + MoveMissionUp.Size.X, AddMissionBtn.Position.Y),
            () => new Vector2(MoveMissionUp.Size.X, AddMissionBtn.Size.Y));
        MoveMissionDown.OnLeftClick = (a) => { MoveMission(false); };
        _listModifyButtons.Add(MoveMissionDown);

        AddMissionBtn.IsVisible =
            RemoveMissionBtn.IsVisible =
            MoveMissionUp.IsVisible =
            MoveMissionDown.IsVisible = IsActive;

        TestLevel = new(TankGame.GameLanguage.TestLevel, FontGlobals.RebirthFont, Color.White);
        TestLevel.SetDimensions(() => new(WindowUtils.WindowWidth * 0.01f, WindowUtils.WindowHeight * 0.725f), () => new Vector2(200, 50).ToResolution());

        TestLevel.OnLeftClick = (l) => {
            Close(false);
            CameraGlobals.OverheadView = false;

            IsTestingLevel = true;

            var name = loadedCampaign.CachedMissions[loadedCampaign.CurrentMissionId].Name;
            var grantsLife = loadedCampaign.CachedMissions[loadedCampaign.CurrentMissionId].GrantsExtraLife;
            cachedMission = Mission.GetCurrent(name, grantsLife);
        };

        ReturnToEditor = new(TankGame.GameLanguage.Return, FontGlobals.RebirthFont, Color.White);
        ReturnToEditor.SetDimensions(() => new(WindowUtils.WindowWidth * 0.01f, WindowUtils.WindowHeight * 0.02f), () => new Vector2(250, 50).ToResolution());

        ReturnToEditor.OnLeftClick = (l) => {
            TryOpen(false);
            IsTestingLevel = false;
            CameraGlobals.OverheadView = true;
            CampaignGlobals.InMission = false;
            // GameHandler.CleanupScene();
            if (cachedMission is { Tanks: not null, Blocks: not null, Name: not null, Note: not null })
                Mission.LoadDirectly(cachedMission);
            if (loadedCampaign is { })
                SetupMissionsBar(loadedCampaign);

            if (loadedCampaign is not null)
                _missionButtons[loadedCampaign.CurrentMissionId].Color = SelectedColor;
        };

        Perspective = new(TankGame.GameLanguage.Perspective, FontGlobals.RebirthFont, Color.White);
        Perspective.SetDimensions(() => new(WindowUtils.WindowWidth * 0.125f, WindowUtils.WindowHeight * 0.725f), () => new Vector2(200, 50).ToResolution());
        Perspective.Tooltip = TankGame.GameLanguage.PerspectiveFlavor;
        Perspective.OnLeftClick = (l) => { CameraGlobals.OverheadView = !CameraGlobals.OverheadView; };

        TerrainCategory = new(TankGame.GameLanguage.Terrain, FontGlobals.RebirthFont, Color.White);
        TerrainCategory.SetDimensions(() => new(WindowUtils.WindowWidth * 0.75f, WindowUtils.WindowHeight * 0.725f), () => new Vector2(200, 50).ToResolution());
        TerrainCategory.OnLeftClick = (l) => { CurCategory = EditorCategory.Terrain; };

        EnemyTanksCategory = new(TankGame.GameLanguage.AIControlled, FontGlobals.RebirthFont, Color.White);
        EnemyTanksCategory.SetDimensions(() => new(WindowUtils.WindowWidth * 0.875f, WindowUtils.WindowHeight * 0.725f), () => new Vector2(200, 50).ToResolution());
        EnemyTanksCategory.OnLeftClick = (l) => { CurCategory = EditorCategory.EnemyTanks; };
        PlayerTanksCategory = new(TankGame.GameLanguage.Players, FontGlobals.RebirthFont, Color.White);
        PlayerTanksCategory.SetDimensions(() => new(WindowUtils.WindowWidth * 0.875f, WindowUtils.WindowHeight * 0.65f), () => new Vector2(200, 50).ToResolution());
        PlayerTanksCategory.OnLeftClick = (l) => { CurCategory = EditorCategory.PlayerTanks; };

        AutoOrientTanks = new(TankGame.GameLanguage.AutoOrientTanks, FontGlobals.RebirthFont, Color.White);
        AutoOrientTanks.SetDimensions(() => new Vector2(WindowUtils.WindowWidth * 0.875f, WindowUtils.WindowHeight * 0.575f), 
            () => new Vector2(200, 50).ToResolution());
        AutoOrientTanks.Tooltip = TankGame.GameLanguage.AutoOrientTanksFlavor;
        AutoOrientTanks.TextScale = () => new Vector2(0.8f);
        AutoOrientTanks.OnLeftClick = (e) => {
            PlacementSquare.Placements.ForEach(p => {
                // maybe in the original game this was based off the top-left of the blocks?
                var tnkRot = WiiMap.GetAutoTankRotation(p.Position.FlattenZ());

                var flashColor = tnkRot switch {
                    // down
                    0 => Color.Blue,
                    // up
                    MathHelper.Pi => Color.Red,
                    // left
                    -MathHelper.PiOver2 => Color.Yellow,
                    // right
                    _ => Color.Green
                };
                p.FlashAsColor(flashColor);
                if (p.TankId > -1) {
                    var tank = GameHandler.AllTanks[p.TankId];
                    tank.ChassisRotation = tnkRot;

                    // :( negatives why
                    tank.DesiredChassisRotation = -tnkRot;
                    tank.TurretRotation = tnkRot;
                }

            });
        };

        Properties = new(TankGame.GameLanguage.Properties, FontGlobals.RebirthFont, Color.White);

        float width = 200;

        Properties.SetDimensions(() => new(WindowUtils.WindowWidth * 0.425f - (width / 2).ToResolutionX(), 10.ToResolutionY()), () => new Vector2(width, 50).ToResolution());
        Properties.OnLeftClick = (a) => {
            if (!_saveMenuOpen)
                EditState = LevelEditState.SavingThings;
            else
                EditState = LevelEditState.LevelEditor;
        };

        LoadLevel = new(TankGame.GameLanguage.Load, FontGlobals.RebirthFont, Color.White);

        LoadLevel.SetDimensions(() => new(WindowUtils.WindowWidth * 0.575f - (width / 2).ToResolutionX(), 10.ToResolutionY()), () => new Vector2(width, 50).ToResolution());
        LoadLevel.OnLeftClick = (a) => {
            var res = Dialog.FileOpen("mission,campaign,bin", TankGame.SaveDirectory);
            if (res.Path == null || !res.IsOk) return;

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
                    _missionButtons[0].Color = Color.SkyBlue;
                    CampaignStartingLives.Text = loadedCampaign.MetaData.StartingLives.ToString();
                }
                else if (ext == ".bin") {
                    var map = new WiiMap(res.Path);
                    ChatSystem.SendMessage($"(Width, Height): ({map.Width}, {map.Height})", Color.White);

                    WiiMap.ApplyToGameWorld(map);
                }

                ChatSystem.SendMessage($"Loaded '{Path.GetFileName(res.Path)}'.", Color.White);
            } catch (Exception e) when (!Debugger.IsAttached) {
                ChatSystem.SendMessage("Failed to load.", Color.Red);
                ChatSystem.SendMessage(e.Message, Color.Red);
            }
        };
        LoadLevel.Tooltip = TankGame.GameLanguage.LoadMissionFlavor;
        InitializeSaveMenu();
        SetLevelEditorVisibility(false);
    }
    public static void TryOpen(bool fromMainMenu = true) {
        if (fromMainMenu) {
            OpenPeripherals();
            float time = 180;
            IntermissionSystem.TimeBlack = time;
            _queueOpen = true;
            _openCountndown = time;
            CampaignGlobals.ShouldMissionsProgress = false;
        }
        else {
            Theme.Play();
            IsActive = true;
            SetLevelEditorVisibility(true);
        }
        IsEditing = true;
    }
    public static void OpenForce() {
        IsActive = true;
        CameraGlobals.OverheadView = true;
        Theme.Play();
        SetLevelEditorVisibility(true);
        loadedCampaign = new();
        loadedCampaign.CachedMissions[0] = new([], []) {
            Name = "No Name"
        };
        SetupMissionsBar(loadedCampaign);
        _missionButtons[0].Color = SelectedColor;
        _loadTask = null;
    }
    public static void Close(bool toMainMenu) {
        IsActive = false;
        IsTestingLevel = false;

        RemoveMissionButtons();

        Theme.SetVolume(0);
        Theme.Stop();
        SetLevelEditorVisibility(false);
        SetSaveMenuVisibility(false);
        // SetMissionsVisibility(false);
        if (toMainMenu) {
            //ClosePeripherals();
            IsEditing = false;
            RemoveEditButtons();
            loadedCampaign = new();
            loadedCampaign.CachedMissions[0] = new([], []) {
                Name = "No Name"
            };
            SetupMissionsBar(loadedCampaign);
        }
        PlacementSquare.ResetSquares();
    }

    // this code is god-tier atrocious. rework soon.
    static Rectangle _clickRect;

    static string _curDescription = string.Empty;
    static Rectangle _curHoverRect;

    public static Color SelectionColor = Color.NavajoWhite;
    public static Color HoverBoxColor = Color.SkyBlue;

    // FIXME: this code hurts my eyes. who wrote this?
    public static void Render(SpriteBatch sb) {
        if (!_initialized)
            return;

        // called twice since Update() isn't called when paused, rip
        AddMissionBtn.IsVisible =
            AutoOrientTanks.IsVisible = 
            RemoveMissionBtn.IsVisible =
            MoveMissionUp.IsVisible =
            MoveMissionDown.IsVisible = IsActive && !GameUI.Paused;

        ShouldDrawBarUI = !GameUI.Paused;
        SwapMenu.Text = _viewMissionDetails ? "Campaign Details" : "Mission Details";

        if (PlacementSquare.CurrentlyHovered != null) {
            var mapCoords = PlacementSquare.CurrentlyHovered.RelativePosition;

            DrawUtils.DrawStringWithBorder(TankGame.SpriteRenderer, FontGlobals.RebirthFont, mapCoords.ToString(),
                MouseUtils.MousePosition - Vector2.UnitY * 30, Color.White, Color.Black, Vector2.One * 0.5f, 0f,
            Anchor.BottomCenter, 0.5f);
        }

        var measure = FontGlobals.RebirthFont.MeasureString(AlertText);

        DrawAlerts(sb);
        if (!ShouldDrawBarUI) return;

        #region Main UI

        float xOff = 0;
        _clickRect = new(0, (int)(WindowUtils.WindowBottom.Y * 0.8f), WindowUtils.WindowWidth, (int)(WindowUtils.WindowHeight * 0.2f));
        sb.Draw(TextureGlobals.Pixels[Color.White], _clickRect, null, Color.White, 0f, Vector2.Zero, default, 0f);

        DrawTankDescriptionFlavor();

        // i feel like i could turn these panels into their own method.
        // but whatever.
        // Ryan, 2/11/25: this code is fucking archaic.

        DrawLevelInfo(sb);

        // render peripherals
        DrawCampaigns();
        DrawPlacementInfo(sb);

        if (CurCategory == EditorCategory.EnemyTanks) {
            // Note the idOffset of 1 to account for selected index logic
            // xOff = DrawCategoryRow(_renderNamesTanks, SelectedTankTier, 1);
            xOff = DrawCategoryRow(_tankCategoryParticles, TankID.Collection, SelectedTankTier, 1);
            _maxScroll = xOff;
        }
        else if (CurCategory == EditorCategory.Terrain) {
            // xOff = DrawCategoryRow(_renderNamesBlocks, SelectedBlockType, 0);
            xOff = DrawCategoryRow(_blockCategoryParticles, BlockID.Collection, SelectedBlockType, 0, 0.8f, 1.1f);
            _maxScroll = xOff;
        }
        else if (CurCategory == EditorCategory.PlayerTanks) {
            xOff = DrawCategoryRow(_plrCategoryParticles, PlayerID.Collection, SelectedPlayerType, 0);
            _maxScroll = xOff;
        }

        // TODO: Cum
        // here lies model drawing code for the level editor
        //EditorParticleSystem.Scissor = PlaceInfoRect;
        // RenderEditorParticles();

        #endregion

        // TankGame.SpriteRenderer.Draw(TextureGlobals.Pixels[Color.White], _missionButtonScissor, null, Color.Red * 0.5f, 0f, Vector2.Zero, default, 0f);

        // used to have an Active check, but since we only call this method when Active is true, don't bother
        if (HoveringAnyTank) {
            var tex = GameResources.GetGameResource<Texture2D>("Assets/textures/ui/leveledit/rotate");
            sb.Draw(tex,
                MouseUtils.MousePosition + new Vector2(20, -20).ToResolution(),
                null,
                Color.White,
                0f,
                new Vector2(0, tex.Size().Y),
                0.2f.ToResolution(),
                default,
                0f);
        }
        var txt = !_viewMissionDetails ? TankGame.GameLanguage.CampaignDetails : TankGame.GameLanguage.MissionDetails;

        if (EditState == LevelEditState.SavingThings) {
            sb.Draw(TextureGlobals.Pixels[Color.White],
                LevelContentsPanel,
                null,
                Color.Gray,
                0f,
                Vector2.Zero,
                default,
                0f);
            sb.DrawString(FontGlobals.RebirthFont,
                    txt,
                    new Vector2(LevelContentsPanel.X + LevelContentsPanel.Width / 2, LevelContentsPanel.Y + 10.ToResolutionY()),
                    Color.White,
                    Vector2.One.ToResolution(),
                    0f,
                    new Vector2(FontGlobals.RebirthFont.MeasureString(txt).X / 2, 0));
        }

        // i believe this makes the text left-origin instead of center-origin
        // TODO: make text origin in relation to the ui element a property of the UI itself..?
        _campaignTextInputs.ForEach(elem => {
            elem.UniqueDraw = (a, b) => {
                elem.DrawText = false;
                if (!elem.IsVisible)
                    return;
                // fix why this isn't drawing???
                var pos = new Vector2(elem.Position.X + 10.ToResolutionX(), elem.Position.Y + elem.Size.Y / 2);
                string text = elem.DefaultString + ": " + elem.GetRealText();
                var msr1 = FontGlobals.RebirthFontLarge.MeasureString(text);
                var msr2 = FontGlobals.RebirthFontLarge.MeasureString(elem.DefaultString);
                float constScale = 0.4f.ToResolutionX();
                float scale =  msr1.X * constScale > elem.Size.X ? msr2.X / (msr1.X + msr2.X) : constScale;
                b.DrawString(FontGlobals.RebirthFontLarge, text, pos, Color.Black, new Vector2(scale).ToResolution(), 0f, new Vector2(0, msr1.Y / 2));
            };
        });
    }
    static int GetHoveredBarIndex() {
        _clickRect = new(0, (int)(WindowUtils.WindowBottom.Y * 0.8f), WindowUtils.WindowWidth, (int)(WindowUtils.WindowHeight * 0.2f));

        // If the mouse isn't in the bottom bar at all, return -1 early
        if (!_clickRect.Contains(MouseUtils.MousePosition.ToPoint()))
            return -1;

        float startX = BAR_START_X.ToResolutionX() + _barOffset;
        float strideX = BAR_WIDTH.ToResolutionX();
        float relativeX = MouseUtils.MousePosition.X - startX;

        // Mouse is too far to the left
        if (relativeX < 0) return -1;

        // Math trick: Divide the relative mouse position by the stride to get the exact index!
        int hoveredIndex = (int)(relativeX / strideX);

        int maxItems = _categoryParticles[CurCategory].Count;

        // Ensure we aren't clicking in empty space to the right of the last p
        return hoveredIndex < maxItems ? hoveredIndex : -1;
    }
    // TODO: teleporters dont work? make holes better to view?
    static float DrawCategoryRow<T>(List<Particle> pEntries, ReflectionDictionary<T> dict, int selectedIndex, int idOffset = 0, float minScale = 6.5f, float maxScale = 8.5f) where T : class, new() {
        float xOff = 0;

        for (int i = 0; i < pEntries.Count; i++) {
            bool isSelected = selectedIndex == (i + idOffset);

            var pos = new Vector2((BAR_START_X + BAR_WIDTH / 2).ToResolutionX() + xOff + _barOffset, WindowUtils.WindowBottom.Y * 0.925f);
            pEntries[i].Position = DrawUtils.CenteredOrthoToScreen(pos).Expand();


            DrawUtils.DrawStringWithBorderAndShadow(TankGame.SpriteRenderer, FontGlobals.RebirthFontLarge, pos, Vector2.UnitY, dict.GetKey(i + idOffset)!,
                isSelected ? ColorUtils.DiscoPartyColor : Color.White, Color.Black, Vector2.One * 0.8f, 1f, Anchor.TopCenter, shadowAlpha: 0.5f);

            pEntries[i].Scale = Vector3.One * MathHelper.Lerp(pEntries[i].Scale.X, isSelected ? maxScale : minScale, 0.1f * RuntimeData.DeltaTime);

            // this code hurts me. emotionally
            xOff += BAR_WIDTH.ToResolutionX();
        }
        return xOff;
    }
    public static void Update() {
        // the code is a tad better!
        if (!_initialized) return;

        if (loadedCampaign is not null && !IsTestingLevel)
            cachedMission = loadedCampaign.CachedMissions[loadedCampaign.CurrentMissionId];

        if (_openCountndown >= 0) {
            _openCountndown -= RuntimeData.DeltaTime;
        }
        else if (_queueOpen) {
            OpenForce();
            _queueOpen = false;
        }

        HoveringAnyTank = false;

        if (!MainMenuUI.IsActive && (CameraGlobals.OverheadView || IsActive)) {
            // this used to be in the fkn loop. come on ryan.
            var mouseRay = RayUtils.GetMouseToWorldRay();

            // foreach -> for
            for (int i = 0; i < GameHandler.AllTanks.Length; i++) {
                var tnk = GameHandler.AllTanks[i];
                if (tnk == null || tnk.IsDestroyed) continue;

                if (mouseRay.Intersects(tnk.Worldbox).HasValue) {
                    HoveringAnyTank = true;
                    if (InputUtils.KeyJustPressed(Keys.K))
                        tnk.Destroy(new TankHurtContextOther(null, TankHurtContextOther.HurtContext.FromOther, "Smitten by zeus!"), false); // hmmm

                    if (InputUtils.CanDetectClick(rightClick: true)) {
                        tnk!.ChassisRotation = (tnk.ChassisRotation - MathHelper.PiOver2).WrapTauAngle() - MathHelper.Pi;
                        tnk!.DesiredChassisRotation = (tnk.DesiredChassisRotation - MathHelper.PiOver2).WrapTauAngle() - MathHelper.Pi;
                        tnk!.TurretRotation = (tnk.TurretRotation + MathHelper.PiOver2).WrapTauAngle() - MathHelper.Pi;

                        // why tf was this here???
                        // tnk!.TurretRotation = (tnk.TurretRotation + MathHelper.PiOver2).WrapTauAngle() - MathHelper.Pi;
                    }

                    tnk.IsHoveredByMouse = true;
                }
                else {
                    tnk.IsHoveredByMouse = false;
                }
            }
        }

        AddMissionBtn.IsVisible =
            RemoveMissionBtn.IsVisible =
            MoveMissionUp.IsVisible =
            MoveMissionDown.IsVisible = IsActive && !GameUI.Paused;

        if (_missionButtonScissor.Contains(MouseUtils.MousePosition))
            _missionsOffset += InputUtils.GetScrollWheelChange() * 30;

        _missionsMaxOff = _missionButtons.Count * 30.ToResolutionY();
        SaveLevelConfirm.Tooltip = _viewMissionDetails ? TankGame.GameLanguage.MissionSaveFlavor : TankGame.GameLanguage.CampaignSaveFlavor;

        CampaignMajorVictory.Text = TankGame.GameLanguage.HasMajorVictoryTheme + ": " + (_hasMajorVictory ? TankGame.GameLanguage.Yes : TankGame.GameLanguage.No);

        if (loadedCampaign is not null)
            MissionGrantsLife.Text = TankGame.GameLanguage.GrantsBonusLife + ": " + (loadedCampaign.CachedMissions[loadedCampaign.CurrentMissionId].GrantsExtraLife ? TankGame.GameLanguage.Yes : TankGame.GameLanguage.No);

        if (_missionsOffset > 0)
            _missionsOffset = 0;
        else if (-_missionsMaxOff < -_missionButtonScissor.Height && _missionsOffset < -_missionsMaxOff + _missionButtonScissor.Height)
            _missionsOffset = -_missionsMaxOff + _missionButtonScissor.Height;

        LevelContentsPanel = new Rectangle(WindowUtils.WindowWidth / 4, (int)(WindowUtils.WindowHeight * 0.1f), WindowUtils.WindowWidth / 2, (int)(WindowUtils.WindowHeight * 0.625f));
        PlacementSquare.PlacesBlock = CurCategory == EditorCategory.Terrain;

        // much better code now
        switch (CurCategory) {
            case EditorCategory.EnemyTanks:
                EnemyTanksCategory.Color = Color.DeepSkyBlue;
                TerrainCategory.Color = Color.White;
                PlayerTanksCategory.Color = Color.White;
                break;
            case EditorCategory.Terrain:
                EnemyTanksCategory.Color = Color.White;
                TerrainCategory.Color = Color.DeepSkyBlue;
                PlayerTanksCategory.Color = Color.White;
                break;
            case EditorCategory.PlayerTanks:
                EnemyTanksCategory.Color = Color.White;
                TerrainCategory.Color = Color.White;
                PlayerTanksCategory.Color = Color.DeepSkyBlue;
                break;
        }
        if (IsActive) {
            if (TankGame.Instance.IsActive)
                UpdateParticles();

            // prevent any gameplay via this set to 190
            IntermissionHandler.TankFunctionWait = 190;
            if (DebugManager.DebuggingEnabled)
                if (InputUtils.KeyJustPressed(Keys.T))
                    PlacementSquare.DrawStacks = !PlacementSquare.DrawStacks;

            Theme.SetVolume(0.4f * TankGame.Settings.MusicVolume);

            _curDescription = string.Empty;
            _curHoverRect = new();

            // much betta
            int hoveredIndex = GetHoveredBarIndex();

            if (hoveredIndex != -1) {
                float xOff = hoveredIndex * BAR_WIDTH.ToResolutionX();
                _curHoverRect = new Rectangle(
                    (int)(BAR_START_X.ToResolutionX() + xOff + _barOffset),
                    (int)(WindowUtils.WindowBottom.Y * 0.8f),
                    (int)BAR_WIDTH.ToResolutionX(),
                    (int)(WindowUtils.WindowHeight * 0.2f)
                );

                _curDescription = CurCategory switch {
                    EditorCategory.EnemyTanks => GetTankFlavor(hoveredIndex + 1),
                    EditorCategory.Terrain => GetBlockFlavor(hoveredIndex),
                    EditorCategory.PlayerTanks => GetPlayerFlavor(hoveredIndex),
                    _ => string.Empty
                };
            }

            if (InputUtils.CanDetectClick()) {
                _origClick = MouseUtils.MousePosition - new Vector2(_barOffset, 0);

                if (hoveredIndex != -1) {
                    if (CurCategory == EditorCategory.EnemyTanks)
                        SelectedTankTier = hoveredIndex + 1;
                    else if (CurCategory == EditorCategory.Terrain)
                        SelectedBlockType = hoveredIndex;
                    else if (CurCategory == EditorCategory.PlayerTanks)
                        SelectedPlayerType = hoveredIndex;
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

            BlockStack = MathHelper.Clamp(BlockStack, 1, 7);

            if (CurCategory == EditorCategory.EnemyTanks || CurCategory == EditorCategory.PlayerTanks) {
                // tank place handling, etc
                if (InputUtils.KeyJustPressed(Keys.Up))
                    SelectedTankTeam--;
                if (InputUtils.KeyJustPressed(Keys.Down))
                    SelectedTankTeam++;

                SelectedTankTeam = MathHelper.Clamp(SelectedTankTeam, TeamID.NoTeam, TeamID.Magenta);
            }
            else if (CurCategory == EditorCategory.Terrain) {
                if (InputUtils.KeyJustPressed(Keys.Up))
                    BlockStack++;
                if (InputUtils.KeyJustPressed(Keys.Down))
                    BlockStack--;
                if (SelectedBlockType == BlockID.Hole || SelectedBlockType == BlockID.Teleporter)
                    BlockStack = 1;

                BlockStack = MathHelper.Clamp(BlockStack, 1, 7);
            }
        }
        else if (IsEditing && !IsActive && cachedMission != default && CampaignGlobals.InMission)
            if (IntermissionHandler.NothingCanHappenAnymore(cachedMission, out _))
                QueueEditorReEntry(120f);

        ReturnToEditor.IsVisible = IsEditing && !IsActive && !MainMenuUI.IsActive;
    }

    static string GetTankFlavor(int id) {
        return id switch {
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
            _ => "Did Not Load (DNL)"
        };
    }

    static string GetBlockFlavor(int id) {
        return id switch {
            BlockID.Wood => TankGame.GameLanguage.WoodFlavor,
            BlockID.Cork => TankGame.GameLanguage.CorkFlavor,
            BlockID.Hole => TankGame.GameLanguage.HoleFlavor,
            _ => "Did Not Load (DNL)"
        };
    }

    static string GetPlayerFlavor(int id) {
        return id switch {
            PlayerID.Blue => TankGame.GameLanguage.P1TankFlavor,
            PlayerID.Red => TankGame.GameLanguage.P2TankFlavor,
            PlayerID.Green => TankGame.GameLanguage.P3TankFlavor,
            PlayerID.Yellow => TankGame.GameLanguage.P4TankFlavor,
            _ => "Did Not Load (DNL)"
        };
    }

    static float _waitTime;
    static bool _isWaiting;

    static void QueueEditorReEntry(float delay) {
        if (!_isWaiting)
            _waitTime = delay;
        _isWaiting = true;

        _waitTime -= RuntimeData.DeltaTime;
        if (_waitTime < 0) {
            ReturnToEditor?.OnLeftClick?.Invoke(null);
            _waitTime = 0;
            _isWaiting = false;
        }
    }
}