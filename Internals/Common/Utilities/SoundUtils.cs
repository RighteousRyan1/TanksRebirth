using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Octokit;
using System;
using System.Diagnostics;
using TanksRebirth.GameContent;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.Internals.Common.Framework.Audio;

namespace TanksRebirth.Internals.Common.Utilities;

public static class SoundUtils
{
    public static bool IsPlaying(this SoundEffectInstance instance) => instance.State == SoundState.Playing;
    public static bool IsPaused(this SoundEffectInstance instance) => instance.State == SoundState.Paused;
    public static bool IsStopped(this SoundEffectInstance instance) => instance.State == SoundState.Stopped;

    public static float GetPanFromScreenPosition(float posX) {
        return MathUtils.CreateGradientValueWithNegative(posX, -200, WindowUtils.WindowWidth + 200);
    }
    public static float GetVolumeFromScreenPosition(Vector2 pos) {
        var volumeY = MathUtils.CreateGradientValue(pos.Y, -200, WindowUtils.WindowHeight + 200);
        var volumeX = MathUtils.CreateGradientValue(pos.X, -200, WindowUtils.WindowWidth + 200);
        /*if (volumeY > 1) volumeY = 1;
        if (volumeY < 0) volumeY = 0;*/
        return volumeY * volumeX;
    }
    public static float GetVolumeFromCameraPosition(Vector3 sourcePos, Vector3 camPos, float maxSoundDist = 800f) {
        var dist = 1f - Vector3.Distance(camPos, sourcePos) / maxSoundDist;
        dist = MathHelper.Clamp(dist, 0, 1);

        return dist;
    }

    public static void CreateSpatialSound(OggAudio sound, Vector3 sourcePos, Vector3 camPos, float maxSoundDist = 1f) {
        //float vol = GetVolumeFromCameraPosition(sourcePos, camPos, maxSoundDist);

        //var unproj = MatrixUtils.ConvertWorldToScreen(Vector3.Zero, Matrix.CreateTranslation(sourcePos), CameraGlobals.GameView, CameraGlobals.GameProjection);

        //float pan = GetPanFromScreenPosition(unproj.X);

        if (TankGame.Settings.EffectsVolume == 0f) return;

        sound.Volume = 1f;
        sound.MaxVolume = 1f;

        var lForward = CameraGlobals.RebirthFreecam.World.Backward;
        var lPos = camPos / 15 / maxSoundDist / TankGame.Settings.EffectsVolume;
        var lUp = CameraGlobals.RebirthFreecam.World.Down;

        var eForward = Vector3.Backward;
        var ePos = sourcePos / 15 / maxSoundDist / TankGame.Settings.EffectsVolume;
        var eUp = Vector3.Down;

        static bool checkCompNan(Vector3 v) {
            return float.IsNaN(v.X) || float.IsNaN(v.Y) || float.IsNaN(v.Z);
        }

        if (checkCompNan(lPos)) {
            Debugger.Break();
        }
        if (checkCompNan(ePos)) {
            Debugger.Break();
        }

        sound.Instance.Apply3D(new AudioListener() {
            Forward = lForward,
            Position = lPos,
            Up = lUp,
            //Velocity = CameraGlobals.RebirthFreecam.Velocity
        }, new AudioEmitter() {
            //DopplerScale = 2f,
            Forward = eForward,
            Position = ePos,
            Up = eUp
        });
    }
    public static void CreateSpatialSoundSimple(OggAudio sound, Vector3 sourcePos, Vector3 camPos, float maxSoundDist = 1f) {
        float vol = GetVolumeFromCameraPosition(sourcePos, camPos, maxSoundDist * 800f);

        var unproj = MatrixUtils.ConvertWorldToScreen(Vector3.Zero, Matrix.CreateTranslation(sourcePos), CameraGlobals.GameView, CameraGlobals.GameProjection);

        float pan = GetPanFromScreenPosition(unproj.X);

        sound.MaxVolume = vol;
        sound.Instance.Pan = pan;
    }
}
