using LiteNetLib;
using LiteNetLib.Utils;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using TanksRebirth.Enums;
using TanksRebirth.GameContent;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.RebirthUtils;
using TanksRebirth.GameContent.Systems;
using TanksRebirth.GameContent.Systems.LevelSystem;
using TanksRebirth.GameContent.Systems.PingSystem;
using TanksRebirth.GameContent.Tanks;

using TanksRebirth.GameContent.UI;
using TanksRebirth.GameContent.UI.MainMenu;
using TanksRebirth.Internals.Common.Framework.Audio;

namespace TanksRebirth.Net;

// TODO: optimize, wildly
// change to serialized messages and deserialized ones, like
// new SyncLivesMessage(player: 1, lives: 2) or something
/// <summary>
/// Holds the keys to all of what happens through the networking of the game.
/// </summary>
public static class NetPlay {
    public static IPEndPoint? Ip;
    public static int Port;
    public static Client CurrentClient;
    public static Server CurrentServer;

    // real client id to game client id
    /// <summary>Takes in the client's real client ID (<see cref="NetPeer.Id"/>) and returns its in-game mapped ID (<see cref="Client.Id"/>).</summary>
    public static Dictionary<int, int> PeerMap { get; set; } = [];

    /// <summary>Takes in the client's mapped client ID (<see cref="Client.Id"/>) and returns its real ID (<see cref="NetPeer.Id"/>).</summary>
    public static Dictionary<int, int> ReversePeerMap { get; set; } = [];

    /// <summary>Whether or not to log packets going out or coming in.</summary>
    public static bool DoPacketLogging = false;

    public static string? ServerName;

    /// <summary>How many clients were in the lobby as of the last lobby info this client got.</summary>
    internal static int LobbyClientCount;

    //public static ConcurrentQueue<Action> NetworkQueue = [];
    //public static Thread NetworkingThread;

    public delegate void OnRecieveServerPacketDelegate(int packet, NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod);
    /// <summary>Called when a packet is recieved server-side. This is called before all packets are handled.</summary>
    public static event OnRecieveServerPacketDelegate? OnReceiveServerPacket;
    public delegate void OnRecieveClientPacketDelegate(int packet, NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod);
    /// <summary>Called when a packet is recieved client-side. This is called before all packets are handled.</summary>
    public static event OnRecieveClientPacketDelegate? OnReceiveClientPacket;
    public static void MapClientNetworking() {
        Client.ClientListener.NetworkReceiveEvent += OnPacketRecieve_Client;
        Client.ClientListener.PeerConnectedEvent += OnClientJoin;
        Client.ClientListener.PeerDisconnectedEvent += OnClientLeave;
    }
    public static void UnmapClientNetworking() {
        if (Client.ClientListener is null) return;

        Client.ClientListener.NetworkReceiveEvent -= OnPacketRecieve_Client;
        Client.ClientListener.PeerConnectedEvent -= OnClientJoin;
        Client.ClientListener.PeerDisconnectedEvent -= OnClientLeave;
    }

    // the connection to the server ended without this client choosing to leave
    // will need to
    static void OnClientLeave(NetPeer peer, DisconnectInfo disconnectInfo) {
        var reason = disconnectInfo.Reason switch {
            DisconnectReason.ConnectionRejected => "Wrong password, or lobby may be full.",
            DisconnectReason.ConnectionFailed => "Could not connect to the server.",
            DisconnectReason.Timeout => "Lost connection to the server.",
            DisconnectReason.RemoteConnectionClose or DisconnectReason.DisconnectPeerCalled => "The host has closed the server.",
            _ => $"Disconnected from the server. ({disconnectInfo.Reason})",
        };
        TankGame.ClientLog.Write(reason, Internals.RebirthLogLevel.Info);
        ChatSystem.SendMessage(reason, Color.Red);
        Client.Disconnect();
    }

    private static void OnClientJoin(NetPeer peer) {
        TankGame.ClientLog.Write($"Connected to remote server.", Internals.RebirthLogLevel.Debug);
        ChatSystem.SendMessage("Connected to server.", Color.Lime, netSend: true);

        Client.SendClientInfo();
    }

    public static void PollEvents() {
        if (CurrentClient is not null)
            Client.ClientManager.PollEvents();
        if (CurrentServer is not null)
            Server.NetManager.PollEvents();
    }

    public static void MapServerNetworking() {
        Server.NetListener.NetworkReceiveEvent += OnPacketRecieve_Server;
    }
    public static void TryUnmapServerNetworking() {
        if (Server.NetManager is null) return;

        Server.SendHostDisconnect();
        // also tells every client that the connection closed, so they all disconnect
        Server.NetManager.Stop();

        Server.NetListener.NetworkReceiveEvent -= OnPacketRecieve_Server;
    }

    private static void OnPacketRecieve_Client(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod) {
        var packet = reader.GetInt();
        if (GameLauncher.IsConsoleAllocated) {
            if (DebugManager.DebuggingEnabled) {
                if (deliveryMethod != DeliveryMethod.Unreliable) {
                    // GameHandler.ClientLog.Write($"Packet Recieved: {packet} from peer {peer.Id}.", Internals.RebirthLogLevel.Debug);
                    Console.WriteLine($"[DEBUG]: Recieved packet {PacketID.Collection.GetKey(packet)} from peer {peer.Id}", Color.Blue);
                }
            }
        }
        OnReceiveClientPacket?.Invoke(packet, peer, reader, deliveryMethod);

        switch (packet) {
            #region Info
            case PacketID.ClientInfo:
                int returnedID = reader.GetInt();

                CurrentClient.Id = returnedID;

                PeerMap[Client.NetClient.Id] = returnedID;
                ReversePeerMap[returnedID] = Client.NetClient.Id;

                ChatSystem.SendMessage($"Connected as peer {Client.NetClient.Id} -> {returnedID}");

                Client.RequestLobbyInfo();
                break;
            case PacketID.LobbyInfo:
                int serverMaxClients = reader.GetByte();

                string servName = reader.GetString();
                ServerName = servName;

                // the server keeps its own list
                if (!Client.IsHost())
                    Server.ConnectedClients = new Client[serverMaxClients];

                for (int i = 0; i < serverMaxClients; i++) {
                    bool isClientAvailable = reader.GetBool();

                    if (isClientAvailable) {
                        byte clientId = reader.GetByte();
                        string clientName = reader.GetString();

                        if (!Client.IsHost())
                            Server.ConnectedClients![i] = new Client(clientId, clientName, isOperatedByPlayer: isClientAvailable);
                    }
                }

                Server.CurrentClientCount = reader.GetInt();
                // also sent when someone leaves, so only a bigger lobby means someone joined
                if (Server.CurrentClientCount > LobbyClientCount)
                    SoundPlayer.PlaySoundInstance("Assets/sounds/menu/client_join.ogg", SoundContext.Effect, 0.75f);
                LobbyClientCount = Server.CurrentClientCount;

                // only the client that just joined says hello, so it isn't repeated by everyone
                if (!Client.LobbyDataReceived)
                    ChatSystem.SendMessage($"Welcome {Server.ConnectedClients![CurrentClient.Id]?.Name}!", Color.Lime, netSend: true);
                Client.LobbyDataReceived = true;

                MainMenuUI.ShouldServerButtonsBeVisible = false;
                break;
            case PacketID.LeaveGame:
                int leftSlot = reader.GetInt();
                var leftName = reader.GetString();
                var leaveReason = (DisconnectReason)reader.GetInt();

                ChatSystem.SendMessage(leaveReason == DisconnectReason.Timeout ? $"{leftName} lost connection." : $"{leftName} has left.", Color.Red);
                SoundPlayer.PlaySoundInstance("Assets/sounds/menu/client_leave.ogg", SoundContext.Effect, 0.75f);

                // gone without the effects of being destroyed, and not brought back by the next mission
                GameHandler.AllPlayerTanks[leftSlot]?.Remove(true);
                PlayerTank.Lives[leftSlot] = 0;

                // the player who left may have been the last one alive
                if (CampaignGlobals.InMission)
                    CampaignProgression.CheckMissionCompletion();
                break;
            case PacketID.HostDisconnect:
                // forcibly disconnects the user
                ChatSystem.SendMessage("The host has disconnected. Disconnecting...", Color.Red);
                Client.Disconnect();
                break;
            #endregion
            #region One-Off
            case PacketID.SyncSeeds:
                var millis = reader.GetInt();
                Server.SetSessionSeed(millis);
                break;
            case PacketID.SendCommandUsage:
                var cmd = reader.GetString();
                TankGame.IngameConsole.ProcessCommand(cmd, false);
                break;
            case PacketID.StartGame:
                int checkpoint = reader.GetInt();
                bool shouldProgress = reader.GetBool();
                CampaignGlobals.ShouldMissionsProgress = shouldProgress;

                CampaignGlobals.LoadedCampaign.LoadMission(checkpoint); // maybe change to checkpoints eventually.

                MainMenuUI.TransitionToGame();

                break;
            case PacketID.QuitLevel:
                GameUI.QuitOut(true);
                break;
            case PacketID.StartOver:
                GameUI.StartOver(true);
                break;
            case PacketID.MissionEnd:
                int endRestartTime = reader.GetInt();
                var endContext = (MissionEndContext)reader.GetInt();
                bool endExtraLife = reader.GetBool();
                int endMissionId = reader.GetInt();

                // the host's lives are the real ones
                int endLifeCount = reader.GetInt();
                for (int i = 0; i < endLifeCount; i++) {
                    int hostLives = reader.GetInt();
                    if (i < PlayerTank.Lives.Length)
                        PlayerTank.Lives[i] = hostLives;
                }

                IntermissionHandler.ApplyHostMissionEnd(endRestartTime, endContext, endExtraLife, endMissionId);
                break;
            case PacketID.PlayerSpawn:

                var type = reader.GetInt();
                var team = reader.GetInt();

                float x = reader.GetFloat();
                float y = reader.GetFloat();
                float tnkRot = reader.GetFloat();
                float turRot = reader.GetFloat();

                var tank = new PlayerTank(type);
                tank.Physics.Position = new(x, y);
                tank.IsDestroyed = false;
                tank.ChassisRotation = tnkRot;
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

                var colorOverride = reader.GetColor();

                // why did i make it this way
                TankHurtContextStandard context = new(null);

                GameHandler.AllTanks[hurtTankId]?.Damage(context, false, colorOverride);
                break;
            case PacketID.ShellDestroy:
                var destroyedNetId = reader.GetInt();
                var cxt = reader.GetByte();

                var destroyedShell = Shell.FindByNetId(destroyedNetId);
                if (destroyedShell is not null)
                    destroyedShell.Destroy((Shell.DestructionContext)cxt, wasSentByAnotherClient: true);
                else
                    Shell.LogDeadShell(destroyedNetId);
                break;
            case PacketID.MineDetonate:
                var detonatedMineNetId = reader.GetInt();

                Mine.FindByNetId(detonatedMineNetId)?.Detonate(netSend: false);
                break;
            case PacketID.ShellFire:
                var shellType = reader.GetInt();
                var shellPos = reader.GetVector2();
                var shellVel = reader.GetVector2();
                var shellRicochets = reader.GetInt();
                var shellOwner = reader.GetInt();
                var shellNetId = reader.GetInt();
                var shellVolley = reader.GetInt();

                // already destroyed (the destroy message overtook this one), or already here
                if (Shell.WasDestroyed(shellNetId) || Shell.FindByNetId(shellNetId) is not null)
                    break;

                // before there was some bs that just straight up caused this code to malfunction via lag
                var shooter = shellOwner >= 0 && shellOwner < GameHandler.AllTanks.Length ? GameHandler.AllTanks[shellOwner] : null;
                var firedShell = Shell.Create(shellPos, shellVel, shellType, shooter, shellRicochets);
                // no room for it
                if (firedShell is null)
                    break;
                firedShell.SetNetId(shellNetId);
                firedShell.VolleyId = shellVolley;
                if (shooter is not null) {
                    firedShell.Properties.Homing = shooter.Properties.ShellHoming;
                    shooter.DoShootParticles();
                }
                break;
            case PacketID.MinePlacement:
                var minePos = reader.GetVector2();
                var detTime = reader.GetFloat();
                var mineOwner = reader.GetInt();
                var mineNetId = reader.GetInt();

                var mineLayer = mineOwner >= 0 && mineOwner < GameHandler.AllTanks.Length ? GameHandler.AllTanks[mineOwner] : null;
                Mine.Create(mineLayer, minePos, detTime)?.SetNetId(mineNetId);

                break;
            case PacketID.SendCampaign:
                var campName = reader.GetString();
                var missionId = reader.GetInt();        // Obtain the mission id from the server itself. Fixes issues when loading missions.
                Server.SetSessionSeed(reader.GetInt());

                // if this solution fails, simply change param 2 (wasConfirmed) to true
                var success = MainMenuUI.PrepareGameplay(campName, false, true, missionId); // second param to false when doing a check
                Client.SendCampaignStatus(campName, CurrentClient.Id, success); // if this player doesn't own said campaign, cancel the operation.
                if (success) {
                    MainMenuUI.PrepareGameplay(campName, true, true, missionId);
                }
                break;
            case PacketID.Cleanup:
                SceneManager.CleanupScene();
                break;
            case PacketID.SendCampaignStatus:
                if (Client.IsHost()) {
                    var camName = reader.GetString();
                    var returningId = reader.GetInt();
                    var successful = reader.GetBool();
                    if (successful) {
                        MainMenuUI.plrsConfirmed++;
                        if (MainMenuUI.plrsConfirmed == Server.CurrentClientCount - 1)
                            MainMenuUI.PrepareGameplay(camName, true, false);
                        // lowkey praying this works.
                    }
                    //MainMenu.PrepareGameplay(camName, true, true);
                    else {
                        ChatSystem.SendMessage($"{Server.ConnectedClients[returningId].Name} does not own this campaign! Send it to them to be able to play it.", Color.Red);
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
            case PacketID.SendAirplane:
                var planePos = reader.GetVector2();
                var planeVel = reader.GetVector2();

                Airplane.SpawnPlaneWithSmokeGrenades(planePos, planeVel, true);
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

                GameHandler.AllPlayerTanks[id].Physics.Position = new(x2, y2);
                GameHandler.AllPlayerTanks[id].ChassisRotation = tankRotation;
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

                bool hasTarget = reader.GetBool();
                bool seesTarget = reader.GetBool();

                if (GameHandler.AllAITanks[id1] is null)
                    break;

                if (hasTarget) {
                    var targetWorldId = reader.GetInt();
                    GameHandler.AllAITanks[id1].TargetTank = GameHandler.AllTanks[targetWorldId];
                    GameHandler.AllAITanks[id1].SeesTarget = seesTarget;
                }

                GameHandler.AllAITanks[id1].Physics.Position = new(x3, y3);
                GameHandler.AllAITanks[id1].ChassisRotation = tankRotation1;
                GameHandler.AllAITanks[id1].TurretRotation = turretRotation1;
                GameHandler.AllAITanks[id1].Velocity = new(vX1, vY1);
                break;
            case PacketID.SyncLives:
                var clid = reader.GetInt();
                var lives = reader.GetInt();

                // the host already took the life away when the tank died. a send from before that client saw its own death would give it back
                if (Client.IsHost() && CampaignGlobals.InMission && GameHandler.AllPlayerTanks[clid] is { IsDestroyed: true })
                    break;

                PlayerTank.Lives[clid] = lives;
                break;
            case PacketID.SyncKills:
                var clientid = reader.GetInt();
                var kills = reader.GetInt();

                PlayerTank.KillCounts[clientid] = kills;
                break;
            case PacketID.SyncDifficulties:
                var modifierCount = reader.GetInt();
                var modifierValues = new List<KeyValuePair<string, int>>(modifierCount);
                for (int i = 0; i < modifierCount; i++) {
                    var key = reader.GetString();
                    modifierValues.Add(new(key, reader.GetInt()));
                }
                var unknownModifiers = Modifiers.ApplyHostValues(modifierValues);
                if (unknownModifiers.Count > 0)
                    TankGame.ClientLog.Write($"The host has modifiers that aren't loaded here (missing a mod?): {string.Join(", ", unknownModifiers)}", Internals.RebirthLogLevel.Warn);
                break;
                #endregion
        }


        reader.Recycle();
    }

    private static void OnPacketRecieve_Server(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod) {
        var packet = reader.GetInt();
        if (DebugManager.DebuggingEnabled) {
            if (deliveryMethod != DeliveryMethod.Unreliable) {
                // GameHandler.ClientLog.Write($"Packet Recieved: {packet} from peer {peer.Id}.", Internals.RebirthLogLevel.Debug);

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

                int slot = Server.AddClient(peer, name);
                // every slot filled up between accepting the connection and now
                if (slot < 0) {
                    peer.Disconnect();
                    break;
                }
                message.Put(slot);

                peer.Send(message, deliveryMethod);
                Server.SyncSeeds();
                break;
            case PacketID.LobbyInfo:
                Server.SendLobbyInfo();
                break;
            case PacketID.HostDisconnect:
                // tells all other peers that the host disconnected
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
            case PacketID.StartOver:
                Server.NetManager.SendToAll(message, deliveryMethod, peer);
                break;
            case PacketID.MissionEnd:
                int endRestartTime = reader.GetInt();
                int endContext = reader.GetInt();
                bool endExtraLife = reader.GetBool();
                int endMissionId = reader.GetInt();
                message.Put(endRestartTime);
                message.Put(endContext);
                message.Put(endExtraLife);
                message.Put(endMissionId);

                int endLifeCount = reader.GetInt();
                message.Put(endLifeCount);
                for (int i = 0; i < endLifeCount; i++)
                    message.Put(reader.GetInt());

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
                var colorOverride = reader.GetColor();

                message.Put(hurtTankId);
                message.Put(colorOverride);

                Server.NetManager.SendToAll(message, deliveryMethod, peer);
                break;
            case PacketID.ShellDestroy:
                var senderShellNetId = reader.GetInt();
                var cxt = reader.GetByte();

                message.Put(senderShellNetId);
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
                var shellRicochets = reader.GetInt();
                var shellOwner = reader.GetInt();
                var shellNetId = reader.GetInt();
                var shellVolley = reader.GetInt();

                message.Put(shellType);
                message.Put(shellPos);
                message.Put(shellVel);
                message.Put(shellRicochets);
                message.Put(shellOwner);
                message.Put(shellNetId);
                message.Put(shellVolley);

                Server.NetManager.SendToAll(message, deliveryMethod, peer);
                break;
            case PacketID.MinePlacement:
                var minePos = reader.GetVector2();
                var detTime = reader.GetFloat();
                var mineOwner = reader.GetInt();
                var mineNetId = reader.GetInt();

                message.Put(minePos);
                message.Put(detTime);
                message.Put(mineOwner);
                message.Put(mineNetId);
                Server.NetManager.SendToAll(message, deliveryMethod, peer);
                break;
            case PacketID.SendCampaign:
                var campName = reader.GetString();
                var missionId = reader.GetInt();
                var sessionSeed = reader.GetInt();
                message.Put(campName);
                message.Put(missionId);
                message.Put(sessionSeed);

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
            case PacketID.SendAirplane:
                var planePos = reader.GetVector2();
                var planeVel = reader.GetVector2();

                message.Put(planePos);
                message.Put(planeVel);

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

                bool hasTarget = reader.GetBool();
                bool seesTarget = reader.GetBool();

                message.Put(id1);
                message.Put(x3);
                message.Put(y3);
                message.Put(tankRotation1);
                message.Put(turretRotation1);
                message.Put(vX1);
                message.Put(vY1);
                message.Put(hasTarget);
                message.Put(seesTarget);

                if (hasTarget) {
                    int targetWorldId = reader.GetInt();
                    message.Put(targetWorldId);
                }

                Server.NetManager.SendToAll(message, deliveryMethod, peer);
                break;
            case PacketID.SyncLives:

                var clid = reader.GetInt();
                var lives = reader.GetInt();

                message.Put(clid);
                message.Put(lives);

                Server.NetManager.SendToAll(message, deliveryMethod, peer);
                break;
            case PacketID.SyncKills:
                var clientid = reader.GetInt();
                var kills = reader.GetInt();

                message.Put(clientid);
                message.Put(kills);

                Server.NetManager.SendToAll(message, deliveryMethod, peer);
                break;
            case PacketID.SyncDifficulties:
                var relayCount = reader.GetInt();
                message.Put(relayCount);
                for (int i = 0; i < relayCount; i++) {
                    message.Put(reader.GetString());
                    message.Put(reader.GetInt());
                }
                Server.NetManager.SendToAll(message, deliveryMethod, peer);
                break;
                #endregion
        }

        // peer.Send(message, DeliveryMethod.ReliableOrdered);

        // GameHandler.ClientLog.Write($"Packet Recieved: {packet} from client {peer.Id}. Current clients connected: {Server.CurrentClientCount}", Internals.RebirthLogLevel.Debug);
        reader.Recycle();
    }
    /// <summary>Check whether the current client's ID on the server matches a given integer.</summary>
    public static bool IsClientMatched(int otherId) {
        if (CurrentClient is null && CurrentServer is null) return true;
        if (CurrentClient is null && CurrentServer is not null) return false;
        if (CurrentClient.Id != otherId) return false;
        return true;
    }

    // this stuff is really hard to name. help anyone?
    /// <summary>Whether this client decides what <paramref name="owner"/>'s shells and explosions hit, so only one client reports each hit.
    /// Players decide for their own tank. The host decides for AI tanks, players who left, and anything without an owner.</summary>
    public static bool IsReporter(Tank? owner) {
        if (!Client.IsConnected()) return true;
        if (owner is PlayerTank player && Server.IsOccupied(player.PlayerId))
            return IsClientMatched(player.PlayerId);
        return Client.IsHost();
    }
    public static int GetMyClientId() {
        if (!Client.IsConnected()) return 0;
        else return CurrentClient.Id;
    }
}

// [Owner = 0, Owner = 1, null = 2, null = 3, null = 4]

// find: null
// null -> 2
// new Owner id -> 2

// [Owner = 0, Owner = 1, Owner = 2, null = 3, null = 4]
