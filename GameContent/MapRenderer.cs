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
                FloorModelBase = GameResources.GetGameResource<Model>("Assets/floor");

                foreach (var mesh in FloorModelBase.Meshes)
                {
                    foreach (BasicEffect effect in mesh.Effects)
                    {
                        effect.TextureEnabled = true;

                        effect.Texture = GameResources.GetGameResource<Texture2D>("Assets/textures/ingame/ground");

                        effect.LightingEnabled = true;
                        effect.PreferPerPixelLighting = true;
                        effect.EnableDefaultLighting();
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
                BoundaryModel = GameResources.GetGameResource<Model>("Assets/outerbounds");

                foreach (var mesh in BoundaryModel.Meshes)
                {
                    foreach (BasicEffect effect in mesh.Effects)
                    {
                        effect.LightingEnabled = true;
                        effect.PreferPerPixelLighting = true;
                        effect.EnableDefaultLighting();

                        effect.TextureEnabled = true;
                    }

                    SetBlockTexture(mesh, "polygon54", BoundaryTextureContext.block_shadow_b);
                    SetBlockTexture(mesh, "polygon68", BoundaryTextureContext.block_shadow_d);
                    SetBlockTexture(mesh, "polygon8", BoundaryTextureContext.block_other_a);
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

        public const int TANKS_MIN_X = -268;
        public const int TANKS_MAX_X = 268;
        public const int TANKS_MIN_Y = -155;
        public const int TANKS_MAX_Y = 400;

        public const int MIN_X = -278;
        public const int MAX_X = 278;
        public const int MIN_Y = -165;
        public const int MAX_Y = 410;
        public static void InitializeRenderers()
        {
            FloorRenderer.LoadFloor();
            BoundsRenderer.LoadBounds();
        }

        public static void DrawWorldModels()
        {
            viewMatrix = TankGame.GameView;
            projectionMatrix = TankGame.GameProjection;
            worldMatrix = Matrix.CreateTranslation(0, 0, 130); //* Matrix.CreateRotationX(Internals.Common.Utilities.GameUtils.MousePosition.X / 500);

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