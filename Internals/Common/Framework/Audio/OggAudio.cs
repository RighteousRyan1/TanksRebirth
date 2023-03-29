using Microsoft.Xna.Framework.Audio;
using StbVorbisSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth.Internals.Common.Framework.Audio
{
    public class OggAudio : IDisposable, IAudio
    {
        public void Dispose() {
            GC.SuppressFinalize(this);
            Instance.Dispose();
            _effect.Dispose();
            IsDisposed = true;
        }

        private SoundEffect _effect;
        public SoundEffectInstance Instance;
        public bool IsDisposed { get; private set; }
        public string SongPath { get; }

        private float _backingVolume;
        
        public float Volume {
            get => _backingVolume;
            set {
                if (value > MaxVolume)
                    value = MaxVolume;
                else if (value < 0)
                    value = 0;

                _backingVolume = value;
                Instance.Volume = value;
            }
        }
        public float MaxVolume { get; set; }
        public string Name { get; set; } 
        
        public SoundState State => Instance.State;

        public void Play() {
            ThrowIfDisposed();
            Instance.Play();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)] //Inline afaik.
        private void ThrowIfDisposed() {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(OggAudio), "This object instance has been disposed.");
        }

        public void Pause() {
            ThrowIfDisposed();
            Instance.Pause();
        }

        public void Stop() {
            ThrowIfDisposed();
            Instance.Stop();
        }

        public OggAudio(string path) {
            SongPath = path;
            Load(SongPath);
            this.MaxVolume = 1f;
        }
        
        public OggAudio(string path, string audioName, float maxVolume) {
            SongPath = path;
            Name = audioName;
            Load(SongPath);
            this.MaxVolume = maxVolume;
        }
        
        public OggAudio(string path, float maxVolume) {
            SongPath = path;
            Load(SongPath);
            this.MaxVolume = maxVolume;
        }
        
        private void Load(string path) {
            var buffer = File.ReadAllBytes(path);

            var audioShort = StbVorbis.decode_vorbis_from_memory(buffer, out int sampleRate, out int channels);

            byte[] audioData_Backing = new byte[audioShort.Length * 2];
            Span<byte> audioData = audioData_Backing;
            
            ref var searchSpace = ref MemoryMarshal.GetReference(audioData);
            
            for (var i = 0; i < audioShort.Length; ++i) {
                if (i * 2 >= audioData.Length)
                    break;

                ref var currentByte = ref Unsafe.Add(ref searchSpace, i);
                ref var currentDuped = ref Unsafe.Add(ref searchSpace, i * 2);
                ref var currentDupedPlusOne = ref Unsafe.Add(ref searchSpace, (i * 2) + 1);


                currentDuped = currentByte;
                currentDupedPlusOne = (byte)(currentByte >> 8);
            }
            _effect = new SoundEffect(audioData_Backing, sampleRate, (AudioChannels)channels);
            Instance = _effect.CreateInstance();
        }

        public bool IsPaused() {
            if (_effect == null)
                return false;
            return Instance.State == SoundState.Paused;
        }
        public bool IsStopped() {
            if (_effect == null)
                return true;
            return Instance.State == SoundState.Stopped;
        }
        public bool IsPlaying() {
            if (_effect == null)
                return false;
            return Instance.State == SoundState.Playing;
        }
    }
}
