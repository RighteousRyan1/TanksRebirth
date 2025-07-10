using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TanksRebirth.Internals;

namespace TanksRebirth.Graphics.Shaders;

// unused but maybe in the future
// pixel shader was not the right approach lmao
public class AOEffect {
    public readonly Effect Shader;

    public float _intensity;
    public Vector3 _direction;

    public float Intensity {
        get => _intensity;
        set {
            Shader.Parameters["oIntensity"]?.SetValue(value);
            _intensity = value;
        }
    }
    public Vector3 Direction {
        get => _direction;
        set {
            Shader.Parameters["oDirection"]?.SetValue(value);
            _direction = value;
        }
    }

    public AOEffect(Vector3? direction = null, float intensity = 0.7f) {
        // might not need it to be an instanced asset, but we'll see
        Shader = GameResources.GetRawGameAsset<Effect>("Assets/shaders/ambient_occlusion");

        if (direction is null)
            direction = Vector3.Normalize(new Vector3(0, 1, 1));

        Intensity = intensity;
        Direction = direction.Value;
    }

    public static implicit operator Effect(AOEffect effect) => effect.Shader;
}
