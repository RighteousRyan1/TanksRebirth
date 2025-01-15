using LiteNetLib;
using LiteNetLib.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using TanksRebirth.GameContent;
using TanksRebirth.GameContent.Systems;

namespace TanksRebirth.Net;

// moderately confused as to why this class isn't static... ¯\_(ツ)_/¯
public class Server
{
    public delegate void ServerStartDelegate(Server server);
    /// <summary>Fired when a server is created. Here you can hook into <see cref="NetListener"/>'s "NetworkReceiveEvent" to handle your packets.</summary>
    public static event ServerStartDelegate OnServerStart;

    public static NetManager NetManager;
    public static EventBasedNetListener NetListener;

    public string Password;
    public string Address;
    public string Name;

    public static int CurrentClientCount;
    public int Port;

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
    /// <summary>Can be used to do randomization on the client in single-player, but once the server is started, should only be used for random events
    /// that should happen on both clients at the same time.</summary>
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
        //ServerRandom = new

        NetPlay.CurrentServer = server;
        OnServerStart?.Invoke(server);

        TankGame.ClientLog.Write($"Server started. (Name = \"{name}\" | Port = \"{port}\" | Address = \"{address}\" | Password = \"{password}\")", Internals.LogType.Debug);

        NetManager.Start(port);

        // serverNetManager.NatPunchEnabled = true;

        NetListener.ConnectionRequestEvent += request =>
        {
            if (NetManager.ConnectedPeersCount < MaxClients)
            {
                request.AcceptIfKey(password);
            }
            else
            {
                ChatSystem.SendMessage("User rejected: Incorrect password.", Color.Red);
                request.Reject();
            }
            NetListener.PeerConnectedEvent += peer =>
            {
                //NetDataWriter writer = new();

                //writer.Put("Client successfully connected.");

                //peer.Send(writer, DeliveryMethod.ReliableOrdered);
            };
        };
    }

    public static void SyncSeeds() {
        if (!Client.IsConnected()) return;
        if (!Client.IsHost()) return;

        NetDataWriter message = new();
        // 'Millisecond' will be different on other clients if not sent (latency)
        var millis = DateTime.Now.Millisecond;
        message.Put(PacketID.SyncSeeds);
        message.Put(millis);

        RandSeed = millis;

        ChatSystem.SendMessage("Seed synced: " + millis, Color.Lime);

        // since this is sending from the server itself, no point in sending to itself.
        NetManager.SendToAll(message, DeliveryMethod.ReliableOrdered, Client.NetClient);
    }
}
