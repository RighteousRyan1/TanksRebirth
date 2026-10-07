# Dynamic lighting

Shadow-mapped sun, point lights and spot lights, volumetric sun shafts, and a few lighting presets for Tanks Rebirth.
Built for MonoGame 3.8.4 DesktopGL: `vs_3_0` / `ps_3_0` shaders only (the OpenGL effect compiler's limit),
`SurfaceFormat.Color` render targets no larger than 2048, Reach profile.

## Controls

| Key / command | What it does |
|---|---|
| **F7** | Cycle presets: Afternoon → Golden Hour → Night → Blackout → Off |
| **F8** | Split view: left half original, right half lit |
| `/lighting afternoon \| golden \| night \| blackout \| off` | Pick a preset |
| `/lighting split` | Toggle split view |
| `/lighting stats` | Draw / light / shadow counts for the last frame |

## Files

| File | Role |
|---|---|
| `Content/Assets/shaders/lighting/lighting.fx` | All shaders (shadow depth, light accumulation, shafts, composite) |
| `LightingSystem.cs` | Core: capture, shadow maps, light buffer, composite. Depends only on MonoGame |
| `LightCaptureEffect.cs` | `BasicEffect` subclass that reports draws to the lighting system |
| `Lights.cs` | `PointLight`, `SpotLight`, `SunLight`, `AmbientLight`, `MeshLighting` |
| `LightingPresets.cs` | The presets, room lamps, map lanterns, helpers for gameplay lights |
| `LightingShowcase.cs` | Game glue: loads the effect, feeds tanks/shells/mines/explosions, hotkeys, `/lighting` |

## How it plugs in

Nothing in the game's draw code changed. The integration is these hooks:

1. `GameResources` calls `LightingSystem.Instrument(model)` on every loaded `Model`. That swaps each `BasicEffect`
   for a `LightCaptureEffect`, which renders identically (effects shared between meshes stay shared) but records
   each draw and its world matrix.
2. `TankGame.PrepareGameBuffers` calls `LightingSystem.BeginFrame(view, projection)` before the world is drawn and
   `LightingSystem.EndFrame(GameFrameBuffer)` after it.
3. `GameShaders.DrawTankMesh` calls `LightingSystem.Submit(...)`, because tanks use `tank.fx` instead of `BasicEffect`.
   Do the same for any other mesh you draw with a custom effect.
4. `TankGame.LoadContent` calls `LightingShowcase.Initialize(GraphicsDevice)` and `Update` calls `LightingShowcase.Update()`.

At `EndFrame` the recorded geometry is drawn again into the sun shadow map, a 2048² shadow atlas (point lights as
6×256² cube faces, spot lights as 512² tiles) and a light buffer. The light buffer is multiplied onto the finished
frame (2× modulate, so lights can also brighten), then the sun shafts are added. If `lighting.fx` fails to load or
anything throws, lighting turns itself off and the game renders as before.

## Using it

```csharp
// a lamp that stays until removed
var lamp = new PointLight(new Vector3(0, 60, 0), Color.Orange, intensity: 1f, range: 250f, castsShadows: true);
LightingSystem.Lights.Add(lamp);

// a flashlight
var spot = new SpotLight(position, direction, Color.White, 1.5f, 400f, innerDegrees: 15, outerDegrees: 30, castsShadows: true);
LightingSystem.Lights.Add(spot);

// lights that follow game objects: add them every frame
LightingSystem.CollectLights += () => LightingSystem.AddFrameLight(myLight);

// the sun / moon
LightingSystem.Sun.Aim(from: windowPosition, to: Vector3.Zero);
LightingSystem.Sun.Shafts.Enabled = true;

// per-mesh options: glowing, no shadows, invisible to lighting
LightingSystem.SetMeshLighting(model, "Lamp_Shade", new MeshLighting { ReceivesLight = false, Emissive = new(1.6f, 1.4f, 1f) });
```

## Tuning

- `MaxShadowedPointLights` (default 4, max 8) and `MaxShadowedSpotLights` (default 3) bound the shadow cost.
  Each shadowed point light re-renders nearby geometry 6 times.
- `MaxLocalLights` (default 32) bounds the light passes. Lights are batched 4 per pass and ranked by
  `Priority`, then by distance to `FocusPoint`.
- `Sun.ShadowRadius` trades shadow sharpness for coverage (560 covers the board).
- `Sun.Shafts.Density` / `MarchLength`, or turn the shafts off.
- `LightingShowcase.StartupPreset` picks the preset at launch.

## Things to know

- Lighting multiplies the finished scene, so 2D/additive particles drawn into the scene are darkened in dark
  presets like everything else. Explosions and shells emit their own light to compensate.
- The game's baked blob shadows under tanks and on walls still draw, on top of the real shadows.
  `CommandGlobals.DrawMeshShadows = false` hides the tank ones if you prefer.
- Shader rules that keep it working on MonoGame's OpenGL path are listed at the top of `lighting.fx`. In particular,
  pixel shaders avoid array and matrix constants, because MonoGame packs those differently from MojoShader.
