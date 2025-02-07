using System;
using System.IO;
using LiteNetLib;
using LiteNetLib.Utils;
using Microsoft.Xna.Framework;
using TanksRebirth.GameContent;
using TanksRebirth.GameContent.Systems;
using TanksRebirth.GameContent.UI.MainMenu;

namespace TanksRebirth.Net;
/// <summary>Contains many ways to send information across the nets. Used for vanilla packet handling.</summary>
public class Client {
    public delegate void ClientStart(Client client);
    public static event ClientStart? OnClientStart;
    public delegate void ClientDisconnected(Client client);
    public static event ClientDisconnected? OnClientDisconnect;
    public static NetManager ClientManager;
    public static EventBasedNetListener ClientListener;
    public static NetPeer NetClient;
    public static int PingTime = 60;
    public static bool LobbyDataReceived;

    public int Id;
    public string Name;
    internal Client(int id, string username) {
        Id = id;
        Name = username;
    }

    public static void CreateClient(string username) {
        ClientListener = new();
        ClientManager = new(ClientListener) {
            NatPunchEnabled = true,
            UpdateTime = 15
        };
        ClientManager.Start();
        NetPlay.MapClientNetworking();

        Console.ForegroundColor = ConsoleColor.Green;

        Client c = new(0, username);

        TankGame.ClientLog.Write($"Created a new client with name '{username}'.", Internals.LogType.Debug);
        NetPlay.CurrentClient = c;
    }

    public static void AttemptConnectionTo(string address, int port, string password) {
        TankGame.ClientLog.Write($"Attempting connection to server...", Internals.LogType.Debug);
        ClientManager?.Start();
        NetClient = ClientManager!.Connect(address, port, password);
        if (NetClient is not null)
            OnClientStart?.Invoke(NetPlay.CurrentClient!);
    }

    public static void SendCommandUsage(string command) {
        if (!IsConnected()) return;

        NetDataWriter message = new();
        message.Put(PacketID.SendCommandUsage);
        message.Put(command);

        NetClient.Send(message, DeliveryMethod.ReliableOrdered);
    }
    public static void SendQuit() {
        if (MainMenuUI.Active || !IsConnected() || !IsHost())
            return;

        NetDataWriter message = new();
        message.Put(PacketID.QuitLevel);

        NetClient.Send(message, DeliveryMethod.ReliableOrdered);
    }
    public static void SendLives() {
        if (MainMenuUI.Active || !IsConnected())
            return;

        NetDataWriter message = new();
        message.Put(PacketID.SyncLives);

        message.Put(NetPlay.CurrentClient.Id);
        message.Put(PlayerTank.GetMyLives());

        NetClient.Send(message, DeliveryMethod.Unreliable);
    }
    public static void SyncCleanup() {
        if (MainMenuUI.Active || !IsConnected())
            return;

        NetDataWriter message = new();
        message.Put(PacketID.Cleanup);

        NetClient.Send(message, DeliveryMethod.Unreliable);
    }
    public static void SendClientInfo() {
        NetDataWriter message = new();
        message.Put(PacketID.ClientInfo);
        message.Put(NetPlay.CurrentClient.Name);

        NetClient.Send(message, DeliveryMethod.ReliableOrdered);
    }
    public static void RequestLobbyInfo() {
        NetDataWriter message = new();
        message.Put(PacketID.LobbyInfo);

        NetClient.Send(message, DeliveryMethod.ReliableOrdered);
    }
    public static void RequestStartGame(int checkpoint, bool shouldProgressMissions) {
        if (MainMenuUI.Active || !IsConnected())
            return;
        NetDataWriter message = new();
        message.Put(PacketID.StartGame);
        message.Put(checkpoint);
        message.Put(shouldProgressMissions);

        NetClient.Send(message, DeliveryMethod.ReliableOrdered);
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

        NetClient.Send(message, DeliveryMethod.ReliableOrdered);
    }
    public static void SyncPlayerTank(PlayerTank tank) {
        if (MainMenuUI.Active || !IsConnected())
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

        NetClient.Send(message, DeliveryMethod.Unreliable);
    }
    public static void SyncAITank(AITank tank) {
        if (MainMenuUI.Active || !IsConnected())
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

        NetClient.Send(message, DeliveryMethod.Unreliable);
    }
    /// <summary>Be sure to sync by accessing the index of the tank from the AllTanks array. (<see cref="GameHandler.AllTanks"/>)
    /// <para></para>
    /// <c>AllTanks[<paramref name="tankId"/>].Shoot()</c>
    /// </summary>
    /// <param name="tankId">The identified of the <see cref="Tank"/> that fired.</param>
    public static void SyncShellFire(Shell shell) {
        if (MainMenuUI.Active || !IsConnected())
            return;

        NetDataWriter message = new();
        message.Put(PacketID.ShellFire);

        message.Put(shell.Type);
        message.Put(shell.Position);
        message.Put(shell.Velocity);
        message.Put(shell.RicochetsRemaining);
        message.Put(shell.Owner!.WorldId);
        message.Put(shell.Id);

        // ChatSystem.SendMessage($"Pos: {shell.Position} | Vel: {shell.Velocity}", Color.White);

        // FIXME: could probably use more syncing... who cares?


        NetClient.Send(message, DeliveryMethod.Sequenced);
    }
    public static void SyncDamage(int id/*, ITankHurtContext context*/) {
        if (!IsConnected() || MainMenuUI.Active)
            return;
        NetDataWriter message = new();
        message.Put(PacketID.TankDamage);

        message.Put(id);

        //message.Put(context.TankId);
        //message.Put(context.IsPlayer);

        NetClient.Send(message, DeliveryMethod.ReliableOrdered);
    }
    public static void SyncShellDestroy(Shell shell, Shell.DestructionContext cxt) {
        if (!IsConnected() || MainMenuUI.Active)
            return;
        NetDataWriter message = new();
        message.Put(PacketID.ShellDestroy);
        // TODO: bool hasOwner to decide whether or not to subtract from an owner's shell count

        message.Put(shell.Owner.WorldId);
        // send the index of the shell in the owner's OwnedShell array for destruction on other clients
        message.Put(Array.IndexOf(shell.Owner.OwnedShells, shell));
        message.Put((byte)cxt);

        NetClient.Send(message, DeliveryMethod.ReliableOrdered);
    }
    // maybe have owner stuff go here?
    public static void SyncMineDetonate(Mine mine) {
        if (!IsConnected() || MainMenuUI.Active)
            return;
        NetDataWriter message = new();
        message.Put(PacketID.MineDetonate);

        message.Put(mine.Id);

        NetClient.Send(message, DeliveryMethod.ReliableOrdered);
    }
    public static void SyncMinePlace(Vector2 position, float detonateTime, int id) {
        if (MainMenuUI.Active || !IsConnected())
            return;

        NetDataWriter message = new();
        message.Put(PacketID.MinePlacement);

        message.Put(position);
        message.Put(detonateTime);
        message.Put(id);

        NetClient.Send(message, DeliveryMethod.Sequenced);
    }
    public static void SendMessage(string text, Color color, string sender) {
        if (!IsConnected())
            return;
        NetDataWriter message = new();
        message.Put(PacketID.ChatMessage);

        message.Put(text);
        message.Put(color);
        message.Put(sender);

        NetClient.Send(message, DeliveryMethod.ReliableOrdered);
    }
    public static void SendServerNameChange(string newName) {
        if (!IsConnected())
            return;
        NetDataWriter message = new();
        message.Put(PacketID.ServerNameSync);
        message.Put(newName);

        Server.NetManager.SendToAll(message, DeliveryMethod.ReliableOrdered);
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

        NetClient.Send(message, DeliveryMethod.Sequenced);
    }
    public static void SendCampaignByName(string name, int missionId) {
        if (!IsConnected())
            return;
        NetDataWriter message = new();
        message.Put(PacketID.SendCampaignByName);

        message.Put(name);
        message.Put(missionId);

        NetClient.Send(message, DeliveryMethod.ReliableOrdered);
    }
    public static void SendDisconnect(int peerId, string name, string reason) {
        if (!IsConnected())
            return;
        OnClientDisconnect?.Invoke(NetPlay.CurrentClient);
        NetDataWriter message = new();
        message.Put(PacketID.Disconnect);

        message.Put(peerId);
        message.Put(name);
        message.Put(reason);

        NetClient.Send(message, DeliveryMethod.ReliableOrdered);
    }
    public static void SendCampaignStatus(string campaignName, int clientId, bool success) {
        if (!IsConnected())
            return;
        NetDataWriter message = new();
        message.Put(PacketID.SendCampaignStatus);

        message.Put(campaignName);
        message.Put(clientId);
        message.Put(success);

        NetClient.Send(message, DeliveryMethod.ReliableOrdered);
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

        NetClient.Send(message, DeliveryMethod.ReliableOrdered);
    }
    public static void SendDiffiulties() {
        NetDataWriter message = new();

        message.Put(PacketID.SyncDifficulties);
        foreach (var item in Difficulties.Types) {
            message.Put(item.Value);
        }
        message.Put(Difficulties.RandomTanksLower);
        message.Put(Difficulties.RandomTanksUpper);
        message.Put(Difficulties.MonochromeValue);
        message.Put(Difficulties.DisguiseValue);
        NetClient.Send(message, DeliveryMethod.Sequenced);
    }
    public static void SendMapPing(Vector3 location, int pingId, int playerId) {
        NetDataWriter message = new();

        message.Put(PacketID.MapPing);
        message.Put(location);
        message.Put(pingId);
        message.Put(playerId);

        NetClient.Send(message, DeliveryMethod.ReliableOrdered);
    }
    public static bool IsConnected() {
        if (NetClient is not null)
            return NetClient.ConnectionState == ConnectionState.Connected;
        return false;
    }
    public static bool IsHost() => IsConnected() && Server.NetManager is not null;
}
