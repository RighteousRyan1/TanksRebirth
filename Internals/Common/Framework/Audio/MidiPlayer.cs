using MeltySynth;
using Microsoft.Xna.Framework.Audio;
using NVorbis;
using System;
using System.Runtime.InteropServices;

namespace WiiPlayTanksRemake.Internals.Common.Framework.Audio
{
    public class OggSound : IDisposable
    {
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        private static readonly int sampleRate = 44100;
        private static readonly int bufferLength = sampleRate / 10;

        private DynamicSoundEffectInstance dynamicSound;

        private byte[] byteBuffer;
        private float[] floatBuffer;

        private VorbisReader reader;

        public OggSound(string path)
        {
            dynamicSound = new DynamicSoundEffectInstance(sampleRate, AudioChannels.Stereo);
            byteBuffer = new byte[4 * bufferLength];
            floatBuffer = new float[4 * bufferLength];

            reader = new(path + ".ogg");

            int samples = reader.ReadSamples(floatBuffer, 0, bufferLength);
            reader.ReadSamples(floatBuffer, samples, bufferLength - samples);

            dynamicSound.BufferNeeded += (s, e) => SubmitBuffer();
        }
        private void SubmitBuffer()
        {
            dynamicSound.SubmitBuffer(byteBuffer, 0, byteBuffer.Length);
        }

        public void Play()
            => dynamicSound?.Play();
        public void Pause()
            => dynamicSound?.Pause();
        public void Resume()
            => dynamicSound?.Resume();
        public void SetVolume(float volume)
            => dynamicSound.Volume = volume;
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