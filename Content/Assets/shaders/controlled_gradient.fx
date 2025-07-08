
sampler TexSampler : register(s0);

float4 oTopColor : register(c0);
float4 oBottomColor : register(c1);
float oAngle : register(c2); // Angle in radians (0 = vertical, PI/2 = horizontal)
float oCenter : register(c3); // Center position (0.0 to 1.0)

// Pixel Shader
float4 main(float2 coords : TEXCOORD0) : COLOR0
{
    // Calculate gradient direction based on angle
    float2 gradientDir = float2(sin(oAngle), cos(oAngle));
    
    float4 color = tex2D(TexSampler, coords);
    
    // Calculate the gradient position along the gradient direction
    // This projects the UV coordinate onto the gradient direction
    float gradientPos = dot(coords - 0.5, gradientDir) + 0.5;
    
    // Apply gradient center offset
    // Transform the gradient position based on the center parameter
    float adjustedPos;
    if (gradientPos <= oCenter)
    {
        // Map [0, GradientCenter] to [0, 0.5]
        adjustedPos = (gradientPos / oCenter) * 0.5;
    }
    else
    {
        // Map [GradientCenter, 1] to [0.5, 1]
        adjustedPos = 0.5 + ((gradientPos - oCenter) / (1.0 - oCenter)) * 0.5;
    }
    
    // Clamp to ensure we stay within bounds
    adjustedPos = saturate(adjustedPos);
    
    // float4 gradientColor = lerp(oTopColor, oBottomColor, adjustedPos);
    
    if (color.a < 0.01)
        return float4(0, 0, 0, 0);
    
    // Interpolate between top and bottom colors
    color = lerp(oTopColor, oBottomColor, adjustedPos);
    
    return color; //float4(gradientColor.rgb, color.a * gradientColor.a);

}
technique Gradient
{
    pass GradientPass
    {
        PixelShader = compile ps_2_0 main();
    }
}