using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.Globals.Assets;
using TanksRebirth.GameContent.ID;
using TanksRebirth.GameContent.RebirthUtils;
using TanksRebirth.GameContent.Systems.LevelSystem;
using TanksRebirth.GameContent.Tanks;
using TanksRebirth.GameContent.Tanks.AI;
using TanksRebirth.GameContent.UI.LevelEditor;
using TanksRebirth.Graphics;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Internals.UI;

namespace TanksRebirth.GameContent.Systems.Coordinates;

// idea: probably change it from being a model to a quad?
/// <summary>
/// The level editor's clickable square over one <see cref="MapCell"/> of <see cref="MapGrid.Current"/>.
/// </summary>
public class EditorTile {
    static MapGrid? _builtFor;
    // Drag-and-Drop
    
    public static bool DrawStacks { get; set; } = true;
    public static bool IsPlacing { get; private set; }
    public bool HasItem => BlockId > -1 || TankId > -1;

    public static EditorTile? CurrentlyHovered;

    public static bool DisplayHeights = true;

    /// <summary>One square per cell of <see cref="MapGrid.Current"/>, in the same order (row by row): <c>AllTiles[cell.Index]</c>.</summary>
    public static List<EditorTile> AllTiles = [];

    public Color SquareColor = Color.White;

    /// <summary>World position of the center of this square's <see cref="MapCell"/>.</summary>
    public Vector3 Position { get; }

    // flat_face.fbx is 32 units across
    const float FlatFaceModelSize = 32f;

    BoundingBox _box;

    readonly Model _model;

    public float Alpha;

    public static bool AutoAlphaHandle = true;
    public bool IsHovered => _hovered;

    bool _hovered;
    static Ray _mouseRay;

    static readonly string[] _numberText = BuildByteText(string.Empty);
    static readonly string[] _teleporterText = BuildByteText("TP:");
    static readonly Dictionary<int, string> _idText = [];

    public static bool PlacesBlock; // if false, tanks will be placed

    /// <summary>The grid cell this square sits on.</summary>
    public MapCell Cell { get; }

    /// <summary>The <see cref="Tank.WorldId"/> of the tank on this square, or -1 (stored on <see cref="Cell"/>).</summary>
    public int TankId {
        get => Cell.TankId;
        set => Cell.TankId = value;
    }
    /// <summary>The <see cref="Block.Id"/> of the block on this square, or -1 (stored on <see cref="Cell"/>).</summary>
    public int BlockId {
        get => Cell.BlockId;
        set => Cell.BlockId = value;
    }

    private Action<EditorTile>? _onClick = null;

    public bool HasBlock; // if false, a tank exists here

    public readonly int Id;

    float _flashTime;

    public Matrix World;
    public Matrix View;
    public Matrix Projection;

    EditorTile(MapCell cell) {
        Cell = cell;
        Position = cell.Position3D;
        var half = cell.Grid.CellSize / 2;
        _box = new(Position - new Vector3(half, 0, half), Position + new Vector3(half, 0, half));

        _model = ModelGlobals.FlatFace.Asset;

        Id = AllTiles.Count;

        AllTiles.Add(this);
    }
    /// <summary>Makes a square for every cell of <see cref="MapGrid.Current"/>. Does nothing if they already match it.</summary>
    public static void InitializeLevelEditorSquares() {
        if (_builtFor == MapGrid.Current)
            return;
        RebuildSquares();
    }
    /// <summary>Throws the squares away and makes new ones for <see cref="MapGrid.Current"/>, and is called when the grid changes.</summary>
    public static void RebuildSquares() {
        AllTiles.Clear();
        CurrentlyHovered = null;
        foreach (var cell in MapGrid.Current) {
            new EditorTile(cell) {
                _onClick = (place) => {
                    if (!place.HasItem)
                        place.DoPlacementAction(true);
                    else
                        place.DoPlacementAction(false);
                },
            };
        }
        _builtFor = MapGrid.Current;
    }
    /// <summary>Forgets what's placed on every square (doesn't remove anything from the game).</summary>
    public static void ResetSquares() {
        MapGrid.Current.Forget();
    }
    // TODO: need a sound for placement

    /// <summary>The square of the cell <paramref name="pos"/> is inside of, or null if it's off the grid.</summary>
    /// <param name="pos">The position to check from.</param>
    /// <returns>The square under the position.</returns>
    public static EditorTile? GetFromClosest(Vector3 pos) {
        var cell = MapGrid.Current.CellAt(pos);
        return cell is null ? null : At(cell);
    }

    /// <summary>The square on cell (<paramref name="x"/>, <paramref name="y"/>), or null if that's off the grid.</summary>
    public static EditorTile? At(int x, int y)
        => MapGrid.Current.TryGetCell(x, y, out var cell) ? At(cell) : null;

    /// <summary>The square on <paramref name="cell"/>. Is null if the squares haven't been made for its grid.</summary>
    public static EditorTile? At(MapCell cell) {
        InitializeLevelEditorSquares();
        var index = cell.Index;
        return cell.Grid == _builtFor && index < AllTiles.Count ? AllTiles[index] : null;
    }

    /// <summary>
    /// Does default block placement/removal.
    /// </summary>
    /// <param name="place">Whether or not to place the block or remove the block. If false, the block is removed.</param>
    public void DoPlacementAction(bool place) {
        if (UIElement.GetElementsAt(MouseUtils.MousePosition).Count > 0 || LevelEditorUI.EditState == LevelEditorUI.LevelEditState.SavingThings)
            return;
        if (PlacesBlock) {
            if (!HasBlock && HasItem)
                return;

            if (place) {
                var block = new Block(LevelEditorUI.IsActive ? LevelEditorUI.SelectedBlockType : DebugManager.blockType, LevelEditorUI.IsActive ? LevelEditorUI.BlockStack : DebugManager.blockHeight, Position.FlattenZ());
                BlockId = block.Id;

                HasBlock = true;
                IsPlacing = true;
            }
            else {
                if (BlockId > -1)
                    Block.AllBlocks[BlockId]?.Remove();
                if (TankId > -1)
                    GameHandler.AllTanks[TankId]?.Remove(true);
                BlockId = -1;
                TankId = -1;

                HasBlock = true;
                IsPlacing = false;
            }
            LevelEditorUI.difficultyRating = DifficultyAlgorithm.GetDifficulty(Mission.GetCurrent());
        }
        else {
            // var team = LevelEditor.Active ? LevelEditor.SelectedTankTeam : (TankTeam)GameHandler.tankToSpawnTeam;

            if (HasBlock && HasItem)
                return;

            if (!HasBlock && HasItem) {
                // FIXME: why does this even craaaash?
                GameHandler.AllTanks[TankId].Remove(true);
                TankId = -1;
                LevelEditorUI.difficultyRating = DifficultyAlgorithm.GetDifficulty(Mission.GetCurrent());
                return;
            }

            var team = LevelEditorUI.IsActive ? LevelEditorUI.SelectedTankTeam : DebugManager.tankToSpawnTeam;
            if (LevelEditorUI.CurCategory == LevelEditorUI.EditorCategory.EnemyTanks) {
                if (LevelEditorUI.IsActive && LevelEditorUI.SelectedTankTier < TankID.Brown)
                    return;
                if (AIManager.CountAll() >= GameHandler.MAX_AI_TANKS) {
                    LevelEditorUI.Alert("You are at enemy tank capacity!");
                    return;
                }
                var tnk = DebugManager.SpawnTankAt(Position, LevelEditorUI.IsActive ? LevelEditorUI.SelectedTankTier : DebugManager.tankToSpawnType, team); // todo: finish
                HasBlock = false;
                TankId = tnk.WorldId;
            }
            else {
                var type = LevelEditorUI.IsActive ? LevelEditorUI.SelectedPlayerType : PlayerID.Blue;

                var idx = Array.FindIndex(GameHandler.AllPlayerTanks, x => x is not null && x.PlayerType == type);
                if (idx > -1) {
                    LevelEditorUI.Alert($"This color player tank already exists! (ID {idx})");
                    return;
                }

                var me = DebugManager.SpawnMe(type, team);
                TankId = me.WorldId;
            }
            LevelEditorUI.difficultyRating = DifficultyAlgorithm.GetDifficulty(Mission.GetCurrent());
        }
    }

    // prevents new strings from being created willy nilly
    // i do believe these strings were a massive issue when it came to garbage collection when in the level editor
    static string[] BuildByteText(string prefix) {
        var text = new string[256];
        for (int i = 0; i < text.Length; i++)
            text[i] = prefix + i;
        return text;
    }
    static string IdText(int id) {
        if (!_idText.TryGetValue(id, out var text)) {
            text = "ID: " + id;
            _idText[id] = text;
        }
        return text;
    }

    public static void UpdateAll() {
        _mouseRay = RayUtils.GetMouseToWorldRay();
        for (int i = 0; i < AllTiles.Count; i++)
            AllTiles[i]?.Update();
    }
    public static void RenderAll() {
        if (UIElement.GetElementsAt(MouseUtils.MousePosition).Count > 0)
            return;

        _mouseRay = RayUtils.GetMouseToWorldRay();
        var view = CameraGlobals.GameView;
        var projection = CameraGlobals.GameProjection;
        for (int i = 0; i < AllTiles.Count; i++)
            AllTiles[i]?.Render(view, projection);
    }

    void Update() {
        _hovered = _mouseRay.Intersects(_box).HasValue;

        if (!_hovered) {
            if (_flashTime > 0) {
                _flashTime -= 0.01f * RuntimeData.DeltaTime;
                Alpha = _flashTime * 0.5f;
                if (_flashTime <= 0) {
                    AutoAlphaHandle = true;
                    SquareColor = Color.White;
                }
            }
            return;
        }
        CurrentlyHovered = this;
        if (InputUtils.Click())
            _onClick?.Invoke(this);

        if (!InputUtils.MouseLeft) return;

        if (PlacesBlock) {
            if (IsPlacing) {
                if (!HasItem) {
                    DoPlacementAction(true);
                }
            }
            else {
                if (HasItem) {
                    DoPlacementAction(false);
                }
            }
        }
        if (BlockId > -1) {
            if (PlacesBlock) {
                if (Block.AllBlocks[BlockId] is null)
                    BlockId = -1;
            }
        }
        else if (TankId > -1) {
            if (GameHandler.AllTanks[TankId] is null)
                TankId = -1;
        }
    }
    /// <summary>The color must be a pre-defined color within <see cref="Color"/>.</summary>
    public void FlashAsColor(Color c) {
        _flashTime = 1f;
        AutoAlphaHandle = false;
        SquareColor = c;
    }

    void Render(in Matrix view, in Matrix projection) {
        _hovered = _mouseRay.Intersects(_box).HasValue;

        World = Matrix.CreateScale(Cell.Grid.CellSize / FlatFaceModelSize) * Matrix.CreateTranslation(Position + new Vector3(0, 0.1f, 0));
        View = view;
        Projection = projection;

        if (AutoAlphaHandle)
            Alpha = _hovered ? 0.7f : 0f;

        if (Alpha > 0f) {
            var texture = TextureGlobals.Pixels[SquareColor];
            foreach (var mesh in _model.Meshes) {
                foreach (BasicEffect effect in mesh.Effects) {
                    effect.World = World;
                    effect.View = View;
                    effect.Projection = Projection;

                    effect.TextureEnabled = true;
                    effect.Texture = texture;

                    effect.Alpha = Alpha;
                    effect.SetDefaultGameLighting_IngameEntities();
                }
                mesh.Draw();
            }
        }

        if (!DrawStacks) return;

        if (BlockId > -1) {
            BlockDisplay();
        }
        else if (TankId > -1) {
            TankDisplay();
        }
    }

    void BlockDisplay() {
        var block = Block.AllBlocks[BlockId];
        if (!DisplayHeights || block is null) return;

        if (block.Properties.CanStack) {
            var pos = MatrixUtils.ConvertWorldToScreen(Vector3.Zero, World, View, Projection);

            DrawUtils.DrawStringWithBorder(TankGame.SpriteRenderer, FontGlobals.RebirthFont, _numberText[block.Stack], pos, Color.White, Color.Black,
                new Vector2(CameraGlobals.AddativeZoom * 1.5f).ToResolution(), 0f, Anchor.Center);
        }
        if (block.Type == BlockID.Teleporter) {
            var pos = MatrixUtils.ConvertWorldToScreen(Vector3.Zero, World, View, Projection);

            DrawUtils.DrawStringWithBorder(TankGame.SpriteRenderer, FontGlobals.RebirthFont, _teleporterText[block.TpLink], pos, Color.White, Color.Black,
                new Vector2(CameraGlobals.AddativeZoom).ToResolution(), 0f, Anchor.Center, borderThickness: 0.75f);
        }
    }
    void TankDisplay() {
        var tank = GameHandler.AllTanks[TankId];
        if (tank is null) return;

        var pos = MatrixUtils.ConvertWorldToScreen(Vector3.Zero, World, View, Projection);
        var teamColor = TeamID.TeamColors[tank.Team];

        DrawUtils.DrawStringWithBorder(TankGame.SpriteRenderer, FontGlobals.RebirthFont, LevelEditorUI.TeamColorsLocalized[tank.Team], pos - new Vector2(0, 8).ToResolution(), teamColor, Color.Black, new Vector2(0.9f).ToResolution() * CameraGlobals.AddativeZoom, 0f, Anchor.Center);

        if (tank is AITank ai)
            DrawUtils.DrawStringWithBorder(TankGame.SpriteRenderer, FontGlobals.RebirthFont, IdText(ai.AITankId), pos + new Vector2(0, 8), teamColor, Color.Black, new Vector2(0.8f).ToResolution() * CameraGlobals.AddativeZoom, 0f, Anchor.Center);
        else if (tank is PlayerTank player)
            DrawUtils.DrawStringWithBorder(TankGame.SpriteRenderer, FontGlobals.RebirthFont, IdText(player.PlayerId), pos + new Vector2(0, 8), teamColor, Color.Black, new Vector2(0.8f).ToResolution() * CameraGlobals.AddativeZoom, 0f, Anchor.Center);
    }
}