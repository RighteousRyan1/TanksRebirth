float4x4 WorldView;
float4x4 NormalMatrix;
float4x4 Projection;
float3 LightDirection;
float Opacity;
float HasEnvironment;
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
    float2 Uv : TEXCOORD0;
};

struct VertexOutput
{
    float4 Position : POSITION0;
    float3 Normal : TEXCOORD1;
    float2 Uv : TEXCOORD0;
    float2 EnvironmentUv : TEXCOORD2;
};

VertexOutput Transform(VertexInput input)
{
    VertexOutput output;
    float4 viewPosition = mul(input.Position, WorldView);
    output.Position = mul(viewPosition, Projection);
    float3 normal = normalize(mul(float4(input.Normal, 0), NormalMatrix).xyz);
    output.Normal = normal;
    output.Uv = input.Uv;
    output.EnvironmentUv = float2(0.5 + 0.5 * normal.x, 0.5 - 0.5 * normal.y);
    return output;
}

float4 Shade(VertexOutput input) : COLOR0
{
    float diffuse = max(0, dot(normalize(input.Normal), normalize(LightDirection)));
    float lighting = saturate(100.0 / 255.0 + diffuse);
    float4 color = tex2D(DiffuseSampler, input.Uv);
    float3 result = color.rgb * lighting;
    if (HasEnvironment > 0.5)
    {
        float3 environment = tex2D(EnvironmentSampler, input.EnvironmentUv).rgb;
        result += environment * float3(177.0, 165.0, 129.0) / 255.0;
    }
    return float4(saturate(result), color.a * Opacity);
}

technique Tank
{
    pass P0
    {
        VertexShader = compile vs_3_0 Transform();
        PixelShader = compile ps_3_0 Shade();
    }
}