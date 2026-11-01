float4x4 World;
float4x4 ViewProjection;

float  Time;            // seconds
float  Intensity;       // 0 = dry, 1 = full storm
float  CellSize;        // world size of one bead cell
float  BeadDensity;     // share of bead cells holding a bead at full intensity
float  DripDensity;     // share of drip columns running at full intensity
float  DripSpeed;       // how fast drips fall, in drip cells per second
float  Refraction;      // how strongly water bends the view behind it
float  FogAmount;       // condensation strength at full intensity
float3 FogColor;
float4 GlassTint;       // rgb = tint, a = how much of it is mixed over the result

// the scene behind the glass, and a small blurry copy of it for the condensation
texture SceneTexture;
sampler SceneSampler = sampler_state
{
    Texture = <SceneTexture>;
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = None;
    AddressU = Clamp;
    AddressV = Clamp;
};

texture FogTexture;
sampler FogSampler = sampler_state
{
    Texture = <FogTexture>;
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = None;
    AddressU = Clamp;
    AddressV = Clamp;
};

float2 SceneTexelSize;  // 1 / size of the scene copy

#define TAU 6.2831853
#define DRIP_CELLS 3.0      // a drip column is this many bead cells wide
#define DRIP_PERIOD 7.0     // and a drip falls through this many drip cells before the next one starts

struct VSInput
{
    float4 Position : POSITION0;
    float3 Normal   : NORMAL0;
};

struct VSOutput
{
    float4 Position : POSITION0;
    float3 WorldPos : TEXCOORD0;
    float3 Normal   : TEXCOORD1;
    float4 Clip     : TEXCOORD2;
};

struct PSInput
{
    float3 WorldPos : TEXCOORD0;
    float3 Normal   : TEXCOORD1;
    float4 Clip     : TEXCOORD2;
};

VSOutput RainVS(VSInput input)
{
    VSOutput output;
    float4 worldPos = mul(input.Position, World);
    output.Position = mul(worldPos, ViewProjection);
    output.WorldPos = worldPos.xyz;
    output.Normal = mul(input.Normal, (float3x3)World);
    output.Clip = output.Position;
    return output;
}

// used for randomizing somewhat
float Hash12(float2 p)
{
    float3 p3 = frac(p.xyx * 0.1031);
    p3 += dot(p3, p3.yzx + 33.33);
    return frac((p3.x + p3.y) * p3.z);
}

float2 Hash22(float2 p)
{
    float3 p3 = frac(p.xyx * float3(0.1031, 0.1030, 0.0973));
    p3 += dot(p3, p3.yzx + 33.33);
    return frac((p3.xx + p3.yz) * p3.zy);
}

// beads that cling to glass
void Beads(float2 p, inout float2 offset, inout float mask)
{
    float2 cell = floor(p);
    float2 f = frac(p) - 0.5;

    float h = Hash12(cell);
    float cycle = Time * (0.04 + 0.05 * h) + h * 7.0;
    float2 jitter = (Hash22(cell + floor(cycle)) - 0.5) * 0.45;
    float life = frac(cycle);

    float exists = step(Hash12(cell + floor(cycle) * 1.37 + 5.3), BeadDensity * Intensity);
    float radius = (0.08 + 0.14 * frac(h * 17.3)) * smoothstep(0.0, 0.15, life) * (1.0 - smoothstep(0.75, 1.0, life));

    float2 d = f - jitter;
    
    // beads sag a little under their own weight
    d.y *= 1.15;
    float m = smoothstep(radius, radius * 0.55, length(d)) * exists;

    // a bead is a tiny lens, so the view through it is flipped around its center
    offset -= d / max(radius, 1e-3) * m * 0.6;
    mask = max(mask, m);
}

// it kinda wiggles a bit
float DripPathX(float y, float h)
{
    return 0.5 + (h - 0.5) * 0.4 + sin(y * 1.7 + h * TAU) * 0.08;
}

// drips run down in jerky bursts
void Drips(float2 p, inout float2 offset, inout float mask, inout float clear)
{
    float2 q = p / DRIP_CELLS;
    float column = floor(q.x);
    float tile = floor(q.y / DRIP_PERIOD);
    float x = frac(q.x);
    float y = q.y - tile * DRIP_PERIOD;

    float h = Hash12(float2(column, tile));
    float fall = Time * DripSpeed * (0.6 + 0.8 * h) / DRIP_PERIOD + h * 3.0;
    float run = floor(fall);
    float progress = frac(fall);
    // speeds up and slows down a few times on the way down
    progress -= sin(progress * TAU * 3.0) / (TAU * 3.0) * 0.75;

    float active = step(Hash12(float2(column + run * 3.1, tile + 0.7)), DripDensity * Intensity);
    float hr = Hash12(float2(column, tile + run));
    float headY = DRIP_PERIOD * (1.0 - progress);

    // head
    float2 d = float2(x - DripPathX(headY, hr), y - headY) / float2(0.1, 0.14);
    float head = smoothstep(1.0, 0.55, length(d)) * active;

    // trail above the head, drying out the further it is from the head
    float above = y - headY;
    float behind = step(0.0, above) * exp(-above * 0.35) * active;
    float pathDist = x - DripPathX(y, hr);

    // droplets left along the trail
    float2 droplet = float2(pathDist, (frac(y * 1.4 + hr) - 0.5) / 1.4) / 0.045;
    float droplets = smoothstep(1.0, 0.45, length(droplet)) * behind * step(0.15, above);

    offset -= d * head * 1.2 + droplet * droplets * 0.4;
    mask = max(mask, max(head, droplets));
    clear = max(clear, max(head, smoothstep(0.12, 0.03, abs(pathDist)) * behind));
}

float4 RainPS(PSInput input) : COLOR0
{
    float3 n = normalize(input.Normal);
    // horizontal direction along the glass
    float3 side = normalize(cross(float3(0.0, 1.0, 0.0), n) + float3(1e-4, 0.0, 1e-4));
    float2 p = float2(dot(input.WorldPos, side), input.WorldPos.y) / CellSize;

    float2 offset = 0.0;
    float mask = 0.0;
    float clear = 0.0;
    Beads(p, offset, mask);
    Drips(p, offset, mask, clear);

    // bead cells to screen pixels, so the bend scales with how big the water looks on screen
    float pixelsPerCell = 1.0 / max(max(length(ddx(p)), length(ddy(p))), 1e-4);
    float2 screenUV = input.Clip.xy / input.Clip.w * float2(0.5, -0.5) + 0.5;
    float2 bend = offset * float2(1.0, -1.0) * pixelsPerCell * Refraction * SceneTexelSize;

    float3 sharp = tex2D(SceneSampler, screenUV + bend).rgb;

    // a soft blurred view, condensation
    float3 fogged = lerp(tex2D(FogSampler, screenUV).rgb, FogColor, 0.3);
    float fog = FogAmount * Intensity * (1.0 - max(clear, mask));
    float3 color = lerp(sharp, fogged, fog);

    // water edges darken where they curve away and catch a little light from above
    float edge = mask * (1.0 - mask) * 4.0;
    color *= 1.0 - edge * 0.2;
    color += mask * saturate(-offset.y) * 0.08;

    color = lerp(color, GlassTint.rgb, GlassTint.a);
    float4 result;
    result.rgb = color;
    result.a = 1.0;
    return result;
}

technique RainyWindow
{
    pass P0
    {
        VertexShader = compile vs_3_0 RainVS();
        PixelShader = compile ps_3_0 RainPS();
    }
}
