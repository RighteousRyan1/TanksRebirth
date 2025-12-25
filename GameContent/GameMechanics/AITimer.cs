namespace TanksRebirth.GameContent.GameMechanics;

public class AITimer {
    public string? Label;
    public float Value;

    public bool IsModOf(float remainder) => Value % remainder < RuntimeData.DeltaTime;
}