using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using System;
using System.Collections.Generic;
using System.IO;

namespace TanksRebirth.Internals.Common.Framework.Audio;

public static class SoundPlayer
{
    /// <summary>In perspective view, sounds start fading this far from the camera unless told otherwise.</summary>
    public const float DEFAULT_MIN_DISTANCE = 300f;
    /// <summary>In perspective view, sounds are silent this far from the camera unless told otherwise.</summary>
    public const float DEFAULT_MAX_DISTANCE = 2500f;

    static readonly List<OggAudio> _positionedSounds = [];

    /// <summary>Sounds that will have a sound limit.</summary>
    public static Dictionary<string, OggAudio> SavedSounds = [];
    static readonly Dictionary<string, TimeSpan> _durationCache = [];

    public static TimeSpan GetLengthOfSound(string filePath) {
        if (_durationCache.TryGetValue(filePath, out var cached))
            return cached;

        using var stream = File.OpenRead(filePath);
        using var soundEffect = SoundEffect.FromStream(stream);

        var duration = soundEffect.Duration;
        _durationCache[filePath] = duration;

        return duration;
    }
    private static float MusicVolume => TankGame.Settings.MusicVolume;
    private static float EffectsVolume => TankGame.Settings.EffectsVolume;
    private static float AmbientVolume => TankGame.Settings.AmbientVolume;
    // my spidey senses are telling me this is horribly inefficient.
    /// <param name="position">Where the sound plays from in the world. Null plays it without positional panning or falloff.</param>
    /// <param name="minDist">In perspective view, the distance from the camera before the sound starts fading.</param>
    /// <param name="maxDist">In perspective view, the distance from the camera where the sound becomes silent.</param>
    /// <param name="orthographicFade">Whether the sound also fades by distance in orthographic view, as heard from the board.</param>
    public static OggAudio PlaySoundInstance(string audioPath, SoundContext context, float volume = 1f, float maxVolume = 1f, float panOverride = 0f, float pitchOverride = 0f, bool rememberMe = false,
        Vector3? position = null, float minDist = DEFAULT_MIN_DISTANCE, float maxDist = DEFAULT_MAX_DISTANCE, bool orthographicFade = false) {
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
        sfx.BasePan = MathHelper.Clamp(panOverride, -1f, 1f);
        sfx.MinDistance = minDist;
        sfx.MaxDistance = maxDist;
        sfx.OrthographicFade = orthographicFade;
        sfx.Position = position;
        sfx.ApplyPosition();
        sfx.Pitch = pitchOverride;
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
    public static void PlaySoundInstance(OggAudio fromSound, SoundContext context, float volume = 1f, float maxVolume = 1f, bool playNew = false, float panOverride = 0f, float pitchOverride = 0f,
        Vector3? position = null, float minDist = DEFAULT_MIN_DISTANCE, float maxDist = DEFAULT_MAX_DISTANCE, bool orthographicFade = false) {
        volume *= context switch {
            SoundContext.Music => MusicVolume,
            SoundContext.Effect => EffectsVolume,
            SoundContext.Ambient => AmbientVolume,
            _ => throw new ArgumentOutOfRangeException(nameof(context), context, "Uh oh! Seems like a new sound type was implemented, but I was not given a way to handle it!"),
        };
        //if (playNew) {
            PlaySoundInstance(fromSound.Path, context, volume, maxVolume, panOverride, pitchOverride, position: position, minDist: minDist, maxDist: maxDist, orthographicFade: orthographicFade);
            return;
        //}
    }

    /// <summary>Makes a sound with a <see cref="OggAudio.Position"/> follow it every frame while it plays.</summary>
    internal static void Track(OggAudio sound) {
        lock (_positionedSounds) {
            if (!_positionedSounds.Contains(sound))
                _positionedSounds.Add(sound);
        }
    }

    /// <summary>Re-pans and re-fades every playing sound that has a <see cref="OggAudio.Position"/>. Call once per frame after the camera moves.</summary>
    public static void UpdatePositionedSounds() {
        lock (_positionedSounds) {
            for (int i = _positionedSounds.Count - 1; i >= 0; i--) {
                var sound = _positionedSounds[i];
                if (sound.IsDisposed || sound.Position is null || sound.IsStopped()) {
                    _positionedSounds.RemoveAt(i);
                    continue;
                }
                sound.ApplyPosition();
            }
        }
    }

    public static OggAudio SoundError() => PlaySoundInstance("Assets/sounds/menu/menu_error.ogg", SoundContext.Effect, rememberMe: true);
}
public enum SoundContext : byte
{
    Music,
    Effect,
    Ambient
}