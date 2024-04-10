using TanksRebirth.Internals.Common.Framework.Collections;

namespace TanksRebirth.Net;

public sealed class PacketID
{
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

    // Debugging packets:

    public const int PlayerSpawn = 11;
    public const int AiTankSpawn = 12;
    public const int CrateSpawn = 13;

    // more server syncing

    public const int ServerNameSync = 14;
    public const int StartGame = 15;
    public const int LeaveGame = 16;
    public const int Disconnect = 17;

    // map sync

    public const int SendCampaign = 18;
    public const int SendCampaignByName = 19;
    public const int SendMission = 20;

    // misc

    public const int SyncShellId = 21;
    public const int Cleanup = 22;
    public const int QuitLevel = 23;
    public const int SendCampaignStatus = 24; // i.e: whether another client doesn't have something a host does, such as a campaign
    public const int SendCommandUsage = 25;

    // difficulty sync.
    public const int SyncDifficulties = 26;

    public static int AddPacketId(string name, int id) => Collection.ForcefullyInsert(name, id);

    public static readonly ReflectionDictionary<PacketID, int> Collection = new(MemberType.Fields);
}
