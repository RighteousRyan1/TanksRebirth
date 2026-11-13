using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using TanksRebirth;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Net;

namespace TanksRebirth.GameContent.UI;

public class XpBar {
    // NOTE: XpSpark is certifiably stupid but, until i rework particles (again)
    // this will allow 2d particles to be entirely separate from 3d particles
    // ...or i could just make any particle with a Z component exist in world space. SO MANY IDEAS!
    struct XpSpark {
        public Vector2 Position;
        public Vector2 Velocity;
        public float Life;
        public float MaxLife;
        public float Size;
        public Color Color;
    }

    const int MAX_SPARKS = 128;
    const float SPARK_GRAVITY = 0.04f;

    float _interp;
    //float _popupInterp;

    public float ApproachValue;

    public ushort Level;

    public float Value;
    public float MaxValue = 1f;

    Vector2 _realScale;
    public Vector2 Scale;

    //Vector2 _animPos;
    //Vector2 _targetPos;
    public Vector2 Position { get; set; }

    public Color EmptyColor;
    public Color FillColor;
    public Color GainedColor;

    public Anchor Alignment = Anchor.Center;

    float _glowTimer;

    readonly List<XpSpark> _sparks = new(MAX_SPARKS);

    bool IsGaining => ApproachValue - Value > MaxValue * 0.001f;
    Vector2 BarTopLeft => Position - Alignment.GetAnchor(_realScale);

    public void GainExperience(float xp) {
        _interp = 0;
        ApproachValue += xp;

        if (ApproachValue > MaxValue) {
            Level++;
        }
    }

    public void Update() {
        _realScale = Scale.ToResolution();
        //if (_popupInterp >= 1) {

        //}
        //else _popupInterp += 0.005f * RuntimeData.DeltaFrames;

        _glowTimer += RuntimeData.DeltaFrames * 0.1f;

        if (_interp < 1) {
            _interp += 0.005f * RuntimeData.DeltaFrames;

            if (Value >= MaxValue) {
                // bring back to the beginning of the bar
                Value -= MaxValue;
                ApproachValue -= MaxValue;
            }
        }
        else _interp = 1;

        Value += (ApproachValue - Value) * Easings.ComputeEase(EasingFunction.InOutQuint, _interp);

        if (IsGaining)
            SpawnSparks();

        UpdateSparks();
    }

    private void SpawnSparks() {
        float ratio = MathF.Min(1f, Value / MaxValue);
        var topLeft = BarTopLeft;
        float res = 1f.ToResolutionX();

        // roughly 1-2 sparks per frame, scaled by frame time
        int count = (int)MathF.Ceiling(RuntimeData.DeltaFrames * Client.ClientRandom.NextFloat(1f, 2f));

        for (int i = 0; i < count && _sparks.Count < MAX_SPARKS; i++) {
            var life = Client.ClientRandom.NextFloat(25f, 50f);

            _sparks.Add(new XpSpark {
                Position = new Vector2(
                    topLeft.X + _realScale.X * ratio,
                    topLeft.Y + _realScale.Y * Client.ClientRandom.NextFloat(0f, 1f)),
                Velocity = new Vector2(Client.ClientRandom.NextFloat(-0.3f, 0.8f), Client.ClientRandom.NextFloat(-1.2f, -0.3f)) * res,
                Life = life,
                MaxLife = life,
                Size = Client.ClientRandom.NextFloat(1.5f, 3.5f) * res,
                Color = Color.Lerp(GainedColor, FillColor, Client.ClientRandom.NextFloat(0f, 1f))
            });
        }
    }

    void UpdateSparks() {
        float res = 1f.ToResolutionX();

        for (int i = _sparks.Count - 1; i >= 0; i--) {
            var s = _sparks[i];

            s.Life -= RuntimeData.DeltaFrames;
            if (s.Life <= 0) {
                _sparks.RemoveAt(i);
                continue;
            }

            s.Position += s.Velocity * RuntimeData.DeltaFrames;
            s.Velocity.Y += SPARK_GRAVITY * res * RuntimeData.DeltaFrames;

            _sparks[i] = s;
        }
    }

    // todo, when xp gained, draw a text particle that shows how much was gained
    // also when leveling up make it go to end and then go to supposed xp value
    public void Draw(SpriteBatch sb) {
        // _realScale is already resolution-scaled, so nothing below scales it again
        _realScale = Scale.ToResolution();
        var pixel = TextureGlobals.Pixels[Color.White];

        var text = $"Level: {Level} | {MathF.Floor(Value / MaxValue * 100)}%";
        // draw empty xp (text)
        DrawUtils.DrawStringWithBorder(sb, FontGlobals.RebirthFont, text, Position - (Vector2.UnitY * 20).ToResolution(),
            Color.White, Color.Black, new Vector2(0.6f).ToResolution(),
            0f, Alignment, borderThickness: 0.5f);

        var topLeft = BarTopLeft;
        float borderSize = 1.25f.ToResolutionX();

        // outline
        sb.Draw(pixel, topLeft - new Vector2(borderSize), null, Color.Black * 0.85f, 0f, Vector2.Zero, _realScale + new Vector2(borderSize * 2), default, 0f);

        // empty xp
        sb.Draw(pixel, topLeft, null, EmptyColor, 0f, Vector2.Zero, _realScale, default, 0f);

        // width calc
        float approachRatio = MathF.Min(MaxValue, ApproachValue) / MaxValue;
        float currentRatio = MathF.Min(1f, Value / MaxValue);

        // pulse
        float pulseAlpha = 0.6f + MathF.Sin(_glowTimer) * 0.4f;

        // approaching xp value
        sb.Draw(pixel, topLeft, null, GainedColor * pulseAlpha, 0f, Vector2.Zero, new Vector2(_realScale.X * approachRatio, _realScale.Y), default, 0f);

        // gained xp
        var fillSize = new Vector2(_realScale.X * currentRatio, _realScale.Y);
        sb.Draw(pixel, topLeft, null, FillColor, 0f, Vector2.Zero, fillSize, default, 0f);

        // a slight glossy look. good enough i think
        sb.Draw(pixel, topLeft, null, Color.White * 0.18f, 0f, Vector2.Zero, new Vector2(fillSize.X, fillSize.Y * 0.4f), default, 0f);

        // bright leading edge while the bar is growing
        if (IsGaining && fillSize.X > 0f) {
            float edgeWidth = 2f.ToResolutionX();
            sb.Draw(pixel, topLeft + new Vector2(MathF.Max(0f, fillSize.X - edgeWidth), 0f), null, Color.White * (0.5f + pulseAlpha * 0.4f), 0f, Vector2.Zero, new Vector2(MathF.Min(edgeWidth, fillSize.X), fillSize.Y), default, 0f);
        }

        // sparks
        foreach (var s in _sparks) {
            float t = s.Life / s.MaxLife;
            sb.Draw(pixel, s.Position, null, s.Color * t, 0f, new Vector2(0.5f), s.Size * t, default, 0f);
        }
    }
}