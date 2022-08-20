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

namespace TanksRebirth.GameContent.Systems.Coordinates
{
    public class PlacementSquare
    {
        // Drag-and-Drop
        public static bool IsPlacing { get; private set; }


        public bool HasItem => BlockId > -1 || TankId > -1;

        public static PlacementSquare CurrentlyHovered;

        public static bool displayHeights = true;

        public static List<PlacementSquare> Placements = new();

        public Vector3 Position { get; set; }

        private BoundingBox _box;

        private Model _model;

        public bool IsHovered => GameUtils.GetMouseToWorldRay().Intersects(_box).HasValue;

        public static bool PlacesBlock; // if false, tanks will be placed

        public int TankId = -1;
        public int BlockId = -1;

        private Action<PlacementSquare> _onClick = null;

        public bool HasBlock; // if false, a tank exists here

        public PlacementSquare(Vector3 position, float dimensions)
        {
            Position = position;
            _box = new(position - new Vector3(dimensions / 2, 0, dimensions / 2), position + new Vector3(dimensions / 2, 0, dimensions / 2));

            _model = GameResources.GetGameResource<Model>("Assets/check");


            Placements.Add(this);
        }
        public static void InitializeLevelEditorSquares()
        {
            for (int i = 0; i < CubeMapPosition.MAP_WIDTH; i++)
            {
                for (int j = 0; j < CubeMapPosition.MAP_HEIGHT; j++)
                {
                    new PlacementSquare(new CubeMapPosition(i, j), Block.FULL_BLOCK_SIZE)
                    {
                        _onClick = (place) =>
                        {
                            if (!place.HasItem)
                                place.DoPlacementAction(true);
                            else
                                place.DoPlacementAction(false);
                        }
                    };
                }
            }
        }
        // TODO: need a sound for placement

        /// <summary>
        /// Does default block placement/removal.
        /// </summary>
        /// <param name="place">Whether or not to place the block or remove the block. If false, the block is removed.</param>
        public void DoPlacementAction(bool place)
        {
            if (UIElement.GetElementsAt(GameUtils.MousePosition).Count > 0)
                return;
            if (PlacesBlock)
            {
                if (!HasBlock && HasItem)
                    return;

                if (place)
                {
                    var cube = new Block(LevelEditor.Active ? LevelEditor.SelectedBlockType : (Block.BlockType)GameHandler.BlockType, LevelEditor.Active ?  LevelEditor.BlockHeight : GameHandler.CubeHeight, Position.FlattenZ());
                    BlockId = cube.Id;

                    HasBlock = true;
                    IsPlacing = true;
                }
                else
                {
                    Block.AllBlocks[BlockId].Remove();
                    BlockId = -1;

                    HasBlock = true;
                    IsPlacing = false;
                }
            }
            else {
                // var team = LevelEditor.Active ? LevelEditor.SelectedTankTeam : (TankTeam)GameHandler.tankToSpawnTeam;

                if (HasBlock && HasItem)
                    return;

                if (!HasBlock && HasItem)
                {
                    GameHandler.AllTanks[TankId].Remove();
                    TankId = -1;
                    return;
                }

                var team = LevelEditor.Active ? LevelEditor.SelectedTankTeam : (TankTeam)GameHandler.tankToSpawnTeam;
                if (LevelEditor.CurCategory == LevelEditor.Category.EnemyTanks)
                {
                    if (LevelEditor.Active && LevelEditor.SelectedTankTier < TankTier.Brown)
                        return;
                    var tnk = GameHandler.SpawnTankAt(Position, LevelEditor.Active ? LevelEditor.SelectedTankTier : (TankTier)GameHandler.tankToSpawnType, team); // todo: finish
                    HasBlock = false;
                    TankId = tnk.WorldId;
                }
                else
                {
                    var me = GameHandler.SpawnMe(LevelEditor.Active ? LevelEditor.SelectedPlayerType : PlayerType.Blue, team);
                    TankId = me.WorldId;
                }
            }
        }
        public void Update()
        {
            if (IsHovered)
            {
                CurrentlyHovered = this;
                if (Input.CanDetectClick())
                    _onClick?.Invoke(this);

                if (PlacesBlock)
                {
                    if (Input.MouseLeft)
                    {
                        if (IsPlacing)
                        {
                            if (!HasItem)
                            {
                                DoPlacementAction(true);
                            }
                        }
                        else
                        {
                            if (HasItem)
                            {
                                DoPlacementAction(false);
                            }
                        }
                    }
                }
            }

            if (BlockId > -1)
            {
                if (PlacesBlock)
                {
                    if (Block.AllBlocks[BlockId] is null)
                        BlockId = -1;
                }
                else
                {
                    if (GameHandler.AllTanks[BlockId] is null)
                        TankId = -1;
                }
            }
        }

        public void Render()
        {
            var hoverUi = UIElement.GetElementsAt(GameUtils.MousePosition).Count > 0;

            foreach (var mesh in _model.Meshes)
            {
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.World = Matrix.CreateScale(0.68f) * Matrix.CreateTranslation(Position + new Vector3(0, 0.1f, 0));
                    effect.View = TankGame.GameView;
                    effect.Projection = TankGame.GameProjection;

                    if (BlockId > -1)
                        if (displayHeights && Block.AllBlocks[BlockId] is not null)
                        {
                            if (Block.AllBlocks[BlockId].CanStack)
                            {
                                var pos = GeometryUtils.ConvertWorldToScreen(Vector3.Zero, effect.World, effect.View, effect.Projection);

                                SpriteFontUtils.DrawBorderedText(TankGame.SpriteRenderer, TankGame.TextFont, $"{Block.AllBlocks[BlockId].Stack}", pos, Color.White, Color.Black, new Vector2(TankGame.AddativeZoom * 1.5f).ToResolution(), 0f);
                            }
                            if (Block.AllBlocks[BlockId].Type == Block.BlockType.Teleporter)
                            {
                                var pos = GeometryUtils.ConvertWorldToScreen(Vector3.Zero, effect.World, effect.View, effect.Projection);

                                SpriteFontUtils.DrawBorderedText(TankGame.SpriteRenderer, TankGame.TextFont, $"TP:{Block.AllBlocks[BlockId].TpLink}", pos, Color.White, Color.Black, new Vector2(TankGame.AddativeZoom * 1.5f).ToResolution(), 0f);
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
}
