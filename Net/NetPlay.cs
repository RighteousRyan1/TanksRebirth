using LiteNetLib;
using LiteNetLib.Utils;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using TanksRebirth.GameContent;
using TanksRebirth.GameContent.Properties;
using TanksRebirth.GameContent.RebirthUtils;
using TanksRebirth.GameContent.Systems;
using TanksRebirth.GameContent.Systems.PingSystem;
using TanksRebirth.GameContent.UI;
using TanksRebirth.Internals.Common.Framework.Audio;
using TanksRebirth.Internals.Common.Utilities;

namespace TanksRebirth.Net;

/// <summary>
/// Holds the keys to all of what happens through the networking of the game.
/// </summary>
public class NetPlay {
    public static IPEndPoint? Ip;
    public static int Port;
    public static Client? CurrentClient;
    public static Server? CurrentServer;
    /// <summary>Whether or not to log packets going out or coming in.</summary>

    public static bool DoPacketLogging = false;

    public static string? ServerName;

    public delegate void OnRecieveServerPacketDelegate(int packet, NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod);
    /// <summary>Called when a packet is recieved server-side. This is called before all packets are handled.</summary>
    public static event OnRecieveServerPacketDelegate? OnReceiveServerPacket;
    public delegate void OnRecieveClientPacketDelegate(int packet, NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod);
    /// <summary>Called when a packet is recieved client-side. This is called before all packets are handled.</summary>
    public static event OnRecieveClientPacketDelegate? OnReceiveClientPacket;

    public static void MapClientNetworking() {
        Client.ClientListener.NetworkReceiveEvent += OnPacketRecieve_Client;
        Client.ClientListener.PeerConnectedEvent += OnClientJoin;
    }
    public static void UnmapClientNetworking() {
        Client.ClientListener.NetworkReceiveEvent -= OnPacketRecieve_Client;
        Client.ClientListener.PeerConnectedEvent -= OnClientJoin;
    }

    private static void OnClientJoin(NetPeer peer) {
        TankGame.ClientLog.Write($"Connected to remote server.", Internals.LogType.Debug);
        ChatSystem.SendMessage("Connected to server.", Color.Lime, netSend: true);

        Client.SendClientInfo();
    }

    public static void MapServerNetworking() {
        Server.NetListener.NetworkReceiveEvent += OnPacketRecieve_Server;
    }
    public static void UnmapServerNetworking() {
        Server.NetListener.NetworkReceiveEvent -= OnPacketRecieve_Server;
    }

    private static void OnPacketRecieve_Client(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod) {
        var packet = reader.GetInt();
        if (DebugManager.DebuggingEnabled) {
            if (deliveryMethod != DeliveryMethod.Unreliable) {
                // GameHandler.ClientLog.Write($"Packet Recieved: {packet} from peer {peer.Id}.", Internals.LogType.Debug);

                ChatSystem.SendMessage($"[DEBUG]: Recieved packet {PacketID.Collection.GetKey(packet)} from peer {peer.Id}", Color.Blue);
            }
        }
        OnReceiveClientPacket?.Invoke(packet, peer, reader, deliveryMethod);

        switch (packet) {
            #region Info
            case PacketID.ClientInfo:
                byte returnedID = reader.GetByte();

                CurrentClient.Id = returnedID;
                Client.RequestLobbyInfo();
                break;
            case PacketID.LobbyInfo:
                int serverMaxClients = reader.GetByte();

                string servName = reader.GetString();
                ServerName = servName;

                Server.ConnectedClients = new Client[serverMaxClients];

                byte clientId = 0;
                for (int i = 0; i < serverMaxClients; i++) {
                    bool isClientAvailable = reader.GetBool();

                    if (isClientAvailable) {
                        clientId = reader.GetByte();
                        string clientName = reader.GetString();

                        Server.ConnectedClients[i] = new Client(clientId, clientName);
                    }
                }

                Server.CurrentClientCount = reader.GetInt();

                Client.LobbyDataReceived = true;

                SoundPlayer.PlaySoundInstance("Assets/sounds/menu/client_join.ogg", SoundContext.Effect, 0.75f);

                ChatSystem.SendMessage($"Welcome {Server.ConnectedClients[clientId].Name}!", Color.Lime, netSend: true);

                MainMenu.ShouldServerButtonsBeVisible = false;
                break;
            case PacketID.Disconnect:
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

                SoundPlayer.PlaySoundInstance("Assets/sounds/menu/client_join.ogg", SoundContext.Effect, 0.75f);
                break;
            #endregion
            #region One-Off
            case PacketID.SyncSeeds:
                var millis = reader.GetInt();
                Server.RandSeed = millis;
                ChatSystem.SendMessage("Seed synced: " + millis, Color.Lime);
                break;
            case PacketID.SendCommandUsage:
                var cmd = reader.GetString();
                ChatSystem.SendMessage(cmd, Color.White, "cmd_sync");
                break;
            case PacketID.SendCampaign:
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

                for (int i = 0; i < missionCount; i++) {
                    var blockLen = reader.GetInt();
                    var tnkLen = reader.GetInt();

                    var missionName = reader.GetString();
                    var note = reader.GetString();

                    List<BlockTemplate> blockTotal = new();
                    List<TankTemplate> tankTotal = new();

                    for (int m = 0; m < blockLen; m++) {
                        var bPos = reader.GetVector2();
                        var bType = reader.GetInt();
                        var bStack = reader.GetByte();
                        var tpLink = reader.GetSByte();

                        blockTotal.Add(new() {
                            Position = bPos,
                            Type = bType,
                            Stack = bStack,
                            TpLink = tpLink,
                        });
                    }
                    for (var t = 0; t < tnkLen; t++) {
                        var tPos = reader.GetVector2();
                        var tRot = reader.GetFloat();
                        var tIsPlayer = reader.GetBool();
                        var tTeam = reader.GetInt();
                        var typeOrTier = reader.GetInt();

                        TankTemplate tmp = new() {
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

                    campaign.CachedMissions[i] = new(tankTotal.ToArray(), blockTotal.ToArray()) {
                        Name = missionName,
                        Note = note
                    };
                }
                CampaignGlobals.LoadedCampaign = campaign;

                break;
            case PacketID.StartGame:
                int checkpoint = reader.GetInt();
                bool shouldProgress = reader.GetBool();
                CampaignGlobals.ShouldMissionsProgress = shouldProgress;

                CampaignGlobals.LoadedCampaign.LoadMission(checkpoint); // maybe change to checkpoints eventually.

                MainMenu.TransitionToGame();

                break;
            case PacketID.QuitLevel:
                GameUI.QuitOut(true);
                break;
            case PacketID.PlayerSpawn:

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
            case PacketID.ChatMessage:
                string msg = reader.GetString();
                Color color = reader.GetColor();
                string sender = reader.GetString();

                ChatSystem.SendMessage(msg, color, sender, true);
                break;
            case PacketID.TankDamage:
                var hurtTankId = reader.GetInt();
                //var causingTankId = reader.GetInt();
                //var isPlayer = reader.GetBool();

                if (GameHandler.AllTanks[hurtTankId] is PlayerTank player) {
                    // checks if the damage recipient is themself
                    if (IsClientMatched(hurtTankId)) {

                    }
                }
                GameHandler.AllTanks[hurtTankId]?.Damage(null);

                break;
            case PacketID.ShellDestroy:
                var ownerId = reader.GetInt();
                var ownerShellIndex = reader.GetInt();
                var cxt = reader.GetByte();

                // FIXME: crashes if recieving on a dead player. weird.
                // FIXME? wait for results.
                // TODO: see if this works.

                if (GameHandler.AllTanks[ownerId] is not null)
                    if (ownerShellIndex > -1)
                        if (GameHandler.AllTanks[ownerId].OwnedShells.Length > ownerShellIndex)
                            if (GameHandler.AllTanks[ownerId].OwnedShells[ownerShellIndex] is not null)
                                GameHandler.AllTanks[ownerId].OwnedShells[ownerShellIndex]?.Destroy((Shell.DestructionContext)cxt, wasSentByAnotherClient: true);

                break;
            case PacketID.MineDetonate:
                var destroyedMineId = reader.GetInt();

                Mine.AllMines[destroyedMineId]?.Detonate();
                break;
            case PacketID.ShellFire:
                var shellType = reader.GetInt();
                var shellPos = reader.GetVector2();
                var shellVel = reader.GetVector2();
                var shellRicochets = reader.GetUInt();
                var shellOwner = reader.GetInt();

                // GameHandler.AllTanks[shellOwner].Shoot(true);
                var shell = new Shell(shellPos, shellVel, shellType, GameHandler.AllTanks[shellOwner], ricochets: shellRicochets);
                GameHandler.AllTanks[shellOwner].DoShootParticles(shell.Position3D);

                // ChatSystem.SendMessage($"Pos: {shell.Position} | Vel: {shell.Velocity}", Color.White);
                break;
            case PacketID.MinePlacement:
                var minePos = reader.GetVector2();
                var detTime = reader.GetFloat();
                var mineOwner = reader.GetInt();

                new Mine(GameHandler.AllTanks[mineOwner], minePos, detTime);

                break;
            case PacketID.SendCampaignByName:
                var campName = reader.GetString();
                var missionId = reader.GetInt();        // Obtain the mission id from the server itself. Fixes issues when loading missions.

                // if this solution fails, simply change param 2 (wasConfirmed) to true
                var success = MainMenu.PrepareGameplay(campName, false, true, missionId); // second param to false when doing a check
                Client.SendCampaignStatus(campName, CurrentClient.Id, success); // if this player doesn't own said campaign, cancel the operation.
                if (success) {
                    MainMenu.PrepareGameplay(campName, true, true, missionId);
                }
                break;
            case PacketID.Cleanup:
                SceneManager.CleanupScene();
                break;
            case PacketID.SendCampaignStatus:
                if (Client.IsHost()) {
                    var camName = reader.GetString();
                    var senderId = reader.GetInt();
                    var successful = reader.GetBool();
                    if (successful) {
                        MainMenu.plrsConfirmed++;
                        if (MainMenu.plrsConfirmed == Server.CurrentClientCount - 1)
                            MainMenu.PrepareGameplay(camName, true, false);
                        // lowkey praying this works.
                    }
                    //MainMenu.PrepareGameplay(camName, true, true);
                    else {
                        ChatSystem.SendMessage($"{Server.ConnectedClients[senderId].Name} does not own this campaign! Send it to them to be able to play it.", Color.Red);
                        SoundPlayer.SoundError();
                    }
                }
                break;
            case PacketID.MapPing:
                var loc = reader.GetVector3();
                var pingId = reader.GetInt();
                var pid = reader.GetInt();

                IngamePing.CreateFromTankSender(loc, pingId, pid);
                break;
            #endregion
            #region ConstantSends
            case PacketID.SyncPlayer:
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
            case PacketID.SyncAiTank:
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
            case PacketID.SyncLives:
                var clid = reader.GetInt();
                var lives = reader.GetInt();

                PlayerTank.Lives[clid] = lives;
                break;
            case PacketID.SyncDifficulties:
                for (int i = 0; i < Difficulties.Types.Count; i++) {
                    Difficulties.Types[Difficulties.Types.Keys.ElementAt(i)] = reader.GetBool();
                }
                Difficulties.RandomTanksLower = reader.GetInt();
                Difficulties.RandomTanksUpper = reader.GetInt();
                Difficulties.MonochromeValue = reader.GetInt();
                Difficulties.DisguiseValue = reader.GetInt();
                break;
                #endregion
        }

        //peer.Send(message, DeliveryMethod.ReliableOrdered);

        //GameHandler.ClientLog.Write(string.Join(",", reader.RawData), Internals.LogType.Debug);
        reader.Recycle();
    }

    private static void OnPacketRecieve_Server(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod) {
        var packet = reader.GetInt();
        if (DebugManager.DebuggingEnabled) {
            if (deliveryMethod != DeliveryMethod.Unreliable) {
                // GameHandler.ClientLog.Write($"Packet Recieved: {packet} from peer {peer.Id}.", Internals.LogType.Debug);

                ChatSystem.SendMessage($"[DEBUG]: Server recieved packet {PacketID.Collection.GetKey(packet)} from peer {peer.Id}", Color.Blue);
            }
        }

        NetDataWriter message = new();

        message.Put(packet);

        OnReceiveServerPacket?.Invoke(packet, peer, reader, deliveryMethod);

        switch (packet) {
            #region Info
            case PacketID.ClientInfo:
                string name = reader.GetString();

                Server.ConnectedClients[Server.CurrentClientCount] = new Client(Server.CurrentClientCount, name);
                message.Put(Server.CurrentClientCount);
                Server.CurrentClientCount++;

                //Server.serverNetManager.SendToAll(message, DeliveryMethod.ReliableOrdered);
                peer.Send(message, deliveryMethod);

                Server.SyncSeeds();
                break;
            case PacketID.LobbyInfo:
                message.Put(Server.MaxClients);     //This dang ushort was throwing the entire packet off

                message.Put(CurrentServer.Name);

                for (int i = 0; i < Server.MaxClients; i++) {
                    bool clientExists = Server.ConnectedClients[i] is not null;
                    message.Put(clientExists);
                    if (clientExists) {
                        Client client = Server.ConnectedClients[i];
                        message.Put((byte)client.Id);
                        message.Put(client.Name);
                    }
                }

                message.Put(Server.CurrentClientCount);

                Server.NetManager.SendToAll(message, deliveryMethod, peer);     //This sends to everyone but the one who sent
                peer.Send(message, deliveryMethod);     //This sends it back to the guy who sent

                break;
            case PacketID.Disconnect:
                var clientId = reader.GetInt();
                var clientName = reader.GetString();
                var reason = reader.GetString();

                message.Put(clientId);
                message.Put(clientName);
                message.Put(reason);

                Server.NetManager.SendToAll(message, deliveryMethod, peer);
                break;
            #endregion
            #region One-Off
            case PacketID.SyncSeeds:
                // no implementation since the server itself sends this packet.
                break;
            case PacketID.SendCommandUsage:
                var cmd = reader.GetString();
                message.Put(cmd);

                Server.NetManager.SendToAll(message, deliveryMethod, peer);
                break;
            case PacketID.ChatMessage:
                string msg = reader.GetString();
                Color color = reader.GetColor();
                string sender = reader.GetString();

                message.Put(msg);
                message.Put(color);
                message.Put(sender);

                Server.NetManager.SendToAll(message, deliveryMethod, peer);
                break;
            case PacketID.StartGame:
                int checkpoint = reader.GetInt();
                bool shouldMissionsProgress = reader.GetBool();
                message.Put(checkpoint);
                message.Put(shouldMissionsProgress);

                Server.NetManager.SendToAll(message, deliveryMethod, peer);
                break;
            case PacketID.QuitLevel:
                Server.NetManager.SendToAll(message, deliveryMethod, peer);
                break;
            case PacketID.PlayerSpawn:
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

                Server.NetManager.SendToAll(message, deliveryMethod, peer);
                break;
            case PacketID.TankDamage:
                var hurtTankId = reader.GetInt();
                message.Put(hurtTankId);

                Server.NetManager.SendToAll(message, deliveryMethod, peer);
                break;
            case PacketID.ShellDestroy:
                var ownerId = reader.GetInt();
                var ownerShellIndex = reader.GetInt();
                var cxt = reader.GetByte();

                message.Put(ownerId);
                message.Put(ownerShellIndex);
                message.Put(cxt);

                Server.NetManager.SendToAll(message, deliveryMethod, peer);
                break;
            case PacketID.MineDetonate:
                var destroyedMineId = reader.GetInt();
                message.Put(destroyedMineId);

                Server.NetManager.SendToAll(message, deliveryMethod, peer);
                break;
            case PacketID.ShellFire:
                var shellType = reader.GetInt();
                var shellPos = reader.GetVector2();
                var shellVel = reader.GetVector2();
                var shellRicochets = reader.GetUInt();
                var shellOwner = reader.GetInt();

                message.Put(shellType);
                message.Put(shellPos);
                message.Put(shellVel);
                message.Put(shellRicochets);
                message.Put(shellOwner);

                Server.NetManager.SendToAll(message, deliveryMethod, peer);
                break;
            case PacketID.MinePlacement:
                var minePos = reader.GetVector2();
                var detTime = reader.GetFloat();
                var mineOwner = reader.GetInt();

                message.Put(minePos);
                message.Put(detTime);
                message.Put(mineOwner);
                Server.NetManager.SendToAll(message, deliveryMethod, peer);
                break;
            case PacketID.SendCampaign:
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

                for (int i = 0; i < missionCount; i++) {
                    var blockLen = reader.GetInt();
                    var tnkLen = reader.GetInt();

                    var missionName = reader.GetString();
                    var note = reader.GetString();

                    message.Put(blockLen);
                    message.Put(tnkLen);

                    message.Put(missionName);
                    message.Put(note);

                    for (int m = 0; m < blockLen; m++) {
                        var bPos = reader.GetVector2();
                        var bType = reader.GetInt();
                        var bStack = reader.GetSByte();
                        var tpLink = reader.GetSByte();

                        message.Put(bPos);
                        message.Put(bType);
                        message.Put(bStack);
                        message.Put(tpLink);
                    }
                    for (var t = 0; t < tnkLen; t++) {
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

                Server.NetManager.SendToAll(message, deliveryMethod, peer);
                break;
            case PacketID.SendCampaignByName:
                var campName = reader.GetString();
                var missionId = reader.GetInt();
                message.Put(campName);
                message.Put(missionId);

                Server.NetManager.SendToAll(message, deliveryMethod, peer);
                break;
            case PacketID.Cleanup:
                Server.NetManager.SendToAll(message, deliveryMethod, peer);
                break;
            case PacketID.SendCampaignStatus:
                var camName = reader.GetString();
                var cliId = reader.GetInt();
                var success = reader.GetBool();
                message.Put(camName);
                message.Put(cliId);
                message.Put(success);
                //peer.Send(message, DeliveryMethod.Sequenced);
                Server.NetManager.SendToAll(message, deliveryMethod, peer);
                break;
            case PacketID.MapPing:
                var loc = reader.GetVector3();
                var pingId = reader.GetInt();
                var pid = reader.GetInt();
                message.Put(loc);
                message.Put(pingId);
                message.Put(pid);

                Server.NetManager.SendToAll(message, deliveryMethod, peer);
                break;
            #endregion
            #region ConstantSends
            case PacketID.SyncPlayer:
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

                Server.NetManager.SendToAll(message, deliveryMethod, peer);
                break;
            case PacketID.SyncAiTank:
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

                Server.NetManager.SendToAll(message, deliveryMethod, peer);
                break;
            case PacketID.SyncLives:

                var clid = reader.GetInt();
                var lives = reader.GetInt();

                message.Put(clid);
                message.Put(lives);

                Server.NetManager.SendToAll(message, deliveryMethod, peer);
                break;
            case PacketID.SyncDifficulties:
                for (int i = 0; i < Difficulties.Types.Count; i++) {
                    var val = reader.GetBool();
                    message.Put(val);
                }
                var lower = reader.GetInt();
                var upper = reader.GetInt();
                var monoValue = reader.GetInt();
                var disguiseValue = reader.GetInt();
                message.Put(lower);
                message.Put(upper);
                message.Put(monoValue);
                message.Put(disguiseValue);
                Server.NetManager.SendToAll(message, deliveryMethod, peer);
                break;
                #endregion
        }

        // peer.Send(message, DeliveryMethod.ReliableOrdered);

        // GameHandler.ClientLog.Write($"Packet Recieved: {packet} from client {peer.Id}. Current clients connected: {Server.CurrentClientCount}", Internals.LogType.Debug);
        reader.Recycle();
    }
    /// <summary>Check whether the current client's ID on the server matches a given integer.</summary>
    public static bool IsClientMatched(int otherId) {
        if (CurrentClient is null && CurrentServer is null)
            return true;
        if (CurrentClient is null && CurrentServer is not null)
            return false;
        if (CurrentClient.Id != otherId)
            return false;
        return true;
    }
    public static int GetMyClientId() {
        if (!Client.IsConnected())
            return 0;
        else
            return CurrentClient.Id;
    }
}

// [Tank = 0, Tank = 1, null = 2, null = 3, null = 4]

// find: null
// null -> 2
// new Tank id -> 2

// [Tank = 0, Tank = 1, Tank = 2, null = 3, null = 4]