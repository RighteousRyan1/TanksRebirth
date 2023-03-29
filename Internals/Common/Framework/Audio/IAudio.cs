using Microsoft.Xna.Framework.Audio;

namespace TanksRebirth.Internals.Common.Framework.Audio;

public interface IAudio  {
    float Volume { get; set; }
    float MaxVolume { get; set; }
    string Name { get; set; }
    SoundState State { get; }
    void Play();
    void Pause();
    void Stop();
}