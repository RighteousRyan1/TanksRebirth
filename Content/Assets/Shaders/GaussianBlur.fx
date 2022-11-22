bool oEnabledBlur : register(C0);
float2 oResolution : register(C1);
float oBlurFactor : register(C2);

sampler TexSampler : register(s0);

float4 main(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    if (oEnabledBlur == false)
        return tex2D(TexSampler, coords);
    //float4 color = tex2D(TexSampler, coords);
    
    float2 uv = coords / oResolution.xy;

    float4 color = tex2D(TexSampler, uv);
        
    float PiOver2 = 6.28318530718;

    float Directions = 16.0;
    float Quality = 16.0; // normally 3.0
    
    float2 Radius = oBlurFactor / oResolution.xy;
    
    for (float d = 0.0; d < PiOver2; d += PiOver2 / Directions)
    {
        for (float i = 1.0 / Quality; i <= 1.0; i += 1.0 / Quality)
        {
            color += tex2D(TexSampler, uv + float2(cos(d), sin(d)) * Radius * i);
        }
    }
    
    color /= Quality * Directions - 15.0;
        
    return color;
}
technique GaussianBlur
{
    pass BlurPass
    {
        PixelShader = compile ps_3_0 main();
    }
}