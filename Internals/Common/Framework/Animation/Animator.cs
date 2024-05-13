using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static TanksRebirth.Internals.Common.Utilities.TweenUtils;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth.Internals.Common.Framework.Animation;

public class Animator
{
    public static List<Animator> Animators = new();

    private TimeSpan _elapsedOffset;
    private TimeSpan _elapsedInternal;
    public TimeSpan ElapsedTime => _elapsedInternal + _elapsedOffset;
    public TimeSpan TimeRemaining => EstimatedCompletionTime - ElapsedTime;
    public TimeSpan EstimatedCompletionTime {
        get {
            var returned = TimeSpan.Zero;
            for (int i = 0; i < KeyFrames.Count; i++) {
                returned += KeyFrames[i].Duration;
            }
            return returned;
        }
    }
    private bool _isRunning;
    public Vector2 CurrentPosition { get; private set; }
    public Vector2 CurrentScale { get; private set; }
    public float CurrentRotation { get; private set; }

    /// <summary>The list of <see cref="KeyFrame"/>s.</summary>
    public List<KeyFrame> KeyFrames { get; set; }
    /// <summary>The currently seeked <see cref="KeyFrame"/>. Can be represented as <c>KeyFrames[CurrentId]</c></summary>
    public KeyFrame Current => KeyFrames[CurrentId];
    /// <summary>The identifier of the current seeked <see cref="KeyFrame"/>.</summary>
    public int CurrentId { get; private set; }
    /// <summary>The interpolated value from 0 to 1 representing the percent complection of change in the last seeked <see cref="KeyFrame"/> to the currently seeked <see cref="KeyFrame"/>.</summary>
    public float CurrentInterpolation { get; private set; }
    /// <summary>The interpolated value from 0 to 1 representing the total percent completion of the entire animation stored in this <see cref="Animator"/>.</summary>
    public float Interpolated { get; private set; }
    private Animator() {
        KeyFrames = new();
        RestartInternal();
        Animators.Add(this);
    }
    /// <summary>The first frame does not require a <see cref="Duration"/> or <see cref="Easing"/>. Easing defaults to <see cref="EasingFunction.Linear"/>.
    /// <para>
    /// Construct your animation by appending <c>.WithFrame(KeyFrame frame)</c> to this method. This can be chained.
    /// </para></summary>
    public static Animator Create() => new();
    private void RestartInternal() {
        Interpolated = 0;
        CurrentInterpolation = 0;
        _elapsedOffset = TimeSpan.Zero;
        _elapsedInternal = TimeSpan.Zero;
        _isRunning = false;
        Seek(0);
    }
    public void Restart() {
        CurrentPosition = KeyFrames[0].Position;
        CurrentScale = KeyFrames[0].Scale;
        CurrentRotation = KeyFrames[0].Rotation;
        _isRunning = false;
        RestartInternal();
    }
    /// <summary>Run this animation from where it is located.</summary>
    public void Run() {
        if (CurrentId == KeyFrames.Count - 1)
            return;

        _isRunning = true;
    }
    public void Stop() {
        _isRunning = false;
    }
    public Animator WithFrame(KeyFrame frame) {
        KeyFrames.Add(frame);
        return this;
    }
    /// <summary>Advances this <see cref="Animator"/> by one frame.</summary>
    public void Step(int steps) {
        CurrentId += steps;
    }
    /// <summary>Seek an entirely separate frame, starting at <paramref name="frameId"/>.</summary>
    public void Seek(int frameId) {
        CurrentId = frameId;
        CurrentInterpolation = 0;
        Interpolated = CalculateTotalInterp(frameId);
    }
    /// <summary>
    /// Currently unimplemented.
    /// </summary>
    /// <param name="percent">The percentage of completion to seek.</param>
    public void SeekExact(float percent) {

    }
    /// <summary>
    /// Currently unimplemented.
    /// </summary>
    /// <param name="timeInTimeline">At what time during the timeline of the animation to jump to.</param>
    public void SeekExact(TimeSpan timeInTimeline) {

    }

    private float CalculateTotalInterp(int frameId) {
        var timeBefore = TimeSpan.Zero;
        for (int i = 0; i < frameId; i++) {
            timeBefore += KeyFrames[frameId].Duration;
        }
        return (float)(timeBefore.TotalSeconds / EstimatedCompletionTime.TotalSeconds);
    }

    internal void PlayAnimation(GameTime gameTime) {
        if (!_isRunning || CurrentId == KeyFrames.Count - 1)
            return;

        var futureFrame = KeyFrames[CurrentId + 1];

        var timeUntilThisFrame = TimeSpan.Zero;
        for (int i = 0; i < CurrentId; i++) {
            timeUntilThisFrame += KeyFrames[i].Duration;
        }

        CurrentInterpolation += (float)(gameTime.ElapsedGameTime.TotalSeconds / futureFrame.Duration.TotalSeconds);
        _elapsedInternal += gameTime.ElapsedGameTime;

        var ease = Easings.GetEasingBehavior(futureFrame.Easing, CurrentInterpolation);
        CurrentPosition = Current.Position + (futureFrame.Position - Current.Position) * ease;
        CurrentScale = Current.Scale + (futureFrame.Scale - Current.Scale) * ease;
        CurrentRotation = Current.Rotation + (futureFrame.Rotation - Current.Rotation) * ease;

        if (CurrentInterpolation > 1) {
            CurrentInterpolation = 0;
            Step(1);
            CurrentPosition = Current.Position;
            CurrentScale = Current.Scale;
            CurrentRotation = Current.Rotation;

            if (CurrentId == KeyFrames.Count - 1) {
                _isRunning = false;
            }
        }
    }
}
