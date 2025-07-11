using Microsoft.Xna.Framework.Audio;
using System;
using System.Runtime.CompilerServices;
using TanksRebirth.Internals.Common.Framework.Audio.AudioSerializers;

namespace TanksRebirth.Internals.Common.Framework.Audio;

public class OggAudio : IDisposable, IAudio {
    private SoundEffect? _effect;
    private float _backingVolume;

    [MethodImpl(MethodImplOptions.AggressiveInlining)] //Inline afaik.
    private void ThrowIfDisposed() {
        if (IsDisposed)
            throw new ObjectDisposedException(nameof(OggAudio), "This object instance has been disposed.");
    }

    public SoundEffectInstance Instance;
    public bool IsDisposed { get; private set; }
    public string Path { get; }
    public float Volume {
        get => _backingVolume;
        set {
            // changed from capping at MaxVolume to just multiplying volume by maxvolume.
            if (value > 1f)
                value = 1f;
            else if (value < 0)
                value = 0f;

            _backingVolume = value;
            Instance.Volume = value * MaxVolume;
        }
    }

    float _maxVolume;
    public float MaxVolume {
        // maybe causes volume decay if called constantly...?
        get => _maxVolume;
        set {
            _maxVolume = value;

            // intentional...? to call the setter. weird code but whatever
            Volume = Volume;
        }
    }
    public string Name { get; set; }

    public SoundState State => Instance.State;

    public static OggDeserializer _oggDeserializer = new();

    public void Play() {
        ThrowIfDisposed();
        Instance.Play();
    }

    public void Pause() {
        ThrowIfDisposed();
        Instance.Pause();
    }

    public void Stop() {
        ThrowIfDisposed();
        Instance.Stop();
    }

    public OggAudio(string path, SoundEffect effect) {
        Path = path;
        _effect = effect;
        Instance = effect.CreateInstance();
    }
    public OggAudio(string path) {
        Path = path;
        Load(Path);
        MaxVolume = 1f;
    }

    public OggAudio(string path, string audioName, float maxVolume) {
        Path = path;
        Name = audioName;
        Load(Path);
        MaxVolume = maxVolume;
    }

    public OggAudio(string path, float maxVolume) {
        Path = path;
        Load(Path);
        MaxVolume = maxVolume;
    }

    private void Load(string path, bool compressToMono = false) {
        var soundEffect = GameResources.GetGameResource<SoundEffect>(path);
        if (soundEffect != null) {
            _effect = soundEffect;
            Instance = soundEffect.CreateInstance();
            return;
        }
        
        var audioData = _oggDeserializer.Deserialize(path);

        if (compressToMono) {
            if (audioData.channelCount == 2) {

                // average left and right channels lol
                var monoData = new byte[audioData.binaryData.Length / 2];
                for (int i = 0; i < monoData.Length; i += 2) {
                    short left = BitConverter.ToInt16(audioData.binaryData, i * 2);
                    short right = BitConverter.ToInt16(audioData.binaryData, i * 2 + 2);
                    short mono = (short)((left + right) / 2);
                    byte[] monoBytes = BitConverter.GetBytes(mono);
                    monoData[i] = monoBytes[0];
                    monoData[i + 1] = monoBytes[1];
                }
                audioData.binaryData = monoData;
                audioData.channelCount = 1;
            }
        }

        _effect = new SoundEffect(audioData.binaryData, audioData.sampleRate, (AudioChannels)audioData.channelCount);
        Instance = _effect.CreateInstance();
    }

    public bool IsPaused() {
        if (_effect == null)
            return false;
        return State == SoundState.Paused;
    }

    public bool IsStopped() {
        if (_effect == null)
            return true;
        return State == SoundState.Stopped;
    }

    public bool IsPlaying() {
        if (_effect == null)
            return false;
        return State == SoundState.Playing;
    }

    public void Dispose() {
        GC.SuppressFinalize(this);
        Instance.Dispose();
        _effect?.Dispose();
        IsDisposed = true;
    }
}