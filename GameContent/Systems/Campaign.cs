using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TanksRebirth.Enums;
using TanksRebirth.GameContent.ID;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.RebirthUtils;
using TanksRebirth.GameContent.Systems.Coordinates;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common.Framework;
using TanksRebirth.Internals.Common.Framework.Graphics;
using TanksRebirth.Internals.Common.IO;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Net;
using TanksRebirth.GameContent.UI.LevelEditor;
using TanksRebirth.GameContent.Systems.AI;

namespace TanksRebirth.GameContent.Systems;

/// <summary>A campaign for players to play on with <see cref="AITank"/>s, or even <see cref="PlayerTank"/>s if supported.</summary>
public class Campaign
{
    public delegate void MissionLoadDelegate(Tank[] tanks, Block[] blocks);
    public static event MissionLoadDelegate OnMissionLoad;

    public delegate void PreLoadTankDelegate(ref TankTemplate template);
    public static event PreLoadTankDelegate OnPreLoadTank;

    public delegate void PreLoadBlockDelegate(ref BlockTemplate template);
    public static event PreLoadBlockDelegate OnPreLoadBlock;

    /// <summary>Returns the names of campaigns in the user's <c>Campaigns/</c> directory.</summary>
    public static string[] GetCampaignNames()
        => Directory.GetFiles(Path.Combine(TankGame.SaveDirectory, "Campaigns")); //IOUtils.GetSubFolders(Path.Combine(TankGame.SaveDirectory, "Campaigns"), true);
    public Mission[] CachedMissions;
    public Mission CurrentMission { get; private set; }
    public Mission LoadedMission { get; private set; }
    public int CurrentMissionId { get; private set; }

    /// <summary>The meta-data for this campaign.</summary>
    public CampaignMetaData MetaData;

    public Campaign() {
        CachedMissions = new Mission[1];
        TrackedSpawnPoints = [];
        MetaData = CampaignMetaData.GetDefault();
    }
    /// <summary>Load a specific mission into the current mission.</summary>
    /// <param name="mission">The mission.</param>
    public void LoadMission(Mission mission) {
        if (string.IsNullOrEmpty(mission.Name))
            return;

        TrackedSpawnPoints = new (Vector2, bool)[mission.Tanks.Length];
        LoadedMission = mission;
    }
    /// <summary>Load a mission already in memory by ID.</summary>
    /// <param name="id">The index of the mission in the <see cref="CachedMissions"/> array.</param>
    public void LoadMission(int id) {
        LoadedMission = CachedMissions[id];

        CurrentMissionId = id;
        if (LoadedMission.Tanks != null) {
            TrackedSpawnPoints = new (Vector2, bool)[LoadedMission.Tanks.Length];
            for (int i = 0; i < LoadedMission.Tanks.Length; i++) {
                TrackedSpawnPoints[i].Position = LoadedMission.Tanks[i].Position;
                TrackedSpawnPoints[i].Alive = true;
            }
        }
    }

    /// <summary>Loads an array of <see cref="Mission"/>s into memory.</summary>
    public void LoadMissionsToCache(params Mission[] missions) {
        var list = CachedMissions.ToList();

        list.AddRange(missions);

        CachedMissions = [.. list];
    }

    /// <summary>Loads the next mission in the <see cref="Campaign"/>.</summary>
    public void LoadNextMission() {
        if (CurrentMissionId + 1 >= CachedMissions.Length) {
            TankGame.ClientLog.Write($"CachedMissions[{CurrentMissionId + 1}] is not existent.", LogType.Warn);
            return;
        }
        CurrentMissionId++;

        LoadedMission = CachedMissions[CurrentMissionId];

        TrackedSpawnPoints = new (Vector2, bool)[LoadedMission.Tanks.Length];
        for (int i = 0; i < LoadedMission.Tanks.Length; i++) {
            TrackedSpawnPoints[i].Position = LoadedMission.Tanks[i].Position;
            TrackedSpawnPoints[i].Alive = true;
        }
        // run line 120 and 121 in each when i get back
    }

    // FIXME: not sure why this is public?
    public (Vector2 Position, bool Alive)[] TrackedSpawnPoints { get; set; } // position of spawn, alive

    /// <summary>Sets up the <see cref="Mission"/> that is loaded.</summary>
    /// <param name="spawnNewSet">If true, will spawn all tanks as if it's the first time the player(s) has/have entered this mission.</param>
    public void SetupLoadedMission(bool spawnNewSet) {
        // FIXME: source of level editor bug.
        PlacementSquare.ResetSquares();
        SceneManager.CleanupEntities();
        const int roundingFactor = 5;
        int numPlayers = 0;
        for (int i = 0; i < LoadedMission.Tanks.Length; i++) {
            var template = LoadedMission.Tanks[i];

            if (spawnNewSet) {
                TrackedSpawnPoints[i].Position = LoadedMission.Tanks[i].Position;
                TrackedSpawnPoints[i].Alive = true;
            }

            while (template.Rotation < 0) {
                template.Rotation += MathHelper.Tau;
            }

            while (template.Rotation > MathHelper.Tau) {
                template.Rotation -= MathHelper.Tau;
            }

            OnPreLoadTank?.Invoke(ref template);

            var chassisRotation = MathF.Round(template.Rotation, roundingFactor);

            if (!template.IsPlayer) {
                if (TrackedSpawnPoints[i].Alive) {
                    var tank = template.GetAiTank();

                    tank.Position = template.Position;

                    tank.TankRotation = chassisRotation;
                    tank.TargetTankRotation = chassisRotation;
                    tank.TurretRotation = MathF.Round(-template.Rotation, roundingFactor);
                    tank.Dead = false;
                    tank.Team = template.Team;
                    if (CampaignGlobals.ShouldMissionsProgress) {
                        tank.OnDestroy += () => {
                            TrackedSpawnPoints[Array.IndexOf(TrackedSpawnPoints, TrackedSpawnPoints.First(pos => pos.Position == template.Position))].Alive = false; // make sure the tank is not spawned again
                        };
                    }
                    var placement = PlacementSquare.Placements.FindIndex(place => Vector3.Distance(place.Position, tank.Position3D) < Block.SIDE_LENGTH / 2);

                    if (placement > -1) {
                        // ChatSystem.SendMessage("Loaded " + TankID.Collection.GetKey(tank.Tier), Color.Blue);
                        PlacementSquare.Placements[placement].TankId = tank.WorldId;
                        PlacementSquare.Placements[placement].HasBlock = false;
                    }
                }
            }
            else {
                numPlayers++;
                if ((Client.IsConnected() && numPlayers <= Server.ConnectedClients.Count(x => x is not null)) || !Client.IsConnected()) {
                    var tank = template.GetPlayerTank();

                    tank.Position = template.Position;
                    tank.TankRotation = chassisRotation;
                    tank.TargetTankRotation = chassisRotation;
                    tank.TurretRotation = MathF.Round(-template.Rotation, roundingFactor);
                    tank.Dead = false;
                    tank.Team = template.Team;

                    if (tank.PlayerId <= Server.CurrentClientCount) {
                        if (!LevelEditorUI.Active) {
                            if (NetPlay.IsClientMatched(tank.PlayerId)) {
                                PlayerTank.MyTeam = tank.Team;
                                PlayerTank.MyTankType = tank.PlayerType;
                            }
                        }
                    }
                    else if (!LevelEditorUI.Active)
                        tank.Remove(true);
                    if (Client.IsConnected()) {
                        if (PlayerTank.Lives[tank.PlayerId] <= 0)
                            tank.Remove(true);
                    }
                    // TODO: note to self, this code above is what causes the skill issue.
                    if (Difficulties.Types["AiCompanion"] && 
                        (template.PlayerType == Server.CurrentClientCount + PlayerID.Red || (Server.CurrentClientCount == 4 && template.PlayerType == PlayerID.Yellow))) {
                        var randomTier = AITank.PickRandomTier();
                        var tnk = new AITank(randomTier) {
                            // target = rot - pi
                            // turret =  -rot
                            Position = template.Position,
                            Team = tank.Team,
                            TankRotation = MathF.Round(template.Rotation, roundingFactor),
                            TargetTankRotation = MathF.Round(template.Rotation, roundingFactor),
                            TurretRotation = MathF.Round(-template.Rotation, roundingFactor),
                            Dead = false,
                        };
                        tnk.Physics.Position = template.Position / Tank.UNITS_PER_METER;
                    }
                    var placement = PlacementSquare.Placements.FindIndex(place => Vector3.Distance(place.Position, tank.Position3D) < Block.SIDE_LENGTH / 2);

                    if (placement > -1) {
                        PlacementSquare.Placements[placement].TankId = tank.WorldId;
                        PlacementSquare.Placements[placement].HasBlock = false;
                    }
                }
            }
        }

        for (int b = 0; b < LoadedMission.Blocks.Length; b++) {
            var template = LoadedMission.Blocks[b];

            OnPreLoadBlock?.Invoke(ref template);

            var block = template.GetBlock();

            var placement = PlacementSquare.Placements.FindIndex(place => Vector3.Distance(place.Position, block.Position3D) < Block.SIDE_LENGTH / 2);
            if (placement > -1) {
                PlacementSquare.Placements[placement].BlockId = block.Id;
                PlacementSquare.Placements[placement].HasBlock = true;
            }
        }

        CurrentMission = LoadedMission;
        TankGame.ClientLog.Write($"Loaded mission '{LoadedMission.Name}' with {LoadedMission.Tanks.Length} " +
            $"({LoadedMission.Tanks.Count(x => x.IsPlayer)} player(s), {LoadedMission.Tanks.Count(x => !x.IsPlayer)} AI(s)) tanks and {LoadedMission.Blocks.Length} obstacles.", LogType.Info);

        OnMissionLoad?.Invoke(GameHandler.AllTanks, Block.AllBlocks);
    }

    [Obsolete("This is for the legacy version of TanksRebirth.")]
    /// <summary>
    /// Loads missions from inside the <paramref name="campaignName"/> folder to memory.
    /// </summary>
    /// <param name="campaignName">The name of the campaign folder to load files from.</param>
    /// <param name="autoSetLoadedMission">Sets the currently loaded mission to the first mission loaded from this folder.</param>
    /// <exception cref="FileLoadException"></exception>
    public static Campaign LoadFromFolder(string campaignName, bool autoSetLoadedMission) {
        Campaign campaign = new();

        var root = Path.Combine(TankGame.SaveDirectory, "Campaigns");
        var path = Path.Combine(root, campaignName);
        Directory.CreateDirectory(root);
        if (!Directory.Exists(path)) {
            TankGame.ClientLog.Write($"Could not find a campaign folder with name {campaignName}. Aborting folder load...", LogType.Warn);
            return default!;
        }

        CampaignMetaData properties = CampaignMetaData.Get(path, "_properties.json");

        var files = Directory.GetFiles(path).Where(file => file.EndsWith(".mission")).ToArray();

        Mission[] missions = new Mission[files.Length];

        Span<Mission> missionSpan = missions;
        ReadOnlySpan<string> missionFileSpan = files;

        ref var searchSpaceMissions = ref MemoryMarshal.GetReference(missionSpan);
        ref var searchSpaceFiles = ref MemoryMarshal.GetReference(missionFileSpan);
        for (var i = 0; i < files.Length; i++) {
            ref var mission = ref Unsafe.Add(ref searchSpaceMissions, i);
            var file = Unsafe.Add(ref searchSpaceFiles, i);

            mission = Mission.Load(file, "");
            // campaignName argument is empty since we are loading from the campaign folder anyway. 

        }
        campaign.CachedMissions = missions;

        if (autoSetLoadedMission) {
            campaign.LoadMission(0); // first mission in campaign
            campaign.TrackedSpawnPoints = new (Vector2, bool)[campaign.LoadedMission.Tanks.Length];
            PlayerTank.StartingLives = properties.StartingLives;
        }

        campaign.MetaData = properties;


        return campaign;
    }

    /// <summary>
    /// Saves the campaign as a <c>.campaign</c> file.
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="campaign"></param>
    public static void Save(string fileName, Campaign campaign) {
        var endsWith = fileName.EndsWith(".campaign");
        var newFileName = endsWith ? fileName : (fileName + ".campaign");
        using var writer = new BinaryWriter(File.Open(newFileName, FileMode.OpenOrCreate));

        writer.Write(LevelEditorUI.LevelFileHeader);
        writer.Write(LevelEditorUI.EDITOR_VERSION);

        int totalMissions = campaign.CachedMissions.Count(m => m != default);
        writer.Write(totalMissions);

        writer.Write(campaign.MetaData.Name);
        writer.Write(campaign.MetaData.Description);
        writer.Write(campaign.MetaData.Author);
        writer.Write(campaign.MetaData.Tags.Length);

        Array.ForEach(campaign.MetaData.Tags, writer.Write);
        writer.Write(campaign.MetaData.StartingLives);

        writer.Write(campaign.MetaData.Version);
        writer.Write(campaign.MetaData.HasMajorVictory);
        writer.Write(campaign.MetaData.MissionStripColor);
        writer.Write(campaign.MetaData.BackgroundColor);

        for (int i = 0; i < totalMissions; i++)
            campaign.CachedMissions[i].WriteToStream(writer);

        ChatSystem.SendMessage($"Saved campaign with {totalMissions} missions.", Color.Lime);
    }

    public static Campaign Load(string fileName) {
        Campaign campaign = new();

        using var reader = new BinaryReader(File.Open(Path.Combine(TankGame.SaveDirectory, fileName), FileMode.Open, FileAccess.Read));

        var header = reader.ReadBytes(4);
        if (!header.SequenceEqual(LevelEditorUI.LevelFileHeader))
            throw new FileLoadException($"The byte header of this file does not match what this game expects! File name = \"{fileName}\"");
        var editorVersion = reader.ReadInt32();
        if (editorVersion < 2)
            throw new FileLoadException($"Cannot load a campaign at this level editor version! File name = \"{fileName}\"");
        // first available version.
        if (editorVersion == 2 || editorVersion == 3) {
            var totalMissions = reader.ReadInt32();

            campaign.CachedMissions = new Mission[totalMissions];

            campaign.MetaData.Name = reader.ReadString();
            campaign.MetaData.Description = reader.ReadString();
            campaign.MetaData.Author = reader.ReadString();
            campaign.MetaData.Tags = new string[reader.ReadInt32()];
            for (int j = 0; j < campaign.MetaData.Tags.Length; j++)
                campaign.MetaData.Tags[j] = reader.ReadString();
            campaign.MetaData.StartingLives = reader.ReadInt32();
            campaign.MetaData.ExtraLivesMissions = new int[reader.ReadInt32()];
            for (int j = 0; j < campaign.MetaData.ExtraLivesMissions.Length; j++)
                campaign.MetaData.ExtraLivesMissions[j] = reader.ReadInt32();

            campaign.MetaData.Version = reader.ReadString();
            campaign.MetaData.HasMajorVictory = reader.ReadBoolean();
            campaign.MetaData.MissionStripColor = reader.ReadColor();
            campaign.MetaData.BackgroundColor = reader.ReadColor();

            for (int i = 0; i < totalMissions; i++) {
                campaign.CachedMissions[i] = Mission.Read(reader);
                var isIdMatched = campaign.MetaData.ExtraLivesMissions.Any(x => x == i + 1);
                campaign.CachedMissions[i].GrantsExtraLife = isIdMatched;
            }
        }
        else if (editorVersion == 4) {
            var totalMissions = reader.ReadInt32();

            campaign.CachedMissions = new Mission[totalMissions];

            campaign.MetaData.Name = reader.ReadString();
            campaign.MetaData.Description = reader.ReadString();
            campaign.MetaData.Author = reader.ReadString();
            campaign.MetaData.Tags = new string[reader.ReadInt32()];
            for (int j = 0; j < campaign.MetaData.Tags.Length; j++)
                campaign.MetaData.Tags[j] = reader.ReadString();
            campaign.MetaData.StartingLives = reader.ReadInt32();

            campaign.MetaData.Version = reader.ReadString();
            campaign.MetaData.HasMajorVictory = reader.ReadBoolean();
            campaign.MetaData.MissionStripColor = reader.ReadColor();
            campaign.MetaData.BackgroundColor = reader.ReadColor();

            for (int i = 0; i < totalMissions; i++) {
                // TODO: do for loop, load each.

                campaign.CachedMissions[i] = Mission.Read(reader);
            }
        }
        return campaign;
    }
    /// <summary>The metadata for any given campaign.</summary>
    public struct CampaignMetaData
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Author { get; set; }
        public string Version { get; set; }
        public string[] Tags { get; set; }
        public bool HasMajorVictory { get; set; }

        public int[] ExtraLivesMissions { get; set; }
        public int StartingLives { get; set; }

        public UnpackedColor BackgroundColor { get; set; }
        public UnpackedColor MissionStripColor { get; set; }
        public static CampaignMetaData Get(string path, string fileName) {
            var properties = GetDefault();

            var file = Path.Combine(path, fileName);

            JsonHandler<CampaignMetaData> handler = new(properties, file);

            if (!File.Exists(file)) {
                handler.Serialize(new() { WriteIndented = true }, true);
                return properties;
            }

            properties = handler.Deserialize();

            return properties;
        }

        public static CampaignMetaData GetDefault() {
            return new() {
                Name = "Unnamed",
                Description = "No description",
                Author = "Unknown",
                Version = "0.0.0.0",
                Tags = ["N/A"],
                ExtraLivesMissions = [],
                StartingLives = 3,
                BackgroundColor = IntermissionSystem.DefaultBackgroundColor,
                MissionStripColor = IntermissionSystem.DefaultStripColor,
                HasMajorVictory = false
            };
        }
    }

    /*public int LoadRandomizedMission(Range<int> missionRange, TankTier highestTier = TankTier.None, int highestCount = 0)
    {
        if (missionRange.Max >= CachedMissions.Length)
            missionRange.Max = CachedMissions.Length - 1;

        int num = Client.ClientRandom.Next(missionRange.Min, missionRange.Max);

        var mission = CachedMissions[num];

        for (int i = 0; i < mission.Tanks.Length; i++)
        {
            var tnk = mission.Tanks[i];

            if (!tnk.IsPlayer)
            {
                tnk.AiTier = TankTier.Random;
                tnk.RandomizeRange = new();
            }
        }

        return num;
    }*/
    // Considering making all 100 campaigns unique...
}