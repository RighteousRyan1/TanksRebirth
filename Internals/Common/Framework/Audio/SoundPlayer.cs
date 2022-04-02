using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using System;
using System.IO;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth.Internals.Common.Framework.Audio
{
    public static class SoundPlayer
    {
        // public static SoundEffectInstance[] PlayingSounds = new SoundEffectInstance[256];
        public static TimeSpan GetLengthOfSound(string filePath)
        {
            byte[] bytes = File.ReadAllBytes(filePath);

            return SoundEffect.GetSampleDuration(bytes.Length, sizeof(byte) * bytes.Length, AudioChannels.Stereo);
        }
        private static float MusicVolume => TankGame.Settings.MusicVolume;
        private static float EffectsVolume => TankGame.Settings.EffectsVolume;
        private static float AmbientVolume => TankGame.Settings.AmbientVolume;
        public static SoundEffectInstance PlaySoundInstance(SoundEffect fromSound, SoundContext context, float volume = 1f)
        {
            switch (context)
            {
                case SoundContext.Music:
                    volume *= MusicVolume;
                    break;
                case SoundContext.Effect:
                    volume *= EffectsVolume;
                    break;
                case SoundContext.Ambient:
                    volume *= AmbientVolume;
                    break;
            }
            var sfx = fromSound.CreateInstance();
            sfx.Volume = volume;
            sfx?.Play();

            return sfx;
        }

        public static SoundEffectInstance PlaySoundInstance(SoundEffect fromSound, SoundContext context, Vector3 position, Matrix world, float volume = 1f)
        {
            switch (context)
            {
                case SoundContext.Music:
                    volume *= MusicVolume;
                    break;
                case SoundContext.Effect:
                    volume *= EffectsVolume;
                    break;
                case SoundContext.Ambient:
                    volume *= AmbientVolume;
                    break;
            }

            var pos2d = GeometryUtils.ConvertWorldToScreen(position, world, TankGame.GameView, TankGame.GameProjection);

            var lerp = GameUtils.ModifiedInverseLerp(-(GameUtils.WindowWidth / 2), GameUtils.WindowWidth + GameUtils.WindowWidth / 2, pos2d.X, true);

            var sfx = fromSound.CreateInstance();
            sfx.Volume = volume;

            // System.Diagnostics.Debug.WriteLine(sfx.Pan);
            sfx?.Play();
            sfx.Pan = lerp;

            return sfx;
        }
    }
    public enum SoundContext : byte
    {
        Music,
        Effect,
        Ambient
    }
}