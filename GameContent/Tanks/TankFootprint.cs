#pragma warning disable
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.RebirthUtils;
using TanksRebirth.GameContent.Systems.ParticleSystem;
using TanksRebirth.Graphics;

namespace TanksRebirth.GameContent.Tanks;

/// <summary>A single tread mark. Each footprint is drawn via quads batched together in one draw.</summary>
public class TankFootprint {
    public static bool ShouldTracksFade;

    public static TankFootprint[] AllFootprints = new TankFootprint[TankGame.Settings.TankFootprintLimit];

    /// <summary>How many footprints currently exist.</summary>
    public static int Count { get; private set; }

    public Vector3 Scale;
    public Vector3 Position;
    public float Rotation;

    public readonly Tank Owner;
    public Texture2D Texture;

    public readonly bool alternate;

    public long lifeTime;

    public int Id = 0;

    /// <summary>The opacity of this footprint, before lighting.</summary>
    public float Alpha = 0.4f;

    static readonly Vector3 DefaultScale = new(0.5f, 0.55f, 0.5f);

    static ulong _serialCounter;
    readonly ulong _serial;
    bool _removed;

    bool _hasGeometry;

    // 4 corners of the quad
    Vector3 _c0, _c1, _c2, _c3;

    // shared rendering stuff
    static bool _dirty = true;
    static int _cursor;
    static ParticleVertex[] _verts = new ParticleVertex[4 * 256];
    static int _vertexCount;
    static DynamicVertexBuffer _vb;
    static AlphaTestEffect _fx;

    // prevents overlapping draws
    static readonly DepthStencilState NoOverlapState = new() {
        DepthBufferEnable = true,
        DepthBufferWriteEnable = false,
        StencilEnable = true,
        StencilFunction = CompareFunction.Equal,
        ReferenceStencil = 0,
        StencilPass = StencilOperation.IncrementSaturation,
        StencilFail = StencilOperation.Keep,
        StencilDepthBufferFail = StencilOperation.Keep,
    };

    public static TankFootprint Place(Tank? owner, float rotation, bool alt = false) {
        if (owner == null) return null;

        if (FindFreeSlot() < 0)
            RemoveOldest();

        return new(owner, rotation, alt);
    }

    static int FindFreeSlot() {
        var all = AllFootprints;
        for (int n = 0; n < all.Length; n++) {
            int i = (_cursor + n) % all.Length;
            if (all[i] is null) {
                _cursor = i;
                return i;
            }
        }
        return -1;
    }

    static void RemoveOldest() {
        TankFootprint oldest = null;
        foreach (var fp in AllFootprints) {
            if (fp is null) continue;
            if (oldest is null || fp._serial < oldest._serial)
                oldest = fp;
        }
        oldest?.Remove();
    }

    public TankFootprint(Tank owner, float rotation, bool alt = false) {
        Rotation = rotation;
        alternate = alt;
        Owner = owner;
        Position = owner.Position3D;
        Scale = owner.DrawParams.Scaling;
        Texture = alt ? TextureGlobals.FootprintThick : TextureGlobals.FootprintStandard;

        _serial = _serialCounter++;

        Id = FindFreeSlot();
        AllFootprints[Id] = this;
        Count++;
        _dirty = true;
    }

    public void Update() {
        lifeTime++;

        if (ShouldTracksFade) {
            Alpha -= 0.001f * RuntimeData.DeltaTime;
            _dirty = true;

            if (Alpha <= 0)
                Remove();
        }
    }

    public void Remove() {
        if (_removed) return;
        _removed = true;

        if (Id >= 0 && Id < AllFootprints.Length && ReferenceEquals(AllFootprints[Id], this))
            AllFootprints[Id] = null;

        Count--;
        _dirty = true;
    }

    void BuildGeometry() {
        // basically same code as before, but works into the sprite quad batch system
        var scale = Scale * DefaultScale;
        var world = Matrix.CreateScale(scale) *
                    Matrix.CreateFromYawPitchRoll(Rotation, MathHelper.PiOver2, 0f) *
                    Matrix.CreateTranslation(Position);

        var quad = new ParticleVertex[4];
        SpriteQuad.Write(quad, 0, Texture, null, new Vector2(Texture.Width / 2, Texture.Height / 2), scale.X, 0f, 0f, in world, Vector4.One);
        _c0 = quad[0].Position;
        _c1 = quad[1].Position;
        _c2 = quad[2].Position;
        _c3 = quad[3].Position;
        _hasGeometry = true;
    }

    static void Rebuild() {
        int needed = Count * 4;
        if (_verts.Length < needed) {
            int size = _verts.Length;
            while (size < needed) size *= 2;
            _verts = new ParticleVertex[size];
        }

        int v = 0;
        for (int i = 0; i < AllFootprints.Length; i++) {
            var fp = AllFootprints[i];
            if (fp is null) continue;
            if (!fp._hasGeometry) fp.BuildGeometry();

            // lighting is applied per frame through the effect's DiffuseColor; the vertex only carries opacity
            var a = fp.Alpha;
            var tint = new Vector4(a, a, a, a);

            _verts[v + 0] = new ParticleVertex { Position = fp._c0, Tint = tint, UV = new Vector2(0, 0) };
            _verts[v + 1] = new ParticleVertex { Position = fp._c1, Tint = tint, UV = new Vector2(1, 0) };
            _verts[v + 2] = new ParticleVertex { Position = fp._c2, Tint = tint, UV = new Vector2(0, 1) };
            _verts[v + 3] = new ParticleVertex { Position = fp._c3, Tint = tint, UV = new Vector2(1, 1) };
            v += 4;
        }
        _vertexCount = v;
    }

    /// <summary>Draws every footprint. Texture is chosen per-footprint, so footprints are drawn in one call per texture.</summary>
    internal static void Render(GraphicsDevice device, Matrix view, Matrix projection) {
        if (Count <= 0) return;

        if (_fx is null || _fx.IsDisposed || _fx.GraphicsDevice != device) {
            _fx?.Dispose();
            _fx = new AlphaTestEffect(device) {
                VertexColorEnabled = true,
                AlphaFunction = CompareFunction.Greater,
                ReferenceAlpha = 1, // only texels that are actually visible may claim a stencil value
                World = Matrix.Identity,
                Alpha = 1f,
                FogEnabled = false,
            };
        }

        if (_dirty) {
            Rebuild();
            _dirty = false;
            UploadAll(device);
        }
        if (_vertexCount == 0 || _vb is null) return;

        bool hasStencil = device.PresentationParameters.DepthStencilFormat == DepthFormat.Depth24Stencil8;
        if (hasStencil)
            device.Clear(ClearOptions.Stencil, Color.Transparent, 1f, 0);

        // same lighting result the old BasicEffect path produced for a white sprite: (emissive + ambient * diffuse) * alpha
        float brightness = SceneManager.GameLight.Brightness;
        _fx.DiffuseColor = Vector3.One * (brightness * brightness) + Lighting.AmbientDiffuseProduct;
        _fx.View = view;
        _fx.Projection = projection;

        device.SetVertexBuffer(_vb);
        device.Indices = QuadIndexBuffer.Get(device);
        device.BlendState = BlendState.NonPremultiplied;
        device.DepthStencilState = hasStencil ? NoOverlapState : DepthStencilState.DepthRead;
        device.RasterizerState = RenderGlobals.DefaultRasterizer;
        device.SamplerStates[0] = SamplerState.PointWrap;

        // the thick and standard tread textures differ, so draw them as two ranges. Footprints are grouped by texture in UploadAll.
        DrawRange(device, 0, _standardCount, TextureGlobals.FootprintStandard);
        DrawRange(device, _standardCount, _vertexCount / 4 - _standardCount, TextureGlobals.FootprintThick);

        device.SetVertexBuffer(null);
    }

    static int _standardCount;

    static void UploadAll(GraphicsDevice device) {
        // group by texture so each texture is one draw. typically max 2 footprint types, but modders can introduce more
        int quads = _vertexCount / 4;
        var sorted = new ParticleVertex[_vertexCount];
        int cursor = 0;
        int footprintIndex = 0;
        _standardCount = 0;

        // pass 1: standard tread
        for (int pass = 0; pass < 2; pass++) {
            footprintIndex = 0;
            for (int i = 0; i < AllFootprints.Length; i++) {
                var fp = AllFootprints[i];
                if (fp is null) continue;
                bool isThick = fp.alternate;
                if ((pass == 0 && isThick) || (pass == 1 && !isThick)) { footprintIndex++; continue; }

                Array.Copy(_verts, footprintIndex * 4, sorted, cursor * 4, 4);
                cursor++;
                footprintIndex++;
            }
            if (pass == 0) _standardCount = cursor;
        }

        if (_vb is null || _vb.IsDisposed || _vb.GraphicsDevice != device || _vb.VertexCount < sorted.Length) {
            _vb?.Dispose();
            _vb = new DynamicVertexBuffer(device, ParticleVertex.Declaration, Math.Max(sorted.Length, 1024), BufferUsage.WriteOnly);
        }
        _vb.SetData(sorted, 0, sorted.Length, SetDataOptions.Discard);
    }

    static void DrawRange(GraphicsDevice device, int firstQuad, int quadCount, Texture2D texture) {
        if (quadCount <= 0) return;

        _fx.Texture = texture;
        _fx.CurrentTechnique.Passes[0].Apply();

        while (quadCount > 0) {
            int n = Math.Min(quadCount, QuadIndexBuffer.MaxQuads);
            device.DrawIndexedPrimitives(PrimitiveType.TriangleList, firstQuad * 4, 0, n * 2);
            firstQuad += n;
            quadCount -= n;
        }
    }
}
