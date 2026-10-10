using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.RebirthUtils;
using TanksRebirth.Graphics;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth.GameContent.Systems.ParticleSystem;

#pragma warning disable CS8618

// Code partially assisted by Claude. It's cool to study what it writes for the future!

/// <summary>Vertex used by every batched sprite: world position, pre-lit tint (rgb) + alpha, and UV.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct ParticleVertex : IVertexType {
    public Vector3 Position;
    public Vector4 Tint;
    public Vector2 UV;

    public static readonly VertexDeclaration Declaration = new(
        new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
        new VertexElement(12, VertexElementFormat.Vector4, VertexElementUsage.Color, 0),
        new VertexElement(28, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0));

    readonly VertexDeclaration IVertexType.VertexDeclaration => Declaration;
}

/// <summary>One shared index buffer for every quad batch (two triangles per quad, 16384 quads max per draw).</summary>
internal static class QuadIndexBuffer {
    public const int MaxQuads = 16384; // 16384 * 4 = 65536 vertices, the most a 16-bit index can address.

    static IndexBuffer _buffer;

    public static IndexBuffer Get(GraphicsDevice device) {
        if (_buffer is { IsDisposed: false } && _buffer.GraphicsDevice == device)
            return _buffer;

        var indices = new ushort[MaxQuads * 6];
        for (int q = 0; q < MaxQuads; q++) {
            int v = q * 4;
            int i = q * 6;
            // same winding SpriteBatch uses: (TL, TR, BL), (BR, BL, TR)
            indices[i + 0] = (ushort)(v + 0);
            indices[i + 1] = (ushort)(v + 1);
            indices[i + 2] = (ushort)(v + 2);
            indices[i + 3] = (ushort)(v + 3);
            indices[i + 4] = (ushort)(v + 2);
            indices[i + 5] = (ushort)(v + 1);
        }
        _buffer = new IndexBuffer(device, IndexElementSize.SixteenBits, indices.Length, BufferUsage.WriteOnly);
        _buffer.SetData(indices);
        return _buffer;
    }
}

/// <summary>Writes a quad using exactly the same math as <see cref="SpriteBatch"/> followed by a world matrix, so batched
/// particles land where the old one-draw-call-per-particle path put them.</summary>
internal static class SpriteQuad {
    public static void Write(ParticleVertex[] verts, int offset, Texture2D texture, Rectangle? crop, Vector2 origin,
                             float scale, float rotation2D, float layer, in Matrix world, Vector4 tint) {
        float texW = texture.Width;
        float texH = texture.Height;

        float srcX = 0, srcY = 0, srcW = texW, srcH = texH;
        if (crop.HasValue) {
            var c = crop.Value;
            srcX = c.X; srcY = c.Y; srcW = c.Width; srcH = c.Height;
        }

        float u0 = srcX / texW, v0 = srcY / texH;
        float u1 = (srcX + srcW) / texW, v1 = (srcY + srcH) / texH;

        float dx = -origin.X * scale;
        float dy = -origin.Y * scale;
        float w = srcW * scale;
        float h = srcH * scale;

        float sin = 0f, cos = 1f;
        if (rotation2D != 0f) {
            sin = MathF.Sin(rotation2D);
            cos = MathF.Cos(rotation2D);
        }

        Put(ref verts[offset + 0], dx, dy, sin, cos, layer, in world, tint, u0, v0);          // TL
        Put(ref verts[offset + 1], dx + w, dy, sin, cos, layer, in world, tint, u1, v0);      // TR
        Put(ref verts[offset + 2], dx, dy + h, sin, cos, layer, in world, tint, u0, v1);      // BL
        Put(ref verts[offset + 3], dx + w, dy + h, sin, cos, layer, in world, tint, u1, v1);  // BR
    }

    static void Put(ref ParticleVertex v, float x, float y, float sin, float cos, float z, in Matrix world, Vector4 tint, float u, float t) {
        var local = new Vector3(x * cos - y * sin, x * sin + y * cos, z);
        v.Position = Vector3.Transform(local, world);
        v.Tint = tint;
        v.UV = new Vector2(u, t);
    }
}

/// <summary>Batches like textures and combines them into one draw call. Modders won't need this because it's used via the particle systems by default.</summary>
internal sealed class SpriteParticleBatcher {
    sealed class Bucket {
        public Texture2D Texture;
        public bool Additive;
        public Particle[] Items = new Particle[64];
        public int Count;
        /// <summary>Highest <see cref="Particle.Layer"/> in the bucket: buckets draw in this order, so higher layers end up on top.</summary>
        public float Layer;

        public void Add(Particle p) {
            if (Count == Items.Length) Array.Resize(ref Items, Items.Length * 2);
            Layer = Count == 0 ? p.Layer : MathF.Max(Layer, p.Layer);
            Items[Count++] = p;
        }
    }

    readonly Dictionary<(Texture2D, bool), Bucket> _map = [];
    readonly List<Bucket> _active = [];

    ParticleVertex[] _verts = new ParticleVertex[4 * 1024];
    DynamicVertexBuffer _vb;
    BasicEffect _fx;

    public bool HasSprites => _active.Count > 0;

    public void Begin() {
        for (int i = 0; i < _active.Count; i++) {
            Array.Clear(_active[i].Items, 0, _active[i].Count);
            _active[i].Count = 0;
        }
        _active.Clear();
    }

    public void Add(Particle p) {
        var key = (p.Texture, p.HasAdditiveBlending);
        if (!_map.TryGetValue(key, out var bucket))
            _map[key] = bucket = new Bucket { Texture = p.Texture, Additive = p.HasAdditiveBlending };
        if (bucket.Count == 0)
            _active.Add(bucket);
        bucket.Add(p);
    }

    public void Draw(GraphicsDevice device, Matrix view, Matrix projection) {
        // draws lower layers first
        for (int i = 1; i < _active.Count; i++) {
            var bucket = _active[i];
            int j = i - 1;
            while (j >= 0 && _active[j].Layer > bucket.Layer) {
                _active[j + 1] = _active[j];
                j--;
            }
            _active[j + 1] = bucket;
        }

        int totalQuads = 0;
        for (int i = 0; i < _active.Count; i++) totalQuads += _active[i].Count;
        if (totalQuads == 0) return;

        EnsureCapacity(device, totalQuads);

        float brightness = SceneManager.GameLight.Brightness;

        Vector3 ambient = StaticLighting.AmbientDiffuseProduct;

        var cam = CameraGlobals.RebirthFreecam;
        Vector3 camPos = cam.Position;
        Vector3 camDown = cam.World.Down;
        Vector3 camForward = cam.World.Forward;

        int quad = 0;
        for (int b = 0; b < _active.Count; b++) {
            var bucket = _active[b];
            for (int i = 0; i < bucket.Count; i++) {
                var p = bucket.Items[i];

                // anyhting that hasn't been inited yet will just kinda exist in nothingness for a frame, still usable tho
                if (p.Position.X > 1e8f || p.Position.X < -1e8f) {
                    WriteDegenerate(quad++);
                    continue;
                }

                Matrix world;
                if (p.FaceTowardsMe) {
                    world = Matrix.CreateScale(p.Scale) *
                            Matrix.CreateBillboard(p.Position, camPos, camDown, camForward);
                }
                else {
                    world = Matrix.CreateScale(p.Scale) *
                            Matrix.CreateFromYawPitchRoll(p.Yaw, p.Pitch, p.Roll) *
                            Matrix.CreateTranslation(p.Position);
                }

                Vector3 emissive = p.Color.ToVector3() * brightness;
                if (p.ApplyGameLight)
                    emissive *= brightness;

                Vector3 rgb = (emissive + ambient) * p.Alpha;
                var tint = new Vector4(rgb, p.Alpha);

                var origin = p.Origin2D != default ? p.Origin2D : new Vector2(p.Texture.Width / 2, p.Texture.Height / 2);

                SpriteQuad.Write(_verts, quad * 4, p.Texture, p.TextureCrop, origin, p.Scale.X, p.Rotation2D, p.Layer, in world, tint);
                quad++;
            }
        }

        _vb.SetData(_verts, 0, totalQuads * 4, SetDataOptions.Discard);

        _fx.View = view;
        _fx.Projection = projection;

        device.SetVertexBuffer(_vb);
        device.Indices = QuadIndexBuffer.Get(device);
        device.DepthStencilState = DepthStencilState.DepthRead;
        device.RasterizerState = RenderGlobals.DefaultRasterizer;
        device.SamplerStates[0] = SamplerState.PointWrap;

        int cursor = 0;
        for (int b = 0; b < _active.Count; b++) {
            var bucket = _active[b];

            device.BlendState = bucket.Additive ? BlendState.Additive : BlendState.NonPremultiplied;
            _fx.Texture = bucket.Texture;
            _fx.CurrentTechnique.Passes[0].Apply();

            int remaining = bucket.Count;
            while (remaining > 0) {
                int n = Math.Min(remaining, QuadIndexBuffer.MaxQuads);
                device.DrawIndexedPrimitives(PrimitiveType.TriangleList, cursor * 4, 0, n * 2);
                cursor += n;
                remaining -= n;
            }
        }

        device.SetVertexBuffer(null);
    }

    // just a bs quad
    void WriteDegenerate(int quad) {
        for (int k = 0; k < 4; k++)
            _verts[quad * 4 + k] = default;
    }

    // basically just ensures that we aren't going over the quad limit 
    void EnsureCapacity(GraphicsDevice device, int quads) {
        int needed = quads * 4;
        if (_verts.Length < needed) {
            int size = _verts.Length;
            while (size < needed) size *= 2;
            _verts = new ParticleVertex[size];
        }

        if (_vb is null || _vb.IsDisposed || _vb.GraphicsDevice != device || _vb.VertexCount < needed) {
            _vb?.Dispose();
            _vb = new DynamicVertexBuffer(device, ParticleVertex.Declaration, Math.Max(_verts.Length, needed), BufferUsage.WriteOnly);
        }

        if (_fx is null || _fx.IsDisposed || _fx.GraphicsDevice != device) {
            _fx?.Dispose();
            _fx = new BasicEffect(device) {
                TextureEnabled = true,
                VertexColorEnabled = true,
                LightingEnabled = false,
                FogEnabled = false,
                World = Matrix.Identity,
                Alpha = 1f,
                DiffuseColor = Vector3.One,
            };
        }
    }
}
