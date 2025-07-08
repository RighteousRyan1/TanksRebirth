using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth.Graphics;

public class Trail {
    private int _maxTrailPoints = 20;
    public int MaxTrailPoints {
        get => _maxTrailPoints;
        set {
            _maxTrailPoints = value;
            _vertices = new VertexPositionColor[_maxTrailPoints * 4];
            _indices = new short[(_maxTrailPoints - 1) * 12];
        }
    }
    public float StartWidth = 5f;
    public float BorderOffset = 3f;

    private readonly GraphicsDevice _graphicsDevice;

    // probably shouldn't be instanced per-trail. fix later.
    private readonly BasicEffect _effect;
    private readonly Queue<Vector2> _positions = [];
    private VertexPositionColor[] _vertices;
    private short[] _indices;

    public const float WIDTH_TRAILS_ENJOY = 800;
    public const float HEIGHT_TRAILS_ENJOY = 480;

    private Color _mainColor;
    public Color MainColor {
        get => _mainColor;
        set {
            _borderColor = Color.Lerp(_mainColor, Color.Black, 0.3f);
            _mainColor = value;
        }
    }

    // maybe public later. not for now tho xd.
    private Color _borderColor;

    public Trail(GraphicsDevice device, Color mainColor) {
        _graphicsDevice = device;
        MainColor = mainColor;
        MaxTrailPoints = 20;

        _effect = new BasicEffect(device) {
            VertexColorEnabled = true,
            Projection = Matrix.CreateOrthographicOffCenter(0, device.Viewport.Width, device.Viewport.Height, 0, 0, 1)
        };

        InitializeIndices();
    }

    private void InitializeIndices() {
        for (int i = 0; i < MaxTrailPoints - 1; i++) {
            // border indices
            int baseIndex = i * 12;
            _indices[baseIndex] = (short)(i * 4);
            _indices[baseIndex + 1] = (short)(i * 4 + 1);
            _indices[baseIndex + 2] = (short)(i * 4 + 4);
            _indices[baseIndex + 3] = (short)(i * 4 + 1);
            _indices[baseIndex + 4] = (short)(i * 4 + 5);
            _indices[baseIndex + 5] = (short)(i * 4 + 4);

            // main trail indices
            _indices[baseIndex + 6] = (short)(i * 4 + 2);
            _indices[baseIndex + 7] = (short)(i * 4 + 3);
            _indices[baseIndex + 8] = (short)(i * 4 + 6);
            _indices[baseIndex + 9] = (short)(i * 4 + 3);
            _indices[baseIndex + 10] = (short)(i * 4 + 7);
            _indices[baseIndex + 11] = (short)(i * 4 + 6);
        }
    }

    public void Update(Vector2 newPosition) {
        _positions.Enqueue(newPosition);
        while (_positions.Count > MaxTrailPoints) _positions.Dequeue();
        UpdateVertices();
    }

    private void UpdateVertices() {
        Vector2[] points = [.. _positions];
        int pointCount = points.Length;

        for (int i = 0; i < pointCount; i++) {
            float age = (float)i / pointCount;

            Vector2 current = points[i];
            Vector2 direction = MathUtils.GetSmoothedDirection(points, i);
            Vector2 normal = new(-direction.Y, direction.X);

            // make trail fade away slowly (in size)
            float currentWidth = StartWidth * age;
            float borderWidth = currentWidth + BorderOffset / currentWidth;

            // fade out over time (in alpha)
            Color mainColor = MainColor * age;
            Color borderColor = _borderColor * age;

            // border vertices
            _vertices[i * 4] = new(
                new Vector3(current + normal * borderWidth, 0),
                borderColor);

            _vertices[i * 4 + 1] = new(
                new Vector3(current + normal * borderWidth, 0),
                borderColor);

            // main trail vertices
            _vertices[i * 4 + 2] = new(
                new Vector3(current + normal * currentWidth, 0),
                mainColor);

            _vertices[i * 4 + 3] = new(
                new Vector3(current - normal * currentWidth, 0),
                mainColor);
        }
    }

    public void Draw() {
        if (_positions.Count < 2) return;

        _effect.CurrentTechnique.Passes[0].Apply();
        _graphicsDevice.DrawUserIndexedPrimitives(
            PrimitiveType.TriangleList,
            _vertices,
            0,
            _positions.Count * 4,
            _indices,
            0,
            (_positions.Count - 1) * 4);
    }
}