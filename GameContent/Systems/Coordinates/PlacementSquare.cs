using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using TanksRebirth.Graphics;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common;
using TanksRebirth.Internals.Common.Utilities;
using FontStashSharp;

namespace TanksRebirth.GameContent.Systems.Coordinates
{
    public class PlacementSquare
    {
        // Drag-and-Drop
        public static bool IsPlacing { get; private set; }


        public bool HasBlock => CurrentBlockId > -1;

        public static PlacementSquare CurrentlyHovered;

        public static bool displayHeights = true;

        internal static List<PlacementSquare> Placements = new();

        public Vector3 Position { get; set; }

        private BoundingBox _box;

        private Model _model;

        public bool IsHovered => GameUtils.GetMouseToWorldRay().Intersects(_box).HasValue;

        public int CurrentBlockId = -1;

        private Action<PlacementSquare> _onClick = null;

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
                            if (!place.HasBlock)
                            {
                                ChatSystem.SendMessage("Added!", Color.Lime);
                                place.DoBlockAction(true);
                            }
                            else
                            {
                                ChatSystem.SendMessage("Removed!", Color.Red);
                                place.DoBlockAction(false);
                            }
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
        public void DoBlockAction(bool place)
        {
            if (place)
            {
                var cube = new Block((Block.BlockType)GameHandler.BlockType, GameHandler.CubeHeight, Position.FlattenZ());
                CurrentBlockId = cube.Id;

                IsPlacing = true;
            }
            else
            {
                Block.AllBlocks[CurrentBlockId].Remove();
                CurrentBlockId = -1;

                IsPlacing = false;
            }
        }
        public void Update()
        {
            if (IsHovered)
            {
                CurrentlyHovered = this;
                if (Input.CanDetectClick())
                    _onClick?.Invoke(this);

                if (IsHovered)
                {
                    if (Input.MouseLeft)
                    {
                        if (IsPlacing)
                        {
                            if (!HasBlock)
                            {
                                DoBlockAction(true);
                            }
                        }
                        else
                        {
                            if (HasBlock)
                            {
                                DoBlockAction(false);
                            }
                        }
                    }
                }
            }

            if (CurrentBlockId > -1)
                if (Block.AllBlocks[CurrentBlockId] is null)
                    CurrentBlockId = -1;
        }

        public void Render()
        {
            foreach (var mesh in _model.Meshes)
            {
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.World = Matrix.CreateScale(0.68f) * Matrix.CreateTranslation(Position +  new Vector3(0, 0.1f, 0));
                    effect.View = TankGame.GameView;
                    effect.Projection = TankGame.GameProjection;

                    if (CurrentBlockId > -1)
                        if (displayHeights && Block.AllBlocks[CurrentBlockId] is not null)
                        {
                            if (Block.AllBlocks[CurrentBlockId].CanStack)
                            {
                                var pos = GeometryUtils.ConvertWorldToScreen(Vector3.Zero, effect.World, effect.View, effect.Projection);
                                for (int i = 0; i < 4; i++)
                                    TankGame.spriteBatch.DrawString(TankGame.TextFont, $"{Block.AllBlocks[CurrentBlockId].Stack}", pos + new Vector2(0, 2f).RotatedByRadians(MathHelper.PiOver2 * i + MathHelper.PiOver4),
                                        Color.Black, new(1f), 0f, TankGame.TextFont.MeasureString($"{Block.AllBlocks[CurrentBlockId].Stack}") / 2);


                                pos = GeometryUtils.ConvertWorldToScreen(Vector3.Zero, effect.World, effect.View, effect.Projection);
                                TankGame.spriteBatch.DrawString(TankGame.TextFont, $"{Block.AllBlocks[CurrentBlockId].Stack}", pos,
                                    Color.White, new(1f), 0f, TankGame.TextFont.MeasureString($"{Block.AllBlocks[CurrentBlockId].Stack}") / 2);
                            }
                            if (Block.AllBlocks[CurrentBlockId].Type == Block.BlockType.Teleporter)
                            {
                                var pos = GeometryUtils.ConvertWorldToScreen(Vector3.Zero, effect.World, effect.View, effect.Projection);
                                for (int i = 0; i < 4; i++)
                                    TankGame.spriteBatch.DrawString(TankGame.TextFont, $"TP:{Block.AllBlocks[CurrentBlockId].TpLink}", pos + new Vector2(0, 2f).RotatedByRadians(MathHelper.PiOver2 * i + MathHelper.PiOver4),
                                        Color.Black, new(1f), 0f, TankGame.TextFont.MeasureString($"TP:{Block.AllBlocks[CurrentBlockId].TpLink}") / 2);

                                pos = GeometryUtils.ConvertWorldToScreen(Vector3.Zero, effect.World, effect.View, effect.Projection);
                                TankGame.spriteBatch.DrawString(TankGame.TextFont, $"TP:{Block.AllBlocks[CurrentBlockId].TpLink}", pos,
                                    Color.White, new(1f), 0f, TankGame.TextFont.MeasureString($"TP:{Block.AllBlocks[CurrentBlockId].TpLink}") / 2);
                            }
                        }

                    effect.TextureEnabled = true;
                    effect.Texture = GameResources.GetGameResource<Texture2D>("Assets/textures/WhitePixel");
                    if (IsHovered)
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
