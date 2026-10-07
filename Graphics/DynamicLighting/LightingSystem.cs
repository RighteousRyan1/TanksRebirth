using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace TanksRebirth.Graphics.DynamicLighting;

/// <summary>
/// Plug and play dynamic lighting for MonoGame 3.8 (DesktopGL / Shader Model 3).
/// </summary>
/// <remarks>
/// <para>How it works:</para>
/// <list type="number">
/// <item>Every <see cref="Model"/> passed to <see cref="Instrument"/> gets <see cref="LightCaptureEffect"/>s.
/// They look and behave exactly like the original <see cref="BasicEffect"/>s, but every mesh part drawn between
/// <see cref="BeginFrame"/> and <see cref="EndFrame"/> is recorded with its world matrix. Nothing else in the game
/// has to change how it draws.</item>
/// <item>At <see cref="EndFrame"/> the recorded geometry is rendered again into shadow maps (sun + an atlas for
/// point and spot lights) and into a light buffer (ambient + sun + local lights, with PCF shadows).</item>
/// <item>The light buffer is multiplied onto the finished frame (2x modulate, so lights can also over-brighten),
/// and the optional sun shafts are added on top.</item>
/// </list>
/// <para>Everything uses <see cref="SurfaceFormat.Color"/> render targets no larger than 2048 and vs_3_0 / ps_3_0
/// shaders, so it works with the Reach profile on DesktopGL.</para>
/// </remarks>
public static class LightingSystem {
    // =============================================================================================
    //  public settings
    // =============================================================================================

    /// <summary>Master switch. When false, <see cref="EndFrame"/> leaves the frame untouched.</summary>
    public static bool Enabled = true;
    /// <summary>True once <see cref="Initialize"/> succeeded and no unrecoverable error happened.</summary>
    public static bool IsAvailable { get; private set; }

    public static readonly AmbientLight Ambient = new();
    public static readonly SunLight Sun = new();
    /// <summary>Persistent lights. Add and remove freely.</summary>
    public static readonly List<Light> Lights = [];

    /// <summary>Raised once per frame during <see cref="EndFrame"/>; call <see cref="AddFrameLight"/> from here for lights that follow game objects.</summary>
    public static event Action? CollectLights;
    /// <summary>Raised once per frame during <see cref="EndFrame"/>; call <see cref="SubmitShadowCaster"/> from here for geometry that should cast shadows even when it was culled from the camera.</summary>
    public static event Action? CollectShadowCasters;

    /// <summary>Most local lights evaluated per frame (the rest are dropped by priority, then distance to <see cref="FocusPoint"/>).</summary>
    public static int MaxLocalLights = 32;
    /// <summary>Most point lights with shadows per frame (each costs 6 shadow renders). Hard cap: 8.</summary>
    public static int MaxShadowedPointLights = 4;
    /// <summary>Most spot lights with shadows per frame. Hard cap: 3.</summary>
    public static int MaxShadowedSpotLights = 3;
    /// <summary>Point of interest used to rank lights when there are too many.</summary>
    public static Vector3 FocusPoint = Vector3.Zero;

    /// <summary>Draws with a lower alpha than this are treated as transparent and ignored.</summary>
    public static float AlphaCutoff = 0.95f;
    /// <summary>Size of the sun shadow map (max 2048 for the Reach profile).</summary>
    public static int SunShadowMapSize = 2048;
    /// <summary>Light buffer value where nothing was captured (the background). 1 = untouched.</summary>
    public static float BackgroundLight = 1f;
    /// <summary>Noise added to the light buffer to hide 8 bit banding in dark scenes.</summary>
    public static float Dither = 1.5f / 255f;
    /// <summary>0..1, the part of the screen (from the left) shown without lighting, for before / after comparisons.</summary>
    public static float SplitScreen;

    /// <summary>Optional logger (defaults to the console).</summary>
    public static Action<string> Log = Console.WriteLine;

    /// <summary>Numbers from the last rendered frame.</summary>
    public static FrameStats Stats { get; private set; }

    public struct FrameStats {
        public int CapturedDraws;
        public int ShadowOnlyDraws;
        public int PointLights;
        public int SpotLights;
        public int ShadowedPointLights;
        public int ShadowedSpotLights;
        public int ShadowDraws;
        public int LightBufferDraws;
        public override readonly string ToString() =>
            $"draws {CapturedDraws} (+{ShadowOnlyDraws} shadow-only), points {PointLights} ({ShadowedPointLights} shadowed), " +
            $"spots {SpotLights} ({ShadowedSpotLights} shadowed), shadow draws {ShadowDraws}, light draws {LightBufferDraws}";
    }

    // =============================================================================================
    //  internal state
    // =============================================================================================

    const int ATLAS_SIZE = 2048;
    const int CUBE_TILE = 256;
    const int SPOT_TILE = 512;
    const int MAX_POINT_SHADOW_SLOTS = 8;
    const int MAX_SPOT_SHADOW_SLOTS = 3;
    const int LIGHTS_PER_PASS = 4;

    [Flags]
    enum DrawFlags : byte {
        None = 0,
        CastsShadows = 1,
        Lit = 2,
        ShadowOnly = 4,
        HasNormals = 8,
    }

    struct DrawRecord {
        public ModelMeshPart Part;
        public Matrix World;
        public Matrix ViewProjection;
        public BoundingSphere Bounds;
        public Vector3 Emissive;
        public DrawFlags Flags;
    }

    enum DrawMode {
        /// <summary>world matrix only, the view projection was set for the whole shadow view</summary>
        Shadow,
        /// <summary>world + the camera view projection recorded with the draw</summary>
        Camera,
        /// <summary>like Camera, for the lit techniques</summary>
        CameraLit,
    }

    struct ShadowSlot {
        public Vector4 Rect;     // xy origin (uv), z tile size (uv), w inset
        public Point Origin;     // pixels
    }

    static GraphicsDevice _device = null!;
    static Effect _effect = null!;

    static RenderTarget2D? _sunShadowMap;
    static RenderTarget2D? _shadowAtlas;
    static RenderTarget2D? _lightBuffer;
    static RenderTarget2D? _shaftBuffer;

    static bool _capturing;
    static Matrix _cameraView, _cameraProjection;
    static readonly BoundingFrustum _cameraFrustum = new(Matrix.Identity);
    static readonly BoundingFrustum _scratchFrustum = new(Matrix.Identity);

    static readonly List<DrawRecord> _draws = [];
    static readonly HashSet<ModelMeshPart> _capturedParts = [];
    static readonly List<Light> _frameLights = [];
    static readonly List<Light> _activeLights = [];
    static readonly List<PointLight> _points = [];
    static readonly List<SpotLight> _spots = [];
    static readonly Dictionary<Light, ShadowSlot> _shadowSlots = [];
    static readonly Dictionary<ModelMesh, MeshLighting> _meshLighting = [];
    static readonly HashSet<ModelMesh> _noShadowMeshes = [];
    static readonly Dictionary<VertexBuffer, bool> _hasNormals = [];
    static readonly HashSet<Model> _instrumented = [];

    static readonly ShadowSlot[] _pointSlots = new ShadowSlot[MAX_POINT_SHADOW_SLOTS];
    static readonly ShadowSlot[] _spotSlots = new ShadowSlot[MAX_SPOT_SHADOW_SLOTS];
    static ShadowSlot _noShadowSlot;

    static Matrix _sunViewProjection;
    static float _shaftJitter;

    static BlendState _modulate2X = null!;
    static BlendState _additive = null!;
    static DepthStencilState _depthReadLessEqual = null!;
    static readonly VertexPositionTexture[] _fullscreenQuad = [
        new(new Vector3(-1, 1, 0), new Vector2(0, 0)),
        new(new Vector3(1, 1, 0), new Vector2(1, 0)),
        new(new Vector3(-1, -1, 0), new Vector2(0, 1)),
        new(new Vector3(1, -1, 0), new Vector2(1, 1)),
    ];

    // per-pass constant scratch
    static readonly Vector4[] _lPosInvRange = new Vector4[LIGHTS_PER_PASS];
    static readonly Vector4[] _lColor = new Vector4[LIGHTS_PER_PASS];
    static readonly Vector4[] _lDirCone = new Vector4[LIGHTS_PER_PASS];
    static readonly Vector4[] _lParams = new Vector4[LIGHTS_PER_PASS];
    static readonly Vector4[] _lShadowRect = new Vector4[LIGHTS_PER_PASS];
    static readonly Vector4[] _lAxisX = new Vector4[LIGHTS_PER_PASS];
    static readonly Vector4[] _lAxisY = new Vector4[LIGHTS_PER_PASS];
    static readonly BoundingSphere[] _batchSpheres = new BoundingSphere[LIGHTS_PER_PASS];

    // cube face orientation. MUST match CubeAtlasUV in lighting.fx
    static readonly Vector3[] _cubeForward = [Vector3.UnitX, -Vector3.UnitX, Vector3.UnitY, -Vector3.UnitY, Vector3.UnitZ, -Vector3.UnitZ];
    static readonly Vector3[] _cubeUp = [Vector3.UnitY, Vector3.UnitY, -Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitY, Vector3.UnitY];

    // effect parameters (null when the compiler stripped them)
    static EffectParameter? _pWorld, _pViewProjection;
    static EffectParameter? _pCameraPosition, _pCameraForward, _pCameraIsOrtho;
    static EffectParameter? _pLightScale, _pDither;
    static EffectParameter? _pAmbientSky, _pAmbientGround, _pSunDirection, _pSunColor, _pSunWrap;
    static EffectParameter? _pSunClipX, _pSunClipY, _pSunClipZ, _pSunShadowParams, _pSunShadowEnabled, _pSunShadowMap;
    // one parameter per light slot and field (the shader avoids arrays, see lighting.fx)
    static readonly EffectParameter?[] _pLightPosition = new EffectParameter?[LIGHTS_PER_PASS], _pLightColor = new EffectParameter?[LIGHTS_PER_PASS],
        _pLightDirCone = new EffectParameter?[LIGHTS_PER_PASS], _pLightParams = new EffectParameter?[LIGHTS_PER_PASS],
        _pLightRect = new EffectParameter?[LIGHTS_PER_PASS], _pLightAxisX = new EffectParameter?[LIGHTS_PER_PASS], _pLightAxisY = new EffectParameter?[LIGHTS_PER_PASS];
    static EffectParameter? _pAtlasSize, _pShadowAtlas, _pShadowLightPosInvRange, _pUnlitLight;
    static EffectParameter? _pShaftParams, _pShaftColor;
    static EffectParameter? _pSplitPosition, _pNeutralValue, _pSourceTexture;

    static EffectTechnique _tShadowLinear = null!, _tShadowOrtho = null!, _tAmbientSun = null!, _tPointLights = null!,
        _tSpotLights = null!, _tUnlit = null!, _tSunShafts = null!, _tComposite = null!;

    // =============================================================================================
    //  setup
    // =============================================================================================

    /// <summary>Call once after the graphics device exists. <paramref name="lightingEffect"/> is the compiled lighting.fx.</summary>
    public static bool Initialize(GraphicsDevice device, Effect lightingEffect) {
        try {
            _device = device;
            _effect = lightingEffect;

            _tShadowLinear = Technique("ShadowLinear");
            _tShadowOrtho = Technique("ShadowOrtho");
            _tAmbientSun = Technique("AmbientSun");
            _tPointLights = Technique("PointLights");
            _tSpotLights = Technique("SpotLights");
            _tUnlit = Technique("Unlit");
            _tSunShafts = Technique("SunShafts");
            _tComposite = Technique("Composite");

            var p = _effect.Parameters;
            _pWorld = p["World"];
            _pViewProjection = p["ViewProjection"];
            _pCameraPosition = p["CameraPosition"];
            _pCameraForward = p["CameraForward"];
            _pCameraIsOrtho = p["CameraIsOrtho"];
            _pLightScale = p["LightScale"];
            _pDither = p["DitherStrength"];
            _pAmbientSky = p["AmbientSky"];
            _pAmbientGround = p["AmbientGround"];
            _pSunDirection = p["SunDirection"];
            _pSunColor = p["SunColor"];
            _pSunWrap = p["SunWrap"];
            _pSunClipX = p["SunClipX"];
            _pSunClipY = p["SunClipY"];
            _pSunClipZ = p["SunClipZ"];
            _pSunShadowParams = p["SunShadowParams"];
            _pSunShadowEnabled = p["SunShadowEnabled"];
            _pSunShadowMap = p["SunShadowMap"];
            for (int i = 0; i < LIGHTS_PER_PASS; i++) {
                _pLightPosition[i] = p[$"Light{i}Position"];
                _pLightColor[i] = p[$"Light{i}Color"];
                _pLightDirCone[i] = p[$"Light{i}DirCone"];
                _pLightParams[i] = p[$"Light{i}Params"];
                _pLightRect[i] = p[$"Light{i}Rect"];
                _pLightAxisX[i] = p[$"Light{i}AxisX"];
                _pLightAxisY[i] = p[$"Light{i}AxisY"];
            }
            _pAtlasSize = p["AtlasSize"];
            _pShadowAtlas = p["ShadowAtlas"];
            _pShadowLightPosInvRange = p["ShadowLightPosInvRange"];
            _pUnlitLight = p["UnlitLight"];
            _pShaftParams = p["ShaftParams"];
            _pShaftColor = p["ShaftColor"];
            _pSplitPosition = p["SplitPosition"];
            _pNeutralValue = p["NeutralValue"];
            _pSourceTexture = p["SourceTexture"];

            _modulate2X = new BlendState {
                Name = "Lighting.Modulate2X",
                ColorSourceBlend = Blend.DestinationColor,
                ColorDestinationBlend = Blend.SourceColor,
                ColorBlendFunction = BlendFunction.Add,
                AlphaSourceBlend = Blend.Zero,
                AlphaDestinationBlend = Blend.One,
                AlphaBlendFunction = BlendFunction.Add,
            };
            _additive = new BlendState {
                Name = "Lighting.Additive",
                ColorSourceBlend = Blend.One,
                ColorDestinationBlend = Blend.One,
                ColorBlendFunction = BlendFunction.Add,
                AlphaSourceBlend = Blend.Zero,
                AlphaDestinationBlend = Blend.One,
                AlphaBlendFunction = BlendFunction.Add,
            };
            _depthReadLessEqual = new DepthStencilState {
                Name = "Lighting.DepthRead",
                DepthBufferEnable = true,
                DepthBufferWriteEnable = false,
                DepthBufferFunction = CompareFunction.LessEqual,
            };

            BuildAtlasLayout();

            IsAvailable = true;
            return true;
        }
        catch (Exception e) {
            IsAvailable = false;
            Log($"[Lighting] Initialization failed, lighting disabled: {e.Message}");
            return false;
        }
    }

    static EffectTechnique Technique(string name)
        => _effect.Techniques[name] ?? throw new InvalidOperationException($"lighting effect is missing technique '{name}'");

    static void BuildAtlasLayout() {
        const float texel = 1f / ATLAS_SIZE;

        // point lights: 3x2 blocks of 256px faces in the left 1536px
        for (int i = 0; i < MAX_POINT_SHADOW_SLOTS; i++) {
            var origin = new Point(i % 2 * CUBE_TILE * 3, i / 2 * CUBE_TILE * 2);
            _pointSlots[i] = new ShadowSlot {
                Origin = origin,
                Rect = new Vector4(origin.X * texel, origin.Y * texel, CUBE_TILE * texel, 1.5f / CUBE_TILE),
            };
        }
        // spot lights: 512px tiles in the right column
        for (int i = 0; i < MAX_SPOT_SHADOW_SLOTS; i++) {
            var origin = new Point(CUBE_TILE * 6, i * SPOT_TILE);
            _spotSlots[i] = new ShadowSlot {
                Origin = origin,
                Rect = new Vector4(origin.X * texel, origin.Y * texel, SPOT_TILE * texel, 1.5f / SPOT_TILE),
            };
        }
        // bottom right 512px is never rendered and stays white (= fully lit)
        var white = new Point(CUBE_TILE * 6 + SPOT_TILE / 2, MAX_SPOT_SHADOW_SLOTS * SPOT_TILE + SPOT_TILE / 2);
        _noShadowSlot = new ShadowSlot { Origin = white, Rect = new Vector4(white.X * texel, white.Y * texel, 0f, 0f) };
    }

    // =============================================================================================
    //  model instrumentation
    // =============================================================================================

    /// <summary>
    /// Swaps the model's <see cref="BasicEffect"/>s for <see cref="LightCaptureEffect"/>s so its draws are seen by
    /// the lighting system. Safe to call more than once, and safe to call before <see cref="Initialize"/>.
    /// </summary>
    public static void Instrument(Model? model) {
        if (model is null)
            return;

        lock (_instrumented) {
            if (!_instrumented.Add(model))
                return;
        }

        var clones = new Dictionary<Effect, LightCaptureEffect>();
        foreach (var mesh in model.Meshes) {
            foreach (var part in mesh.MeshParts) {
                switch (part.Effect) {
                    case LightCaptureEffect existing:
                        existing.Register(mesh, part);
                        break;
                    case BasicEffect basic:
                        if (!clones.TryGetValue(basic, out var capture)) {
                            capture = new LightCaptureEffect(basic);
                            clones[basic] = capture;
                        }
                        capture.Register(mesh, part);
                        break;
                }
            }
        }

        // swap after registering so sharing between meshes is preserved exactly
        foreach (var mesh in model.Meshes) {
            foreach (var part in mesh.MeshParts) {
                if (part.Effect is not null && clones.TryGetValue(part.Effect, out var capture))
                    part.Effect = capture;
            }
        }
    }

    /// <summary>Per mesh options (emissive, no shadows, ignored...).</summary>
    public static void SetMeshLighting(ModelMesh mesh, MeshLighting settings) => _meshLighting[mesh] = settings;
    /// <summary>Applies <paramref name="settings"/> to every mesh of <paramref name="model"/> whose name contains <paramref name="nameContains"/>.</summary>
    public static void SetMeshLighting(Model model, string nameContains, MeshLighting settings) {
        foreach (var mesh in model.Meshes)
            if (mesh.Name.Contains(nameContains, StringComparison.OrdinalIgnoreCase))
                _meshLighting[mesh] = settings;
    }
    /// <summary>
    /// Turns shadow casting on or off for every mesh of a model, whether it's drawn on screen or submitted as a
    /// shadow caster. Independent of <see cref="MeshLighting"/>, so it combines with emissive / ignore settings.
    /// </summary>
    public static void SetCastsShadows(Model model, bool castsShadows) {
        foreach (var mesh in model.Meshes) {
            if (castsShadows) _noShadowMeshes.Remove(mesh);
            else _noShadowMeshes.Add(mesh);
        }
    }

    public static void ClearMeshLighting(ModelMesh mesh) => _meshLighting.Remove(mesh);
    public static MeshLighting GetMeshLighting(ModelMesh mesh) => _meshLighting.TryGetValue(mesh, out var s) ? s : MeshLighting.Default;

    // =============================================================================================
    //  frame capture
    // =============================================================================================

    /// <summary>Start recording draws. Call right before the world is drawn.</summary>
    public static void BeginFrame(Matrix view, Matrix projection) {
        _draws.Clear();
        _capturedParts.Clear();
        _frameLights.Clear();
        _cameraView = view;
        _cameraProjection = projection;
        _cameraFrustum.Matrix = view * projection;
        _capturing = Enabled && IsAvailable;
    }

    /// <summary>Adds a light for the current frame only.</summary>
    public static void AddFrameLight(Light light) => _frameLights.Add(light);

    internal static void OnCaptureEffectApplied(LightCaptureEffect effect, ModelMesh mesh, ModelMeshPart part) {
        if (!_capturing || effect.Alpha < AlphaCutoff)
            return;

        var device = effect.GraphicsDevice;
        // additive draws (glows) and draws that don't write depth (decals) are not surfaces
        var blend = device.BlendState;
        if (blend.ColorDestinationBlend == Blend.One && blend.ColorSourceBlend != Blend.Zero)
            return;
        var depth = device.DepthStencilState;
        if (!depth.DepthBufferEnable || !depth.DepthBufferWriteEnable)
            return;

        var settings = GetMeshLighting(mesh);
        if (settings.Ignore)
            return;

        var receives = effect.LightingEnabled && settings.ReceivesLight;
        AddRecord(mesh, part, effect.World, effect.View * effect.Projection, receives, settings, false);
    }

    /// <summary>
    /// Records a mesh drawn with a custom effect (one the capture effects can't see). Call it next to the
    /// mesh's own draw call, with the same matrices.
    /// </summary>
    public static void Submit(ModelMesh mesh, Matrix world, Matrix view, Matrix projection, float alpha = 1f) {
        if (!_capturing || alpha < AlphaCutoff)
            return;
        var settings = GetMeshLighting(mesh);
        if (settings.Ignore)
            return;
        var viewProjection = view * projection;
        foreach (var part in mesh.MeshParts)
            if (part.PrimitiveCount > 0)
                AddRecord(mesh, part, world, viewProjection, settings.ReceivesLight, settings, false);
    }

    /// <summary>
    /// Makes a mesh cast shadows this frame without being visible (for example room walls that the camera culled
    /// but that should still block the sun). Ignored for parts that were already drawn this frame.
    /// </summary>
    public static void SubmitShadowCaster(ModelMesh mesh, Matrix world) {
        if (!_capturing)
            return;
        var settings = GetMeshLighting(mesh);
        if (settings.Ignore || !settings.CastsShadows || _noShadowMeshes.Contains(mesh))
            return;
        foreach (var part in mesh.MeshParts) {
            if (part.PrimitiveCount <= 0 || _capturedParts.Contains(part))
                continue;
            AddRecord(mesh, part, world, Matrix.Identity, false, settings, true);
        }
    }

    static void AddRecord(ModelMesh mesh, ModelMeshPart part, Matrix world, Matrix viewProjection, bool lit,
        MeshLighting settings, bool shadowOnly) {
        if (part.VertexBuffer is null || part.IndexBuffer is null)
            return;

        var flags = DrawFlags.None;
        if (settings.CastsShadows && !_noShadowMeshes.Contains(mesh)) flags |= DrawFlags.CastsShadows;
        if (shadowOnly) flags |= DrawFlags.ShadowOnly;
        else _capturedParts.Add(part);
        if (HasNormals(part.VertexBuffer)) {
            flags |= DrawFlags.HasNormals;
            if (lit) flags |= DrawFlags.Lit;
        }

        var local = mesh.BoundingSphere;
        var bounds = new BoundingSphere(Vector3.Transform(local.Center, world), local.Radius * MaxScale(world));
        // degenerate or bad bounds: never cull
        if (!(bounds.Radius > 0f) || float.IsNaN(bounds.Radius) || float.IsInfinity(bounds.Radius))
            bounds.Radius = float.MaxValue;

        _draws.Add(new DrawRecord {
            Part = part,
            World = world,
            ViewProjection = viewProjection,
            Bounds = bounds,
            Emissive = lit ? Vector3.One : settings.Emissive,
            Flags = flags,
        });
    }

    static bool HasNormals(VertexBuffer buffer) {
        if (_hasNormals.TryGetValue(buffer, out var has))
            return has;
        has = false;
        foreach (var element in buffer.VertexDeclaration.GetVertexElements()) {
            if (element.VertexElementUsage == VertexElementUsage.Normal && element.UsageIndex == 0) {
                has = true;
                break;
            }
        }
        _hasNormals[buffer] = has;
        return has;
    }

    static float MaxScale(in Matrix m) {
        var sx = new Vector3(m.M11, m.M12, m.M13).LengthSquared();
        var sy = new Vector3(m.M21, m.M22, m.M23).LengthSquared();
        var sz = new Vector3(m.M31, m.M32, m.M33).LengthSquared();
        return MathF.Sqrt(MathF.Max(sx, MathF.Max(sy, sz)));
    }

    // =============================================================================================
    //  frame rendering
    // =============================================================================================

    /// <summary>
    /// Stops recording, renders shadows and lights, and composites them onto <paramref name="target"/>.
    /// Leaves <paramref name="target"/> bound as the render target.
    /// </summary>
    public static void EndFrame(RenderTarget2D target) {
        var wasCapturing = _capturing;
        _capturing = false;
        if (!wasCapturing || !Enabled || !IsAvailable || target is null || target.IsDisposed)
            return;

        // let gameplay code add per-frame lights and casters (capture stays open for SubmitShadowCaster)
        _capturing = true;
        try {
            CollectShadowCasters?.Invoke();
            CollectLights?.Invoke();
        }
        catch (Exception e) {
            Log($"[Lighting] a light/caster callback threw: {e}");
        }
        _capturing = false;

        if (_draws.Count == 0)
            return;

        var oldBlend = _device.BlendState;
        var oldDepth = _device.DepthStencilState;
        var oldRaster = _device.RasterizerState;
        var oldSampler0 = _device.SamplerStates[0];
        var oldSampler1 = _device.SamplerStates[1];
        var oldSampler2 = _device.SamplerStates[2];

        try {
            Render(target);
        }
        catch (Exception e) {
            // never take the game down because of lighting
            IsAvailable = false;
            Log($"[Lighting] rendering failed, lighting disabled: {e}");
        }
        finally {
            _device.SetRenderTarget(target);
            _device.BlendState = oldBlend;
            _device.DepthStencilState = oldDepth;
            _device.RasterizerState = oldRaster;
            _device.Textures[0] = null;
            _device.Textures[1] = null;
            _device.Textures[2] = null;
            _device.Textures[3] = null;
            _device.SamplerStates[0] = oldSampler0;
            _device.SamplerStates[1] = oldSampler1;
            _device.SamplerStates[2] = oldSampler2;
        }
    }

    static void Render(RenderTarget2D target) {
        var stats = new FrameStats();
        foreach (var d in _draws) {
            if ((d.Flags & DrawFlags.ShadowOnly) != 0) stats.ShadowOnlyDraws++;
            else stats.CapturedDraws++;
        }

        GatherLights(ref stats);
        EnsureTargets(target);

        _device.RasterizerState = RasterizerState.CullNone;

        var sunActive = Sun.Enabled && Sun.Intensity > 0f;
        var sunShadows = sunActive && Sun.CastsShadows;
        if (sunShadows)
            RenderSunShadows(ref stats);

        if (_shadowSlots.Count > 0)
            RenderLocalShadows(ref stats);

        SetCameraParameters();
        _pSunShadowEnabled?.SetValue(sunShadows ? 1f : 0f);
        _pSunShadowMap?.SetValue(_sunShadowMap);
        _pShadowAtlas?.SetValue(_shadowAtlas);
        _pAtlasSize?.SetValue(new Vector2(ATLAS_SIZE, 1f / ATLAS_SIZE));
        _pLightScale?.SetValue(0.5f);
        _pDither?.SetValue(Dither);

        RenderLightBuffer(sunActive, ref stats);

        var shafts = sunShadows && Sun.Shafts.Enabled && Sun.Shafts.Density > 0f;
        if (shafts)
            RenderShafts();

        Composite(target, shafts);

        Stats = stats;
    }

    static void GatherLights(ref FrameStats stats) {
        _activeLights.Clear();
        _points.Clear();
        _spots.Clear();
        _shadowSlots.Clear();

        foreach (var l in Lights) ConsiderLight(l);
        foreach (var l in _frameLights) ConsiderLight(l);
        _activeLights.Sort(CompareLights);
        if (_activeLights.Count > MaxLocalLights)
            _activeLights.RemoveRange(MaxLocalLights, _activeLights.Count - MaxLocalLights);

        int pointShadowBudget = Math.Clamp(MaxShadowedPointLights, 0, MAX_POINT_SHADOW_SLOTS);
        int spotShadowBudget = Math.Clamp(MaxShadowedSpotLights, 0, MAX_SPOT_SHADOW_SLOTS);

        foreach (var light in _activeLights) {
            switch (light) {
                case SpotLight spot:
                    _spots.Add(spot);
                    if (spot.CastsShadows && stats.ShadowedSpotLights < spotShadowBudget)
                        _shadowSlots[spot] = _spotSlots[stats.ShadowedSpotLights++];
                    break;
                case PointLight point:
                    _points.Add(point);
                    if (point.CastsShadows && stats.ShadowedPointLights < pointShadowBudget)
                        _shadowSlots[point] = _pointSlots[stats.ShadowedPointLights++];
                    break;
            }
        }
        stats.PointLights = _points.Count;
        stats.SpotLights = _spots.Count;
    }

    static void ConsiderLight(Light light) {
        if (!light.Enabled || light.Intensity <= 0f || light.Range <= 0f)
            return;
        if (_cameraFrustum.Contains(new BoundingSphere(light.Position, light.Range)) == ContainmentType.Disjoint)
            return;
        _activeLights.Add(light);
    }

    static int CompareLights(Light a, Light b) {
        var p = b.Priority.CompareTo(a.Priority);
        if (p != 0) return p;
        return Vector3.DistanceSquared(a.Position, FocusPoint).CompareTo(Vector3.DistanceSquared(b.Position, FocusPoint));
    }

    static void EnsureTargets(RenderTarget2D target) {
        var sunSize = Math.Clamp(SunShadowMapSize, 256, 2048);
        if (_sunShadowMap is null || _sunShadowMap.IsDisposed || _sunShadowMap.Width != sunSize) {
            _sunShadowMap?.Dispose();
            _sunShadowMap = new RenderTarget2D(_device, sunSize, sunSize, false, SurfaceFormat.Color, DepthFormat.Depth24);
        }
        if (_shadowAtlas is null || _shadowAtlas.IsDisposed) {
            _shadowAtlas = new RenderTarget2D(_device, ATLAS_SIZE, ATLAS_SIZE, false, SurfaceFormat.Color, DepthFormat.Depth24);
            // clear once so the reserved "no shadow" region is white even before the first shadow render
            _device.SetRenderTarget(_shadowAtlas);
            _device.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.White, 1f, 0);
        }
        if (_lightBuffer is null || _lightBuffer.IsDisposed || _lightBuffer.Width != target.Width || _lightBuffer.Height != target.Height) {
            _lightBuffer?.Dispose();
            _lightBuffer = new RenderTarget2D(_device, target.Width, target.Height, false, SurfaceFormat.Color, DepthFormat.Depth24);
        }
        var div = Math.Clamp(Sun.Shafts.Downsample, 1, 4);
        int sw = Math.Max(1, target.Width / div), sh = Math.Max(1, target.Height / div);
        if (Sun.Shafts.Enabled && (_shaftBuffer is null || _shaftBuffer.IsDisposed || _shaftBuffer.Width != sw || _shaftBuffer.Height != sh)) {
            _shaftBuffer?.Dispose();
            _shaftBuffer = new RenderTarget2D(_device, sw, sh, false, SurfaceFormat.Color, DepthFormat.Depth24);
        }
    }

    // ------------------------------------------------------------------------------------------ shadows

    static void RenderSunShadows(ref FrameStats stats) {
        var size = _sunShadowMap!.Width;
        var dir = SafeNormalize(Sun.Direction, Vector3.Down);
        var radius = MathF.Max(1f, Sun.ShadowRadius);
        var depth = MathF.Max(10f, Sun.ShadowDepth);
        var up = MathF.Abs(dir.Y) > 0.99f ? Vector3.UnitZ : Vector3.UnitY;

        var view = Matrix.CreateLookAt(Sun.ShadowCenter - dir * depth * 0.5f, Sun.ShadowCenter, up);
        // snap to whole texels so shadows don't shimmer if the center moves
        var texelWorld = 2f * radius / size;
        var originLS = Vector3.Transform(Vector3.Zero, view);
        var snap = new Vector3(
            MathF.Round(originLS.X / texelWorld) * texelWorld - originLS.X,
            MathF.Round(originLS.Y / texelWorld) * texelWorld - originLS.Y, 0f);
        view *= Matrix.CreateTranslation(snap);
        var projection = Matrix.CreateOrthographicOffCenter(-radius, radius, -radius, radius, 0f, depth);
        _sunViewProjection = view * projection;

        _device.SetRenderTarget(_sunShadowMap);
        _device.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.White, 1f, 0);
        _device.BlendState = BlendState.Opaque;
        _device.DepthStencilState = DepthStencilState.Default;

        _effect.CurrentTechnique = _tShadowOrtho;
        _pViewProjection?.SetValue(_sunViewProjection);
        _scratchFrustum.Matrix = _sunViewProjection;
        foreach (var d in _draws) {
            if ((d.Flags & DrawFlags.CastsShadows) == 0)
                continue;
            if (d.Bounds.Radius < float.MaxValue && _scratchFrustum.Contains(d.Bounds) == ContainmentType.Disjoint)
                continue;
            DrawPart(d, DrawMode.Shadow);
            stats.ShadowDraws++;
        }

        // columns of the (orthographic) view projection, so clip.x = dot(float4(p, 1), SunClipX)
        var m = _sunViewProjection;
        _pSunClipX?.SetValue(new Vector4(m.M11, m.M21, m.M31, m.M41));
        _pSunClipY?.SetValue(new Vector4(m.M12, m.M22, m.M32, m.M42));
        _pSunClipZ?.SetValue(new Vector4(m.M13, m.M23, m.M33, m.M43));
        _pSunShadowParams?.SetValue(new Vector4(1f / size, size, Sun.ShadowBias / depth, texelWorld * 1.5f));
    }

    static void RenderLocalShadows(ref FrameStats stats) {
        _device.SetRenderTarget(_shadowAtlas);
        _device.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.White, 1f, 0);
        _device.BlendState = BlendState.Opaque;
        _device.DepthStencilState = DepthStencilState.Default;
        _effect.CurrentTechnique = _tShadowLinear;

        foreach (var (light, slot) in _shadowSlots) {
            var range = light.Range;
            var near = MathF.Max(0.5f, range * 0.005f);
            var lightSphere = new BoundingSphere(light.Position, range);
            _pShadowLightPosInvRange?.SetValue(new Vector4(light.Position, 1f / range));

            if (light is SpotLight spot) {
                GetSpotShadowCamera(spot, near, out var view, out var projection, out _, out _);
                _device.Viewport = new Viewport(slot.Origin.X, slot.Origin.Y, SPOT_TILE, SPOT_TILE);
                RenderShadowView(view * projection, lightSphere, ref stats);
            }
            else {
                var projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver2, 1f, near, range);
                for (int face = 0; face < 6; face++) {
                    var view = Matrix.CreateLookAt(light.Position, light.Position + _cubeForward[face], _cubeUp[face]);
                    _device.Viewport = new Viewport(slot.Origin.X + face % 3 * CUBE_TILE, slot.Origin.Y + face / 3 * CUBE_TILE, CUBE_TILE, CUBE_TILE);
                    RenderShadowView(view * projection, lightSphere, ref stats);
                }
            }
        }
    }

    static void RenderShadowView(Matrix viewProjection, BoundingSphere lightSphere, ref FrameStats stats) {
        _pViewProjection?.SetValue(viewProjection);
        _scratchFrustum.Matrix = viewProjection;
        foreach (var d in _draws) {
            if ((d.Flags & DrawFlags.CastsShadows) == 0)
                continue;
            if (d.Bounds.Radius < float.MaxValue) {
                if (!d.Bounds.Intersects(lightSphere) || _scratchFrustum.Contains(d.Bounds) == ContainmentType.Disjoint)
                    continue;
            }
            DrawPart(d, DrawMode.Shadow);
            stats.ShadowDraws++;
        }
    }

    static void GetSpotShadowCamera(SpotLight spot, float near, out Matrix view, out Matrix projection, out Vector3 axisX, out Vector3 axisY) {
        var dir = SafeNormalize(spot.Direction, Vector3.Down);
        var up = MathF.Abs(dir.Y) > 0.99f ? Vector3.UnitZ : Vector3.UnitY;
        view = Matrix.CreateLookAt(spot.Position, spot.Position + dir, up);
        var fov = MathHelper.Clamp(spot.OuterAngle * 2f + MathHelper.ToRadians(4f), MathHelper.ToRadians(10f), MathHelper.ToRadians(170f));
        projection = Matrix.CreatePerspectiveFieldOfView(fov, 1f, near, spot.Range);
        var scale = 1f / MathF.Tan(fov * 0.5f);
        // camera basis straight from the view matrix, so the shader matches the rendered map exactly
        axisX = new Vector3(view.M11, view.M21, view.M31) * scale;
        axisY = new Vector3(view.M12, view.M22, view.M32) * scale;
    }

    // ------------------------------------------------------------------------------------------ light buffer

    static void SetCameraParameters() {
        var inverseView = Matrix.Invert(_cameraView);
        var isOrtho = MathF.Abs(_cameraProjection.M44 - 1f) < 1e-4f && MathF.Abs(_cameraProjection.M34) < 1e-6f;
        _pCameraPosition?.SetValue(inverseView.Translation);
        _pCameraForward?.SetValue(SafeNormalize(inverseView.Forward, Vector3.Forward));
        _pCameraIsOrtho?.SetValue(isOrtho ? 1f : 0f);
    }

    static void RenderLightBuffer(bool sunActive, ref FrameStats stats) {
        _device.SetRenderTarget(_lightBuffer);
        var background = new Vector3(MathHelper.Clamp(BackgroundLight * 0.5f, 0f, 1f));
        _device.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, new Color(background), 1f, 0);

        // pass 1: ambient + sun, writes depth
        _device.BlendState = BlendState.Opaque;
        _device.DepthStencilState = DepthStencilState.Default;

        var sunDir = SafeNormalize(Sun.Direction, Vector3.Down);
        _pAmbientSky?.SetValue(Ambient.SkyVector);
        _pAmbientGround?.SetValue(Ambient.GroundVector);
        _pSunDirection?.SetValue(sunDir);
        _pSunColor?.SetValue(sunActive ? Sun.ColorVector : Vector3.Zero);
        _pSunWrap?.SetValue(Sun.Wrap);

        foreach (var d in _draws) {
            if ((d.Flags & DrawFlags.ShadowOnly) != 0)
                continue;
            if ((d.Flags & DrawFlags.Lit) != 0) {
                _effect.CurrentTechnique = _tAmbientSun;
                DrawPart(d, DrawMode.CameraLit);
            }
            else {
                _effect.CurrentTechnique = _tUnlit;
                _pUnlitLight?.SetValue(d.Emissive);
                DrawPart(d, DrawMode.Camera);
            }
            stats.LightBufferDraws++;
        }

        // pass 2+: local lights, additive, depth test against pass 1
        _device.BlendState = _additive;
        _device.DepthStencilState = _depthReadLessEqual;

        for (int start = 0; start < _points.Count; start += LIGHTS_PER_PASS) {
            int count = Math.Min(LIGHTS_PER_PASS, _points.Count - start);
            for (int i = 0; i < LIGHTS_PER_PASS; i++) {
                if (i < count) {
                    var light = _points[start + i];
                    FillCommon(i, light);
                    _batchSpheres[i] = new BoundingSphere(light.Position, light.Range);
                }
                else {
                    ClearSlot(i);
                }
            }
            UploadLightArrays();
            _effect.CurrentTechnique = _tPointLights;
            DrawLitBatch(count, ref stats);
        }

        for (int start = 0; start < _spots.Count; start += LIGHTS_PER_PASS) {
            int count = Math.Min(LIGHTS_PER_PASS, _spots.Count - start);
            for (int i = 0; i < LIGHTS_PER_PASS; i++) {
                if (i < count) {
                    var spot = _spots[start + i];
                    FillCommon(i, spot);
                    var near = MathF.Max(0.5f, spot.Range * 0.005f);
                    GetSpotShadowCamera(spot, near, out _, out _, out var axisX, out var axisY);
                    var cosOuter = MathF.Cos(MathHelper.Clamp(spot.OuterAngle, 0.01f, MathHelper.PiOver2 * 0.98f));
                    var cosInner = MathF.Cos(MathHelper.Clamp(MathF.Min(spot.InnerAngle, spot.OuterAngle - 0.005f), 0f, MathHelper.PiOver2));
                    _lDirCone[i] = new Vector4(SafeNormalize(spot.Direction, Vector3.Down), cosOuter);
                    _lParams[i].X = 1f / MathF.Max(cosInner - cosOuter, 1e-4f);
                    _lAxisX[i] = new Vector4(axisX, 0f);
                    _lAxisY[i] = new Vector4(axisY, 0f);
                    _batchSpheres[i] = new BoundingSphere(spot.Position, spot.Range);
                }
                else {
                    ClearSlot(i);
                }
            }
            UploadLightArrays();
            _effect.CurrentTechnique = _tSpotLights;
            DrawLitBatch(count, ref stats);
        }
    }

    static void FillCommon(int i, Light light) {
        var range = MathF.Max(light.Range, 0.01f);
        var slot = _shadowSlots.TryGetValue(light, out var s) ? s : _noShadowSlot;
        // world size of one shadow texel around half range, used for normal offset
        var tile = light is SpotLight ? SPOT_TILE : CUBE_TILE;
        var normalOffset = MathF.Max(0.35f, range / tile * 1.25f);

        _lPosInvRange[i] = new Vector4(light.Position, 1f / range);
        _lColor[i] = new Vector4(light.ColorVector, MathHelper.Clamp(light.Wrap, 0f, 1f));
        _lParams[i] = new Vector4(0f, normalOffset, light.ShadowBias / range, 0f);
        _lShadowRect[i] = slot.Rect;
        _lDirCone[i] = new Vector4(0f, -1f, 0f, -1f);
        _lAxisX[i] = Vector4.Zero;
        _lAxisY[i] = Vector4.Zero;
    }

    static void ClearSlot(int i) {
        // an unused slot: black light far away
        _lPosInvRange[i] = new Vector4(0f, -1e6f, 0f, 1f);
        _lColor[i] = Vector4.Zero;
        _lDirCone[i] = new Vector4(0f, -1f, 0f, -1f);
        _lParams[i] = new Vector4(1f, 0f, 0f, 0f);
        _lShadowRect[i] = _noShadowSlot.Rect;
        _lAxisX[i] = Vector4.Zero;
        _lAxisY[i] = Vector4.Zero;
        _batchSpheres[i] = new BoundingSphere(new Vector3(0f, -1e6f, 0f), 0f);
    }

    static void UploadLightArrays() {
        for (int i = 0; i < LIGHTS_PER_PASS; i++) {
            _pLightPosition[i]?.SetValue(_lPosInvRange[i]);
            _pLightColor[i]?.SetValue(_lColor[i]);
            _pLightDirCone[i]?.SetValue(_lDirCone[i]);
            _pLightParams[i]?.SetValue(_lParams[i]);
            _pLightRect[i]?.SetValue(_lShadowRect[i]);
            _pLightAxisX[i]?.SetValue(_lAxisX[i]);
            _pLightAxisY[i]?.SetValue(_lAxisY[i]);
        }
    }

    static void DrawLitBatch(int count, ref FrameStats stats) {
        foreach (var d in _draws) {
            if ((d.Flags & (DrawFlags.Lit | DrawFlags.ShadowOnly)) != DrawFlags.Lit)
                continue;
            if (d.Bounds.Radius < float.MaxValue) {
                bool touched = false;
                for (int i = 0; i < count && !touched; i++)
                    touched = d.Bounds.Intersects(_batchSpheres[i]);
                if (!touched)
                    continue;
            }
            DrawPart(d, DrawMode.CameraLit);
            stats.LightBufferDraws++;
        }
    }

    // ------------------------------------------------------------------------------------------ shafts + composite

    static void RenderShafts() {
        _device.SetRenderTarget(_shaftBuffer);
        _device.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.Black, 1f, 0);
        _device.BlendState = BlendState.Opaque;
        _device.DepthStencilState = DepthStencilState.Default;

        _shaftJitter = (_shaftJitter + 0.618034f) % 1f;
        var s = Sun.Shafts;
        _pShaftParams?.SetValue(new Vector4(s.MarchLength, s.Density, MathHelper.Clamp(s.Anisotropy, -0.95f, 0.95f), _shaftJitter));
        _pShaftColor?.SetValue(s.Color.ToVector3() * Sun.Intensity);
        _effect.CurrentTechnique = _tSunShafts;

        foreach (var d in _draws) {
            if ((d.Flags & DrawFlags.ShadowOnly) != 0)
                continue;
            DrawPart(d, DrawMode.Camera);
        }
    }

    static void Composite(RenderTarget2D target, bool shafts) {
        _device.SetRenderTarget(target);
        _device.DepthStencilState = DepthStencilState.None;
        _effect.CurrentTechnique = _tComposite;
        _pSplitPosition?.SetValue(MathHelper.Clamp(SplitScreen, 0f, 1f));

        _device.BlendState = _modulate2X;
        _pSourceTexture?.SetValue(_lightBuffer);
        _pNeutralValue?.SetValue(new Vector4(0.5f, 0.5f, 0.5f, 1f));
        DrawFullscreen();

        if (shafts) {
            _device.BlendState = _additive;
            _pSourceTexture?.SetValue(_shaftBuffer);
            _pNeutralValue?.SetValue(Vector4.Zero);
            DrawFullscreen();
        }
        _pSourceTexture?.SetValue((Texture2D?)null);
    }

    static void DrawFullscreen() {
        _effect.CurrentTechnique.Passes[0].Apply();
        _device.DrawUserPrimitives(PrimitiveType.TriangleStrip, _fullscreenQuad, 0, 2);
    }

    static void DrawPart(in DrawRecord d, DrawMode mode) {
        if (mode != DrawMode.Shadow)
            _pViewProjection?.SetValue(d.ViewProjection);
        _pWorld?.SetValue(d.World);
        _effect.CurrentTechnique.Passes[0].Apply();

        var part = d.Part;
        _device.SetVertexBuffer(part.VertexBuffer);
        _device.Indices = part.IndexBuffer;
        _device.DrawIndexedPrimitives(PrimitiveType.TriangleList, part.VertexOffset, part.StartIndex, part.PrimitiveCount);
    }

    static Vector3 SafeNormalize(Vector3 v, Vector3 fallback) {
        var length = v.Length();
        return length > 1e-6f ? v / length : fallback;
    }

    /// <summary>Releases the render targets. They are recreated on demand.</summary>
    public static void Unload() {
        _sunShadowMap?.Dispose(); _sunShadowMap = null;
        _shadowAtlas?.Dispose(); _shadowAtlas = null;
        _lightBuffer?.Dispose(); _lightBuffer = null;
        _shaftBuffer?.Dispose(); _shaftBuffer = null;
    }
}
