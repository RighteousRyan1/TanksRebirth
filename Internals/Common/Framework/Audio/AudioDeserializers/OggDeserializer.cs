using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using StbVorbisSharp;

namespace TanksRebirth.Internals.Common.Framework.Audio.AudioSerializers;

public class OggDeserializer : IAudioDeserializer {
    DeseralizationData DeserializeInternal(short[] vorbisData,int sampleRate, int channels) {
        var audioData = new byte[vorbisData.Length * 2];

        ref var shortSearchSpace = ref MemoryMarshal.GetArrayDataReference(vorbisData);
        ref var searchSpace = ref MemoryMarshal.GetReference(audioData.AsSpan());

        // The following converts a Short into a byte in a convoluted way, enjoyyyyy.
        const float channelCount = 2f;
        for (var i = 0; i < vorbisData.Length && i * 2 <= audioData.Length; i++) {

            ref var currentShort = ref Unsafe.Add(ref shortSearchSpace, i);
            ref var currentDuped = ref Unsafe.Add(ref searchSpace, (int)(i * channelCount));
            ref var currentDupedPlusOne = ref Unsafe.Add(ref searchSpace, ((int)(i * channelCount)) + 1);

            unsafe {
                Unsafe.Write(Unsafe.AsPointer(ref currentDuped), (byte)currentShort);
                Unsafe.Write(Unsafe.AsPointer(ref currentDupedPlusOne), (byte)(currentShort >> 8));
            }
        }

        return new DeseralizationData(audioData, channels, sampleRate);
    }
    public async Task<DeseralizationData> DeserializeAsync(string path) {
        var buffer = await File.ReadAllBytesAsync(path);
        var audioShort = StbVorbis.decode_vorbis_from_memory(buffer, out var sampleRate, out var channels);
        return DeserializeInternal(audioShort, sampleRate, channels);
    }
    
    public DeseralizationData Deserialize(string path) {
        var buffer = File.ReadAllBytes(path);
        var audioShort = StbVorbis.decode_vorbis_from_memory(buffer, out var sampleRate, out var channels);

        return DeserializeInternal(audioShort, sampleRate, channels);
    }
}