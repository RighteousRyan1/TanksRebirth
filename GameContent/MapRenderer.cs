using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
                    }
                }
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

    }
}