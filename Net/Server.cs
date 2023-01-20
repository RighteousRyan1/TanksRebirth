using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiteNetLib;
using LiteNetLib.Layers;
using LiteNetLib.Utils;
using Microsoft.Xna.Framework;
using TanksRebirth.GameContent;
using TanksRebirth.GameContent.Systems;

namespace TanksRebirth.Net
{
    public class Server
    {
        public static NetManager serverNetManager;

        public static EventBasedNetListener serverNetListener;

        public string Password;
        public string Address;
        public int Port;

        public string Name;

        public static ushort MaxClients;

        public static Client[] ConnectedClients;

        public static int CurrentClientCount;

        public static void CreateServer(ushort maxClients = 4)
        {
            MaxClients = maxClients;

            serverNetListener = new();
            serverNetManager = new(serverNetListener);

            ConnectedClients = new Client[maxClients];

            GameHandler.ClientLog.Write($"Server created.", Internals.LogType.Debug);

            NetPlay.MapServerNetworking();
        }

        public static void StartServer(string name, int port, string address, string password)
        {
            var server = new Server
            {
                Port = port,
                Address = address,
                Password = password,
                Name = name
            };

            NetPlay.CurrentServer = server;

            GameHandler.ClientLog.Write($"Server started. (Name = \"{name}\" | Port = \"{port}\" | Address = \"{address}\" | Password = \"{password}\")", Internals.LogType.Debug);

            serverNetManager.Start(port);

            // serverNetManager.NatPunchEnabled = true;

            serverNetListener.ConnectionRequestEvent += request =>
            {
                if (serverNetManager.ConnectedPeersCount < MaxClients)
                {
                    request.AcceptIfKey(password);
                }
                else
                {
                    ChatSystem.SendMessage("User rejected: Incorrect password.", Color.Red);
                    request.Reject();
                }
                serverNetListener.PeerConnectedEvent += peer =>
                {
                    NetDataWriter writer = new();

                    writer.Put("Client successfully connected.");

                    peer.Send(writer, DeliveryMethod.ReliableOrdered);
                };
            };
        }
    }
}
