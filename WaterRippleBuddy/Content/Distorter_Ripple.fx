// ============================================================================
// Distorter_Ripple.fx
// Water ripple distortion shader for WaterRippleBuddy.
//
// Supports up to MAX_DROPLETS simultaneous ripples in a single pass.
// Each droplet is a sinusoidal wave that radiates outward, attenuates with
// distance and age, and applies:
//   - Refraction : UV offset to distort the scene beneath the wave
//   - Reflection : color tint at wave crests
//
// This file is compiled by the consuming game's content pipeline (MGCB).
// Add it to your Content/Content.mgcb with an EffectImporter/EffectProcessor.
// ============================================================================

#if OPENGL
    #define SV_POSITION POSITION
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0_level_9_1
    #define PS_SHADERMODEL ps_4_0_level_9_1
#endif

#define MAX_DROPLETS 16

// ── Droplet data ─────────────────────────────────────────────────────────────
// Each element: xy = normalized screen position [0,1],
//               z  = normalized age [0,1]  (0 = just spawned, 1 = expired),
//               w  = unused (reserved)
float4 DropletData[MAX_DROPLETS];
float  DropletCount;        // passed as float to avoid int-uniform driver quirks

// ── Wave parameters ───────────────────────────────────────────────────────────
float WaveSpeed;            // how fast the wave pattern travels outward
float WaveFrequency;        // controls ring spacing (radians per UV unit)
float RefractionStrength;   // UV displacement scale
float ReflectionStrength;   // reflection tint blend factor
float3 ReflectionColor;     // color blended in at wave crests
float AspectRatio;          // viewport width / height (corrects circular waves)

// ── Scene texture (bound by SpriteBatch) ─────────────────────────────────────
Texture2D SceneTexture;
sampler2D SceneSampler = sampler_state
{
    Texture   = <SceneTexture>;
    AddressU  = Clamp;
    AddressV  = Clamp;
    MagFilter = Linear;
    MinFilter = Linear;
    MipFilter = Linear;
};

// ── Pixel shader input (matches SpriteBatch vertex output) ───────────────────
struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color    : COLOR0;
    float2 TexCoord : TEXCOORD0;
};

// ── Pixel shader ─────────────────────────────────────────────────────────────
float4 PS(VertexShaderOutput input) : COLOR
{
    float2 uv = input.TexCoord;

    float2 totalDisplacement = float2(0.0, 0.0);
    float  totalIntensity    = 0.0;

    int count = (int)DropletCount;

    for (int i = 0; i < count; i++)
    {
        float2 dropPos = DropletData[i].xy;
        float  age     = DropletData[i].z;   // [0, 1]

        // ── Distance (aspect-corrected so ripples are circular) ──────────────
        float2 delta = uv - dropPos;
        delta.x *= AspectRatio;
        float dist = length(delta);

        // ── Traveling wave ───────────────────────────────────────────────────
        // sin(dist * freq - age * speed * freq) produces a ring that moves
        // outward as age increases.  Two decay terms:
        //   exp(-dist * k)  →  waves attenuate as they spread
        //   exp(-age  * k)  →  waves fade as they age
        float phase = dist * WaveFrequency - age * WaveSpeed * WaveFrequency;
        float wave  = sin(phase)
                    * exp(-dist * 4.0)
                    * exp(-age  * 4.0);

        // ── Radial displacement direction ────────────────────────────────────
        float2 dir = float2(0.0, 0.0);
        if (dist > 0.001)
        {
            dir   = normalize(delta);
            dir.x /= AspectRatio;   // un-correct back to UV space
        }

        totalDisplacement += dir * wave;
        totalIntensity    += abs(wave);
    }

    // ── Refraction ───────────────────────────────────────────────────────────
    float2 refractedUV = uv + totalDisplacement * RefractionStrength;
    refractedUV = clamp(refractedUV, float2(0.0, 0.0), float2(1.0, 1.0));

    float4 sceneColor = tex2D(SceneSampler, refractedUV);

    // ── Reflection ───────────────────────────────────────────────────────────
    float  reflFactor = saturate(totalIntensity * ReflectionStrength);
    float3 finalRGB   = lerp(sceneColor.rgb, ReflectionColor, reflFactor);

    return float4(finalRGB, sceneColor.a) * input.Color;
}

// ── Technique ────────────────────────────────────────────────────────────────
technique WaterRipple
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL PS();
    }
}
