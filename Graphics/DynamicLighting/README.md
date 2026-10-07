# Dynamic lighting

Shadow-mapped sun, point lights and spot lights, volumetric sun shafts, and a few lighting presets for Tanks Rebirth.
Built for MonoGame 3.8.4 DesktopGL: `vs_3_0` / `ps_3_0` shaders only (the OpenGL effect compiler's limit),
`SurfaceFormat.Color` render targets no larger than 2048, Reach profile.

## Controls

| Key / command | What it does |
|---|---|
| **F7** | Cycle presets: the times of day in order → Blackout → DayCycle → Off |
| **F8** | Split view: left half original, right half lit |
| `/lighting afternoon \| golden \| night \| blackout \| off` | Pick a preset |
| `/lighting split` | Toggle split view |
| `/lighting stats` | Draw / light / shadow counts for the last frame |

## Files

| File | Role |
|---|---|
| `Content/Assets/shaders/lighting/lighting.fx` | All shaders (shadow depth, light accumulation, shafts, composite) |
| `LightingSystem.cs` | Engine: capture, shadow maps, light buffer, composite. Depends only on MonoGame |
| `LightCaptureEffect.cs` | Engine: `BasicEffect` subclass that reports draws to the lighting system |
| `Lights.cs` | Engine: `PointLight`, `SpotLight`, `SunLight`, `AmbientLight`, `MeshLighting` |
| `LightingQuality.cs` | Engine: performance limits (shadow map sizes, light counts, shaft resolution, filters) |
| `LightingPresets.cs` | Look: time-of-day presets, day cycle, room lamps. No game types |
| `GameplayLights.cs` | Look: headlights, shell, mine and explosion lights. No game types |
| `LightingSettings.cs` | Menu-facing options (on/off, quality levels, time of day) mapped onto the above |
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
- The sun uses two shadow maps: the sharp one around `Sun.ShadowCenter` (the board), and a coarser one covering
  `Sun.RoomShadowBounds` (the whole room; `null` fits it to everything drawn). Outside the sharp map the room map
  takes over, so sunlight and shadows work anywhere the camera goes. The shafts march through the room map.
  `Sun.RoomShadows = false` turns the room map off (saves one 2048² shadow render).
- `Sun.Shafts.Density` / `MarchLength`, or turn the shafts off.
- `LightingShowcase.StartupPreset` picks the preset at launch.

## Things to know

- Lighting multiplies the finished scene, so 2D/additive particles drawn into the scene are darkened in dark
  presets like everything else. Explosions and shells emit their own light to compensate.
- The game's baked blob shadows under tanks and on walls still draw, on top of the real shadows.
  `CommandGlobals.DrawMeshShadows = false` hides the tank ones if you prefer.
- Shader rules that keep it working on MonoGame's OpenGL path are listed at the top of `lighting.fx`. In particular,
  pixel shaders avoid array and matrix constants, because MonoGame packs those differently from MojoShader.

## Day cycle

`/lighting cycle` (or `Preset.DayCycle`) runs a clock. The sun and moon have real positions for each minute,
worked out from `Latitude`, `Declination` (the season), `SolarNoon` and `CompassRotation` in `LightingPresets`.
The room faces south through the back windows: sunrise is in the east (−X), and the sun sets past the side windows (+X).
Colors, the sun's warmth, ambient light, light beams, lamps and headlights all follow the sun's height,
from `LightingPresets.SkyGradient` (one entry per elevation; edit it to restyle the whole day). After dusk the moon takes over.

- `/lighting time 8:01` jumps the clock; `/lighting daylength 24` = 24 real minutes per day (1 game minute per real second at 1440)
- `/lighting season summer|spring|winter` (or a declination in degrees) changes how long and how high the sun goes
- `/lighting pause` freezes the clock; `LightingPresets.HourText` gives the time as "8:01 AM"
- `LightingSystem.SunBlockers` are invisible boxes that only block the sun. The showcase uses them to seal the small
  gaps where the room's walls meet the ceiling, which otherwise let thin lines of sun through at some hours.

## Shadow resolution

- `Sun.ShadowBounds` (set to `LightingPresets.BoardBounds` by the presets) fits the sharp sun map tightly around the
  board as the sun sees it. A low sun gets several times more detail than with the old square `ShadowRadius` map.
- `Quality.SunShadowMapSize` / `RoomShadowMapSize`: 2048 by default, 4096 with the HiDef profile (2x sharper, ~4x memory).
- `Quality.ShadowAtlasSize`: lamps, headlights and explosions. 4096 at High (512px cube faces), falls back
  to 2048 on Reach. `Quality.SoftLocalShadows` picks the smooth 3x3 filter or the cheaper 2x2 one.

## Settings (for a graphics menu)

Look and cost are separate: presets say what the scene looks like, `LightingSystem.Quality` caps what it may cost.
When they disagree quality wins (a preset asking for 5 shadowed lamps gets at most the quality's limit).

`LightingSettings` holds everything a settings page shows, as plain properties that serialize with System.Text.Json:

| Property | Menu control |
|---|---|
| `Enabled` | Dynamic lighting on / off (off frees its video memory) |
| `QualityLevel` + `SetQualityLevel()` | Low / Medium / High / Ultra dropdown (`Custom` after a manual change, see `UpdateQualityLevel()`) |
| `SunShadows`, `LampShadows` | Off / Low / Medium / High / Ultra |
| `LightShafts`, `LightCount` | Off / Low / Medium / High |
| `TimeOfDay`, `DayLengthMinutes` | Time of day dropdown, day length slider |
| `GameplayLights` | Shell / mine / explosion lights |

```csharp
// GameConfig.cs
public LightingSettings Lighting { get; set; } = new();

// after the config is loaded (before LightingShowcase.Initialize)
LightingSettings.Current = TankGame.Settings.Lighting;

// in the menu: edit a copy, apply on "Apply"
var candidate = LightingSettings.Current.Clone();
candidate.SetQualityLevel(LightingQualityLevel.Medium);
LightingSettings.Current = candidate;
candidate.Apply();
```

`LightingSettings.ToQuality()` is the only place menu choices become numbers; edit it to retune the levels.
`LightingPresets.PresetChanged` keeps `TimeOfDay` in sync when F7 or `/lighting` changes it.
`/lighting quality low|medium|high|ultra`, `/lighting enable`, `/lighting disable` test the settings before a menu exists.
