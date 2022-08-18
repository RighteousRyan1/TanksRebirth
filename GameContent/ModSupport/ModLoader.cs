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
        Loading,
        Compiling,
        Sandboxing,
        Complete
    }
    public static class ModLoader
    {
        public delegate void PostLoadModContent();
        public static event PostLoadModContent OnPostModLoad;

        public static int ActionsNeeded { get; private set; }
        public static int ActionsComplete { get; private set; }
        public static LoadStatus Status { get; private set; } = LoadStatus.Loading; 
        public static string ModBeingLoaded { get; private set; } = "";
        public static bool LoadingMods { get; private set; }
        public static string ModsPath { get; } = Path.Combine(TankGame.SaveDirectory, "Mods");

        private static List<Assembly> _loadedAssemblies = new();

        public const string ModNETVersion = "net6.0";

        private volatile static List<Action> _loadingActions = new();
        private volatile static List<Action> _sandboxingActions = new();

        private static bool _firstLoad = true;
        private static void Compile(string modName)
        {
            if (!TankGame.IsWindows) {
                GameHandler.ClientLog.Write("Auto-compilation of mod failed. Specified OS architecture is not Windows.", Internals.LogType.Warn);
                return;
            }
            Status = LoadStatus.Compiling;
            Process process = new();
            ProcessStartInfo startInfo = new() {
                UseShellExecute = false,

                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = @"C:\Windows\system32\cmd.exe",
                WorkingDirectory = Path.Combine(ModsPath, modName),
                Arguments = $"/c dotnet build -c Release"
            };

            process.StartInfo = startInfo;
            process.Start();
            process.WaitForExit();

            // garbage collector hell
            // Thread.Sleep(GameHandler.GameRand.Next(250, 1250));
        }
        /// <summary>Prepare your garbage collector!</summary>
        internal static void LoadMods()
        {
            _loadedAssemblies.Clear();
            _loadingActions.Clear();
            _sandboxingActions.Clear();

            ActionsNeeded = 0;
            ActionsComplete = 0;

            if (!_firstLoad)
                ChatSystem.SendMessage("Reloading mods...", Color.Red);

            Directory.CreateDirectory(ModsPath);

            var folders = Directory.GetDirectories(ModsPath);

            if (folders.Length == 0) {
                return;
            }
            LoadingMods = true;

            foreach (var folder in folders)
            {
                var files = Directory.GetFiles(folder);
                foreach (var file1 in files)
                {
                    bool isSln = file1.Contains(".sln");
                    if (isSln)
                    {
                        foreach (var file2 in files)
                        {
                            var fileName = Path.GetFileName(file2);
                            var modName = folder.Split('\\')[^1];
                            if (fileName == modName + ".sln")
                            {
                                ActionsNeeded++;

                                _loadingActions.Add(() =>
                                {
                                    ModBeingLoaded = modName;
                                    Compile(modName);
                                    Status = LoadStatus.Loading;
                                    string filepath = Path.Combine(folder, "bin", "Release", ModNETVersion, $"{modName}.dll");
                                    string pdb = Path.ChangeExtension(filepath, ".pdb");
                                    var assembly = Assembly.Load(File.ReadAllBytes(filepath), File.ReadAllBytes(pdb));
                                    _loadedAssemblies.Add(assembly);

                                    _sandboxingActions.Add(() =>
                                    {
                                        Status = LoadStatus.Sandboxing;
                                        var types = assembly.GetTypes();
                                        foreach (var type in types)
                                        {
                                            if (type.Name == "MainRun")
                                            {
                                                var methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public);
                                                foreach (var method in methods)
                                                    if (method.Name != "Main")
                                                        method.Invoke(null, null);
                                            }
                                            //Thread.Sleep(250);
                                        }
                                        // ActionsComplete++;
                                    });

                                    ActionsComplete++;
                                    GameHandler.ClientLog.Write($"Loaded mod assembly '{assembly.GetName().Name}', version '{assembly.GetName().Version}'", Internals.LogType.Info);
                                });
                            }
                        }
                    }
                }
            }
            Task.Run(() => {
                _loadingActions.ForEach(x => x.Invoke());
                _sandboxingActions.ForEach(x => x.Invoke());
                LoadingMods = false;
                Status = LoadStatus.Complete;
                ChatSystem.SendMessage(_firstLoad ? $"Loaded {_loadedAssemblies.Count} mod(s)." : $"Reloaded {_loadedAssemblies.Count} mod(s).", Color.Lime);
                _firstLoad = false;
                OnPostModLoad?.Invoke();
            });
        }
    }
}
