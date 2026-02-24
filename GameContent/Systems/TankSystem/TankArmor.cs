using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Internals;
using TanksRebirth.Graphics;
using TanksRebirth.GameContent.Globals.Assets;
using System;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.Systems.AI;

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

    const int ARMOR_OFF_Z = 6;
    static readonly Vector3[] OffsetsHP1 = [Vector3.Zero];
    static readonly Vector3[] OffsetsHP2 = [new(0, 0, ARMOR_OFF_Z), new(0, 0, -ARMOR_OFF_Z)];
    static readonly Vector3[] OffsetsHP3 = [new(0, 0, ARMOR_OFF_Z), Vector3.Zero, new(0, 0, -ARMOR_OFF_Z)];

    public TankArmor(Tank host, int hitPoints) {
        Host = host;
        HitPoints = _hitpointsMax = hitPoints;

        _model = ModelGlobals.Armor.Duplicate();
        _maskingTexture = GameResources.GetGameResource<Texture2D>("Assets/textures/misc/armor");
    }
    public void Render(bool drawHealthBar = true) {
        if (HitPoints <= 0) return;

        if (drawHealthBar && _hitpointsMax > 3 && HitPoints > 0) {
            DrawHealthBar(damageColor: Color.Red, healthColor: Color.Lime);
        }

        if (HideArmor) return;

        int visualPieces = HitPoints;

        // if we have more than 3 max HP, scale the visual pieces proportionally
        if (_hitpointsMax > 3) {
            float healthPercent = (float)HitPoints / _hitpointsMax;
            visualPieces = (int)MathF.Ceiling(healthPercent * 3f);
        }

        var activeOffsets = visualPieces switch {
            1 => OffsetsHP1,
            2 => OffsetsHP2,
            _ => OffsetsHP3
        };

        var baseTransform = Matrix.CreateRotationY(-Host.ChassisRotation)
                             * Matrix.CreateTranslation(Host.Position3D);

        foreach (ModelMesh mesh in _model.Meshes) {
            foreach (BasicEffect effect in mesh.Effects) {
                effect.View = Host.DrawParams.View;
                effect.Projection = Host.DrawParams.Projection;
                effect.TextureEnabled = true;
                effect.Texture = _maskingTexture;
                effect.SetDefaultGameLighting_IngameEntities();
            }
        }

        foreach (var offset in activeOffsets) {
            var world = Matrix.CreateTranslation(offset) * baseTransform;

            foreach (ModelMesh mesh in _model.Meshes) {
                foreach (BasicEffect effect in mesh.Effects) {
                    effect.World = world;
                }
                mesh.Draw();
            }
        }
    }
    public void DrawHealthBar(Color damageColor, Color healthColor) {
        var dims = new Vector2(100, 20).ToResolution();
        var anchor = Anchor.Center;
        var screenPos = MatrixUtils.ConvertWorldToScreen(new Vector3(0, 30, 0), Host.DrawParams.World, Host.DrawParams.View, Host.DrawParams.Projection);

        var tex = TextureGlobals.Pixels[Color.White];
        DrawUtils.DrawTextureWithShadow(TankGame.SpriteRenderer, tex,
            screenPos, Vector2.UnitY, damageColor, dims, 1f, anchor, shadowAlpha: 0.5f, shadowDistScale: 0.5f);
        TankGame.SpriteRenderer.Draw(tex, screenPos, null, healthColor, 0f, tex.Size() / 2, new Vector2(dims.X * (float)HitPoints / _hitpointsMax, dims.Y), default, 0);
    }
    /// <summary>Remove this <see cref="TankArmor"/> from memory.</summary>
    public void Remove() {
        // i think this works
        Host.Extras.Armor = null;
    }
}