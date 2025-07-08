using Microsoft.Xna.Framework;
using Steamworks;
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

namespace TanksRebirth.GameContent.ModSupport;

public enum LoadStatus
{
    Inactive,
    Unloading,
    Loading,
    Compiling,
    Sandboxing,
    Complete
}
public static class ModLoader
{
    public delegate void FinishModLoading();
    public static event FinishModLoading? OnFinishModLoading;
    public delegate void PostLoadModContent(TanksMod mod);
    public static event PostLoadModContent? OnPostModLoad;

    public static List<TanksMod> LoadedMods { get; set; } = [];
    private static List<AssemblyLoadContext> _loadedAlcs = [];

    public static int ActionsNeeded { get; private set; }
    public static int ActionsComplete { get; private set; }
    public static LoadStatus Status { get; private set; } = LoadStatus.Inactive; 
    public static string ModBeingLoaded { get; private set; } = string.Empty;
    public static bool LoadingMods { get; private set; }
    public static string ModsPath { get; } = Path.Combine(TankGame.SaveDirectory, "Mods");

    public const string ModNETVersion = "net8.0";

    private volatile static List<Action> _loadingActions = [];
    private volatile static List<Action> _sandboxingActions = [];

    private static Dictionary<string, AssemblyLoadContext> _modDeps = [];
    private static Dictionary<TanksMod, List<ModTank>> _modTankDictionary = [];
    private static Dictionary<TanksMod, List<ModBlock>> _modBlockDictionary = [];
    private static Dictionary<TanksMod, List<ModShell>> _modShellDictionary = [];
    public static ModTank[] ModTanks { get; private set; } = [];
    private static List<ModTank> _modTanks = [];

    public static ModBlock[] ModBlocks { get; private set; } = [];
    private static List<ModBlock> _modBlocks = [];

    public static ModShell[] ModShells { get; private set; } = [];
    private static List<ModShell> _modShells = [];

    private static bool _firstLoad = true;
    /// <summary>The error given from the mod-loading process.</summary>
    public static string Error = string.Empty;
    public static string LoadType = string.Empty;
    private static void AttemptCompile(string modName) {
        if (!RuntimeData.IsWindows) {
            TankGame.ClientLog.Write("Auto-compilation of mod failed. Specified OS architecture is not Windows.", Internals.LogType.Warn);
            return;
        }
        else {
            // painfully local code.
            var checkPath = "C:\\Program Files\\dotnet\\sdk";
            Process process = new();
            process.StartInfo.FileName = "dotnet.exe";
            process.StartInfo.Arguments = "--list-sdks";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.Start();

            string[] versions = process.StandardOutput
                .ReadToEnd()
                .TrimEnd()
                .Replace("[", string.Empty)
                .Replace("]", string.Empty)
                .Replace("\r", string.Empty)
                .Replace(checkPath, string.Empty)
                .Split("\n")
                .Select(x => x.Trim())
                .ToArray();

            var versionsSingular = string.Join(", ", versions);

            process.WaitForExit();
            // check if any version starts with a '8' to indicate that it is a .NET 8.0 SDK.
            if (!versions.Any(x => x.StartsWith('8'))) {
                TankGame.ClientLog.Write("Auto-compilation of mod failed. User does not have a .NET 8.0 SDK installed.", LogType.Warn);
                return;
            }
        }
        Status = LoadStatus.Compiling;
        Process proc = new();
        try {
            LoadType = Debugger.IsAttached ? "Debug" : "Release";
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
        if (MainMenuUI.Active) {
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
        _sandboxingActions.Clear();
        LoadedMods.ForEach(mod => {
            // for indivudally unloaded mods.

            // unload modded stuff and all data
            var modTankCount = _modTankDictionary[mod].Count;
            for (int i = 0; i < _modTankDictionary[mod].Count; i++) {
                _modTankDictionary[mod][i].Unload();
            }
            // this allows the next mod to unload properly... thrice
            ModTank.unloadOffset += modTankCount;
            _modTankDictionary[mod].Clear();

            var modBlockCount = _modBlockDictionary[mod].Count;
            for (int i = 0; i < _modBlockDictionary[mod].Count; i++) {
                _modBlockDictionary[mod][i].Unload();
            }
            ModBlock.unloadOffset += modBlockCount;
            _modBlockDictionary[mod].Clear();

            var modShellCount = _modShellDictionary[mod].Count;
            for (int i = 0; i < _modShellDictionary[mod].Count; i++) {
                _modShellDictionary[mod][i].Unload();
            }
            ModShell.unloadOffset += modShellCount;
            _modShellDictionary[mod].Clear();

            mod.OnUnload();
            UnloadModContent(ref mod);
        });
        LoadedMods.Clear();
        _loadedAlcs.ForEach(asm => {
            asm.Unload();
            ChatSystem.SendMessage($"Unloaded '{asm.Name}'", Color.Orange);
        });
        foreach (var entry in _modDeps) {
            var alc = entry.Value;
            var mod = entry.Key;

            ChatSystem.SendMessage($"Unloaded dependency '{alc.Name}' from {mod}", Color.DarkOrange);
        }
        // for when the unloading process is done.
        _modTankDictionary.Clear();
        _modBlockDictionary.Clear();
        _modShellDictionary.Clear();
        ModContent.moddedTypes.Clear();
        ResetContentDictionaries();
        _loadedAlcs.Clear();
        ModTank.unloadOffset = 0;
        ModBlock.unloadOffset = 0;
        ModShell.unloadOffset = 0;
        ChatSystem.SendMessage("Mod unload successful!", Color.Lime);
        Status = LoadStatus.Complete;

    }
    // doesn't work?
    private static void ResetContentDictionaries() {
        BlockID.Collection = new(MemberType.Fields);
        PingID.Collection = new(MemberType.Fields);
        PlayerID.Collection = new(MemberType.Fields);
        ShellID.Collection = new(MemberType.Fields);
        TankID.Collection = new(MemberType.Fields);
        TeamID.Collection = new(MemberType.Fields);
        TrackID.Collection = new(MemberType.Fields);
    }
    private static void UnloadModContent(ref TanksMod mod) {
        return;
        // unfinished for now.
        /*var types = mod.GetType().Assembly.GetTypes();

        for (int i = 0; i < types.Length; i++) {
            var fields = types[i].GetFields();
            for (int j = 0; j < fields.Length; i++) {
                var events = fields[j].FieldType.GetEvents();

                for (int k = 0; k < events.Length; k++) {
                    var @event = events[k];
                    var eventType = @event.EventHandlerType;
                    var cEvent = @event.get
                    foreach (var subscriber in )
                }
            }
        }*/
    }
    /// <summary>Prepare your garbage collector!</summary>
    internal static void LoadMods() {
        if (Status == LoadStatus.Unloading)
            ChatSystem.SendMessage("Mods are currently unloading! Unable to load mods.", Color.Red);
        if (Status == LoadStatus.Loading || Status == LoadStatus.Compiling || Status == LoadStatus.Sandboxing)
            ChatSystem.SendMessage("Mods are currently loading! Unable to load mods.", Color.Red);
        if (LoadedMods.Count > 0)
            UnloadAll();

        ActionsNeeded = 0;
        ActionsComplete = 0;

        if (!_firstLoad)
            ChatSystem.SendMessage("Reloading mods...", Color.Red);

        Directory.CreateDirectory(ModsPath);

        var folders = Directory.GetDirectories(ModsPath);

        if (folders.Length == 0) {
            _firstLoad = true;
            Status = LoadStatus.Complete;
            ChatSystem.SendMessage(_firstLoad ? $"Loaded {_loadedAlcs.Count} mod(s)." : $"Reloaded {_loadedAlcs.Count} mod(s).", Color.Lime);
            return;
        }
        LoadingMods = true;

        foreach (var folder in folders) {
            var files = Directory.GetFiles(folder);
            foreach (var file1 in files) {
                bool isProj = file1.Contains(".csproj");
                if (isProj) {
                    foreach (var file2 in files) {
                        var fileName = Path.GetFileName(file2);
                        var modName = folder.Split('\\')[^1];
                        if (fileName == modName + ".csproj") {
                            ActionsNeeded++;

                            var lines = File.ReadAllLines(file2);
                            var netVer = GetCsprojPropertyValue(lines, LocateCsprojProperty(lines, "TargetFramework"));

                            if (netVer != ModNETVersion) {
                                Error = $"This mod does not match TanksRebirth's .NET version ({ModNETVersion})";
                                return;
                            }

                            _loadingActions.Add(() => {
                                ModBeingLoaded = modName;
                                // load assemblies included in the modrefs folder
                                var dirPath = Path.Combine(folder, "modrefs");

                                // TODO: what the sigma? (translation - "why does this always throw an error when AttemptCompile is called?")
                                if (Directory.Exists(dirPath)) {
                                    foreach (var dllPath in Directory.GetFiles(dirPath).Where(x => x.EndsWith(".dll"))) {
                                        var dll = Path.GetFileName(dllPath);
                                        var depAlc = new AssemblyLoadContext(Path.GetFileNameWithoutExtension(dllPath), true);

                                        _modDeps[modName] = depAlc;

                                        depAlc.LoadFromStream(File.Open(dllPath, FileMode.Open));
                                    }
                                }
                                AttemptCompile(modName);
                                Status = LoadStatus.Loading;
                                string filepath = Path.Combine(folder, "bin", LoadType, ModNETVersion, $"{modName}.dll");
                                string pdb = Path.ChangeExtension(filepath, ".pdb");
                                // TODO: load PDB into the ALC.
                                var alc = new AssemblyLoadContext(modName, true);
                                // alc.LoadFromAssemblyPath(filepath);
                                alc.LoadFromStream(File.Open(filepath, FileMode.Open), File.Open(pdb, FileMode.Open));

                                _loadedAlcs.Add(alc);

                                var assembly = alc.Assemblies.First();

                                _sandboxingActions.Add(() => {
                                    try {
                                        Status = LoadStatus.Sandboxing;
                                        var types = assembly.GetTypes();
                                        int modClassCount = 0;
                                        foreach (var type in types) {
                                            if (type.IsSubclassOf(typeof(TanksMod)) && !type.IsAbstract) {
                                                modClassCount++;
                                                if (modClassCount > 1)
                                                    throw new Exception("Too many mod classes. Only one is allowed per-mod.");

                                                // let's run the virtually overridden methods.
                                                var tanksMod = Activator.CreateInstance(type) as TanksMod;
                                                tanksMod!.InternalName = modName;
                                                tanksMod!.Name = modName;

                                                _modTankDictionary.Add(tanksMod, []);
                                                _modBlockDictionary.Add(tanksMod, []);
                                                _modShellDictionary.Add(tanksMod, []);

                                                // now lets scan the mod's content
                                                // i feel like this is gravely inefficient but it can be changed
                                                // update: this is definitely somewhat horrid
                                                foreach (var type2 in tanksMod.GetType().Assembly.GetTypes()) {
                                                    if (type2.IsSubclassOf(typeof(ModTank)) && !type.IsAbstract) {
                                                        var modTank = (Activator.CreateInstance(type2) as ModTank)!;
                                                        _modTankDictionary[tanksMod].Add(modTank);
                                                        _modTanks.Add(modTank);
                                                        modTank!.Mod = tanksMod;

                                                        // load each tank and its data, add to moddedTypes the singleton of the ModTank.
                                                        ModContent.moddedTypes.Add(modTank);
                                                        modTank.Name.AddLocalization(LangCode.English, $"{tanksMod.InternalName}.{modTank.GetType().Name}");
                                                        modTank!.Register();
                                                        DifficultyAlgorithm.TankDiffs[modTank.Type] = 0f;
                                                        TankGame.ClientLog.Write($"Loaded modded tank '{modTank.Name.GetLocalizedString(LangCode.English)}'", LogType.Info);
                                                    } 
                                                    else if (type2.IsSubclassOf(typeof(ModBlock)) && !type.IsAbstract) {
                                                        var modBlock = (Activator.CreateInstance(type2) as ModBlock)!;
                                                        _modBlockDictionary[tanksMod].Add(modBlock);
                                                        _modBlocks.Add(modBlock);
                                                        modBlock!.Mod = tanksMod;

                                                        // again, but with modlbocks
                                                        ModContent.moddedTypes.Add(modBlock);
                                                        modBlock.Name.AddLocalization(LangCode.English, $"{tanksMod.InternalName}.{modBlock.GetType().Name}");
                                                        modBlock.Register();
                                                        TankGame.ClientLog.Write($"Loaded modded block '{modBlock.Name.GetLocalizedString(LangCode.English)}'", LogType.Info);
                                                    }
                                                    else if (type2.IsSubclassOf(typeof(ModShell)) && !type.IsAbstract) {
                                                        var modShell = (Activator.CreateInstance(type2) as ModShell)!;
                                                        _modShellDictionary[tanksMod].Add(modShell);
                                                        _modShells.Add(modShell);
                                                        modShell!.Mod = tanksMod;

                                                        // again, but with modshels
                                                        ModContent.moddedTypes.Add(modShell);
                                                        modShell.Name.AddLocalization(LangCode.English, $"{tanksMod.InternalName}.{modShell.GetType().Name}");
                                                        modShell.Register();
                                                        TankGame.ClientLog.Write($"Loaded modded shell '{modShell.Name.GetLocalizedString(LangCode.English)}'", LogType.Info);
                                                    }
                                                }

                                                LoadedMods.Add(tanksMod);

                                                tanksMod.OnLoad();

                                                OnPostModLoad?.Invoke(tanksMod);
                                            }
                                        }
                                    }
                                    catch (Exception e) {
                                        TankGame.ReportError(e, true, true);
                                        Error = e.Message;
                                        return;
                                    }
                                });

                                ActionsComplete++;
                                TankGame.ClientLog.Write($"Loaded mod assembly '{assembly.GetName().Name}', version '{assembly.GetName().Version}'", Internals.LogType.Info);
                            });
                            break;
                        }
                    }
                }
            }
        }
        Task.Run(async () => {
            _loadingActions.ForEach(x => x.Invoke());
            while (Error != string.Empty)
                await Task.Delay(10).ConfigureAwait(false);
            _sandboxingActions.ForEach(x => x.Invoke());
            while (Error != string.Empty)
                await Task.Delay(10).ConfigureAwait(false);
            LoadingMods = false;
            ModBeingLoaded = string.Empty;
            Status = LoadStatus.Complete;
            ChatSystem.SendMessage(_firstLoad ? $"Loaded {_loadedAlcs.Count} mod(s)." : $"Reloaded {_loadedAlcs.Count} mod(s).", Color.Lime);
            _firstLoad = false;

            OnFinishModLoading?.Invoke();
            ModTanks = _modTanks.ToArray();
            ModBlocks = _modBlocks.ToArray();
            ModShells = _modShells.ToArray();
        });

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

    public static void DrawModLoading() {
        var alpha = 0.7f;
        var width = WindowUtils.WindowWidth / 3;
        TankGame.SpriteRenderer.Draw(TextureGlobals.Pixels[Color.White], new Vector2(WindowUtils.WindowWidth / 2, WindowUtils.WindowHeight / 2), null, Color.SkyBlue * alpha, 0f, GameUtils.GetAnchor(Anchor.Center, TextureGlobals.Pixels[Color.White].Size()), new Vector2(width, 200.ToResolutionY()), default, 0f);

        var barDims = new Vector2(width - 120, 20).ToResolution();

        TankGame.SpriteRenderer.Draw(TextureGlobals.Pixels[Color.White], new Vector2(WindowUtils.WindowWidth / 2, WindowUtils.WindowHeight / 2), null, Color.Goldenrod * alpha, 0f, GameUtils.GetAnchor(Anchor.Center, TextureGlobals.Pixels[Color.White].Size()),
            barDims, default, 0f);
        var ratio = (float)ModLoader.ActionsComplete / ModLoader.ActionsNeeded;
        if (ModLoader.ActionsNeeded == 0)
            ratio = 0;
        TankGame.SpriteRenderer.Draw(TextureGlobals.Pixels[Color.White], new Vector2(WindowUtils.WindowWidth / 2, WindowUtils.WindowHeight / 2), null, Color.Yellow * alpha, 0f, GameUtils.GetAnchor(Anchor.Center, TextureGlobals.Pixels[Color.White].Size()),
            barDims * new Vector2(ratio, 1f).ToResolution(), default, 0f);

        var txt = $"{ModLoader.Status} {ModLoader.ModBeingLoaded}...";
        TankGame.SpriteRenderer.DrawString(FontGlobals.RebirthFont, txt, new(WindowUtils.WindowWidth / 2, WindowUtils.WindowHeight / 2 - 75.ToResolutionY()), Color.White, Vector2.One.ToResolution(), 0f, GameUtils.GetAnchor(Anchor.Center, FontGlobals.RebirthFont.MeasureString(txt)));

        txt = ModLoader.Error == string.Empty ? $"Loading your mods... {ratio * 100:0}% ({ModLoader.ActionsComplete} / {ModLoader.ActionsNeeded})" :
        $"Error Loading '{ModLoader.ModBeingLoaded}' ({ModLoader.Error})";
        TankGame.SpriteRenderer.DrawString(FontGlobals.RebirthFont, txt, new(WindowUtils.WindowWidth / 2, WindowUtils.WindowHeight / 2 - 150.ToResolutionY()), Color.White, Vector2.One.ToResolution(), 0f, GameUtils.GetAnchor(Anchor.Center, FontGlobals.RebirthFont.MeasureString(txt)));
    }
}
