using LiteNetLib;
using LiteNetLib.Utils;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using TanksRebirth.Enums;
using TanksRebirth.GameContent;
using TanksRebirth.GameContent.Systems;
using TanksRebirth.GameContent.UI;
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
            ChatSystem.SendMessage("Connected to server.", Color.Lime);

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
                    byte returnedID = reader.GetByte();

                    CurrentClient.Id = returnedID;
                    Client.RequestLobbyInfo();
                    break;

                case PacketType.LobbyInfo:
                    ServerName = "ServerName";
                    int serverMaxClients = reader.GetByte();

                    string servName = reader.GetString();
                    ServerName = servName;

                    Server.ConnectedClients = new Client[serverMaxClients];
                    for (int i = 0; i < serverMaxClients; i++)
                    {
                        bool isClientAvailable = reader.GetBool();

                        if (isClientAvailable)
                        {
                            byte clientId = reader.GetByte();
                            string clientName = reader.GetString();

                            Server.ConnectedClients[i] = new Client()
                            {
                                Id = clientId,
                                Name = clientName,
                            };
                        }
                    }
                    Client.lobbyDataReceived = true;

                    break;
                case PacketType.StartGame:
                    MainMenu.PlayButton_SinglePlayer.OnLeftClick?.Invoke(null); // launches the game

                    break;
                case PacketType.PlayerSpawn:

                    PlayerType type = (PlayerType)reader.GetByte();
                    TankTeam team = (TankTeam)reader.GetByte();

                    float x = reader.GetFloat();
                    float y = reader.GetFloat();
                    float tnkRot = reader.GetFloat();
                    float turRot = reader.GetFloat();

                    var t = new PlayerTank(type);
                    t.Body.Position = new(x, y);
                    t.Dead = false;
                    t.TankRotation = tnkRot;
                    t.TurretRotation = turRot;
                    t.Team = team;

                    break;
                case PacketType.PlayerData:

                    int id = reader.GetInt();
                    float x2 = reader.GetFloat();
                    float y2 = reader.GetFloat();
                    float tankRotation = reader.GetFloat();
                    float turretRotation = reader.GetFloat();
                    float vX = reader.GetFloat();
                    float vY = reader.GetFloat();

                    GameHandler.AllPlayerTanks[id].Body.Position = new(x2, y2);
                    GameHandler.AllPlayerTanks[id].TankRotation = tankRotation;
                    GameHandler.AllPlayerTanks[id].TurretRotation = turretRotation;
                    GameHandler.AllPlayerTanks[id].Velocity = new(vX, vY);
                    break;
                case PacketType.ChatMessage:
                    string msg = reader.GetString();
                    Color color = reader.GetColor();
                    string sender = reader.GetString();

                    ChatSystem.SendMessage(msg, color, sender);
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

            NetDataWriter message;

            //message.Put(packet);

            switch (packet)
            {
                case PacketType.ClientInfo:
                    message = new();
                    message.Put(packet);
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
                    message = new();        //I have yet to test with this how it previously was.
                    message.Put(packet);
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
                            // fix desync of client ids
                        }
                    }

                    Server.serverNetManager.SendToAll(message, DeliveryMethod.ReliableOrdered);     //This sends to everyone but the one who sent
                    peer.Send(message, deliveryMethod);     //This sends it back to the guy who sent

                    break;
                case PacketType.StartGame:
                case PacketType.PlayerSpawn:
                    message = new();
                    message.Put(packet);
                    Server.serverNetManager.SendToAll(message, DeliveryMethod.ReliableOrdered, peer);
                    // We don't need to do anything since it's handled in the Client's method.
                    break;
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
        StartGame,
        LeaveGame,

        // Ingame packets: players
        PlayerData,

        // Ingame packets: ai
        AiTankData,

        // Ingame packets: tank mechanics
        MinePlacement,
        BulletFire,

        // Human communication

        ChatMessage,

        // Debugging packets:

        PlayerSpawn,
        AiTankSpawn,
        CrateSpawn,

        // more server syncing

        ServerNameSync
    }
}