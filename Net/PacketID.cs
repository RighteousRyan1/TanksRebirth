using TanksRebirth.Internals.Common.Framework.Collections;

namespace TanksRebirth.Net;

public sealed class PacketID {
    // first-time networking
    public const int ClientInfo = 0;
    public const int LobbyInfo = 1;

    // players
    public const int SyncPlayer = 2;
    public const int SyncLives = 3;

    // owner
    public const int SyncAiTank = 4;

    // entitites
    public const int TankDamage = 5;
    public const int ShellDestroy = 6;
    public const int MineDetonate = 7;

    // tank mechanics
    public const int MinePlacement = 8;
    public const int ShellFire = 9;

    // human communication

    public const int ChatMessage = 10;
    public const int MapPing = 11;

    // debug

    public const int PlayerSpawn = 12;
    public const int AiTankSpawn = 13;
    public const int CrateSpawn = 14;

    // more server syncing

    public const int ServerNameSync = 15;
    public const int StartGame = 16;
    public const int LeaveGame = 17;

    // map sync

    public const int SendCampaign = 18;
    public const int SendMission = 19;

    // misc

    /// <summary>The game has to reassign identifiers of shells beacuse there can be a client-server mismatch of IDs.</summary>
    public const int SyncShellId = 20;
    /// <summary>The packet for map cleanup (removal of death X's, etc)</summary>
    public const int Cleanup = 21;
    /// <summary>When the host leaves the level.</summary>
    public const int QuitLevel = 22;
    /// <summary>To check if another client doesn't have something a host does, such as a campaign.</summary>
    public const int SendCampaignStatus = 23;
    /// <summary>Sent across the network when a command is used that has serverside effects.</summary>
    public const int SendCommandUsage = 24;

    public const int SyncDifficulties = 25;
    /// <summary>Syncs randomization seeds to random events are synchronous on each client.</summary>
    public const int SyncSeeds = 26;

    public const int SyncKills = 27;
    public const int SendAirplane = 28;

    public const int HostDisconnect = 29;
    /// <summary>The host's result for a finished mission, so every client ends it the same way.</summary>
    public const int MissionEnd = 30;
    /// <summary>When the host starts the campaign over from the pause menu.</summary>
    public const int StartOver = 31;

    public static int AddPacketId(string name) => Collection.ForcefullyInsert(name);

    public static ReflectionDictionary<PacketID> Collection { get; internal set; } = new(MemberType.Fields);
}
