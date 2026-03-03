using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using TanksRebirth;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth.GameContent.UI; 
public class XpBar {
    // TODO: make fancy and cool
    float _interp;
    //float _popupInterp;

    public float ApproachValue;

    public ushort Level;

    public float Value;
    public float MaxValue = 1f;

    public Vector2 Scale;

    //Vector2 _animPos;
    //Vector2 _targetPos;
    public Vector2 Position { get; set; }

    public Color EmptyColor;
    public Color FillColor;
    public Color GainedColor;

    public Anchor Alignment;

    // Added a timer to drive the glowing pulse animation
    private float _glowTimer;

    public void GainExperience(float xp) {
        _interp = 0;
        ApproachValue += xp;

        if (ApproachValue > MaxValue) {
            Level++;
        }
    }

    public void Update() {
        //if (_popupInterp >= 1) {

        //}
        //else _popupInterp += 0.005f * RuntimeData.DeltaTime;

        _glowTimer += RuntimeData.DeltaTime * 0.1f;

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
            if (Value >= MaxValue) {
                // bring back to the beginning of the bar
                Value -= MaxValue;
                ApproachValue -= MaxValue;
            }
        }
        else _interp = 1;

        Value += (ApproachValue - Value) * Easings.ComputeEase(EasingFunction.InOutQuint, _interp);
    }

    // todo, when xp gained, draw a text particle that shows how much was gained
    // also when leveling up make it go to end and then go to supposed xp value
    public void Render(SpriteBatch sb) {
        var text = $"Level: {Level} | {MathF.Floor(Value / MaxValue * 100)}%";
        Alignment = Anchor.Center;
        // draw empty xp (Text)
        DrawUtils.DrawStringWithBorder(sb, FontGlobals.RebirthFont, text, Position - (Vector2.UnitY * 20).ToResolution(), Color.White, Color.Black, new Vector2(0.6f).ToResolution(),
            0f, Alignment, borderThickness: 0.5f);

        Vector2 origin = Alignment.GetAnchor(TextureGlobals.Pixels[Color.White].Size());
        float borderSize = 2f.ToResolutionX();

        // 1. Draw the Outline/Backdrop
        sb.Draw(TextureGlobals.Pixels[Color.White], Position, null, Color.Black * 0.85f, 0f, origin, (Scale + new Vector2(borderSize * 2)).ToResolution(), default, 0f);

        // draw empty xp (Track)
        sb.Draw(TextureGlobals.Pixels[Color.White], Position, null, EmptyColor, 0f, origin, Scale.ToResolution(), default, 0f);

        // 2. Add an inner shadow to the empty track for depth
        sb.Draw(TextureGlobals.Pixels[Color.White], Position, null, Color.Black * 0.3f, 0f, origin, new Vector2(Scale.X, Scale.Y * 0.25f).ToResolution(), default, 0f);

        // Calculate widths dynamically based on MaxValue
        float approachRatio = MathF.Min(MaxValue, ApproachValue) / MaxValue;
        float currentRatio = Value / MaxValue;

        // 3. Create a smooth sine-wave pulse for the approach bar
        float pulseAlpha = 0.6f + MathF.Sin(_glowTimer) * 0.4f;

        // draw approaching xp value (Glow Trail)
        sb.Draw(TextureGlobals.Pixels[Color.White], Position, null, GainedColor * pulseAlpha, 0f, origin, new Vector2(Scale.X * approachRatio, Scale.Y).ToResolution(), default, 0f);

        // draw gained xp (Main Fill)
        sb.Draw(TextureGlobals.Pixels[Color.White], Position, null, FillColor, 0f, origin, new Vector2(Scale.X * currentRatio, Scale.Y).ToResolution(), default, 0f);

        // 4. Draw a glossy highlight across the top edge of the filled bar
        sb.Draw(TextureGlobals.Pixels[Color.White], Position, null, Color.White * 0.25f, 0f, origin, new Vector2(Scale.X * currentRatio, Scale.Y * 0.35f).ToResolution(), default, 0f);
    }
}