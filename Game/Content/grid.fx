#if OPENGL
#define SV_POSITION POSITION
#define VS_SHADERMODEL vs_3_0
#define PS_SHADERMODEL ps_3_0
#else
#define VS_SHADERMODEL vs_4_0_level_9_1
#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

sampler TextureSampler : register(s0);
float2 line_size;
float2 grid_size;
float4x4 view_projection;
float4x4 tex_transform;

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

PixelInput SpriteVertexShader(VertexInput v) {
    PixelInput Output;

    Output.Position = mul(v.Position, view_projection);
    Output.Color = v.Color;
    Output.TexCoord = mul(v.TexCoord, tex_transform);

    return Output;
}

float4 SpritePixelShader(PixelInput p): COLOR0 {
    float4 color;
    float x = p.TexCoord.x;
    float y = p.TexCoord.y;

    float lineWidth = line_size.x;
    float lineHeight = line_size.y;

    float width = grid_size.x;
    float height = grid_size.y;

    if (mod(x + (lineWidth / 2), width) <= lineWidth || mod(y + (lineHeight / 2), height) <= lineHeight) {
        color = p.Color;
    } else {
        discard;
    }

    return color;
}

technique SpriteBatch {
    pass {
        VertexShader = compile VS_SHADERMODEL SpriteVertexShader();
        PixelShader = compile PS_SHADERMODEL SpritePixelShader();
    }
}
