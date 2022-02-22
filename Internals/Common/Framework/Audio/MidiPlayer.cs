using MeltySynth;
using Microsoft.Xna.Framework.Audio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace WiiPlayTanksRemake.Internals.Common.Framework.Audio
{
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