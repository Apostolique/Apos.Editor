#if OPENGL
#define SV_POSITION POSITION
#define VS_SHADERMODEL vs_3_0
#define PS_SHADERMODEL ps_3_0
#else
#define VS_SHADERMODEL vs_4_0_level_9_1
#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

sampler TextureSampler : register(s0);
float line_size;
float2 grid_size;
float4x4 view_projection;
float4x4 tex_transform;
float ps;

struct VertexInput {
    float4 Position : POSITION0;
    float4 Color : COLOR0;
    float4 TexCoord : TEXCOORD0;
};
struct PixelInput {
    float4 Position : SV_Position0;
    float4 Color : COLOR0;
    float4 TexCoord : TEXCOORD0;
};

float mod(float x, float m) {
    if (m == 0) {
        return x;
    }
    return (x % m + m) % m;
}
float BoxSDF(float2 p, float2 b) {
    float2 d = abs(p) - b;
    return length(max(d, 0.0)) + min(max(d.x, d.y), 0.0);
}
float Onion(float d, float r) {
    return abs(d) - r;
}
float Antialias(float d, float size) {
    return lerp(1.0, 0.0, smoothstep(0.0, size, d));
}

PixelInput SpriteVertexShader(VertexInput v) {
    PixelInput Output;

    Output.Position = mul(v.Position, view_projection);
    Output.Color = v.Color;
    Output.TexCoord = mul(v.TexCoord, tex_transform);

    return Output;
}

float4 SpritePixelShader(PixelInput p): COLOR0 {
    float2 size = grid_size;
    float2 halfSize = size * 0.5;
    float aa = ps * 1.5;
    float lineSize = line_size * ps / 4.0;

    float x = p.TexCoord.x;
    float y = p.TexCoord.y;
    float d = BoxSDF(float2(mod(x, size.x) - halfSize.x, mod(y, size.y) - halfSize.y), halfSize) + lineSize;
    d = Onion(d, lineSize);

    float4 c = p.Color * Antialias(d, aa);
    return c;
}

technique SpriteBatch {
    pass {
        VertexShader = compile VS_SHADERMODEL SpriteVertexShader();
        PixelShader = compile PS_SHADERMODEL SpritePixelShader();
    }
}
