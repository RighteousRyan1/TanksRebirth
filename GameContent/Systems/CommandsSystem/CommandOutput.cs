namespace TanksRebirth.GameContent.Systems.CommandsSystem;

public delegate void CommandUsageCallback(string[] args);
/// <summary>A structure which represents the metadata of the output of a command. This parameter should be fed the handle of a type.</summary>
/// <remarks>Create the output of a command.</remarks>
/// <param name="commandCallback">The action to perform if this <see cref="CommandOutput"/> is successful.</param>
/// <param name="requireCheats">Whether or not cheats are required to use this command.</param>
/// <param name="netSync">Whether or not this <see cref="CommandOutput"/> should sync across a multiplayer server.</param>
public readonly struct CommandOutput(bool netSync, bool requireCheats, CommandUsageCallback commandCallback) {
    /// <summary>Whether or not this <see cref="CommandOutput"/> should sync across a multiplayer server.</summary>
    public readonly bool NetSync = netSync;
    /// <summary>Whether or not the output of a given command requires cheats to be enabled.</summary>
    public readonly bool RequireCheats = requireCheats;
    /// <summary>The action to perform if this <see cref="CommandOutput"/> is successful.</summary>
    public readonly CommandUsageCallback ActionToPerform = commandCallback;
}