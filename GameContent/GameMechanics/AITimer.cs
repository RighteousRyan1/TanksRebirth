namespace TanksRebirth.GameContent.GameMechanics;

public class AITimer {
    public string? Label;
    public float Value;

    public bool IsModOf(float remainder) => Value % remainder < Tanks.AI.AIManager.AIDeltaFrames;
}