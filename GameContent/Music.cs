using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Audio;
using System;
using WiiPlayTanksRemake.Internals.Common;
using NVorbis;
using System.IO;
using WiiPlayTanksRemake.Internals.Common.Utilities;

namespace WiiPlayTanksRemake.GameContent
{
    public enum MusicState
    {
        Playing,
        Paused,
        Stopped
    }
    /// <summary>
    /// Be sure to use the 
    /// </summary>
    public class Music
    {
        public static List<Music> AllMusic { get; } = new();

        public float volume;

        public MusicState State { get; private set; }

        public float maxVolume;

        public string Name { get; set; }

        private SoundEffect _sound;
        public SoundEffectInstance Track { get; private set; }

        private Music(string name, string musicPath, float maxVolume) {
            Name = name;
            _sound = TankGame.Instance.Content.Load<SoundEffect>(musicPath);
            Track = _sound.CreateInstance();
            Track.IsLooped = true;
            this.maxVolume = maxVolume;
            AllMusic.Add(this);
        }

        public static Music CreateMusicTrack(string name, string musicPath, float maxVolume)
            => new(name, musicPath, maxVolume);

        public void Play() {
            Track.Volume = volume;
            Track?.Play();
            OnBegin?.Invoke(this, new());
        }

        public void Pause() {
            State = MusicState.Paused;
            Track?.Pause();
        }

        public void Stop() {
            Track.Volume = 0f;
            Track?.Stop();
            OnStop?.Invoke(this, new());
        }

        internal void Update() {
            Track.Volume = volume;
            if (volume > maxVolume)
                volume = maxVolume;

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
            => $"name: {Name} | state: {State} | volume/max: {volume}/{maxVolume}";
    }
}