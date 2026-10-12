// one pass per direction
float2 oStep;
// multiplies the color, so the second pass can brighten the result
float oBrightness;

sampler TexSampler : register(s0);

float4 main(float4 color : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
    float4 sum = tex2D(TexSampler, uv) * 0.2042;
    sum += (tex2D(TexSampler, uv + oStep) + tex2D(TexSampler, uv - oStep)) * 0.1802;
    sum += (tex2D(TexSampler, uv + oStep * 2.0) + tex2D(TexSampler, uv - oStep * 2.0)) * 0.1238;
    sum += (tex2D(TexSampler, uv + oStep * 3.0) + tex2D(TexSampler, uv - oStep * 3.0)) * 0.0663;
    sum += (tex2D(TexSampler, uv + oStep * 4.0) + tex2D(TexSampler, uv - oStep * 4.0)) * 0.0276;
    sum.rgb *= oBrightness;
    return float4(sum.rgb, 1.0);
}

technique GaussianBlur
{
    pass BlurPass
    {
        PixelShader = compile ps_3_0 main();
    }
}
