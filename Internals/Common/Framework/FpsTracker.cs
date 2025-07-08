namespace TanksRebirth.Internals.Common.Framework;

public class FpsTracker {
    private int _frameCount = 0;
    private double _totalTime = 0.0; // seconds

    public double AverageFPS => _totalTime == 0 ? 0 : _frameCount / _totalTime;

    public void Update(double deltaTimeSeconds) {
        _frameCount++;
        _totalTime += deltaTimeSeconds;
    }
}
