using MeltySynth;
using Microsoft.Xna.Framework.Audio;
using StbVorbisSharp;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace TanksRebirth.Internals.Common.Framework.Audio
{
    public class MidiPlayer : IDisposable
    {
        private const int DEFAULT_SAMPLE_RATE = 44100;
        private const int DEFAULT_BUFFER_LENGTH = DEFAULT_SAMPLE_RATE / 10;

        private Synthesizer synthesizer;
        private MidiFileSequencer sequencer;

        private DynamicSoundEffectInstance dynamicSound;
        private byte[] buffer;

        public MidiPlayer(string soundFontPath, SynthesizerSettings settings = null) {
            dynamicSound = new DynamicSoundEffectInstance(DEFAULT_SAMPLE_RATE, AudioChannels.Stereo);
            buffer = new byte[4 * DEFAULT_BUFFER_LENGTH];
            dynamicSound.BufferNeeded += (_, _) => SubmitBuffer();

            if (settings is null) {
                synthesizer = new Synthesizer(soundFontPath, DEFAULT_SAMPLE_RATE); //  Settings were not provided, use the default sample rate.
                sequencer = new MidiFileSequencer(synthesizer);
                return;
            }
            settings = new(DEFAULT_SAMPLE_RATE);
            synthesizer = new Synthesizer(soundFontPath, settings);
            sequencer = new MidiFileSequencer(synthesizer);
        }

        public void Play(MidiFile midiFile, bool loop) {
            sequencer.Play(midiFile, loop);

            if (dynamicSound.State == SoundState.Playing) return;
            SubmitBuffer();
            dynamicSound.Play();
        }

        public void Stop() {
            sequencer.Stop();
        }

        private void SubmitBuffer() {
            sequencer.RenderInterleavedInt16(MemoryMarshal.Cast<byte, short>(buffer));
            dynamicSound.SubmitBuffer(buffer, 0, buffer.Length);
        }

        public void Dispose() {
            if (dynamicSound == null) return;
            dynamicSound.Dispose();
            dynamicSound = null;
            GC.SuppressFinalize(this);
        }

        public void NoteOn(int channel, int key, int velocity) {
            synthesizer.NoteOn(channel, key, velocity);
        }

        public void NoteOff(int channel, int key) {
            synthesizer.NoteOff(channel, key);
        }

        public void NoteOffAll() {
            synthesizer.NoteOffAll(true);
        }

        public SoundState State => dynamicSound.State;
    }
}