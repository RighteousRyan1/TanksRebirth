using LiteNetLib;
using LiteNetLib.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Mono.Unix.Native;
using System;
using TanksRebirth.GameContent;
using TanksRebirth.GameContent.Systems;
using TanksRebirth.Internals.Common.Framework.Audio;

namespace TanksRebirth.Net;

#pragma warning disable CA2211
// moderately confused as to why this class isn't static... ¯\_(ツ)_/¯
public class Server
{
    public delegate void ServerStartDelegate(Server server);
    /// <summary>Fired when a server is created. Here you can hook into <see cref="NetListener"/>'s "NetworkReceiveEvent" to handle your packets.</summary>
    public static event ServerStartDelegate? OnServerStart;

    public static NetManager NetManager;
    public static EventBasedNetListener NetListener;

    public string? Password { get; set; }
    public string? Address { get; set; }
    public string? Name { get; set; }

    public static int CurrentClientCount { get; internal set; }
    public int Port { get; set; }

    /// <summary>If some nutcase wants to mod more than 4 player multiplayer into the game, they definitely won't need more than 255 players.</summary>
    public static byte MaxClients = 4;

    private static int _randSeed;
    public static int RandSeed {
        get => _randSeed;
        set {
            _randSeed = value;
            ServerRandom = new(value);
        }
    }
    /// <summary>Should only be used for events in a multiplayer context in order for events to happen the same way on all clients.</summary>
    public static Random ServerRandom { get; private set; } = new();

    public static Client[] ConnectedClients;

    public static void CreateServer(byte maxClients = 4)
    {
        MaxClients = maxClients;

        NetListener = new();
        NetManager = new(NetListener);

        ConnectedClients = new Client[maxClients];

        TankGame.ClientLog.Write($"Server created.", Internals.LogType.Debug);

        NetPlay.MapServerNetworking();
    }

    public static void StartServer(string name, int port, string address, string password) {
        var server = new Server {
            Port = port,
            Address = address,
            Password = password,
            Name = name
        };

        NetPlay.CurrentServer = server;
        OnServerStart?.Invoke(server);

        TankGame.ClientLog.Write($"Server started. (Name = \"{name}\" | Port = \"{port}\" | Address = \"{address}\" | Password = \"{password}\")", Internals.LogType.Debug);

        NetManager.Start(port);
        NetManager.DisconnectTimeout = 10000;
        NetManager.UpdateTime = 15;

        // serverNetManager.NatPunchEnabled = true;
        NetListener.PeerDisconnectedEvent += NetListener_PeerDisconnectedEvent;
        NetListener.ConnectionRequestEvent += NetListener_ConnectionRequestEvent;
    }

    private static void NetListener_ConnectionRequestEvent(ConnectionRequest request) {
        if (NetManager.ConnectedPeersCount < MaxClients) {
            var peer = request.AcceptIfKey(NetPlay.CurrentServer!.Password);

            // fix the peer map on the server
            NetPlay.PeerMap[peer.Id] = CurrentClientCount;
            NetPlay.ReversePeerMap[CurrentClientCount] = peer.Id;
            ChatSystem.SendMessage($"Connected peer {peer.Id} -> {CurrentClientCount}");
        }
        else {
            ChatSystem.SendMessage("User rejected: Incorrect password.", Color.Red);
            request.Reject();
        }
    }

    private static void NetListener_PeerDisconnectedEvent(NetPeer peer, DisconnectInfo disconnectInfo) {
        var peerIdReal = NetPlay.PeerMap[peer.Id];

        ChatSystem.SendMessage($"{ConnectedClients[peerIdReal].Name} has disconnected. ({disconnectInfo.Reason})", Color.Red);
        CurrentClientCount--;

        GameHandler.AllPlayerTanks[peer.Id]?.Destroy(new TankHurtContextOther(), false);

        SoundPlayer.PlaySoundInstance("Assets/sounds/menu/client_leave.ogg", SoundContext.Effect, 0.75f);
    }

    public static void SyncSeeds() {
        if (!Client.IsConnected()) return;
        if (!Client.IsHost()) return;

        NetDataWriter message = new();

        var seed = Guid.NewGuid().GetHashCode();
        message.Put(PacketID.SyncSeeds);

        message.Put(seed);

        RandSeed = seed;

        // since this is sending from the server itself, no point in sending to itself.
        NetManager.SendToAll(message, DeliveryMethod.ReliableOrdered, Client.NetClient);
    }

    public static void SendHostDisconnect() {
        if (!Client.IsConnected()) return;
        if (!Client.IsHost()) return;

        NetDataWriter message = new();

        message.Put(PacketID.HostDisconnect);

        // since this is sending from the server itself, no point in sending to itself.
        NetManager.SendToAll(message, DeliveryMethod.ReliableOrdered, Client.NetClient);
    }
}
