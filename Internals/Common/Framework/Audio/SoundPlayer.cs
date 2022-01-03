using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using WiiPlayTanksRemake.Internals.Common.Utilities;

namespace WiiPlayTanksRemake.Internals.Common.Framework.Audio
{
    public static class SoundPlayer
    {
        private static float MasterVolume => TankGame.Settings.MasterVolume;
        private static float MusicVolume => TankGame.Settings.MusicVolume;
        private static float SoundVolume => TankGame.Settings.SoundVolume;
        private static float AmbientVolume => TankGame.Settings.AmbientVolume;
        public static SoundEffectInstance PlaySoundInstance(SoundEffect fromSound, SoundContext context, float volume = 1f)
        {
            switch (context)
            {
                case SoundContext.Music:
                    volume *= MusicVolume * MasterVolume;
                    break;
                case SoundContext.Sound:
                    volume *= SoundVolume * MasterVolume;
                    break;
                case SoundContext.Ambient:
                    volume *= AmbientVolume * MasterVolume;
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
                    volume *= MusicVolume * MasterVolume;
                    break;
                case SoundContext.Sound:
                    volume *= SoundVolume * MasterVolume;
                    break;
                case SoundContext.Ambient:
                    volume *= AmbientVolume * MasterVolume;
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
        Sound,
        Ambient
    }
}