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
            public static string[] MeshNames = new string[0];

            public static Model BoundaryModel;

            public static void LoadBounds()
            {
                BoundaryModel = GameResources.GetGameResource<Model>("Assets/outerbounds");

                var list = MeshNames.ToList();

                foreach (var mesh in BoundaryModel.Meshes)
                {
                    list.Add(mesh.Name);

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
                }

                MeshNames = list.ToArray();
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

    public static class CollisionMapViewer
    {
        // make a 2d collision map :smil:
    }
}