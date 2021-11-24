float oDistortion;
float oVariability;
float oGlobalTime;

sampler TexSampler : register(s0);

float4 PixelShaderFunction(float2 coords : TEXCOORD0) : COLOR0
{
    float4 color = tex2D(TexSampler, coords);
    
    if (color.rgb == float3(0, 0, 0))
    {
        
    }
    
    return color;
}
technique UIDistortion
{
    pass UIDistortionPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}