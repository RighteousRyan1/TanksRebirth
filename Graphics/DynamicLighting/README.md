## How to Use

```csharp
// a lamp that stays until removed
var lamp = new PointLight(new Vector3(0, 60, 0), Color.Orange, intensity: 1f, range: 250f, castsShadows: true);
LightingSystem.Lights.Add(lamp);

// a flashlight
var spot = new SpotLight(position, direction, Color.White, 1.5f, 400f, innerDegrees: 15, outerDegrees: 30, castsShadows: true);
LightingSystem.Lights.Add(spot);

// lights that follow game objects
LightingSystem.CollectLights += () => LightingSystem.AddFrameLight(myLight);

// the sun / moon
LightingSystem.Sun.Aim(from: windowPosition, to: Vector3.Zero);
LightingSystem.Sun.Shafts.Enabled = true;

// allows certain meshes to have specific lighting
LightingSystem.SetMeshLighting(model, "Lamp_Shade", new MeshLighting { ReceivesLight = false, Emissive = new(1.6f, 1.4f, 1f) });
```
