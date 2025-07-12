float oSoftness;
float oRounding;

sampler TexSampler : register(s0);

// unsigned round box
float udRoundBox(float3 p, float3 b, float r)
{
    return length(max(abs(p) - b, 0.0)) - r;
}

// substracts shape d1 from shape d2
float opS(float d1, float d2)
{
    return max(-d1, d2);
}

// to get the border of a udRoundBox, simply substract a smaller udRoundBox !
float udRoundBoxBorder(float3 p, float3 b, float r, float borderFactor)
{
    return opS(udRoundBox(p, b * borderFactor, r), udRoundBox(p, b, r));
}

float4 main(float2 coords : TEXCOORD0) : COLOR0
{
    float4 color = tex2D(TexSampler, coords);
    // box setup
    float3 boxPosition = float3(0.5, 0.5, 0.0);
    float3 boxSize = float3(0.1, 0.1, 0.0);
    float boxRounding = 0.05;
    
    // render the box
    float3 curPosition = float3(coords, 0.0);
    float dist = udRoundBoxBorder(curPosition - boxPosition, boxSize, boxRounding, 0.9);
    float THRESHOLD = 0.0001;
    if (dist <= THRESHOLD)
        color.rgb = float3(1.0, 0.0, 0.0);
    
    return color;
}
technique MouseShader
{
    pass MouseShaderPass
    {
        PixelShader = compile ps_3_0 main();
    }
}