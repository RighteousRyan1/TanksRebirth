float3 oColor;

sampler TexSampler : register(s0);
float lum(float3 color)
{
    return dot(color, float3(0.299f, 0.587f, 0.114f));
}
float4 changeBlackHue(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float4 color = tex2D(TexSampler, coords);
    if (color.a && lum(color.rgb) <= 0.01)
    {
        return float4(oColor, 1);
    }
    return color;
}
technique ApplyBlackHue
{
    pass ApplyBlackHuePass
    {
        PixelShader = compile ps_2_0 changeBlackHue();
    }
}