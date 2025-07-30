using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TanksRebirth.Internals.Common.Utilities; 
public static class TextureUtils {
    public static Texture2D Duplicate(this Texture2D t, GraphicsDevice device) {
        // Get pixel data from the original texture
        Color[] data = new Color[t.Width * t.Height];
        t.GetData(data);

        // Create a new texture and set the copied data
        var copy = new Texture2D(device, t.Width, t.Height);
        copy.SetData(data);

        return copy;
    }
}
