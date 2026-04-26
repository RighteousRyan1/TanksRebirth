namespace TanksRebirth.GameContent.Tanks.AI;

public interface IAISystem {
    AITank Tank { get; }
    void AILoop();
}
