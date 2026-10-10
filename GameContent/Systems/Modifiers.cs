using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.ModSupport;
using TanksRebirth.GameContent.ID;
using TanksRebirth.GameContent.RebirthUtils;
using TanksRebirth.GameContent.Systems.Coordinates;
using TanksRebirth.GameContent.Systems.LevelSystem;
using TanksRebirth.GameContent.Tanks;

using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Net;

namespace TanksRebirth.GameContent.Systems;

public record Modifiers {
    static readonly List<ModifierDefinition> _definitions = [];
    static readonly Dictionary<string, ModifierDefinition> _byKey = [];
    static readonly Dictionary<string, int> _values = [];
    static readonly List<string> _categories = [];

    /// <summary>Every registered modifier, in the order they were registered.</summary>
    public static IReadOnlyList<ModifierDefinition> Definitions => _definitions;
    /// <summary>Every section name, in the order they first appeared.</summary>
    public static IReadOnlyList<string> Categories => _categories;

    /// <summary>A version tracker for the modifiers menu, as the menu rebuilds after every change.</summary>
    public static int RegistryVersion { get; private set; }
    /// <summary>Goes up whenever any modifier's value changes (the host re-sends the modifiers when it changes).</summary>
    public static int ValuesVersion { get; private set; }

    /// <summary>Raised after a modifier's value changes: key, old value, new value.</summary>
    public static event Action<string, int, int>? OnModifierChanged;

    static Modifiers() {
        RegisterVanilla();
    }

    /// <summary>Adds a modifier. Throws if the key is taken.</summary>
    internal static ModifierDefinition Register(ModifierDefinition definition) {
        if (_byKey.ContainsKey(definition.Key))
            throw new ArgumentException($"A modifier with the key '{definition.Key}' is already registered.", nameof(definition));
        _definitions.Add(definition);
        _byKey[definition.Key] = definition;
        RegisterCategory(definition.Category);
        RegistryVersion++;
        return definition;
    }

    /// <summary>
    /// Makes a section exist (and fixes its place in the order) before any modifier uses it. 
    /// Sections otherwise appear in the order their first modifier was registered.
    /// </summary>
    public static void RegisterCategory(string category) {
        if (!_categories.Contains(category))
            _categories.Add(category);
    }

    /// <summary>Removes a modifier (its value goes with it). Returns false if there was none with that key.</summary>
    public static bool Unregister(string key) {
        if (!_byKey.Remove(key, out var definition))
            return false;
        _definitions.Remove(definition);
        _values.Remove(key);
        RemoveEmptyCategories();
        RegistryVersion++;
        ValuesVersion++;
        return true;
    }

    /// <summary>Removes every modifier <paramref name="mod"/> registered.</summary>
    public static void UnregisterAll(TanksMod mod) {
        for (int i = _definitions.Count - 1; i >= 0; i--)
            if (_definitions[i].Owner == mod)
                Unregister(_definitions[i].Key);
    }

    static void RemoveEmptyCategories() {
        _categories.RemoveAll(category => !_definitions.Exists(definition => definition.Category == category));
    }

    public static bool TryGetDefinition(string key, out ModifierDefinition definition) => _byKey.TryGetValue(key, out definition!);

    // value management

    /// <summary>The value of a modifier (its default if it was never set, 0 if there's no such modifier).</summary>
    public static int Get(string key) {
        if (_values.TryGetValue(key, out var value))
            return value;
        return _byKey.TryGetValue(key, out var definition) ? definition.Default : 0;
    }

    /// <summary>Whether a modifier is on.</summary>
    public static bool IsOn(string key) => _byKey.TryGetValue(key, out var definition) && definition.IsActiveWith(Get(key));

    /// <summary>Sets a modifier's value, and clamps it.</summary>
    public static void Set(string key, int value) => Set(key, value, constrain: true);

    static void Set(string key, int value, bool constrain) {
        if (!_byKey.TryGetValue(key, out var definition))
            return;
        var (min, max) = definition.Range;
        value = Math.Clamp(value, min, max);
        var old = Get(key);
        if (old == value)
            return;
        _values[key] = value;
        ValuesVersion++;
        definition.Changed?.Invoke(old, value);
        OnModifierChanged?.Invoke(key, old, value);
        if (constrain)
            definition.Constrain?.Invoke(value);
    }

    /// <summary>Turns a toggle on or off. For other kinds, false puts it back to its default and true leaves it alone.</summary>
    public static void SetOn(string key, bool on) {
        if (!_byKey.TryGetValue(key, out var definition))
            return;
        if (definition.Kind == ModifierKind.Toggle)
            Set(key, on ? 1 : 0);
        else if (!on)
            Set(key, definition.Default);
    }

    /// <summary>
    /// Moves a modifier to its next (<paramref name="direction"/> = 1).
    /// </summary>
    public static void Step(string key, int direction) {
        if (!_byKey.TryGetValue(key, out var definition))
            return;
        if (definition.Kind == ModifierKind.Toggle) {
            Set(key, Get(key) != 0 ? 0 : 1);
            return;
        }
        var (min, max) = definition.Range;
        var step = definition.Kind == ModifierKind.Number ? Math.Max(1, definition.Step) : 1;
        var next = Get(key) + direction * step;
        if (definition.Wraps || definition.Kind is ModifierKind.Tank or ModifierKind.Choice) {
            var count = max - min + 1;
            next = ((next - min) % count + count) % count + min;
        }
        Set(key, next);
    }

    /// <summary>Puts a modifier back to its default.</summary>
    public static void Reset(string key) {
        if (_byKey.TryGetValue(key, out var definition))
            Set(key, definition.Default);
    }

    /// <summary>Puts every modifier back to its default.</summary>
    public static void ResetAll() {
        foreach (var definition in _definitions.ToArray())
            Set(definition.Key, definition.Default);
    }

    /// <summary>How many modifiers are on (only those that count toward totals).</summary>
    public static int ActiveCount() {
        var count = 0;
        foreach (var definition in _definitions)
            if (definition.CountsTowardTotal && definition.IsActiveWith(Get(definition.Key)))
                count++;
        return count;
    }

    /// <summary>Every modifier that isn't at its default, as key / value pairs (what the host sends).</summary>
    public static List<KeyValuePair<string, int>> NonDefaultValues() {
        var values = new List<KeyValuePair<string, int>>();
        foreach (var definition in _definitions) {
            var value = Get(definition.Key);
            if (value != definition.Default)
                values.Add(new(definition.Key, value));
        }
        return values;
    }

    /// <summary>
    /// Replaces every value with the host's: anything not in <paramref name="values"/> goes back to its default. 
    /// Returns the keys this client doesn't have (the host has a mod that isn't loaded here).
    /// </summary>
    public static List<string> ApplyHostValues(IEnumerable<KeyValuePair<string, int>> values) {
        var unknown = new List<string>();
        var received = new Dictionary<string, int>();
        foreach (var (key, value) in values) {
            if (_byKey.ContainsKey(key))
                received[key] = value;
            else
                unknown.Add(key);
        }
        foreach (var definition in _definitions.ToArray())
            Set(definition.Key, received.TryGetValue(definition.Key, out var value) ? value : definition.Default, constrain: false);
        return unknown;
    }

    // vanilla modifiers

    public static int MonochromeValue { get => Get(MONOCHROME); set => Set(MONOCHROME, value); }
    public static int RandomTanksLower { get => Get(RANDOM_ENEMY); set => Set(RANDOM_ENEMY, value); }
    public static int RandomTanksUpper { get => Get(RANDOM_ENEMY_MAX); set => Set(RANDOM_ENEMY_MAX, value); }
    public static int DisguiseValue { get => Get(DISGUISE); set => Set(DISGUISE, value); }

    static ModifierDefinition Toggle(string key, string category, string name, string description)
        => Register(new ModifierDefinition(key, ModifierKind.Toggle) {
            Category = category,
            Name = () => name,
            Description = () => description,
        });

    // localize this bullshit lmao
    static void RegisterVanilla() {
        // the order sections are packed into the menu, not the order they read in: Enemies takes two columns, Player and
        // World stack in the third, All Tanks fills the space under Enemies
        RegisterCategory(ModifierCategory.Enemies);
        RegisterCategory(ModifierCategory.Player);
        RegisterCategory(ModifierCategory.World);
        RegisterCategory(ModifierCategory.AllTanks);

        Toggle(MASTER, ModifierCategory.Enemies, "Master Mode",
            "Original tanks will become much more difficult.\nNew music, mechanics, and more!");
        Toggle(BUMP, ModifierCategory.Enemies, "Bump Up",
            "Makes the game a bit harder by \"Bumping up\" each tank, giving them one extra tier.");
        Toggle(EXTRA_CALCS, ModifierCategory.Enemies, "Tanks are Calculators",
            "ALL tanks will begin to look for angles on you (and other enemies) outside of their immediate aim." +
            "\nDo note that this uses significantly more CPU power.");
        Toggle(PREDICTIONS, ModifierCategory.Enemies, "Predictions",
            "Every tank predicts your future position.");
        Toggle(DEFLECT, ModifierCategory.Enemies, "Bullet Blocking",
            "Enemies *attempt* to block your bullets." +
            "\nIt doesn't always work, sometimes even killing teammates. High fire-rate enemies are mostly affected.");
        Toggle(HOMING, ModifierCategory.Enemies, "Seekers",
            "Every enemy tank now has homing bullets.");
        Toggle(ARMOR, ModifierCategory.Enemies, "Armored",
            "Every single non-player tank has 3 armor points added to it.");
        Toggle(INVIS, ModifierCategory.Enemies, "All Invisible",
            "Every single non-player tank is now invisible and no longer lay tracks!");
        Toggle(STATIONARY, ModifierCategory.Enemies, "All Stationary",
            "Every single non-player tank is now stationary.\nThis should REDUCE difficulty.");
        Register(new ModifierDefinition(MONOCHROME, ModifierKind.Tank) {
            Category = ModifierCategory.Enemies,
            Name = () => "Monochrome",
            Description = () => "Makes every tank the tank of your choice. \"Bump Up\" effects are ignored.",
        });

        const string randomDescription = "Every tank is now randomized, rolled between these two tiers." +
            "\nA black tank could appear where a brown tank would be! Both ends need a tank for it to turn on.";
        static bool randomActive() => Get(RANDOM_ENEMY) != TankID.None && Get(RANDOM_ENEMY_MAX) != TankID.None;
        Register(new ModifierDefinition(RANDOM_ENEMY, ModifierKind.Tank) {
            Category = ModifierCategory.Enemies,
            Name = () => "Randomized: Lowest",
            Description = () => randomDescription,
            ActiveWhen = randomActive,
            Constrain = lower => {
                var upper = Get(RANDOM_ENEMY_MAX);
                if (lower != TankID.None && (upper == TankID.None || upper < lower))
                    Set(RANDOM_ENEMY_MAX, lower);
            },
        });
        Register(new ModifierDefinition(RANDOM_ENEMY_MAX, ModifierKind.Tank) {
            Category = ModifierCategory.Enemies,
            Name = () => "Randomized: Highest",
            Description = () => randomDescription,
            ActiveWhen = randomActive,
            CountsTowardTotal = false,
            Constrain = upper => {
                var lower = Get(RANDOM_ENEMY);
                if (upper != TankID.None && (lower == TankID.None || lower > upper))
                    Set(RANDOM_ENEMY, upper);
            },
        });

        Toggle(INF_LIFE, ModifierCategory.Player, "Infinite Lives",
            "You now have infinite lives. Have fun!");
        Toggle(AI_COMPANION, ModifierCategory.Player, "AI Companion",
            "A random tank will spawn at your location and help you throughout every mission.");
        Toggle(RANDOM_PLAYER, ModifierCategory.Player, "Randomized Player",
            "You become a random enemy tank every life.");
        Register(new ModifierDefinition(DISGUISE, ModifierKind.Tank) {
            Category = ModifierCategory.Player,
            Name = () => "Disguise",
            Description = () => "You become a tank of your choosing during gameplay.",
        });

        Toggle(PLANES, ModifierCategory.World, "Tactical Planes",
            "Airplanes will occasionally come through the sky and drop smoke grenades to block your vision!");
        Toggle(THUNDER, ModifierCategory.World, "Thunder Mode",
            "The scene is much darker, and thunder is your only source of decent light.");
        Toggle(LANTERN, ModifierCategory.World, "Lantern Mode",
            "Everything is dark. Only you and your lantern can save you now.");
        Toggle(POV, ModifierCategory.World, "POV Mode",
            "Play the game in the POV of your tank!" +
            "\nYou can move around inter-directionally with WASD, and aim by dragging the mouse.");

        Toggle(MACHINE_GUNS, ModifierCategory.AllTanks, "Machine Guns",
            "Every tank (including the player) now has the ability to fire as fast as they want.");
        Toggle(SHOTGUNS, ModifierCategory.AllTanks, "Shotguns",
            "Every tank now fires a spread of bullets.");
        Toggle(TRIPLE_BOUNCE, ModifierCategory.AllTanks, "Bullet Hell",
            "Bullets now ricochet thrice as much as before!");
        Toggle(BIG_MINES, ModifierCategory.AllTanks, "Ultra Mines",
            "Mines are now 2x as deadly! Their explosion radii are now 2x as big!");
        Toggle(MINE_SPAM, ModifierCategory.AllTanks, "Lemon Pie Factory",
            "Makes yellow tanks absurdly more dangerous by turning them into mine-laying machines." +
            "\nOh, yeah. They're immune to explosions now too.");
        Toggle(FFA, ModifierCategory.AllTanks, "Free-for-all",
            "Every tank is on their own!");
    }

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

    // fun modifiers stuff
    public static TankTemplate[] HijackTanks(TankTemplate[] tanks) {
        var missionId = CampaignGlobals.LoadedCampaign.CurrentMissionId;
        for (int i = 0; i < tanks.Length; i++) {
            var t = tanks[i];
            if (t.IsPlayer)
                continue;

            var newTemplate = t;

            newTemplate.AiTier = Server.RandomFor(Server.RandomKey.ENEMY_TIER, missionId, i).Next(RandomTanksLower, RandomTanksUpper + 1);
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

        var tanksWithPlacements = new Dictionary<TankTemplate, EditorTile>();
        var blocksWithPlacements = new Dictionary<BlockTemplate, EditorTile>();

        EditorTile.InitializeLevelEditorSquares();

        for (int i = 0; i < tanks.Length; i++) {
            // bro :sob:
            tanksWithPlacements[tanks[i]] = EditorTile.AllTiles.First(x => Vector2.Distance(x.Position.FlattenZ(), tanks[i].Position) < 5);
        }
        for (int i = 0; i < blocks.Length; i++) {
            blocksWithPlacements[blocks[i]] = EditorTile.AllTiles.First(x => Vector2.Distance(x.Position.FlattenZ(), blocks[i].Position) < 5);
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

        if (((DebugManager.DebuggingEnabled && DebugManager.DebugLevel == DebugManager.Id.AirplaneTest) || IsOn(PLANES)) && CampaignGlobals.InMission) {
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
    public const string RANDOM_ENEMY_MAX      = "rnd_tanks_max";
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
