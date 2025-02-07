using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using TanksRebirth.GameContent;

namespace TanksRebirth.Internals.Common.Utilities;

public static class SoundUtils
{
    public static bool IsPlaying(this SoundEffectInstance instance) => instance.State == SoundState.Playing;
    public static bool IsPaused(this SoundEffectInstance instance) => instance.State == SoundState.Paused;
    public static bool IsStopped(this SoundEffectInstance instance) => instance.State == SoundState.Stopped;

    public static float GetPanFromScreenPosition(float posX) {
        return MathUtils.CreateGradientValue(posX, -200, WindowUtils.WindowWidth);
    }
    public static float GetVolumeFromScreenPosition(Vector2 pos) {
        var volumeY = MathUtils.CreateGradientValue(pos.Y, -200, WindowUtils.WindowHeight + 200);
        var volumeX = MathUtils.CreateGradientValue(pos.X, -200, WindowUtils.WindowWidth + 200);
        /*if (volumeY > 1) volumeY = 1;
        if (volumeY < 0) volumeY = 0;*/
        return volumeY * volumeX;
    }
    public static float GetVolumeFromCameraPosition(Vector3 sourcePos, Vector3 camPos, float maxSoundDist = 600f) {
        var dist = 1f - Vector3.Distance(camPos, sourcePos) / maxSoundDist;
        dist = MathHelper.Clamp(dist, 0, 1);

        return dist;
    }
}
