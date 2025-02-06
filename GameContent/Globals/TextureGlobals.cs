using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using tainicom.Aether.Physics2D.Dynamics;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth.GameContent.Globals; 
public static class TextureGlobals {
    public static Dictionary<Color, Texture2D> Pixels = [];

    /// <summary>Load pixel textures for each default MonoGame color.
    /// Will be loaded as White by default, and then asynchronously loaded as their proper color. NVM</summary>
    public static void CreateDynamicTexturesAsync(GraphicsDevice device) {
        /*var whitePixel = new Texture2D(device, 1, 1);
        whitePixel.SetData(new Color[] { Color.White });
        Array.ForEach(ColorUtils.AllColors, x => {
            Pixels.TryAdd(x, whitePixel);
        });*/

        // then asynchronously load
        //Task.Run(() => {
            // await Task.Delay(1);
            for (int i = 0; i < ColorUtils.AllColors.Length; i++) {
                if (Pixels.ContainsKey(ColorUtils.AllColors[i])) continue;
                var texture = new Texture2D(device, 1, 1);
                texture.SetData(new Color[] { ColorUtils.AllColors[i] });
                Pixels[ColorUtils.AllColors[i]] = texture;
            }
        //});
    }
}
