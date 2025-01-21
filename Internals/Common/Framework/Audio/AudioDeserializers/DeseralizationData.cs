namespace TanksRebirth.Internals.Common.Framework.Audio.AudioSerializers;

public struct DeseralizationData {
    public byte[] binaryData;
    public int channelCount;
    public int sampleRate;

    public DeseralizationData(byte[] binaryData, int channelCount, int sampleRate) {
        this.binaryData = binaryData;
        this.channelCount = channelCount;
        this.sampleRate = sampleRate;
    }
}