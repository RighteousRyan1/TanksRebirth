using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Linq;
using WiiPlayTanksRemake.Internals;

namespace WiiPlayTanksRemake.GameContent
{
    public static class MapRenderer
    {
        public static Matrix viewMatrix;
        public static Matrix projectionMatrix;
        public static Matrix worldMatrix;

        public class FloorRenderer
        {
            public static Model FloorModelBase;

            public static void LoadFloor()
            {
                FloorModelBase = GameResources.GetGameResource<Model>("Assets/floor_big");

                foreach (var mesh in FloorModelBase.Meshes)
                {
                    foreach (BasicEffect effect in mesh.Effects)
                    {
                        effect.TextureEnabled = true;

                        effect.Texture = GameResources.GetGameResource<Texture2D>("Assets/textures/ingame/ground");

                        effect.LightingEnabled = true;
                        effect.PreferPerPixelLighting = true;
                        effect.EnableDefaultLighting();

                        effect.DirectionalLight0.Enabled = true;
                        effect.DirectionalLight1.Enabled = false;
                        effect.DirectionalLight2.Enabled = false;

                        effect.DirectionalLight0.Direction = Vector3.Down;
                    }
                }
            }

            public static void RenderFloor()
            {
                foreach (var mesh in FloorModelBase.Meshes)
                {
                    foreach (IEffectMatrices matrices in mesh.Effects)
                    {
                        matrices.View = viewMatrix;
                        matrices.Projection = projectionMatrix;
                        matrices.World = worldMatrix;
                    }

                    mesh.Draw();
                }
            }
        }

        public class BoundsRenderer
        {
            private static BoundingBox[] enclosingBoxes = new BoundingBox[4];

            public static BoundingBox BoundaryBox;

            public static Model BoundaryModel;

            public static void LoadBounds()
            {
                BoundaryModel = GameResources.GetGameResource<Model>("Assets/outerbounds_big");

                foreach (var mesh in BoundaryModel.Meshes)
                {
                    foreach (BasicEffect effect in mesh.Effects)
                    {
                        effect.LightingEnabled = true;
                        effect.PreferPerPixelLighting = true;
                        effect.EnableDefaultLighting();

                        effect.TextureEnabled = true;

                        effect.DirectionalLight0.Enabled = true;
                        effect.DirectionalLight1.Enabled = true;
                        effect.DirectionalLight2.Enabled = false;

                        effect.DirectionalLight0.Direction = new Vector3(0, -0.7f, -0.7f);
                        effect.DirectionalLight1.Direction = new Vector3(0, -0.7f, 0.7f);

                        effect.SpecularColor = new Vector3(0, 0, 0);
                    }

                    SetBlockTexture(mesh, "polygon33", BoundaryTextureContext.block_other_a);
                    SetBlockTexture(mesh, "polygon7", BoundaryTextureContext.block_shadow_b);
                    SetBlockTexture(mesh, "polygon20", BoundaryTextureContext.block_shadow_d);
                    SetBlockTexture(mesh, "polygon21", BoundaryTextureContext.block_shadow_b);

                    //SetBlockTexture(mesh, "polygon54", BoundaryTextureContext.block_shadow_b);
                    //SetBlockTexture(mesh, "polygon68", BoundaryTextureContext.block_shadow_d);

                    //SetBlockTexture(mesh, "polygon54", BoundaryTextureContext.block_shadow_b);
                    //SetBlockTexture(mesh, "polygon68", BoundaryTextureContext.block_shadow_d);
                    //SetBlockTexture(mesh, "polygon8", BoundaryTextureContext.block_other_a);

                    // SetBlockTexture(mesh, "polygon83", BoundaryTextureContext.ground);
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
                foreach (var mesh in BoundaryModel.Meshes)
                {
                    foreach (IEffectMatrices matrices in mesh.Effects)
                    {
                        matrices.View = viewMatrix;
                        matrices.Projection = projectionMatrix;
                        matrices.World = worldMatrix;
                    }

                    mesh.Draw();
                }
            }

            /// <summary>
            /// Sets a block texture in the boundary model to 
            /// </summary>
            /// <param name="mesh"></param>
            /// <param name="meshNameMatch"></param>
            /// <param name="textureContext"></param>
            private static void SetBlockTexture(ModelMesh mesh, string meshNameMatch, BoundaryTextureContext context)
            {
                foreach (BasicEffect effect in mesh.Effects)
                {
                    if (mesh.Name == meshNameMatch)
                    {
                        effect.Texture = GameResources.GetGameResource<Texture2D>($"Assets/textures/ingame/{context}");
                    }
                }
            }

            private enum BoundaryTextureContext
            {
                block_other_a,
                block_other_b,
                block_other_c,
                block_other_b_test,
                ground,
                block_shadow_b,
                block_shadow_d
            }
        }

        /*public const float TANKS_MIN_X = -268;
        public const float TANKS_MAX_X = 268;
        public const float TANKS_MIN_Y = -155;
        public const float TANKS_MAX_Y = 400;

        public const float MIN_X = -278;
        public const float MAX_X = 278;
        public const float MIN_Y = -165;
        public const float MAX_Y = 410;

        public const float CUBE_MIN_X = MIN_X + Cube.FULLBLOCK_SIZE / 2;
        public const float CUBE_MAX_X = MAX_X - Cube.FULLBLOCK_SIZE / 2;
        public const float CUBE_MIN_Y = MIN_Y + Cube.FULLBLOCK_SIZE / 2;
        public const float CUBE_MAX_Y = MAX_Y - Cube.FULLBLOCK_SIZE / 2;*/

        public const float TANKS_MIN_X = -313.2546f;
        public const float TANKS_MAX_X = 313.2546f;
        public const float TANKS_MIN_Y = -111.45461f;
        public const float TANKS_MAX_Y = 372.32504f;

        public const float MIN_X = -324;
        public const float MAX_X = 324;
        public const float MIN_Y = -117;
        public const float MAX_Y = 377;

        public const float CUBE_MIN_X = MIN_X + Cube.FULLBLOCK_SIZE / 2 - 4f;
        public const float CUBE_MAX_X = MAX_X - Cube.FULLBLOCK_SIZE / 2 + 2f;
        public const float CUBE_MIN_Y = MIN_Y + Cube.FULLBLOCK_SIZE / 2 - 8f;
        public const float CUBE_MAX_Y = MAX_Y - Cube.FULLBLOCK_SIZE / 2 + 8f;

        public static Vector3 TopLeft => new(CUBE_MIN_X, 0, CUBE_MAX_Y);
        public static Vector3 TopRight => new(CUBE_MAX_X, 0, CUBE_MAX_Y);
        public static Vector3 BottomLeft => new(CUBE_MIN_X, 0, CUBE_MIN_Y);
        public static Vector3 BottomRight => new(CUBE_MAX_X, 0, CUBE_MIN_Y);
        public static void InitializeRenderers()
        {
            FloorRenderer.LoadFloor();
            BoundsRenderer.LoadBounds();
        }

        public static void DrawWorldModels()
        {
            viewMatrix = TankGame.GameView;
            projectionMatrix = TankGame.GameProjection;
            worldMatrix = Matrix.CreateScale(0.855f) * Matrix.CreateTranslation(0, 0, 130);

            FloorRenderer.RenderFloor();
            BoundsRenderer.RenderBounds();
        }
    }

    public static class CollisionMap
    {
        // make a 2d collision map :smil:

        public static Vector2 renderPosition;

        public static Vector2[] trackedTankPositions;
        public static Vector2[] trackedCubePositions;
    }
}