# WaterRippleBuddy

A MonoGame `DrawableGameComponent` that renders an animated water-ripple distortion effect over a scene.

Call `AddDrop(position)` to spawn a droplet. Concentric rings radiate outward from the impact point, bending the scene beneath via UV displacement (refraction) and optionally tinting wave crests with a reflection color. Up to 16 droplets can be active simultaneously — all processed in a single shader pass.

## Requirements

- .NET 8
- MonoGame 3.8.2 (DesktopGL)

## Project Layout

```
WaterRippleBuddy/          library — component, droplet, interface, shader source
WaterRippleBuddy.Example/   runnable demo — click to spawn ripples
```

## Building

```bash
dotnet build WaterRippleBuddy.sln
dotnet run --project WaterRippleBuddyExample
```

`MonoGame.Content.Builder.Task` runs MGCB automatically during the build, compiling the shader and textures.

## Integrating the library into your own game

### 1 — Add the shader to your content pipeline

Copy `WaterRippleBuddy/Content/Distorter_Ripple.fx` into your game's `Content/` folder and add it to `Content.mgcb`:

```
#begin Distorter_Ripple.fx
/importer:EffectImporter
/processor:EffectProcessor
/processorParam:DebugMode=Auto
/build:Distorter_Ripple.fx
```

### 2 — Create and register the component

```csharp
// In Game.Initialize():
_water = new WaterRippleComponent(this)
{
    DrawOrder          = 100,
    ReflectionColor    = new Color(200, 220, 255),
    DropletLifetime    = 2.5f
};
Components.Add(_water);
```

### 3 — Render your scene into a RenderTarget

```csharp
// In Game.LoadContent():
_sceneTarget = new RenderTarget2D(GraphicsDevice, width, height);
_water.RenderTarget = _sceneTarget;

// In Game.Draw():
GraphicsDevice.SetRenderTarget(_sceneTarget);
// ... draw your scene ...
GraphicsDevice.SetRenderTarget(null);

// WaterRippleComponent draws the distorted result to the back buffer.
// Do NOT draw _sceneTarget yourself.
base.Draw(gameTime);
```

### 4 — Spawn ripples

```csharp
_water.AddDrop(new Vector2(mouseX, mouseY));
```

## API

### Constructor

```csharp
WaterRippleComponent(Game game, string shaderContentPath = "Distorter_Ripple")
```

`shaderContentPath` is the content-pipeline path to the compiled effect. Defaults to `"Distorter_Ripple"` (shader at the root of your Content directory).

### Properties

| Property | Type | Default | Description |
|---|---|---|---|
| `RenderTarget` | `RenderTarget2D?` | `null` | Scene to distort. Assign before each frame. |
| `WaveSpeed` | `float` | `1.5` | How fast the wave pattern travels outward. |
| `WaveFrequency` | `float` | `40` | Ring spacing — higher = tighter rings. |
| `RefractionStrength` | `float` | `0.03` | UV distortion intensity (how far the image bends). |
| `ReflectionStrength` | `float` | `0.25` | Color-tint blend at wave crests. |
| `ReflectionColor` | `Color` | `Color.White` | Color blended in at crests. Try a soft blue for water. |
| `DropletLifetime` | `float` | `2.0` | Seconds before a droplet fades out. |

### Methods

```csharp
void AddDrop(Vector2 screenPosition)
```

Spawns a ripple at `screenPosition` (pixel coordinates). Silently ignored if 16 droplets are already active.

## Shader notes

The HLSL is in `WaterRippleBuddy/Content/Distorter_Ripple.fx`. The effect takes all active droplets in a single pass via a `float4 DropletData[16]` array. Each element packs normalized screen position (xy) and normalized age (z). The wave model is a damped traveling sinusoid:

```
wave = sin(dist * WaveFrequency - age * WaveSpeed * WaveFrequency)
     * exp(-dist * 4)   // spatial attenuation
     * exp(-age  * 4)   // temporal attenuation
```

`MAX_DROPLETS` in the shader must match `MaxDroplets` in `WaterRippleComponent.cs` if you change the limit.

## License

MIT
