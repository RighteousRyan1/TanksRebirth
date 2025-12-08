using System;
using System.Collections.Generic;
using System.Linq;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Net;

namespace TanksRebirth.GameContent.Cosmetics;

// TODO: pets lol???
// Not inherently locked to cosmetics only, but exist to distinguish from tank crates.

/// <summary>Determines the "rarity" of a loot box item. <br></br>
/// The WeightedContents dictionary will be populated automatically with properly fitted floats for the given name.</summary>
public enum LootBoxRarity : byte {
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary,
    Mythical,
    Godly
}
public struct LootBox<TReward>(Dictionary<TReward, LootBoxRarity> weightedContents) where TReward : notnull {
    /// <summary>All of the contents should add up to 1.0.</summary>
    public Dictionary<TReward, LootBoxRarity> ContentWeights = weightedContents;

    public readonly TReward Roll(out float percent) {
        if (ContentWeights is null || ContentWeights.Count == 0)
            throw new InvalidOperationException("LootBox has no contents.");

        float roll = Client.ClientRandom.NextFloat(0, 1);
        percent = roll;

        var rarity = VanillaCosmetics.GetRarityFromFloat(percent);

        var candidates = ContentWeights.Where(x => x.Value == rarity).ToArray();

        var rand = Client.ClientRandom.Next(candidates.Length);
        var rolledItem = candidates[rand];

        // falls back if failure
        return rolledItem.Key;
    }
}
