namespace TanksRebirth.GameContent.Tanks.AI;

public interface IAISystem {
    AITank Owner { get; }
    void AILoop();
    void Initialize();
}
