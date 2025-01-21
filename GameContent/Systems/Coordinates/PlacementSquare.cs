using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using TanksRebirth.Graphics;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common;
using TanksRebirth.Internals.Common.Utilities;
using FontStashSharp;
using System.Linq;
using TanksRebirth.GameContent.UI;
using TanksRebirth.Enums;
using TanksRebirth.Internals.UI;
using TanksRebirth.GameContent.ID;
using Microsoft.Xna.Framework.Input;
using TanksRebirth.GameContent.RebirthUtils;

namespace TanksRebirth.GameContent.Systems.Coordinates;

public class PlacementSquare {
    private static bool _initialized;
    // Drag-and-Drop
    
    public static bool DrawStacks { get; set; } = true;
    public static bool IsPlacing { get; private set; }
    public bool HasItem => BlockId > -1 || TankId > -1;

    public static PlacementSquare CurrentlyHovered;

    public static bool displayHeights = true;

    public static List<PlacementSquare> Placements = new();

    public Vector3 Position { get; set; }

    private BoundingBox _box;

    private Model _model;

    public bool IsHovered => RayUtils.GetMouseToWorldRay().Intersects(_box).HasValue;

    public static bool PlacesBlock; // if false, tanks will be placed

    public int TankId = -1;
    public int BlockId = -1;

    private Action<PlacementSquare> _onClick = null;

    public bool HasBlock; // if false, a tank exists here

    public readonly int Id;

    public Point RelativePosition;

    public PlacementSquare(Vector3 position, float dimensions) {
        Position = position;
        _box = new(position - new Vector3(dimensions / 2, 0, dimensions / 2), position + new Vector3(dimensions / 2, 0, dimensions / 2));

        _model = GameResources.GetGameResource<Model>("Assets/check");

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
                    RelativePosition = new Point(i, j)
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

    /// <summary>
    /// Does default block placement/removal.
    /// </summary>
    /// <param name="place">Whether or not to place the block or remove the block. If false, the block is removed.</param>
    public void DoPlacementAction(bool place) {
        if (UIElement.GetElementsAt(MouseUtils.MousePosition).Count > 0)
            return;
        if (PlacesBlock) {
            if (!HasBlock && HasItem)
                return;

            if (place) {
                var block = new Block(LevelEditor.Active ? LevelEditor.SelectedBlockType : DebugManager.blockType, LevelEditor.Active ? LevelEditor.BlockHeight : DebugManager.blockHeight, Position.FlattenZ());
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
            LevelEditor.missionToRate = Mission.GetCurrent();
        }
        else {
            // var team = LevelEditor.Active ? LevelEditor.SelectedTankTeam : (TankTeam)GameHandler.tankToSpawnTeam;

            if (HasBlock && HasItem)
                return;

            if (!HasBlock && HasItem) {
                // FIXME: why does this even craaaash?
                GameHandler.AllTanks[TankId].Remove(true);
                TankId = -1;
                LevelEditor.missionToRate = Mission.GetCurrent();
                return;
            }

            var team = LevelEditor.Active ? LevelEditor.SelectedTankTeam : DebugManager.tankToSpawnTeam;
            if (LevelEditor.CurCategory == LevelEditor.Category.EnemyTanks) {
                if (LevelEditor.Active && LevelEditor.SelectedTankTier < TankID.Brown)
                    return;
                if (AIManager.CountAll() >= 50) {
                    LevelEditor.Alert("You are at enemy tank capacity!");
                    return;
                }
                var tnk = DebugManager.SpawnTankAt(Position, LevelEditor.Active ? LevelEditor.SelectedTankTier : DebugManager.tankToSpawnType, team); // todo: finish
                HasBlock = false;
                TankId = tnk.WorldId;
            }
            else {
                var type = LevelEditor.Active ? LevelEditor.SelectedPlayerType : PlayerID.Blue;

                var idx = Array.FindIndex(GameHandler.AllPlayerTanks, x => x is not null && x.PlayerType == type);
                if (idx > -1) {
                    LevelEditor.Alert($"This color player tank already exists! (ID {idx})");
                    return;
                }

                var me = DebugManager.SpawnMe(type, team);
                TankId = me.WorldId;
            }
            LevelEditor.missionToRate = Mission.GetCurrent();
        }
    }
    public void Update() {
        if (IsHovered) {
            CurrentlyHovered = this;
            if (InputUtils.CanDetectClick())
                _onClick?.Invoke(this);

            if (PlacesBlock) {
                if (InputUtils.MouseLeft) {
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

    public void Render() {
        var hoverUi = UIElement.GetElementsAt(MouseUtils.MousePosition).Count > 0;

        foreach (var mesh in _model.Meshes) {
            foreach (BasicEffect effect in mesh.Effects) {
                effect.World = Matrix.CreateScale(0.68f) * Matrix.CreateTranslation(Position + new Vector3(0, 0.1f, 0));
                effect.View = TankGame.GameView;
                effect.Projection = TankGame.GameProjection;

                //var pos1 = MatrixUtils.ConvertWorldToScreen(Vector3.Zero, effect.World, effect.View, effect.Projection);

                //SpriteFontUtils.DrawBorderedText(TankGame.SpriteRenderer, TankGame.TextFont, $"{Id}\n({RelativePosition.X}, {RelativePosition.Y})", pos1, Color.White, Color.Black, new Vector2(TankGame.AddativeZoom * 0.8f).ToResolution(), 0f);

                if (DrawStacks) {
                    if (BlockId > -1) {
                        if (displayHeights && Block.AllBlocks[BlockId] is not null) {
                            if (Block.AllBlocks[BlockId].Properties.CanStack) {
                                var pos = MatrixUtils.ConvertWorldToScreen(Vector3.Zero, effect.World, effect.View, effect.Projection);

                                SpriteBatchUtils.DrawBorderedText(TankGame.SpriteRenderer, TankGame.TextFont, $"{Block.AllBlocks[BlockId].Stack}", pos, Color.White, Color.Black, new Vector2(TankGame.AddativeZoom * 1.5f).ToResolution(), 0f, Anchor.Center);
                            }
                            if (Block.AllBlocks[BlockId].Type == BlockID.Teleporter) {
                                var pos = MatrixUtils.ConvertWorldToScreen(Vector3.Zero, effect.World, effect.View, effect.Projection);

                                SpriteBatchUtils.DrawBorderedText(TankGame.SpriteRenderer, TankGame.TextFont, $"TP:{Block.AllBlocks[BlockId].TpLink}", pos, Color.White, Color.Black, new Vector2(TankGame.AddativeZoom * 1.5f).ToResolution(), 0f, Anchor.Center);
                            }
                        }
                    }
                    else if (TankId > -1) {
                        var pos = MatrixUtils.ConvertWorldToScreen(Vector3.Zero, effect.World, effect.View, effect.Projection);

                        SpriteBatchUtils.DrawBorderedText(TankGame.SpriteRenderer, TankGame.TextFont, $"{LevelEditor.TeamColorsLocalized[GameHandler.AllTanks[TankId].Team]}", pos - new Vector2(0, 8).ToResolution(), TeamID.TeamColors[GameHandler.AllTanks[TankId].Team], Color.Black, new Vector2(0.9f).ToResolution() * TankGame.AddativeZoom, 0f, Anchor.Center);

                        if (GameHandler.AllTanks[TankId] is AITank ai)
                            SpriteBatchUtils.DrawBorderedText(TankGame.SpriteRenderer, TankGame.TextFont, $"ID: {ai.AITankId}", pos + new Vector2(0, 8), TeamID.TeamColors[GameHandler.AllTanks[TankId].Team], Color.Black, new Vector2(0.8f).ToResolution() * TankGame.AddativeZoom, 0f, Anchor.Center);
                        if (GameHandler.AllTanks[TankId] is PlayerTank player)
                            SpriteBatchUtils.DrawBorderedText(TankGame.SpriteRenderer, TankGame.TextFont, $"ID: {player.PlayerId}", pos + new Vector2(0, 8), TeamID.TeamColors[GameHandler.AllTanks[TankId].Team], Color.Black, new Vector2(0.8f).ToResolution() * TankGame.AddativeZoom, 0f, Anchor.Center);
                    }
                }
                effect.TextureEnabled = true;
                effect.Texture = GameResources.GetGameResource<Texture2D>("Assets/textures/WhitePixel");

                if (IsHovered && !hoverUi)
                    effect.Alpha = 0.7f;
                else
                    effect.Alpha = 0f;

                effect.SetDefaultGameLighting_IngameEntities();
            }
            mesh.Draw();
        }
    }
}