using System;

namespace TanksRebirth.Achievements;

public interface IAchievement {
    void Complete();
    Func<bool>? Requirement { get; set; }
    bool IsComplete { get; set; }
    string Name { get; set; }
}
