using Microsoft.Xna.Framework.Audio;

namespace TanksRebirth.Internals.Common.Framework.Audio;

public class OggMusic : IAudio {
    private float _vol;

    public float Volume {
        get => _vol;
        set {
            if (value > MaxVolume)
                value = MaxVolume;
            else if (value < 0)
                value = 0;

            _vol = value;
            BackingAudio.Instance.Volume = value;
        }
    }

    public float MaxVolume { get; set; }
    public SoundState State => BackingAudio.Instance.State;
    public string Name { get; set; }
    public OggAudio BackingAudio { get; private set; }
    
    public OggMusic(string name, OggAudio backingAudio, float maxVolume) {
        BackingAudio = backingAudio;
        Name = name;
        MaxVolume = maxVolume;
        BackingAudio.Instance.IsLooped = true;
        BackingAudio.Instance.Volume = 0;
    }
    
    public OggMusic(string name, string songPath, float maxVolume) {
        BackingAudio = new(songPath);
        Name = name;
        MaxVolume = maxVolume;
        BackingAudio.Instance.IsLooped = true;
        BackingAudio.Instance.Volume = 0;
    }

    public void Play()
        => BackingAudio.Instance.Play();

    public void Pause()
        => BackingAudio?.Instance.Pause();

    public void Resume()
        => BackingAudio?.Instance.Resume();

    public void SetVolume(float volume)
        => Volume = volume;

    public void Stop()
        => BackingAudio?.Instance.Stop();

    public bool IsPaused()
        => BackingAudio.Instance.State == SoundState.Paused;

    public bool IsStopped()
        => BackingAudio.Instance.State == SoundState.Stopped;

    public bool IsPlaying()
        => BackingAudio.Instance.State == SoundState.Playing;
}