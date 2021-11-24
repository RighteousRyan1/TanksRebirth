float oVariability;
float oGlobalTime;
float4 oColor;

sampler TexSampler : register(s0);

float4 whitePartShader(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float localY = frac((coords.y * 32) / 32);
    float4 color = tex2D(TexSampler, coords);

    if (!any(color.r))
    {
        float3 rainbow = (color.r + color.g + color.b) * 0.15 + localY + oGlobalTime;
        rainbow += float3(0.111, 0.333, 0.666);
        rainbow = frac(rainbow) * 2 - 1;

        color.rgb += rainbow * 1;

        // return oColor;

        return color * color.a * sampleColor;
    }
    
    return color;
}
technique UIDistortion
{
    pass UIDistortionPass
    {
        PixelShader = compile ps_2_0 whitePartShader();
    }
}