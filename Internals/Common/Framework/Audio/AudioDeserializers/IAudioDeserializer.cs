using System;
using System.Threading.Tasks;

namespace TanksRebirth.Internals.Common.Framework.Audio.AudioSerializers;

public interface IAudioDeserializer {
    public Task<DeseralizationData> DeserializeAsync(string path);
    public DeseralizationData Deserialize(string path);
}