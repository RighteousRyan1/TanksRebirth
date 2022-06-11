using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Audio;
using System;
using TanksRebirth.Internals.Common;
using NVorbis;
using System.IO;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Internals.Common.Framework.Audio;
using Microsoft.Xna.Framework.Media;

namespace TanksRebirth.GameContent
{
    public enum MusicState
    {
        Playing,
        Paused,
        Stopped
    }
    public interface IAudio
    {
        float Volume { get; set; }
        float MaxVolume { get; set; }
        string Name { get; set; }
        MusicState State { get; set; }

        void Play();
        void Pause();
        void Stop();

        void Update();
    }
    public class OggMusic : IAudio
    {
        public float Volume { get; set; }
        public float MaxVolume { get; set; }
        public MusicState State { get; set; }
        public string Name { get; set; }
        /*
         * 
            var song = new Internals.Common.Framework.Audio.OggAudio("amethyst1");
            song.Play();
            song.SetVolume(0f);*/
        private OggAudio _music;
        public OggMusic(string name, string songPath, float maxVolume)
        {
            _music = new(songPath);
            Name = name;
            MaxVolume = maxVolume;
        }
        public void Play()
            => _music.Play();
        public void Pause()
            => _music?.Pause();
        public void Resume()
            => _music?.Resume();
        public void SetVolume(float volume)
            => _music.SetVolume(volume);
        public void Stop()
            => _music?.Stop();

        public bool IsPaused()
            => _music.State == SoundState.Paused;
        public bool IsStopped()
            => _music.State == SoundState.Stopped;
        public bool IsPlaying()
            => _music.State == SoundState.Playing;

        public void Update()
        {
            _music.SetVolume(Volume);
            if (Volume > MaxVolume)
                Volume = MaxVolume;

            if (!TankGame.Instance.IsActive)
            {
                if (!_music.IsPaused())
                {
                    _music?.Pause();
                }
            }
            else
            {
                if (_music.IsPaused())
                {
                    _music?.Resume();
                }
            }
        }
    }
    /// <summary>
    /// A class that allows simple usage of a music track.
    /// </summary>
    public class Music : IAudio
    {
        public static List<IAudio> AllMusic { get; } = new();

        private float _volume;
        public float Volume
        {
            get => _volume;
            set => _volume = MathHelper.Clamp(value, 0, 1);
        }

        public MusicState State { get; set; }

        public float MaxVolume { get; set; }

        public string Name { get; set; }

        private SoundEffect _sound;
        public SoundEffectInstance Track { get; private set; }

        private Music(string name, string musicPath, float maxVolume) {
            Name = name;
            _sound = TankGame.Instance.Content.Load<SoundEffect>(musicPath);
            Track = _sound.CreateInstance();
            Track.IsLooped = true;
            MaxVolume = maxVolume;
            AllMusic.Add(this);
        }

        /// <summary>
        /// Creates a <see cref="Music"/>.
        /// </summary>
        /// <param name="name">The name of this <see cref="Music"/>.</param>
        /// <param name="musicPath">The path to the audio file for this <see cref="Music"/>.</param>
        /// <param name="maxVolume">The maximum volume allowed for this <see cref="Music"/>.</param>
        /// <returns></returns>
        public static Music CreateMusicTrack(string name, string musicPath, float maxVolume)
            => new(name, musicPath, maxVolume);

        /// <summary>
        /// Plays the <see cref="Track"/>.
        /// </summary>
        public void Play() {
            Track.Volume = _volume;
            Track?.Play();
            OnBegin?.Invoke(this, new());
        }

        /// <summary>
        /// Pauses the <see cref="Track"/>.
        /// </summary>
        public void Pause() {
            State = MusicState.Paused;
            Track?.Pause();
        }

        /// <summary>
        /// Stops the <see cref="Track"/>.
        /// </summary>
        public void Stop() {
            Track.Volume = 0f;
            Track?.Stop();
            OnStop?.Invoke(this, new());
        }

        public void Update() {
            Track.Volume = _volume;
            if (Volume > MaxVolume)
                Volume = MaxVolume;

            if (!TankGame.Instance.IsActive) {
                if (!Track.IsPaused()) {
                    Track?.Pause();
                }
            }
            else {
                if (Track.IsPaused()) {
                    Track?.Resume();
                }
            }
        }
        public event EventHandler OnBegin;
        public event EventHandler OnStop;

        public override string ToString()
            => $"name: {Name} | state: {State} | volume/max: {Volume}/{MaxVolume}";
    }
}