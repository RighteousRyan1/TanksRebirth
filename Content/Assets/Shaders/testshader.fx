/*//float oTime;
//float oDistortionFactor;
float2 oPosition;
float oPower;

sampler TexSampler : register(s0);
float lum(float3 color)
{
    return dot(color, float3(0.299f, 0.587f, 0.114f));
}
float4 testShader(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    //coords.x += oDistortionFactor * cosh(oTime + coords.y + coords.x);
    //coords.y -= oDistortionFactor * sinh(oTime + coords.y + coords.x); page turn

    //coords.x += oDistortionFactor * cosh(oTime + coords.y + coords.x);
    //coords.y -= oDistortionFactor * sinh(oTime + coords.y + coords.x);

    //coords.x += oDistortionFactor * sin(oTime + coords.y * 75 + coords.x / 50);
    //coords.y -= oDistortionFactor * cos(oTime + coords.y * 25 + coords.x * 50);

    float4 color = tex2D(TexSampler, coords);

    //float3 offset = float3(coords.Y * 0.5, 0, 0) + oTime * 1.5;

    //color += float4(offset, 0);

    float lerp = distance(oPosition, coords) / oPower;
    float4 newColor = float4(1, 0, 0, 1);

    color.rgba -= lerp;


    //if (lum(color.rgb) > 0.01)
        //color *= float4(cos(coords.x + oTime), sin(coords.y + oTime), tanh(coords.x * coords.y * oTime), 1);
        //color *= float4(abs(sin(oTime)), abs(cos(oTime)), abs(tan(oTime)), 1);


    return color;
}
technique TestShader
{
    pass TestShaderPass
    {
        PixelShader = compile ps_2_0 testShader();
    }
}*/
//float2 oPosition;
//float oPower;
float oTime;
float oBend;

bool oLantern;
float2 oPosition;
float oPower;

sampler TexSampler : register(s0);
float lum(float3 color)
{
    return dot(color, float3(0.299f, 0.587f, 0.114f));
}
float invlerp(float b, float e, float v)
{
    return (v - b) / (e - b);
}

float random(float2 range)
{
    return frac(sin(dot(range, float2(12.9898, 78.233))) * 43758.5453123);
}

float4 testShader(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    //coords.y /= coords.x * oBend;
    // coords.x *= invlerp(0, 1, oBend); //refract(coords.x, coords.y, oBend);
    
    float4 color = tex2D(TexSampler, coords);
    
    if (oLantern)
    {
        float lerp = distance(oPosition, coords) / oPower;
        // float4 newColor = float4(1, 0, 0, 1);
        
        color.rgba -= lerp;
    }
    return color;
}

technique TestShader
{
    pass TestShaderPass
    {
        PixelShader = compile ps_2_0 testShader();
    }
}