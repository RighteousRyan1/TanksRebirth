using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace TanksRebirth.GameContent.Systems.ParticleSystem;

// Parameters + Build by claude... My brain doesn't think in graphics mathematics.

/// <summary>
/// The smoke trail behind rockets, drawn the way the original game draws it: a darker strip centered on the path, and a
/// lighter fin at right angles to it that sticks out of its middle on one side. Both twist along the trail, so from the
/// tilted camera the dark strip pinches every half turn while the fin flips from one side to the other, which gives the
/// braided look.
/// </summary>
/// <remarks>
/// <para>A shell calls <see cref="Start"/> once, <see cref="Emit"/> every update with the position of its tail, and
/// <see cref="Detach"/> when it's destroyed. The trail then stays where it is and fades out on its own.</para>
/// <para>All trails are drawn in one draw call by <see cref="RenderAll"/>, before the sprite particles, so a rocket's
/// flame always draws over its own smoke.</para>
/// <para>Only depends on MonoGame. Ages are in update frames (the game's <c>DeltaTime</c> units).</para>
/// </remarks>
public sealed class ShellTrail {
    // looks

    /// <summary>Width of the dark strip once fully grown, in world units.</summary>
    public static float Width = 7f;
    /// <summary>How far the light fin sticks out from the middle, in world units (fully grown).</summary>
    public static float FinLength = 5.5f;
    /// <summary>Width right behind the shell, as a fraction of <see cref="Width"/>.</summary>
    public static float HeadWidthScale = 0.45f;
    /// <summary>Frames until a piece of trail reaches <see cref="Width"/>.</summary>
    public static float GrowFrames = 20f;
    /// <summary>How much wider the oldest smoke gets while it fades (1 = no spreading).</summary>
    public static float SpreadScale = 1.3f;
    /// <summary>Frames a piece of trail lives.</summary>
    public static float LifeFrames = 80f;
    /// <summary>Fraction of <see cref="LifeFrames"/> after which a piece of trail starts fading out.</summary>
    public static float FadeStart = 0.55f;
    /// <summary>Opacity of the dark strip.</summary>
    public static float Opacity = 0.5f;
    /// <summary>Opacity of the light fin.</summary>
    public static float FinOpacity = 0.22f;
    /// <summary>Distance behind the shell over which the trail fades in (hides its start under the flame).</summary>
    public static float FadeInDistance = 40f;
    /// <summary>Twist in radians per world unit (π / 68: the dark strip pinches every 68 units, like the original).</summary>
    public static float TwistPerUnit = MathF.PI / 68f;
    /// <summary>How fast the whole trail spins around its length, in radians per frame (0 = frozen in place).</summary>
    public static float SpinSpeed = 0.05f;
    /// <summary>Brightness of the dark strip, relative to the trail color.</summary>
    public static float StripShade = 0.12f;
    /// <summary>Brightness of the light fin, relative to the trail color.</summary>
    public static float FinShade = 0.15f;
    /// <summary>Fraction of each strip's half-width over which its edges fade out (0 = hard edges).</summary>
    public static float EdgeSoftness = 0.55f;
    /// <summary>A new point is added once the shell has moved this far (the newest one follows the shell in between).</summary>
    public static float PointSpacing = 3f;

    // state

    /// <summary>Every trail that is still visible.</summary>
    public static readonly List<ShellTrail> All = [];

    struct TrailPoint {
        public Vector3 Position;
        public float Born;
        public float Distance;
    }

    static float _clock;
    readonly float _started = _clock;

    readonly List<TrailPoint> _points = [];
    readonly float _phase;
    TrailPoint _head;
    bool _hasHead;

    /// <summary>The trail's smoke color (shaded lighter or darker per strip).</summary>
    public Color Color;
    /// <summary>Whether a shell is still adding to this trail.</summary>
    public bool IsAttached { get; private set; } = true;

    ShellTrail(Color color, float phase) {
        Color = color;
        _phase = phase;
    }

    /// <summary>Starts a trail. <paramref name="phase"/> rotates the twist so trails don't all look the same.</summary>
    public static ShellTrail Start(Color color, float phase = 0f) {
        var trail = new ShellTrail(color, phase);
        All.Add(trail);
        return trail;
    }

    /// <summary>Extends the trail to <paramref name="position"/> (the shell's tail). Call every update.</summary>
    public void Emit(Vector3 position) {
        if (!IsAttached)
            return;
        if (_points.Count == 0) {
            _points.Add(new TrailPoint { Position = position, Born = _clock, Distance = 0f });
            return;
        }
        var last = _points[^1];
        var distance = last.Distance + Vector3.Distance(last.Position, position);
        var point = new TrailPoint { Position = position, Born = _clock, Distance = distance };
        if (distance - last.Distance >= PointSpacing) {
            _points.Add(point);
            _hasHead = false;
        }
        else {
            _head = point;
            _hasHead = true;
        }
    }

    /// <summary>Stops the trail from growing; it fades out where it is.</summary>
    public void Detach() => IsAttached = false;

    /// <summary>Removes every trail at once (scene cleanup).</summary>
    public static void Clear() => All.Clear();

    /// <summary>Ages the trails and drops the faded parts. Call once per update.</summary>
    public static void UpdateAll(float deltaTime) {
        _clock += deltaTime;
        for (int i = All.Count - 1; i >= 0; i--) {
            var trail = All[i];
            var points = trail._points;
            // keep one expired point at the end so the oldest visible segment still has its far end
            int expired = 0;
            while (expired + 1 < points.Count && _clock - points[expired + 1].Born >= LifeFrames)
                expired++;
            if (expired > 0)
                points.RemoveRange(0, expired);

            var newest = trail._hasHead ? trail._head : points.Count > 0 ? points[^1] : default;
            if (!trail.IsAttached && (points.Count == 0 || _clock - newest.Born >= LifeFrames))
                All.RemoveAt(i);
        }
    }

    // drawing

    static VertexPositionColorTexture[] _vertices = new VertexPositionColorTexture[4096];
    static short[] _indices = new short[6144];
    static int _vertexCount, _indexCount;
    static BasicEffect? _effect;
    static Texture2D? _profile;

    /// <summary>
    /// Draws every trail. Call during the world pass, before sprite particles. <paramref name="brightness"/> scales the
    /// smoke color with the scene light (1 = daylight).
    /// </summary>
    public static void RenderAll(GraphicsDevice device, Matrix view, Matrix projection, float brightness = 1f) {
        if (All.Count == 0)
            return;
        EnsureResources(device);

        _effect!.View = view;
        _effect.Projection = projection;

        device.BlendState = BlendState.NonPremultiplied;
        device.DepthStencilState = DepthStencilState.DepthRead;
        device.RasterizerState = RasterizerState.CullNone;
        device.SamplerStates[0] = SamplerState.LinearClamp;

        _vertexCount = 0;
        _indexCount = 0;
        foreach (var trail in All)
            trail.Build(device, brightness);
        Flush(device);
    }

    void Build(GraphicsDevice device, float brightness) {
        int count = _points.Count + (_hasHead ? 1 : 0);
        if (count < 2)
            return;

        // two strips of 'count' vertex pairs each
        if (_vertexCount + count * 4 > short.MaxValue)
            Flush(device);
        EnsureCapacity(count * 4, (count - 1) * 12);

        var headDistance = _hasHead ? _head.Distance : _points[^1].Distance;
        var baseColor = Color.ToVector3() * brightness;

        int firstA = _vertexCount;
        int firstB = _vertexCount + count * 2;
        var lastTangent = Vector3.UnitX;

        for (int i = 0; i < count; i++) {
            var p = PointAt(i);

            // tangent on the ground plane, from the neighbours
            var before = PointAt(Math.Max(i - 1, 0)).Position;
            var after = PointAt(Math.Min(i + 1, count - 1)).Position;
            var tangent = after - before;
            tangent.Y = 0f;
            if (tangent.LengthSquared() > 1e-6f)
                lastTangent = Vector3.Normalize(tangent);
            tangent = lastTangent;
            var side = new Vector3(tangent.Z, 0f, -tangent.X);

            var age = _clock - p.Born;
            var grow = MathHelper.Clamp(age / GrowFrames, 0f, 1f);
            grow = grow * grow * (3f - 2f * grow);
            var halfWidth = 0.5f * Width * MathHelper.Lerp(HeadWidthScale, 1f, grow) *
                            MathHelper.Lerp(1f, SpreadScale, MathHelper.Clamp(age / LifeFrames, 0f, 1f));

            var fadeOut = 1f - MathHelper.Clamp((age - FadeStart * LifeFrames) / ((1f - FadeStart) * LifeFrames), 0f, 1f);
            var fadeIn = MathHelper.Clamp((headDistance - p.Distance) / FadeInDistance, 0f, 1f);
            fadeIn = fadeIn * fadeIn * (3f - 2f * fadeIn);
            var fade = fadeOut * fadeIn;
            var size = halfWidth / (0.5f * Width);

            var twist = _phase + p.Distance * TwistPerUnit + (_clock - _started) * SpinSpeed;
            var (sin, cos) = MathF.SinCos(twist);
            var strip = side * cos + Vector3.Up * sin;
            var fin = side * -sin + Vector3.Up * cos;

            // the strip spans the whole profile texture (edge, middle, edge); the fin only its middle-to-edge half
            var center = p.Position;
            WritePair(firstA + i * 2, center - strip * halfWidth, center + strip * halfWidth, 0f, 1f,
                baseColor * StripShade, Opacity * fade, i);
            WritePair(firstB + i * 2, center, center + fin * (FinLength * size), 0.5f, 1f,
                baseColor * FinShade, FinOpacity * fade, i);
        }

        for (int i = 0; i < count - 1; i++) {
            WriteQuad(firstA + i * 2);
            WriteQuad(firstB + i * 2);
        }
        _vertexCount += count * 4;
    }

    TrailPoint PointAt(int i) => i < _points.Count ? _points[i] : _head;

    static void WritePair(int index, Vector3 from, Vector3 to, float vFrom, float vTo, Vector3 rgb, float alpha, int along) {
        var color = new Color(new Vector4(Vector3.Clamp(rgb, Vector3.Zero, Vector3.One), alpha));
        var u = along * 0.25f;
        _vertices[index] = new VertexPositionColorTexture(from, color, new Vector2(u, vFrom));
        _vertices[index + 1] = new VertexPositionColorTexture(to, color, new Vector2(u, vTo));
    }

    static void WriteQuad(int v) {
        _indices[_indexCount++] = (short)v;
        _indices[_indexCount++] = (short)(v + 1);
        _indices[_indexCount++] = (short)(v + 2);
        _indices[_indexCount++] = (short)(v + 1);
        _indices[_indexCount++] = (short)(v + 3);
        _indices[_indexCount++] = (short)(v + 2);
    }

    static void EnsureCapacity(int vertices, int indices) {
        if (_vertices.Length < _vertexCount + vertices)
            Array.Resize(ref _vertices, Math.Max(_vertices.Length * 2, _vertexCount + vertices));
        if (_indices.Length < _indexCount + indices)
            Array.Resize(ref _indices, Math.Max(_indices.Length * 2, _indexCount + indices));
    }

    static void Flush(GraphicsDevice device) {
        if (_indexCount > 0) {
            foreach (var pass in _effect!.CurrentTechnique.Passes) {
                pass.Apply();
                device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, _vertices, 0, _vertexCount, _indices, 0, _indexCount / 3);
            }
        }
        _vertexCount = 0;
        _indexCount = 0;
    }

    static void EnsureResources(GraphicsDevice device) {
        if (_profile is null || _profile.IsDisposed || _profile.GraphicsDevice != device) {
            // flat in the middle, soft towards both edges
            const int size = 32;
            var data = new Color[size];
            for (int i = 0; i < size; i++) {
                var v = (i + 0.5f) / size;
                var a = MathHelper.Clamp((1f - MathF.Abs(2f * v - 1f)) / EdgeSoftness, 0f, 1f);
                a = a * a * (3f - 2f * a);
                data[i] = new Color(new Vector4(1f, 1f, 1f, a));
            }
            _profile = new Texture2D(device, 1, size);
            _profile.SetData(data);
        }
        if (_effect is null || _effect.IsDisposed || _effect.GraphicsDevice != device) {
            _effect = new BasicEffect(device) {
                TextureEnabled = true,
                VertexColorEnabled = true,
                LightingEnabled = false,
                FogEnabled = false,
                World = Matrix.Identity,
            };
        }
        _effect.Texture = _profile;
    }
}
