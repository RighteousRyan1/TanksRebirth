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
    public class Server
    {
        public static NetManager serverNetManager;

        public static EventBasedNetListener serverNetListener;

        public string Password;
        public string Address;
        public int Port;

        public string Name;

        public ushort MaxClients;

        public static void CreateServer()
        {
            serverNetListener = new();
            serverNetManager = new(serverNetListener);

            Console.ForegroundColor = ConsoleColor.Blue;

            NetPlay.MapServerNetworking();
        }

        public static void StartServer(string name, int port, string address, string password, ushort maxClients = 10)
        {
            var server = new Server();

            server.Port = port;
            server.Address = address;
            server.Password = password;
            server.Name = name;
            server.MaxClients = maxClients;

            NetPlay.CurrentServer = server;



            serverNetManager.Start(port);

            serverNetListener.ConnectionRequestEvent += request =>
            {
                if (serverNetManager.ConnectedPeersCount < server.MaxClients)
                {
                    request.AcceptIfKey(password);
                }
                else
                {
                    Console.WriteLine($"Incorrect password.");
                    request.Reject();
                }

                serverNetListener.PeerConnectedEvent += peer =>
                {
                    Console.WriteLine($"{peer.EndPoint} has connected.");
                    NetDataWriter writer = new();
                    NetPacketProcessor processor = new();

                    writer.Put("Client successfully connected.");

                    peer.Send(writer, DeliveryMethod.ReliableOrdered);
                };
            };
        }
    }
}
