using FontStashSharp;
using MeltySynth;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.ID;
using TanksRebirth.GameContent.RebirthUtils;
using TanksRebirth.GameContent.Systems;
using TanksRebirth.GameContent.UI.MainMenu;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common.Framework.Collections;
using TanksRebirth.Internals.Common.Framework.Interfaces;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Localization;

namespace TanksRebirth.GameContent.ModSupport;

// fuck IDE0044
#pragma warning disable CA2211, IDE0044
public enum LoadStatus {
    /// <summary>No mod is being loaded.</summary>
    Inactive,
    /// <summary>Every mod is being unloaded.</summary>
    Unloading,
    /// <summary>A mod is being loaded.</summary>
    Loading,
    /// <summary>A mod is being compiled.</summary>
    Compiling,
    /// <summary>All mods are finished loading.</summary>
    Complete
}
public static class ModLoader {
    public delegate void FinishModLoading();
    /// <summary>Fired when every mod is finished loading.</summary>
    public static event FinishModLoading? OnFinishModLoading;
    public delegate void PostLoadModContent(TanksMod mod);
    /// <summary>Fired when a specific mod is loaded</summary>
    public static event PostLoadModContent? OnPostModLoad;

    static JsonSerializerOptions _indented = new() { WriteIndented = true };

    /// <summary>All mods currently loaded into Tanks Rebirth.</summary>
    public static List<TanksMod> LoadedMods { get; set; } = [];
    static List<AssemblyLoadContext> _loadedAlcs = [];

    static IModContent? _loadingContent;

    /// <summary>True if mods are loading.</summary>
    public static bool IsLoadingMods { get; private set; }
    /// <summary>Determined at loading-time. False if:
    /// <list type="bullet">
    ///   <item>
    ///     <description>The user does not have a .NET SDK Version >= MIN_NET_VERSION</description>
    ///   </item>
    ///   <item>
    ///     <description>User does not have a SDK verison at all.</description>
    ///   </item>
    /// </list>
    /// </summary>
    public static bool AreCompilesAllowed { get; set; } = false;
    /// <summary>Number of mods * number of actions per mod (load, initialize).</summary>
    public static int ActionsNeeded { get; private set; }
    /// <summary>Number of actions that have already been completed in the mod loading process.</summary>
    public static int ActionsComplete { get; private set; }
    /// <summary>The current status of the mod loader.</summary>
    public static LoadStatus Status { get; private set; } = LoadStatus.Inactive;
    /// <summary>The namespace of the mod being loaded.</summary>
    public static string ModBeingLoaded { get; private set; } = string.Empty;
    /// <summary>The path to where mods are loaded from.</summary>
    public static string ModsPath { get; } = Path.Combine(TankGame.SaveDirectory, "Mods");
    /// <summary>The oldest .NET version (within the CSPROJ) Tanks Rebirth expects mods to be.</summary>
    public const string EXPECTED_NET_VERSION = "net8.0";
    /// <summary>The oldest .NET version Tanks Rebirth expects mod to be.</summary>
    public const int MIN_NET_VERSION = 8;

    volatile static List<Action> _loadingActions = [];

    // set within the mods menu, generally
    public static List<string> FirstLoadMods = [];

    /// <summary>The names of every folder in the mods folder, enabled or not, and whether or not it contains a loadable mod.</summary>
    internal static List<string> modNames = [];

    /// <summary>The file within <see cref="ModsPath"/> that stores the names of the mods that should not be loaded.</summary>
    public static string DisabledModsPath { get; } = Path.Combine(ModsPath, "disabled.json");
    // compared case-insensitively since mod names are folder names
    static HashSet<string> _disabledMods = new(StringComparer.OrdinalIgnoreCase);
    /// <summary>A mod-agnostic list of modded tanks.</summary>
    public static ModTank[] ModTanks { get; private set; } = [];
    static List<ModTank> _modTanks = [];
    /// <summary>A mod-agnostic list of modded blocks.</summary>
    public static ModBlock[] ModBlocks { get; private set; } = [];
    static List<ModBlock> _modBlocks = [];
    /// <summary>A mod-agnostic list of modded shells.</summary>
    public static ModShell[] ModShells { get; private set; } = [];
    static List<ModShell> _modShells = [];

    // i could PROBABLY make this not miserable and just have this not be a new class per modifier, but the implementation idea is nice for now
    /// <summary>A mod-agnostic list of modded modifiers.</summary>
    public static ModModifier[] ModModifiers { get; private set; } = [];
    static List<ModModifier> _modModifiers = [];

    static bool _firstLoad = true;
    /// <summary>Set when a mod fails to compile or load. Every mod is then unloaded for this session. <see cref="DisabledModsPath"/> is left alone.</summary>
    static volatile bool _loadFailed;
    /// <summary>The error given from the mod-loading process.</summary>
    public static string Error = string.Empty;
    /// <summary>What kind of compilation is performed- Debug or Release.</summary>
    public static string LoadType = string.Empty;
    public static bool CheckIfCompilesAreAllowed() {
        if (!RuntimeData.IsWindows) {
            TankGame.ClientLog.Write("Auto-compilation disallowed. Current OS is not Windows.", RebirthLogLevel.Warn);
            return false;
        }
        try {
            // painfully local code.
            var checkPath = "C:\\Program Files\\dotnet\\sdk";
            Process process = new();
            process.StartInfo.FileName = "dotnet.exe";
            process.StartInfo.Arguments = "--list-sdks";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.Start();

            string[] versions = [.. process.StandardOutput
            .ReadToEnd()
            .TrimEnd()
            .Replace("[", string.Empty)
            .Replace("]", string.Empty)
            .Replace("\r", string.Empty)
            .Replace(checkPath, string.Empty)
            .Split("\n")
            .Select(x => x.Trim())];

            var versionsSingular = string.Join(", ", versions);
            var versionsReal = versions.Select(x => new Version(x)).ToArray();

            process.WaitForExit();
            // check if any version starts with a '8' to indicate that it is a .NET 8.0 SDK.
            if (versionsReal.All(x => x.Major < MIN_NET_VERSION)) {
                TankGame.ClientLog.Write($"Auto-compilation disallowed. User does not have a .NET {MIN_NET_VERSION}.0 SDK or higher installed.", RebirthLogLevel.Warn);
                return false;
            }

            TankGame.ClientLog.Write($"Auto-compile allowed. (.NET {versionsReal[^1]})", RebirthLogLevel.Info);
            return true;
        } catch (Exception e) {
            var trace = new StackTrace(e, true);
            var frame = trace.GetFrame(0);
            int line = frame?.GetFileLineNumber() ?? -1;

            TankGame.ClientLog.Write($"Auto-compile eligibility failed. (reason={e.Message}, where={line})", RebirthLogLevel.Info);

            return false;
        }
    }
    /// <summary>Builds a mod. Returns false if it failed to build, with <see cref="Error"/> set.</summary>
    static bool AttemptCompile(string modName) {
        if (!AreCompilesAllowed) return true;

        Status = LoadStatus.Compiling;
        Process proc = new();
        try {
            // uses dotnet instead of just the cli -> dotnet command
            ProcessStartInfo startInfo = new() {
                FileName = "dotnet",
                Arguments = $"build -c {LoadType}",
                WorkingDirectory = Path.Combine(ModsPath, modName),
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                RedirectStandardOutput = true,
            };

            proc.StartInfo = startInfo;
            proc.Start();

            var output = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit();

            if (proc.ExitCode != 0) {
                var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
                var errorLine = lines.FirstOrDefault(l => l.Contains("error", StringComparison.OrdinalIgnoreCase));
                Error = errorLine ?? "Unknown Build Failure";
                TankGame.ReportError(new Exception(Error));
                return false;
            }

            proc.WaitForExit();

            ActionsComplete++;
            return true;
        } catch (Exception e) {
            Error = e.Message;
            proc.Dispose();
            proc.Close();
            return false;
        }
    }
    /// <summary>Whether the mod named <paramref name="modName"/> will be loaded the next time mods are loaded.</summary>
    public static bool IsModEnabled(string modName) => !_disabledMods.Contains(modName);

    /// <summary>The path to the <c>.csproj</c> directly inside <paramref name="modFolder"/>, or null if there isn't one. A folder needs one to be a mod.</summary>
    public static string? FindModProject(string modFolder) => Directory.GetFiles(modFolder, "*.csproj").FirstOrDefault();

    /// <summary>Whether the folder named <paramref name="modName"/> has a <c>.csproj</c>, which is what makes it a mod.</summary>
    public static bool HasModProject(string modName) => FindModProject(Path.Combine(ModsPath, modName)) is not null;

    /// <summary>Enables or disables a mod for the next load, and saves the change to <see cref="DisabledModsPath"/>.</summary>
    public static void SetModEnabled(string modName, bool enabled) {
        var changed = enabled ? _disabledMods.Remove(modName) : _disabledMods.Add(modName);
        if (changed)
            SaveDisabledMods();
    }

    /// <summary>Flips whether a mod is enabled. Returns the new state.</summary>
    public static bool ToggleMod(string modName) {
        var enabled = !IsModEnabled(modName);
        SetModEnabled(modName, enabled);
        return enabled;
    }

    static void LoadDisabledMods() {
        _disabledMods.Clear();

        if (!File.Exists(DisabledModsPath)) return;

        try {
            var names = JsonSerializer.Deserialize<string[]>(File.ReadAllText(DisabledModsPath));
            if (names is not null)
                _disabledMods.UnionWith(names);
        } catch (Exception e) {
            TankGame.ClientLog.Write($"Could not read '{Path.GetFileName(DisabledModsPath)}': {e.Message}", RebirthLogLevel.Warn);
        }
    }

    static void SaveDisabledMods() {
        try {
            Directory.CreateDirectory(ModsPath);
            File.WriteAllText(DisabledModsPath, JsonSerializer.Serialize(_disabledMods.OrderBy(x => x).ToArray(), _indented));
        } catch (Exception e) {
            TankGame.ClientLog.Write($"Could not save '{Path.GetFileName(DisabledModsPath)}': {e.Message}", RebirthLogLevel.Warn);
        }
    }

    internal static void UnloadAll() {
        if (MainMenuUI.IsActive) {
            SceneManager.CleanupEntities();
            SceneManager.CleanupScene();
        }
        if (Status == LoadStatus.Unloading) {
            ChatSystem.SendMessage("Mods are currently unloading! Unable to unload mods.", Color.Red);
            return;
        }

        TankGame.ClientLog.Write("Unloading mods...", RebirthLogLevel.Info);
        Status = LoadStatus.Unloading;
        _loadingActions.Clear();
        LoadedMods.ForEach(mod => {
            // for indivudally unloaded mods.

            var content = mod.Data;

            // unload modded stuff and all data
            var modTankCount = content.Tanks.Count;
            for (int i = modTankCount - 1; i >= 0; i--) {
                content.Tanks[i].Unload();
            }
            // this allows the next mod to unload properly... thrice
            ModTank.unloadOffset += modTankCount;
            content.Tanks.Clear();

            var modBlockCount = content.Blocks.Count;
            for (int i = modBlockCount - 1; i >= 0; i--) {
                content.Blocks[i].Unload();
            }
            ModBlock.unloadOffset += modBlockCount;
            content.Blocks.Clear();
            var modShellCount = content.Shells.Count;
            for (int i = modShellCount - 1; i >= 0; i--) {
                content.Shells[i].Unload();
            }
            ModShell.unloadOffset += modShellCount;
            content.Shells.Clear();

            foreach (var modifier in _modModifiers.FindAll(modifier => modifier.Mod == mod))
                modifier.Unload();
            Modifiers.UnregisterAll(mod);

            mod.OnUnload();
            ModRegistry.UnregisterAllConditionsForMod(mod);
            UnloadModContent(mod);
        });
        LoadedMods.Clear();
        _modModifiers.Clear();
        ModModifiers = [];
        // for when the unloading process is done.
        ModRegistry.singletonMap.Clear();
        ModRegistry.idToModTank.Clear();
        ModRegistry.idToModShell.Clear();
        ModRegistry.idToModBlock.Clear();
        _loadedAlcs.Clear();
        ResetContentDictionaries();
        ModTank.unloadOffset = 0;
        ModBlock.unloadOffset = 0;
        ModShell.unloadOffset = 0;
        TankGame.ClientLog.Write("Mod unload successful!", RebirthLogLevel.Info);
        Status = LoadStatus.Complete;
    }
    // doesn't work?
    static void ResetContentDictionaries() {
        BlockID.Collection = new(MemberType.Fields);
        PingID.Collection = new(MemberType.Fields);
        PlayerID.Collection = new(MemberType.Fields);
        ShellID.Collection = new(MemberType.Fields);
        TankID.Collection = new(MemberType.Fields);
        TeamID.Collection = new(MemberType.Fields);
        TrackID.Collection = new(MemberType.Fields);
    }
    // strictly unloads static events within the assembly
    static void UnloadModContent(TanksMod mod) {
        // assembly that belongs to this mod (collectible ALC)
        var modAssembly = mod.GetType().Assembly;

        var types = Assembly.GetExecutingAssembly().GetTypes();

        foreach (var type in types) {
            if (type == null) continue;

            // only static
            var events = type.GetEvents(BindingFlags.Static | BindingFlags.Public);

            foreach (var ev in events) {
                try {
                    // attempts to find the backing field
                    var field = type.GetField(ev.Name, BindingFlags.NonPublic | BindingFlags.Static)
                        ?? type.GetField($"<{ev.Name}>k__BackingField",
                            BindingFlags.NonPublic | BindingFlags.Static);

                    if (field == null) {
                        continue;
                    }

                    if (field.GetValue(null) is not MulticastDelegate del)
                        continue;

                    // forcefully strip the event of any handlers that belong to the mod's assembly
                    foreach (var handler in del.GetInvocationList()) {
                        var handlerAsm = handler.Method.DeclaringType?.Assembly;
                        if (handlerAsm == modAssembly) {
                            // For static events the target is null
                            ev.RemoveEventHandler(null, handler);
                            TankGame.ClientLog.Write($"Mod {mod.InternalName} forgot to unsubscribe from event {ev.DeclaringType.Name}.{ev.Name}. Unsubscribing...", RebirthLogLevel.Info);
                        }
                    }
                } catch {
                    // oh well lol we tried
                }
            }
        }
        mod.Data.assemblyContainer.Unload();
    }

    /// <summary>Prepare your garbage collector!</summary>
    internal static void LoadMods() {
        if (Status == LoadStatus.Unloading) {
            TankGame.ClientLog.Write("Mods are currently unloading! Unable to load mods.", RebirthLogLevel.Warn);
            return;
        }
        if (IsLoadingMods) {
            TankGame.ClientLog.Write("Mods are currently loading! Unable to load mods.", RebirthLogLevel.Warn);
            return;
        }

        if (LoadedMods.Count > 0) {
            UnloadAll();
        }
        else {
            TankGame.ClientLog.Write("No mods to unload. Skipping...", RebirthLogLevel.Warn);
        }

        LoadType = Debugger.IsAttached ? "Debug" : "Release";

        ActionsNeeded = 0;
        ActionsComplete = 0;

        if (!_firstLoad)
            TankGame.ClientLog.Write("Reloading mods...", RebirthLogLevel.Info);
        else
            AreCompilesAllowed = CheckIfCompilesAreAllowed();

        Directory.CreateDirectory(ModsPath);

        LoadDisabledMods();
        modNames.Clear();

        var folders = Directory.GetDirectories(ModsPath);

        if (folders.Length == 0) {
            _firstLoad = true;
            Status = LoadStatus.Complete;
            return;
        }

        IsLoadingMods = true;
        Error = string.Empty;
        _loadFailed = false;

        // largely refactored from the old one

        foreach (var modFolder in folders) {
            var modName = new DirectoryInfo(modFolder).Name;
            var proj = FindModProject(modFolder);

            // every folder is listed in the mods menu, even ones that can't be loaded
            modNames.Add(modName);

            // there's no csproj to run dotnet build on, skip (aka: not a mod)
            if (proj is null)
                continue;

            // the project decides the name of the dll, so it can differ from the folder's name
            var projectName = Path.GetFileNameWithoutExtension(proj);

            // check .NET compatibility
            var lines = File.ReadAllLines(proj);
            var netVer = GetCsprojPropertyValue(lines, LocateCsprojProperty(lines, "TargetFramework"));

            // if incompatible, don't compile
            if (netVer != EXPECTED_NET_VERSION) {
                Error = $"{modName} does not match TanksRebirth's .NET version ({EXPECTED_NET_VERSION})";
                TankGame.ClientLog.Write(Error, RebirthLogLevel.Warn);
                // nothing has been loaded yet, so there is nothing to unload
                _loadingActions.Clear();
                IsLoadingMods = false;
                Status = LoadStatus.Complete;
                return;
            }

            if (!IsModEnabled(modName)) {
                TankGame.ClientLog.Write($"Skipping mod {modName}", RebirthLogLevel.Info);
                continue;
            }

            // this mod has two actions max: compilation and load or just load
            ActionsNeeded += AreCompilesAllowed ? 2 : 1;

            _loadingActions.Add(() => {
                try {
                    ModBeingLoaded = modName;

                    if (!AttemptCompile(modName)) {
                        _loadFailed = true;
                        return;
                    }

                    Status = LoadStatus.Loading;
                    string filepath = Path.Combine(modFolder, "bin", LoadType, EXPECTED_NET_VERSION, $"{projectName}.dll");
                    string pdb = Path.ChangeExtension(filepath, ".pdb");

                    var alc = new AssemblyLoadContext(modName, isCollectible: true);

                    // loads mod-specific dependencies into *this* ALC
                    var dirPath = Path.Combine(modFolder, "modrefs");
                    if (Directory.Exists(dirPath)) {
                        foreach (var dllPath in Directory.GetFiles(dirPath, "*.dll")) {
                            alc.LoadFromAssemblyPath(dllPath);
                        }
                    }

                    // now load the mod itself into the same ALC
                    using var mainDll = File.OpenRead(filepath);
                    using var mainPdb = File.Exists(pdb) ? File.OpenRead(pdb) : null;

                    if (mainPdb is not null)
                        alc.LoadFromStream(mainDll, mainPdb);
                    else
                        alc.LoadFromStream(mainDll);

                    _loadedAlcs.Add(alc);

                    var assembly = alc.Assemblies.First(x => x.GetName().Name == projectName);
                    var types = assembly.GetTypes();
                    var tanksModTypes = types.Where(t => t.IsSubclassOf(typeof(TanksMod)) && !t.IsAbstract).ToArray();

                    TanksMod mod;

                    if (tanksModTypes.Length != 1) {
                        if (tanksModTypes.Length > 1)
                            throw new ModLoadException($"Too many classes inherit from {nameof(TanksMod)}! Only one is allowed per-mod.");
                        else
                            throw new ModLoadException($"No classes that inherit from {nameof(TanksMod)}, no entrypoint to use.");
                    }
                    // initialize what needs to be initialized (DAMN THATS A BAR)
                    else {
                        mod = (Activator.CreateInstance(tanksModTypes[0]) as TanksMod)!;
                        mod.InternalName = modName;
                        mod.Data.LoadDirectory = modFolder;
                        mod.Data.assemblyContainer = alc;
                        mod.Data.LoadDirectory = filepath;
                        mod.Data.Dependencies = alc.Assemblies.Select(x => x.FullName!).ToArray();
                        mod.Data.Assemblies = alc.Assemblies;

                        var modInfoPath = Path.Combine(modFolder, "mod_info.json");

                        SetupMod(mod, modInfoPath);

                        LoadModContent(mod, types);

                        FirstLoadMods.Add(mod.InternalName);

                        LoadedMods.Add(mod);
                        mod.OnLoad();
                        OnPostModLoad?.Invoke(mod);
                    }
                    ActionsComplete++;
                    TankGame.ClientLog.Write($"Loaded mod '{assembly.GetName().Name}', version '{assembly.GetName().Version}'", RebirthLogLevel.Info);
                } catch (Exception e) {
                    TankGame.ReportError(e, true, true);
                    Error = e.Message;
                    _loadFailed = true;
                }
            });
        }
        Task.Run(() => {
            foreach (var action in _loadingActions) {
                action();

                // don't bother with the rest, they're all coming down anyway
                if (_loadFailed) break;
            }

            if (_loadFailed) {
                TankGame.ClientLog.Write($"A mod failed to compile or load ({Error}). Unloading every mod...", RebirthLogLevel.Warn);
                UnloadAll();
            }

            IsLoadingMods = false;
            ModBeingLoaded = string.Empty;
            Status = LoadStatus.Complete;

            TankGame.ClientLog.Write(_firstLoad ? $"Loaded {LoadedMods.Count} mod(s)." : $"Reloaded {LoadedMods.Count} mod(s).", RebirthLogLevel.Info);
            _firstLoad = false;

            ModTanks = [.. _modTanks];
            ModBlocks = [.. _modBlocks];
            ModShells = [.. _modShells];
            ModModifiers = [.. _modModifiers];

            OnFinishModLoading?.Invoke();
        });
        /*IsLoadingMods = false;
        ModBeingLoaded = string.Empty;
        Status = LoadStatus.Complete;
        TankGame.ClientLog.Write(_firstLoad ? $"Loaded {LoadedMods.Count} mod(s)." : $"Reloaded {LoadedMods.Count} mod(s).", RebirthLogLevel.Info);
        _firstLoad = false;

        ModTanks = [.. _modTanks];
        ModBlocks = [.. _modBlocks];
        ModShells = [.. _modShells];*/

    }
    internal static void SetupMod(TanksMod mod, string modInfoPath) {
        mod.Data.Tanks = [];
        mod.Data.Blocks = [];
        mod.Data.Shells = [];

        // trycatch is now avoided typically
        if (!File.Exists(modInfoPath)) {
            TankGame.ClientLog.Write($"mod_info.json missing for '{mod.InternalName}', creating defaults.", RebirthLogLevel.Info);
            File.WriteAllText(modInfoPath, JsonSerializer.Serialize<ModInfo>(default, _indented));
            mod.ModInfo = new();
            return;
        }

        try {
            var modInfoJson = File.ReadAllText(modInfoPath);
            mod.ModInfo = JsonSerializer.Deserialize<ModInfo>(modInfoJson);
        } catch (JsonException ex) {
            TankGame.ClientLog.Write($"Invalid JSON in '{mod.InternalName}': {ex.Message}.", RebirthLogLevel.Warn);
            mod.ModInfo = new();
        }
    }
    internal static void LoadModContent(TanksMod mod, Type[] types) {
        foreach (var type in types) {
            // now lets scan the mod's content

            var isModTank = type.IsSubclassOf(typeof(ModTank)) && !type.IsAbstract;
            var isModBlock = type.IsSubclassOf(typeof(ModBlock)) && !type.IsAbstract;
            var isModShell = type.IsSubclassOf(typeof(ModShell)) && !type.IsAbstract;
            var isModModifier = type.IsSubclassOf(typeof(ModModifier)) && !type.IsAbstract;

            if (isModTank) {
                LoadModTank(mod, type);
            }
            else if (isModBlock) {
                LoadModBlock(mod, type);
            }
            else if (isModShell) {
                LoadModShell(mod, type);
            }
            else if (isModModifier) {
                LoadModModifier(mod, type);
            }
        }
    }
    public static void LoadModTank(TanksMod mod, Type type) {
        var modContent = (Activator.CreateInstance(type) as ModTank)!;
        var modType = modContent.GetType();
        modContent.ContentKey = $"{type.Assembly.GetName().Name}.{modType.Name}";
        var @namespace = string.Join(Path.DirectorySeparatorChar, modType.FullName!.Split('.')[1..]);

        _loadingContent = modContent;

        mod.Data.Tanks.Add(modContent);
        _modTanks.Add(modContent);
        modContent.Mod = mod;

        // load each tank and its data, add to moddedTypes the singleton of the ModTank.
        ModRegistry.singletonMap.Add(type, modContent);

        modContent.Name ??= new();
        modContent.Texture ??= @namespace;

        // doesn't insert anything if there is already something for English
        modContent.Name.TryAdd(LangCode.English, modContent.ContentKey);
        DifficultyAlgorithm.TankDiffs[modContent.Type] = 0f;

        modContent!.Register();
        TankGame.ClientLog.Write($"Loaded modded tank '{modContent.ContentKey}'", RebirthLogLevel.Info);
    }
    public static void LoadModBlock(TanksMod mod, Type type) {
        var modContent = (Activator.CreateInstance(type) as ModBlock)!;
        var modType = modContent.GetType();
        modContent.ContentKey = $"{type.Assembly.GetName().Name}.{modType.Name}";
        var @namespace = string.Join(Path.DirectorySeparatorChar, modType.FullName!.Split('.')[1..]);

        _loadingContent = modContent;

        mod.Data.Blocks.Add(modContent);
        _modBlocks.Add(modContent);
        modContent!.Mod = mod;

        modContent.Name ??= new();
        modContent.Texture ??= @namespace;

        // again, but with modlbocks
        ModRegistry.singletonMap.Add(type, modContent);

        modContent.Name.TryAdd(LangCode.English, modContent.ContentKey);
        modContent.Register();
        TankGame.ClientLog.Write($"Loaded modded block '{modContent.ContentKey}'", RebirthLogLevel.Info);
    }
    public static void LoadModShell(TanksMod mod, Type type) {
        var modContent = (Activator.CreateInstance(type) as ModShell)!;
        var modType = modContent.GetType();
        modContent.ContentKey = $"{type.Assembly.GetName().Name}.{modType.Name}";
        var @namespace = string.Join(Path.DirectorySeparatorChar, modType.FullName!.Split('.')[1..]);

        _loadingContent = modContent;

        mod.Data.Shells.Add(modContent);
        _modShells.Add(modContent);
        modContent!.Mod = mod;

        modContent.Name ??= new();
        modContent.Texture ??= @namespace;

        // again, but with modshels
        ModRegistry.singletonMap.Add(type, modContent);

        modContent.Name.TryAdd(LangCode.English, modContent.ContentKey);
        TankGame.MainThreadTasks.Enqueue(modContent.Register);
        TankGame.ClientLog.Write($"Loaded modded shell '{modContent.ContentKey}'", RebirthLogLevel.Info);
    }
    public static void LoadModModifier(TanksMod mod, Type type) {
        var modModifier = (Activator.CreateInstance(type) as ModModifier)!;
        modModifier.ContentKey = modModifier.GetType().Name;
        modModifier.Mod = mod;

        _loadingContent = modModifier;
        _modModifiers.Add(modModifier);

        ModRegistry.singletonMap.Add(type, modModifier);

        // the modifiers menu reads the registry every frame, so register on the main thread
        TankGame.MainThreadTasks.Enqueue(modModifier.Register);
        TankGame.ClientLog.Write($"Loaded modded modifier '{modModifier.Key}'", RebirthLogLevel.Info);
    }
    public static int LocateCsprojProperty(string[] contents, string match) {
        return Array.FindIndex(contents, x => {
            var trim = x.Trim().Split('>')[0].Replace("<", "");
            return trim == match;
        });
    }
    public static string GetCsprojPropertyValue(string[] contents, int line) {
        var str = contents[line];
        var property = str.Split('>')[1].Split('<')[0];
        return property;
    }

    // matches the modifiers window: wooden frame, white pill title, grey section card and dark info bar
    public static void DrawModLoading() {
        var renderer = TankGame.SpriteRenderer;
        var font = FontGlobals.RebirthFont;
        var hasError = Error != string.Empty;

        // dims the background to make the loading ui more apparent
        ModifierDraw.Fill(renderer, new Rectangle(0, 0, WindowUtils.WindowWidth, WindowUtils.WindowHeight), Color.Black * 0.6f);

        var panelSize = new Vector2(760, 384).ToResolution();
        var panel = new Rectangle(
            (int)(WindowUtils.WindowWidth / 2f - panelSize.X / 2f),
            (int)(WindowUtils.WindowHeight / 2f - panelSize.Y / 2f),
            (int)panelSize.X,
            (int)panelSize.Y
        );

        // positions inside the panel are in the same 1920x1080 space the modifiers window uses
        Rectangle Area(float x, float y, float width, float height) {
            var position = new Vector2(x, y).ToResolution();
            var size = new Vector2(width, height).ToResolution();
            return new Rectangle(panel.X + (int)position.X, panel.Y + (int)position.Y, (int)size.X, (int)size.Y);
        }

        ModifierDraw.Shadow(renderer, panel, 8f, 14f, 0.5f);
        ModifierDraw.Frame(renderer, panel);

        // title pill, and how many mods are done
        var title = "Loading Mods";
        var titleScale = ModifierColors.Scale(0.95f);
        var titleSize = font.MeasureString(title);
        var pill = Area(28, 24, 0, 54);
        pill.Width = (int)(titleSize.X * titleScale.X + 56f.ToResolutionX());
        ModifierDraw.PillButton(renderer, pill, false, 1f);
        renderer.DrawString(font, title, pill.Center.ToVector2(), ModifierColors.Ink, titleScale, 0f, titleSize / 2f);

        var divisor = AreCompilesAllowed ? 2 : 1;
        DrawUtils.DrawStringWithBorder(renderer, font, $"{ActionsComplete / divisor} / {ActionsNeeded / divisor} mods",
            new Vector2(panel.Right - 28f.ToResolutionX(), pill.Center.Y), Color.White, Color.Black, ModifierColors.Scale(0.75f), 0f, Anchor.RightCenter, 1f);

        // section card: the mod being loaded on the left, what is happening to it on the right
        var section = Area(28, 96, 704, 190);
        var header = section with { Height = (int)48f.ToResolutionY() };
        ModifierDraw.Section(renderer, header, section);

        var pulse = MathF.Sin(RuntimeData.RunTime / 20f) * 0.1f + 0.9f;
        var headerText = ModBeingLoaded != string.Empty ? ModBeingLoaded : Status.ToString();
        var headerScale = ModifierColors.Scale(0.85f);
        var headerSize = font.MeasureString(headerText);
        var statusText = hasError ? "Error" : Status.ToString();
        var statusScale = ModifierColors.Scale(0.6f);
        var statusSize = font.MeasureString(statusText);
        var statusRight = header.Right - 16f.ToResolutionX();

        // shrink a long mod name instead of running it into the status
        var headerRoom = statusRight - statusSize.X * statusScale.X - header.X - 32f.ToResolutionX();
        headerScale *= MathF.Min(1f, headerRoom / MathF.Max(1f, headerSize.X * headerScale.X));

        renderer.DrawString(font, headerText, new Vector2(header.X + 16f.ToResolutionX(), header.Center.Y), ModifierColors.Ink, headerScale, 0f,
            new Vector2(0f, headerSize.Y / 2f));
        renderer.DrawString(font, statusText, new Vector2(statusRight, header.Center.Y), hasError ? Color.Firebrick : ModifierDraw.Fade(ModifierColors.OnDark, pulse),
            statusScale, 0f, new Vector2(statusSize.X, statusSize.Y / 2f));

        // what is being loaded right now
        var contentRow = Area(28, 148, 704, 40);
        var content = _loadingContent != null ? _loadingContent.ContentKey : "Preparing to load mods...";
        var contentScale = ModifierColors.Scale(0.6f);
        var contentSize = font.MeasureString(content);
        contentScale *= MathF.Min(1f, (section.Width - 40f.ToResolutionX()) / MathF.Max(1f, contentSize.X * contentScale.X));
        DrawUtils.DrawStringWithBorder(renderer, font, content, contentRow.Center.ToVector2(), Color.White, Color.Black, contentScale, 0f, Anchor.Center, 1f);

        // progress bar, drawn like the toggles in the modifiers window
        var track = Area(60, 200, 640, 30);
        var edge = ModifierDraw.Px(2f);
        var outline = track;
        outline.Inflate(edge, edge);
        ModifierDraw.Fill(renderer, outline, Color.Black);
        ModifierDraw.Fill(renderer, track, new Color(34, 34, 34));

        float ratio = ActionsNeeded == 0 ? 0 : (float)ActionsComplete / ActionsNeeded;
        var fill = track with { Width = (int)(track.Width * ratio) };
        if (fill.Width > 0) {
            ModifierDraw.Gradient(renderer, fill, hasError ? new Color(214, 64, 64) : ModifierColors.On, hasError ? new Color(140, 30, 30) : ModifierColors.OnDark);
            // ModifierDraw.Fill(renderer, fill with { Height = fill.Height / 2 }, ModifierDraw.Fade(Color.White, 0.22f));
        }

        var percentRow = Area(28, 238, 704, 40);
        DrawUtils.DrawStringWithBorder(renderer, font, $"{ratio * 100:0}%", percentRow.Center.ToVector2(), Color.White, Color.Black,
            ModifierColors.Scale(0.65f), 0f, Anchor.Center, 1f);

        // info bar along the bottom
        var footer = Area(28, 302, 704, 58);
        ModifierDraw.DarkBar(renderer, footer);

        var message = hasError ? $"ERROR: {Error}"
            : AreCompilesAllowed ? "Mods are being compiled."
            : "Mods are loading.";
        var messageScale = ModifierColors.Scale(0.6f);
        var messageSize = font.MeasureString(message);
        messageScale *= MathF.Min(1f, (footer.Width - 48f.ToResolutionX()) / MathF.Max(1f, messageSize.X * messageScale.X));
        renderer.DrawString(font, message, footer.Center.ToVector2(), hasError ? new Color(255, 96, 96) : Color.White, messageScale, 0f, messageSize / 2f);
    }
}
