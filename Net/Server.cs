using LiteNetLib;
using LiteNetLib.Utils;
using Microsoft.Xna.Framework;
using System;
using TanksRebirth.GameContent;
using TanksRebirth.GameContent.Systems;
using TanksRebirth.GameContent.Tanks;
using TanksRebirth.Internals.Common.Framework.Audio;

namespace TanksRebirth.Net;

#pragma warning disable CA2211
// moderately confused as to why this class isn't static... ¯\_(ツ)_/¯
public class Server {
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
    /// <summary>
    /// A shared random stream. It only matches between clients while every client draws from it the same number of times
    /// in the same order, so prefer <see cref="RandomFor"/> for anything that has to come out the same everywhere.
    /// </summary>
    public static Random ServerRandom { get; private set; } = new();

    /// <summary>
    /// The seed of the current game. The host picks a new one whenever a campaign starts, so every client has the same one. <see cref="RandomFor"/> is built from it.
    /// </summary>
    public static int SessionSeed { get; private set; } = Environment.TickCount;

    /// <summary>Sets <see cref="SessionSeed"/> (and restarts <see cref="ServerRandom"/> from it).</summary>
    public static void SetSessionSeed(int seed) {
        SessionSeed = seed;
        RandSeed = seed;
    }

    /// <summary>A new random <see cref="SessionSeed"/>, for the host when it starts a game.</summary>
    public static int NewSessionSeed() => Guid.NewGuid().GetHashCode();

    /// <summary>
    /// A random generator for one specific roll, the same on every client: it's seeded from <see cref="SessionSeed"/> and
    /// <paramref name="key"/> only, so it doesn't matter what else was drawn before, or in what order. Give every roll its
    /// own key (what it's for, the mission, which tank...): e.g. <c>RandomFor(RandomKey.EnemyTier, missionId, tankIndex)</c>.
    /// </summary>
    /// <remarks>Outside of multiplayer this is just <see cref="Client.ClientRandom"/>, so single player stays unpredictable.</remarks>
    public static Random RandomFor(params int[] key) {
        if (!Client.IsConnected())
            return Client.ClientRandom;
        return new Random(Mix(SessionSeed, key));
    }

    /// <summary>What a <see cref="RandomFor"/> roll is for (the first part of its key).</summary>
    public static class RandomKey {
        public const int EnemyTier = 1;
        public const int CompanionTier = 2;
        public const int PlayerTier = 3;
    }

    // splitmix64, which is consistent PRNG, unlike hashcode.combine
    // thank you flipy for suggestion. which psycho game up with this?
    static int Mix(int seed, int[] key) {
        ulong x = (uint)seed;
        foreach (var k in key) {
            x += 0x9E3779B97F4A7C15UL + (uint)k;
            x = (x ^ (x >> 30)) * 0xBF58476D1CE4E5B9UL;
            x = (x ^ (x >> 27)) * 0x94D049BB133111EBUL;
            x ^= x >> 31;
        }
        return (int)(x ^ (x >> 32));
    }

    public static Client[]? ConnectedClients;

    public static void CreateServer(byte maxClients = 4) {
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
            NetPlay.PeerMap.Add(peer.Id, CurrentClientCount);
            NetPlay.ReversePeerMap.Add(CurrentClientCount, peer.Id);
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

    // TODO: make server random seed synced every frame

    /// <summary>
    /// Syncs the seed of <see cref="ServerRandom"/>
    /// </summary>
    public static void SyncSeeds() {
        if (!Client.IsConnected()) return;
        if (!Client.IsHost()) return;

        NetDataWriter message = new();

        var seed = NewSessionSeed();
        message.Put(PacketID.SyncSeeds);

        message.Put(seed);

        SetSessionSeed(seed);

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