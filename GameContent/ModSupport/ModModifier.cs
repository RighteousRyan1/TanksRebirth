using System;
using TanksRebirth.GameContent.ID;
using TanksRebirth.GameContent.Systems;
using TanksRebirth.Internals.Common.Framework.Interfaces;

namespace TanksRebirth.GameContent.ModSupport;

#pragma warning disable CS8618
/// <summary>A modifier added by a mod. Multiplayer sync is handled automatically.</summary>
public abstract class ModModifier : ILoadable, IModContent {
    public string InternalName { get; set; }
    /// <summary>The <see cref="TanksMod"/> that this <see cref="ModModifier"/> is a part of.</summary>
    public TanksMod Mod { get; set; }
    /// <summary>Its position in <see cref="Modifiers.Definitions"/> when it was registered.</summary>
    public int Type { get; set; }

    /// <summary>The key it's stored and synced under: "ModName.ClassName".</summary>
    public string Key => $"{Mod.InternalName}.{InternalName}";
    /// <summary>The registered definition (null until registered).</summary>
    public ModifierDefinition? Definition { get; private set; }

    /// <summary>The name shown in the modifiers menu. Read every frame, so it can follow the game's language.</summary>
    public virtual string DisplayName => InternalName;
    /// <summary>What the modifier does, shown when the mouse is over it. Can be multiple lines.</summary>
    public virtual string Description => string.Empty;
    /// <summary>The section it's listed under. Use one of <see cref="ModifierCategory"/>'s names to join a base game section, or any other name to make a new one. Defaults to the mod's name.</summary>
    public virtual string Category => Mod.InternalName;

    public virtual ModifierKind Kind => ModifierKind.Toggle;
    /// <summary>The "off" value, and what MakeSeekable All and middle click go back to.</summary>
    public virtual int Default => 0;
    /// <summary>The lowest value of a <see cref="ModifierKind.Number"/> modifier.</summary>
    public virtual int Min => 0;
    /// <summary>The highest value of a <see cref="ModifierKind.Number"/> modifier.</summary>
    public virtual int Max => 1;
    /// <summary>How far one click moves a <see cref="ModifierKind.Number"/> modifier.</summary>
    public virtual int Step => 1;
    /// <summary>Whether a <see cref="ModifierKind.Number"/> modifier goes back to <see cref="Min"/> after <see cref="Max"/>.</summary>
    public virtual bool Wraps => false;
    /// <summary>The choices of a <see cref="ModifierKind.Choice"/> modifier.</summary>
    public virtual string[] Options => [];
    /// <summary>Whether it adds to the "N active" counts.</summary>
    public virtual bool CountsTowardTotal => true;

    /// <summary>The text shown for a value. Defaults to a sensible text per <see cref="Kind"/>.</summary>
    public virtual string FormatValue(int value) => DefaultFormat(value);
    /// <summary>Whether the modifier counts as on. Defaults to "the value isn't <see cref="Default"/>".</summary>
    public virtual bool IsActiveWith(int value) => value != Default;

    /// <summary>The current value.</summary>
    public int Value {
        get => Modifiers.Get(Key);
        set => Modifiers.Set(Key, value);
    }
    /// <summary>Whether it's on right now.</summary>
    public bool IsActive => Modifiers.IsOn(Key);

    /// <summary>Initialize what you want alongside the loading of your modifier.</summary>
    public virtual void OnLoad() { }
    /// <summary>Manually unload things that may not be automatically unloaded by the game.</summary>
    public virtual void OnUnload() { }
    /// <summary>Called after the value changes, on every client (also for values received from the host).</summary>
    public virtual void OnChanged(int oldValue, int newValue) { }
    /// <summary>
    /// Called after a LOCAL change only. Use it to keep related modifiers consistent, such as turning another one off when this one turns on.
    /// </summary>
    public virtual void Constrain(int newValue) { }

    string DefaultFormat(int value)
        // default format if FormatValue is not overridden
        => Kind switch {
            ModifierKind.Toggle => value != 0 ? "On" : "Off",
            ModifierKind.Tank => TankID.Collection.GetKey(value) ?? value.ToString(),
            ModifierKind.Choice => value >= 0 && value < Options.Length ? Options[value] : value.ToString(),
            _ => value.ToString(),
        };

    // non-api
    bool _unloaded;

    internal void Register() {
        // registration is queued for the main thread; the mod may have been unloaded before it ran
        if (_unloaded || Definition is not null)
            return;
        Definition = Modifiers.Register(new ModifierDefinition(Key, Kind) {
            Name = () => DisplayName,
            Description = () => Description,
            Category = Category,
            Default = Default,
            Min = Min,
            Max = Max,
            Step = Step,
            Wraps = Wraps || Kind == ModifierKind.Tank,
            Options = Options,
            Format = FormatValue,
            ActiveWhen = () => IsActiveWith(Value),
            Changed = OnChanged,
            Constrain = Constrain,
            CountsTowardTotal = CountsTowardTotal,
            Owner = Mod,
        });
        Type = Modifiers.Definitions.Count - 1;
        OnLoad();
    }

    internal void Unload() {
        _unloaded = true;
        if (Definition is null)
            return;
        OnUnload();
        Modifiers.Unregister(Key);
        Definition = null;
    }
}
