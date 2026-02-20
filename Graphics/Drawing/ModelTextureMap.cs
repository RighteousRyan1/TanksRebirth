using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace TanksRebirth.Graphics.Drawing; 

/// <summary>
/// Describes a mapping of a model's meshes to what textures the given mesh should use.
/// </summary>
public class ModelTextureMap {
    readonly Dictionary<string, Texture2D> _dict;
    public int Count => _dict.Count;
    public Texture2D this[string name] {
        get => _dict[name];
        set => _dict[name] = value;
    }

    public ModelTextureMap() {
        _dict = [];
    }
}
