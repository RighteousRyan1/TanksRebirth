float oGlobalTime;
float3 oColor;
float oSpeed;
float oSpacing;

sampler TexSampler : register(s0);
float lum(float3 color)
{
    return dot(color, float3(0.299f, 0.587f, 0.114f));
}
float4 whitePartShader(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float4 color = tex2D(TexSampler, coords);
    if (color.a && lum(color.rgb) <= 0.01)
    {
        float3 defaultColor = oColor;
        float3 colorShift = float3(0.0, 0.79, 0.0);

        float offset = cos(coords.y * oSpacing + oGlobalTime * oSpeed) * 0.5 + 0.5;
        float3 col = defaultColor + colorShift * offset;

        return float4(col, 1);
    }
    return color;
}
technique MouseShader
{
    pass MouseShaderPass
    {
        PixelShader = compile ps_2_0 whitePartShader();
    }
}