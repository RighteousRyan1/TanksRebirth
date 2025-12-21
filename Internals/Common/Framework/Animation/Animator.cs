using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using TanksRebirth.GameContent.Systems;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth.Internals.Common.Framework.Animation;

public class Animator {
    public static readonly List<Animator> Animators = [];

    // for interpolated floating-point values
    float[] _startFloats = [];
    float[] _endFloats = [];

    // added to after every frame is complete. the frame's completion time is added
    TimeSpan _elapsedOffset;
    // the current elapsed time of the current frame only
    TimeSpan _elapsedInternal;
    // if the animation is currently running
    bool _isRunning;

    /// <summary>Whether or not this Animator shall loop itself upon completion.</summary>
    public bool IsLooped { get; set; }
    /// <summary>Whether or not this animator is playing its animation currently.</summary>
    public bool IsPlaying => _isRunning;
    /// <summary>Total time elapsed in the animation.</summary>
    public TimeSpan ElapsedTime => _elapsedInternal + _elapsedOffset;
    /// <summary>The time left before the animation is complete.</summary>
    public TimeSpan TimeRemaining => EstimatedCompletionTime - ElapsedTime;
    // maybe not a property so this isn't computed every access?
    public TimeSpan EstimatedCompletionTime {
        get {
            var returned = TimeSpan.Zero;
            // only includes frames after frame 0 because the first one does not matter
            for (int i = 1; i < KeyFrames.Count; i++) {
                returned += KeyFrames[i].Duration;
            }
            return returned;
        }
    }
    /// <summary>If > -1, the animation will move from the current frame to this ID in the array of KeyFrames.</summary>
    public int NextKeyFrame = -1;
    /// <summary>The current 2D position of the given animation.</summary>
    public Vector3 CurrentPosition { get; private set; }
    /// <summary>The current two-dimensional scale of the given animation.</summary>
    public Vector3 CurrentScale { get; private set; }
    /// <summary>The current rotation of the given animation. You can use this to match to any float you want.</summary>
    public float[] CurrentFloats { get; private set; }
    /// <summary>The list of <see cref="KeyFrame"/>s.</summary>
    public List<KeyFrame> KeyFrames { get; set; }
    /// <summary>The currently seeked <see cref="KeyFrame"/>. Can be represented as <c>KeyFrames[CurrentId]</c></summary>
    public KeyFrame Current => KeyFrames[CurrentId];
    /// <summary>The identifier of the current seeked <see cref="KeyFrame"/>.</summary>
    public int CurrentId { get; private set; }
    /// <summary>The interpolated value from 0 to 1 representing the percent complection of change in the last seeked <see cref="KeyFrame"/> to the currently seeked <see cref="KeyFrame"/>.</summary>
    public float CurrentProgress { get; private set; }
    /// <summary>The interpolated value from 0 to 1 representing the total percent completion of the entire animation stored in this <see cref="Animator"/>.</summary>
    public float TotalProgress { get; private set; }
    /// <summary>Perform actions once a <see cref="KeyFrame"/> finishes elapsing.</summary>
    /// <param name="frameIndex">The index of the frame that was just completed. -1 Indicates the very start of the animation.</param>
    public delegate void OnKeyFrameEnd(int frameIndex);
    /// <summary>Invoked once a <see cref="KeyFrame"/> finishes playing in this animation.</summary>
    public event OnKeyFrameEnd? OnKeyFrameFinish;
    /// <summary>The first frame does not require a <see cref="KeyFrame.Duration"/> or <see cref="EasingFunction"/>. Easing defaults to <see cref="EasingFunction.Linear"/>.
    /// <para>
    /// Construct your animation by appending <c>.WithFrame(KeyFrame frame)</c> to this method. This can be chained.
    /// </para></summary>
    public static Animator Create() => new();

    public void ModifyFloat(KeyFrame frame, float newFloat) {
        var idx = KeyFrames.IndexOf(frame);

        _endFloats[idx] = newFloat;
    }
    public void ModifyFloat(int frame, float newFloat) {
        _endFloats[frame] = newFloat;
    }
    public void Restart() {
        CurrentPosition = KeyFrames[0].Position;
        CurrentScale = KeyFrames[0].Scale;
        CurrentFloats = KeyFrames[0].Floats;
        RestartInternal();
        // again, the animation has just begun
        // OnKeyFrameFinish?.Invoke(-1);
    }
    /// <summary>Run this animation from where it is located.</summary>
    public void Run() {
        if (CurrentId == KeyFrames.Count - 1)
            return;

        // -1 indicates the animation has just started
        OnKeyFrameFinish?.Invoke(-1);
        _isRunning = true;

        PrepareFloatBuffers();
    }
    public void Stop() {
        _isRunning = false;
    }
    /// <summary>The animator will interpolate between these values to the next frame's values.</summary><param name="frame"></param>
    public Animator WithFrame(KeyFrame frame) {
        // idk if i should keep this xd
        /*if (frame.BezierPoints != null && frame.BezierPoints.Count > 0) {
            // if a keyframe has already been added...
            if (KeyFrames.Count > 0) {
                // previous keyframe from the currently added ones
                var prevFrame = KeyFrames[^1];

                // add the position of this keyframe if it has not already been added.
                if (!frame.BezierPoints.Contains(frame.Position)) {
                    frame.BezierPoints.Insert(0, frame.Position);
                }
                // add the position of the previous keyframe if not already added.
                if (!frame.BezierPoints.Contains(prevFrame.Position)) {
                    frame.BezierPoints?.Insert(0, prevFrame.Position);
                }
            }
        }*/
        KeyFrames.Add(frame);

        // slightly confused why i added this, but if it's bad i guess i'll find out
        CurrentFloats = KeyFrames[0].Floats;
        return this;
    }
    /// <summary>Advances this <see cref="Animator"/> by one frame.</summary>
    public void Step(int steps) {
        OnKeyFrameFinish?.Invoke(CurrentId);
        //_elapsedInternal += Current.Duration;
        CurrentId += steps;
        PrepareFloatBuffers();
    }
    /// <summary>Seek an entirely separate frame, starting at <paramref name="frameId"/>.</summary>
    public void Seek(int frameId) {
        CurrentId = frameId;
        CurrentProgress = 0;
        TotalProgress = CalculateTotalProgress(frameId);
        PrepareFloatBuffers();
    }
    /// <summary>
    /// Seeks to an exact point in the animation timeline, with a percentage of completion.
    /// </summary>
    /// <param name="percent">The percentage of completion to seek.</param>
    public void SeekExact(float percent) {
        // Percent is over the whole timeline.
        percent = Math.Clamp(percent, 0f, 1f);
        var target = TimeSpan.FromSeconds(EstimatedCompletionTime.TotalSeconds * percent);
        SeekExact(target);
    }
    /// <summary>
    /// Seek an exact position.
    /// </summary>
    /// <param name="timeInTimeline">At what time during the timeline of the animation to jump to.</param>
    /// <summary>
    /// Seek an exact position
    /// </summary>
    public void SeekExact(TimeSpan timeInTimeline) {
        if (KeyFrames.Count == 0)
            return;

        // if impossible to seek, exit early
        if (!TryLocate(timeInTimeline, out var segIndex, out var localT))
            return;

        CurrentId = segIndex;

        // _elapsedOffset is total duration of completed frames before segIndex
        TimeSpan offset = TimeSpan.Zero;
        for (int i = 0; i < segIndex; i++)
            offset += KeyFrames[i].Duration;

        _elapsedOffset = offset;

        // global interpolation across the whole animation
        var total = EstimatedCompletionTime.TotalSeconds;
        // _elapsedInternal is how far into the current segment we are
        // Note: Using next frame duration as requested
        var nextFrameIdx = Math.Clamp(segIndex + 1, 0, KeyFrames.Count - 1);
        var segDur = KeyFrames[nextFrameIdx].Duration;

        _elapsedInternal = segDur > TimeSpan.Zero
            ? TimeSpan.FromSeconds(segDur.TotalSeconds * localT)
            : TimeSpan.Zero;

        CurrentProgress = localT;

        TotalProgress = (float)((_elapsedOffset + _elapsedInternal).TotalSeconds / (total <= 0 ? 1 : total));

        var current = KeyFrames[segIndex];

        // if last frame, snap values
        if (segIndex >= KeyFrames.Count - 1) {
            CurrentPosition = current.Position;
            CurrentScale = current.Scale;
            CurrentFloats = current.Floats;
            return;
        }

        var futureFrame = KeyFrames[segIndex + 1];

        // applies the segment's easing for accurate positioning
        // Note: Using next frame's easing as requested
        float ease = Easings.ComputeEase(futureFrame.Easing, CurrentProgress);

        bool hasBezier = futureFrame.BezierPoints != null && futureFrame.BezierPoints.Count > 0;
        CurrentPosition = hasBezier
            ? GetBezierPos(ease, Current, futureFrame)
            : Current.Position + (futureFrame.Position - Current.Position) * ease;

        CurrentScale = current.Scale + (futureFrame.Scale - current.Scale) * ease;

        if (current.Floats is not null && futureFrame.Floats is not null) {
            int n = Math.Min(current.Floats.Length, futureFrame.Floats.Length);
            if (CurrentFloats == null || CurrentFloats.Length != n)
                CurrentFloats = new float[n];

            for (int i = 0; i < n; i++)
                CurrentFloats[i] = current.Floats[i] + (futureFrame.Floats[i] - current.Floats[i]) * ease;
        }
    }

    /// <summary>Perform any movements that would happen in one frame of animation time.</summary>
    // hoping this works lowk
    public void FrameStep(float fps = 60.0f) {
        if (_isRunning || KeyFrames.Count == 0 || CurrentId >= KeyFrames.Count - 1)
            return;

        var dt = TimeSpan.FromSeconds(1f / fps);

        var nextId = NextKeyFrame > -1 ? NextKeyFrame : CurrentId + 1;
        var futureFrame = KeyFrames[nextId];

        // Note: Using futureFrame duration as requested
        float next = CurrentProgress + (float)(dt.TotalSeconds / futureFrame.Duration.TotalSeconds);
        if (next > 1f) next = 1f;

        _elapsedInternal += dt;
        TotalProgress = (float)((_elapsedOffset + _elapsedInternal).TotalSeconds / EstimatedCompletionTime.TotalSeconds);

        // easing + positioning + scaling
        // Note: Using futureFrame easing as requested
        float ease = Easings.ComputeEase(futureFrame.Easing, next);

        bool hasBezier = futureFrame.BezierPoints != null && futureFrame.BezierPoints.Count > 0;
        CurrentPosition = hasBezier
            ? GetBezierPos(ease, Current, futureFrame)
            : Current.Position + (futureFrame.Position - Current.Position) * ease;

        CurrentScale = Current.Scale + (futureFrame.Scale - Current.Scale) * ease;

        // float buffers
        if (CurrentFloats is not null && Current.Floats is not null && futureFrame.Floats is not null) {
            int n = Math.Min(CurrentFloats.Length, Math.Min(Current.Floats.Length, futureFrame.Floats.Length));
            for (int i = 0; i < n; i++)
                CurrentFloats[i] = Current.Floats[i] + (futureFrame.Floats[i] - Current.Floats[i]) * ease;
        }

        CurrentProgress = next;

        // finish frame?
        if (CurrentProgress >= 1f) {
            // close out timing for the finished keyframe
            _elapsedOffset += _elapsedInternal;
            _elapsedInternal = TimeSpan.Zero;

            CurrentProgress = 0f;
            OnKeyFrameFinish?.Invoke(CurrentId);

            Step(1);

            // snap to the new start values
            CurrentPosition = Current.Position;
            CurrentScale = Current.Scale;
            CurrentFloats = Current.Floats!;

            // handle end/loop
            if (CurrentId >= KeyFrames.Count - 1) {
                if (IsLooped) {
                    Restart();
                    Run();
                }
                else {
                    _isRunning = false;
                }
            }
        }
    }

    // non-api methods below
    Animator() {
        CurrentFloats = [];
        KeyFrames = [];
        RestartInternal();
        Animators.Add(this);
    }
    ~Animator() {
        Animators.Remove(this);
    }

    // TODO: fix the last keyframe not firing an event.
    internal void PlayAnimationIfRunning(GameTime gameTime) {
        if (!_isRunning) return;
        if (double.IsNaN(gameTime.ElapsedGameTime.TotalSeconds)) return;

        TotalProgress = MathF.Min(1f, (float)(ElapsedTime.TotalSeconds / EstimatedCompletionTime.TotalSeconds));

        var futureFrame = KeyFrames[NextKeyFrame > -1 ? NextKeyFrame : CurrentId + 1];

        // Note: Using futureFrame duration as requested
        CurrentProgress += (float)(gameTime.ElapsedGameTime.TotalSeconds / futureFrame.Duration.TotalSeconds);
        _elapsedInternal += gameTime.ElapsedGameTime;

        // Note: Using futureFrame easing as requested
        var ease = Easings.ComputeEase(futureFrame.Easing, CurrentProgress);

        // use future frame for bezier...?
        bool hasBezier = futureFrame.BezierPoints != null && futureFrame.BezierPoints.Count > 0;
        CurrentPosition = hasBezier
            ? GetBezierPos(ease, Current, futureFrame)
            : Current.Position + (futureFrame.Position - Current.Position) * ease;
        CurrentScale = Current.Scale + (futureFrame.Scale - Current.Scale) * ease;
        // CurrentPosition = Current.Position + (futureFrame.Position - Current.Position) * ease;

        // TODO: fix, floats array goes by way faster than it should, skips animation basically
        if (CurrentFloats != null && _startFloats.Length > 0) {
            for (int i = 0; i < CurrentFloats.Length; i++)
                CurrentFloats[i] = MathHelper.Lerp(_startFloats[i], _endFloats[i], ease);
        }

        /* current floats will blend into the future floats
         * current scale into future scale
         * current time is the time it takes to get to the next frame, final frame time will not matter
         * current scale blends into future scale
         * */
        // code below is in case the code above fails.

        if (CurrentProgress >= 1 && CurrentId < KeyFrames.Count - 1) {
            CurrentProgress = 0;
            Step(1);
            CurrentPosition = Current.Position;
            CurrentScale = Current.Scale;
            CurrentFloats = Current.Floats!;

            if (CurrentId >= KeyFrames.Count - 1) {
                if (IsLooped) {
                    Restart();
                    Run();
                    return;
                }
                _isRunning = false;
            }
        }
        //if (KeyFrames[1].Duration == TimeSpan.FromSeconds(3.5))
        //Debug.WriteLine(ElapsedTime.TotalSeconds + " : " + Interpolated);
    }

    void RestartInternal() {
        TotalProgress = 0;
        CurrentProgress = 0;
        _elapsedOffset = TimeSpan.Zero;
        _elapsedInternal = TimeSpan.Zero;
        _isRunning = false;
        Seek(0);
    }

    float CalculateTotalProgress(int frameId) {
        var timeBefore = TimeSpan.Zero;
        for (int i = 0; i < frameId; i++) {
            timeBefore += KeyFrames[frameId].Duration;
        }
        return MathF.Min(1f, (float)(timeBefore.TotalSeconds / EstimatedCompletionTime.TotalSeconds));
    }

    // prepares the Floats[] interpolation buffer
    // mutation won't work (for now...) because this float buffer isn't re-read upon mutation
    void PrepareFloatBuffers() {
        if (KeyFrames.Count == 0) return;
        if (CurrentId >= KeyFrames.Count - 1) return;
        if (Current.Floats is null or []) return;

        var a = Current.Floats;
        var b = (NextKeyFrame > -1 ? KeyFrames[NextKeyFrame] : KeyFrames[CurrentId + 1]).Floats;

        int n = Math.Min(a.Length, b.Length);

        if (CurrentFloats == null || CurrentFloats.Length != n)
            CurrentFloats = new float[n];

        if (_startFloats.Length != n) _startFloats = new float[n];
        if (_endFloats.Length != n) _endFloats = new float[n];

        Array.Copy(a, _startFloats, n);
        Array.Copy(b, _endFloats, n);
    }

    // finds which keyframe segment contains t, and returns the local [0..1] interpolation within that segment
    bool TryLocate(TimeSpan t, out int frameIndex, out float localT) {
        frameIndex = 0;
        localT = 0f;

        var total = EstimatedCompletionTime;
        if (KeyFrames.Count == 0 || total <= TimeSpan.Zero)
            return false;

        // clamps to start or end
        if (t <= TimeSpan.Zero) {
            frameIndex = 0;
            localT = 0f;
            return true;
        }
        if (t >= total) {
            frameIndex = Math.Max(0, KeyFrames.Count - 1);
            localT = 1f; return true;
        }

        var acc = TimeSpan.Zero;
        for (int i = 0; i < KeyFrames.Count - 1; i++) {
            var dur = KeyFrames[i].Duration;
            // protect zero or less than zero durations
            if (dur <= TimeSpan.Zero) {
                if (t <= acc) {
                    frameIndex = i;
                    localT = 1f;
                    return true;
                }
                acc += TimeSpan.Zero;
                continue;
            }

            var nextAcc = acc + dur;
            if (t <= nextAcc) {
                frameIndex = i;
                var remain = t - acc;
                localT = (float)(remain.TotalSeconds / dur.TotalSeconds);
                localT = Math.Clamp(localT, 0f, 1f);
                return true;
            }
            acc = nextAcc;
        }

        // failsafe to the final frame
        frameIndex = Math.Max(0, KeyFrames.Count - 1);
        localT = 1f;
        return true;
    }

    static Vector3 GetBezierPos(float t, KeyFrame current, KeyFrame next) {
        // gc hell?
        var safePoints = new List<Vector3>();

        if (current.BezierPoints.Count == 0 || current.BezierPoints[0] != current.Position)
            safePoints.Add(current.Position);

        safePoints.AddRange(next.BezierPoints);

        if (safePoints[^1] != next.Position)
            safePoints.Add(next.Position);

        return MathUtils.Bezier3D(t, safePoints.ToArray());
    }

    public override string ToString()
        => $"keyfc: {KeyFrames.Count} | cprog: {CurrentProgress:0.00} | tprog: {TotalProgress:0.00} | eta: {EstimatedCompletionTime}";
}