using MeltySynth;
using Microsoft.Xna.Framework.Audio;
using StbVorbisSharp;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace TanksRebirth.Internals.Common.Framework.Audio
{
    public class OggAudio : IDisposable
    {
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        public SoundState State
        {
            get
            {
                if (_effect != null)
                    return _effect.State;
                else
                    return SoundState.Stopped;
            }
        }

        private Vorbis _vorbis;
        private DynamicSoundEffectInstance _effect;
        private bool _startedPlaying;

        public float Volume;

        public string SongPath { get; }

        public OggAudio(string path)
        {
            SongPath = path + ".ogg";
        }
        private void SubmitBuffer()
        {
            _vorbis.SubmitBuffer();

            if (_vorbis.Decoded == 0)
            {
                // Restart
                _vorbis.Restart();
                _vorbis.SubmitBuffer();
            }

            var audioShort = _vorbis.SongBuffer;
            byte[] audioData = new byte[_vorbis.Decoded * _vorbis.Channels * 2];
            for (var i = 0; i < _vorbis.Decoded * _vorbis.Channels; ++i)
            {
                /*if (i * 2 >= audioData.Length)
                {
                    break;
                }

                var b1 = (byte)(audioShort[i] >> 8);
                var b2 = (byte)(audioShort[i] & 256);

                audioData[i * 2] = b2;
                audioData[i * 2 + 1] = b1;*/

                short tempShort = audioShort[i];

                audioData[i * 2] = (byte)tempShort;
                audioData[i * 2 + 1] = (byte)(tempShort >> 8);
            }

            _effect.SubmitBuffer(audioData);
        }
        private void LoadSong()
        {
            var buffer = File.ReadAllBytes(SongPath);

            _vorbis = Vorbis.FromMemory(buffer);

            _effect = new DynamicSoundEffectInstance(_vorbis.SampleRate, (AudioChannels)_vorbis.Channels)
            {
                Volume = Volume
            };

            _effect.BufferNeeded += (s, a) => SubmitBuffer();

            SubmitBuffer();
        }

        public void Play()
        {
            if (!_startedPlaying)
            {
                LoadSong();
                _effect.Play();
                _startedPlaying = true;
            }
        }
        public void Stop()
        {
            if (_startedPlaying)
            {
                _effect.Stop();
                _startedPlaying = false;
            }
        }
        public void Pause()
            => _effect?.Pause(); 
        public void Resume()
            => _effect?.Resume();
        public void SetVolume(float volume) {
            if (_effect == null)
                return;
            _effect.Volume = volume;
        }

        public bool IsPaused() {
            if (_effect == null)
                return false; 
            return _effect.State == SoundState.Paused;
        }
        public bool IsStopped()
        {
            if (_effect == null)
                return true;
            return _effect.State == SoundState.Stopped;
        }
        public bool IsPlaying()
        {
            if (_effect == null)
                return false;
            return _effect.State == SoundState.Playing;
        }
    }
    public class MidiPlayer : IDisposable
    {
        private static readonly int sampleRate = 44100;
        private static readonly int bufferLength = sampleRate / 10;

        private Synthesizer synthesizer;
        private MidiFileSequencer sequencer;

        private DynamicSoundEffectInstance dynamicSound;
        private byte[] buffer;

        public MidiPlayer(string soundFontPath, SynthesizerSettings settings = null)
        {
            if (settings is not null)
            {
                settings.SampleRate = sampleRate;
                synthesizer = new Synthesizer(soundFontPath, settings);
            }
            else
                synthesizer = new Synthesizer(soundFontPath, settings);
            sequencer = new MidiFileSequencer(synthesizer);

            dynamicSound = new DynamicSoundEffectInstance(sampleRate, AudioChannels.Stereo);
            buffer = new byte[4 * bufferLength];

            dynamicSound.BufferNeeded += (s, e) => SubmitBuffer();
        }

        public void Play(MidiFile midiFile, bool loop)
        {
            sequencer.Play(midiFile, loop);

            if (dynamicSound.State != SoundState.Playing)
            {
                SubmitBuffer();
                dynamicSound.Play();
            }
        }

        public void Stop()
        {
            sequencer.Stop();
        }

        private void SubmitBuffer()
        {
            sequencer.RenderInterleavedInt16(MemoryMarshal.Cast<byte, short>(buffer));
            dynamicSound.SubmitBuffer(buffer, 0, buffer.Length);
        }

        public void Dispose()
        {
            if (dynamicSound != null)
            {
                dynamicSound.Dispose();
                dynamicSound = null;
            }
        }

        public void NoteOn(int channel, int key, int velocity)
        {
            synthesizer.NoteOn(channel, key, velocity);
        }

        public void NoteOff(int channel, int key)
        {
            synthesizer.NoteOff(channel, key);
        }

        public void NoteOffAll()
        {
            synthesizer.NoteOffAll(true);
        }

        public SoundState State => dynamicSound.State;
    }
}