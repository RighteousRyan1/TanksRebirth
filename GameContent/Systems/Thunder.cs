using Microsoft.Xna.Framework.Audio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TanksRebirth.Graphics;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common.Framework.Audio;

namespace TanksRebirth.GameContent.Systems;

public class Thunder {
    public static OggAudio? SoftRain;

    public static Thunder[] Thunders = new Thunder[20];

    public float MaxBright;
    public float CurBright { get; private set; }

    public float AppearSpeed;
    public float FadeSpeed;
    public int TickDelay;
    public int LingerTime;

    private bool _maxed;

    private readonly int _id = -1;

    public readonly ThunderType Type;

    public bool Active => CurBright > 0;

    public enum ThunderType {
        Fast,
        GradualFast,
        Instant,
        Instant2
    }
    /// <summary>
    /// Creates and immediately starts a new <see cref="Thunder"/>.
    /// </summary>
    /// <param name="soundPath">The sound this <see cref="Thunder"/> plays upon creation.</param>
    /// <param name="maxBright">The maximum brightness this <see cref="Thunder"/> creates.</param>
    /// <param name="appearSpeed">How fast this <see cref="Thunder"/> comes to fruition.</param>
    /// <param name="fadeSpeed">How fast this <see cref="Thunder"/> fades out after <paramref name="lingerTime"/>.</param>
    /// <param name="tickDelay">How long after this <see cref="Thunder"/> is created before it starts brightening the scene. Before, it brightens at 1/4 the speed.</param>
    /// <param name="lingerTime">How long this <see cref="Thunder"/> lingers at max brightness before darkening.</param>
    public Thunder(string soundPath, float maxBright, float appearSpeed, float fadeSpeed, int tickDelay, int lingerTime) {
        MaxBright = maxBright;
        AppearSpeed = appearSpeed;
        FadeSpeed = fadeSpeed;
        TickDelay = tickDelay;
        LingerTime = lingerTime;

        SoundPlayer.PlaySoundInstance(soundPath, SoundContext.Effect, 1f);

        int index = Array.IndexOf(Thunders, Thunders.First(thunder => thunder is null));

        _id = index;

        Thunders[index] = this;
    }
    /// <summary>
    /// Creates and immediately starts a new <see cref="Thunder"/> from a list of <see cref="Thunder"/> prefabs.
    /// </summary>
    /// <param name="type">The type of <see cref="Thunder"/>. </param>
    public Thunder(ThunderType type) {
        Type = type;

        switch (type) {
            case ThunderType.Fast:
                MaxBright = 0.4f;
                AppearSpeed = 0.05f;
                FadeSpeed = 0.0025f;
                TickDelay = 30;
                LingerTime = 60;

                SoundPlayer.PlaySoundInstance("Assets/sounds/thunder/fast.ogg", SoundContext.Effect, 1f);
                break;
            case ThunderType.GradualFast:
                MaxBright = 0.2f;
                AppearSpeed = 0.02f;
                FadeSpeed = 0.0005f;
                TickDelay = 40;
                LingerTime = 0;

                SoundPlayer.PlaySoundInstance("Assets/sounds/thunder/gradual_fast.ogg", SoundContext.Effect, 1f);
                break;
            case ThunderType.Instant:
                MaxBright = 0.6f;
                AppearSpeed = 0.3f;
                FadeSpeed = 0.002f;
                TickDelay = 5;
                LingerTime = 40;

                SoundPlayer.PlaySoundInstance("Assets/sounds/thunder/instant.ogg", SoundContext.Effect, 1f);
                break;
            case ThunderType.Instant2:
                MaxBright = 1f;
                AppearSpeed = 0.2f;
                FadeSpeed = 0.002f;
                TickDelay = 5;
                LingerTime = 40;

                SoundPlayer.PlaySoundInstance("Assets/sounds/thunder/instant_2.ogg", SoundContext.Effect, 1f);
                break;
        }

        int index = Array.IndexOf(Thunders, Thunders.First(thunder => thunder is null));

        _id = index;

        Thunders[index] = this;
    }

    public void Update() {
        if (LingerTime > 0)
            LingerTime--;
        else
            CurBright -= FadeSpeed;

        if (TickDelay > 0)
            TickDelay--;

        if (!_maxed)
            if (TickDelay <= 0)
                CurBright += AppearSpeed;


        if (CurBright >= MaxBright)
            _maxed = true;

        if (_maxed && CurBright <= 0)
            Remove();
    }
    private void Remove() {
        Thunders[_id] = null;
    }
    public override string ToString() {
        StringBuilder sb = new();
        sb.Append('{');
        sb.Append($"MaxBright: {MaxBright} | ");
        sb.Append($"_curBright: {CurBright} | ");
        sb.Append($"AppearSpeed: {AppearSpeed} | ");
        sb.Append($"TickDelay: {TickDelay} | ");
        sb.Append($"LingerTime: {LingerTime} | ");
        sb.Append($"_maxed: {_maxed}");
        sb.Append('}');
        return sb.ToString();
    }
}
