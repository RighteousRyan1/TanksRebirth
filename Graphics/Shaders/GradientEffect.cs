using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TanksRebirth.Internals;

namespace TanksRebirth.Graphics.Shaders;

public class GradientEffect {
    public readonly Effect Shader;

    public float _center, _angle, _opacity;
    public Color _top, _bottom;

    public float Center {
        get => _center;
        set {
            Shader.Parameters["oCenter"]?.SetValue(value);
            _center = value;
        }
    }
    public float Angle {
        get => _angle;
        set {
            Shader.Parameters["oAngle"]?.SetValue(value);
            _angle = value;
        }
    }
    public float Opacity {
        get => _opacity;
        set {
            Shader.Parameters["oOpacity"]?.SetValue(value);
            _angle = value;
        }
    }
    public Color Top {
        get => _top;
        set {
            Shader.Parameters["oTopColor"]?.SetValue(value.ToVector4());
            _top = value;
        }
    }
    public Color Bottom {
        get => _bottom;
        set {
            Shader.Parameters["oBottomColor"]?.SetValue(value.ToVector4());
            _bottom = value;
        }
    }

    public GradientEffect(Color top, Color bottom, float center = 0.5f, float angle = 0f, float opacity = 1f) {
        // might not need it to be an instanced asset, but we'll see
        Shader = GameResources.GetRawGameAsset<Effect>("Assets/shaders/controlled_gradient");

        Top = top;
        Bottom = bottom;

        Center = center;
        Angle = angle;
        Opacity = opacity;
    }

    public static implicit operator Effect(GradientEffect effect) => effect.Shader;
}
