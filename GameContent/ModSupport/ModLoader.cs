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

    static IModContent _loadingContent;

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

    internal static List<string> modNames = [];
    internal static List<bool> modEnablement = [];
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
            }

            proc.WaitForExit();

            ActionsComplete++;
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

        TankGame.ClientLog.Write("Unloading mods...", LogType.Info);
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
        ModSingletonRegistry._singletonMap.Clear();
        _loadedAlcs.Clear();
        ResetContentDictionaries();
        ModTank.unloadOffset = 0;
        ModBlock.unloadOffset = 0;
        ModShell.unloadOffset = 0;
        TankGame.ClientLog.Write("Mod unload successful!", LogType.Info);
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
        if (Status == LoadStatus.Unloading) {
            TankGame.ClientLog.Write("Mods are currently unloading! Unable to load mods.", LogType.Warn);
            return;
        }
        if (IsLoadingMods) {
            TankGame.ClientLog.Write("Mods are currently loading! Unable to load mods.", LogType.Warn);
            return;
        }

        if (LoadedMods.Count > 0) {
            UnloadAll();
        }
        else {
            TankGame.ClientLog.Write("No mods to unload. Skipping...", LogType.Warn);
        }

            LoadType = Debugger.IsAttached ? "Debug" : "Release";

        ActionsNeeded = 0;
        ActionsComplete = 0;

        if (!_firstLoad)
            TankGame.ClientLog.Write("Reloading mods...", LogType.Info);
        else
            AreCompilesAllowed = CheckIfCompilesAreAllowed();

        Directory.CreateDirectory(ModsPath);

        var folders = Directory.GetDirectories(ModsPath);

        if (folders.Length == 0) {
            _firstLoad = true;
            Status = LoadStatus.Complete;
            return;
        }

        IsLoadingMods = true;
        Error = string.Empty;

        // largely refactored from the old one

        // thing that prevents accessing outside of array bounds..es?
        int dummyLittleIThing = 0;
        for (int i = 0; i < folders.Length; i++) {

            var modFolder = folders[i];
            var modName = new DirectoryInfo(modFolder).Name;
            var proj = Path.Combine(modFolder, modName + ".csproj");

            // there's no csproj to run dotnet build on, skip (aka: not a mod)
            if (!File.Exists(proj)) {
                dummyLittleIThing--;
                continue;
            }

            // check .NET compatibility
            var lines = File.ReadAllLines(proj);
            var netVer = GetCsprojPropertyValue(lines, LocateCsprojProperty(lines, "TargetFramework"));

            // if incompatible, don't compile
            if (netVer != EXPECTED_NET_VERSION) {
                Error = $"This mod does not match TanksRebirth's .NET version ({EXPECTED_NET_VERSION})";
                return;
            }

            if (!modNames.Contains(modName)) {
                modNames.Add(modName);
                modEnablement.Add(true);
            }

            if (!modEnablement[i + dummyLittleIThing]) {
                TankGame.ClientLog.Write($"Skipping mod {modName}", LogType.Info);
                continue;
            }

            // this mod has two actions max: compilation and load or just load
            ActionsNeeded += AreCompilesAllowed ? 2 : 1;

            _loadingActions.Add(() => {
                try {
                    ModBeingLoaded = modName;

                    AttemptCompile(modName);

                    Status = LoadStatus.Loading;
                    string filepath = Path.Combine(modFolder, "bin", LoadType, EXPECTED_NET_VERSION, $"{modName}.dll");
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
                    TankGame.ClientLog.Write($"Loaded mod '{assembly.GetName().Name}', version '{assembly.GetName().Version}'", LogType.Info);
                } catch (Exception e) {
                    TankGame.ReportError(e, true, true);
                    Error = e.Message;
                    return;
                }
            });
        }
        Task.Run(() => {
            _loadingActions.ForEach(x => x());

            IsLoadingMods = false;
            ModBeingLoaded = string.Empty;
            Status = LoadStatus.Complete;

            TankGame.ClientLog.Write(_firstLoad ? $"Loaded {LoadedMods.Count} mod(s)." : $"Reloaded {LoadedMods.Count} mod(s).", LogType.Info);
            _firstLoad = false;

            ModTanks = [.. _modTanks];
            ModBlocks = [.. _modBlocks];
            ModShells = [.. _modShells];

            OnFinishModLoading?.Invoke();
        });
        /*IsLoadingMods = false;
        ModBeingLoaded = string.Empty;
        Status = LoadStatus.Complete;
        TankGame.ClientLog.Write(_firstLoad ? $"Loaded {LoadedMods.Count} mod(s)." : $"Reloaded {LoadedMods.Count} mod(s).", LogType.Info);
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
            TankGame.ClientLog.Write($"mod_info.json missing for '{mod.InternalName}', creating defaults.", LogType.Info);
            File.WriteAllText(modInfoPath, JsonSerializer.Serialize<ModInfo>(default, _indented));
            mod.ModInfo = new();
            return;
        }

        try {
            var modInfoJson = File.ReadAllText(modInfoPath);
            mod.ModInfo = JsonSerializer.Deserialize<ModInfo>(modInfoJson);
        } catch (JsonException ex) {
            TankGame.ClientLog.Write($"Invalid JSON in '{mod.InternalName}': {ex.Message}.", LogType.Warn);
            mod.ModInfo = new();
        }
    }
    internal static void LoadModContent(TanksMod mod, Type[] types) {
        foreach (var type in types) {
            // now lets scan the mod's content

            var isModTank = type.IsSubclassOf(typeof(ModTank)) && !type.IsAbstract;
            var isModBlock = type.IsSubclassOf(typeof(ModBlock)) && !type.IsAbstract;
            var isModShell = type.IsSubclassOf(typeof(ModShell)) && !type.IsAbstract;

            if (isModTank) {
                LoadModTank(mod, type);
            }
            else if (isModBlock) {
                LoadModBlock(mod, type);
            }
            else if (isModShell) {
                LoadModShell(mod, type);
            }
        }
    }
    public static void LoadModTank(TanksMod mod, Type type) {
        var modTank = (Activator.CreateInstance(type) as ModTank)!;
        var tankName = modTank.GetType().Name;
        modTank.InternalName = tankName;

        _loadingContent = modTank;

        mod.Data.Tanks.Add(modTank);
        _modTanks.Add(modTank);
        modTank.Mod = mod;

        // load each tank and its data, add to moddedTypes the singleton of the ModTank.
        ModSingletonRegistry._singletonMap.Add(type, modTank);

        modTank.Name ??= new([]);
        modTank.Texture ??= tankName;

        // doesn't insert anything if there is already something for English
        modTank.Name.AddLocalization(LangCode.English, $"{mod.InternalName}.{tankName}");
        DifficultyAlgorithm.TankDiffs[modTank.Type] = 0f;
        modTank!.Load();
        TankGame.ClientLog.Write($"Loaded modded tank '{modTank.Name.GetLocalizedString(LangCode.English)}'", LogType.Info);
    }
    public static void LoadModBlock(TanksMod mod, Type type) {
        var modBlock = (Activator.CreateInstance(type) as ModBlock)!;
        var blockName = modBlock.GetType().Name;

        _loadingContent = modBlock;

        mod.Data.Blocks.Add(modBlock);
        _modBlocks.Add(modBlock);
        modBlock!.Mod = mod;

        // again, but with modlbocks
        ModSingletonRegistry._singletonMap.Add(type, modBlock);
        modBlock.Name.AddLocalization(LangCode.English, $"{mod.InternalName}.{blockName}");
        modBlock.Register();
        TankGame.ClientLog.Write($"Loaded modded block '{modBlock.Name.GetLocalizedString(LangCode.English)}'", LogType.Info);
    }
    public static void LoadModShell(TanksMod mod, Type type) {
        var modShell = (Activator.CreateInstance(type) as ModShell)!;
        var shellName = modShell.GetType().Name;
        modShell.InternalName = shellName;

        _loadingContent = modShell;

        mod.Data.Shells.Add(modShell);
        _modShells.Add(modShell);
        modShell!.Mod = mod;

        // again, but with modshels
        ModSingletonRegistry._singletonMap.Add(type, modShell);
        modShell.Name.AddLocalization(LangCode.English, $"{mod.InternalName}.{shellName}");
        TankGame.MainThreadTasks.Enqueue(modShell.Register);
        TankGame.ClientLog.Write($"Loaded modded shell '{modShell.Name.GetLocalizedString(LangCode.English)}'", LogType.Info);
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

    // okay this is refactored but idk if it's good enough
    public static void DrawModLoading() {
        var renderer = TankGame.SpriteRenderer;
        var font = FontGlobals.RebirthFont;
        var fontLarge = FontGlobals.RebirthFontLarge;

        // dims the background to make the loading ui more apparent
        renderer.Draw(
            TextureGlobals.Pixels[Color.Black],
            new Rectangle(0, 0, WindowUtils.WindowWidth, WindowUtils.WindowHeight),
            Color.Black * 0.6f
        );

        // panel drawing parameters
        // yes they're hardcoded. deal with it.
        var center = new Vector2(WindowUtils.WindowWidth / 2f, WindowUtils.WindowHeight / 2f);
        var panelWidth = 700.ToResolutionX();
        var panelHeight = 350.ToResolutionY();
        var panelRect = new Rectangle(
            (int)(center.X - panelWidth / 2),
            (int)(center.Y - panelHeight / 2),
            (int)panelWidth,
            (int)panelHeight
        );

        // panel shadow
        renderer.Draw(TextureGlobals.Pixels[Color.Black], panelRect.GetOffset(10, 10), Color.Black * 0.5f);

        // panel
        renderer.Draw(TextureGlobals.Pixels[Color.Gray], panelRect, new Color(30, 30, 35));

        // panel accent
        var accentHeight = 6.ToResolutionY();
        renderer.Draw(
            TextureGlobals.Pixels[Color.White],
            new Rectangle(panelRect.X, panelRect.Y, panelRect.Width, (int)accentHeight),
            Color.Goldenrod
        );

        // text drawing
        var titleText = "MOD INITIALIZATION";
        var titleScale = new Vector2(0.6f).ToResolution();
        var titleSize = fontLarge.MeasureString(titleText) * titleScale;
        var titlePos = new Vector2(center.X, panelRect.Y + 60.ToResolutionY());

        // title shadow
        renderer.DrawString(fontLarge, titleText, titlePos + new Vector2(2), Color.Black * 0.5f, titleScale, 0f, GameUtils.GetAnchor(Anchor.Center, titleSize / titleScale), 1f, 0f);
        renderer.DrawString(fontLarge, titleText, titlePos, Color.White, titleScale, 0f, GameUtils.GetAnchor(Anchor.Center, titleSize / titleScale), 1f, 0f);

        // current status
        var statusText = Error != string.Empty ? $"ERROR: {Error}" : $"{Status}: {ModBeingLoaded}...";
        var statusScale = new Vector2(0.9f).ToResolution();
        var contentScale = statusScale * 0.5f;
        var currentContentLoading = _loadingContent != null ? _loadingContent.InternalName : string.Empty;
        var cmclSize = font.MeasureString(currentContentLoading) * contentScale;
        var statusSize = font.MeasureString(statusText) * statusScale;

        // make it "pulse" slightly using sine wave
        var pulse = (float)Math.Sin(RuntimeData.RunTime / 20f) * 0.1f + 0.9f;
        var statusColor = Error != string.Empty ? Color.Red : Color.LightGray * pulse;

        renderer.DrawString(font, statusText, center - new Vector2(0, 20.ToResolutionY()), statusColor, statusScale, 0f, GameUtils.GetAnchor(Anchor.Center, statusSize / statusScale), 1f, 0f);

        renderer.DrawString(font, currentContentLoading, center, statusColor, contentScale, 0f, GameUtils.GetAnchor(Anchor.Center, cmclSize / contentScale), 1f, 0f);

        // progress bar

        var barWidth = panelWidth * 0.8f;
        var barHeight = 30.ToResolutionY();
        var barRect = new Rectangle(
            (int)(center.X - barWidth / 2),
            (int)(center.Y + 40.ToResolutionY()),
            (int)barWidth,
            (int)barHeight
        );

        // bar border
        var borderRect = barRect;
        borderRect.Inflate(2, 2);
        renderer.Draw(TextureGlobals.Pixels[Color.White], borderRect, Color.Gray);

        // bar track
        renderer.Draw(TextureGlobals.Pixels[Color.Black], barRect, Color.Black);

        // bar fill
        float ratio = ActionsNeeded == 0 ? 0 : (float)ActionsComplete / ActionsNeeded;
        var fillWidth = (int)(barWidth * ratio);
        var fillRect = new Rectangle(barRect.X, barRect.Y, fillWidth, barRect.Height);

        // fill color, made to match accent
        renderer.Draw(TextureGlobals.Pixels[Color.White], fillRect, Color.Goldenrod);

        // gloss effect
        renderer.Draw(TextureGlobals.Pixels[Color.White], new Rectangle(fillRect.X, fillRect.Y, fillRect.Width, fillRect.Height / 2), Color.White * 0.3f);

        var divisor = AreCompilesAllowed ? 2 : 1;
        // progress text
        var percentText = $"{ratio * 100:0}% ({ActionsComplete / divisor}/{ActionsNeeded / divisor})";
        var percentScale = new Vector2(0.75f).ToResolution();
        var percentSize = font.MeasureString(percentText) * percentScale;
        var percentPos = new Vector2(center.X, barRect.Bottom + 15.ToResolutionY());

        renderer.DrawString(font, percentText, percentPos, Color.Gray, percentScale, 0f, GameUtils.GetAnchor(Anchor.Center, percentSize / percentScale), 1f, 0f);
    }
}
