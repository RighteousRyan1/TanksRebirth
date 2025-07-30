float oTime;
float oBend;
int oLanternCount;
float2 oLanternPositions[8];
float oLanternPowers[8];
float3 oLanternColors[8];
float oMaxDark = 0.01;

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

float4 lantern_ps(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    /*float4 color = tex2D(TexSampler, coords);
    
    if (oLanternCount > 0)
    {
        float lightAmount = 0.0;
        
        // Each lantern contributes light independently
        for (int i = 0; i < oLanternCount && i < 16; i++)
        {
            float dist = distance(oLanternPositions[i], coords);
            
            // Calculate light falloff for this lantern
            // Closer = more light, further = less light
            float lanternLight = saturate(1.0 - (dist / oLanternPowers[i]));
            
            // Add this lantern's light contribution
            lightAmount += lanternLight;
        }
        
        // Clamp total light to prevent over-brightening
        lightAmount = saturate(lightAmount);
        
        // Apply lighting: areas with light stay bright, areas without get dark
        // Instead of subtracting darkness, we multiply by light
        color.rgb = lerp(color.rgb * oMaxDark, color.rgb, lightAmount); // 0.1 = darkness level
    }*/
    float4 color = tex2D(TexSampler, coords);
    if (oLanternCount > 0)
    {
        float3 totalLight = float3(0.0, 0.0, 0.0);
        
        // Each lantern contributes colored light independently
        for (int i = 0; i < oLanternCount && i < 16; i++)
        {
            float dist = distance(oLanternPositions[i], coords);
            
            // Calculate light falloff for this lantern
            float lanternStrength = saturate(1.0 - (dist / oLanternPowers[i]));
            
            // Add this lantern's colored light contribution
            totalLight += oLanternColors[i] * lanternStrength;
        }
        
        // Calculate overall light intensity
        float lightIntensity = saturate(length(totalLight));
        
        // Normalize the light color
        float3 lightColor = lightIntensity > 0.0 ? normalize(totalLight) : float3(1.0, 1.0, 1.0);
        
        // Apply colored lighting
        float3 darkColor = color.rgb * oMaxDark;
        float3 litColor = color.rgb * lightColor;
        
        color.rgb = lerp(darkColor, litColor, lightIntensity);
    }
    
    return color;
}

technique Lantern
{
    pass LanternPass
    {
        PixelShader = compile ps_3_0 lantern_ps();
    }
}