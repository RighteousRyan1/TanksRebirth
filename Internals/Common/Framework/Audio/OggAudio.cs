using Microsoft.Xna.Framework.Audio;
using StbVorbisSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TanksRebirth.Internals.Common.Framework.Audio.AudioSerializers;
using TanksRebirth.Internals.Common.Utilities;

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

    public float MaxVolume { get; set; }
    public string Name { get; set; }

    public SoundState State => Instance.State;

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
        this.Path = path;
        this._effect = effect;
        this.Instance = effect.CreateInstance();
    }
    public OggAudio(string path) {
        Path = path;
        Load(Path);
        this.MaxVolume = 1f;
    }

    public OggAudio(string path, string audioName, float maxVolume) {
        Path = path;
        Name = audioName;
        Load(Path);
        this.MaxVolume = maxVolume;
    }

    public OggAudio(string path, float maxVolume) {
        Path = path;
        Load(Path);
        this.MaxVolume = maxVolume;
    }

    private void Load(string path) {
        var soundEffect = GameResources.GetGameResource<SoundEffect>(path);
        if (soundEffect != null) {
            _effect = soundEffect;
            Instance = soundEffect.CreateInstance();
            return;
        }
        
        var audioData = new OggDeserializer().Deserialize(path);
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