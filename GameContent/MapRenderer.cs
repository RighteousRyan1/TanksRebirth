using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using WiiPlayTanksRemake.Graphics;
using WiiPlayTanksRemake.Internals;
using WiiPlayTanksRemake.Internals.Common.Utilities;

namespace WiiPlayTanksRemake.GameContent
{
    public enum MapTheme
    {
        Default,
        Forest,
        MasterMod,
        MarbleMod
    }

    // TODO: Chairs and Co (models)
    public static class MapRenderer
    {
        public static bool ShouldRender = true;

        public static Matrix viewMatrix;
        public static Matrix projectionMatrix;
        public static Matrix worldMatrix;

        internal static string assetsRoot;

        public static MapTheme Theme { get; set; } = MapTheme.Default;

        public static class FloorRenderer
        {
            public static Model FloorModelBase;

            public static float scale = 1f;

            public static void LoadFloor()
            {
                FloorModelBase = GameResources.GetGameResource<Model>("Assets/floor_big");
                switch (Theme)
                {
                    case MapTheme.Default:

                        assetsRoot = "Assets/textures/ingame/";

                        break;

                    case MapTheme.Forest:

                        assetsRoot = "Assets/forest/";

                        break;

                    case MapTheme.MasterMod:

                        assetsRoot = "Assets/textures/ingame/mastermod/";

                        break;

                    case MapTheme.MarbleMod:

                        assetsRoot = "Assets/textures/ingame/marblemod/";

                        break;
                }
            }

            public static void RenderFloor()
            {
                foreach (var mesh in FloorModelBase.Meshes)
                {
                    foreach (BasicEffect effect in mesh.Effects)
                    {
                        effect.View = viewMatrix;
                        effect.Projection = projectionMatrix;
                        effect.World = worldMatrix * Matrix.CreateScale(scale);

                        effect.SetDefaultGameLighting();

                        effect.DirectionalLight0.Enabled = true;
                        effect.DirectionalLight1.Enabled = false;
                        effect.DirectionalLight2.Enabled = false;

                        effect.DirectionalLight0.Direction = Vector3.Down;

                        effect.TextureEnabled = true;

                        effect.Texture = GameResources.GetGameResource<Texture2D>(assetsRoot + "floor_face");
                    }

                    mesh.Draw();
                }
            }
        }

        public static class BoundsRenderer
        {
            internal static BoundingBox[] enclosingBoxes = new BoundingBox[4];

            public static BoundingBox BoundaryBox;

            public static Model BoundaryModel;

            public static Vector3[] treePositions = new Vector3[]
            {
                new() { X = 390f, Y = 0f, Z = 200f },
                new() { X = -360f, Y = 0f, Z = 350f },
                new() { X = -372f, Y = 0f, Z = 150f },
                new() { X = 372f, Y = 0f, Z = -100f },
                new() { X = -20f, Y = 0f, Z = -210f },
                new() { X = 50f, Y = 0f, Z = 500f },

                new() { X = -325f, Y = 0f, Z = -180f },
            };
            public static Vector3[] stumpPositions = new Vector3[]
            {
                new() { X = 35f, Y = 0f, Z = -185f }
            };

            // position, inverted
            public static Vector3[] logPilePositions = new Vector3[]
            {
                new() { X = -260f, Y = 0f, Z = -135f },
                new() { X = -120f, Y = 0f, Z = -135f },
                new() { X = 0f, Y = 0f, Z = -135f },
                new() { X = 120f, Y = 0f, Z = -135f },
                new() { X = 240f, Y = 0f, Z = -135f },

                new() { X = 338f, Y = 0f, Z = -90f },
                new() { X = 338f, Y = 0f, Z = 30f },
                new() { X = 338f, Y = 0f, Z = 150f },
                new() { X = 338f, Y = 0f, Z = 270f },
                new() { X = 338f, Y = 0f, Z = 390f },

                new() { X = -260f, Y = 0f, Z = 400f },
                new() { X = -120f, Y = 0f, Z = 400f },
                new() { X = 0f, Y = 0f, Z = 400f },
                new() { X = 120f, Y = 0f, Z = 400f },
                new() { X = 240f, Y = 0f, Z = 400f },

                new() { X = -338f, Y = 0f, Z = -90f },
                new() { X = -338f, Y = 0f, Z = 30f },
                new() { X = -338f, Y = 0f, Z = 150f },
                new() { X = -338f, Y = 0f, Z = 270f },
                new() { X = -338f, Y = 0f, Z = 390f },
            };

            public static bool[] logPileInverted = new bool[]
            {
                false, true, false, true, false,

                false, true, false, true, false,

                false, true, false, true, false,

                false, true, false, true, false
            };

            public static float[] logPileOrientations = new float[]
            {
                0, 0, 0, 0, 0,

                MathHelper.PiOver2, MathHelper.PiOver2, MathHelper.PiOver2, MathHelper.PiOver2, MathHelper.PiOver2,

                MathHelper.Pi, MathHelper.Pi, MathHelper.Pi, MathHelper.Pi, MathHelper.Pi,

                MathHelper.PiOver2, MathHelper.PiOver2, MathHelper.PiOver2, MathHelper.PiOver2, MathHelper.PiOver2
            };

            public static Model TreeModel;
            public static Model TreeStumpModel;
            public static Model LogPileModel;

            public static void LoadBounds()
            {
                switch (Theme)
                {
                    case MapTheme.Default:
                    case MapTheme.MasterMod:
                    case MapTheme.MarbleMod:
                        BoundaryModel = GameResources.GetGameResource<Model>("Assets/toy/outerbounds");

                        SetBlockTexture(BoundaryModel.Meshes["polygon48"], BoundaryTextureContext.block_other_c);
                        SetBlockTexture(BoundaryModel.Meshes["polygon40"], BoundaryTextureContext.block_other_a);
                        SetBlockTexture(BoundaryModel.Meshes["polygon33"], BoundaryTextureContext.block_other_b_test);
                        SetBlockTexture(BoundaryModel.Meshes["polygon7"], BoundaryTextureContext.block_shadow_b);
                        SetBlockTexture(BoundaryModel.Meshes["polygon15"], BoundaryTextureContext.block_shadow_b);

                        SetBlockTexture(BoundaryModel.Meshes["polygon5"], BoundaryTextureContext.block_shadow_h);

                        SetBlockTexture(BoundaryModel.Meshes["polygon20"], BoundaryTextureContext.block_shadow_d);

                        // SetBlockTexture(BoundaryModel.Meshes["polygon9"], BoundaryTextureContext.block_shadow_d);

                        SetBlockTexture(BoundaryModel.Meshes["polygon21"], BoundaryTextureContext.block_shadow_b);

                        SetBlockTexture(BoundaryModel.Meshes["polygon32"], BoundaryTextureContext.floor_lower);

                        break;
                    case MapTheme.Forest:
                        TreeModel = GameResources.GetGameResource<Model>("Assets/forest/tree");
                        TreeStumpModel = GameResources.GetGameResource<Model>("Assets/forest/tree_stump");
                        LogPileModel = GameResources.GetGameResource<Model>("Assets/forest/logpile");

                        break;
                }

                /*
                 * 0 -> right
                 * 1 -> left
                 * 2 -> top
                 * 3 -> bottom
                 */

                enclosingBoxes[0] = new(new Vector3(MAX_X, 0, MIN_Y - 20), new Vector3(MAX_X + 20, 0, MAX_Y + 20));
                enclosingBoxes[1] = new(new Vector3(MIN_X, 0, MIN_Y - 20), new Vector3(MIN_X - 20, 0, MAX_Y + 20));
                enclosingBoxes[2] = new(new Vector3(MIN_X - 20, 0, MIN_Y - 20), new Vector3(MAX_X + 20, 0, MIN_Y));
                enclosingBoxes[3] = new(new Vector3(MIN_X - 20, 0, MAX_Y + 20), new Vector3(MAX_X + 20, 0, MAX_Y));

                var merged1 = BoundingBox.CreateMerged(enclosingBoxes[0], enclosingBoxes[1]);
                var merged2 = BoundingBox.CreateMerged(merged1, enclosingBoxes[2]);
                BoundaryBox = BoundingBox.CreateMerged(merged2, enclosingBoxes[3]);
            }

            public static void RenderBounds()
            {
                switch (Theme)
                {
                    case MapTheme.Default:
                    case MapTheme.MasterMod:
                    case MapTheme.MarbleMod:
                        foreach (var mesh in BoundaryModel.Meshes)
                        {
                            foreach (BasicEffect effect in mesh.Effects)
                            {
                                effect.View = viewMatrix;
                                effect.Projection = projectionMatrix;
                                effect.World = worldMatrix;

                                if (mesh.Name == "polygon2")
                                    effect.Alpha = 0.1f;
                                else
                                    effect.Alpha = 1f;

                                effect.SetDefaultGameLighting();
                            }

                            mesh.Draw();
                        }
                        break;
                    case MapTheme.Forest:
                        for (int i = 0; i < treePositions.Length; i++)
                        {
                            var position = treePositions[i];

                            foreach (var mesh in TreeModel.Meshes)
                            {
                                foreach (BasicEffect effect in mesh.Effects)
                                {
                                    effect.View = viewMatrix;
                                    effect.Projection = projectionMatrix;
                                    effect.World = Matrix.CreateRotationX(-MathHelper.PiOver2) * Matrix.CreateRotationY(MathHelper.Pi) * Matrix.CreateTranslation(position);

                                    effect.TextureEnabled = true;

                                   if (mesh.Name == "Log")
                                       effect.Texture = GameResources.GetGameResource<Texture2D>("Assets/forest/tree_log_tex");
                                   else
                                       effect.Texture = GameResources.GetGameResource<Texture2D>("Assets/forest/grassfloor");

                                    effect.SetDefaultGameLighting();
                                }

                                mesh.Draw();
                            }
                        }
                        for (int i = 0; i < stumpPositions.Length; i++)
                        {
                            var position = stumpPositions[i];

                            foreach (var mesh in TreeStumpModel.Meshes)
                            {
                                foreach (BasicEffect effect in mesh.Effects)
                                {
                                    effect.View = viewMatrix;
                                    effect.Projection = projectionMatrix;
                                    effect.World = Matrix.CreateScale(10) * Matrix.CreateRotationX(-MathHelper.PiOver2) * Matrix.CreateRotationY(MathHelper.Pi) * Matrix.CreateTranslation(position + new Vector3(0, 10, 0));

                                    effect.TextureEnabled = true;

                                    effect.Texture = GameResources.GetGameResource<Texture2D>("Assets/forest/tree_log_tex");

                                    effect.SetDefaultGameLighting();
                                }

                                mesh.Draw();
                            }
                        }
                        for (int i = 0; i < logPilePositions.Length; i++)
                        {
                            var position = logPilePositions[i];

                            var invert = logPileInverted[i];

                            foreach (var mesh in LogPileModel.Meshes)
                            {
                                foreach (BasicEffect effect in mesh.Effects)
                                {
                                    effect.View = viewMatrix;
                                    effect.Projection = projectionMatrix;
                                    effect.World = Matrix.CreateScale(50, 20, 50) * Matrix.CreateRotationX(-MathHelper.PiOver2) * Matrix.CreateFromYawPitchRoll((invert ? MathHelper.Pi : 0) + logPileOrientations[i], 0, 0) * Matrix.CreateTranslation(position);

                                    effect.TextureEnabled = true;

                                    effect.Texture = GameResources.GetGameResource<Texture2D>("Assets/forest/tree_log_tex");

                                    effect.SetDefaultGameLighting();
                                }

                                mesh.Draw();
                            }
                        }
                        break;
                        break;
                }
            }

            /// <summary>
            /// Sets a block texture in the boundary model to 
            /// </summary>
            /// <param name="mesh"></param>
            /// <param name="meshNameMatch"></param>
            /// <param name="textureContext"></param>
            private static void SetBlockTexture(ModelMesh mesh, BoundaryTextureContext context)
            {
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.Texture = GameResources.GetGameResource<Texture2D>($"{assetsRoot}{context}");
                }
            }

            private enum BoundaryTextureContext
            {
                block_other_a,
                block_other_b,
                block_other_c,
                block_other_b_test,
                floor_lower,
                block_shadow_b,
                block_shadow_d,
                block_shadow_h
            }
        }

        public const float TANKS_MIN_X = MIN_X + 6;
        public const float TANKS_MAX_X = MAX_X - 6;
        public const float TANKS_MIN_Y = MIN_Y + 5;
        public const float TANKS_MAX_Y = MAX_Y - 6;

        public const float MIN_X = -264;
        public const float MAX_X = 264;
        public const float MIN_Y = -72;
        public const float MAX_Y = 334;

        public const float CUBE_MIN_X = MIN_X + Block.FULL_BLOCK_SIZE / 2 - 6f;
        public const float CUBE_MAX_X = MAX_X - Block.FULL_BLOCK_SIZE / 2;
        public const float CUBE_MIN_Y = MIN_Y + Block.FULL_BLOCK_SIZE / 2 - 6f;
        public const float CUBE_MAX_Y = MAX_Y - Block.FULL_BLOCK_SIZE / 2;

        public static Vector3 TopLeft => new(CUBE_MIN_X, 0, CUBE_MAX_Y);
        public static Vector3 TopRight => new(CUBE_MAX_X, 0, CUBE_MAX_Y);
        public static Vector3 BottomLeft => new(CUBE_MIN_X, 0, CUBE_MIN_Y);
        public static Vector3 BottomRight => new(CUBE_MAX_X, 0, CUBE_MIN_Y);
        public static void InitializeRenderers()
        {
            FloorRenderer.LoadFloor();
            BoundsRenderer.LoadBounds();
        }

        public static void RenderWorldModels()
        {
            viewMatrix = TankGame.GameView;
            projectionMatrix = TankGame.GameProjection;
            worldMatrix = Matrix.CreateScale(0.7f) * Matrix.CreateTranslation(0, 0, 130);

            if (ShouldRender)
            {
                FloorRenderer.RenderFloor();
                BoundsRenderer.RenderBounds();
            }
        }
    }
}