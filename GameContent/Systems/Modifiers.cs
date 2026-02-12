using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.ID;
using TanksRebirth.GameContent.RebirthUtils;
using TanksRebirth.GameContent.Systems.Coordinates;
using TanksRebirth.GameContent.Systems.TankSystem;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Net;

namespace TanksRebirth.GameContent.Systems;

public record Modifiers {
    public static int MonochromeValue { get; set; }
    public static int RandomTanksUpper { get; set; }
    public static int RandomTanksLower { get; set; }
    public static int DisguiseValue { get; set; }

    public static Dictionary<string, bool> Map { get; } = new() {
        [EXTRA_CALCS]         = false,
        [MINE_SPAM]           = false,
        [BIG_MINES]           = false,
        [TRIPLE_BOUNCE]       = false,
        [INVIS]               = false,
        [STATIONARY]          = false,
        [HOMING]              = false,
        [ARMOR]               = false,
        [BUMP]                = false,
        [MONOCHROME]          = false,
        [INF_LIFE]            = false,
        [MASTER]              = false,
        [PLANES]              = false,
        [MACHINE_GUNS]        = false,
        [RANDOM_ENEMY]        = false,
        [THUNDER]             = false,
        [POV]                 = false,
        [AI_COMPANION]        = false,
        [SHOTGUNS]            = false,
        [PREDICTIONS]         = false,
        [RANDOM_PLAYER]       = false,
        [DEFLECT]             = false,
        [FFA]                 = false,
        [LANTERN]             = false,
        [DISGUISE]            = false
    };
    public static readonly Dictionary<int, int> VanillaToMasterModeConversions = new() {
        [TankID.Brown] = TankID.Bronze,
        [TankID.Ash] = TankID.Silver,
        [TankID.Marine] = TankID.Sapphire,
        [TankID.Yellow] = TankID.Citrine,
        [TankID.Pink] = TankID.Ruby,
        [TankID.Green] = TankID.Emerald,
        [TankID.Violet] = TankID.Amethyst,
        [TankID.White] = TankID.Gold,
        [TankID.Black] = TankID.Obsidian
    };
    public static TankTemplate[] HijackTanks(TankTemplate[] tanks) {
        for (int i = 0; i < tanks.Length; i++) {
            var t = tanks[i];
            if (t.IsPlayer)
                continue;

            var newTemplate = t;

            newTemplate.AiTier = Server.ServerRandom.Next(RandomTanksLower, RandomTanksUpper + 1);
            tanks[i] = newTemplate;
        }
        return tanks;
    }
    public static Mission Flip(Mission mission, bool x = false, bool y = false) {
        if (!(x && y))
            return mission;

        var newMission = mission;

        var tanks = newMission.Tanks;
        var blocks = newMission.Blocks;

        var tanksWithPlacements = new Dictionary<TankTemplate, PlacementSquare>();
        var blocksWithPlacements = new Dictionary<BlockTemplate, PlacementSquare>();

        PlacementSquare.InitializeLevelEditorSquares();

        for (int i = 0; i < tanks.Length; i++) {
            // bro :sob:
            tanksWithPlacements[tanks[i]] = PlacementSquare.Placements.First(x => Vector2.Distance(x.Position.FlattenZ(), tanks[i].Position) < 5);
        }
        for (int i = 0; i < blocks.Length; i++) {
            blocksWithPlacements[blocks[i]] = PlacementSquare.Placements.First(x => Vector2.Distance(x.Position.FlattenZ(), blocks[i].Position) < 5);
        }

        // TODO: this
        // MaxX - PosX = FlipX
        // MaxY - PosY = FlipY
        if (x) {
            for (int i = 0; i < tanks.Length; i++) {

            }
            for (int i = 0; i < blocks.Length; i++) {

            }
        }
        if (y) {
            for (int i = 0; i < tanks.Length; i++) {

            }
            for (int i = 0; i < blocks.Length; i++) {

            }
        }
        return newMission;
    }

    public static void GlobalManage() {
        ManageAirplanes();
    }

    public static void ManageAirplanes() {
        if (!Client.IsHost() && Client.IsConnected()) return;

        if (((DebugManager.DebuggingEnabled && DebugManager.DebugLevel == DebugManager.Id.AirplaneTest) || Map[PLANES]) && CampaignGlobals.InMission) {
            if (RuntimeData.RunTime % 180 <= RuntimeData.DeltaTime) {
                // 33% chance every 5 seconds
                if (Client.ClientRandom.Next(3) == 0) {
                    Airplane.SpawnPlaneWithSmokeGrenades();
                }
            }
        }
    }

    // not enums because it would make compatibility a nightmare.
    // this is good enough!

    // affects only AI
    public const string EXTRA_CALCS           = "tac"; // tac = "tanks are calculators"
    public const string BUMP                  = "bump";
    public const string MASTER                = "master_mode";
    public const string MONOCHROME            = "mono";
    public const string RANDOM_ENEMY          = "rnd_tanks";
    public const string PREDICTIONS           = "preds";
    public const string DEFLECT               = "shel_defl";

    public const string INVIS                 = "invis";
    public const string STATIONARY            = "station";
    public const string ARMOR                 = "armor";
    public const string HOMING                = "homing";

    // affects all tanks
    public const string MACHINE_GUNS          = "mach_gun";
    public const string TRIPLE_BOUNCE         = "tripl_shel";
    public const string SHOTGUNS              = "shg";
    public const string FFA                   = "ffa";

    public const string MINE_SPAM             = "mine_spam";
    public const string BIG_MINES             = "big_mine";

    // affects the player only
    public const string INF_LIFE              = "inf_life";
    public const string RANDOM_PLAYER         = "rnd_pl";
    public const string AI_COMPANION          = "ai_cmp";
    public const string DISGUISE              = "disg";
    
    // gameplay mixups (things that don't just modify stats)
    public const string PLANES                = "tact_planes";
    public const string THUNDER               = "thnd";
    public const string POV                   = "pov";
    public const string LANTERN               = "lant";
}
