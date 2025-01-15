using TanksRebirth.Internals.Common.Framework.Collections;

namespace TanksRebirth.Net;

public sealed class PacketID {
    // First-time networking
    public const int ClientInfo = 0;
    public const int LobbyInfo = 1;

    // Ingame packets: players
    public const int SyncPlayer = 2;
    public const int SyncLives = 3;

    // Ingame packets: ai
    public const int SyncAiTank = 4;

    // Ingame packets: entitites
    public const int TankDamage = 5;
    public const int ShellDestroy = 6;
    public const int MineDetonate = 7;

    // Ingame packets: tank mechanics
    public const int MinePlacement = 8;
    public const int ShellFire = 9;

    // Human communication

    public const int ChatMessage = 10;
    public const int MapPing = 11;

    // Debugging packets:

    public const int PlayerSpawn = 13;
    public const int AiTankSpawn = 14;
    public const int CrateSpawn = 15;

    // more server syncing

    public const int ServerNameSync = 16;
    public const int StartGame = 17;
    public const int LeaveGame = 18;
    public const int Disconnect = 19;

    // map sync

    public const int SendCampaign = 20;
    public const int SendCampaignByName = 21;
    public const int SendMission = 22;

    // misc

    /// <summary>The game has to reassign identifiers of shells beacuse there can be a client-server mismatch of IDs.</summary>
    public const int SyncShellId = 22;
    /// <summary>The packet for map cleanup (removal of death X's, etc)</summary>
    public const int Cleanup = 23;
    /// <summary>When the host leaves the level.</summary>
    public const int QuitLevel = 24;
    /// <summary>To check if another client doesn't have something a host does, such as a campaign.</summary>
    public const int SendCampaignStatus = 25;
    /// <summary>Sent across the network when a command is used that has serverside effects.</summary>
    public const int SendCommandUsage = 26;

    public const int SyncDifficulties = 27;
    /// <summary>Syncs randomization seeds to random events are synchronous on each client.</summary>
    public const int SyncSeeds = 28;

    public static int AddPacketId(string name) => Collection.ForcefullyInsert(name);

    public static ReflectionDictionary<PacketID> Collection { get; internal set; } = new(MemberType.Fields);
}
