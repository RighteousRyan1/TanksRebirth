using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiteNetLib;
using LiteNetLib.Layers;
using LiteNetLib.Utils;

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

        public static void CreateClient()
        {
            clientNetListener = new();
            clientNetManager = new NetManager(clientNetListener);
            clientNetManager.NatPunchEnabled = true;
            clientNetManager.UpdateTime = 15;
            clientNetManager.Start();
            NetPlay.MapClientNetworking();

            Console.ForegroundColor = ConsoleColor.Green;

            Client c = new();
            // set client name to username
            c.Name = "John";
            NetPlay.CurrentClient = c;
        }

        public static void AttemptConnectionTo(string address, int port, string password)
        {
            clientNetManager.Start();
            client = clientNetManager.Connect(address, port, password);
        }

        public static void SendClientInfo()
        {
            NetDataWriter message = new();
            message.Put(PacketType.ClientInfo);
            message.Put(NetPlay.CurrentClient.Id);
            message.Put(NetPlay.CurrentClient.Name);
            client.Send(message, DeliveryMethod.ReliableOrdered);
        }
    }
}
