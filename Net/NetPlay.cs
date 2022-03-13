using LiteNetLib;
using LiteNetLib.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using WiiPlayTanksRemake.Net;

namespace WiiPlayTanksRemake.Net
{
    public class NetPlay
    {
        public static IPEndPoint Ip;
        public static int Port;
        public static Client CurrentClient;
        public static Server CurrentServer;

        public static void MapClientNetworking()
        {
            Client.clientNetListener.NetworkReceiveEvent += OnPacketRecieve_Client;
            Client.clientNetListener.PeerConnectedEvent += OnClientJoin;
        }

        private static void OnClientJoin(NetPeer peer)
        {
            Console.WriteLine($"Connected to remote server.");
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
            }
        }
        private static void OnPacketRecieve_Server(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            var packet = reader.GetPacket();

            NetDataWriter message = new();

            Console.WriteLine($"Packet recieved: {packet}");

            switch (packet)
            {
                case PacketType.ClientInfo:
                    break;
            }
            peer.Send(message, DeliveryMethod.ReliableOrdered);
        }
    }

    // [Tank = 0, Tank = 1, null = 2, null = 3, null = 4]

    // find: null
    // null -> 2
    // new Tank id -> 2

    // [Tank = 0, Tank = 1, Tank = 2, null = 3, null = 4]

    public enum PacketType : byte
    {
        ClientInfo,
        PlayerPosition,
        PlayerTurretAngle,
        PlayerChassisAngle,
        PlayerVelocity,
        AiTankPositions,
        AiTankAngles,
		AiTankVelocities,
        AiTankTurretAngles,
        MinePlacement,
        BulletFire
    }
}