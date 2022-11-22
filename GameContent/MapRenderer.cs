using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using tainicom.Aether.Physics2D.Dynamics;
using TanksRebirth.GameContent.UI;
using TanksRebirth.Graphics;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth.GameContent
{
    public enum MapTheme
    {
        Vanilla,
        Christmas,
    }

    // TODO: Chairs and Co (models)
    public static class MapRenderer
    {
        public static bool ShouldRender = true;

        public static Matrix View;
        public static Matrix Projection;
        public static Matrix World;

        public static string AssetRoot;

        public static MapTheme Theme { get; set; } = MapTheme.Vanilla;

        public static Dictionary<string, Texture2D> Assets = new()
        {
            ["block.1"] = null,
            ["block.2"] = null,
            ["block_harf.1"] = null,
            ["block_other_a"] = null,
            ["block_other_b"] = null,
            ["block_other_b_test"] = null,
            ["block_other_c"] = null,
            ["block_shadow_a"] = null,
            ["block_shadow_b"] = null,
            ["block_shadow_c"] = null,
            ["block_shadow_d"] = null,
            ["block_shadow_h"] = null,
            ["floor_face"] = null,
            ["floor_lower"] = null,
            ["teleporter"] = null,
            ["snow"] = GameResources.GetGameResource<Texture2D>("Assets/christmas/snow")
    };

        public static void LoadTexturePack(string folder)
        {
            if (folder.ToLower() == "vanilla")
            {
                LoadVanillaTextures();
                GameHandler.ClientLog.Write($"Loaded vanilla textures for Scene.", LogType.Info);
                return;
            }

            var baseRoot = Path.Combine(TankGame.SaveDirectory, "Texture Packs");
            var rootGameScene = Path.Combine(TankGame.SaveDirectory, "Texture Packs", "Scene");
            var path = Path.Combine(rootGameScene, folder);

            // ensure that these directories exist before dealing with them
            Directory.CreateDirectory(baseRoot);
            Directory.CreateDirectory(rootGameScene);

            if (!Directory.Exists(path))
            {
                GameHandler.ClientLog.Write($"Error: Directory '{path}' not found when attempting texture pack load.", LogType.Warn);
                return;
            }

            AssetRoot = path;

            foreach (var file in Directory.GetFiles(path))
            {
                if (Assets.Any(type => type.Key == Path.GetFileNameWithoutExtension(file)))
                {
                    Assets[Path.GetFileNameWithoutExtension(file)] = Texture2D.FromFile(TankGame.Instance.GraphicsDevice, Path.Combine(path, Path.GetFileName(file)));
                    GameHandler.ClientLog.Write($"Texture pack '{folder}' overrided texture '{Path.GetFileNameWithoutExtension(file)}'", LogType.Info);
                }
            }
        }

        public static void LoadVanillaTextures()
        {
            AssetRoot = "Assets/textures/ingame";
            static Texture2D get(string s)
                => GameResources.GetGameResource<Texture2D>(Path.Combine(AssetRoot, s));

            Assets["block.1"] = get("block.1");
            Assets["block.2"] = get("block.2");
            Assets["block_harf.1"] = get("block_harf.1");
            Assets["block_other_a"] = get("block_other_a");
            Assets["block_other_b"] = get("block_other_b");
            Assets["block_other_b_test"] = get("block_other_b_test");
            Assets["block_other_c"] = get("block_other_c");
            Assets["block_shadow_a"] = get("block_shadow_a");
            Assets["block_shadow_b"] = get("block_shadow_b");
            Assets["block_shadow_c"] = get("block_shadow_c");
            Assets["block_shadow_d"] = get("block_shadow_d");
            Assets["block_shadow_h"] = get("block_shadow_h");
            Assets["floor_face"] = get("floor_face");
            Assets["floor_lower"] = get("floor_lower");
            Assets["teleporter"] = get("teleporter");
        }

        public static class FloorRenderer
        {
            public static Model FloorModelBase;

            public static float scale = 1f;

            public static void LoadFloor()
            {
                FloorModelBase = GameResources.GetGameResource<Model>("Assets/floor_big");
                switch (Theme)
                {
                    case MapTheme.Vanilla:

                        AssetRoot = "Assets/textures/ingame/";

                        break;

                    case MapTheme.Christmas:

                        AssetRoot = "Assets/christmas/";

                        break;
                }
            }
            // TODO: finish christmas stuff kekw failure
            public static void RenderFloor()
            {
                foreach (var mesh in FloorModelBase.Meshes) {
                    foreach (BasicEffect effect in mesh.Effects) {
                        effect.View = View;
                        effect.Projection = Projection;
                        effect.World = World * Matrix.CreateScale(scale);

                        effect.SetDefaultGameLighting();

                        effect.TextureEnabled = true;

                        //effect.Texture = GameResources.GetGameResource<Texture2D>(AssetRoot + "floor_face");
                        effect.Texture = Theme switch {
                            MapTheme.Vanilla => Assets["floor_face"],
                            MapTheme.Christmas => Assets["snow"],
                            _ => null
                        };
                    }

                    mesh.Draw();
                }
            }
        }


        // make proper texturepack loading.
        public static class BoundsRenderer
        {
            public static Body[] Boundaries = new Body[4];
            public static Model BoundaryModel;

            public static void LoadBounds()
            {
                // 0 -> top, 1 -> right, 2 -> bottom, 3 -> left
                Boundaries[0] = Tank.CollisionsWorld.CreateRectangle(1000 / Tank.UNITS_PER_METER, 5 / Tank.UNITS_PER_METER, 1f, new Vector2(MIN_X, MIN_Y - 7) / Tank.UNITS_PER_METER, 0f, BodyType.Static);

                Boundaries[1] = Tank.CollisionsWorld.CreateRectangle(5 / Tank.UNITS_PER_METER, 1000 / Tank.UNITS_PER_METER, 1f, new Vector2(MAX_X + 7, MAX_Y) / Tank.UNITS_PER_METER, 0f, BodyType.Static);

                Boundaries[2] = Tank.CollisionsWorld.CreateRectangle(1000 / Tank.UNITS_PER_METER, 5 / Tank.UNITS_PER_METER, 1f, new Vector2(MIN_X, MAX_Y + 7) / Tank.UNITS_PER_METER, 0f, BodyType.Static);

                Boundaries[3] = Tank.CollisionsWorld.CreateRectangle(5 / Tank.UNITS_PER_METER, 1000 / Tank.UNITS_PER_METER, 1f, new Vector2(MIN_X - 7, MAX_Y) / Tank.UNITS_PER_METER, 0f, BodyType.Static);
                
                switch (Theme)
                {
                    case MapTheme.Vanilla:
                        BoundaryModel = GameResources.GetGameResource<Model>("Assets/toy/outerbounds");

                        SetBlockTexture(BoundaryModel.Meshes["polygon48"], BoundaryTextureContext.block_other_c);
                        SetBlockTexture(BoundaryModel.Meshes["polygon40"], BoundaryTextureContext.block_other_a);
                        SetBlockTexture(BoundaryModel.Meshes["polygon33"], BoundaryTextureContext.block_other_b_test);
                        SetBlockTexture(BoundaryModel.Meshes["polygon7"], BoundaryTextureContext.block_shadow_b);
                        SetBlockTexture(BoundaryModel.Meshes["polygon15"], BoundaryTextureContext.block_shadow_b);

                        SetBlockTexture(BoundaryModel.Meshes["polygon5"], BoundaryTextureContext.block_shadow_h);

                        SetBlockTexture(BoundaryModel.Meshes["polygon20"], BoundaryTextureContext.block_shadow_d);

                        SetBlockTexture(BoundaryModel.Meshes["polygon21"], BoundaryTextureContext.block_shadow_b);

                        SetBlockTexture(BoundaryModel.Meshes["polygon32"], BoundaryTextureContext.floor_lower);

                        break;
                    case MapTheme.Christmas:
                        BoundaryModel = GameResources.GetGameResource<Model>("Assets/christmas/snowy");

                        SetBlockTexture(BoundaryModel.Meshes["polygon48"], BoundaryTextureContext.block_other_c);
                        SetBlockTexture(BoundaryModel.Meshes["polygon40"], BoundaryTextureContext.block_other_a);
                        SetBlockTexture(BoundaryModel.Meshes["polygon33"], BoundaryTextureContext.block_other_b_test);
                        SetBlockTexture(BoundaryModel.Meshes["polygon7"], BoundaryTextureContext.block_shadow_b);
                        SetBlockTexture(BoundaryModel.Meshes["polygon15"], BoundaryTextureContext.block_shadow_b);

                        SetBlockTexture(BoundaryModel.Meshes["polygon5"], BoundaryTextureContext.block_shadow_h);

                        SetBlockTexture(BoundaryModel.Meshes["polygon20"], BoundaryTextureContext.block_shadow_d);

                        SetBlockTexture(BoundaryModel.Meshes["polygon21"], BoundaryTextureContext.block_shadow_b);

                        SetBlockTexture(BoundaryModel.Meshes["polygon32"], BoundaryTextureContext.floor_lower);

                        SetBlockTexture(BoundaryModel.Meshes["snow_field"], "snow");
                        SetBlockTexture(BoundaryModel.Meshes["snow_blocks"], "snow");
                        break;
                }
            }

            public static void RenderBounds()
            {
                switch (Theme)
                {
                    case MapTheme.Vanilla:
                        foreach (var mesh in BoundaryModel.Meshes) {
                            foreach (BasicEffect effect in mesh.Effects) {
                                effect.View = View;
                                effect.Projection = Projection;
                                effect.World = World;

                                if (mesh.Name == "polygon2")
                                    effect.Alpha = 0.1f;
                                else
                                    effect.Alpha = 1f;

                                effect.SetDefaultGameLighting();
                            }

                            mesh.Draw();
                        }
                        break;
                    case MapTheme.Christmas:
                        foreach (var mesh in BoundaryModel.Meshes) {
                            foreach (BasicEffect effect in mesh.Effects) {
                                effect.View = View;
                                effect.Projection = Projection;
                                effect.World = World;

                                if (mesh.Name == "snow_field" || mesh.Name == "snow_blocks")
                                    effect.World = Matrix.CreateRotationX(-MathHelper.PiOver2) * Matrix.CreateScale(62) * Matrix.CreateTranslation(Center);//Matrix.CreateScale(0.62f) * Matrix.CreateTranslation(0, 0, 130) * 
                                        //Matrix.CreateRotationX(/*-MathHelper.PiOver2*/ GameUtils.MousePosition.X / 300) * Matrix.CreateScale(1f);
                                if (mesh.Name == "polygon2")
                                    effect.Alpha = 0.1f;
                                else
                                    effect.Alpha = 1f;

                                effect.SetDefaultGameLighting();
                            }

                            mesh.Draw();
                        }
                        break;
                }
            }

            /// <summary>
            /// Sets a block texture in the boundary model to the context.
            /// </summary>
            /// <param name="mesh"></param>
            /// <param name="meshNameMatch"></param>
            /// <param name="textureContext"></param>
            private static void SetBlockTexture(ModelMesh mesh, BoundaryTextureContext context) {
                foreach (BasicEffect effect in mesh.Effects)
                    effect.Texture = Assets[context.ToString()];//GameResources.GetGameResource<Texture2D>($"{AssetRoot}{context}");
            }
            private static void SetBlockTexture(ModelMesh mesh, string textureName) {
                foreach (BasicEffect effect in mesh.Effects)
                    effect.Texture = Assets[textureName.ToString()];//GameResources.GetGameResource<Texture2D>($"{AssetRoot}{context}");
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

        public const float MIN_X = -234;
        public const float MAX_X = 234;
        public const float MIN_Y = -48;
        public const float MAX_Y = 312;

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

        public static float Scale = 0.62f;
        public static Vector3 Center = new Vector3(0, 0, 130);

        public static void RenderWorldModels()
        {
            View = TankGame.GameView;
            Projection = TankGame.GameProjection;
            World = Matrix.CreateScale(Scale) * Matrix.CreateTranslation(Center);

            if (ShouldRender)
            {
                FloorRenderer.RenderFloor();
                BoundsRenderer.RenderBounds();
            }
        }
    }
}