using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Internals;
using TanksRebirth.Graphics;
using TanksRebirth.GameContent.Globals.Assets;

namespace TanksRebirth.GameContent.Systems.TankSystem;

// work on 2d drawing the hp bar
public class TankArmor {
    /// <summary>The tank who has this armor.</summary>
    public Tank Host;
    public int HitPoints;
    public bool HideArmor;

    readonly int _hitpointsMax;

    readonly Texture2D _maskingTexture;
    readonly Model _model;

    public TankArmor(Tank host, int hitPoints) {
        _model = ModelGlobals.Armor.Asset;
        Host = host;
        HitPoints = _hitpointsMax = hitPoints;
        _maskingTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/misc/armor");
    }
    public void Render(bool canRenderHealthBar = true) {
        if (HideArmor) return;

        if (canRenderHealthBar && _hitpointsMax > 3) {
            // SetHealthBar(5, 2);
            // draw health bar in 2d
        }
        // DrawHealthBar(MatrixUtils.ConvertWorldToScreen(new Vector3(0, 20, 0f), Host.World, CameraGlobals.GameView, TankGame.GameProjection) - new Vector2(0, 20), 50, 10);

        if (HitPoints < 0) // so armor point amount is clamped to be greater than 0 at all times.
            HitPoints = 0;

        Vector2[] offset = [ Vector2.Zero, Vector2.Zero, Vector2.Zero ];
        bool[] render = [ false, false, false ]; // whether or not to render each.
        switch (HitPoints) {
            case 0:
                // we dont really want to render anything since there isn't any armor present, so call return.
                return;
            case 1:
                render[1] = true; // make the middle armor render.
                break;
            case 2:
                offset[0] = new Vector2(0, 5);
                offset[2] = new Vector2(0, -5);

                render[0] = true; // make left hand armor render.
                render[2] = true; // make right hand armor render.
                break;
            default: // for any case > 2
                offset[0] = new Vector2(0, 5);
                offset[2] = new Vector2(0, -5);

                render[0] = true; // make left hand armor render.
                render[1] = true; // make the middle armor render.
                render[2] = true; // make right hand armor render.
                break;
        }

        float scale = 100f;

        for (int i = 0; i < HitPoints; i++) {
            foreach (ModelMesh mesh in _model.Meshes) {
                foreach (BasicEffect effect in mesh.Effects) {
                    //if (render[i])
                    //{
                    if (i < 3) {
                        effect.World = Matrix.CreateRotationX(-MathHelper.PiOver2)
                             * Matrix.CreateRotationY(-Host.ChassisRotation)
                             * Matrix.CreateScale(scale)
                             * Matrix.CreateTranslation(Host.Position3D + offset[i].RotatedBy(Host.ChassisRotation).ExpandZ());
                    }
                    //}
                    effect.View = Host.DrawParams.View;
                    effect.Projection = Host.DrawParams.Projection;

                    effect.SetDefaultGameLighting_IngameEntities();

                    effect.TextureEnabled = true;

                    effect.Texture = _maskingTexture;
                }
                mesh.Draw();
            }
        }
    }
    /// <summary>Remove this <see cref="TankArmor"/> from memory.</summary>
    public void Remove() {
        // i think this works
        Host.Properties.Armor = null;
    }
}