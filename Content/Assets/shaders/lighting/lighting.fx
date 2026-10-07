// ============================================================================================
//  Tanks Rebirth - dynamic lighting
// --------------------------------------------------------------------------------------------
//  Everything in here targets vs_3_0 / ps_3_0. That is the highest model the DesktopGL
//  (OpenGL / MojoShader) effect compiler accepts in MonoGame 3.8.x, and it also compiles for
//  WindowsDX (where MonoGame maps it to vs/ps_4_0_level_9_3).
//
//  Rules followed so the shader survives MojoShader -> GLSL translation:
//    * no tex2Dlod / tex2Dgrad in pixel shaders (needs extensions on some GL drivers)
//    * no texture fetches inside dynamic branches or dynamic loops
//    * pixel shaders only use plain float/float4 constants: no arrays and no matrices
//      (MonoGame re-packs array constants differently from MojoShader on OpenGL)
//    * every register of a multi-register constant is actually read (see the note below)
//    * pixel shader inputs never include POSITION
//    * render targets are plain SurfaceFormat.Color, so depth is packed into RGB (24 bits)
//
//  Techniques
//    ShadowLinear    - writes |p - lightPos| / range   (point + spot shadow maps)
//    ShadowOrtho     - writes ortho clip depth         (sun shadow map)
//    AmbientSun      - base light buffer: hemisphere ambient + sun (+ PCF shadows, two cascades)
//    PointLights     - up to 4 point lights per pass, cube shadows from an atlas (3x3 tent filter)
//    SpotLights      - up to 4 spot lights per pass, perspective shadows from an atlas (3x3 tent filter)
//    PointLightsFast / SpotLightsFast - the same with a cheaper 2x2 filter (low quality setting)
//    Unlit           - constant light value (meshes that ignore lighting / emissive meshes)
//    SunShafts       - volumetric in-scattering of the sun, ray-marched through the room shadow map
//    Composite       - full screen pass that copies a buffer (used with blend states)
//    ShadowDilate    - closes 1 texel cracks in the room shadow map (seams between meshes)
// ============================================================================================


// ---------------------------------------------------------------- per draw
// NOTE: every register of a matrix constant must be used by the code that reads it. MojoShader packs only
// the used registers, while MonoGame uploads by the declared layout, so a gap shifts every later constant.
float4x4 World;
float4x4 ViewProjection;

// ---------------------------------------------------------------- camera
float3 CameraPosition;
float3 CameraForward;
float  CameraIsOrtho;

// ---------------------------------------------------------------- output
// The light buffer stores light * LightScale so that a value of 1.0 (neutral) fits in 0.5
// and lights can over-brighten the scene up to 2x.
float  LightScale;
float  DitherStrength;

// ---------------------------------------------------------------- ambient + sun
float3 AmbientSky;
float3 AmbientGround;
float3 SunDirection;            // direction the light travels (normalized)
float3 SunColor;                // color * intensity
float  SunWrap;                 // 0 = lambert, 1 = fully wrapped
// sun shadow projection (orthographic), passed as plain vectors: clip.x = dot(float4(p, 1), SunClipX) ...
// (matrix and array constants in pixel shaders are fragile on MonoGame's OpenGL path, vectors are not)
float4 SunClipX;
float4 SunClipY;
float4 SunClipZ;
float4 SunShadowParams;         // x = 1/size, y = size, z = depth bias, w = normal offset (world units)
float  SunShadowEnabled;
// second, wider cascade that covers the whole room (used outside of the sharp board cascade + for the shafts)
float4 RoomClipX;
float4 RoomClipY;
float4 RoomClipZ;
float4 RoomShadowParams;        // same layout as SunShadowParams
float  RoomShadowEnabled;

texture RoomShadowMap;
sampler RoomShadowSampler = sampler_state
{
    Texture = <RoomShadowMap>;
    MinFilter = Point;
    MagFilter = Point;
    MipFilter = None;
    AddressU = Clamp;
    AddressV = Clamp;
};

texture SunShadowMap;
sampler SunShadowSampler = sampler_state
{
    Texture = <SunShadowMap>;
    MinFilter = Point;
    MagFilter = Point;
    MipFilter = None;
    AddressU = Clamp;
    AddressV = Clamp;
};

// ---------------------------------------------------------------- local lights
// four slots per pass, one parameter per slot (no arrays, see the note on SunClipX)
//   Position  xyz position, w = 1 / range
//   Color     rgb color * intensity, w = wrap
//   DirCone   spot: xyz direction, w = cos(outer)
//   Params    x = 1 / (cos(inner) - cos(outer)), y = normal offset, z = depth bias
//   Rect      xy = atlas origin (uv), z = tile size (uv), w = inset (tile-local uv)
//   AxisX/Y   spot: shadow camera right/up * projection scale
#define DECLARE_LIGHT(i) float4 Light##i##Position; float4 Light##i##Color; float4 Light##i##DirCone; \
    float4 Light##i##Params; float4 Light##i##Rect; float4 Light##i##AxisX; float4 Light##i##AxisY;
DECLARE_LIGHT(0)
DECLARE_LIGHT(1)
DECLARE_LIGHT(2)
DECLARE_LIGHT(3)
float2 AtlasSize;                       // x = size in pixels, y = 1 / size

texture ShadowAtlas;
sampler ShadowAtlasSampler = sampler_state
{
    Texture = <ShadowAtlas>;
    MinFilter = Point;
    MagFilter = Point;
    MipFilter = None;
    AddressU = Clamp;
    AddressV = Clamp;
};

// ---------------------------------------------------------------- shadow pass
float4 ShadowLightPosInvRange;

// ---------------------------------------------------------------- unlit
float3 UnlitLight;

// ---------------------------------------------------------------- shafts
float4 ShaftParams;     // x = march length, y = density, z = anisotropy (g), w = jitter offset
float3 ShaftColor;
float  ShaftMaxGlow;    // cap for the forward-scattering boost when looking towards the sun

// ---------------------------------------------------------------- composite
float  SplitPosition;   // 0..1, pixels left of this are shown without lighting
float4 NeutralValue;    // value written left of the split (0.5 for modulate, 0 for add)

texture SourceTexture;
sampler SourceSampler = sampler_state
{
    Texture = <SourceTexture>;
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = None;
    AddressU = Clamp;
    AddressV = Clamp;
};

// ============================================================================================
//  helpers
// ============================================================================================

float3 PackDepth(float depth)
{
    depth = clamp(depth, 0.0, 0.99999);
    float3 enc = frac(depth * float3(1.0, 255.0, 65025.0));
    enc -= enc.yzz * float3(1.0 / 255.0, 1.0 / 255.0, 0.0);
    return enc;
}

float UnpackDepth(float3 rgb)
{
    return dot(rgb, float3(1.0, 1.0 / 255.0, 1.0 / 65025.0));
}

float Hash(float3 p)
{
    p = frac(p * 0.1031);
    p += dot(p, p.yzx + 33.33);
    return frac((p.x + p.y) * p.z);
}

// direction from the surface towards the viewer
float3 ViewVector(float3 worldPos)
{
    float3 toCamPersp = normalize(CameraPosition - worldPos);
    return lerp(toCamPersp, -CameraForward, CameraIsOrtho);
}

// geometry is drawn without culling, so flip normals that face away from the viewer
float3 FaceNormal(float3 normal, float3 v)
{
    float3 n = normalize(normal);
    return n * (step(0.0, dot(n, v)) * 2.0 - 1.0);
}

float WrapDiffuse(float ndl, float wrap)
{
    return saturate((ndl + wrap) / (1.0 + wrap));
}

float3 SunClip(float3 p)
{
    float4 h = float4(p, 1.0);
    return float3(dot(h, SunClipX), dot(h, SunClipY), dot(h, SunClipZ));
}

float3 RoomClip(float3 p)
{
    float4 h = float4(p, 1.0);
    return float3(dot(h, RoomClipX), dot(h, RoomClipY), dot(h, RoomClipZ));
}

// 3x3 tent filter built from 16 point taps (equivalent to 9 bilinear PCF lookups).
// Written as macros so both cascades can use it with their own sampler.
#define SUN_TAP(SAMP, ox, oy) step(depth, UnpackDepth(tex2D(SAMP, base + float2(ox, oy) * texel).rgb))
#define SUN_ROW(SAMP, oy) (SUN_TAP(SAMP, -1.0, oy) * (1.0 - f.x) + SUN_TAP(SAMP, 0.0, oy) + SUN_TAP(SAMP, 1.0, oy) + SUN_TAP(SAMP, 2.0, oy) * f.x)
#define SUN_TENT(SAMP, UV, DEPTH, PARAMS, RESULT) \
    { \
        float texel = PARAMS.x; \
        float depth = DEPTH; \
        float2 t = UV * PARAMS.y - 0.5; \
        float2 f = frac(t); \
        float2 base = (t - f + 0.5) * texel; \
        RESULT = (SUN_ROW(SAMP, -1.0) * (1.0 - f.y) + SUN_ROW(SAMP, 0.0) + SUN_ROW(SAMP, 1.0) + SUN_ROW(SAMP, 2.0) * f.y) / 9.0; \
    }

// 1 inside the [0,1] square, 0 outside; fades over the outer 1/fadeScale of the square when fadeScale > 0
float CascadeWeight(float2 uv, float z, float fadeScale)
{
    float2 e = min(uv, 1.0 - uv);
    float edge = min(e.x, e.y);
    return saturate(edge * fadeScale) * step(0.0, edge) * step(z, 1.0) * step(0.0, z);
}

// Sun shadow with two cascades: the sharp board map, and a coarser map covering the whole room.
// The board map wins where it exists (fading out over its border), the room map fills everything else.
float SunShadow(float3 worldPos, float3 normal, float ndl)
{
    float offset = 1.5 - ndl;

    float3 cb = SunClip(worldPos + normal * (SunShadowParams.w * offset));
    float2 uvb = cb.xy * float2(0.5, -0.5) + 0.5;
    float litBoard;
    SUN_TENT(SunShadowSampler, uvb, cb.z - SunShadowParams.z, SunShadowParams, litBoard)

    float3 cr = RoomClip(worldPos + normal * (RoomShadowParams.w * offset));
    float2 uvr = cr.xy * float2(0.5, -0.5) + 0.5;
    float litRoom;
    SUN_TENT(RoomShadowSampler, uvr, cr.z - RoomShadowParams.z, RoomShadowParams, litRoom)

    // anything outside of both maps counts as lit
    float room = lerp(1.0, litRoom, CascadeWeight(uvr, cr.z, 100000.0) * RoomShadowEnabled);
    return lerp(room, litBoard, CascadeWeight(uvb, cb.z, 12.0) * SunShadowEnabled);
}

#define ATLAS_TAP(ox, oy) step(depth, UnpackDepth(tex2D(ShadowAtlasSampler, base + float2(ox, oy) * AtlasSize.y).rgb))
#define ATLAS_ROW(oy) (ATLAS_TAP(-1.0, oy) * (1.0 - f.x) + ATLAS_TAP(0.0, oy) + ATLAS_TAP(1.0, oy) + ATLAS_TAP(2.0, oy) * f.x)

// 3x3 tent PCF inside a tile of the shadow atlas (16 point taps, same filter as the sun).
// Tiles are inset by 2.5 texels on the C# side so the taps never reach a neighbouring tile.
float AtlasShadow(float2 atlasUV, float depth)
{
    float2 t = atlasUV * AtlasSize.x - 0.5;
    float2 f = frac(t);
    float2 base = (t - f + 0.5) * AtlasSize.y;
    return (ATLAS_ROW(-1.0) * (1.0 - f.y) + ATLAS_ROW(0.0) + ATLAS_ROW(1.0) + ATLAS_ROW(2.0) * f.y) / 9.0;
}

// 2x2 bilinear PCF (4 taps): the cheap version for low quality settings
float AtlasShadowFast(float2 atlasUV, float depth)
{
    float2 t = atlasUV * AtlasSize.x - 0.5;
    float2 f = frac(t);
    float2 base = (t - f + 0.5) * AtlasSize.y;
    return lerp(lerp(ATLAS_TAP(0.0, 0.0), ATLAS_TAP(1.0, 0.0), f.x), lerp(ATLAS_TAP(0.0, 1.0), ATLAS_TAP(1.0, 1.0), f.x), f.y);
}

// Cube map face selection without branches. Face order and orientation match the
// CreateLookAt(position, position + forward, up) calls made in LightingSystem.cs:
//   0 +X (up +Y)   1 -X (up +Y)   2 +Y (up -Z)   3 -Y (up +Z)   4 +Z (up +Y)   5 -Z (up +Y)
// faces are laid out in the atlas as a 3x2 block: column = face % 3, row = face / 3
float2 CubeAtlasUV(float3 d, float4 rect)
{
    float3 a = abs(d);
    float3 s = step(0.0, d) * 2.0 - 1.0;

    float mx = step(a.y, a.x) * step(a.z, a.x);
    float my = (1.0 - mx) * step(a.z, a.y);
    float mz = 1.0 - mx - my;

    float major = max(mx * a.x + my * a.y + mz * a.z, 1e-5);
    float2 uvX = float2(s.x * d.z, d.y);
    float2 uvY = float2(-d.x, -s.y * d.z);
    float2 uvZ = float2(-s.z * d.x, d.y);
    float2 ndc = (uvX * mx + uvY * my + uvZ * mz) / major;
    float2 tc = clamp(ndc * float2(0.5, -0.5) + 0.5, rect.w, 1.0 - rect.w);

    float negative = mx * (1.0 - step(0.0, d.x)) + my * (1.0 - step(0.0, d.y)) + mz * (1.0 - step(0.0, d.z));
    float face = my * 2.0 + mz * 4.0 + negative;
    float row = step(2.5, face);
    float column = face - row * 3.0;

    return rect.xy + (float2(column, row) + tc) * rect.z;
}

// The light terms return the unshadowed light and where to look it up in the shadow atlas. The pixel shaders
// apply the shadow filter themselves (AtlasShadow or AtlasShadowFast), which is how the two quality levels share this code.
float3 PointLightTerm(float3 worldPos, float3 n, float4 posInvRange, float4 color, float4 params, float4 rect,
                      out float2 shadowUV, out float shadowDepth)
{
    float3 d = worldPos - posInvRange.xyz;
    float dist = length(d);
    float3 l = -d / max(dist, 1e-4);
    float ndl = dot(n, l);

    float x = saturate(1.0 - dist * dist * posInvRange.w * posInvRange.w);
    float atten = x * x;

    float3 ds = d + n * (params.y * (1.5 - saturate(ndl)));
    shadowDepth = length(ds) * posInvRange.w - params.z;
    shadowUV = CubeAtlasUV(ds, rect);

    return color.rgb * (WrapDiffuse(ndl, color.w) * atten);
}

float3 SpotLightTerm(float3 worldPos, float3 n, float4 posInvRange, float4 color, float4 dirCone,
                     float4 params, float4 rect, float4 axisX, float4 axisY, out float2 shadowUV, out float shadowDepth)
{
    float3 d = worldPos - posInvRange.xyz;
    float dist = length(d);
    float3 l = -d / max(dist, 1e-4);
    float ndl = dot(n, l);

    float x = saturate(1.0 - dist * dist * posInvRange.w * posInvRange.w);
    float atten = x * x;

    float cone = saturate((dot(-l, dirCone.xyz) - dirCone.w) * params.x);
    cone *= cone;

    float3 ds = d + n * (params.y * (1.5 - saturate(ndl)));
    float z = max(dot(ds, dirCone.xyz), 1e-3);
    float2 ndc = float2(dot(ds, axisX.xyz), dot(ds, axisY.xyz)) / z;
    float2 tc = clamp(ndc * float2(0.5, -0.5) + 0.5, rect.w, 1.0 - rect.w);
    shadowDepth = length(ds) * posInvRange.w - params.z;
    shadowUV = rect.xy + tc * rect.z;

    return color.rgb * (WrapDiffuse(ndl, color.w) * atten * cone);
}

float4 EncodeLight(float3 light, float3 worldPos)
{
    float noise = (Hash(worldPos) - 0.5) * DitherStrength;
    return float4(light * LightScale + noise, 1.0);
}

// ============================================================================================
//  vertex shaders
// ============================================================================================

struct LitVSInput
{
    float4 Position : POSITION0;
    float3 Normal   : NORMAL0;
};

struct LitVSOutput
{
    float4 Position : POSITION0;
    float3 WorldPos : TEXCOORD0;
    float3 Normal   : TEXCOORD1;
};

// pixel shader inputs leave out POSITION (it isn't a valid ps_3_0 input; some compilers alias it onto TEXCOORD0)
struct LitPSInput
{
    float3 WorldPos : TEXCOORD0;
    float3 Normal   : TEXCOORD1;
};

LitVSOutput LitVS(LitVSInput input)
{
    LitVSOutput output;
    float4 worldPos = mul(input.Position, World);
    output.Position = mul(worldPos, ViewProjection);
    output.WorldPos = worldPos.xyz;
    // the game only uses uniform scales, so the world matrix is fine for normals (normalized per pixel)
    output.Normal = mul(input.Normal, (float3x3)World);
    return output;
}

struct PositionVSInput
{
    float4 Position : POSITION0;
};

struct ShadowVSOutput
{
    float4 Position : POSITION0;
    float4 Data     : TEXCOORD0;    // xyz = world position, w = clip depth
};

struct ShadowPSInput
{
    float4 Data     : TEXCOORD0;
};

ShadowVSOutput ShadowVS(PositionVSInput input)
{
    ShadowVSOutput output;
    float4 worldPos = mul(input.Position, World);
    float4 clip = mul(worldPos, ViewProjection);
    output.Position = clip;
    output.Data = float4(worldPos.xyz, clip.z / clip.w);
    return output;
}

struct ScreenVSInput
{
    float4 Position : POSITION0;
    float2 TexCoord : TEXCOORD0;
};

struct ScreenVSOutput
{
    float4 Position : POSITION0;
    float2 TexCoord : TEXCOORD0;
};

struct ScreenPSInput
{
    float2 TexCoord : TEXCOORD0;
};

ScreenVSOutput ScreenVS(ScreenVSInput input)
{
    ScreenVSOutput output;
    output.Position = input.Position;
    output.TexCoord = input.TexCoord;
    return output;
}

// ============================================================================================
//  pixel shaders
// ============================================================================================

float4 ShadowLinearPS(ShadowPSInput input) : COLOR0
{
    float dist = length(input.Data.xyz - ShadowLightPosInvRange.xyz) * ShadowLightPosInvRange.w;
    return float4(PackDepth(dist), 1.0);
}

float4 ShadowOrthoPS(ShadowPSInput input) : COLOR0
{
    return float4(PackDepth(input.Data.w), 1.0);
}

float4 AmbientSunPS(LitPSInput input) : COLOR0
{
    float3 v = ViewVector(input.WorldPos);
    float3 n = FaceNormal(input.Normal, v);

    float3 ambient = lerp(AmbientGround, AmbientSky, n.y * 0.5 + 0.5);

    float ndl = dot(n, -SunDirection);
    float shadow = SunShadow(input.WorldPos, n, saturate(ndl));
    float3 sun = SunColor * (WrapDiffuse(ndl, SunWrap) * shadow);

    return EncodeLight(ambient + sun, input.WorldPos);
}

#define POINT(i, SHADOW) \
    { \
        float2 uv; float depth; \
        float3 c = PointLightTerm(input.WorldPos, n, Light##i##Position, Light##i##Color, Light##i##Params, Light##i##Rect, uv, depth); \
        light += c * SHADOW(uv, depth); \
    }
#define SPOT(i, SHADOW) \
    { \
        float2 uv; float depth; \
        float3 c = SpotLightTerm(input.WorldPos, n, Light##i##Position, Light##i##Color, Light##i##DirCone, Light##i##Params, \
                                 Light##i##Rect, Light##i##AxisX, Light##i##AxisY, uv, depth); \
        light += c * SHADOW(uv, depth); \
    }
#define LIGHTS_PS(NAME, LIGHT, SHADOW) \
    float4 NAME(LitPSInput input) : COLOR0 \
    { \
        float3 v = ViewVector(input.WorldPos); \
        float3 n = FaceNormal(input.Normal, v); \
        float3 light = 0.0; \
        LIGHT(0, SHADOW) LIGHT(1, SHADOW) LIGHT(2, SHADOW) LIGHT(3, SHADOW) \
        return EncodeLight(light, input.WorldPos); \
    }

LIGHTS_PS(PointLightsPS, POINT, AtlasShadow)
LIGHTS_PS(PointLightsFastPS, POINT, AtlasShadowFast)
LIGHTS_PS(SpotLightsPS, SPOT, AtlasShadow)
LIGHTS_PS(SpotLightsFastPS, SPOT, AtlasShadowFast)

float4 UnlitPS(ShadowPSInput input) : COLOR0
{
    return float4(UnlitLight * LightScale, 1.0);
}

#define SHAFT_STEPS 16.0
// one point tap per step: the jitter between pixels and frames plus the soft knee below hide the coarse texels
#define SHAFT_STEP visible += step(sp.z, UnpackDepth(tex2D(RoomShadowSampler, sp.xy).rgb)); sp += stepS;

// Ray marches from the surface back towards the viewer through the sun's room shadow map
// (when the room cascade is off, LightingSystem binds the board cascade here instead).
// The ray is transformed into shadow map space once (the sun projection is orthographic, so
// this is linear) and clipped against the map's [0,1] cube, so every step is a single tap.
float4 SunShaftsPS(ShadowPSInput input) : COLOR0
{
    float3 worldPos = input.Data.xyz;
    float3 v = ViewVector(worldPos);

    float3 c0 = RoomClip(worldPos);
    float3 c1 = RoomClip(worldPos + v * ShaftParams.x);
    float3 s0 = float3(c0.xy * float2(0.5, -0.5) + 0.5, c0.z - RoomShadowParams.z);
    float3 s1 = float3(c1.xy * float2(0.5, -0.5) + 0.5, c1.z - RoomShadowParams.z);

    // clip the segment to the shadow map volume (slab test)
    float3 dir = s1 - s0;
    dir += (step(0.0, dir) * 2.0 - 1.0) * 1e-6;
    float3 invDir = 1.0 / dir;
    float3 tA = -s0 * invDir;
    float3 tB = (1.0 - s0) * invDir;
    float3 tNear = min(tA, tB);
    float3 tFar = max(tA, tB);
    float tEnter = max(max(tNear.x, tNear.y), max(tNear.z, 0.0));
    float tExit = min(min(tFar.x, tFar.y), min(tFar.z, 1.0));
    float coverage = saturate(tExit - tEnter);

    float jitter = frac(Hash(worldPos) + ShaftParams.w);
    float3 stepS = dir * (coverage / SHAFT_STEPS);
    float3 sp = s0 + dir * tEnter + stepS * jitter;

    // manually unrolled (16 steps)
    float visible = 0.0;
    SHAFT_STEP SHAFT_STEP SHAFT_STEP SHAFT_STEP
    SHAFT_STEP SHAFT_STEP SHAFT_STEP SHAFT_STEP
    SHAFT_STEP SHAFT_STEP SHAFT_STEP SHAFT_STEP
    SHAFT_STEP SHAFT_STEP SHAFT_STEP SHAFT_STEP

    // average over the whole ray: the part outside of the map receives no sun
    visible *= coverage / SHAFT_STEPS;

    // Henyey-Greenstein phase: brighter when looking towards the sun
    float g = ShaftParams.z;
    float cosTheta = dot(-v, SunDirection);
    float phase = (1.0 - g * g) / pow(abs(1.0 + g * g - 2.0 * g * cosTheta), 1.5);

    // HG peaks at (1 + g) / (1 - g)^2 (10x for g = 0.6), which blows out to white when looking into the beams
    phase = min(phase, ShaftMaxGlow);

    // soft knee: thin haze stays linear, thick beams roll off towards 1 instead of clipping
    float3 shaft = ShaftColor * (visible * ShaftParams.y * phase * SunShadowEnabled);
    shaft = 1.0 - exp(-shaft);
    return float4(shaft, 1.0);
}

// Min filter over a cross of 5 texels. Seams between separate meshes (wall/ceiling edges) can rasterize as
// one texel wide gaps in the coarse room map, which would let thin lines of sunlight through.
#define DILATE_TAP(ox, oy) UnpackDepth(tex2D(RoomShadowSampler, input.TexCoord + float2(ox, oy) * RoomShadowParams.x).rgb)
float4 ShadowDilatePS(ScreenPSInput input) : COLOR0
{
    float d = min(min(DILATE_TAP(0.0, 0.0), DILATE_TAP(1.0, 0.0)), min(DILATE_TAP(-1.0, 0.0), min(DILATE_TAP(0.0, 1.0), DILATE_TAP(0.0, -1.0))));
    return float4(PackDepth(d), 1.0);
}

float4 CompositePS(ScreenPSInput input) : COLOR0
{
    float4 color = tex2D(SourceSampler, input.TexCoord);
    return lerp(NeutralValue, color, step(SplitPosition, input.TexCoord.x));
}

// ============================================================================================
//  techniques
// ============================================================================================

technique ShadowLinear
{
    pass P0
    {
        VertexShader = compile vs_3_0 ShadowVS();
        PixelShader = compile ps_3_0 ShadowLinearPS();
    }
}

technique ShadowOrtho
{
    pass P0
    {
        VertexShader = compile vs_3_0 ShadowVS();
        PixelShader = compile ps_3_0 ShadowOrthoPS();
    }
}

technique AmbientSun
{
    pass P0
    {
        VertexShader = compile vs_3_0 LitVS();
        PixelShader = compile ps_3_0 AmbientSunPS();
    }
}

technique PointLights
{
    pass P0
    {
        VertexShader = compile vs_3_0 LitVS();
        PixelShader = compile ps_3_0 PointLightsPS();
    }
}

technique PointLightsFast
{
    pass P0
    {
        VertexShader = compile vs_3_0 LitVS();
        PixelShader = compile ps_3_0 PointLightsFastPS();
    }
}

technique SpotLightsFast
{
    pass P0
    {
        VertexShader = compile vs_3_0 LitVS();
        PixelShader = compile ps_3_0 SpotLightsFastPS();
    }
}

technique SpotLights
{
    pass P0
    {
        VertexShader = compile vs_3_0 LitVS();
        PixelShader = compile ps_3_0 SpotLightsPS();
    }
}

technique Unlit
{
    pass P0
    {
        VertexShader = compile vs_3_0 ShadowVS();
        PixelShader = compile ps_3_0 UnlitPS();
    }
}

technique SunShafts
{
    pass P0
    {
        VertexShader = compile vs_3_0 ShadowVS();
        PixelShader = compile ps_3_0 SunShaftsPS();
    }
}

technique ShadowDilate
{
    pass P0
    {
        VertexShader = compile vs_3_0 ScreenVS();
        PixelShader = compile ps_3_0 ShadowDilatePS();
    }
}

technique Composite
{
    pass P0
    {
        VertexShader = compile vs_3_0 ScreenVS();
        PixelShader = compile ps_3_0 CompositePS();
    }
}
