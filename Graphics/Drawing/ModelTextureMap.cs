using Microsoft.Xna.Framework.Graphics;
using System.Collections;
using System.Collections.Generic;

namespace TanksRebirth.Graphics.Drawing; 

/// <summary>
/// Describes a mapping of a model's meshes to what textures the given mesh should use.
/// </summary>
public class ModelTextureMap : IEnumerable<KeyValuePair<string, Texture2D>> {
    readonly Dictionary<string, Texture2D> _dict;
    public int Count => _dict.Count;
    public Texture2D this[string name] {
        get => _dict[name];
        set => _dict[name] = value;
    }

    public ModelTextureMap() {
        _dict = [];
    }

    public IEnumerator<KeyValuePair<string, Texture2D>> GetEnumerator()
        => _dict.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
}