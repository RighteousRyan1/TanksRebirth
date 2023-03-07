using Microsoft.Xna.Framework.Audio;
using StbVorbisSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth.Internals.Common.Framework.Audio
{
    public class OggAudio : IDisposable
    {
        public void Dispose() {
            GC.SuppressFinalize(this);
            // Release resources
            Instance.Dispose();
            _effect.Dispose(); 
            IsDisposed = true;
            Instance = null;
            _effect = null;
        }

        private SoundEffect _effect;
        public SoundEffectInstance Instance;
        public bool IsDisposed { get; private set; }
        public string SongPath { get; }

        public string Name { get; set; }

        public OggAudio(string path) {
            SongPath = path;
            Load(SongPath);
        }
        private void Load(string path) {
            var buffer = File.ReadAllBytes(path);

            var audioShort = StbVorbis.decode_vorbis_from_memory(buffer, out int sampleRate, out int channels);

            byte[] audioData = new byte[audioShort.Length * 2];
            for (var i = 0; i < audioShort.Length; ++i)
            {
                if (i * 2 >= audioData.Length)
                    break;

                short tempShort = audioShort[i];

                audioData[i * 2] = (byte)tempShort;
                audioData[i * 2 + 1] = (byte)(tempShort >> 8);
            }
            _effect = new SoundEffect(audioData, sampleRate, (AudioChannels)channels);
            Instance = _effect.CreateInstance();
        }

        private void ThrowIfDisposed() { // Helper throw method.
            if (!IsDisposed) return;
            throw new ObjectDisposedException(Name, "This sound effect instance with the name of {Name} file has been already disposed!");
        }
        /// <summary>
        ///     Verifies if the sound is paused.
        /// </summary>
        /// <exception cref="ObjectDisposedException">This exception will be thrown if this audio instance has been disposed of.</exception>
        /// <returns>True if the sound is currently paused. False if it is not.</returns>
        public bool IsPaused() {
            ThrowIfDisposed();
            if (_effect == null)
                return false;
            return Instance.State == SoundState.Paused;
        }
        /// <summary>
        ///     Verifies if the sound has been stopped.
        /// </summary>
        /// <exception cref="ObjectDisposedException">This exception will be thrown if this audio instance has been disposed of.</exception>
        /// <returns>True if the sound has stopped playing. False if it is playing.</returns>
        public bool HasStopped() {
            ThrowIfDisposed();
            if (_effect == null)
                return true;
            return Instance.State == SoundState.Stopped;
        }
        /// <summary>
        ///     Verifies if the sound is currently playing.
        /// </summary>
        /// <exception cref="ObjectDisposedException">This exception will be thrown if this audio instance has been disposed of.</exception>
        /// <returns>True if the sound is playing. False if it is not playing.</returns>
        public bool IsPlaying() {
            ThrowIfDisposed();
            if (_effect == null)
                return false;
            return Instance.State == SoundState.Playing;
        }
    }
}
