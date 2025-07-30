float oGlobalTime;
float3 oColor;
float oSpeed;
float oSpacing;
float oRotation;
sampler TexSampler : register(s0);

float lum(float3 color)
{
    return dot(color, float3(0.299f, 0.587f, 0.114f));
}

float2 rotate(float2 coords, float angle)
{
    float cosAngle = cos(angle);
    float sinAngle = sin(angle);
    return float2(
        coords.x * cosAngle - coords.y * sinAngle,
        coords.x * sinAngle + coords.y * cosAngle
    );
}

float4 main(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float4 color = tex2D(TexSampler, coords);
    if (color.a && lum(color.rgb) <= 0.01)
    {
        float3 defaultColor = oColor;
        float3 colorShift = float3(0.0, 0.79, 0.0);
        
        float2 centeredCoords = coords - 0.5;
        
        float2 rotatedCoords = rotate(centeredCoords, oRotation);
        
        rotatedCoords += 0.5;
        
        float offset = cos(rotatedCoords.y * oSpacing + oGlobalTime * oSpeed) * 0.5 + 0.5;
        float3 col = defaultColor + colorShift * offset;
        return float4(col, 1);
    }
    return color;
}

technique MouseShader
{
    pass MouseShaderPass
    {
        PixelShader = compile ps_2_0 main();
    }
}