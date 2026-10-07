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

public class PlacementSquare {
    private static bool _initialized;
    // Drag-and-Drop
    
    public static bool DrawStacks { get; set; } = true;
    public static bool IsPlacing { get; private set; }
    public bool HasItem => BlockId > -1 || TankId > -1;

    public static PlacementSquare? CurrentlyHovered;

    public static bool displayHeights = true;

    public static List<PlacementSquare> Placements = [];

    public Color SquareColor = Color.White;

    public Vector3 Position { get; set; }

    BoundingBox _box;

    Model _model;

    public float Alpha;

    public static bool AutoAlphaHandle = true;
    public bool IsHovered => _hovered;

    bool _hovered;
    static Ray _mouseRay;

    static readonly string[] _numberText = BuildByteText(string.Empty);
    static readonly string[] _teleporterText = BuildByteText("TP:");
    static readonly Dictionary<int, string> _idText = [];

    public static bool PlacesBlock; // if false, tanks will be placed

    public int TankId = -1;
    public int BlockId = -1;

    private Action<PlacementSquare>? _onClick = null;

    public bool HasBlock; // if false, a tank exists here

    public readonly int Id;

    public BlockMapPosition RelativePosition;

    float _flashTime;

    public Matrix World;
    public Matrix View;
    public Matrix Projection;

    public PlacementSquare(Vector3 position, float dimensions) {
        Position = position;
        _box = new(position - new Vector3(dimensions / 2, 0, dimensions / 2), position + new Vector3(dimensions / 2, 0, dimensions / 2));

        _model = ModelGlobals.FlatFace.Asset;

        Id = Placements.Count;

        Placements.Add(this);
    }
    public static void InitializeLevelEditorSquares() {
        if (_initialized)
            return;
        for (int j = 0; j < BlockMapPosition.MAP_HEIGHT; j++) {
            for (int i = 0; i < BlockMapPosition.MAP_WIDTH_169; i++) {
                new PlacementSquare(new BlockMapPosition(i, j), Block.SIDE_LENGTH) {
                    _onClick = (place) => {
                        if (!place.HasItem)
                            place.DoPlacementAction(true);
                        else
                            place.DoPlacementAction(false);
                    },
                    RelativePosition = new BlockMapPosition(i, j)
                };
            }
        }
        _initialized = true;
    }
    public static void ResetSquares() {
        for (int i = 0; i < Placements.Count; i++) {
            Placements[i].TankId = -1;
            Placements[i].BlockId = -1;
        }
    }
    // TODO: need a sound for placement

    /// <summary>Attempts to get the closest <see cref="PlacementSquare"/> given a position. Returns null if one was not found.
    /// <br></br>This method checks if the position is cloest to a <see cref="PlacementSquare"/> within half a block size.</summary>
    /// <param name="pos">The position to check from.</param>
    /// <returns>The closest <see cref="PlacementSquare"/>.</returns>
    public static PlacementSquare? GetFromClosest(Vector3 pos) {
        var closestIdx = Placements.FindIndex(place => Vector3.Distance(place.Position, pos) < Block.SIDE_LENGTH / 2);

        return closestIdx > -1 ? Placements[closestIdx] : null;
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
        for (int i = 0; i < Placements.Count; i++)
            Placements[i]?.Update();
    }
    public static void RenderAll() {
        if (UIElement.GetElementsAt(MouseUtils.MousePosition).Count > 0)
            return;

        _mouseRay = RayUtils.GetMouseToWorldRay();
        var view = CameraGlobals.GameView;
        var projection = CameraGlobals.GameProjection;
        for (int i = 0; i < Placements.Count; i++)
            Placements[i]?.Render(view, projection);
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

        World = Matrix.CreateScale(0.678f) * Matrix.CreateTranslation(Position + new Vector3(0, 0.1f, 0));
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
        if (!displayHeights || block is null) return;

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