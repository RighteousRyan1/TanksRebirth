using System;

namespace TanksRebirth.GameContent.Systems.CommandsSystem;

/// <summary>A structure which represents the metadata of the output of a command. This parameter should be fed the handle of a type.</summary>
public readonly struct CommandOutput {
    /// <summary>Whether or not this <see cref="CommandOutput"/> should sync across a multiplayer server.</summary>
    public readonly bool NetSync;
    /// <summary>The action to perform if this <see cref="CommandOutput"/> is successful.</summary>
    public readonly Action<string[]> ActionToPerform;
    /// <summary>Create the output of a command.</summary>
    /// /// <param name="actionToPerform">The action to perform if this <see cref="CommandOutput"/> is successful.</param>
    /// /// <param name="netSync">Whether or not this <see cref="CommandOutput"/> should sync across a multiplayer server.</param>
    public CommandOutput(bool netSync, Action<string[]> actionToPerform) {
        ActionToPerform = actionToPerform;
        NetSync = netSync;
    }
}