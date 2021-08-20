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

        private  SoundEffect _sound;
        public SoundEffectInstance Instance { get; private set; }

        private Music(string name, string musicPath, float maxVolume) {
            Name = name;
            _sound = TankGame.Instance.Content.Load<SoundEffect>(musicPath);
            Instance = _sound.CreateInstance();
            Instance.IsLooped = true;
            this.maxVolume = maxVolume;
        }

        public static Music CreateMusicTrack(string name, string musicPath, float maxVolume) {
            return new(name, musicPath, maxVolume);
        }

        public void Play() {
            Instance?.Play();
            OnBegin?.Invoke(this, new());
        }

        public void Pause() {
            State = MusicState.Paused;
            Instance?.Pause();
        }

        public void Stop() {
            Instance.Volume = 0f;
            Instance?.Stop();
            OnStop?.Invoke(this, new());
        }

        internal void Update() {
                Instance.Volume = volume;
                if (volume > maxVolume)
                    volume = maxVolume;

            if (!TankGame.Instance.IsActive) {
                if (!Instance.IsPaused()) {
                    Instance?.Pause();
                }
            }
            else {
                if (Instance.IsPaused()) {
                    Instance?.Resume();
                }
            }
        }
        public event EventHandler OnBegin;
        public event EventHandler OnStop;

        public override string ToString()
        {
            return $"name: {Name} | state: {State} | volume/max: {volume}/{maxVolume}";
        }
    }
}