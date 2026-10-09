using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using TanksRebirth.GameContent;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Net;

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

    /// <summary>
    /// Master switch (the "dynamic lighting on/off" option). When false nothing is captured or drawn and the game
    /// looks exactly as it did without this system. Call <see cref="Unload"/> as well to free the video memory.
    /// </summary>
    public static bool Enabled = true;
    /// <summary>
    /// Set by the scene (the Off preset) to show the unlit game without touching the user's <see cref="Enabled"/> choice.
    /// </summary>
    public static bool Suspended;
    /// <summary>True once <see cref="Initialize"/> succeeded and no unrecoverable error happened.</summary>
    public static bool IsAvailable { get; private set; }
    /// <summary>Whether lighting is drawn this frame.</summary>
    public static bool IsActive => Enabled && !Suspended && IsAvailable;

    /// <summary>Performance limits (shadow resolutions, light counts, shafts...). See <see cref="LightingQuality"/>.</summary>
    public static LightingQuality Quality = new();

    public static readonly AmbientLight Ambient = new();
    public static readonly SunLight Sun = new();
    /// <summary>Persistent lights. Add and remove freely.</summary>
    public static readonly List<Light> Lights = [];

    /// <summary>Raised once per frame during <see cref="EndFrame"/>; call <see cref="AddFrameLight"/> from here for lights that follow game objects.</summary>
    public static event Action? CollectLights;
    /// <summary>Raised once per frame during <see cref="EndFrame"/>; call <see cref="SubmitShadowCaster"/> from here for geometry that should cast shadows even when it was culled from the camera.</summary>
    public static event Action? CollectShadowCasters;

    /// <summary>
    /// How many point lights the scene would like shadowed. The actual number is also capped by
    /// <see cref="LightingQuality.MaxShadowedPointLights"/>.
    /// </summary>
    public static int MaxShadowedPointLights = 4;
    /// <summary>How many spot lights the scene would like shadowed (also capped by <see cref="LightingQuality.MaxShadowedSpotLights"/>).</summary>
    public static int MaxShadowedSpotLights = 4;
    /// <summary>Point of interest used to rank lights when there are too many.</summary>
    public static Vector3 FocusPoint = Vector3.Zero;

    /// <summary>Draws with a lower alpha than this are treated as transparent and ignored.</summary>
    public static float AlphaCutoff = 0.95f;

    /// <summary>
    /// Invisible solid boxes that block the sun (they are only drawn into the sun's shadow maps). Use them to seal
    /// gaps in level geometry, like the seams where walls meet the ceiling, that would let thin lines of sunlight through.
    /// </summary>
    public static readonly List<BoundingBox> SunBlockers = [];
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
        /// <summary>Of <see cref="ShadowDraws"/>: the sharp board map, the room map, and the lamp / headlight atlas.</summary>
        public int SunShadowDraws, RoomShadowDraws, LocalShadowDraws;
        /// <summary>True when the room map was reused from an earlier frame instead of re-rendered.</summary>
        public bool RoomShadowsCached;
        /// <summary>Lamp / headlight shadow maps reused from an earlier frame (the light didn't move).</summary>
        public int CachedShadowMaps;
        /// <summary>Point light cube faces / spot maps skipped because nothing they cover is on screen.</summary>
        public int SkippedShadowViews;
        public int ShaftDraws;
        public readonly int TotalDraws => ShadowDraws + LightBufferDraws + ShaftDraws;
        public override readonly string ToString() =>
            $"draws {CapturedDraws} (+{ShadowOnlyDraws} shadow-only), points {PointLights} ({ShadowedPointLights} shadowed), " +
            $"spots {SpotLights} ({ShadowedSpotLights} shadowed) | GPU draw calls {TotalDraws}: shadows {ShadowDraws} " +
            $"(sun {SunShadowDraws}, room {RoomShadowDraws}{(RoomShadowsCached ? " cached" : "")}, lamps {LocalShadowDraws}, {CachedShadowMaps} maps cached, {SkippedShadowViews} views skipped), " +
            $"light {LightBufferDraws}, shafts {ShaftDraws}";
    }

    // state machine

    // atlas layout for a 2048 atlas; everything doubles with a 4096 atlas (see LightingQuality.ShadowAtlasSize)
    static int ATLAS_SIZE = 2048;
    static int CUBE_TILE = 256;
    static int SPOT_TILE = 512;
    const int MAX_POINT_SHADOW_SLOTS = 6;
    const int MAX_SPOT_SHADOW_SLOTS = 6;
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
    static RenderTarget2D? _roomShadowMap;
    static RenderTarget2D? _roomShadowDilated;
    static RenderTarget2D? _shadowAtlas;
    static RenderTarget2D? _lightBuffer;
    static RenderTarget2D? _shaftBuffer;

    static bool _capturing;
    static Matrix _cameraView, _cameraProjection;
    static readonly BoundingFrustum _cameraFrustum = new(Matrix.Identity);
    static readonly BoundingFrustum _scratchFrustum = new(Matrix.Identity);
    static readonly BoundingFrustum _shadowViewFrustum = new(Matrix.Identity);

    static readonly List<DrawRecord> _draws = [];
    static readonly HashSet<ModelMeshPart> _capturedParts = [];
    static readonly List<Light> _frameLights = [];
    static readonly List<Light> _activeLights = [];
    static readonly List<PointLight> _points = [];
    static readonly List<SpotLight> _spots = [];
    static readonly Dictionary<Light, ShadowSlot> _shadowSlots = [];

    /// <summary>What each atlas slot holds, so a light that keeps its slot can reuse last frame's shadow map.</summary>
    sealed class SlotState {
        public Light? Owner;
        public bool OwnerChanged;
        public int LastRendered = int.MinValue / 2;
        public Vector3 Position, Direction;
        public float Range, Angle;
    }
    static readonly SlotState[] _pointSlotStates = CreateSlotStates(MAX_POINT_SHADOW_SLOTS);
    static readonly SlotState[] _spotSlotStates = CreateSlotStates(MAX_SPOT_SHADOW_SLOTS);
    static readonly Dictionary<Light, SlotState> _slotStateOf = [];
    static readonly List<Light> _pointCandidates = [], _spotCandidates = [];
    static int _frameIndex;

    static SlotState[] CreateSlotStates(int count) {
        var states = new SlotState[count];
        for (int i = 0; i < count; i++) states[i] = new SlotState();
        return states;
    }

    static void ResetSlotStates() {
        foreach (var state in _pointSlotStates) state.Owner = null;
        foreach (var state in _spotSlotStates) state.Owner = null;
    }
    static readonly Dictionary<ModelMesh, MeshLighting> _meshLighting = [];
    static readonly HashSet<ModelMesh> _noShadowMeshes = [];
    static readonly Dictionary<VertexBuffer, bool> _hasNormals = [];
    static readonly HashSet<Model> _instrumented = [];

    static readonly ShadowSlot[] _pointSlots = new ShadowSlot[MAX_POINT_SHADOW_SLOTS];
    static readonly ShadowSlot[] _spotSlots = new ShadowSlot[MAX_SPOT_SHADOW_SLOTS];
    static ShadowSlot _noShadowSlot;

    static Matrix _sunViewProjection;
    static Matrix _roomViewProjection;
    static bool _roomShadowsActive;
    // the room map is reused while the sun stands still (see LightingQuality.RoomShadowRefreshInterval)
    static bool _roomCacheValid;
    static Vector3 _roomCacheDirection;
    static int _roomCacheAge;
    static Vector4 _roomCacheParams;
    static int _roomCacheBlockerCount;
    static Matrix _lastViewProjection;
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
    static EffectParameter? _pRoomClipX, _pRoomClipY, _pRoomClipZ, _pRoomShadowParams, _pRoomShadowEnabled, _pRoomShadowMap;
    // one parameter per light slot and field (the shader avoids arrays, see lighting.fx)
    static readonly EffectParameter?[] _pLightPosition = new EffectParameter?[LIGHTS_PER_PASS], _pLightColor = new EffectParameter?[LIGHTS_PER_PASS],
        _pLightDirCone = new EffectParameter?[LIGHTS_PER_PASS], _pLightParams = new EffectParameter?[LIGHTS_PER_PASS],
        _pLightRect = new EffectParameter?[LIGHTS_PER_PASS], _pLightAxisX = new EffectParameter?[LIGHTS_PER_PASS], _pLightAxisY = new EffectParameter?[LIGHTS_PER_PASS];
    static EffectParameter? _pAtlasSize, _pShadowAtlas, _pShadowLightPosInvRange, _pUnlitLight;
    static EffectParameter? _pShaftParams, _pShaftColor, _pShaftMaxGlow;
    static BlendState _screen = null!;
    static EffectParameter? _pSplitPosition, _pNeutralValue, _pSourceTexture;

    static EffectTechnique _tShadowLinear = null!, _tShadowOrtho = null!, _tAmbientSun = null!, _tPointLights = null!,
        _tSpotLights = null!, _tPointLightsFast = null!, _tSpotLightsFast = null!, _tUnlit = null!, _tSunShafts = null!,
        _tComposite = null!, _tShadowDilate = null!;
    // bound in place of shadow maps that a quality setting turned off (white = fully lit)
    static Texture2D _white = null!;

    // setup

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
            _tPointLightsFast = Technique("PointLightsFast");
            _tSpotLightsFast = Technique("SpotLightsFast");
            _tUnlit = Technique("Unlit");
            _tSunShafts = Technique("SunShafts");
            _tComposite = Technique("Composite");
            _tShadowDilate = Technique("ShadowDilate");

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
            _pRoomClipX = p["RoomClipX"];
            _pRoomClipY = p["RoomClipY"];
            _pRoomClipZ = p["RoomClipZ"];
            _pRoomShadowParams = p["RoomShadowParams"];
            _pRoomShadowEnabled = p["RoomShadowEnabled"];
            _pRoomShadowMap = p["RoomShadowMap"];
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
            _pShaftMaxGlow = p["ShaftMaxGlow"];
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
            // screen blend (a + b - a*b): beams brighten dark areas but can't push bright ones past white
            _screen = new BlendState {
                Name = "Lighting.Screen",
                ColorSourceBlend = Blend.InverseDestinationColor,
                ColorDestinationBlend = Blend.One,
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

            _white = new Texture2D(device, 1, 1, false, SurfaceFormat.Color) { Name = "Lighting.White" };
            _white.SetData([Color.White]);

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
        float texel = 1f / ATLAS_SIZE;
        // keep the 3x3 tent filter (which reaches 2 texels out) inside each face
        const float inset = 2.5f;

        // layout in cube tiles (C = atlas / 8, a spot tile is 2C):
        //   x: 0     3C    6C   8C
        //      [pt0 ][pt1 ][sp0]     rows 0..2C
        //      [pt2 ][pt3 ][sp1]     rows 2C..4C
        //      [pt4 ][pt5 ][sp2]     rows 4C..6C
        //      [wht][sp4][sp5][sp3]  rows 6C..8C
        // point lights: 3x2 blocks of cube faces
        for (int i = 0; i < MAX_POINT_SHADOW_SLOTS; i++) {
            var origin = new Point(i % 2 * CUBE_TILE * 3, i / 2 * CUBE_TILE * 2);
            _pointSlots[i] = new ShadowSlot {
                Origin = origin,
                Rect = new Vector4(origin.X * texel, origin.Y * texel, CUBE_TILE * texel, inset / CUBE_TILE),
            };
        }
        // spot lights: the right column, then along the bottom row
        for (int i = 0; i < MAX_SPOT_SHADOW_SLOTS; i++) {
            var origin = i < 4
                ? new Point(CUBE_TILE * 6, i * SPOT_TILE)
                : new Point((i - 3) * SPOT_TILE, CUBE_TILE * 6);
            _spotSlots[i] = new ShadowSlot {
                Origin = origin,
                Rect = new Vector4(origin.X * texel, origin.Y * texel, SPOT_TILE * texel, inset / SPOT_TILE),
            };
        }
        // the bottom left spot-sized tile is never rendered and stays white (= fully lit)
        var white = new Point(SPOT_TILE / 2, CUBE_TILE * 6 + SPOT_TILE / 2);
        _noShadowSlot = new ShadowSlot { Origin = white, Rect = new Vector4(white.X * texel, white.Y * texel, 0f, 0f) };
    }

    // model instrumentation

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
        InvalidateShadowCache();
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
        _capturing = IsActive;
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

    // rendering

    /// <summary>
    /// Stops recording, renders shadows and lights, and composites them onto <paramref name="target"/>.
    /// Leaves <paramref name="target"/> bound as the render target.
    /// </summary>
    public static void EndFrame(RenderTarget2D target) {
        var wasCapturing = _capturing;
        _capturing = false;
        if (!wasCapturing || !IsActive || target is null || target.IsDisposed)
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
        var oldSampler3 = _device.SamplerStates[3];

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
            _device.Textures[4] = null;
            _device.SamplerStates[0] = oldSampler0;
            _device.SamplerStates[1] = oldSampler1;
            _device.SamplerStates[2] = oldSampler2;
            _device.SamplerStates[3] = oldSampler3;
        }
    }

    static void Render(RenderTarget2D target) {
        var stats = new FrameStats();
        foreach (var d in _draws) {
            if ((d.Flags & DrawFlags.ShadowOnly) != 0) stats.ShadowOnlyDraws++;
            else stats.CapturedDraws++;
        }

        EnsureTargets(target);      // first: it may rebuild the atlas layout that GatherLights hands out
        GatherLights(ref stats);

        _device.RasterizerState = RasterizerState.CullNone;

        var q = Quality;
        var sunActive = Sun.Enabled && Sun.Intensity > 0f;
        var sunShadows = sunActive && Sun.CastsShadows && q.SunShadows && _sunShadowMap is not null;
        _roomShadowsActive = false;
        if (sunShadows)
            RenderSunShadows(ref stats);

        if (_shadowSlots.Count > 0)
            RenderLocalShadows(ref stats);

        SetCameraParameters();
        _pSunShadowEnabled?.SetValue(sunShadows ? 1f : 0f);
        _pSunShadowMap?.SetValue(sunShadows ? _sunShadowMap : _white);
        // the shafts always march through the room map; without a room cascade they get the board cascade instead
        _pRoomShadowEnabled?.SetValue(_roomShadowsActive ? 1f : 0f);
        _pRoomShadowMap?.SetValue(_roomShadowsActive ? _roomShadowDilated : sunShadows ? _sunShadowMap : _white);
        // no shadowed lamps this frame (or none allowed): every light reads "fully lit" from a white texel
        _pShadowAtlas?.SetValue(_shadowSlots.Count > 0 && _shadowAtlas is not null ? _shadowAtlas : _white);
        _pAtlasSize?.SetValue(new Vector2(ATLAS_SIZE, 1f / ATLAS_SIZE));
        _pLightScale?.SetValue(0.5f);
        _pDither?.SetValue(Dither);

        RenderLightBuffer(sunActive, ref stats);

        var shafts = sunShadows && q.LightShafts && _shaftBuffer is not null && Sun.Shafts.Enabled && Sun.Shafts.Density > 0f;
        _shaftDrawsThisFrame = 0;
        if (shafts)
            RenderShafts();
        stats.ShaftDraws = _shaftDrawsThisFrame;

        Composite(target, shafts);

        // debug for showing lights
        /*foreach (var light in Lights) {
            light.CastsShadows = true;
            light.Color = Color.White;
            light.Intensity = 1f;
            light.Wrap = 1f;
            light.Range = 2500f;

            if (light is SpotLight s) {
                s.OuterAngle = MathHelper.ToRadians(30);
                s.InnerAngle = MathHelper.ToRadians(10);
                s.Direction = Vector3.Normalize(new Vector3(0, -1, MathF.Sin(RuntimeData.RunTime / 20) / 5));
            }
            //GameHandler.Particles.MakeShineSpot(light.Position, Color.White, 1f);
            //if (light is SpotLight s)
            //    GameHandler.Particles.MakeShineSpot(light.Position + s.Direction * 25, Color.White, 0.5f);
        }*/

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
        var maxLights = Math.Max(0, Quality.MaxLocalLights);
        if (_activeLights.Count > maxLights)
            _activeLights.RemoveRange(maxLights, _activeLights.Count - maxLights);

        // the scene asks, quality caps; without an atlas (quality turned lamp shadows off) nothing gets a slot
        var atlas = _shadowAtlas is not null;
        int pointShadowBudget = atlas ? Math.Clamp(Math.Min(MaxShadowedPointLights, Quality.MaxShadowedPointLights), 0, MAX_POINT_SHADOW_SLOTS) : 0;
        int spotShadowBudget = atlas ? Math.Clamp(Math.Min(MaxShadowedSpotLights, Quality.MaxShadowedSpotLights), 0, MAX_SPOT_SHADOW_SLOTS) : 0;

        _pointCandidates.Clear();
        _spotCandidates.Clear();
        foreach (var light in _activeLights) {
            switch (light) {
                case SpotLight spot:
                    _spots.Add(spot);
                    if (spot.CastsShadows && _spotCandidates.Count < spotShadowBudget)
                        _spotCandidates.Add(spot);
                    break;
                case PointLight point:
                    _points.Add(point);
                    if (point.CastsShadows && _pointCandidates.Count < pointShadowBudget)
                        _pointCandidates.Add(point);
                    break;
            }
        }
        _slotStateOf.Clear();
        AssignSlots(_pointCandidates, _pointSlotStates, _pointSlots, pointShadowBudget);
        AssignSlots(_spotCandidates, _spotSlotStates, _spotSlots, spotShadowBudget);
        stats.ShadowedPointLights = _pointCandidates.Count;
        stats.ShadowedSpotLights = _spotCandidates.Count;
        stats.PointLights = _points.Count;
        stats.SpotLights = _spots.Count;
    }

    /// <summary>
    /// Gives each shadowed light an atlas slot, keeping the slot it had last frame when possible (so its shadow map
    /// can be reused). Slots past the budget or without a light this frame are released.
    /// </summary>
    static void AssignSlots(List<Light> candidates, SlotState[] states, ShadowSlot[] slots, int budget) {
        // 1. lights that already own a slot keep it
        for (int i = 0; i < states.Length; i++) {
            var state = states[i];
            state.OwnerChanged = false;
            if (i >= budget || state.Owner is null || !candidates.Contains(state.Owner)) {
                state.Owner = null;
                continue;
            }
            _slotStateOf[state.Owner] = state;
            _shadowSlots[state.Owner] = slots[i];
        }
        // 2. new lights take free slots
        foreach (var light in candidates) {
            if (_slotStateOf.ContainsKey(light))
                continue;
            for (int i = 0; i < budget; i++) {
                if (states[i].Owner is not null)
                    continue;
                states[i].Owner = light;
                states[i].OwnerChanged = true;
                _slotStateOf[light] = states[i];
                _shadowSlots[light] = slots[i];
                break;
            }
        }
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

    /// <summary>
    /// Creates, resizes or releases render targets to match <see cref="Quality"/>. Targets a setting turned off are
    /// disposed, so lowering quality also frees video memory.
    /// </summary>
    static void EnsureTargets(RenderTarget2D target) {
        var q = Quality;
        var maxSize = _device.GraphicsProfile == GraphicsProfile.HiDef ? 4096 : 2048;

        // sun: sharp board map, then the room map (+ its crack-filled copy)
        var sunWanted = q.SunShadows && Sun.CastsShadows;
        var sunSize = Math.Clamp(q.SunShadowMapSize, 256, maxSize);
        if (!sunWanted)
            Release(ref _sunShadowMap);
        else if (_sunShadowMap is null || _sunShadowMap.IsDisposed || _sunShadowMap.Width != sunSize) {
            _sunShadowMap?.Dispose();
            _sunShadowMap = new RenderTarget2D(_device, sunSize, sunSize, false, SurfaceFormat.Color, DepthFormat.Depth24);
        }

        var roomWanted = sunWanted && q.RoomShadows && Sun.RoomShadows;
        var roomSize = Math.Clamp(q.RoomShadowMapSize, 256, maxSize);
        if (!roomWanted) {
            Release(ref _roomShadowMap);
            Release(ref _roomShadowDilated);
        }
        else if (_roomShadowMap is null || _roomShadowMap.IsDisposed || _roomShadowMap.Width != roomSize) {
            _roomShadowMap?.Dispose();
            _roomShadowMap = new RenderTarget2D(_device, roomSize, roomSize, false, SurfaceFormat.Color, DepthFormat.Depth24);
            _roomShadowDilated?.Dispose();
            // kept between frames (the room map is cached), so the contents must survive
            _roomShadowDilated = new RenderTarget2D(_device, roomSize, roomSize, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            _roomCacheValid = false;
        }

        // point / spot shadow atlas
        var atlasWanted = q.MaxShadowedPointLights > 0 || q.MaxShadowedSpotLights > 0;
        var atlasSize = q.ShadowAtlasSize >= 4096 && maxSize >= 4096 ? 4096 : 2048;
        if (!atlasWanted)
            Release(ref _shadowAtlas);
        else if (_shadowAtlas is null || _shadowAtlas.IsDisposed || _shadowAtlas.Width != atlasSize) {
            _shadowAtlas?.Dispose();
            ATLAS_SIZE = atlasSize;
            CUBE_TILE = atlasSize / 8;
            SPOT_TILE = atlasSize / 4;
            BuildAtlasLayout();
            // kept between frames (cached shadow maps), so the contents must survive
            _shadowAtlas = new RenderTarget2D(_device, ATLAS_SIZE, ATLAS_SIZE, false, SurfaceFormat.Color, DepthFormat.Depth24, 0, RenderTargetUsage.PreserveContents);
            ResetSlotStates();
            // clear once so the reserved "no shadow" region is white even before the first shadow render
            _device.SetRenderTarget(_shadowAtlas);
            _device.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.White, 1f, 0);
        }

        if (_lightBuffer is null || _lightBuffer.IsDisposed || _lightBuffer.Width != target.Width || _lightBuffer.Height != target.Height) {
            _lightBuffer?.Dispose();
            _lightBuffer = new RenderTarget2D(_device, target.Width, target.Height, false, SurfaceFormat.Color, DepthFormat.Depth24);
        }

        // light shafts
        var div = Math.Clamp(q.ShaftDownsample, 1, 4);
        int sw = Math.Max(1, target.Width / div), sh = Math.Max(1, target.Height / div);
        if (!(sunWanted && q.LightShafts))
            Release(ref _shaftBuffer);
        else if (Sun.Shafts.Enabled && (_shaftBuffer is null || _shaftBuffer.IsDisposed || _shaftBuffer.Width != sw || _shaftBuffer.Height != sh)) {
            _shaftBuffer?.Dispose();
            _shaftBuffer = new RenderTarget2D(_device, sw, sh, false, SurfaceFormat.Color, DepthFormat.Depth24);
        }
    }

    static void Release(ref RenderTarget2D? target) {
        target?.Dispose();
        target = null;
    }

    // ------------------------------------------------------------------------------------------ shadows

    static void RenderSunShadows(ref FrameStats stats) {
        var size = _sunShadowMap!.Width;
        var dir = SafeNormalize(Sun.Direction, Vector3.Down);
        var up = MathF.Abs(dir.Y) > 0.99f ? Vector3.UnitZ : Vector3.UnitY;

        // ---- cascade 1: sharp map around the board
        var depth = MathF.Max(10f, Sun.ShadowDepth);
        float texelWorld;
        if (Sun.ShadowBounds is { } bounds)
            _sunViewProjection = FitBoardCascade(bounds, dir, up, size, depth, out texelWorld, out depth);
        else {
            var radius = MathF.Max(1f, Sun.ShadowRadius);
            var view = Matrix.CreateLookAt(Sun.ShadowCenter - dir * depth * 0.5f, Sun.ShadowCenter, up);
            // snap to whole texels so shadows don't shimmer if the center moves
            texelWorld = 2f * radius / size;
            var originLS = Vector3.Transform(Vector3.Zero, view);
            var snap = new Vector3(
                MathF.Round(originLS.X / texelWorld) * texelWorld - originLS.X,
                MathF.Round(originLS.Y / texelWorld) * texelWorld - originLS.Y, 0f);
            view *= Matrix.CreateTranslation(snap);
            _sunViewProjection = view * Matrix.CreateOrthographicOffCenter(-radius, radius, -radius, radius, 0f, depth);
        }

        stats.SunShadowDraws += RenderSunCascade(_sunShadowMap, _sunViewProjection, ref stats);
        SetCascadeParameters(_pSunClipX, _pSunClipY, _pSunClipZ, _pSunShadowParams, _sunViewProjection,
            new Vector4(1f / size, size, Sun.ShadowBias / depth, texelWorld * 1.5f));

        // ---- cascade 2: coarse map over the whole room (everything outside cascade 1, and the shafts)
        if (Sun.RoomShadows && Quality.RoomShadows && _roomShadowMap is not null && _roomShadowDilated is not null) {
            _roomShadowsActive = true;
            // The room map mostly holds the room itself, which never moves, so it's only re-rendered when the sun
            // has moved noticeably or every RoomShadowRefreshInterval frames (moving tanks are covered by the board map).
            _roomCacheAge++;
            var refresh = !_roomCacheValid
                || _roomCacheAge >= Math.Max(1, Quality.RoomShadowRefreshInterval)
                || Vector3.Dot(dir, _roomCacheDirection) < MathF.Cos(MathHelper.ToRadians(0.1f))
                || SunBlockers.Count != _roomCacheBlockerCount;
            if (refresh && TryFitRoomCascade(dir, up, out var roomVP, out var roomTexel, out var roomDepth)) {
                _roomViewProjection = roomVP;
                stats.RoomShadowDraws += RenderSunCascade(_roomShadowMap, _roomViewProjection, ref stats);
                // coarser texels need a proportionally larger bias
                var bias = MathF.Max(Sun.ShadowBias, roomTexel * 0.75f);
                _roomCacheParams = new Vector4(1f / _roomShadowMap.Width, _roomShadowMap.Width, bias / roomDepth, roomTexel * 2f);

                // close the one texel cracks that seams between meshes leave in the coarse map
                _device.SetRenderTarget(_roomShadowDilated);
                _device.DepthStencilState = DepthStencilState.None;
                _pRoomShadowMap?.SetValue(_roomShadowMap);
                _effect.CurrentTechnique = _tShadowDilate;
                DrawFullscreen();
                _pRoomShadowMap?.SetValue((Texture2D?)null);

                _roomCacheValid = true;
                _roomCacheDirection = dir;
                _roomCacheAge = 0;
                _roomCacheBlockerCount = SunBlockers.Count;
            }
            else {
                stats.RoomShadowsCached = true;
            }
            _roomShadowsActive = _roomCacheValid;
            if (_roomCacheValid)
                SetCascadeParameters(_pRoomClipX, _pRoomClipY, _pRoomClipZ, _pRoomShadowParams, _roomViewProjection, _roomCacheParams);
        }
        if (!_roomShadowsActive) {
            // no room cascade: the shafts read the board cascade through the room parameters
            SetCascadeParameters(_pRoomClipX, _pRoomClipY, _pRoomClipZ, _pRoomShadowParams, _sunViewProjection,
                new Vector4(1f / size, size, Sun.ShadowBias / depth, texelWorld * 1.5f));
        }
    }

    /// <summary>
    /// Fits the sharp cascade tightly around <paramref name="box"/> as seen from the sun. For a low sun the box looks
    /// thin from the light's point of view, so the texels get spent where they are needed instead of on a square.
    /// Casters between the box and the sun are kept by extending the depth range <paramref name="depthTowardsSun"/> towards it.
    /// </summary>
    static Matrix FitBoardCascade(BoundingBox box, Vector3 dir, Vector3 up, int size, float depthTowardsSun, out float texelWorld, out float depth) {
        var center = (box.Min + box.Max) * 0.5f;
        // a fixed light-space origin (the box center) keeps the texel grid stable while the sun doesn't move
        var view = Matrix.CreateLookAt(center - dir, center, up);
        box.GetCorners(_boxCorners);
        var lsMin = new Vector3(float.MaxValue);
        var lsMax = new Vector3(float.MinValue);
        foreach (var c in _boxCorners) {
            var v = Vector3.Transform(c, view);
            lsMin = Vector3.Min(lsMin, v);
            lsMax = Vector3.Max(lsMax, v);
        }
        // round the size up in steps so it doesn't change every frame while the sun moves slowly, then snap to texels
        const float step = 16f;
        float w = MathF.Ceiling((lsMax.X - lsMin.X + 2f) / step) * step;
        float h = MathF.Ceiling((lsMax.Y - lsMin.Y + 2f) / step) * step;
        float tx = w / size, ty = h / size;
        float cx = MathF.Round((lsMin.X + lsMax.X) * 0.5f / tx) * tx;
        float cy = MathF.Round((lsMin.Y + lsMax.Y) * 0.5f / ty) * ty;
        texelWorld = MathF.Max(tx, ty);

        // view space looks down -Z: the box spans z in [lsMin.Z, lsMax.Z], the sun is towards +Z
        float far = -lsMin.Z + 4f;
        float near = -lsMax.Z - depthTowardsSun;
        depth = far - near;
        return view * Matrix.CreateOrthographicOffCenter(cx - w * 0.5f, cx + w * 0.5f, cy - h * 0.5f, cy + h * 0.5f, near, far);
    }

    /// <summary>Renders one sun cascade and returns how many draw calls it took.</summary>
    static int RenderSunCascade(RenderTarget2D target, Matrix viewProjection, ref FrameStats stats) {
        var before = stats.ShadowDraws;
        _device.SetRenderTarget(target);
        _device.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.White, 1f, 0);
        _device.BlendState = BlendState.Opaque;
        _device.DepthStencilState = DepthStencilState.Default;

        _effect.CurrentTechnique = _tShadowOrtho;
        _pViewProjection?.SetValue(viewProjection);
        _lastViewProjection = viewProjection;
        _scratchFrustum.Matrix = viewProjection;
        foreach (var d in _draws) {
            if ((d.Flags & DrawFlags.CastsShadows) == 0)
                continue;
            if (d.Bounds.Radius < float.MaxValue && _scratchFrustum.Contains(d.Bounds) == ContainmentType.Disjoint)
                continue;
            DrawPart(d, DrawMode.Shadow);
            stats.ShadowDraws++;
        }
        DrawSunBlockers(ref stats);
        return stats.ShadowDraws - before;
    }

    static void SetCascadeParameters(EffectParameter? clipX, EffectParameter? clipY, EffectParameter? clipZ, EffectParameter? shadowParams,
        in Matrix m, Vector4 parameters) {
        // columns of the (orthographic) view projection, so clip.x = dot(float4(p, 1), ClipX)
        clipX?.SetValue(new Vector4(m.M11, m.M21, m.M31, m.M41));
        clipY?.SetValue(new Vector4(m.M12, m.M22, m.M32, m.M42));
        clipZ?.SetValue(new Vector4(m.M13, m.M23, m.M33, m.M43));
        shadowParams?.SetValue(parameters);
    }

    static readonly Vector3[] _boxCorners = new Vector3[8];
    static VertexPositionColor[] _blockerVertices = [];
    static short[] _blockerIndices = [];
    // BoundingBox.GetCorners order: near face (z max) TL TR BR BL, far face (z min) TL TR BR BL
    static readonly short[] _boxIndices = [0, 1, 2, 0, 2, 3, 4, 6, 5, 4, 7, 6, 4, 5, 1, 4, 1, 0, 3, 2, 6, 3, 6, 7, 1, 5, 6, 1, 6, 2, 4, 0, 3, 4, 3, 7];

    static void DrawSunBlockers(ref FrameStats stats) {
        int count = 0;
        foreach (var box in SunBlockers)
            if (_scratchFrustum.Contains(box) != ContainmentType.Disjoint)
                count++;
        if (count == 0)
            return;

        if (_blockerVertices.Length < count * 8) {
            _blockerVertices = new VertexPositionColor[count * 8];
            _blockerIndices = new short[count * 36];
        }
        int b = 0;
        foreach (var box in SunBlockers) {
            if (_scratchFrustum.Contains(box) == ContainmentType.Disjoint)
                continue;
            box.GetCorners(_boxCorners);
            for (int i = 0; i < 8; i++)
                _blockerVertices[b * 8 + i] = new VertexPositionColor(_boxCorners[i], Color.White);
            for (int i = 0; i < 36; i++)
                _blockerIndices[b * 36 + i] = (short)(b * 8 + _boxIndices[i]);
            b++;
        }
        _pWorld?.SetValue(Matrix.Identity);
        _effect.CurrentTechnique.Passes[0].Apply();
        _device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, _blockerVertices, 0, count * 8, _blockerIndices, 0, count * 12);
        stats.ShadowDraws++;
    }

    /// <summary>Fits an orthographic sun projection tightly around <see cref="SunLight.RoomShadowBounds"/> (or everything drawn).</summary>
    static bool TryFitRoomCascade(Vector3 dir, Vector3 up, out Matrix viewProjection, out float texelWorld, out float depth) {
        viewProjection = Matrix.Identity;
        texelWorld = 1f;
        depth = 1f;

        BoundingBox box;
        if (Sun.RoomShadowBounds is { } bounds) {
            box = bounds;
        }
        else {
            var min = new Vector3(float.MaxValue);
            var max = new Vector3(float.MinValue);
            foreach (var d in _draws) {
                if (d.Bounds.Radius >= float.MaxValue || float.IsNaN(d.Bounds.Radius))
                    continue;
                var r = new Vector3(d.Bounds.Radius);
                min = Vector3.Min(min, d.Bounds.Center - r);
                max = Vector3.Max(max, d.Bounds.Center + r);
            }
            if (min.X > max.X)
                return false;
            // quantize so small movements (tanks, shells) don't make the map swim
            const float q = 64f;
            min = new Vector3(MathF.Floor(min.X / q), MathF.Floor(min.Y / q), MathF.Floor(min.Z / q)) * q;
            max = new Vector3(MathF.Ceiling(max.X / q), MathF.Ceiling(max.Y / q), MathF.Ceiling(max.Z / q)) * q;
            box = new BoundingBox(min, max);
        }

        var center = (box.Min + box.Max) * 0.5f;
        var halfDiagonal = MathF.Max(1f, (box.Max - box.Min).Length() * 0.5f);
        var view = Matrix.CreateLookAt(center - dir * (halfDiagonal + 10f), center, up);

        box.GetCorners(_boxCorners);
        var lsMin = new Vector3(float.MaxValue);
        var lsMax = new Vector3(float.MinValue);
        foreach (var c in _boxCorners) {
            var v = Vector3.Transform(c, view);
            lsMin = Vector3.Min(lsMin, v);
            lsMax = Vector3.Max(lsMax, v);
        }
        // a little margin so geometry right on the bounds still lands inside the map
        var pad = (lsMax.X - lsMin.X + lsMax.Y - lsMin.Y) * 0.005f + 4f;
        lsMin -= new Vector3(pad);
        lsMax += new Vector3(pad);

        var size = _roomShadowMap!.Width;
        texelWorld = MathF.Max(lsMax.X - lsMin.X, lsMax.Y - lsMin.Y) / size;
        // view space looks down -Z: near/far are the negated z extents
        var near = MathF.Max(0f, -lsMax.Z);
        var far = -lsMin.Z;
        depth = MathF.Max(10f, far - near);
        viewProjection = view * Matrix.CreateOrthographicOffCenter(lsMin.X, lsMax.X, lsMin.Y, lsMax.Y, near, near + depth);
        return true;
    }

    static readonly DepthStencilState _clearDepth = new() {
        Name = "Lighting.ClearTile",
        DepthBufferEnable = true,
        DepthBufferWriteEnable = true,
        DepthBufferFunction = CompareFunction.Always,
    };
    // a viewport-filling quad at the far plane: drawn white with "always" depth, it clears just one atlas tile
    static readonly VertexPositionTexture[] _farQuad = [
        new(new Vector3(-1, 1, 1), new Vector2(0, 0)),
        new(new Vector3(1, 1, 1), new Vector2(1, 0)),
        new(new Vector3(-1, -1, 1), new Vector2(0, 1)),
        new(new Vector3(1, -1, 1), new Vector2(1, 1)),
    ];

    /// <summary>
    /// Renders the point / spot shadow maps. The atlas is kept between frames: a light that kept its slot and didn't
    /// move only re-renders every <see cref="LightingQuality.StaticLightShadowInterval"/> frames (lamps stand still;
    /// tanks moving through their light are picked up a few frames later at most).
    /// </summary>
    static void RenderLocalShadows(ref FrameStats stats) {
        _frameIndex++;
        var interval = Math.Max(1, Quality.StaticLightShadowInterval);
        var targetSet = false;

        foreach (var (light, slot) in _shadowSlots) {
            var state = _slotStateOf[light];
            var spot = light as SpotLight;
            var direction = spot?.Direction ?? Vector3.Zero;
            var angle = spot?.OuterAngle ?? 0f;
            var changed = state.OwnerChanged
                || Vector3.DistanceSquared(state.Position, light.Position) > 0.01f
                || Vector3.DistanceSquared(state.Direction, direction) > 1e-6f
                || state.Range != light.Range || state.Angle != angle;
            if (!changed && _frameIndex - state.LastRendered < interval) {
                stats.CachedShadowMaps++;
                continue;
            }

            if (!targetSet) {
                _device.SetRenderTarget(_shadowAtlas);
                _device.BlendState = BlendState.Opaque;
                targetSet = true;
            }

            // clear this light's tiles only (the rest of the atlas holds other lights' cached maps)
            _device.Viewport = spot is not null
                ? new Viewport(slot.Origin.X, slot.Origin.Y, SPOT_TILE, SPOT_TILE)
                : new Viewport(slot.Origin.X, slot.Origin.Y, CUBE_TILE * 3, CUBE_TILE * 2);
            _device.DepthStencilState = _clearDepth;
            _effect.CurrentTechnique = _tComposite;
            _pSourceTexture?.SetValue(_white);
            _pSplitPosition?.SetValue(0f);
            _effect.CurrentTechnique.Passes[0].Apply();
            _device.DrawUserPrimitives(PrimitiveType.TriangleStrip, _farQuad, 0, 2);
            _pSourceTexture?.SetValue((Texture2D?)null);

            _device.DepthStencilState = DepthStencilState.Default;
            _effect.CurrentTechnique = _tShadowLinear;

            var range = light.Range;
            var near = MathF.Max(0.5f, range * 0.005f);
            var lightSphere = new BoundingSphere(light.Position, range);
            _pShadowLightPosInvRange?.SetValue(new Vector4(light.Position, 1f / range));

            if (spot is not null) {
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

            state.LastRendered = _frameIndex;
            state.Position = light.Position;
            state.Direction = direction;
            state.Range = light.Range;
            state.Angle = angle;
        }
    }

    static void RenderShadowView(Matrix viewProjection, BoundingSphere lightSphere, ref FrameStats stats) {
        // a shadow view only matters if something it covers can be seen: skip cube faces / spots facing away from the camera
        _shadowViewFrustum.Matrix = viewProjection;
        if (!_shadowViewFrustum.Intersects(_cameraFrustum)) {
            stats.SkippedShadowViews++;
            return;
        }
        _pViewProjection?.SetValue(viewProjection);
        _lastViewProjection = viewProjection;
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
            stats.LocalShadowDraws++;
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
            _effect.CurrentTechnique = Quality.SoftLocalShadows ? _tPointLights : _tPointLightsFast;
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
            _effect.CurrentTechnique = Quality.SoftLocalShadows ? _tSpotLights : _tSpotLightsFast;
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
        _pShaftMaxGlow?.SetValue(MathF.Max(1f, s.MaxGlow));
        _effect.CurrentTechnique = _tSunShafts;

        foreach (var d in _draws) {
            if ((d.Flags & DrawFlags.ShadowOnly) != 0)
                continue;
            DrawPart(d, DrawMode.Camera);
            _shaftDrawsThisFrame++;
        }
    }

    static int _shaftDrawsThisFrame;

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
            _device.BlendState = _screen;
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
        // every captured draw normally shares the camera's view projection; only upload it when it changes
        if (mode != DrawMode.Shadow && d.ViewProjection != _lastViewProjection) {
            _pViewProjection?.SetValue(d.ViewProjection);
            _lastViewProjection = d.ViewProjection;
        }
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

    /// <summary>
    /// Forces cached shadows (the room map) to be re-rendered next frame. Call after moving or changing static
    /// geometry. Changing presets, the sun or <see cref="SunBlockers"/> is picked up automatically.
    /// </summary>
    public static void InvalidateShadowCache() {
        _roomCacheValid = false;
        ResetSlotStates();
    }

    /// <summary>Releases all render targets (call after turning <see cref="Enabled"/> off). They are recreated on demand.</summary>
    public static void Unload() {
        _sunShadowMap?.Dispose(); _sunShadowMap = null;
        _roomShadowMap?.Dispose(); _roomShadowMap = null;
        _roomShadowDilated?.Dispose(); _roomShadowDilated = null;
        _shadowAtlas?.Dispose(); _shadowAtlas = null;
        _lightBuffer?.Dispose(); _lightBuffer = null;
        _shaftBuffer?.Dispose(); _shaftBuffer = null;
    }
}
