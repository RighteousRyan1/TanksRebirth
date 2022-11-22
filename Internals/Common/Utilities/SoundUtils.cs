using Microsoft.Xna.Framework.Audio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TanksRebirth.Internals.Common.Utilities;

public static class SoundUtils
{
    public static bool IsPlaying(this SoundEffectInstance instance) => instance.State == SoundState.Playing;
    public static bool IsPaused(this SoundEffectInstance instance) => instance.State == SoundState.Paused;
    public static bool IsStopped(this SoundEffectInstance instance) => instance.State == SoundState.Stopped;
}
