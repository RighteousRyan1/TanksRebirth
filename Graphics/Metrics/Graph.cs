using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.Internals.Common.Framework;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth.Graphics.Metrics;

public class Graph(string name, Func<float> value, float rangeMax, int length = 100, float deltaX = 1f, float deltaY = 1f, uint updateCount = 30) {
    int _numElements;
    float[] _values = new float[length];
    readonly Func<float> _independent = value;

    public string Name { get; set; } = name;
    /// <summary>Measured in frames, not ticks, so the graph will update faster on higher framerates.</summary>
    public uint UpdateFrequency { get; set; } = updateCount;
    /// <summary>The range of values within the graph, vertically, aka: the Y value range.</summary>
    public Range<float> VerticalRange { get; set; } = new(0, rangeMax);
    public float CurrentValue { get; private set; }

    int _length = length;
    /// <summary>The length of the graph, aka the X value range. X will always be time.</summary>
    public int Length {
        get => _length;
        set {
            // resize only if necessary
            if (value != _length) {
                _length = value;
                Array.Resize(ref _values, _length);
            }
        }
    }
    public float HeightBetweenPoints { get; set; } = deltaY;
    /// <summary>The visual X separation between points on the graph.</summary>
    public float LengthBetweenPoints { get; set; } = deltaX;

    public bool AdjustAutomatically = true;
    public int AdjustFactor = 50;

    public float MaxSince(int iterations) {
        var values = _values[(_numElements - iterations).._numElements];
        return values.Max();
    }
    public float MinSince(int iterations) {
        var values = _values[(_numElements - iterations).._numElements];
        return values.Min();
    }

    public void AdjustToFit(int roundingFactorMax) {
        int newHundred = 0;
        while (newHundred < _values.Max())
            newHundred += roundingFactorMax;
        VerticalRange = new(VerticalRange.Min, newHundred);
    }

    public void Update() {
        if (RuntimeData.UpdateCount % UpdateFrequency != 0)
            return;

        var valueReal = _independent.Invoke();
        CurrentValue = float.IsFinite(valueReal) ? valueReal : 0;
        //if (CurrentValue > VerticalRange.Max) CurrentValue = 0;

        if (_numElements < Length) {
            _values[_numElements] = CurrentValue;
            _numElements++;
        } else {
            _values = ArrayUtils.Shift(_values, -1, 0);
            _values[_numElements - 1] = CurrentValue;
        }
        HeightBetweenPoints = 0.35f;
        LengthBetweenPoints = 3f;
        
        if (AdjustAutomatically)
            AdjustToFit(AdjustFactor);
    }

    public void Draw(SpriteBatch sb, Vector2 position, float scale = 1f) {
        // define what we need
        var graphVisualWidth = LengthBetweenPoints * Length;
        var graphVisualHeight = HeightBetweenPoints * VerticalRange.Difference;

        // draw background
        sb.Draw(TextureGlobals.Pixels[Color.White], position - new Vector2(0, graphVisualHeight) * scale, null, Color.DarkGray * 0.25f, 0f, Vector2.Zero,
            new Vector2(graphVisualWidth, graphVisualHeight) * scale, default, 0);

        // draw graph name
        DrawUtils.DrawStringWithBorder(sb, FontGlobals.RebirthFontLarge, Name,
            position - new Vector2(0, graphVisualHeight) * scale - new Vector2(0, 20),
            Color.White, Color.Black, new Vector2(scale) * 0.08f, 0f, Anchor.LeftCenter, 0.65f, charSpacing: 8);

        // draw X plane
        sb.Draw(TextureGlobals.Pixels[Color.White], position, null, Color.White, 0f, Vector2.Zero, 
            new Vector2(graphVisualWidth, 1f) * scale, default, 0);

        // draw Y plane
        sb.Draw(TextureGlobals.Pixels[Color.White], position - new Vector2(0, graphVisualHeight) * scale, null, Color.White, 0f, Vector2.Zero, 
            new Vector2(1f, graphVisualHeight) * scale, default, 0);

        // draw text
        int splits = 5;

        for (int i = 0; i <= splits; i++) {
            var vValue = VerticalRange.Max / splits * i;
            DrawUtils.DrawStringWithBorder(sb, FontGlobals.RebirthFontLarge, vValue.ToString(), position - new Vector2(20, vValue * scale * HeightBetweenPoints),
                Color.White, Color.Black, new Vector2(scale) * 0.05f, 0f, Anchor.RightCenter, 0.65f);
        }

        // draw current value
        DrawUtils.DrawStringWithBorder(sb, FontGlobals.RebirthFontLarge, CurrentValue.ToString(),
            position - new Vector2(-LengthBetweenPoints * scale * _numElements, CurrentValue * scale * HeightBetweenPoints),
            Color.White, Color.Black, new Vector2(scale) * 0.05f, 0f, Anchor.LeftCenter, 0.65f, charSpacing: 8);

        // draw min, max, mean, range

        var graphWidthReal = Length * LengthBetweenPoints;
        var splitLen = graphWidthReal / 4;

        var min = _values.Min();
        var max = _values.Max();

        var average = MathF.Round(_values.Average());

        DrawUtils.DrawStringWithBorder(sb, FontGlobals.RebirthFontLarge, $"Min: {min}", position + new Vector2(0, 20),
            Color.White, Color.Black, new Vector2(scale) * 0.07f, 0f, Anchor.LeftCenter, 0.65f, charSpacing: 8);

        DrawUtils.DrawStringWithBorder(sb, FontGlobals.RebirthFontLarge, $"Max: {max}", position + new Vector2(splitLen * scale, 20),
            Color.White, Color.Black, new Vector2(scale) * 0.07f, 0f, Anchor.LeftCenter, 0.65f, charSpacing: 8);

        DrawUtils.DrawStringWithBorder(sb, FontGlobals.RebirthFontLarge, $"Avg: {average}", position + new Vector2(splitLen * 2 * scale, 20),
            Color.White, Color.Black, new Vector2(scale) * 0.07f, 0f, Anchor.LeftCenter, 0.65f, charSpacing: 8);

        DrawUtils.DrawStringWithBorder(sb, FontGlobals.RebirthFontLarge, $"Range: {max - min}", position + new Vector2(splitLen * 3 * scale, 20),
            Color.White, Color.Black, new Vector2(scale) * 0.07f, 0f, Anchor.LeftCenter, 0.65f, charSpacing: 8);

        // draw lines to connect points
        // only draw if there's more than one point
        if (_numElements < 1) return;
        for (int i = 1; i < _numElements; i++) {
            // invert since negative Y is visually up
            var vPrev = -_values[i - 1] * HeightBetweenPoints;
            var vNext = -_values[i] * HeightBetweenPoints;

            // add scale since we don't want to draw over the Y axis
            var posPrev = new Vector2(LengthBetweenPoints * (i - 1) + scale, vPrev) * scale;
            var posNext = new Vector2(LengthBetweenPoints * i + scale, vNext) * scale;

            var angleVector = posPrev.DirectionTo(posNext);
            var distance = Vector2.Distance(posPrev, posNext) * (1f / scale);
            var angleRotation = angleVector.ToRotation();

            // NOTE THAT:
            // 0 rad = point right
            // pi/2 rad = point down
            // pi rad = point left
            // pi/2*3 rad = point up

            var size = TextureGlobals.Pixels[Color.White].Size();
            sb.Draw(TextureGlobals.Pixels[Color.White], position + posPrev, null, Color.Red, angleRotation, 
                new Vector2(0, size.Y / 2),
                new Vector2(distance, 1f) * scale, default, 0f);
            /*sb.Draw(TextureGlobals.Pixels[Color.White], position + posPrev, null, Color.Red, 0f,
                new Vector2(0, size.Y / 2),
                Vector2.One * scale, default, 0f);*/
        }
    }
    public void Draw(SpriteBatch sb, float x, float y, float scale = 1f) => Draw(sb, new Vector2(x, y), scale);
}
