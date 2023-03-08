using System;
using System.Security.Cryptography;

namespace TanksRebirth.Internals.Common.Utilities;
#pragma warning disable
public static class Easings {
    public static float Linear(float t) => t;
    public static float InQuad(float t) => t * t;
    public static float OutQuad(float t) => 1 - InQuad(1 - t);

    public static float InOutQuad(float t) {
        if (t < 0.5) return InQuad(t * 2) / 2;
        return 1 - InQuad((1 - t) * 2) / 2;
    }

    public static float InCubic(float t) => t * t * t;
    public static float OutCubic(float t) => 1 - InCubic(1 - t);

    public static float InOutCubic(float t) {
        if (t < 0.5) return InCubic(t * 2) / 2;
        return 1 - InCubic((1 - t) * 2) / 2;
    }

    public static float InQuart(float t) => t * t * t * t;
    public static float OutQuart(float t) => 1 - InQuart(1 - t);

    public static float InOutQuart(float t) {
        if (t < 0.5) return InQuart(t * 2) / 2;
        return 1 - InQuart((1 - t) * 2) / 2;
    }

    public static float InQuint(float t) => t * t * t * t * t;
    public static float OutQuint(float t) => 1 - InQuint(1 - t);

    public static float InOutQuint(float t) {
        if (t < 0.5) return InQuint(t * 2) / 2;
        return 1 - InQuint((1 - t) * 2) / 2;
    }

    public static float InSine(float t) => (float)-Math.Cos(t * Math.PI / 2);
    public static float OutSine(float t) => (float)Math.Sin(t * Math.PI / 2);
    public static float InOutSine(float t) => (float)(Math.Cos(t * Math.PI) - 1) / -2;

    public static float InExpo(float t) => (float)Math.Pow(2, 10 * (t - 1));
    public static float OutExpo(float t) => 1 - InExpo(1 - t);

    public static float InOutExpo(float t) {
        if (t < 0.5) return InExpo(t * 2) / 2;
        return 1 - InExpo((1 - t) * 2) / 2;
    }

    public static float InCirc(float t) => -((float)Math.Sqrt(1 - t * t) - 1);
    public static float OutCirc(float t) => 1 - InCirc(1 - t);

    public static float InOutCirc(float t) {
        if (t < 0.5) return InCirc(t * 2) / 2;
        return 1 - InCirc((1 - t) * 2) / 2;
    }

    public static float InElastic(float t) => 1 - OutElastic(1 - t);

    public static float OutElastic(float t) {
        const float p = 0.3f;
        return (float)Math.Pow(2, -10 * t) * (float)Math.Sin((t - p / 4) * (2 * Math.PI) / p) + 1;
    }

    public static float InOutElastic(float t) {
        if (t < 0.5) return InElastic(t * 2) / 2;
        return 1 - InElastic((1 - t) * 2) / 2;
    }

    public static float InBack(float t) {
        const float s = 1.70158f;
        return t * t * ((s + 1) * t - s);
    }

    public static float OutBack(float t) => 1 - InBack(1 - t);

    public static float InOutBack(float t) {
        if (t < 0.5) return InBack(t * 2) / 2;
        return 1 - InBack((1 - t) * 2) / 2;
    }

    public static float InBounce(float t)
        => 1 - OutBounce(1 - t);

    public static float OutBounce(float t) {
        const float DIVISION_FACTOR = 2.75f;
        const float MULTIPLICATION_FACTOR = 7.5625f;

        switch (t) {
            case < 1 / DIVISION_FACTOR:
                return MULTIPLICATION_FACTOR * t * t;
            case < 2 / DIVISION_FACTOR:
                t -= 1.5f / DIVISION_FACTOR;
                return MULTIPLICATION_FACTOR * t * t + 0.75f;
            case < 2.5f / DIVISION_FACTOR:
                t -= 2.25f / DIVISION_FACTOR;
                return MULTIPLICATION_FACTOR * t * t + 0.9375f;
            default:
                t -= 2.625f / DIVISION_FACTOR;
                return MULTIPLICATION_FACTOR * t * t + 0.984375f;
        }
    }

    public static float InOutBounce(float t) {
        if (t < 0.5) return InBounce(t * 2) / 2;
        return 1 - InBounce((1 - t) * 2) / 2;
    }

    public static float GetEasingBehavior(this EasingType easingType, float easeValue) {
        return easingType switch {
            EasingType.Linear => Easings.Linear(easeValue),
            EasingType.InQuad => Easings.InQuad(easeValue),
            EasingType.OutQuad => Easings.OutQuad(easeValue),
            EasingType.InOutQuad => Easings.InOutQuad(easeValue),
            EasingType.InCubic => Easings.InCubic(easeValue),
            EasingType.OutCubic => Easings.OutCubic(easeValue),
            EasingType.InOutCubic => Easings.InOutCubic(easeValue),
            EasingType.InQuart => Easings.InQuart(easeValue),
            EasingType.OutQuart => Easings.OutQuart(easeValue),
            EasingType.InOutQuart => Easings.InOutQuart(easeValue),
            EasingType.InQuint => Easings.InQuint(easeValue),
            EasingType.OutQuint => Easings.OutQuint(easeValue),
            EasingType.InOutQuint => Easings.InOutQuint(easeValue),
            EasingType.InSine => Easings.InSine(easeValue),
            EasingType.OutSine => Easings.OutSine(easeValue),
            EasingType.InOutSine => Easings.InOutSine(easeValue),
            EasingType.InExpo => Easings.InExpo(easeValue),
            EasingType.OutExpo => Easings.OutExpo(easeValue),
            EasingType.InOutExpo => Easings.InOutExpo(easeValue),
            EasingType.InCirc => Easings.InCirc(easeValue),
            EasingType.OutCirc => Easings.OutCirc(easeValue),
            EasingType.InOutCirc => Easings.InOutCirc(easeValue),
            EasingType.InElastic => Easings.InElastic(easeValue),
            EasingType.OutElastic => Easings.OutElastic(easeValue),
            EasingType.InOutElastic => Easings.InOutElastic(easeValue),
            EasingType.InBack => Easings.InBack(easeValue),
            EasingType.OutBack => Easings.OutBack(easeValue),
            EasingType.InOutBack => Easings.InOutBack(easeValue),
            EasingType.InBounce => Easings.InBounce(easeValue),
            EasingType.OutBounce => Easings.OutBounce(easeValue),
            EasingType.InOutBounce => Easings.InOutBounce(easeValue),
            _ => throw new Exception("Unknown easing type " + easingType)
        };
    }
}

public enum EasingType {
    Linear,
    InQuad,
    OutQuad,
    InOutQuad,
    InCubic,
    OutCubic,
    InOutCubic,
    InQuart,
    OutQuart,
    InOutQuart,
    InQuint,
    OutQuint,
    InOutQuint,
    InSine,
    OutSine,
    InOutSine,
    InExpo,
    OutExpo,
    InOutExpo,
    InCirc,
    OutCirc,
    InOutCirc,
    InElastic,
    OutElastic,
    InOutElastic,
    InBack,
    OutBack,
    InOutBack,
    InBounce,
    OutBounce,
    InOutBounce
}