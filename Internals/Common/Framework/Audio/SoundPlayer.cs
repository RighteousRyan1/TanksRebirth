using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using TanksRebirth.GameContent.ModSupport;
using TanksRebirth.GameContent.UI;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth.Internals.Common.Framework.Audio;

public static class SoundPlayer
{
    /// <summary>Sounds that will have a sound limit.</summary>
    public static Dictionary<string, OggAudio> SavedSounds = new();
    public static TimeSpan GetLengthOfSound(string filePath) {
        byte[] soundData = File.ReadAllBytes(filePath);
        return SoundEffect.GetSampleDuration(soundData.Length, 44100, AudioChannels.Stereo);
    }
    private static float MusicVolume => TankGame.Settings.MusicVolume;
    private static float EffectsVolume => TankGame.Settings.EffectsVolume;
    private static float AmbientVolume => TankGame.Settings.AmbientVolume;
    // my spidey senses are telling me this is horribly inefficient.
    public static OggAudio PlaySoundInstance(string audioPath, SoundContext context, float volume = 1f, float maxVolume = 1f, float panOverride = 0f, float pitchOverride = 0f, bool rememberMe = false) {
        // because ogg is the only good audio format.
        audioPath = Path.Combine(audioPath.Contains("Content/") ? "" : TankGame.Instance.Content.RootDirectory, audioPath);

        volume *= context switch {
            SoundContext.Music => MusicVolume,
            SoundContext.Effect => EffectsVolume,
            SoundContext.Ambient => AmbientVolume,
            _ => throw new ArgumentOutOfRangeException(nameof(context), context, "Uh oh! Seems like a new sound type was implemented, but I was not given a way to handle it!"),
        };

        OggAudio sfx;

        // Verify that the sound exists and we are told to remember it, if so, load the sound from the dictionary.
        // And proceed to cache it.
        var existsInDictionary = SavedSounds.ContainsKey(audioPath) && rememberMe;
        sfx = existsInDictionary ? SavedSounds[audioPath] : new OggAudio(audioPath);
        if (!existsInDictionary && rememberMe)
            SavedSounds.Add(audioPath, sfx);
        sfx.Instance.Pan = MathHelper.Clamp(panOverride, -1f, 1f);
        sfx.Instance.Pitch = MathHelper.Clamp(pitchOverride, -1f, 1f);
        sfx.Play();
        sfx.Volume = MathHelper.Clamp(volume * maxVolume, 0f, 1f);

        //GameContent.Systems.ChatSystem.SendMessage($"{nameof(exists)}: {exists}", Color.White);
        //GameContent.Systems.ChatSystem.SendMessage($"new list count: {Sounds.Count}", Color.White);

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
    public static void PlaySoundInstance(OggAudio fromSound, SoundContext context, float volume = 1f, float maxVolume = 1f, bool playNew = false, float panOverride = 0f, float pitchOverride = 0f) {
        volume *= context switch {
            SoundContext.Music => MusicVolume,
            SoundContext.Effect => EffectsVolume,
            SoundContext.Ambient => AmbientVolume,
            _ => throw new ArgumentOutOfRangeException(nameof(context), context, "Uh oh! Seems like a new sound type was implemented, but I was not given a way to handle it!"),
        };
        //if (playNew) {
            PlaySoundInstance(fromSound.Path, context, volume, maxVolume, panOverride, pitchOverride);
            return;
        //}
    }

    public static OggAudio SoundError() => PlaySoundInstance("Assets/sounds/menu/menu_error.ogg", SoundContext.Effect, rememberMe: true);
}
public enum SoundContext : byte
{
    Music,
    Effect,
    Ambient
}