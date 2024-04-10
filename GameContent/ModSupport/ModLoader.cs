using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TanksRebirth.GameContent.Systems;

namespace TanksRebirth.GameContent.ModSupport
{
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
        public static event FinishModLoading OnFinishModLoading;
        public delegate void PostLoadModContent(TanksMod mod);
        public static event PostLoadModContent OnPostModLoad;

        private static List<TanksMod> _loadedMods = new();
        private static List<AssemblyLoadContext> _loadedAlcs = new();

        public static int ActionsNeeded { get; private set; }
        public static int ActionsComplete { get; private set; }
        public static LoadStatus Status { get; private set; } = LoadStatus.Inactive; 
        public static string ModBeingLoaded { get; private set; } = "";
        public static bool LoadingMods { get; private set; }
        public static string ModsPath { get; } = Path.Combine(TankGame.SaveDirectory, "Mods");

        public const string ModNETVersion = "net6.0";

        private volatile static List<Action> _loadingActions = new();
        private volatile static List<Action> _sandboxingActions = new();

        private static Dictionary<TanksMod, List<ModTank>> _modTankDictionary = new();

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
                if (!Directory.Exists(checkPath) || !Directory.GetDirectories(checkPath).Any(x => x.Contains("6.0"))) {
                    GameHandler.ClientLog.Write("Auto-compilation of mod failed. Specified OS architecture is not Windows.", Internals.LogType.Warn);
                    return;
                }
            }
            Status = LoadStatus.Compiling;
            Process process = new();
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

                process.StartInfo = startInfo;
                process.Start();
                process.WaitForExit();
            } catch (Exception e) {
                Error = e.Message;
                process.Dispose();
                process.Close();
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
            _loadedMods.ForEach(mod => {
                // for indivudally unloaded mods.
                _modTankDictionary[mod].Clear();
                mod.OnUnload();
                UnloadModContent(ref mod);
            });
            _loadedMods.Clear();
            _loadedAlcs.ForEach(asm => {
                asm.Unload();
                ChatSystem.SendMessage($"Unloaded '{asm.Name}'", Color.Orange);
            });
            // for when the unloading process is done.
            _modTankDictionary.Clear();
            _loadedAlcs.Clear();
            ChatSystem.SendMessage("Mod unload successful!", Color.Lime);
            Status = LoadStatus.Complete;
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
        //private delegate Assembly AsmInternalLoad(ReadOnlySpan<byte> arrAssembly, ReadOnlySpan<byte> arrSymbols);
        /// <summary>Prepare your garbage collector!</summary>
        internal static void LoadMods() {
            /*var method = typeof(AssemblyLoadContext)
                .GetMethod("InternalLoad", BindingFlags.NonPublic | BindingFlags.Instance)
                .CreateDelegate<AsmInternalLoad>();*/

            if (Status == LoadStatus.Unloading) {
                ChatSystem.SendMessage("Mods are currently unloading! Unable to load mods.", Color.Red);
            }
            if (Status == LoadStatus.Loading || Status == LoadStatus.Compiling || Status == LoadStatus.Sandboxing) {
                ChatSystem.SendMessage("Mods are currently loading! Unable to load mods.", Color.Red);
            }
            if (_loadedMods.Count > 0)
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
                    bool isSln = file1.Contains(".csproj");
                    if (isSln) {
                        foreach (var file2 in files) {
                            var fileName = Path.GetFileName(file2);
                            var modName = folder.Split('\\')[^1];
                            if (fileName == modName + ".csproj") {
                                ActionsNeeded++;

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

                                                    /*_modTankDictionary.Add(tanksMod, new());

                                                    // now lets scan the mod's content for ModTanks
                                                    // i feel like this is gravely inefficient but it can be changed
                                                    foreach (var type2 in tanksMod.GetType().Assembly.GetTypes()) {
                                                        if (type2.IsSubclassOf(typeof(ModTank)) && !type.IsAbstract) {
                                                            var modTank = Activator.CreateInstance(type2) as ModTank;
                                                            _modTankDictionary[tanksMod].Add(modTank!);
                                                        }
                                                    }*/

                                                    _loadedMods.Add(tanksMod);

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
            });
        }
    }
}
