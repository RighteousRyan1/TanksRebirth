float2 oResolution;
float oBlurFactor;

sampler TexSampler : register(s0);

float4 doBlur(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = coords / oResolution.xy;

    float4 Color = tex2D(TexSampler, uv);

    float PiOver2 = 6.28318530718;
    
    float Directions = 16.0;
    float Quality = 3.0;
   
    float2 Radius = oBlurFactor / oResolution.xy;

    for (float d = 0.0; d < PiOver2; d += PiOver2 / Directions)
    {
        for (float i = 1.0 / Quality; i <= 1.0; i += 1.0 / Quality)
        {
            Color += tex2D(TexSampler, uv + float2(cos(d), sin(d)) * Radius * i);
        }
    }
    
    Color /= Quality * Directions - 15.0;

    return Color;
}
technique GaussianBlur
{
    pass BlurPass
    {
        PixelShader = compile ps_3_0 doBlur();
    }
}