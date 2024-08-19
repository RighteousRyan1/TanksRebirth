using Microsoft.Xna.Framework;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Loader;
using System.Threading.Tasks;
using TanksRebirth.GameContent.ID;
using TanksRebirth.GameContent.Systems;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common.Framework.Collections;
using TanksRebirth.Localization;

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
    public static string ModBeingLoaded { get; private set; } = "";
    public static bool LoadingMods { get; private set; }
    public static string ModsPath { get; } = Path.Combine(TankGame.SaveDirectory, "Mods");

    public const string ModNETVersion = "net8.0";

    private volatile static List<Action> _loadingActions = [];
    private volatile static List<Action> _sandboxingActions = [];

    private static Dictionary<TanksMod, List<ModTank>> _modTankDictionary = new();
    public static ModTank[] ModTanks { get; private set; } = [];
    private static List<ModTank> _modTanks = new();

    private static bool _firstLoad = true;
    /// <summary>The error given from the mod-loading process.</summary>
    public static string Error = string.Empty;
    public static string LoadType = string.Empty;
    private static void AttemptCompile(string modName) {
        if (!TankGame.IsWindows) {
            GameHandler.ClientLog.Write("Auto-compilation of mod failed. Specified OS architecture is not Windows.", Internals.LogType.Warn);
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
                GameHandler.ClientLog.Write("Auto-compilation of mod failed. User does not have a .NET 8.0 SDK installed.", LogType.Warn);
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
                Arguments = $"/c dotnet build -c " + LoadType
            };

            proc.StartInfo = startInfo;
            proc.Start();
            proc.WaitForExit();
        } catch (Exception e) {
            Error = e.Message;
            proc.Dispose();
            proc.Close();
        }
    }

    internal static void UnloadAll() {
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

            // unload modded tanks and all of their data.
            for (int i = 0; i < _modTankDictionary[mod].Count; i++) {
                _modTankDictionary[mod][i].Unload();
            }
            _modTankDictionary[mod].Clear();
            mod.OnUnload();
            UnloadModContent(ref mod);
        });
        LoadedMods.Clear();
        _loadedAlcs.ForEach(asm => {
            asm.Unload();
            ChatSystem.SendMessage($"Unloaded '{asm.Name}'", Color.Orange);
        });
        // for when the unloading process is done.
        _modTankDictionary.Clear();
        ModContent.moddedTypes.Clear();
        ResetContentDictionaries();
        _loadedAlcs.Clear();
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
        // unfinished for now.
        /*var types = mod.GetType().Assembly.GetTypes();

        for (int i = 0; i < types.Length; i++) {
            var members = types[i].GetMembers();
            for (int j = 0; j < members.Length; i++) {
                members[j].GetType().get
            }
        }*/
    }
    /// <summary>Prepare your garbage collector!</summary>
    internal static void LoadMods() {
        if (Status == LoadStatus.Unloading) {
            ChatSystem.SendMessage("Mods are currently unloading! Unable to load mods.", Color.Red);
        }
        if (Status == LoadStatus.Loading || Status == LoadStatus.Compiling || Status == LoadStatus.Sandboxing) {
            ChatSystem.SendMessage("Mods are currently loading! Unable to load mods.", Color.Red);
        }
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

                            if (netVer != "net8.0") {
                                Error = "This mod does not match TanksRebirth's .NET version (8.0)";
                                return;
                            }

                            _loadingActions.Add(() => {
                                ModBeingLoaded = modName;
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

                                                _modTankDictionary.Add(tanksMod, new());

                                                // now lets scan the mod's content for ModTanks
                                                // i feel like this is gravely inefficient but it can be changed
                                                foreach (var type2 in tanksMod.GetType().Assembly.GetTypes()) {
                                                    if (type2.IsSubclassOf(typeof(ModTank)) && !type.IsAbstract) {
                                                        var modTank = (Activator.CreateInstance(type2) as ModTank)!;
                                                        _modTankDictionary[tanksMod].Add(modTank);
                                                        _modTanks.Add(modTank);
                                                        modTank!.Mod = tanksMod;

                                                        // load each tank and its data, add to moddedTypes the singleton of the ModTank.
                                                        ModContent.moddedTypes.Add(modTank);
                                                        modTank!.Register();
                                                        GameHandler.ClientLog.Write($"Loaded modded tank '{modTank.Name.GetLocalizedString(LangCode.English)}'", LogType.Info);
                                                    }
                                                }

                                                LoadedMods.Add(tanksMod);

                                                tanksMod.OnLoad();

                                                OnPostModLoad?.Invoke(tanksMod);
                                            }
                                        }
                                    }  catch (Exception e) {
                                        TankGame.ReportError(e, true, true);
                                        Error = e.Message;
                                        return;
                                    }
                                });

                                ActionsComplete++;
                                GameHandler.ClientLog.Write($"Loaded mod assembly '{assembly.GetName().Name}', version '{assembly.GetName().Version}'", Internals.LogType.Info);
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
}
