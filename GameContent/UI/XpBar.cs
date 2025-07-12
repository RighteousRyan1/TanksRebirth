using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using TanksRebirth.Internals.Common.Utilities;
using FontStashSharp;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.Net;

namespace TanksRebirth.GameContent.UI;

public class XpBar {
    // TODO: make fancy and cool
    float _interp;

    public float ApproachValue;

    public ushort Level;

    public float Value;
    public float MaxValue;

    public Vector2 Scale;
    public Vector2 Position;

    public Color EmptyColor;
    public Color FillColor;
    public Color GainedColor;

    public Anchor Alignment;

    public void GainExperience(float xp) {
        _interp = 0;
        ApproachValue += xp;

        if (ApproachValue > MaxValue) {
            ApproachValue -= MaxValue;
            Level++;
        }
    }

    public void Update() {
        if (_interp < 1) {
            _interp += 0.005f * RuntimeData.DeltaTime;

            // later.
            /*var p = GameHandler.Particles.MakeParticle(new Vector3(), TextureGlobals.Pixels[Color.White]);
            p.Color = GainedColor;

            float initVelY = Client.ClientRandom.NextFloat(-0.1f, 0.1f);
            // p.Position.X = 

            p.UniqueBehavior = (a) => {
                float velX = Client.ClientRandom.NextFloat(-0.5f, -0.1f) * RuntimeData.DeltaTime;
            };*/
        }
        else _interp = 1;

        Value += (ApproachValue - Value) * Easings.GetEasingBehavior(EasingFunction.InOutQuint, _interp);
    }

    // todo, when xp gained, draw a text particle that shows how much was gained
    // also when leveling up make it go to end and then go to supposed xp value
    public void Render(SpriteBatch sb) {
        var text = $"Level: {Level} | {MathF.Floor(Value * 100)}%";
        // draw empty xp
        DrawUtils.DrawTextWithBorder(sb, FontGlobals.RebirthFont, text, Position - (Vector2.UnitY * 20).ToResolution(), Color.White, Color.Black, new Vector2(0.6f).ToResolution(),
            0f, Alignment, borderThickness: 0.5f);

        sb.Draw(TextureGlobals.Pixels[Color.White], Position, null, EmptyColor, 0f, GameUtils.GetAnchor(Alignment, TextureGlobals.Pixels[Color.White].Size()), Scale.ToResolution(), default, 0f);

        // draw approaching xp value
        sb.Draw(TextureGlobals.Pixels[Color.White], Position, null, GainedColor, 0f, GameUtils.GetAnchor(Alignment, TextureGlobals.Pixels[Color.White].Size()), new Vector2(Scale.X * ApproachValue, Scale.Y).ToResolution(), default, 0f);

        // draw gained xp
        sb.Draw(TextureGlobals.Pixels[Color.White], Position , null, FillColor, 0f, GameUtils.GetAnchor(Alignment, TextureGlobals.Pixels[Color.White].Size()), new Vector2(Scale.X * Value, Scale.Y).ToResolution(), default, 0f);
    }
}