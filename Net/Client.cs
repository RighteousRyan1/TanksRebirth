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
    public class Client
    {
        public static NetManager clientNetManager;
        public static EventBasedNetListener clientNetListener;
        public static NetPeer client;
        public static int PingTime = 60;
        public static bool lobbyDataReceived;

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
        public static void RequestStartGame(bool shouldProgressMissions)
        {
            NetDataWriter message = new();
            message.Put(PacketType.StartGame);
            message.Put(shouldProgressMissions);

            Server.serverNetManager.SendToAll(message, DeliveryMethod.ReliableOrdered);
        }
        public static void RequestPlayerTankSpawn(PlayerTank tank)
        {
            NetDataWriter message = new();
            message.Put(PacketType.PlayerSpawn);

            message.Put((byte)tank.PlayerType);
            message.Put((byte)tank.Team);
            message.Put(tank.Body.Position.X);
            message.Put(tank.Body.Position.Y);
            message.Put(tank.TankRotation);
            message.Put(tank.TurretRotation);

            // Server.serverNetManager.SendToAll(message, DeliveryMethod.ReliableOrdered);

            client.Send(message, DeliveryMethod.ReliableOrdered);
        }
        public static void SyncPlayer(PlayerTank tank)
        {
            NetDataWriter message = new();
            message.Put(PacketType.PlayerData);

            message.Put(tank.PlayerId);
            message.Put(tank.Body.Position.X);
            message.Put(tank.Body.Position.Y);
            message.Put(tank.TankRotation);
            message.Put(tank.TurretRotation);
            message.Put(tank.Body.LinearVelocity.X);
            message.Put(tank.Body.LinearVelocity.Y);

            client.Send(message, DeliveryMethod.ReliableOrdered);
        }
        /// <summary>Be sure to sync by accessing the index of the tank from the AllTanks array. (<see cref="GameHandler.AllTanks"/>)
        /// <para></para>
        /// <c>AllTanks[tankId].Shoot()</c>
        /// </summary>
        /// <param name="tankId">The identified of the <see cref="Tank"/> that fired.</param>
        public static void SyncTankFire(int tankId)
        {
            NetDataWriter message = new();
            message.Put(PacketType.PlayerData);

            message.Put(tankId);

            client.Send(message, DeliveryMethod.ReliableOrdered);
        }
        public static void SendMessage(string text, Color color, string sender)
        {
            NetDataWriter message = new();
            message.Put(PacketType.ChatMessage);

            message.Put(text);
            message.Put(color);
            message.Put(sender);

            client.Send(message, DeliveryMethod.ReliableOrdered);
        }
        public static bool IsConnected()
        {
            if (client is not null)
                return client.ConnectionState == ConnectionState.Connected;
            return false;
        }

        public static void SendServerNameChange(string newName)
        {
            NetDataWriter message = new();
            message.Put(PacketType.ServerNameSync);

            Server.serverNetManager.SendToAll(message, DeliveryMethod.ReliableOrdered);
        }
        /// <summary>
        /// Sends a campaign through the connected server.
        /// 
        /// <para></para>
        /// 
        /// Sends in this order: <para></para>
        ///     Sends Campaign properties (name, lives, bg color, strip color), Mission Count
        ///     Then sends each: Block Count, Tank Count, Mission Name, Mission Note <para></para>
        ///         Then sends each: Mission Block (Position, Type, Stack, TpLink) <para></para>
        ///             Finally, sends each: Mission TankTemplate (Position, Rotation, IsPlayer, Team)
        ///             if IsPlayer is true, send PlayerType, otherwise send AiTier
        /// 
        /// </summary>
        /// <param name="campaign">The campaign to send.</param>
        public static void SendCampaign(Campaign campaign)
        {
            NetDataWriter message = new();
            message.Put(PacketType.SendCampaign);

            message.Put(campaign.MetaData.Name);
            message.Put(campaign.MetaData.StartingLives);
            message.Put(campaign.MetaData.BackgroundColor);
            message.Put(campaign.MetaData.MissionStripColor);

            message.Put(campaign.CachedMissions.Length);

            foreach (var mission in campaign.CachedMissions)
            {
                message.Put(mission.Blocks.Length);
                message.Put(mission.Tanks.Length);

                message.Put(mission.Name);
                message.Put(mission.Note);

                foreach (var block in mission.Blocks)
                {
                    message.Put(block.Position);
                    message.Put((byte)block.Type);
                    message.Put(block.Stack);
                    message.Put(block.TpLink);
                }
                foreach (var tank in mission.Tanks)
                {
                    message.Put(tank.Position);
                    message.Put(tank.Rotation);
                    message.Put(tank.IsPlayer);
                    message.Put((byte)tank.Team);

                    if (tank.IsPlayer)
                        message.Put((byte)tank.PlayerType);
                    else
                        message.Put((byte)tank.AiTier);
                }
            }

            Server.serverNetManager.SendToAll(message, DeliveryMethod.ReliableOrdered, client); // TODO: find a way to make this not send back to the sender. done.
        }
    }
}
