using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;
using TanksRebirth.GameContent.ID;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.RebirthUtils;
using TanksRebirth.GameContent.Systems;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common.Framework.Collections;
using TanksRebirth.Localization;
using TanksRebirth.Internals.Common.Utilities;
using FontStashSharp;
using TanksRebirth.GameContent.UI.MainMenu;
using System.Text.Json;

namespace TanksRebirth.GameContent.ModSupport;

// fuck IDE0044
#pragma warning disable CA2211, IDE0044
public enum LoadStatus
{
    Inactive,
    Unloading,
    Loading,
    Compiling,
    Complete
}
public static class ModLoader {
    public delegate void FinishModLoading();
    public static event FinishModLoading? OnFinishModLoading;
    public delegate void PostLoadModContent(TanksMod mod);
    public static event PostLoadModContent? OnPostModLoad;

    static JsonSerializerOptions _indented = new() { WriteIndented = true };

    public static List<TanksMod> LoadedMods { get; set; } = [];
    static List<AssemblyLoadContext> _loadedAlcs = [];

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
    /// <summary>The name of the mod being loaded.</summary>
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
    public static Dictionary<string, bool> ModsEnabled = [];
    /// <summary>A mod-agnostic list of modded tanks.</summary>
    public static ModTank[] ModTanks { get; private set; } = [];
    static List<ModTank> _modTanks = [];
    /// <summary>A mod-agnostic list of modded blocks.</summary>
    public static ModBlock[] ModBlocks { get; private set; } = [];
    static List<ModBlock> _modBlocks = [];
    /// <summary>A mod-agnostic list of modded shells.</summary>
    public static ModShell[] ModShells { get; private set; } = [];
    static List<ModShell> _modShells = [];

    static bool _firstLoad = true;
    /// <summary>The error given from the mod-loading process.</summary>
    public static string Error = string.Empty;
    /// <summary>What kind of compilation is performed- Debug or Release.</summary>
    public static string LoadType = string.Empty;
    public static bool CheckIfCompilesAreAllowed() {
        if (!RuntimeData.IsWindows) {
            TankGame.ClientLog.Write("Auto-compilation disallowed. Current OS is not Windows.", LogType.Warn);
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
                TankGame.ClientLog.Write($"Auto-compilation disallowed. User does not have a .NET {MIN_NET_VERSION}.0 SDK or higher installed.", LogType.Warn);
                return false;
            }

            TankGame.ClientLog.Write($"Auto-compile allowed. (.NET {versionsReal[^1]})", LogType.Info);
            return true;
        }
        catch(Exception e) {
            var trace = new StackTrace(e, true);
            var frame = trace.GetFrame(0);
            int line = frame?.GetFileLineNumber() ?? -1;

            TankGame.ClientLog.Write($"Auto-compile eligibility failed. (reason={e.Message}, where={line})", LogType.Info);

            return false;
        }
    }
    static void AttemptCompile(string modName) {
        if (!AreCompilesAllowed) return;

        Status = LoadStatus.Compiling;
        Process proc = new();
        try {
            ProcessStartInfo startInfo = new() {
                UseShellExecute = false,

                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = @"C:\Windows\system32\cmd.exe",
                WorkingDirectory = Path.Combine(ModsPath, modName),
                Arguments = $"/c dotnet build -c " + LoadType,
                RedirectStandardOutput = true,
            };

            proc.StartInfo = startInfo;
            proc.Start();

            var lines = proc.StandardOutput
                .ReadToEnd()
                .Replace("\n", "")
                .Split('\r')
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrEmpty(x))
                .ToArray();

            // find a build failure

            var idx = Array.FindIndex(lines, l => l.Contains("build failed", StringComparison.CurrentCultureIgnoreCase));
            if (idx > -1) {
                var reasonP = Environment.NewLine + lines[idx - 1];
                var reason = reasonP.Remove(Array.FindIndex(reasonP.ToArray(), x => x == '['));
                Error = reason;
                TankGame.ReportError(new Exception(reason));
            }

            proc.WaitForExit();
        } catch (Exception e) {
            Error = e.Message;
            proc.Dispose();
            proc.Close();
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

        ChatSystem.SendMessage("Unloading mods...", Color.Yellow);
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

            mod.OnUnload();
            UnloadModContent(mod);
        });
        LoadedMods.Clear();
        // for when the unloading process is done.
        ModSingletons.moddedTypes.Clear();
        _loadedAlcs.Clear();
        ResetContentDictionaries();
        ModTank.unloadOffset = 0;
        ModBlock.unloadOffset = 0;
        ModShell.unloadOffset = 0;
        ChatSystem.SendMessage("Mod unload successful!", Color.Lime);
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
                            TankGame.ClientLog.Write($"Mod {mod.InternalName} forgot to unsubscribe from event {ev.DeclaringType.Name}.{ev.Name}. Unsubscribing...", LogType.Info);
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
        if (Status == LoadStatus.Unloading)
            ChatSystem.SendMessage("Mods are currently unloading! Unable to load mods.", Color.Red);
        if (Status == LoadStatus.Loading || Status == LoadStatus.Compiling)
            ChatSystem.SendMessage("Mods are currently loading! Unable to load mods.", Color.Red);
        if (LoadedMods.Count > 0)
            UnloadAll();

        LoadType = Debugger.IsAttached ? "Debug" : "Release";

        ActionsNeeded = 0;
        ActionsComplete = 0;

        if (!_firstLoad)
            ChatSystem.SendMessage("Reloading mods...", Color.Red);
        else
            AreCompilesAllowed = CheckIfCompilesAreAllowed();

        Directory.CreateDirectory(ModsPath);

        var folders = Directory.GetDirectories(ModsPath);

        if (folders.Length == 0) {
            _firstLoad = true;
            Status = LoadStatus.Complete;
            ChatSystem.SendMessage(_firstLoad ? $"Loaded {LoadedMods.Count} mod(s)." : $"Reloaded {LoadedMods.Count} mod(s).", Color.Lime);
            return;
        }

        IsLoadingMods = true;
        foreach (var folder in folders) {
            var files = Directory.GetFiles(folder);

            foreach (var modFile in files) {
                bool isProj = modFile.EndsWith(".csproj");

                if (!isProj) continue;

                var fileName = Path.GetFileName(modFile);

                var modName = folder.Split('\\')[^1];

                // skip mods that should not be loaded
                if (ModsEnabled.TryGetValue(modName, out bool value))
                    if (!value) continue;

                if (fileName != modName + ".csproj") continue;
                ActionsNeeded++;

                var lines = File.ReadAllLines(modFile);
                var netVer = GetCsprojPropertyValue(lines, LocateCsprojProperty(lines, "TargetFramework"));

                if (netVer != EXPECTED_NET_VERSION) {
                    Error = $"This mod does not match TanksRebirth's .NET version ({EXPECTED_NET_VERSION})";
                    return;
                }

                _loadingActions.Add(() => {
                    try {
                        ModBeingLoaded = modName;

                        AttemptCompile(modName);

                        Status = LoadStatus.Loading;
                        string filepath = Path.Combine(folder, "bin", LoadType, EXPECTED_NET_VERSION, $"{modName}.dll");
                        string pdb = Path.ChangeExtension(filepath, ".pdb");

                        var alc = new AssemblyLoadContext(modName, isCollectible: true);
                        // alc.LoadFromStream(File.Open(filepath, FileMode.Open), File.Open(pdb, FileMode.Open));

                        // Load mod-specific dependencies into *this* ALC
                        var dirPath = Path.Combine(folder, "modrefs");
                        if (Directory.Exists(dirPath)) {
                            foreach (var dllPath in Directory.GetFiles(dirPath, "*.dll")) {
                                alc.LoadFromAssemblyPath(dllPath);
                            }
                        }

                        // Now load the mod itself into the same ALC
                        using var mainDll = File.OpenRead(filepath);
                        using var mainPdb = File.Exists(pdb) ? File.OpenRead(pdb) : null;

                        if (mainPdb is not null)
                            alc.LoadFromStream(mainDll, mainPdb);
                        else
                            alc.LoadFromStream(mainDll);

                        _loadedAlcs.Add(alc);

                        var assembly = alc.Assemblies.First(x => x.GetName().Name == modName);
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
                            mod.Data.LoadDirectory = folder;
                            mod.Data.assemblyContainer = alc;
                            mod.Data.LoadDirectory = filepath;
                            mod.Data.Dependencies = alc.Assemblies.Select(x => x.FullName!).ToArray();
                            mod.Data.Assemblies = alc.Assemblies;

                            var modInfoPath = Path.Combine(folder, "mod_info.json");

                            SetupMod(mod, modInfoPath);

                            LoadModContent(mod, types);

                            FirstLoadMods.Add(mod.InternalName);
                            ModsEnabled.TryAdd(mod.InternalName, true);

                            LoadedMods.Add(mod);
                            mod.OnLoad();
                            OnPostModLoad?.Invoke(mod);
                        }
                        ActionsComplete++;
                        TankGame.ClientLog.Write($"Loaded mod assembly '{assembly.GetName().Name}', version '{assembly.GetName().Version}'", LogType.Info);
                    } catch (Exception e) {
                        TankGame.ReportError(e, true, true);
                        Error = e.Message;
                        return;
                    }
                });
            }
        }
        Task.Run(() => {
            _loadingActions.ForEach(x => x.Invoke());

            IsLoadingMods = false;
            ModBeingLoaded = string.Empty;
            Status = LoadStatus.Complete;

            ChatSystem.SendMessage(_firstLoad ? $"Loaded {LoadedMods.Count} mod(s)." : $"Reloaded {LoadedMods.Count} mod(s).", Color.Lime);
            _firstLoad = false;

            ModTanks = [.. _modTanks];
            ModBlocks = [.. _modBlocks];
            ModShells = [.. _modShells];

            OnFinishModLoading?.Invoke();
        });

    }
    internal static void SetupMod(TanksMod tanksMod, string modInfoPath) {
        if (File.Exists(modInfoPath)) {
            try {
                var modInfoJson = File.ReadAllText(modInfoPath);
                tanksMod.ModInfo = JsonSerializer.Deserialize<ModInfo>(modInfoJson);
            } catch (Exception ex) {
                TankGame.ClientLog.Write($"Bad data in mod_info.json for mod '{tanksMod.InternalName}': {ex.Message}.", LogType.Warn);

                tanksMod.ModInfo = new();
            }
        }
        else {
            TankGame.ClientLog.Write($"mod_info.json not found for mod '{tanksMod.InternalName}', using defaults.", LogType.Info);

            // create a default one
            File.WriteAllText(modInfoPath, JsonSerializer.Serialize<ModInfo>(default, _indented));
            tanksMod.ModInfo = new();
        }
        tanksMod.Data.Tanks = [];
        tanksMod.Data.Blocks = [];
        tanksMod.Data.Shells = [];
    }
    internal static void LoadModContent(TanksMod mod, Type[] types) {
        foreach (var type in types) {
            // now lets scan the mod's content

            var isModTank = type.IsSubclassOf(typeof(ModTank)) && !type.IsAbstract;
            var isModBlock = type.IsSubclassOf(typeof(ModBlock)) && !type.IsAbstract;
            var isModShell = type.IsSubclassOf(typeof(ModShell)) && !type.IsAbstract;

            if (isModTank) {
                var modTank = (Activator.CreateInstance(type) as ModTank)!;
                mod.Data.Tanks.Add(modTank);
                _modTanks.Add(modTank);
                modTank!.Mod = mod;

                // load each tank and its data, add to moddedTypes the singleton of the ModTank.
                ModSingletons.moddedTypes.Add(modTank);

                var tankName = modTank.GetType().Name;

                modTank.Name ??= new([]);
                modTank.Texture ??= tankName;

                // doesn't insert anything if there is already something for English
                modTank.Name.AddLocalization(LangCode.English, $"{mod.InternalName}.{tankName}");
                DifficultyAlgorithm.TankDiffs[modTank.Type] = 0f;
                modTank!.Load();
                TankGame.ClientLog.Write($"Loaded modded tank '{modTank.Name.GetLocalizedString(LangCode.English)}'", LogType.Info);
            }
            else if (isModBlock) {
                var modBlock = (Activator.CreateInstance(type) as ModBlock)!;
                mod.Data.Blocks.Add(modBlock);
                _modBlocks.Add(modBlock);
                modBlock!.Mod = mod;

                // again, but with modlbocks
                ModSingletons.moddedTypes.Add(modBlock);
                modBlock.Name.AddLocalization(LangCode.English, $"{mod.InternalName}.{modBlock.GetType().Name}");
                modBlock.Register();
                TankGame.ClientLog.Write($"Loaded modded block '{modBlock.Name.GetLocalizedString(LangCode.English)}'", LogType.Info);
            }
            else if (isModShell) {
                var modShell = (Activator.CreateInstance(type) as ModShell)!;
                mod.Data.Shells.Add(modShell);
                _modShells.Add(modShell);
                modShell!.Mod = mod;

                // again, but with modshels
                ModSingletons.moddedTypes.Add(modShell);
                modShell.Name.AddLocalization(LangCode.English, $"{mod.InternalName}.{modShell.GetType().Name}");
                modShell.Register();
                TankGame.ClientLog.Write($"Loaded modded shell '{modShell.Name.GetLocalizedString(LangCode.English)}'", LogType.Info);
            }
        }
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

    // rendering
    // refactor pl0x
    public static void DrawModLoading() {
        var alpha = 0.7f;
        var width = WindowUtils.WindowWidth / 3;
        TankGame.SpriteRenderer.Draw(TextureGlobals.Pixels[Color.White], new Vector2(WindowUtils.WindowWidth / 2, WindowUtils.WindowHeight / 2), null, Color.SkyBlue * alpha, 0f, GameUtils.GetAnchor(Anchor.Center, TextureGlobals.Pixels[Color.White].Size()), new Vector2(width, 200.ToResolutionY()), default, 0f);

        var barDims = new Vector2(width - 120, 20).ToResolution();

        TankGame.SpriteRenderer.Draw(TextureGlobals.Pixels[Color.White], new Vector2(WindowUtils.WindowWidth / 2, WindowUtils.WindowHeight / 2), null, Color.Goldenrod * alpha, 0f, GameUtils.GetAnchor(Anchor.Center, TextureGlobals.Pixels[Color.White].Size()),
            barDims, default, 0f);
        var ratio = (float)ActionsComplete / ActionsNeeded;
        if (ActionsNeeded == 0)
            ratio = 0;
        TankGame.SpriteRenderer.Draw(TextureGlobals.Pixels[Color.White], new Vector2(WindowUtils.WindowWidth / 2, WindowUtils.WindowHeight / 2), null, Color.Yellow * alpha, 0f, GameUtils.GetAnchor(Anchor.Center, TextureGlobals.Pixels[Color.White].Size()),
            barDims * new Vector2(ratio, 1f).ToResolution(), default, 0f);

        var txt = $"{Status} {ModBeingLoaded}...";
        TankGame.SpriteRenderer.DrawString(FontGlobals.RebirthFont, txt, new(WindowUtils.WindowWidth / 2, WindowUtils.WindowHeight / 2 - 75.ToResolutionY()), Color.White, Vector2.One.ToResolution(), 0f, GameUtils.GetAnchor(Anchor.Center, FontGlobals.RebirthFont.MeasureString(txt)));

        txt = Error == string.Empty ? 
            $"Loading mods... {ratio * 100:0}% ({ActionsComplete + 1} / {ActionsNeeded})" :
            $"Error Loading '{ModBeingLoaded}' ({Error})";

        TankGame.SpriteRenderer.DrawString(FontGlobals.RebirthFont, txt, new(WindowUtils.WindowWidth / 2, WindowUtils.WindowHeight / 2 - 150.ToResolutionY()), Color.White, Vector2.One.ToResolution(), 0f, GameUtils.GetAnchor(Anchor.Center, FontGlobals.RebirthFont.MeasureString(txt)));
    }
}
