﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Internals;
using TanksRebirth.Graphics;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.Globals.Assets;
using TanksRebirth.GameContent.Systems.ParticleSystem;

namespace TanksRebirth.GameContent;

public class TankArmor {
    /// <summary>The tank who has this armor.</summary>
    public Tank Host;

    private readonly int _hitpointsMax;
    public int HitPoints;

    private readonly Texture2D _maskingTexture;
    private readonly Model _model;

    public bool HideArmor;

    private readonly Particle _healthBarTotal;
    private readonly Particle _healthBarCurrent;

    public TankArmor(Tank host, int hitPoints) {
        _model = ModelGlobals.Armor.Asset;
        Host = host;
        HitPoints = _hitpointsMax = hitPoints;
        _maskingTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/misc/armor");

        _healthBarTotal = GameHandler.Particles.MakeParticle(host.Position3D + new Vector3(0, 20, 0), TextureGlobals.Pixels[Color.White]);
        _healthBarCurrent = GameHandler.Particles.MakeParticle(host.Position3D + new Vector3(0, 20, 0), TextureGlobals.Pixels[Color.White]);

        _healthBarTotal.HasAddativeBlending = false;
        _healthBarCurrent.HasAddativeBlending = false;

        _healthBarTotal.Color = Color.Red;
        _healthBarCurrent.Color = Color.Lime;

        _healthBarCurrent.Roll = -CameraGlobals.DEFAULT_ORTHOGRAPHIC_ANGLE;
        _healthBarTotal.Roll = -CameraGlobals.DEFAULT_ORTHOGRAPHIC_ANGLE;
    }
    public void Render(bool canRenderHealthBar = true) {
        if (HideArmor) return;
        /*void DrawHealthBar(Vector2 position, float width, float height)
        {
            //TankGame.spriteBatch.Draw(GameResources.GetGameResource<Texture2D>("Assets/textures/WhitePixel"), new Rectangle((int)(position.X - _hitpointsMax / 2 * width), (int)position.Y, (int)(HitPoints * width), (int)height), Color.Red);
            //TankGame.spriteBatch.Draw(GameResources.GetGameResource<Texture2D>("Assets/textures/WhitePixel"), new Rectangle((int)(position.X - _hitpointsMax / 2 * width), (int)position.Y, (int)(HitPoints * width), (int)height), Color.Lime);
        }*/

        void setHealthBar(float xScl, float yScl) {
            _healthBarTotal.Scale = new(xScl, yScl, 1f);
            _healthBarCurrent.Scale = new(xScl * (HitPoints + 1) / (_hitpointsMax + 1), yScl, 1f);
        }

        if (canRenderHealthBar && _hitpointsMax > 3) {
            setHealthBar(5, 2);
            _healthBarTotal.Position = Host.Position3D + new Vector3(0, 40, 0);
            _healthBarCurrent.Position = Host.Position3D + new Vector3(0, 40, 0);
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
                             * Matrix.CreateRotationY(-Host.TankRotation)
                             * Matrix.CreateScale(scale)
                             * Matrix.CreateTranslation(Host.Position3D + offset[i].Rotate(Host.TankRotation).ExpandZ());
                    }
                    //}
                    effect.View = Host.View;
                    effect.Projection = Host.Projection;

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
        _healthBarTotal.Destroy();
        _healthBarCurrent.Destroy();

        // i think this works
        Host.Properties.Armor = null;
    }
}