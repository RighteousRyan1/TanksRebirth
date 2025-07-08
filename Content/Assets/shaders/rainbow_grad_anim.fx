float oTime;
float oSpeed;
float oStrength;
float oAngle;
float oMinLum;

sampler TexSampler : register(s0);

float lum(float3 color)
{
    return dot(color, float3(0.299f, 0.587f, 0.114f));
}

float4 rainbowGrad(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float hue = coords.x * cos(radians(oAngle)) - coords.y * sin(radians(oAngle));
    hue = frac(hue + frac(oTime * oSpeed));
    float x = 1.0 - abs(((hue / (1.0 / 6.0)) % 2.0) - 1.0);
    float3 rainbow;
    if (hue < 1.0 / 6.0)
        rainbow = float3(1.0, x, 0.0);
    else if (hue < 1.0 / 3.0)
        rainbow = float3(x, 1.0, 0.0);
    else if (hue < 0.5)
        rainbow = float3(0.0, 1.0, x);
    else if (hue < 2.0 / 3.0)
        rainbow = float3(0.0, x, 1.0);
    else if (hue < 5.0 / 6.0)
        rainbow = float3(x, 0.0, 1.0);
    else
        rainbow = float3(1.0, 0.0, x);
    
    float4 color = tex2D(TexSampler, coords);
    if (color.a > 0)
        if (lum(color.rgb) > oMinLum)
            color = lerp(color, float4(rainbow, color.a), oStrength);
    //else if (color.a > 0)
        //color = float4(0, 0, 0, 255);
    
    return color;
}

technique AnimatedRainbow
{
    pass RainbowPass
    {
        PixelShader = compile ps_3_0 rainbowGrad();
    }
}