namespace TanksRebirth.GameContent;

public interface ITankHurtContext {
    bool IsPlayer { get; set; }

    // PlayerType PlayerType { get; set; } // don't use if IsPlayer is false.
}