using System;
using System.Collections;
using System.Collections.Generic;
using TanksRebirth.GameContent.ID;
using TanksRebirth.GameContent.ModSupport;

namespace TanksRebirth.GameContent.Systems;

/// <summary>What kind of value a modifier holds, and so how its row in the modifiers menu behaves.</summary>
public enum ModifierKind {
    /// <summary>On or Off, shown as a switch.</summary>
    Toggle,
    /// <summary>A tank tier from <see cref="TankID"/>, where <see cref="TankID.None"/> is off.</summary>
    Tank,
    /// <summary>A whole number from <see cref="ModifierDefinition.Min"/> to <see cref="ModifierDefinition.Max"/>, moving by <see cref="ModifierDefinition.Step"/>.</summary>
    Number,
    /// <summary>An index into <see cref="ModifierDefinition.Options"/>.</summary>
    Choice,
}

/// <summary>The section names the base game's modifiers use. Any other string makes a new section.</summary>
public static class ModifierCategory {
    public const string Enemies = "Enemies";
    public const string AllTanks = "All Tanks";
    public const string Player = "Player";
    public const string World = "World";
    public const string Other = "Other";
}

/// <summary>
/// Describes a modifier and how it's changed by the user. Entries can be created by inheriting a class from <see cref="ModModifier"/>.
/// <para></para>
/// Doing this, the modifier will get its own section on the modifiers UI.
/// </summary>
public sealed class ModifierDefinition {
    /// <summary>The unique key (also what's sent over the network). Modded keys are prefixed with the mod's name.</summary>
    public string Key { get; }
    public ModifierKind Kind { get; }

    /// <summary>The name in the modifiers menu. A function so it can follow the game's language.</summary>
    public Func<string> Name { get; init; }
    /// <summary>What the modifier does, shown when the mouse is over it. Can be multiple lines.</summary>
    public Func<string> Description { get; init; } = () => string.Empty;
    /// <summary>The section it's listed under (see <see cref="ModifierCategory"/>).</summary>
    public string Category { get; init; } = ModifierCategory.Other;

    /// <summary>The "off" value, and what Reset All and middle click go back to.</summary>
    public int Default { get; init; }
    public int Min { get; init; }
    public int Max { get; init; } = 1;
    public int Step { get; init; } = 1;
    /// <summary>The choices of a <see cref="ModifierKind.Choice"/> modifier.</summary>
    public string[] Options { get; init; } = [];
    /// <summary>Whether stepping past the end goes back to the start (always true for tanks and choices).</summary>
    public bool Wraps { get; init; }

    /// <summary>Turns the value into the text shown in the menu. Defaults to a sensible text per <see cref="Kind"/>.</summary>
    public Func<int, string>? Format { get; init; }
    /// <summary>Overrides when it should be enabled/on.</summary>
    public Func<bool>? ActiveWhen { get; init; }
    /// <summary>Runs after the value changes. Runs on every client, also for values received from the host.</summary>
    public Action<int, int>? Changed { get; init; }
    /// <summary>Runs after a local change.</summary>
    public Action<int>? Constrain { get; init; }
    /// <summary>Whether it adds to the "N active" counts. Off for helper rows, like the upper end of a range.</summary>
    public bool CountsTowardTotal { get; init; } = true;

    /// <summary>The mod that registered it (null for the base game). Its modifiers are removed when the mod unloads.</summary>
    public TanksMod? Owner { get; init; }

    public ModifierDefinition(string key, ModifierKind kind) {
        Key = key;
        Kind = kind;
        Name = () => key;
        if (kind == ModifierKind.Tank)
            Wraps = true;
    }

    /// <summary>The lowest and highest value this modifier accepts.</summary>
    public (int Min, int Max) Range => Kind switch {
        ModifierKind.Toggle => (0, 1),
        ModifierKind.Tank => (TankID.None, Math.Max(TankID.None, TankID.Collection.Count - 1)),
        ModifierKind.Choice => (0, Math.Max(0, Options.Length - 1)),
        _ => (Min, Max),
    };

    /// <summary>The text shown for <paramref name="value"/>.</summary>
    public string FormatValue(int value) {
        if (Format is not null)
            return Format(value);
        return Kind switch {
            ModifierKind.Toggle => value != 0 ? "On" : "Off",
            ModifierKind.Tank => TankID.Collection.GetKey(value) ?? value.ToString(),
            ModifierKind.Choice => value >= 0 && value < Options.Length ? Options[value] : value.ToString(),
            _ => value.ToString(),
        };
    }

    /// <summary>Whether a modifier holding <paramref name="value"/> is on.</summary>
    public bool IsActiveWith(int value) => ActiveWhen?.Invoke() ?? value != Default;
}