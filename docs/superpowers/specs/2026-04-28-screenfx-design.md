# ScreenFX — Design Spec
**Date:** 2026-04-28  
**Status:** Approved  
**Repo:** WaterRippleBuddy (alongside existing WaterRippleBuddy library)

---

## Overview

ScreenFX is a standalone MonoGame post-processing effects library for 2D fighting games. It provides a ping-pong render target chain that allows multiple simultaneous screen-space shader effects — force ripples, gravity waves, chromatic aberration, screen shake, heat haze, hit flash, and an anime super move effect.

Designed for Smash Brothers-style chaos: many characters triggering many effects simultaneously with no performance cliff.

---

## Goals

- Multiple effects composited correctly in a single frame (no overwrite problem)
- Fixed pass count per frame (one GPU pass per active effect *type*, not per instance)
- Simple game-facing API: one call to trigger, automatic lifetime management
- Same shader authoring pattern as WaterRippleBuddy (learned the hard way)
- Self-contained library — game projects reference it and copy shaders, nothing else required

---

## Non-Goals

- Not a general-purpose post-processing framework
- No runtime effect ordering / reordering (order is fixed by design)
- No support for 3D scenes
- No built-in editor or inspector

---

## Project Structure

Added to the existing `WaterRippleBuddy` repo and solution:

```
WaterRippleBuddy/  (repo root)
├── WaterRippleBuddy/               ← existing, unchanged
├── WaterRippleBuddy.Example/       ← existing, unchanged
│
├── ScreenFX/                       ← NEW library project
│   ├── Content/
│   │   ├── ForceRipple.fx
│   │   ├── GravityWave.fx
│   │   ├── ScreenShake.fx
│   │   ├── ChromaticAberration.fx
│   │   ├── HeatHaze.fx
│   │   ├── HitFlash.fx
│   │   └── AnimeSuper.fx
│   ├── Effects/
│   │   ├── IEffectLayer.cs
│   │   ├── EffectLayerBase.cs
│   │   ├── ForceRippleLayer.cs
│   │   ├── GravityWaveLayer.cs
│   │   ├── ScreenShakeLayer.cs
│   │   ├── ChromaticAberrationLayer.cs
│   │   ├── HeatHazeLayer.cs
│   │   ├── HitFlashLayer.cs
│   │   └── AnimeSuperLayer.cs
│   ├── ScreenFXComponent.cs
│   └── ScreenFX.csproj
│
├── ScreenFX.Example/               ← NEW example game project
│   ├── Content/
│   │   ├── Content.mgcb
│   │   ├── Braid_screenshot8.jpg   ← same test asset
│   │   ├── ForceRipple.fx          ← copies of all 7 shaders
│   │   ├── GravityWave.fx
│   │   ├── ScreenShake.fx
│   │   ├── ChromaticAberration.fx
│   │   ├── HeatHaze.fx
│   │   ├── HitFlash.fx
│   │   └── AnimeSuper.fx
│   ├── Game1.cs
│   ├── Program.cs
│   └── ScreenFX.Example.csproj
│
└── WaterRippleBuddy.sln            ← add both new projects
```

---

## Architecture

### Ping-Pong Per-Type Chain

Two internal `RenderTarget2D` buffers (`_ping`, `_pong`) alternate as read/write targets as each effect layer runs. At the end of the chain the final buffer is blitted to the back buffer. Overlay effects then draw additively on top.

```
_sceneTarget (game-owned)
    │
    ▼  [ForceRippleLayer   — handles all active force ripple instances in one pass]
   ping/pong
    ▼  [GravityWaveLayer   — handles all active gravity wave instances]
   ping/pong
    ▼  [ScreenShakeLayer   — single shared trauma value]
   ping/pong
    ▼  [ChromaticAberrationLayer]
   ping/pong
    ▼  [HeatHazeLayer      — handles all active heat haze zones]
   ping/pong
    ▼
  Back Buffer  ← blit result
    │
    ▼  [HitFlashLayer      — additive overlay, no scene read]
    ▼  [AnimeSuperLayer    — additive overlay, no scene read]
```

**Inactive layers are skipped at zero cost.** The chain only runs layers that have at least one active instance.

**Ping-pong swap:** After each layer writes, the read/write targets flip. The layer always reads from the "current source" and writes to the "current destination."

### Render Target Ownership

- `_sceneTarget` — owned and populated by the **game**, passed to `ScreenFXComponent` via `SceneTarget` property each frame (same pattern as WaterRippleBuddy).
- `_ping`, `_pong` — owned by `ScreenFXComponent`, created at `LoadContent`, sized to match back buffer. Disposed on `UnloadContent`.

---

## Core Types

### `ScreenFXComponent : DrawableGameComponent`

The single component the game adds to `Components`. Owns the ping-pong targets and orchestrates the layer chain.

```csharp
public sealed class ScreenFXComponent : DrawableGameComponent
{
    // Game sets this each frame before base.Draw() is called (same as WaterRippleBuddy)
    public RenderTarget2D? SceneTarget { get; set; }

    // Trigger methods — game calls these on gameplay events
    public void TriggerForceRipple(Vector2 screenPosition);
    public void TriggerGravityWave(float screenY);
    public void TriggerScreenShake(float intensity);          // trauma-based, auto-decays
    public void TriggerChromaticAberration(float intensity, float duration = 0.2f);
    public void TriggerHeatHaze(Vector2 screenPosition, float duration = -1f); // -1 = looping
    public void StopHeatHaze(Vector2 screenPosition);         // remove a specific zone
    public void TriggerHitFlash(Color? color = null);         // default white
    public void TriggerAnimeSuper();

    // Constructor
    public ScreenFXComponent(Game game, string shaderContentPath = "");
}
```

`shaderContentPath` is a prefix for shader asset names (e.g. `"Effects/"` if shaders live in a subfolder of Content).

**Draw order:** Set `DrawOrder = 100` so it runs after the game's own draw (same convention as WaterRippleBuddy).

**Integration pattern (game's Draw method):**
```csharp
protected override void Draw(GameTime gameTime)
{
    // 1. Render scene to off-screen target
    GraphicsDevice.SetRenderTarget(_sceneTarget);
    GraphicsDevice.Clear(Color.Black);
    _spriteBatch.Begin();
    _spriteBatch.Draw(_background, screenBounds, Color.White);
    _spriteBatch.End();

    // 2. Hand target to component, switch to back buffer
    _fx.SceneTarget = _sceneTarget;
    GraphicsDevice.SetRenderTarget(null);

    // 3. Component runs the full chain and outputs to back buffer
    base.Draw(gameTime);
}
```

---

### `IEffectLayer`

```csharp
public interface IEffectLayer
{
    bool IsActive { get; }         // true if any instances are running
    bool IsOverlay { get; }        // true = additive pass after back buffer blit, false = chain pass
    void LoadContent(ContentManager content, GraphicsDevice gd, string pathPrefix);
    void Update(float deltaSeconds);
    // For chain layers: reads from source, draws to whatever render target is currently active
    // (ScreenFXComponent calls SetRenderTarget(dest) before calling this)
    void Apply(SpriteBatch sb, RenderTarget2D source, Viewport viewport);
    // For overlay layers: draws additively onto whatever is currently on the back buffer
    void ApplyOverlay(SpriteBatch sb, Viewport viewport);
    void UnloadContent();
}
```

### `EffectLayerBase`

Abstract base providing shared instance-list management (add, update, expire) and the `IsActive` property. Concrete layers extend this.

---

## Effect Layers

### Distortion Layers (chain, read scene → write distorted)

#### ForceRippleLayer
- Shader: `ForceRipple.fx`
- Supports up to **16 simultaneous instances** (same `float4 RippleData[16]` pattern as WaterRippleBuddy)
- Instance data: `xy` = normalised screen position, `z` = normalised age [0→1], `w` = unused
- Parameters: `RippleData`, `RippleCount`, `AspectRatio`, `WaveSpeed`, `WaveFrequency`, `RefractionStrength`, `ReflectionStrength`, `ReflectionColor`
- Lifetime: configurable, default **0.6s** (faster/sharper than water ripple — this is a shockwave)
- Wave decays faster with distance (`exp(-dist * 8.0)`) for a tight ring look

#### GravityWaveLayer
- Shader: `GravityWave.fx`
- Supports up to **8 simultaneous instances**
- Instance data: `x` = normalised Y position of ground contact, `y` = normalised age, `z/w` = unused
- Distortion is **horizontal only** — UV.x is displaced, UV.y is not
- Wave travels left and right from a horizontal band at the contact Y
- Parameters: `WaveData`, `WaveCount`, `WaveSpeed`, `WaveFrequency`, `RefractionStrength`
- Lifetime: default **0.4s**

#### ScreenShakeLayer
- Shader: `ScreenShake.fx`
- **Trauma-based:** `TriggerScreenShake(float intensity)` adds to a `_trauma` value (clamped 0–1). Each frame, `_trauma` decays by `TraumaDecayRate` (default 2.0/s). Shake magnitude = `trauma²`.
- Shader receives `ShakeOffset` (a `Vector2` computed each frame from trauma + a pseudo-random seed driven by elapsed time). Shifts UV uniformly — no distortion, just translation.
- Parameters: `ShakeOffset`
- Multiple calls to `TriggerScreenShake` stack (add to trauma).

#### ChromaticAberrationLayer
- Shader: `ChromaticAberration.fx`
- Splits R, G, B channels, samples each at a slightly offset UV, recombines
- Intensity decays linearly over `duration`
- Parameters: `Intensity`, `AspectRatio`
- Single active instance (new trigger overwrites/extends current)

#### HeatHazeLayer
- Shader: `HeatHaze.fx`
- Supports up to **8 simultaneous zones** (around characters, projectiles, etc.)
- Each zone: position + radius + intensity + phase offset (for varied animation)
- Continuous sinusoidal UV distortion within the zone radius; no age-based decay unless `duration` is set
- `StopHeatHaze(position)` removes the nearest zone to that position
- Parameters: `HazeData[8]`, `HazeCount`, `Time`, `AspectRatio`
- Shader reads `Time` (total elapsed seconds) to animate — component passes this each frame

---

### Overlay Layers (additive, drawn after back buffer blit)

#### HitFlashLayer
- Shader: `HitFlash.fx`
- Draws a full-screen quad with `BlendState.Additive` at `intensity * color`
- Intensity curve: instant to 1.0, then exponential decay over **0.1s**
- Parameters: `FlashColor`, `Intensity`
- Multiple triggers stack intensity (cap at 1.0)

#### AnimeSuperLayer
- Shader: `AnimeSuper.fx`
- Timed sequence driven by a single `Progress` float [0→1] over **1.5s**:
  - 0.0–0.1: white flash (intensity ramps to 1 then drops)
  - 0.1–0.8: radial speed lines rotate around screen centre, red vignette pulses
  - 0.8–1.0: fade out
- `Progress` is driven automatically by elapsed time; layer expires at 1.0
- Parameters: `Progress`, `LineColor`, `VignetteColor`
- Only one instance at a time; a second trigger while active restarts from 0

---

## Shader Authoring Rules

All shaders MUST follow the pattern proven during WaterRippleBuddy development:

```hlsl
#if OPENGL
    #define SV_POSITION POSITION
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0_level_9_1
    #define PS_SHADERMODEL ps_4_0_level_9_1
#endif

// ... uniforms ...

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color    : COLOR0;
    float2 TexCoord : TEXCOORD0;
};

float4 PS(VertexShaderOutput input) : COLOR   // ← COLOR not COLOR0 or SV_Target
{
    // ... pixel shader logic ...
}

technique EffectName
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL PS();
        // NO VertexShader — SpriteBatch provides it
    }
}
```

**Why:** On DesktopGL (OpenGL), omitting the `#if OPENGL` block causes silent failure (black screen). SpriteBatch owns the vertex stage; providing a VertexShader in the technique causes it to be ignored or to conflict.

---

## Shader Content Pipeline

Shaders are **not embedded** in the library DLL. The consuming game must:

1. Copy the `.fx` files from `ScreenFX/Content/` into their game's `Content/` folder.
2. Add each to `Content.mgcb`:

```
#begin ForceRipple.fx
/importer:EffectImporter
/processor:EffectProcessor
/processorParam:DebugMode=Auto
/build:ForceRipple.fx
```

This is the same requirement as WaterRippleBuddy. The `ScreenFX.Example` project demonstrates the complete `Content.mgcb` setup.

---

## Example Project (ScreenFX.Example)

Demonstrates all 7 effects triggered by keyboard input:

| Key | Effect |
|-----|--------|
| Click | Force Ripple at cursor |
| Space | Gravity Wave at screen centre-bottom |
| S | Screen Shake (medium) |
| C | Chromatic Aberration (0.5s) |
| H | Heat Haze at cursor (toggle) |
| F | Hit Flash |
| A | Anime Super |

Same background image (`Braid_screenshot8.jpg`) as WaterRippleBuddy.Example.

---

## .csproj Setup

`ScreenFX.csproj` targets `net8.0`, references MonoGame.Framework.DesktopGL. No content pipeline references needed (shaders are not built by the library project).

`ScreenFX.Example.csproj` references the `ScreenFX` project and includes MonoGame content pipeline tooling (same setup as `WaterRippleBuddy.Example.csproj`).

---

## Out of Scope for v1

- Configurable layer ordering at runtime
- Effect parameter tweaking at runtime (beyond what triggers provide)
- More than 16 simultaneous force ripples / 8 gravity waves / 8 heat haze zones
- Any 3D support
