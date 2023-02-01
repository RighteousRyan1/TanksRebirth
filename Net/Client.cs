using System;
using System.IO;
using System.Xml.Linq;
using LiteNetLib;
using LiteNetLib.Utils;
using Microsoft.Xna.Framework;
using TanksRebirth.GameContent;
using TanksRebirth.GameContent.Systems;
using TanksRebirth.GameContent.UI;

namespace TanksRebirth.Net
{
    public class Client {

        public static NetManager clientNetManager;
        public static EventBasedNetListener clientNetListener;
        public static NetPeer client;
        public static int PingTime = 60;
        public static bool lobbyDataReceived;

        public int Id;
        public string Name;

        public static void CreateClient(string username) {
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

        public static void AttemptConnectionTo(string address, int port, string password) {
            GameHandler.ClientLog.Write($"Attempting connection to server...", Internals.LogType.Debug);
            clientNetManager.Start();
            client = clientNetManager.Connect(address, port, password);
        }

        public static void SendCommandUsage(string command) {
            if (!IsConnected())
                return;

            NetDataWriter message = new();
            message.Put(PacketID.SendCommandUsage);
            message.Put(command);

            client.Send(message, DeliveryMethod.ReliableOrdered);
        }

        public static void SendQuit() {
            if (MainMenu.Active || !IsConnected())
                return;

            NetDataWriter message = new();
            message.Put(PacketID.QuitLevel);

            client.Send(message, DeliveryMethod.ReliableOrdered);
        }
        public static void SendLives() {
            if (MainMenu.Active || !IsConnected())
                return;

            NetDataWriter message = new();
            message.Put(PacketID.SyncLives);

            message.Put(NetPlay.CurrentClient.Id);
            message.Put(PlayerTank.GetMyLives());

            client.Send(message, DeliveryMethod.Unreliable);
        }

        public static void SyncCleanup() {
            if (MainMenu.Active || !IsConnected())
                return;

            NetDataWriter message = new();
            message.Put(PacketID.Cleanup);

            client.Send(message, DeliveryMethod.Unreliable);
        }

        public static void SendClientInfo() {
            NetDataWriter message = new();
            message.Put(PacketID.ClientInfo);
            message.Put(NetPlay.CurrentClient.Name);

            client.Send(message, DeliveryMethod.ReliableOrdered);
        }
        public static void RequestLobbyInfo() {
            NetDataWriter message = new();
            message.Put(PacketID.LobbyInfo);

            client.Send(message, DeliveryMethod.ReliableOrdered);
        }
        public static void RequestStartGame(int checkpoint, bool shouldProgressMissions) {
            NetDataWriter message = new();
            message.Put(PacketID.StartGame);
            message.Put(checkpoint);
            message.Put(shouldProgressMissions);

            client.Send(message, DeliveryMethod.ReliableOrdered);
        }
        public static void RequestPlayerTankSpawn(PlayerTank tank) {
            NetDataWriter message = new();
            message.Put(PacketID.PlayerSpawn);

            message.Put(tank.PlayerType);
            message.Put(tank.Team);
            message.Put(tank.Body.Position.X);
            message.Put(tank.Body.Position.Y);
            message.Put(tank.TankRotation);
            message.Put(tank.TurretRotation);

            //Server.serverNetManager.SendToAll(message, DeliveryMethod.ReliableOrdered);

            client.Send(message, DeliveryMethod.ReliableOrdered);
        }
        public static void SyncPlayerTank(PlayerTank tank) {
            if (MainMenu.Active || !IsConnected())
                return;

            NetDataWriter message = new();
            message.Put(PacketID.SyncPlayer);

            message.Put(tank.PlayerId);
            message.Put(tank.Body.Position.X);
            message.Put(tank.Body.Position.Y);
            message.Put(tank.TankRotation);
            message.Put(tank.TurretRotation);
            message.Put(tank.Velocity.X);
            message.Put(tank.Velocity.Y);

            client.Send(message, DeliveryMethod.Unreliable);
        }
        public static void SyncAITank(AITank tank) {
            if (MainMenu.Active || !IsConnected())
                return;

            NetDataWriter message = new();
            message.Put(PacketID.SyncAiTank);

            message.Put(tank.AITankId);
            message.Put(tank.Body.Position.X);
            message.Put(tank.Body.Position.Y);
            message.Put(tank.TankRotation);
            message.Put(tank.TurretRotation);
            message.Put(tank.Velocity.X);
            message.Put(tank.Velocity.Y);

            client.Send(message, DeliveryMethod.Unreliable);
        }
        /// <summary>Be sure to sync by accessing the index of the tank from the AllTanks array. (<see cref="GameHandler.AllTanks"/>)
        /// <para></para>
        /// <c>AllTanks[<paramref name="tankId"/>].Shoot()</c>
        /// </summary>
        /// <param name="tankId">The identified of the <see cref="Tank"/> that fired.</param>
        public static void SyncShellFire(Shell shell) {
            if (MainMenu.Active || !IsConnected())
                return;

            NetDataWriter message = new();
            message.Put(PacketID.ShellFire);

            message.Put(shell.Type);
            message.Put(shell.Position);
            message.Put(shell.Velocity);
            message.Put(shell.RicochetsRemaining);
            message.Put(shell.Owner.WorldId);
            message.Put(shell.Id);

            // ChatSystem.SendMessage($"Pos: {shell.Position} | Vel: {shell.Velocity}", Color.White);

            // FIXME: could probably use more syncing... who cares?


            client.Send(message, DeliveryMethod.Sequenced);
        }
        public static void SyncDamage(int id/*, ITankHurtContext context*/) {
            if (!IsConnected() || MainMenu.Active)
                return;
            NetDataWriter message = new();
            message.Put(PacketID.TankDamage);

            message.Put(id);

            //message.Put(context.TankId);
            //message.Put(context.IsPlayer);

            client.Send(message, DeliveryMethod.ReliableOrdered);
        }
        public static void SyncShellDestroy(Shell shell, Shell.DestructionContext cxt) {
            if (!IsConnected() || MainMenu.Active)
                return;
            NetDataWriter message = new();
            message.Put(PacketID.ShellDestroy);
            // TODO: bool hasOwner to decide whether or not to subtract from an owner's shell count

            message.Put(shell.Owner.WorldId);
            // send the index of the shell in the owner's OwnedShell array for destruction on other clients
            message.Put(Array.IndexOf(shell.Owner.OwnedShells, shell));
            message.Put((byte)cxt);

            client.Send(message, DeliveryMethod.ReliableOrdered);
        }
        // maybe have owner stuff go here?
        public static void SyncMineDetonate(Mine mine) {
            if (!IsConnected() || MainMenu.Active)
                return;
            NetDataWriter message = new();
            message.Put(PacketID.MineDetonate);

            message.Put(mine.Id);

            client.Send(message, DeliveryMethod.ReliableOrdered);
        }

        public static void SyncMinePlace(Vector2 position, float detonateTime, int id) {
            if (MainMenu.Active || !IsConnected())
                return;

            NetDataWriter message = new();
            message.Put(PacketID.MinePlacement);

            message.Put(position);
            message.Put(detonateTime);
            message.Put(id);

            client.Send(message, DeliveryMethod.Sequenced);
        }
        public static void SendMessage(string text, Color color, string sender) {
            if (!IsConnected())
                return;
            NetDataWriter message = new();
            message.Put(PacketID.ChatMessage);

            message.Put(text);
            message.Put(color);
            message.Put(sender);

            client.Send(message, DeliveryMethod.ReliableOrdered);
        }
        public static bool IsConnected() {
            if (client is not null)
                return client.ConnectionState == ConnectionState.Connected;
            return false;
        }

        public static void SendServerNameChange(string newName) {
            if (!IsConnected())
                return;
            NetDataWriter message = new();
            message.Put(PacketID.ServerNameSync);
            message.Put(newName);

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
        public static void SendCampaignBytes(Campaign campaign) {
            if (!IsConnected())
                return;
            NetDataWriter message = new();
            message.Put(PacketID.SendCampaign);

            message.Put(campaign.MetaData.Name);
            message.Put(campaign.MetaData.StartingLives);
            message.Put(campaign.MetaData.BackgroundColor);
            message.Put(campaign.MetaData.MissionStripColor);

            message.Put(campaign.CachedMissions.Length);

            foreach (var mission in campaign.CachedMissions) {
                message.Put(mission.Blocks.Length);
                message.Put(mission.Tanks.Length);

                message.Put(mission.Name);
                message.Put(mission.Note);

                foreach (var block in mission.Blocks) {
                    message.Put(block.Position);
                    message.Put(block.Type);
                    message.Put(block.Stack);
                    message.Put(block.TpLink);
                }
                foreach (var tank in mission.Tanks) {
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

        public static void SendCampaign(string name) {
            if (!IsConnected())
                return;
            NetDataWriter message = new();
            message.Put(PacketID.SendCampaignByName);

            message.Put(name);

            client.Send(message, DeliveryMethod.ReliableOrdered);
        }
        public static void SendDisconnect(int peerId, string name, string reason) {
            if (!IsConnected())
                return;
            NetDataWriter message = new();
            message.Put(PacketID.Disconnect);

            message.Put(peerId);
            message.Put(name);
            message.Put(reason);

            client.Send(message, DeliveryMethod.ReliableOrdered);
        }

        public static void SendCampaignSuccess(string campaignName, int clientId, bool success) {
            if (!IsConnected())
                return;
            NetDataWriter message = new();
            message.Put(PacketID.CampaignSendSuccess);

            message.Put(campaignName);
            message.Put(clientId);
            message.Put(success);

            client.Send(message, DeliveryMethod.ReliableOrdered);
        }

        public static void SendFile(string filePath) {
            var fBytes = File.ReadAllBytes(filePath);

            const int MAX_SEND_LEN = 1024; // i think?

            int numPacketsToSend = sizeof(byte) * fBytes.Length / MAX_SEND_LEN;

            // NetDataWriter message = new();

            // message.Put(numPacketsToSend);

            for (int i = 0; i < numPacketsToSend; i++) {
                var byteSplit = fBytes[(numPacketsToSend * i)..(numPacketsToSend * (i + 1))];

                // message.Put(byteSplit);
                SendCampaignBytes(byteSplit);
            }
        }

        public static void SendCampaignBytes(byte[] bytes) {
            NetDataWriter message = new();

            //message.Put(PacketID.SendCampaignBytes);
            message.Put(bytes);

            client.Send(message, DeliveryMethod.ReliableOrdered);
        }
    }
}
