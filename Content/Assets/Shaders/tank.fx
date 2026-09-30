float4x4 WorldView;
float4x4 NormalMatrix;
float4x4 Projection;
float3 LightPosition;
float HasEnvironment;
float EnvironmentStrength;
float Opacity;
texture DiffuseTexture;
texture EnvironmentTexture;

sampler DiffuseSampler = sampler_state
{
    Texture = <DiffuseTexture>;
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Linear;
    AddressU = Wrap;
    AddressV = Wrap;
};

sampler EnvironmentSampler = sampler_state
{
    Texture = <EnvironmentTexture>;
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Linear;
    AddressU = Clamp;
    AddressV = Clamp;
};

struct VertexInput
{
    float4 Position : POSITION0;
    float3 Normal : NORMAL0;
    float4 Color : COLOR0;
    float2 Uv : TEXCOORD0;
};

struct PixelInput
{
    float4 Position : POSITION0;
    float4 Color : COLOR0;
    float2 Uv : TEXCOORD0;
    float2 EnvironmentUv : TEXCOORD1;
};

PixelInput Transform(VertexInput input)
{
    PixelInput output;
    float4 position = mul(input.Position, WorldView);
    float3 normal = normalize(mul(float4(input.Normal, 0), NormalMatrix).xyz);
    float diffuse = max(0, dot(normal, normalize(LightPosition - position.xyz)));

    // Match the port's vertex light/material quantization before interpolation.
    int light = min(255, 100 + (int)round(255 * diffuse));
    int3 material = (int3)round(input.Color.rgb * 255);
    output.Color = float4((material * (light + (light / 128))) / 256 / 255.0, input.Color.a);
    output.Position = mul(position, Projection);
    output.Uv = input.Uv;
    output.EnvironmentUv = float2(0.5 * normal.x + 0.5, 0.5 - 0.5 * normal.y);
    return output;
}

int4 MultiplyTev(int4 value, int4 weight)
{
    return (value * (weight + (weight / 128)) + 128) / 256;
}

float4 Shade(PixelInput input) : COLOR0
{
    int4 sample = (int4)floor(saturate(tex2D(DiffuseSampler, input.Uv)) * 255 + 0.5);
    int4 raster = (int4)floor(saturate(input.Color) * 255 + 0.5);
    int4 color = MultiplyTev(sample, raster);
    if (HasEnvironment > 0.5)
    {
        int4 environment = (int4)floor(saturate(tex2D(EnvironmentSampler, input.EnvironmentUv)) * 255 + 0.5);
        int3 reflection = MultiplyTev(environment, int4(177, 165, 129, 0)).rgb;
        color.rgb += (int3)round(reflection * EnvironmentStrength);
    }
    color.a = (int)round(color.a * Opacity);
    return clamp(color, 0, 255) / 255.0;
}

technique Tank
{
    pass P0
    {
        VertexShader = compile vs_3_0 Transform();
        PixelShader = compile ps_3_0 Shade();
    }
}
