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
    public enum UICategory {
        LevelEditor,
        SavingThings,
    }
    public static readonly byte[] LevelFileHeader = [84, 65, 78, 75]; // T, A, N, K
    public const int EDITOR_VERSION = 4;

    public const int MAX_MISSION_CHARS = 30;
    public static bool IsTestingLevel { get; private set; }

    public static bool IsActive { get; private set; }
    public static OggMusic Theme = new("Level Editor Theme", "Content/Assets/music/mainmenu/editor.ogg", 0.7f);

    public static Category CurCategory { get; private set; }
    public static int SelectedTankTier { get; private set; }
    public static int SelectedTankTeam { get; private set; }
    public static int SelectedPlayerType { get; private set; }
    public static int SelectedBlockType { get; private set; }
    public static int BlockHeight { get; private set; }
    public static bool IsEditing { get; internal set; }
    public static bool HoveringAnyTank;

    internal static Mission cachedMission;
    public enum Category {
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

    internal static Mission missionToRate = new([], []);

    static bool _sdbui;

    static bool _queueOpen;
    static float _openCountndown;
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

    private static List<UITextButton> _missionButtons = [];
    private static List<UITextButton> _listModifyButtons = [];
    private static Rectangle _missionTab = new(0, 150, 350, 535);
    private static Rectangle _missionButtonScissor;
    private static float _missionsOffset;
    private static float _missionsMaxOff;

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
                RenderTextures.Add(fileName, GameResources.GetGameResource<Texture2D>(file, false, false));
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

                if (RenderTextures.ContainsKey(nTL + "Plr"))
                    _renderNamesPlayers.Add(nTL + "Plr");
            }
        }

        #endregion

        _initialized = true;

        AddMissionBtn = new UITextButton("+", FontGlobals.RebirthFont, Color.White, () => Vector2.One.ToResolution()) {
            Tooltip = "Insert a blank mission after the currently selected mission."
        };
        AddMissionBtn.SetDimensions(
            () => new Vector2(_missionButtonScissor.X + 15.ToResolutionX(), _missionButtonScissor.Y + _missionButtonScissor.Height + 5.ToResolutionY()),
            () => new Vector2(_missionButtonScissor.Width / 4.5f, 25.ToResolutionY()));
        AddMissionBtn.OnLeftClick = (a) => {
            AddMission();
        };
        _listModifyButtons.Add(AddMissionBtn);

        RemoveMissionBtn = new UITextButton("-", FontGlobals.RebirthFont, Color.White, () => Vector2.One.ToResolution()) {
            Tooltip = "Remove this mission from the missions list."
        };
        RemoveMissionBtn.SetDimensions(
            () => new Vector2(AddMissionBtn.Position.X + AddMissionBtn.Size.X, AddMissionBtn.Position.Y),
            () => new Vector2(AddMissionBtn.Size.X, AddMissionBtn.Size.Y));
        RemoveMissionBtn.OnLeftClick = (a) => {
            RemoveMission();
        };
        _listModifyButtons.Add(RemoveMissionBtn);

        MoveMissionUp = new UITextButton("v", FontGlobals.RebirthFont, Color.White, () => Vector2.One.ToResolution()) {
            Tooltip = "Swap the currently selected mission with the one above it.",
            TextRotation = MathHelper.Pi
        };
        MoveMissionUp.SetDimensions(
            () => new(RemoveMissionBtn.Position.X + RemoveMissionBtn.Size.X, RemoveMissionBtn.Position.Y),
            () => new Vector2(AddMissionBtn.Size.X, RemoveMissionBtn.Size.Y));
        MoveMissionUp.OnLeftClick = (a) => { MoveMission(true); };
        _listModifyButtons.Add(MoveMissionUp);

        MoveMissionDown = new UITextButton("v", FontGlobals.RebirthFont, Color.White, () => Vector2.One.ToResolution()) {
            Tooltip = "Swap the currently selected mission with the one below it.",
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
        TerrainCategory.OnLeftClick = (l) => { CurCategory = Category.Terrain; };

        EnemyTanksCategory = new(TankGame.GameLanguage.AIControlled, FontGlobals.RebirthFont, Color.White);
        EnemyTanksCategory.SetDimensions(() => new(WindowUtils.WindowWidth * 0.875f, WindowUtils.WindowHeight * 0.725f), () => new Vector2(200, 50).ToResolution());
        EnemyTanksCategory.OnLeftClick = (l) => { CurCategory = Category.EnemyTanks; };
        PlayerTanksCategory = new(TankGame.GameLanguage.Players, FontGlobals.RebirthFont, Color.White);
        PlayerTanksCategory.SetDimensions(() => new(WindowUtils.WindowWidth * 0.875f, WindowUtils.WindowHeight * 0.65f), () => new Vector2(200, 50).ToResolution());
        PlayerTanksCategory.OnLeftClick = (l) => { CurCategory = Category.PlayerTanks; };

        AutoOrientTanks = new(TankGame.GameLanguage.AutoOrientTanks, FontGlobals.RebirthFont, Color.White);
        AutoOrientTanks.SetDimensions(() => new Vector2(WindowUtils.WindowWidth * 0.875f, WindowUtils.WindowHeight * 0.575f), 
            () => new Vector2(200, 50).ToResolution());
        AutoOrientTanks.Tooltip = TankGame.GameLanguage.AutoOrientTanksFlavor;
        AutoOrientTanks.TextScale = () => new Vector2(0.8f);
        AutoOrientTanks.OnLeftClick = (e) => {
            PlacementSquare.Placements.ForEach(p => {

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
                    tank.TankRotation = tnkRot;

                    // :( negatives why
                    tank.TargetTankRotation = -tnkRot;
                    tank.TurretRotation = tnkRot;
                }

            });
        };

        Properties = new(TankGame.GameLanguage.Properties, FontGlobals.RebirthFont, Color.White);

        float width = 200;

        Properties.SetDimensions(() => new(WindowUtils.WindowWidth * 0.425f - (width / 2).ToResolutionX(), 10.ToResolutionY()), () => new Vector2(width, 50).ToResolution());
        Properties.OnLeftClick = (a) => {
            if (!_saveMenuOpen)
                GUICategory = UICategory.SavingThings;
            else
                GUICategory = UICategory.LevelEditor;
        };

        LoadLevel = new(TankGame.GameLanguage.Load, FontGlobals.RebirthFont, Color.White);

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
            }
            return;
        };
        // TODO: non-windows support. i am lazy. fuck this. also localize bozo
        LoadLevel.Tooltip = "Will open a file dialog for\nyou to choose what mission/campaign to load.";
        InitializeSaveMenu();
        SetLevelEditorVisibility(false);
    }
    public static void TryOpen(bool fromMainMenu = true) {
        if (fromMainMenu) {
            //OpenPeripherals();
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
    private static Rectangle _clickRect;
    private static readonly Dictionary<Rectangle, (int, string)> ClickEventsPerItem = []; // hover zone, id, description

    private static string _curDescription = string.Empty;
    private static Rectangle _curHoverRect;

    public static Color SelectionColor = Color.NavajoWhite;
    public static Color HoverBoxColor = Color.SkyBlue;

    // FIXME: this code hurts my eyes. who wrote this?
    public static void Render() {
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

            DrawUtils.DrawTextWithBorder(TankGame.SpriteRenderer, FontGlobals.RebirthFont, mapCoords.ToString(),
                MouseUtils.MousePosition - Vector2.UnitY * 30, Color.White, Color.Black, Vector2.One * 0.5f, 0f,
            Anchor.BottomCenter, 0.5f);
        }

        var measure = FontGlobals.RebirthFont.MeasureString(AlertText);

        DrawAlerts();
        if (!ShouldDrawBarUI) return;

        #region Main UI

        int xOff = 0;
        _clickRect = new(0, (int)(WindowUtils.WindowBottom.Y * 0.8f), WindowUtils.WindowWidth, (int)(WindowUtils.WindowHeight * 0.2f));
        TankGame.SpriteRenderer.Draw(TextureGlobals.Pixels[Color.White], _clickRect, null, Color.White, 0f, Vector2.Zero, default, 0f);

        DrawTankDescriptionFlavor();

        // i feel like i could turn these panels into their own method.
        // but whatever.
        // Ryan, 2/11/25: this code is fucking archaic.

        DrawLevelInfo();

        // render peripherals
        DrawCampaigns();
        DrawPlacementInfo();
        // render teams
        //if (CurCategory == Category.Blocks)
        //TankGame.SpriteRenderer.DrawString(FontGlobals.RebirthFont, $"Block Stack: {BlockHeight}", new Vector2(WindowUtils.WindowWidth - 335.ToResolutionX(), 40.ToResolutionY()), Color.White, Vector2.One.ToResolution(), 0f, Vector2.Zero);

        if (CurCategory == Category.EnemyTanks) {
            for (int i = 0; i < _renderNamesTanks.Count; i++) {
                // TODO: i come back to this code and i think "what kind of drugs was ryan on?" to my surprise i have no clue.
                // the magic numbers here hurt my brain.
                // 1/22/2025 ryan here: WHAT THE ACTUAL SHiT? weed couldn't even solve my problems atp
                ClickEventsPerItem[new Rectangle((int)(34.ToResolutionX() + xOff + _barOffset), (int)(WindowUtils.WindowBottom.Y * 0.8f), (int)234.ToResolutionX(), (int)(WindowUtils.WindowHeight * 0.2f))] =
                    (i + 1, (i + 1) switch {
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
                    }); // TODO: localize this. i hate english.

                TankGame.SpriteRenderer.Draw(RenderTextures[_renderNamesTanks[i]],
                    new Vector2(24.ToResolutionX() + xOff + _barOffset, WindowUtils.WindowBottom.Y * 0.75f),
                    null,
                    // -1 offset since we have none at id 0
                    SelectedTankTier - 1 == i ? SelectionColor : Color.White,
                    0f,
                    Vector2.Zero,
                    Vector2.One.ToResolution(),
                    default,
                    0f);
                // this code hurts me. emotionally
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
                    SelectedBlockType == i ? SelectionColor : Color.White,
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
                        PlayerID.Green => TankGame.GameLanguage.P3TankFlavor,
                        PlayerID.Yellow => TankGame.GameLanguage.P4TankFlavor,
                        _ => "Did Not Load (DNL)"
                    });

                TankGame.SpriteRenderer.Draw(RenderTextures[_renderNamesPlayers[i]],
                    new Vector2(24.ToResolutionX() + xOff + _barOffset, WindowUtils.WindowBottom.Y * 0.75f),
                    null,
                    SelectedPlayerType == i ? SelectionColor : Color.White,
                    0f,
                    Vector2.Zero,
                    Vector2.One.ToResolution(),
                    default,
                    0f);
                xOff += (int)234.ToResolutionX();
            }
            _maxScroll = xOff;
        }

        // TODO: Cum
        // here lies model drawing code for the level editor
        //EditorParticleSystem.Scissor = PlaceInfoRect;
        // RenderEditorParticles();

        if (DebugManager.DebuggingEnabled) {
            int a = 0;
            foreach (var thing in ClickEventsPerItem) {
                if (DebugManager.DebugLevel == 3)
                    TankGame.SpriteRenderer.Draw(TextureGlobals.Pixels[Color.White], thing.Key, null, Color.Red * 0.5f, 0f, Vector2.Zero, default, 0f);
                var text = thing.Key.Contains(MouseUtils.MousePosition.ToPoint()) ? $"{thing.Key} ---- {thing.Value.Item1} (HOVERED)" : $"{thing.Key} ---- {thing.Value.Item1}";
                DebugManager.DrawDebugString(TankGame.SpriteRenderer, text, new Vector2(500, 20 + a), 3);
                a += 20;
            }
        }

        #endregion

        // TankGame.SpriteRenderer.Draw(TextureGlobals.Pixels[Color.White], _missionButtonScissor, null, Color.Red * 0.5f, 0f, Vector2.Zero, default, 0f);

        // used to have an Active check, but since we only call this method when Active is true, don't bother
        if (HoveringAnyTank) {
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
        var txt = !_viewMissionDetails ? TankGame.GameLanguage.CampaignDetails : TankGame.GameLanguage.MissionDetails;

        if (GUICategory == UICategory.SavingThings) {
            TankGame.SpriteRenderer.Draw(TextureGlobals.Pixels[Color.White],
                LevelContentsPanel,
                null,
                Color.Gray,
                0f,
                Vector2.Zero,
                default,
                0f);
            TankGame.SpriteRenderer.DrawString(FontGlobals.RebirthFont,
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

    public static void Update() {

        // it honestly hurts to look at this code. pls refactor soon
        // With love,
        //              Ryan
        if (!_initialized)
            return;

        if (loadedCampaign is not null && !IsTestingLevel)
            cachedMission = loadedCampaign.CachedMissions[loadedCampaign.CurrentMissionId];

        if (_openCountndown >= 0) {
            _openCountndown -= RuntimeData.DeltaTime;
        } else if (_queueOpen) {
            OpenForce();
            _queueOpen = false;
        }

        HoveringAnyTank = false;
        // TODO: why is this here and not LevelEditor
        // ... or literally anywhere else
        if (!MainMenuUI.IsActive && (CameraGlobals.OverheadView || IsActive)) {
            foreach (var tnk in GameHandler.AllTanks) {
                if (tnk == null) continue;

                if (tnk.Dead)
                    continue;

                if (RayUtils.GetMouseToWorldRay().Intersects(tnk.Worldbox).HasValue) {
                    HoveringAnyTank = true;
                    if (InputUtils.KeyJustPressed(Keys.K))
                        tnk.Destroy(new TankHurtContextOther(null, TankHurtContextOther.HurtContext.FromOther, "Smitten by zeus!"), false); // hmmm

                    if (InputUtils.CanDetectClick(rightClick: true)) {
                        tnk!.TankRotation = (tnk.TankRotation - MathHelper.PiOver2).WrapTauAngle() - MathHelper.Pi;
                        tnk!.TargetTankRotation = (tnk.TargetTankRotation - MathHelper.PiOver2).WrapTauAngle() - MathHelper.Pi;
                        tnk!.TurretRotation = (tnk.TurretRotation + MathHelper.PiOver2).WrapTauAngle() - MathHelper.Pi;

                        // why tf was this here???
                        // tnk!.TurretRotation = (tnk.TurretRotation + MathHelper.PiOver2).WrapTauAngle() - MathHelper.Pi;
                    }

                    tnk.IsHoveredByMouse = true;
                }
                else
                    tnk.IsHoveredByMouse = false;
            }
        }

        if (missionToRate == MainMenuUI.curMenuMission)
            ReturnToEditor.OnLeftClick?.Invoke(null!);

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
        PlacementSquare.PlacesBlock = CurCategory == Category.Terrain;

        // the fact that i wrote this code should literally give me cancer and herpes
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
                if (InputUtils.KeyJustPressed(Keys.Up))
                    SelectedTankTeam--;
                if (InputUtils.KeyJustPressed(Keys.Down))
                    SelectedTankTeam++;
                if (SelectedTankTeam > TeamID.Magenta)
                    SelectedTankTeam = TeamID.Magenta;
                if (SelectedTankTeam < TeamID.NoTeam)
                    SelectedTankTeam = TeamID.NoTeam;
            }
            else if (CurCategory == Category.Terrain) {
                if (InputUtils.KeyJustPressed(Keys.Up))
                    BlockHeight++;
                if (InputUtils.KeyJustPressed(Keys.Down))
                    BlockHeight--;
                if (SelectedBlockType == BlockID.Hole || SelectedBlockType == BlockID.Teleporter)
                    BlockHeight = 1;

                BlockHeight = MathHelper.Clamp(BlockHeight, 1, 7);
            }
            ClickEventsPerItem.Clear();
        }
        else if (IsEditing && !IsActive && cachedMission != default && CampaignGlobals.InMission)
            if (IntermissionHandler.NothingCanHappenAnymore(cachedMission, out bool victory))
                QueueEditorReEntry(120f);
        // if (ReturnToEditor != null)
        ReturnToEditor.IsVisible = IsEditing && !IsActive && !MainMenuUI.IsActive;
    }

    private static float _waitTime;
    private static bool _isWaiting;

    private static void QueueEditorReEntry(float delay) {
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