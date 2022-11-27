using LiteNetLib;
using LiteNetLib.Utils;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using TanksRebirth.Enums;
using TanksRebirth.GameContent;
using TanksRebirth.GameContent.Properties;
using TanksRebirth.GameContent.Systems;
using TanksRebirth.GameContent.UI;
using TanksRebirth.Internals.Common.Framework.Audio;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Net;

namespace TanksRebirth.Net
{
    public class NetPlay
    {
        public static IPEndPoint Ip;
        public static int Port;
        public static Client CurrentClient;
        public static Server CurrentServer;

        public static bool DoPacketLogging = false;

        public static string ServerName;

        public static void MapClientNetworking()
        {
            Client.clientNetListener.NetworkReceiveEvent += OnPacketRecieve_Client;
            Client.clientNetListener.PeerConnectedEvent += OnClientJoin;
        }

        private static void OnClientJoin(NetPeer peer)
        {
            GameHandler.ClientLog.Write($"Connected to remote server.", Internals.LogType.Debug);
            ChatSystem.SendMessage("Connected to server.", Color.Lime, wasRecieved: true);

            Client.SendClientInfo();
        }

        public static void MapServerNetworking()
        {
            Server.serverNetListener.NetworkReceiveEvent += OnPacketRecieve_Server;
        }

        private static void OnPacketRecieve_Client(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            var packet = reader.GetPacket();
            if (DebugUtils.DebuggingEnabled) {
                if (deliveryMethod != DeliveryMethod.Unreliable) {
                    GameHandler.ClientLog.Write($"Packet Recieved: {packet} from peer {peer.Id}.", Internals.LogType.Debug);

                    ChatSystem.SendMessage($"[DEBUG]: Recieved packet {packet} from peer {peer.Id}", Color.Blue);
                }
            }

            switch (packet)
            {
                #region Info
                case PacketType.ClientInfo:
                    byte returnedID = reader.GetByte();

                    CurrentClient.Id = returnedID;
                    Client.RequestLobbyInfo();
                    break;

                case PacketType.LobbyInfo:
                    int serverMaxClients = reader.GetByte();

                    string servName = reader.GetString();
                    ServerName = servName;

                    Server.ConnectedClients = new Client[serverMaxClients];

                    byte clientId = 0;
                    for (int i = 0; i < serverMaxClients; i++)
                    {
                        bool isClientAvailable = reader.GetBool();

                        if (isClientAvailable)
                        {
                            clientId = reader.GetByte();
                            string clientName = reader.GetString();

                            Server.ConnectedClients[i] = new Client()
                            {
                                Id = clientId,
                                Name = clientName,
                            };
                        }
                    }

                    Server.CurrentClientCount = reader.GetInt();

                    Client.lobbyDataReceived = true;

                    SoundPlayer.PlaySoundInstance("Assets/sounds/menu/client_join", SoundContext.Effect, 0.75f);

                    ChatSystem.SendMessage($"Welcome {Server.ConnectedClients[clientId].Name}!", Color.Lime, wasRecieved: true);
                    break;
                case PacketType.Disconnect:
                    var cId = reader.GetInt();
                    var clName = reader.GetString();
                    var reason = reader.GetString();

                    ChatSystem.SendMessage($"{clName} left the game ({reason}).", Color.Red, "Server", true);

                    Server.ConnectedClients[cId] = null;
                    Server.CurrentClientCount--;

                    // shift from the client id index in the array to the end of the array.

                    // i.e: [ "Name1", "Name2", "Name3", "Name4", "Name5", null, null, null ]
                    // client "Name3" now leaves...
                    // i.e: [ "Name1", "Name2", null, "Name4", "Name5", null, null, null ]
                    // fill in that gap
                    // i.e: [ "Name1", "Name2", "Name4", "Name5", null, null, null, null ]
                    Server.ConnectedClients = ArrayUtils.Shift(Server.ConnectedClients, -1, cId, 0);

                    SoundPlayer.PlaySoundInstance("Assets/sounds/menu/client_join", SoundContext.Effect, 0.75f);
                    break;
                #endregion
                #region One-Off
                case PacketType.SendCampaign:
                    var campaign = new Campaign();

                    var cName = reader.GetString();
                    var startLives = reader.GetInt();
                    var bgColor = reader.GetColor();
                    var stripColor = reader.GetColor();

                    campaign.MetaData.Name = cName;
                    campaign.MetaData.StartingLives = startLives;
                    campaign.MetaData.BackgroundColor = bgColor;
                    campaign.MetaData.MissionStripColor = stripColor;

                    var missionCount = reader.GetInt();

                    campaign.CachedMissions = new Mission[missionCount];

                    for (int i = 0; i < missionCount; i++)
                    {
                        var blockLen = reader.GetInt();
                        var tnkLen = reader.GetInt();

                        var missionName = reader.GetString();
                        var note = reader.GetString();

                        List<BlockTemplate> blockTotal = new();
                        List<TankTemplate> tankTotal = new();

                        for (int m = 0; m < blockLen; m++)
                        {
                            var bPos = reader.GetVector2();
                            var bType = reader.GetInt();
                            var bStack = reader.GetSByte();
                            var tpLink = reader.GetSByte();

                            blockTotal.Add(new()
                            {
                                Position = bPos,
                                Type = bType,
                                Stack = bStack,
                                TpLink = tpLink,
                            });
                        }
                        for (var t = 0; t < tnkLen; t++)
                        {
                            var tPos = reader.GetVector2();
                            var tRot = reader.GetFloat();
                            var tIsPlayer = reader.GetBool();
                            var tTeam = reader.GetInt();
                            var typeOrTier = reader.GetInt();

                            TankTemplate tmp = new()
                            {
                                Position = tPos,
                                Rotation = tRot,
                                IsPlayer = tIsPlayer,
                                Team = tTeam
                            };

                            if (tmp.IsPlayer)
                                tmp.PlayerType = typeOrTier;
                            else
                                tmp.AiTier = typeOrTier;

                            tankTotal.Add(tmp);
                        }

                        campaign.CachedMissions[i] = new(tankTotal.ToArray(), blockTotal.ToArray())
                        {
                            Name = missionName,
                            Note = note
                        };
                    }
                    GameProperties.LoadedCampaign = campaign;

                    break;
                case PacketType.StartGame:
                    int checkpoint = reader.GetInt();
                    bool shouldProgress = reader.GetBool();
                    GameProperties.ShouldMissionsProgress = shouldProgress;

                    GameProperties.LoadedCampaign.LoadMission(checkpoint); // maybe change to checkpoints eventually.

                    MainMenu.TransitionToGame();

                    break;
                case PacketType.PlayerSpawn:

                    var type = reader.GetInt();
                    var team = reader.GetInt();

                    float x = reader.GetFloat();
                    float y = reader.GetFloat();
                    float tnkRot = reader.GetFloat();
                    float turRot = reader.GetFloat();

                    var tank = new PlayerTank(type);
                    tank.Body.Position = new(x, y);
                    tank.Dead = false;
                    tank.TankRotation = tnkRot;
                    tank.TurretRotation = turRot;
                    tank.Team = team;

                    break;
                case PacketType.ChatMessage:
                    string msg = reader.GetString();
                    Color color = reader.GetColor();
                    string sender = reader.GetString();

                    ChatSystem.SendMessage(msg, color, sender, true);
                    break;
                case PacketType.TankDamage:
                    var hurtTankId = reader.GetInt();
                    //var causingTankId = reader.GetInt();
                    //var isPlayer = reader.GetBool();

                    GameHandler.AllTanks[hurtTankId]?.Damage(null);

                    break;
                case PacketType.ShellDestroy:
                    var ownerId = reader.GetInt();
                    var ownerShellIndex = reader.GetInt();
                    var cxt = reader.GetByte();
                    // sends: 0
                    // count: 0
                    // perform: 0 - 1
                    // reality: -1, 0
                    // equation: -1 > 0
                    // result: break from the loop
                    if (GameHandler.AllTanks[ownerId] is null || GameHandler.AllTanks[ownerId].OwnedShells.Count > ownerShellIndex - 1
                        || GameHandler.AllTanks[ownerId].OwnedShells.Count <= ownerShellIndex)
                        break;
                    GameHandler.AllTanks[ownerId].OwnedShells[ownerShellIndex].Destroy((Shell.DestructionContext)cxt);
                    //Shell.AllShells[destroyedShellId]?.Destroy((Shell.DestructionContext)cxt);

                    break;
                case PacketType.MineDetonate:
                    var destroyedMineId = reader.GetInt();

                    Mine.AllMines[destroyedMineId]?.Detonate();
                    break;
                case PacketType.ShellFire:
                    var shellType = reader.GetInt();
                    var shellPos = reader.GetVector3();
                    var shellVel = reader.GetVector3();
                    var shellRicochets = reader.GetUInt();
                    var shellOwner = reader.GetInt();

                    // GameHandler.AllTanks[shellOwner].Shoot(true);
                    var shell = new Shell(shellPos, shellVel, shellType, GameHandler.AllTanks[shellOwner], ricochets: shellRicochets);

                    // ChatSystem.SendMessage($"Pos: {shell.Position} | Vel: {shell.Velocity}", Color.White);
                    break;
                case PacketType.MinePlacement:
                    var minePos = reader.GetVector2();
                    var detTime = reader.GetFloat();
                    var mineOwner = reader.GetInt();

                    new Mine(GameHandler.AllTanks[mineOwner], minePos, detTime);

                    break;
                case PacketType.SendCampaignByName:
                    var campName = reader.GetString();

                    MainMenu.PrepareGameplay(campName, true);
                    break;
                case PacketType.Cleanup:
                    GameHandler.CleanupScene();
                    break;
                #endregion
                #region ConstantSends
                case PacketType.SyncPlayer:
                    int id = reader.GetInt();
                    float x2 = reader.GetFloat();
                    float y2 = reader.GetFloat();
                    float tankRotation = reader.GetFloat();
                    float turretRotation = reader.GetFloat();
                    float vX = reader.GetFloat();
                    float vY = reader.GetFloat();

                    if (GameHandler.AllPlayerTanks[id] is null)
                        break;

                    GameHandler.AllPlayerTanks[id].Body.Position = new(x2, y2);
                    GameHandler.AllPlayerTanks[id].TankRotation = tankRotation;
                    GameHandler.AllPlayerTanks[id].TurretRotation = turretRotation;
                    GameHandler.AllPlayerTanks[id].Velocity = new(vX, vY);
                    break;
                case PacketType.SyncAiTank:
                    int id1 = reader.GetInt();
                    float x3 = reader.GetFloat();
                    float y3 = reader.GetFloat();
                    float tankRotation1 = reader.GetFloat();
                    float turretRotation1 = reader.GetFloat();
                    float vX1 = reader.GetFloat();
                    float vY1 = reader.GetFloat();

                    if (GameHandler.AllAITanks[id1] is null)
                        break;

                    GameHandler.AllAITanks[id1].Body.Position = new(x3, y3);
                    GameHandler.AllAITanks[id1].TankRotation = tankRotation1;
                    GameHandler.AllAITanks[id1].TurretRotation = turretRotation1;
                    GameHandler.AllAITanks[id1].Velocity = new(vX1, vY1);
                    break;
                case PacketType.SyncLives:
                    var clid = reader.GetInt();
                    var lives = reader.GetInt();

                    PlayerTank.Lives[clid] = lives;
                    break;
                #endregion
            }

            //peer.Send(message, DeliveryMethod.ReliableOrdered);

            //GameHandler.ClientLog.Write(string.Join(",", reader.RawData), Internals.LogType.Debug);
            reader.Recycle();
        }

        private static void OnPacketRecieve_Server(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            var packet = reader.GetPacket();
            if (DebugUtils.DebuggingEnabled) {
                if (deliveryMethod != DeliveryMethod.Unreliable) {
                    GameHandler.ClientLog.Write($"Packet Recieved: {packet} from peer {peer.Id}.", Internals.LogType.Debug);

                    ChatSystem.SendMessage($"[DEBUG]: Server recieved packet {packet} from peer {peer.Id}", Color.Blue);
                }
            }

            NetDataWriter message = new();

            message.Put(packet);

            //message.Put(packet);

            switch (packet)
            {
                #region Info
                case PacketType.ClientInfo:
                    string name = reader.GetString();

                    Server.ConnectedClients[Server.CurrentClientCount] = new Client()
                    {
                        Id = Server.CurrentClientCount,
                        Name = name
                    };
                    message.Put((byte)Server.CurrentClientCount);
                    Server.CurrentClientCount++;

                    //Server.serverNetManager.SendToAll(message, DeliveryMethod.ReliableOrdered);
                    peer.Send(message, deliveryMethod);
                    break;
                case PacketType.LobbyInfo:
                    message.Put((byte)Server.MaxClients);     //This dang ushort was throwing the entire packet off

                    message.Put(CurrentServer.Name);

                    for (int i = 0; i < Server.MaxClients; i++)
                    {
                        bool clientExists = Server.ConnectedClients[i] is not null;
                        message.Put(clientExists);
                        if (clientExists)
                        {
                            Client client = Server.ConnectedClients[i];
                            message.Put((byte)client.Id);
                            message.Put(client.Name);
                        }
                    }

                    message.Put(Server.CurrentClientCount);

                    Server.serverNetManager.SendToAll(message, DeliveryMethod.ReliableOrdered, peer);     //This sends to everyone but the one who sent
                    peer.Send(message, deliveryMethod);     //This sends it back to the guy who sent

                    break;
                case PacketType.Disconnect:
                    var clientId = reader.GetInt();
                    var clientName = reader.GetString();
                    var reason = reader.GetString();

                    message.Put(clientId);
                    message.Put(clientName);
                    message.Put(reason);

                    Server.serverNetManager.SendToAll(message, DeliveryMethod.Sequenced, peer);
                    break;
                #endregion
                #region One-Off
                case PacketType.ChatMessage:
                    string msg = reader.GetString();
                    Color color = reader.GetColor();
                    string sender = reader.GetString();

                    message.Put(msg);
                    message.Put(color);
                    message.Put(sender);

                    Server.serverNetManager.SendToAll(message, DeliveryMethod.ReliableOrdered, peer);
                    break;
                case PacketType.StartGame:
                    int checkpoint = reader.GetInt();
                    bool shouldMissionsProgress = reader.GetBool();
                    message.Put(checkpoint);
                    message.Put(shouldMissionsProgress);

                    Server.serverNetManager.SendToAll(message, DeliveryMethod.ReliableOrdered, peer);
                    break;
                case PacketType.PlayerSpawn:
                    var type = reader.GetInt();
                    var team = reader.GetInt();

                    float x = reader.GetFloat();
                    float y = reader.GetFloat();
                    float tnkRot = reader.GetFloat();
                    float turRot = reader.GetFloat();

                    message.Put(type);
                    message.Put(team);
                    message.Put(x);
                    message.Put(y);
                    message.Put(tnkRot);
                    message.Put(turRot);

                    Server.serverNetManager.SendToAll(message, DeliveryMethod.ReliableOrdered, peer);
                    break;

                case PacketType.TankDamage:
                    var hurtTankId = reader.GetInt();
                    message.Put(hurtTankId);

                    Server.serverNetManager.SendToAll(message, DeliveryMethod.ReliableOrdered, peer);
                    break;
                case PacketType.ShellDestroy:
                    var ownerId = reader.GetInt();
                    var ownerShellIndex = reader.GetInt();
                    var cxt = reader.GetByte();

                    message.Put(ownerId);
                    message.Put(ownerShellIndex);
                    message.Put(cxt);

                    Server.serverNetManager.SendToAll(message, DeliveryMethod.ReliableOrdered, peer);
                    break;
                case PacketType.MineDetonate:
                    var destroyedMineId = reader.GetInt();
                    message.Put(destroyedMineId);

                    Server.serverNetManager.SendToAll(message, DeliveryMethod.ReliableOrdered, peer);
                    break;
                case PacketType.ShellFire:
                    var shellType = reader.GetInt();
                    var shellPos = reader.GetVector3();
                    var shellVel = reader.GetVector3();
                    var shellRicochets = reader.GetUInt();
                    var shellOwner = reader.GetInt();

                    message.Put(shellType);
                    message.Put(shellPos);
                    message.Put(shellVel);
                    message.Put(shellRicochets);
                    message.Put(shellOwner);

                    Server.serverNetManager.SendToAll(message, DeliveryMethod.Sequenced, peer);
                    break;
                case PacketType.MinePlacement:
                    var minePos = reader.GetVector2();
                    var detTime = reader.GetFloat();
                    var mineOwner = reader.GetInt();

                    message.Put(minePos);
                    message.Put(detTime);
                    message.Put(mineOwner);
                    Server.serverNetManager.SendToAll(message, DeliveryMethod.Sequenced, peer);
                    break;
                case PacketType.SendCampaign:
                    var cName = reader.GetString();
                    var startLives = reader.GetInt();
                    var bgColor = reader.GetColor();
                    var stripColor = reader.GetColor();

                    var missionCount = reader.GetInt();

                    message.Put(cName);
                    message.Put(startLives);
                    message.Put(bgColor);
                    message.Put(stripColor);
                    message.Put(missionCount);

                    for (int i = 0; i < missionCount; i++)
                    {
                        var blockLen = reader.GetInt();
                        var tnkLen = reader.GetInt();

                        var missionName = reader.GetString();
                        var note = reader.GetString();

                        message.Put(blockLen);
                        message.Put(tnkLen);

                        message.Put(missionName);
                        message.Put(note);

                        for (int m = 0; m < blockLen; m++)
                        {
                            var bPos = reader.GetVector2();
                            var bType = reader.GetInt();
                            var bStack = reader.GetSByte();
                            var tpLink = reader.GetSByte();

                            message.Put(bPos);
                            message.Put(bType);
                            message.Put(bStack);
                            message.Put(tpLink);
                        }
                        for (var t = 0; t < tnkLen; t++)
                        {
                            var tPos = reader.GetVector2();
                            var tRot = reader.GetFloat();
                            var tIsPlayer = reader.GetBool();
                            var tTeam = reader.GetInt();
                            var typeOrTier = reader.GetInt();

                            message.Put(tPos);
                            message.Put(tRot);
                            message.Put(tIsPlayer);
                            message.Put(tTeam);
                            message.Put(typeOrTier);
                        }
                    }

                    Server.serverNetManager.SendToAll(message, DeliveryMethod.Sequenced, peer);
                    break;
                case PacketType.SendCampaignByName:
                    var campName = reader.GetString();
                    message.Put(campName);

                    Server.serverNetManager.SendToAll(message, DeliveryMethod.Sequenced, peer);
                    break;
                case PacketType.Cleanup:
                    Server.serverNetManager.SendToAll(message, DeliveryMethod.Sequenced, peer);
                    break;
                #endregion
                #region ConstantSends
                case PacketType.SyncPlayer:
                    int id = reader.GetInt();
                    float x2 = reader.GetFloat();
                    float y2 = reader.GetFloat();
                    float tankRotation = reader.GetFloat();
                    float turretRotation = reader.GetFloat();
                    float vX = reader.GetFloat();
                    float vY = reader.GetFloat();

                    message.Put(id);
                    message.Put(x2);
                    message.Put(y2);
                    message.Put(tankRotation);
                    message.Put(turretRotation);
                    message.Put(vX);
                    message.Put(vY);

                    Server.serverNetManager.SendToAll(message, DeliveryMethod.Unreliable, peer);
                    break;
                case PacketType.SyncAiTank:
                    int id1 = reader.GetInt();
                    float x3 = reader.GetFloat();
                    float y3 = reader.GetFloat();
                    float tankRotation1 = reader.GetFloat();
                    float turretRotation1 = reader.GetFloat();
                    float vX1 = reader.GetFloat();
                    float vY1 = reader.GetFloat();

                    message.Put(id1);
                    message.Put(x3);
                    message.Put(y3);
                    message.Put(tankRotation1);
                    message.Put(turretRotation1);
                    message.Put(vX1);
                    message.Put(vY1);

                    Server.serverNetManager.SendToAll(message, DeliveryMethod.Unreliable, peer);
                    break;
                case PacketType.SyncLives:

                    var clid = reader.GetInt();
                    var lives = reader.GetInt();

                    message.Put(clid);
                    message.Put(lives);

                    Server.serverNetManager.SendToAll(message, DeliveryMethod.Unreliable, peer);
                    break;
                    #endregion
            }

            // peer.Send(message, DeliveryMethod.ReliableOrdered);

            GameHandler.ClientLog.Write($"Packet Recieved: {packet} from client {peer.Id}. Current clients connected: {Server.CurrentClientCount}", Internals.LogType.Debug);
            reader.Recycle();
        }
        public static bool IsClientMatched(int otherId)
        {
            if (CurrentClient is null && CurrentServer is null)
                return true;
            if (CurrentClient is null && CurrentServer is not null)
                return false;
            if (CurrentClient.Id != otherId)
                return false;
            return true;
        }
    }

    // [Tank = 0, Tank = 1, null = 2, null = 3, null = 4]

    // find: null
    // null -> 2
    // new Tank id -> 2

    // [Tank = 0, Tank = 1, Tank = 2, null = 3, null = 4]

    public enum PacketType : byte
    {
        // First-time networking
        ClientInfo,
        LobbyInfo,

        // Ingame packets: players
        SyncPlayer,
        SyncLives,

        // Ingame packets: ai
        SyncAiTank,

        // Ingame packets: entitites
        TankDamage,
        ShellDestroy,
        MineDetonate,

        // Ingame packets: tank mechanics
        MinePlacement,
        ShellFire,

        // Human communication

        ChatMessage,

        // Debugging packets:

        PlayerSpawn,
        AiTankSpawn,
        CrateSpawn,

        // more server syncing

        ServerNameSync,
        StartGame,
        LeaveGame,
        Disconnect,

        // map sync

        SendCampaign,
        SendCampaignByName,
        SendMission,

        // misc

        SyncShellId,
        Cleanup
    }
}