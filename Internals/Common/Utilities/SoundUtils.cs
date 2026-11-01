using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using System;
using System.Diagnostics;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.Internals.Common.Framework.Audio;

namespace TanksRebirth.Internals.Common.Utilities;

public struct SpatialParams(float pan, float volume) {
    public float Pan = pan;
    public float Volume = volume;
    // ... pitch would be funny for a pseudo-doppler effect
}
public static class SoundUtils {
    /// <summary>How far a sound pans at the left or right edge of the screen in orthographic view.</summary>
    public const float ORTHO_PAN_STRENGTH = 0.3f;
    /// <summary>How far a sound pans in perspective view when it is directly to the side of the camera.</summary>
    public const float PERSPECTIVE_PAN_STRENGTH = 0.8f;
    /// <summary>Where the listener stands in orthographic view, for sounds that fade by distance there.</summary>
    public static readonly Vector3 BoardListenerPosition = Vector3.Zero;

    /// <summary>The pan and volume a sound at <paramref name="position"/> should have for the current camera.</summary>
    /// <remarks>
    /// In orthographic view the sound pans by where it is on the screen, and only fades when <paramref name="orthographicFade"/>
    /// is set, measured from <see cref="BoardListenerPosition"/>. In perspective view it pans by its direction from the camera
    /// and fades out between <paramref name="minDist"/> and <paramref name="maxDist"/>.
    /// </remarks>
    public static SpatialParams GetSpatialParams(Vector3 position, float minDist, float maxDist, bool orthographicFade = false) {
        var view = CameraGlobals.GameView;
        var projection = CameraGlobals.GameProjection;
        float volume, pan;

        // cool way of getting normalized screen coordinates of a world position. should probably methodize this.
        if (MatrixUtils.IsOrthographic(projection)) {
            var clip = Vector4.Transform(new Vector4(position, 1f), view * projection);
            pan = MathHelper.Clamp(clip.X / clip.W, -1f, 1f) * ORTHO_PAN_STRENGTH;
            volume = orthographicFade ? DistanceFade(Vector3.Distance(BoardListenerPosition, position), minDist, maxDist) : 1f;
            return new(pan, volume);
        }

        var camera = Matrix.Invert(view);
        var toSound = position - camera.Translation;
        var distance = toSound.Length();
        // left to right pan detection... dot product or basic ahh math?
        pan = distance > 0.001f ? Vector3.Dot(toSound / distance, camera.Right) * PERSPECTIVE_PAN_STRENGTH : 0f;

        // i could have opted for a linear falloff but a smooth falloff is better
        volume = DistanceFade(distance, minDist, maxDist);
        return new(pan, volume);
    }
    /// <summary>Full volume up to <paramref name="minDist"/>, then drops quickly and tails off into silence at <paramref name="maxDist"/>.</summary>
    static float DistanceFade(float distance, float minDist, float maxDist) {
        var t = MathUtils.InverseLerp(minDist, maxDist, distance, clamped: true);
        return Easings.InQuad(1f - t);
    }
    public static bool IsPlaying(this SoundEffectInstance instance) => instance.State == SoundState.Playing;
    public static bool IsPaused(this SoundEffectInstance instance) => instance.State == SoundState.Paused;
    public static bool IsStopped(this SoundEffectInstance instance) => instance.State == SoundState.Stopped;

    public static float GetVolumeFromScreenPosition(Vector2 pos) {
        var volumeY = MathUtils.TriangleWave(pos.Y, -200, WindowUtils.WindowHeight + 200);
        var volumeX = MathUtils.TriangleWave(pos.X, -200, WindowUtils.WindowWidth + 200);
        return volumeY * volumeX;
    }
    public static float GetVolumeFromCameraPosition(Vector3 sourcePos, Vector3 camPos, float maxSoundDist = 800f) {
        var dist = 1f - Vector3.Distance(camPos, sourcePos) / maxSoundDist;
        dist = MathHelper.Clamp(dist, 0, 1);

        return dist;
    }
}