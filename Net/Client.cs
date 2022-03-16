using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiteNetLib;
using LiteNetLib.Layers;
using LiteNetLib.Utils;
using WiiPlayTanksRemake.GameContent;

namespace WiiPlayTanksRemake.Net
{
    public class Client
    {
        public static NetManager clientNetManager;
        public static EventBasedNetListener clientNetListener;
        public static NetPeer client;
        public static int PingTime = 60;

        public int Id;
        public string Name;

        public static void CreateClient(string username)
        {
            clientNetListener = new();
            clientNetManager = new(clientNetListener);
            clientNetManager.NatPunchEnabled = true;
            clientNetManager.UpdateTime = 15;
            clientNetManager.Start();
            NetPlay.MapClientNetworking();

            Console.ForegroundColor = ConsoleColor.Green;

            Client c = new();
            // set client name to username
            c.Name = username;

            // var tank = new PlayerTank(Enums.PlayerType.Blue);

            GameHandler.ClientLog.Write($"Created a new client with name '{username}'.", Internals.LogType.Debug);
            NetPlay.CurrentClient = c;
        }

        public static void AttemptConnectionTo(string address, int port, string password)
        {
            GameHandler.ClientLog.Write($"Attempting connection to server...", Internals.LogType.Debug);
            clientNetManager.Start();
            client = clientNetManager.Connect(address, port, password);
        }

        public static void SendClientInfo()
        {
            NetDataWriter message = new();
            message.Put(PacketType.ClientInfo);
            message.Put(NetPlay.CurrentClient.Name);

            client.Send(message, DeliveryMethod.ReliableOrdered);
        }
        public static void RequestLobbyInfo()
        {
            NetDataWriter message = new();
            message.Put(PacketType.LobbyInfo);

            client.Send(message, DeliveryMethod.ReliableOrdered);
        }
        public static void RequestStartGame()
        {
            NetDataWriter message = new();
            message.Put(PacketType.StartGame);

            Server.serverNetManager.SendToAll(message, DeliveryMethod.ReliableOrdered);
        }

        public static bool IsClientConnected()
        {
            if (client is not null)
                return client.ConnectionState == ConnectionState.Connected;
            return false;
        }
    }
}
