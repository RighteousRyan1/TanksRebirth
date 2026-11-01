using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Net;

namespace TanksRebirth.Graphics.DynamicLighting; 
public struct JackOLanternSettings() {
    public float Retarget;
    public float FlickerTarget = 1f;
    public float Flicker = 1f;
    public float Dip;
    public float Seed = Client.ClientRandom.NextFloat(1, 1000);
    public Color BaseColor = Color.Goldenrod;
}
public static class LightFunctions {
    public static void JackOLanternFlicker(Light light, GameTime gameTime, ref JackOLanternSettings settings) {
        var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        // slow uneven sway, about -1 to 1
        var t = RuntimeData.RunTime + settings.Seed;
        var sway = MathF.Sin(t * 1.7f) * 0.5f + MathF.Sin(t * 2.9f + 1.3f) * 0.3f + MathF.Sin(t * 5.3f + 4.1f) * 0.2f;

        // fast jitter, picks a new random target 10 to 20 times a second and eases toward it
        settings.Retarget -= dt;
        if (settings.Retarget <= 0f) {
            settings.FlickerTarget = 0.82f + Client.ClientRandom.NextFloat(0.3f);
            settings.Retarget = 0.05f + Client.ClientRandom.NextFloat() * 0.05f;
        }
        settings.Flicker = MathHelper.Lerp(settings.Flicker, settings.FlickerTarget, 1f - MathF.Exp(-dt * 25f));

        // rare gutter, a quick dip down and back up
        if (settings.Dip <= 0f && Client.ClientRandom.NextFloat() < dt * 0.15f)
            settings.Dip = 1f;
        settings.Dip = MathF.Max(0f, settings.Dip - dt * 3f);
        var gutter = 1f - 0.45f * MathF.Sin(settings.Dip * MathHelper.Pi);

        var strength = (1f + sway * 0.12f) * settings.Flicker * gutter;

        // a wee bit hardcoded for yellow huh
        light.Intensity = 2f * strength;
        // also deifnitely unhardcode this
        //light.Range = 110f * (Scale * 0.075f);
        light.Color = Color.Lerp(new Color(255, 110, 25), settings.BaseColor, MathHelper.Clamp((strength - 0.55f) / 0.5f, 0f, 1f));
        //light.Position = Position + Vector3.UnitY * 0.6f * Scale;
    }
}
