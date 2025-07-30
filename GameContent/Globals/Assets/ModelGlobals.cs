using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TanksRebirth.GameContent.Globals.Assets;

#pragma warning disable
// only ModelResources for now. maybe TextureResources
public static class ModelGlobals {
    // general models
    public static Resource<Model> Armor = new(PathGlobals.MODEL_PATH, "armor");
    public static Resource<Model> BoxFace = new(PathGlobals.MODEL_PATH, "box_face");
    public static Resource<Model> Bullet = new(PathGlobals.MODEL_PATH, "bullet");
    public static Resource<Model> Chest = new(PathGlobals.MODEL_PATH, "chest");
    public static Resource<Model> Dome = new(PathGlobals.MODEL_PATH, "dome");
    public static Resource<Model> FlatFace = new(PathGlobals.MODEL_PATH, "flat_face");
    public static Resource<Model> Key = new(PathGlobals.MODEL_PATH, "key");
    public static Resource<Model> Medal = new(PathGlobals.MODEL_PATH, "medal");
    public static Resource<Model> Mine = new(PathGlobals.MODEL_PATH, "mine");
    public static Resource<Model> Ping = new(PathGlobals.MODEL_PATH, "ping");
    public static Resource<Model> Plane = new(PathGlobals.MODEL_PATH, "plane");
    public static Resource<Model> Smoke = new(PathGlobals.MODEL_PATH, "smoke");
    public static Resource<Model> SmokeGrenade = new(PathGlobals.MODEL_PATH, "smoke_grenade");
    public static Resource<Model> TankEnemy = new(PathGlobals.MODEL_PATH, "tank_e");
    public static Resource<Model> TankPlayer = new(PathGlobals.MODEL_PATH, "tank_p");
    public static Resource<Model> Teleporter = new(PathGlobals.MODEL_PATH, "teleporter");

    // scene models
    public static Resource<Model> GameBoundary = new(PathGlobals.SCENE_PATH, "outer_bounds");
    public static Resource<Model> Floor = new(PathGlobals.SCENE_PATH, "scene_floor");
    public static Resource<Model> BlockStack = new(PathGlobals.SCENE_PATH, "block_stack");
    public static Resource<Model> BlockStackAlt = new(PathGlobals.SCENE_PATH, "block_stack_alt");

    public static Resource<Model> GameBoundarySnowy = new(PathGlobals.CHRISTMAS_PATH, "outer_bounds_snowy");
    public static Resource<Model> BlockStackSnowy = new(PathGlobals.CHRISTMAS_PATH, "block_stack_alt_snowy");
    public static Resource<Model> BlockStackAltSnowy = new(PathGlobals.CHRISTMAS_PATH, "block_stack_alt_snowy");

    // skybox model
    public static Resource<Model> Room = new(PathGlobals.SKYBOX_PATH, "room");

    // cosmetics
    public static Resource<Model> ArmyHat = new(PathGlobals.COSMETICS_PATH, "army_hat");
    public static Resource<Model> BlenderDefaultCube = new(PathGlobals.COSMETICS_PATH, "blender_default_cube");
    public static Resource<Model> Crown = new(PathGlobals.COSMETICS_PATH, "crown");
    public static Resource<Model> Halo = new(PathGlobals.COSMETICS_PATH, "halo");
    public static Resource<Model> Horns = new(PathGlobals.COSMETICS_PATH, "horns");
    public static Resource<Model> SantaHat = new(PathGlobals.COSMETICS_PATH, "santa_hat");
    public static Resource<Model> WitchHat = new(PathGlobals.COSMETICS_PATH, "witch_hat");

    // misc
    public static Resource<Model> Logo = new(PathGlobals.MODEL_PATH + "/logo", "logo");
    public static void Initialize() {
        
    }
}
