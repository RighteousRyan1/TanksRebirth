using System;

namespace TanksRebirth.Internals.Common.Framework.Audio.AudioSerializers;

public interface IAudioDeserializer {
    public DeseralizationData Deserialize(string path);
}