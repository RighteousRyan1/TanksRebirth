using System;
using LiteNetLib;
using LiteNetLib.Utils;
using Microsoft.Xna.Framework;
using TanksRebirth.GameContent;
using TanksRebirth.GameContent.Systems;
using TanksRebirth.GameContent.UI;

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
        public static void RequestStartGame(int checkpoint, bool shouldProgressMissions)
        {
            NetDataWriter message = new();
            message.Put(PacketType.StartGame);
            message.Put(checkpoint);
            message.Put(shouldProgressMissions);

            client.Send(message, DeliveryMethod.ReliableOrdered);
        }
        public static void RequestPlayerTankSpawn(PlayerTank tank)
        {
            NetDataWriter message = new();
            message.Put(PacketType.PlayerSpawn);

            message.Put(tank.PlayerType);
            message.Put(tank.Team);
            message.Put(tank.Body.Position.X);
            message.Put(tank.Body.Position.Y);
            message.Put(tank.TankRotation);
            message.Put(tank.TurretRotation);

            //Server.serverNetManager.SendToAll(message, DeliveryMethod.ReliableOrdered);

            client.Send(message, DeliveryMethod.ReliableOrdered);
        }
        public static void SyncTank(Tank tank)
        {
            if (MainMenu.Active || !IsConnected())
                return;
            NetDataWriter message = new();
            message.Put(PacketType.TankData);

            message.Put(tank.WorldId);
            message.Put(tank.Body.Position.X);
            message.Put(tank.Body.Position.Y);
            message.Put(tank.TankRotation);
            message.Put(tank.TurretRotation);
            message.Put(tank.Velocity.X);
            message.Put(tank.Velocity.Y);

            //Server.serverNetManager.SendToAll(message, DeliveryMethod.ReliableOrdered);
            client.Send(message, DeliveryMethod.Unreliable);
        }
        /// <summary>Be sure to sync by accessing the index of the tank from the AllTanks array. (<see cref="GameHandler.AllTanks"/>)
        /// <para></para>
        /// <c>AllTanks[<paramref name="tankId"/>].Shoot()</c>
        /// </summary>
        /// <param name="tankId">The identified of the <see cref="Tank"/> that fired.</param>
        public static void SyncBulletFire(int type, Vector3 position, Vector3 velocity, int owner, uint ricochets, int id)
        {
            if (MainMenu.Active || !IsConnected())
                return;

            NetDataWriter message = new();
            message.Put(PacketType.BulletFire);

            message.Put(type);
            message.Put(position);
            message.Put(velocity);
            message.Put(owner);
            message.Put(ricochets);
            message.Put(id);

            // FIXME: could probably use more syncing... who cares?


            client.Send(message, DeliveryMethod.Sequenced);
        }
        public static void SyncMinePlace(Vector2 position, float detonateTime, int id)
        {
            if (MainMenu.Active || !IsConnected())
                return;

            NetDataWriter message = new();
            message.Put(PacketType.MinePlacement);

            message.Put(position);
            message.Put(detonateTime);
            message.Put(id);

            client.Send(message, DeliveryMethod.Sequenced);
        }
        public static void SendMessage(string text, Color color, string sender)
        {
            if (!IsConnected())
                return;
            NetDataWriter message = new();
            message.Put(PacketType.ChatMessage);

            message.Put(text);
            message.Put(color);
            message.Put(sender);

            client.Send(message, DeliveryMethod.ReliableOrdered); // send to the server for parsing...
        }
        public static bool IsConnected()
        {
            if (client is not null)
                return client.ConnectionState == ConnectionState.Connected;
            return false;
        }

        public static void SendServerNameChange(string newName)
        {
            if (!IsConnected())
                return;
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
        public static void SendCampaignBytes(Campaign campaign)
        {
            if (!IsConnected())
                return;
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
                    message.Put(block.Type);
                    message.Put(block.Stack);
                    message.Put(block.TpLink);
                }
                foreach (var tank in mission.Tanks)
                {
                    message.Put(tank.Position);
                    message.Put(tank.Rotation);
                    message.Put(tank.IsPlayer);
                    message.Put(tank.Team);

                    if (tank.IsPlayer)
                        message.Put(tank.PlayerType);
                    else
                        message.Put(tank.AiTier);
                }
            }

            client.Send(message, DeliveryMethod.Sequenced);
        }

        public static void SendCampaign(string name)
        {
            if (!IsConnected())
                return;
            NetDataWriter message = new();
            message.Put(PacketType.SendCampaignByName);

            message.Put(name);

            client.Send(message, DeliveryMethod.ReliableOrdered);
        }
    }
}
