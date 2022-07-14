using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth.Internals.Common.Framework.Audio
{
    public static class SoundPlayer
    {
        public readonly struct SoundDefinition {
            public readonly OggAudio Sound { get; init; }
            public readonly string Name { get; init; }
        }

        public static List<SoundDefinition> Sounds = new();
        public static TimeSpan GetLengthOfSound(string filePath)
        {
            byte[] bytes = File.ReadAllBytes(filePath);

            return SoundEffect.GetSampleDuration(bytes.Length, sizeof(byte) * bytes.Length, AudioChannels.Stereo);
        }
        private static float MusicVolume => TankGame.Settings.MusicVolume;
        private static float EffectsVolume => TankGame.Settings.EffectsVolume;
        private static float AmbientVolume => TankGame.Settings.AmbientVolume;
        public static OggAudio PlaySoundInstance(string audioPath, SoundContext context, float volume = 1f, bool autoApplyContentPrefix = true)
        {
            // because ogg is the only good audio format.
            var prepend = autoApplyContentPrefix ? TankGame.Instance.Content.RootDirectory + "/" : string.Empty;
            audioPath = prepend + audioPath;

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
            var sfx = new OggAudio(audioPath);

            var soundDef = new SoundDefinition()
            {
                Sound = sfx,
                Name = sfx.Name,
            };

            // check if it exists in the cache first
            bool exists = Sounds.Any(ogg => ogg.Name == sfx.Name);

            //GameContent.Systems.ChatSystem.SendMessage($"{nameof(exists)}: {exists}", Color.White);
            //GameContent.Systems.ChatSystem.SendMessage($"new list count: {Sounds.Count}", Color.White);

            sfx.Instance.Play();
            sfx.Instance.Volume = volume;

            /*if (exists)
            {
                var sound = Sounds[Sounds.FindIndex(p => p.Name == soundDef.Name)];// = soundDef;

                if (sound.Sound.IsPlaying())
                    sound.Sound.Instance.Stop();
                sound.Sound.Instance.Play();
                sound.Sound.Instance.Volume = volume;

                Sounds[Sounds.FindIndex(p => p.Name == soundDef.Name)] = soundDef;
            }
            else
            {
                soundDef.Sound.Instance.Volume = volume;
                soundDef.Sound.Instance?.Play();
                Sounds.Add(soundDef);
            }*/

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