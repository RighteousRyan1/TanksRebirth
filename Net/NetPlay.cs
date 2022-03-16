using LiteNetLib;
using LiteNetLib.Utils;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using WiiPlayTanksRemake.GameContent;
using WiiPlayTanksRemake.GameContent.Systems;
using WiiPlayTanksRemake.GameContent.UI;
using WiiPlayTanksRemake.Net;

namespace WiiPlayTanksRemake.Net
{
    public class NetPlay
    {
        public static IPEndPoint Ip;
        public static int Port;
        public static Client CurrentClient;
        public static Server CurrentServer;

        public static bool DoPacketLogging = false;

        public static void MapClientNetworking()
        {
            Client.clientNetListener.NetworkReceiveEvent += OnPacketRecieve_Client;
            Client.clientNetListener.PeerConnectedEvent += OnClientJoin;
        }

        private static void OnClientJoin(NetPeer peer)
        {
            GameHandler.ClientLog.Write($"Connected to remote server.", Internals.LogType.Debug);
            ChatSystem.SendMessage("Connected to server.", Color.Lime);
            // GameHandler.ClientLog.Write($"Connected to remote server.", Internals.LogType.Debug);

            Client.SendClientInfo();
        }

        public static void MapServerNetworking()
        {
            Server.serverNetListener.NetworkReceiveEvent += OnPacketRecieve_Server;
        }

        private static void OnPacketRecieve_Client(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            var packet = reader.GetPacket();

            switch (packet)
            {
                case PacketType.ClientInfo:
                    Client.RequestLobbyInfo();
                    break;
                case PacketType.LobbyInfo:
                    int len = reader.GetInt();
                    Server.ConnectedClients = new Client[len];
                    for (int i = 0; i < len; i++)
                    {
                        int clientId = reader.GetInt();
                        string clientName = reader.GetString();

                        Server.ConnectedClients[i] = new Client()
                        {
                            Id = clientId,
                            Name = clientName,
                        };
                    }
                    string servName = reader.GetString();

                    break;
                case PacketType.StartGame:
                    MainMenu.PlayButton_SinglePlayer.OnLeftClick?.Invoke(null); // launches the game

                    new PlayerTank(Enums.PlayerType.Blue);

                    break;
            }

            //peer.Send(message, DeliveryMethod.ReliableOrdered);

            GameHandler.ClientLog.Write($"Packet Recieved: {packet} from server {peer.Id}.", Internals.LogType.Debug);
            //GameHandler.ClientLog.Write(string.Join(",", reader.RawData), Internals.LogType.Debug);
            reader.Recycle();
        }
        private static void OnPacketRecieve_Server(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            var packet = reader.GetPacket();

            NetDataWriter message = new();

            message.Put(packet);

            switch (packet)
            {
                case PacketType.ClientInfo:
                    string name = reader.GetString();

                    Server.ConnectedClients[Server.CurrentClientCount] = new Client()
                    {
                        Id = Server.CurrentClientCount,
                        Name = name
                    };

                    Server.CurrentClientCount++;

                    Server.serverNetManager.SendToAll(message, DeliveryMethod.ReliableOrdered);
                    break;
                case PacketType.LobbyInfo:
                    message.Put(Server.ConnectedClients.Count(x => x is not null));

                    for (int i = 0; i < Server.ConnectedClients.Count(x => x is not null); i++)
                    {
                        var client = Server.ConnectedClients[i];
                        message.Put(client.Id);
                        message.Put(client.Name);
                    }
                    message.Put(CurrentServer.Name);

                    Server.serverNetManager.SendToAll(message, DeliveryMethod.ReliableOrdered);

                    break;
                case PacketType.StartGame:
                    // We don't need to do anything since it's handled in the Client's method.
                    break;
            }

            // peer.Send(message, DeliveryMethod.ReliableOrdered);

            GameHandler.ClientLog.Write($"Packet Recieved: {packet} from client {peer.Id}. Current clients connected: {Server.CurrentClientCount}", Internals.LogType.Debug);
            reader.Recycle();
        }
        public static bool IsIdEqualTo(int otherId)
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
        StartGame,
        LeaveGame,

        // Ingame packets: players
        PlayerPosition,
        PlayerTurretAngle,
        PlayerAngle,
        PlayerVelocity,

        // Ingame packets: ai
        AiTankPositions,
        AiTankAngles,
        AiTankVelocities,
        AiTankTurretAngles,

        // Ingame packets: tank mechanics
        MinePlacement,
        BulletFire,
    }
}