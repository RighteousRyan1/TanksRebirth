using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using tainicom.Aether.Physics2D.Dynamics;
using TanksRebirth.GameContent;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.Globals.Assets;
using TanksRebirth.GameContent.Tanks;
using TanksRebirth.Graphics.Drawing;
using TanksRebirth.Internals;

namespace TanksRebirth.Graphics;

#pragma warning disable
public enum MapTheme {
    Vanilla,
    Christmas,
}
public static class GameScene {
    public static bool UpdateAndRender = true;

    static bool _ucscBacking;
    public static bool UseCustomSceneColor {
        get => _ucscBacking;

        set {
            // only do if necessary
            if (!value && _ucscBacking)
                BoundsRenderer.SetBoundTextureDefaults();
            _ucscBacking = value;
        }
    }

    public static Color SceneRenderColor = Color.Transparent;

    // wrong and also useless?
    public static readonly Vector2 MapCenter = Vector2.Zero;

    public delegate void PostLoadBoundsDelegate();
    public static event PostLoadBoundsDelegate PostLoadBounds;
    public delegate void PostLoadFloorDelegate();
    public static event PostLoadFloorDelegate PostLoadFloor;
    public delegate void PostLoadTexturesDelegate();
    public static event PostLoadTexturesDelegate PostLoadTextures;

    public static BasicDrawParams DrawParams;

    public static string AssetRoot;

    public static MapTheme Theme { get; set; } = MapTheme.Vanilla;

    public static Dictionary<string, Texture2D> Assets = new() {
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

    public static void LoadTexturePack(string folder) {
        LoadVanillaTextures();
        if (folder.Equals("vanilla", StringComparison.CurrentCultureIgnoreCase)) {
            TankGame.ClientLog.Write($"Loaded vanilla textures for Scene.", LogType.Info);
            return;
        }

        var baseRoot = Path.Combine(TankGame.SaveDirectory, "Resource Packs");
        var rootGameScene = Path.Combine(baseRoot, "Scene");
        var path = Path.Combine(rootGameScene, folder);

        // ensure that these directories exist before dealing with them
        Directory.CreateDirectory(baseRoot);
        Directory.CreateDirectory(rootGameScene);

        if (!Directory.Exists(path)) {
            TankGame.ClientLog.Write($"Error: Directory '{path}' not found when attempting texture pack load.", LogType.Warn);
            return;
        }

        AssetRoot = path;

        foreach (var file in Directory.GetFiles(path)) {
            if (Assets.Any(type => type.Key == Path.GetFileNameWithoutExtension(file))) {
                Assets[Path.GetFileNameWithoutExtension(file)] = Texture2D.FromFile(TankGame.Instance.GraphicsDevice, Path.Combine(path, Path.GetFileName(file)));
                TankGame.ClientLog.Write($"Texture pack '{folder}' overrided texture '{Path.GetFileNameWithoutExtension(file)}'", LogType.Info);
            }
        }
    }

    public static void LoadVanillaTextures() {
        AssetRoot = "Assets/textures/ingame";
        static Texture2D get(string s)
            => GameResources.GetGameResource<Texture2D>(Path.Combine(AssetRoot, s), premultiply: true);

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

        PostLoadTextures?.Invoke();
    }

    public static class FloorRenderer {
        public static Model FloorModelBase;

        public static float scale = 1f;

        public static void LoadFloor() {
            FloorModelBase = ModelGlobals.Floor.Asset;
            switch (Theme) {
                case MapTheme.Vanilla:
                    AssetRoot = "Assets/textures/ingame/";
                    break;

                case MapTheme.Christmas:
                    AssetRoot = "Assets/christmas/";
                    break;
            }
            PostLoadFloor?.Invoke();
        }
        // TODO: finish christmas stuff kekw failure
        public static void RenderFloor() {
            scale = 0.95f; // cus naturally it's a little large?

            foreach (var mesh in FloorModelBase.Meshes) {
                foreach (BasicEffect effect in mesh.Effects) {
                    effect.View = DrawParams.View;
                    effect.Projection = DrawParams.Projection;
                    effect.World = DrawParams.World * Matrix.CreateScale(scale);

                    effect.TextureEnabled = true;
                    effect.Texture = Assets["floor_lower"];

                    if (UseCustomSceneColor) {
                        effect.Texture = TextureGlobals.Pixels[SceneRenderColor];
                        effect.LightingEnabled = false;
                        // effect.VertexColorEnabled = false;
                        continue;
                    }
                    effect.Texture = Theme switch {
                        MapTheme.Vanilla => Assets["floor_face"],
                        MapTheme.Christmas => Assets["snow"],
                        _ => null
                    };

                    effect.SetDefaultGameLighting();
                }

                mesh.Draw();
            }
        }
    }


    // make proper texturepack loading.
    public static class BoundsRenderer {
        public const string BOUNDARY_TAG = "bounds";
        public static Body[] Boundaries = new Body[4];
        public static Model BoundaryModel;

        public static void LoadBounds() {
            // 0 -> top, 1 -> right, 2 -> bottom, 3 -> left
            Boundaries[0] = Tank.CollisionsWorld.CreateRectangle(1000 / Tank.UNITS_PER_METER, 5 / Tank.UNITS_PER_METER, 1f,
                new Vector2(MIN_X, MIN_Z - 4) / Tank.UNITS_PER_METER, 0f, BodyType.Static);

            Boundaries[1] = Tank.CollisionsWorld.CreateRectangle(5 / Tank.UNITS_PER_METER, 1000 / Tank.UNITS_PER_METER, 1f,
                new Vector2(MAX_X + 7, MAX_Z) / Tank.UNITS_PER_METER, 0f, BodyType.Static);

            Boundaries[2] = Tank.CollisionsWorld.CreateRectangle(1000 / Tank.UNITS_PER_METER, 5 / Tank.UNITS_PER_METER, 1f,
                new Vector2(MIN_X, MAX_Z + 4) / Tank.UNITS_PER_METER, 0f, BodyType.Static);

            Boundaries[3] = Tank.CollisionsWorld.CreateRectangle(5 / Tank.UNITS_PER_METER, 1000 / Tank.UNITS_PER_METER, 1f,
                new Vector2(MIN_X - 7, MAX_Z) / Tank.UNITS_PER_METER, 0f, BodyType.Static);

            Array.ForEach(Boundaries, x => x.Tag = BOUNDARY_TAG);

            SetBoundTextureDefaults();
            PostLoadBounds?.Invoke();
        }

        public static void SetBoundTextureDefaults() {
            switch (Theme) {
                case MapTheme.Vanilla:
                    BoundaryModel = ModelGlobals.GameBoundary.Asset;
                    break;
                case MapTheme.Christmas:
                    BoundaryModel = ModelGlobals.GameBoundarySnowy.Asset;
                    SetBlockTexture(BoundaryModel.Meshes["snow_field"], "snow");
                    SetBlockTexture(BoundaryModel.Meshes["snow_blocks"], "snow");
                    break;
            }
            SetBlockTexture(BoundaryModel.Meshes["polygon48"], BoundaryTextureContext.block_other_c);
            SetBlockTexture(BoundaryModel.Meshes["polygon40"], BoundaryTextureContext.block_other_a);
            SetBlockTexture(BoundaryModel.Meshes["polygon33"], BoundaryTextureContext.block_other_b_test);

            var tryShadow7 = BoundaryModel.Meshes.TryGetValue("shadow_7", out var shadow_7);
            if (tryShadow7)
                SetBlockTexture(shadow_7, BoundaryTextureContext.block_shadow_b); // polygon15

            SetBlockTexture(BoundaryModel.Meshes["polygon5"], BoundaryTextureContext.block_shadow_h);

            SetBlockTexture(BoundaryModel.Meshes["shadow_interior_upper"], BoundaryTextureContext.block_shadow_d);
            SetBlockTexture(BoundaryModel.Meshes["shadow_interior_vertical"], BoundaryTextureContext.block_shadow_b); // polygon7
            SetBlockTexture(BoundaryModel.Meshes["shadow_corner"], BoundaryTextureContext.block_shadow_b);
        }

        public static void RenderBounds() {
            // more hardcode yay
            switch (Theme) {
                case MapTheme.Vanilla:
                    foreach (var mesh in BoundaryModel.Meshes) {
                        if (mesh.Name.Contains("outer", StringComparison.InvariantCultureIgnoreCase)) {
                            continue;
                        }
                        else if (mesh.Name.Contains("shadow", StringComparison.InvariantCultureIgnoreCase)) {
                            TankGame.Instance.GraphicsDevice.SamplerStates[0] = RenderGlobals.ClampingSampler;
                        }

                        foreach (BasicEffect effect in mesh.Effects) {

                            effect.View = DrawParams.View;
                            effect.Projection = DrawParams.Projection;
                            effect.World = DrawParams.World;
                            effect.VertexColorEnabled = true;

                            if (UseCustomSceneColor) {
                                effect.Texture = TextureGlobals.Pixels[SceneRenderColor];
                                effect.LightingEnabled = false;
                                effect.VertexColorEnabled = false;
                                continue;
                            }

                            if (mesh.Name == "polygon2")
                                effect.Alpha = 0.1f;
                            else
                                effect.Alpha = 1f;

                            effect.SetDefaultGameLighting();
                        }

                        mesh.Draw();

                        TankGame.Instance.GraphicsDevice.SamplerStates[0] = RenderGlobals.WrappingSampler;
                    }
                    break;
                case MapTheme.Christmas:
                    foreach (var mesh in BoundaryModel.Meshes) {
                        foreach (BasicEffect effect in mesh.Effects) {
                            effect.View = DrawParams.View;
                            effect.Projection = DrawParams.Projection;
                            effect.World = DrawParams.World;
                            effect.TextureEnabled = true;
                            effect.Alpha = 1f;

                            if (mesh.Name == "snow_field" || mesh.Name == "snow_blocks") {
                                // ... apparently my initial assignment just... doesn't work. so it's set here.
                                effect.Texture = Assets["snow"];
                            }

                            if (UseCustomSceneColor) {
                                effect.Texture = TextureGlobals.Pixels[SceneRenderColor];
                                continue;
                            }

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
        private static void SetBlockTexture(ModelMesh mesh, BoundaryTextureContext context) {
            foreach (BasicEffect effect in mesh.Effects)
                effect.Texture = Assets[context.ToString()];
        }
        private static void SetBlockTexture(ModelMesh mesh, string textureName) {
            foreach (BasicEffect effect in mesh.Effects)
                effect.Texture = Assets[textureName.ToString()];
        }

        private enum BoundaryTextureContext {
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

    // the padding coordinates for a tank for it to not visually clip into a wall
    public const float TANKS_MIN_X = MIN_X + 6;
    public const float TANKS_MAX_X = MAX_X - 6;
    public const float TANKS_MIN_Y = MIN_Z + 5;
    public const float TANKS_MAX_Y = MAX_Z - 6;

    // the min/max coordinates of a point, in units, where an outer wall is met on the map
    public const float MIN_X = -234;
    public const float MAX_X = 234;
    public const float MIN_Z = -182;
    public const float MAX_Z = 182;

    // the padding coordinates for a block for it to not visually clip into a wall
    public const float CUBE_MIN_X = MIN_X + Block.SIDE_LENGTH / 2 - 6f;
    public const float CUBE_MAX_X = MAX_X - Block.SIDE_LENGTH / 2;
    public const float CUBE_MIN_Z = MIN_Z + Block.SIDE_LENGTH / 2 - 6f;
    public const float CUBE_MAX_Z = MAX_Z - Block.SIDE_LENGTH / 2;

    public static Vector3 TopLeft => new(CUBE_MIN_X, 0, CUBE_MAX_Z);
    public static Vector3 TopRight => new(CUBE_MAX_X, 0, CUBE_MAX_Z);
    public static Vector3 BottomLeft => new(CUBE_MIN_X, 0, CUBE_MIN_Z);
    public static Vector3 BottomRight => new(CUBE_MAX_X, 0, CUBE_MIN_Z);
    public static void InitializeRenderers() {
        FloorRenderer.LoadFloor();
        BoundsRenderer.LoadBounds();
    }

    public static float Scale = 0.62f;
    public static Vector3 Center = Vector3.Zero;

    public static void RenderWorldModels() {
        DrawParams.View = CameraGlobals.GameView;
        DrawParams.Projection = CameraGlobals.GameProjection;
        DrawParams.World = Matrix.CreateScale(Scale) * Matrix.CreateTranslation(Center);

        if (UpdateAndRender) {
            FloorRenderer.RenderFloor();
            BoundsRenderer.RenderBounds();
        }
    }
}