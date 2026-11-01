using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth.Graphics;

public class Trail {
    const int VERTS_PER_SAMPLE = 5;
    const int BANDS = VERTS_PER_SAMPLE - 1;

    int _maxTrailPoints;
    /// <summary>How many points the trail can hold.</summary>
    public int MaxTrailPoints {
        get => _maxTrailPoints;
        set {
            _maxTrailPoints = Math.Max(2, value);
            _points = new Vector2[_maxTrailPoints];
            _times = new float[_maxTrailPoints];
            _start = 0;
            _count = 0;

            var maxSamples = 1 + (_maxTrailPoints - 1) * MAX_SUBDIVISIONS;
            _samples = new Vector2[maxSamples];
            _sampleLife = new float[maxSamples];
            _vertices = new VertexPositionColor[maxSamples * VERTS_PER_SAMPLE];
            _indices = new short[(maxSamples - 1) * BANDS * 6];
            InitializeIndices();
        }
    }
    /// <summary>Width of the trail at its head in pixels.</summary>
    public float StartWidth = 5f;
    /// <summary>Width of the faded edge on each side in pixels.</summary>
    public float EdgeSoftness = 1.5f;
    /// <summary>How long a point stays on the trail in seconds.</summary>
    public float Lifetime = 0.3f;
    /// <summary>The followed point has to move this many pixels before a new point is added.</summary>
    public float PointSpacing = 3f;
    /// <summary>How much lighter the middle of the trail is than its edges, from 0 to 1.</summary>
    public float CoreHighlight = 0.55f;

    const int MAX_SUBDIVISIONS = 8;
    // point split dist
    const float SAMPLE_LENGTH = 6f;

    readonly GraphicsDevice _graphicsDevice;

    // probably shouldn't be instanced per-trail. fix later.
    readonly BasicEffect _effect;

    // ring buffer of points, oldest first
    Vector2[] _points = [];
    float[] _times = [];
    int _start, _count;

    // the points smoothed into a curve
    Vector2[] _samples = [];
    float[] _sampleLife = [];
    int _sampleCount;

    VertexPositionColor[] _vertices = [];
    short[] _indices = [];

    public Color MainColor;

    public Trail(GraphicsDevice device, Color mainColor) {
        _graphicsDevice = device;
        MainColor = mainColor;
        MaxTrailPoints = 128;

        _effect = new BasicEffect(device) {
            VertexColorEnabled = true,
            Projection = Matrix.CreateOrthographicOffCenter(0, device.Viewport.Width, device.Viewport.Height, 0, 0, 1)
        };
    }

    void InitializeIndices() {
        // each sample has 5 vertices across the trail (edge, body, center, body, edge), so there are 4 bands between them
        for (int s = 0; s < _samples.Length - 1; s++) {
            for (int b = 0; b < BANDS; b++) {
                int i = (s * BANDS + b) * 6;
                short a = (short)(s * VERTS_PER_SAMPLE + b);
                short c = (short)((s + 1) * VERTS_PER_SAMPLE + b);
                _indices[i] = a;
                _indices[i + 1] = (short)(a + 1);
                _indices[i + 2] = c;
                _indices[i + 3] = (short)(a + 1);
                _indices[i + 4] = (short)(c + 1);
                _indices[i + 5] = c;
            }
        }
    }

    Vector2 PointAt(int i) => _points[(_start + i) % _maxTrailPoints];
    float TimeAt(int i) => _times[(_start + i) % _maxTrailPoints];

    /// <summary>Removes every point from the trail.</summary>
    public void Clear() {
        _start = 0;
        _count = 0;
        _sampleCount = 0;
    }

    public void Update(Vector2 newPosition) => Update(newPosition, (float)TankGame.LastGameTime.TotalGameTime.TotalSeconds);

    public void Update(Vector2 newPosition, float time) {
        _effect.Projection = Matrix.CreateOrthographicOffCenter(0, WindowUtils.WindowWidth, WindowUtils.WindowHeight, 0, 0, 1);

        // expired points (their width has already shrunk to nothing, so they disappear without a pop)
        while (_count > 0 && time - _times[_start] > Lifetime) {
            _start = (_start + 1) % _maxTrailPoints;
            _count--;
        }

        if (_count > 1 && Vector2.DistanceSquared(PointAt(_count - 2), newPosition) < PointSpacing * PointSpacing) {
            // too close to add a new point, so the head just follows
            int head = (_start + _count - 1) % _maxTrailPoints;
            _points[head] = newPosition;
            _times[head] = time;
        }
        else {
            if (_count == _maxTrailPoints) {
                _start = (_start + 1) % _maxTrailPoints;
                _count--;
            }
            int index = (_start + _count) % _maxTrailPoints;
            _points[index] = newPosition;
            _times[index] = time;
            _count++;
        }

        BuildSamples(time);
        UpdateVertices();
    }

    void BuildSamples(float time) {
        _sampleCount = 0;
        if (_count < 2)
            return;

        AddSample(PointAt(0), TimeAt(0), time);
        for (int i = 0; i < _count - 1; i++) {
            var p0 = PointAt(Math.Max(i - 1, 0));
            var p1 = PointAt(i);
            var p2 = PointAt(i + 1);
            var p3 = PointAt(Math.Min(i + 2, _count - 1));
            float t1 = TimeAt(i), t2 = TimeAt(i + 1);

            // low fps makes more points
            int pieces = Math.Clamp((int)MathF.Ceiling(Vector2.Distance(p1, p2) / SAMPLE_LENGTH), 1, MAX_SUBDIVISIONS);
            for (int k = 1; k <= pieces; k++) {
                float t = (float)k / pieces;
                AddSample(Vector2.CatmullRom(p0, p1, p2, p3, t), MathHelper.Lerp(t1, t2, t), time);
            }
        }
    }

    void AddSample(Vector2 position, float pointTime, float time) {
        _samples[_sampleCount] = position;
        _sampleLife[_sampleCount] = MathHelper.Clamp(1f - (time - pointTime) / Lifetime, 0f, 1f);
        _sampleCount++;
    }

    void UpdateVertices() {
        var core = Color.Lerp(MainColor, Color.White, CoreHighlight);
        var normal = Vector2.UnitY;

        for (int i = 0; i < _sampleCount; i++) {
            var current = _samples[i];
            var direction = _samples[Math.Min(i + 1, _sampleCount - 1)] - _samples[Math.Max(i - 1, 0)];

            // keep the last normal where the trail doubles back on itself
            if (direction.LengthSquared() > 1e-4f) {
                direction.Normalize();
                normal = new Vector2(-direction.Y, direction.X);
            }

            // the trail thins out and fades away toward its tail
            float life = _sampleLife[i];
            float half = StartWidth * 0.5f * life;
            float edge = half + EdgeSoftness;

            var body = MainColor * life;
            var center = core * life;

            int v = i * VERTS_PER_SAMPLE;
            _vertices[v] = new(new Vector3(current + normal * edge, 0), Color.Transparent);
            _vertices[v + 1] = new(new Vector3(current + normal * half, 0), body);
            _vertices[v + 2] = new(new Vector3(current, 0), center);
            _vertices[v + 3] = new(new Vector3(current - normal * half, 0), body);
            _vertices[v + 4] = new(new Vector3(current - normal * edge, 0), Color.Transparent);
        }
    }

    public void Draw() {
        if (_sampleCount < 2) return;

        _graphicsDevice.BlendState = BlendState.AlphaBlend;
        _graphicsDevice.RasterizerState = RasterizerState.CullNone;
        _graphicsDevice.DepthStencilState = DepthStencilState.None;

        _effect.CurrentTechnique.Passes[0].Apply();
        _graphicsDevice.DrawUserIndexedPrimitives(
            PrimitiveType.TriangleList,
            _vertices,
            0,
            _sampleCount * VERTS_PER_SAMPLE,
            _indices,
            0,
            (_sampleCount - 1) * BANDS * 2);
    }
}
