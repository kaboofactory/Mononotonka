#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

Texture2D SpriteTexture;
sampler2D SpriteTextureSampler = sampler_state
{
	Texture = <SpriteTexture>;
};

float FilterType; // 0:None, 1:Grey, 2:Sepia, 3:ScanLine, 4:Mosaic, 5:Blur
float Amount;     // Intensity or Params
float Time;       // For animation if needed
float2 Resolution; // Screen Resolution

struct VertexShaderOutput
{
	float4 Position : SV_POSITION;
	float4 Color : COLOR0;
	float2 TextureCoordinates : TEXCOORD0;
};

float4 MainPS(VertexShaderOutput input) : COLOR
{
	float4 color = tex2D(SpriteTextureSampler, input.TextureCoordinates) * input.Color;

    if (FilterType == 1) // Greyscale
    {
        float gray = dot(color.rgb, float3(0.299, 0.587, 0.114));
        color.rgb = lerp(color.rgb, float3(gray, gray, gray), Amount);
    }
    else if (FilterType == 2) // Sepia
    {
         float3 sepia = float3(
            dot(color.rgb, float3(0.393, 0.769, 0.189)),
            dot(color.rgb, float3(0.349, 0.686, 0.168)),
            dot(color.rgb, float3(0.272, 0.534, 0.131))
        );
        color.rgb = lerp(color.rgb, sepia, Amount);
    }
    else if (FilterType == 3) // ScanLine
    {
        float pos = input.TextureCoordinates.y * Resolution.y;
        if (fmod(pos, 2.0) < 1.0)
        {
            color.rgb *= (1.0 - Amount);
        }
    }
    else if (FilterType == 4) // Mosaic
    {
        if (Amount > 0)
        {
            float dx = Amount * (1.0 / Resolution.x);
            float dy = Amount * (1.0 / Resolution.y);
            float2 coord = float2(
                floor(input.TextureCoordinates.x / dx) * dx,
                floor(input.TextureCoordinates.y / dy) * dy
            );
            color = tex2D(SpriteTextureSampler, coord) * input.Color;
        }
    }
    else if (FilterType == 5) // Blur
    {
        float2 uv = input.TextureCoordinates;
        float2 off = float2(1.0/Resolution.x, 1.0/Resolution.y) * Amount;
        float4 c = float4(0,0,0,0);
        
        // 3x3 Box Blur
        c += tex2D(SpriteTextureSampler, uv + float2(-off.x, -off.y));
        c += tex2D(SpriteTextureSampler, uv + float2(0,      -off.y));
        c += tex2D(SpriteTextureSampler, uv + float2(off.x,  -off.y));
        
        c += tex2D(SpriteTextureSampler, uv + float2(-off.x, 0));
        c += tex2D(SpriteTextureSampler, uv);
        c += tex2D(SpriteTextureSampler, uv + float2(off.x,  0));
        
        c += tex2D(SpriteTextureSampler, uv + float2(-off.x, off.y));
        c += tex2D(SpriteTextureSampler, uv + float2(0,      off.y));
        c += tex2D(SpriteTextureSampler, uv + float2(off.x,  off.y));
        
        color = (c / 9.0) * input.Color;
    }
    else if (FilterType == 6) // ChromaticAberration
    {
        float2 uv = input.TextureCoordinates;
        // R and B offset
        float2 offset = float2(0.01, 0.0) * Amount; // Adjust strength as needed
        
        float r = tex2D(SpriteTextureSampler, uv - offset).r;
        float g = tex2D(SpriteTextureSampler, uv).g;
        float b = tex2D(SpriteTextureSampler, uv + offset).b;
        float a = tex2D(SpriteTextureSampler, uv).a;
        
        color = float4(r, g, b, a) * input.Color;
    }
    else if (FilterType == 7) // Vignette
    {
        float2 uv = input.TextureCoordinates;
        float2 d = uv - 0.5;
        float len = length(d);
        
        // Amount 1.0 -> Radius -0.5 (Fully black)
        // Amount 0.0 -> Radius 1.0 (Fully clear)
        float radius = 1.0 - 1.5 * Amount;
        float softness = 0.5;
        
        float vignette = smoothstep(radius, radius + softness, len);
        
        color.rgb *= (1.0 - vignette);
    }
    else if (FilterType == 8) // Invert
    {
        // Simple Invert
        float3 inverted = 1.0 - color.rgb;
        color.rgb = lerp(color.rgb, inverted, Amount);
    }
    else if (FilterType == 9) // Distortion (Wave)
    {
        float2 uv = input.TextureCoordinates;
        
        // Simple Sine Wave
        // Amount controls strength
        float strength = 0.05 * Amount; // Max 5% uv shift
        float speed = 2.0;
        float frequency = 10.0;
        
        // Wave on X axis based on Y
        uv.x += sin(uv.y * frequency + Time * speed) * strength;
        
        // (Optional) Wave on Y axis based on X
        // uv.y += cos(uv.x * frequency + Time * speed) * strength;
        
        color = tex2D(SpriteTextureSampler, uv) * input.Color;
    }
    else if (FilterType == 10) // Noise
    {
        float2 uv = input.TextureCoordinates;
        
        // Random noise generator function (pseudo-random)
        float noise = frac(sin(dot(uv + Time, float2(12.9898, 78.233))) * 43758.5453);
        
        // Mix noise with original color
        // Amount controls noise intensity
        color = tex2D(SpriteTextureSampler, uv) * input.Color;
        color.rgb = lerp(color.rgb, float3(noise, noise, noise), Amount);
    }
    else if (FilterType == 11) // EdgeDetect
    {
        float2 uv = input.TextureCoordinates;
        float2 off = float2(1.0/Resolution.x, 1.0/Resolution.y);
        
        // Laplacian or Sobel-like simple difference
        float4 c = tex2D(SpriteTextureSampler, uv);
        float4 cL = tex2D(SpriteTextureSampler, uv - float2(off.x, 0));
        float4 cR = tex2D(SpriteTextureSampler, uv + float2(off.x, 0));
        float4 cU = tex2D(SpriteTextureSampler, uv - float2(0, off.y));
        float4 cD = tex2D(SpriteTextureSampler, uv + float2(0, off.y));
        
        float4 diff = abs(c - cL) + abs(c - cR) + abs(c - cU) + abs(c - cD);
        float edge = length(diff.rgb); // Brightness of difference
        
        // If edge is strong, show edge color (e.g., White or original amplified)
        // Amount = 1.0 -> Only Edges, Black Background
        // Amount = 0.0 -> Original Image (No effect logic handled by lerp if needed, but here simple switch)
        
        // To make it look like "Line Drawing":
        // Background White, Lines Black? OR Background Black, Lines White?
        // Let's go with: Dark Background, Glowing Lines.
        
        float3 edgeColor = float3(edge, edge, edge) * 2.0; // Boost brightness
        
        // Mix original and edge based on Amount
        // Amount 1.0: Pure Edge View
        color = lerp(c * input.Color, float4(edgeColor, c.a), Amount); 
    }
    
    else if (FilterType == 12) // Radial Blur
    {
        float2 uv = input.TextureCoordinates;
        float2 center = float2(0.5, 0.5);
        float2 toCenter = center - uv;
        float3 col = float3(0,0,0);
        float total = 0.0;
        
        // Sample along the vector to center
        int samples = 10;
        float strength = 0.05 * Amount; 
        
        [unroll]
        for(int t=0; t<=samples; t++)
        {
            float scale = 1.0 - strength * (float(t) / float(samples));
            col += tex2D(SpriteTextureSampler, uv + toCenter * (1.0 - scale)).rgb;
            total += 1.0;
        }
        
        color.rgb = col / total * input.Color.rgb;
        color.a = tex2D(SpriteTextureSampler, uv).a * input.Color.a;
    }
    else if (FilterType == 13) // Posterize
    {
        // Reduce colors
        float steps = max(2.0, 16.0 * (1.0 - Amount)); // Amount 0: 16 steps, Amount 1: 2 steps
        color.rgb = floor(color.rgb * steps) / steps;
    }
    else if (FilterType == 14) // Fish Eye
    {
        float2 uv = input.TextureCoordinates;
        float2 d = uv - 0.5;
        float r = length(d);
        float power = Amount * 0.5; // Distortion strength
            
        float2 uv2 = uv;
        
        // Simple Barrel Distortion
        // r = r (1 + k r^2) 
        // We want to map current uv to source uv
        
        // New r
        float nr = r * (1.0 + power * (r * r));
        
        // New UV
        uv2 = d * (nr / r) + 0.5;
        
        // Check bounds
        if (uv2.x < 0.0 || uv2.x > 1.0 || uv2.y < 0.0 || uv2.y > 1.0)
        {
            color = float4(0,0,0,1);
        }
        else
        {
            color = tex2D(SpriteTextureSampler, uv2) * input.Color;
        }
    }

    return color;
}

technique SpriteDrawing
{
	pass P0
	{
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};
