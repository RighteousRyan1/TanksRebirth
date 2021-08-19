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

        public static int CurrentMusicTrack { get; private set; }

        public int id;
        public float volume;

        public MusicState State { get; private set; }

        public float maxVolume;

        public string Name { get; set; }

        public DynamicSoundEffectInstance DynamicSoundInstance { get; private set; }

        private string path;

        private VorbisReader Reader { get; set; }

        private Music(string name, string musicPath, float maxVolume) {
            path = musicPath;
            Name = name;
            Reader = new(Path.Combine(TankGame.Instance.Content.RootDirectory, musicPath + ".ogg"));
            this.maxVolume = maxVolume;
            DynamicSoundInstance = new(Reader.SampleRate, (AudioChannels)Reader.Channels);

            DynamicSoundInstance.BufferNeeded += DynamicSoundInstance_BufferNeeded;
        }

        private void DynamicSoundInstance_BufferNeeded(object sender, EventArgs e) {
            Reader = new(Path.Combine(TankGame.Instance.Content.RootDirectory, path + ".ogg"));
            GetBuffer();
            DynamicSoundInstance = new(Reader.SampleRate, (AudioChannels)Reader.Channels);
        }

        public static Music CreateMusicTrack(string name, string musicPath, float maxVolume) {
            return new(name, musicPath, maxVolume);
        }

        public void Play(float volume) {
            GetBuffer();
            CurrentMusicTrack = id;
            DynamicSoundInstance?.Play();
            DynamicSoundInstance.Volume = maxVolume * volume;
            OnBegin?.Invoke(this, new());
        }

        public void Pause() {
            State = MusicState.Paused;
            DynamicSoundInstance?.Pause();
        }

        public void Stop() {
            DynamicSoundInstance.Volume = 0f;
            DynamicSoundInstance?.Stop();
            OnStop?.Invoke(this, new());
        }

        public void ResetBuffer() => GetBuffer(true);

        internal void Update() {
            if (CurrentMusicTrack != id) {
                State = MusicState.Stopped;
                Stop();

                /*if (!roughTransition)
                {
                    if (volume > 0f)
                        volume -= 0.075f;
                    else
                        _instance?.Stop();
                }*/
            }
            if (CurrentMusicTrack == id) {
                DynamicSoundInstance.Volume = volume;
                State = MusicState.Playing;
                Play(1f);
                /*if (!roughTransition)
                {
                    if (volume < 1f)
                        volume += 0.075f;
                    if (volume >= 1f)
                        volume = 1f;
                }*/
                if (volume > maxVolume)
                    volume = maxVolume;
            }

            if (!TankGame.Instance.IsActive) {
                if (!DynamicSoundInstance.IsPaused()) {
                    DynamicSoundInstance?.Pause();
                }
            }
            else {
                if (DynamicSoundInstance.IsPaused()) {
                    DynamicSoundInstance?.Resume();
                }
            }
        }

        private void GetBuffer(bool reset = false) {
            const int bufferSize = 4096;
            float[] buffer = new float[bufferSize];

            int samples = Reader.ReadSamples(buffer, 0, buffer.Length);
            Reader.ReadSamples(buffer, samples, buffer.Length - samples);

            //DynamicSoundInstance.SubmitFloatBufferEXT(buffer);
        }
        public event EventHandler OnBegin;
        public event EventHandler OnStop;
    }
}