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
        public void Dispose() => GC.SuppressFinalize(this);

        private SoundEffect _effect;
        public SoundEffectInstance Instance;
        public string SongPath { get; }

        public string Name { get; set; }

        public OggAudio(string path)
        {
            SongPath = path;
            Load(SongPath);
        }
        private void Load(string path)
        {
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

        public bool IsPaused()
        {
            if (_effect == null)
                return false;
            return Instance.State == SoundState.Paused;
        }
        public bool IsStopped()
        {
            if (_effect == null)
                return true;
            return Instance.State == SoundState.Stopped;
        }
        public bool IsPlaying()
        {
            if (_effect == null)
                return false;
            return Instance.State == SoundState.Playing;
        }
    }
}
