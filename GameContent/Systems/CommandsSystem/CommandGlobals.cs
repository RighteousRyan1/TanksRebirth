using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TanksRebirth.GameContent.UI;
using TanksRebirth.Internals.Common.Framework.Input;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Localization;
using TanksRebirth.Net;
using Microsoft.Xna.Framework.Input;
using TanksRebirth.Internals;

namespace TanksRebirth.GameContent.Systems.CommandsSystem;
#pragma warning disable
public static class CommandGlobals {

    private static PropertyInfo[] _playerPropertyInfoCache = null; // Cached PropertyInfo[] for the PlayerTank class. Used in changetankproperty
    public static bool AreCheatsEnabled;

    public static bool IsUpdatePending;

    /// <summary>The expected prefix to prepend before writing down a command.</summary>
    public const char ExpectedPrefix = '/';
    /// <summary>Commands for the chat. Feel free to add your own here.</summary>
    public static Dictionary<CommandInput, CommandOutput> Commands = new() {
        // general
        [new CommandInput(name: "help", description: "Get a list of all commands.")] = new CommandOutput(netSync: false, false, (args) => {
            for (int i = 0; i < Commands.Count; i++) {
                var elem = Commands.ElementAt(i);
                if (args.Length > 0 && args[0].ToLower() == "cheats") {
                    if (elem.Value.RequireCheats) {
                        ChatSystem.SendMessage($"{elem.Key.Name}: {elem.Key.Description}", Color.Khaki);
                    }
                }
                else
                    ChatSystem.SendMessage($"{elem.Key.Name}: {elem.Key.Description}", Color.Khaki);
            }
        }),
        /*[new CommandInput(name: "update", description: "Download any pending updates and restart the game.")] = new CommandOutput(netSync: false, false, (args) => {
            TankGame.AutoUpdater.FetchData();
            TankGame.AutoUpdater.DownloadUpdate();
            TankGame.AutoUpdater.OnDownloadComplete += (name, link) => {

            };
        }),*/
        [new CommandInput(name: "setbind", description: "Change a keybind by a given internal name.")] = new CommandOutput(netSync: false, false, (args) => {
            for (int i = 0; i < Keybind.AllKeybinds.Count; i++) {
                var bind = Keybind.AllKeybinds[i];
                if (bind.Name == args[0]) {
                    if (Enum.TryParse<Keys>(args[1], true, out var result)) {
                        bind.ForceReassign(result);
                        ChatSystem.SendMessage($"Changed keybind '{args[0]}' to '{args[1]}'", Color.DodgerBlue);
                        return;
                    }
                    else {
                        ChatSystem.SendMessage($"Invalid key code '{args[1]}'", Color.Red);
                    }
                }

                ChatSystem.SendMessage($"No keybind matches name '{args[0]}'", Color.Khaki);
            }
        }),
        [new CommandInput(name: "setlang", description: "Set the game's language.")] = new CommandOutput(netSync: false, false, (args) => {
            var lang = args[0];

            var exists = File.Exists(Path.Combine("Localization", lang + ".loc"));
            if (exists) {
                var parseLang = LangCode.Parse(lang);
                Language.LoadLang(parseLang, out TankGame.GameLanguage);
                TankGame.Settings.Language = parseLang;

                // TODO: try to only initialize the localization lol (causes UI to appear when it shouldn't)
                MainMenu.InitializeUIGraphics();
                GameUI.Initialize();
                VolumeUI.Initialize();
                GraphicsUI.Initialize();
                LevelEditor.Initialize();
                LevelEditor.InitializeSaveMenu();
                // ControlsUI.Initialize();
            }
        }),
        [new CommandInput(name: "musicvolume", description: "Set music volume.")] = new CommandOutput(netSync: false, false, (args) => {
            TankGame.Settings.MusicVolume = float.Parse(args[0]);
        }),
        [new CommandInput(name: "soundvolume", description: "Set sound volume.")] = new CommandOutput(netSync: false, false, (args) => {
            TankGame.Settings.EffectsVolume = float.Parse(args[0]);
        }),
        [new CommandInput(name: "ambvolume", description: "Set ambient volume.")] = new CommandOutput(netSync: false, false, (args) => {
            TankGame.Settings.AmbientVolume = float.Parse(args[0]);
        }),
        // render engine
        [new CommandInput(name: "rendermenu", description: "Disable/enable game rendering/updating in main menu.")] = new CommandOutput(netSync: false, false, (args) => {
                MapRenderer.ShouldRenderAll = bool.Parse(args[0]);
        }),
        [new CommandInput(name: "renderbounds", description: "Disable/enable the drawing of the outer bounds of the map.")] = new CommandOutput(netSync: false, false, (args) => {
            MapRenderer.ShouldRenderBounds = bool.Parse(args[0]);
        }),
        [new CommandInput(name: "renderfloor", description: "Disable/enable the drawing of the floor of the map.")] = new CommandOutput(netSync: false, false, (args) => {
            if (bool.TryParse(args[0], out bool truefalse)) {
                MapRenderer.ShouldRenderFloor = truefalse;
            } else if (args[0].ToLower() == "black") {
                MapRenderer.RenderFloorAsBlack = !MapRenderer.RenderFloorAsBlack;
            }
        }),
        // main menu
        [new CommandInput(name: "uselegacymenumusic", description: "Switch to and from the legacy menu music.")] = new CommandOutput(netSync: false, false, (args) => {
            if (bool.Parse(args[0])) {
                MainMenu.Theme.Stop();
                MainMenu.Theme = null;
                MainMenu.Theme = new("Theme_Legacy", "Content/Assets/mainmenu/theme_legacy", 1f);
                MainMenu.Theme.Play();
                return;
            }
            MainMenu.Theme.Stop();
            MainMenu.Theme = null;
            MainMenu.Theme = MainMenu.GetAppropriateMusic();
            MainMenu.Theme.Play();
        }),
        // client side
        [new CommandInput(name: "showtankteams", description: "Shows tank teams visually. Applies for each new tank.")] = new CommandOutput(netSync: true, false, (args) => {
            Tank.ShowTeamVisuals = bool.Parse(args[0]);
            ChatSystem.SendMessage("Tank team visuals are now " + (Tank.ShowTeamVisuals ? "enabled" : "disabled" + ".") + ".", Tank.ShowTeamVisuals ? Color.Green : Color.Red);
        }),
        // server side
        [new CommandInput(name: "cheats", description: "Enables cheats on the server")] = new CommandOutput(netSync: true, false, (args) => {
                AreCheatsEnabled = bool.Parse(args[0]);
                ChatSystem.SendMessage("Cheats are now " + (AreCheatsEnabled ? "enabled" : "disabled" + ".") + ".", AreCheatsEnabled ? Color.Green : Color.Red);
        }),
        [new CommandInput(name: "changetankproperty", description: "Changes a property parameter of your tank.")] = new CommandOutput(netSync: false, true, (args) => {
                PlayerTank? playerTank = null;
                if (NetPlay.GetMyClientId() <= GameHandler.AllPlayerTanks.Length)
                    playerTank = GameHandler.AllPlayerTanks[NetPlay.GetMyClientId()];

                if (playerTank == null) { // The playerTank was out of range... Somehow...
                    GameHandler.ClientLog.Write(
                        $"\'changetankproperty\' command failed! The tank identifier was out of the range of the array! The tank identifier is {NetPlay.GetMyClientId()}, while the length of the list was {GameHandler.AllPlayerTanks.Length}", 
                        LogType.ErrorFatal,
                        false);
                    return;
                }
                // The class is not likely going to change dynamically, just set the props once.
                _playerPropertyInfoCache ??= playerTank.Properties.GetType().GetProperties();

                if (args.Length < 2) {
                    ChatSystem.SendMessage("Usage: /changetankproperty <property name> <new property value>", Color.Red);
                    return;
                }

                var tankProperty = args[0];
                var newValueOfProperty = args[1];
                var idxFind = -1;
                
                { // Use diff scope to not pollute outer scope.
                    ref var searchSpace = ref MemoryMarshal.GetArrayDataReference(_playerPropertyInfoCache);
                    for (int i = 0; i < _playerPropertyInfoCache.Length; i++) {
                        var currProp = Unsafe.Add(ref searchSpace,i);
                        if (currProp.Name != tankProperty) continue;
                        idxFind = i;
                        break;
                    }
                }

                if (idxFind == -1) {
                    ChatSystem.SendMessage($"No such field as \'{tankProperty}\' in the player tank.", Color.Maroon);
                    return;
                }
                
                try {
                    var oldValue = _playerPropertyInfoCache[idxFind].GetValue(playerTank.Properties);

                    switch (oldValue) {
                        case int:
                            _playerPropertyInfoCache[idxFind].SetValue(playerTank.Properties, int.Parse(newValueOfProperty));
                            break;
                        case uint:
                            _playerPropertyInfoCache[idxFind].SetValue(playerTank.Properties, uint.Parse(newValueOfProperty));
                            break;
                        case bool:
                            _playerPropertyInfoCache[idxFind].SetValue(playerTank.Properties, bool.Parse(newValueOfProperty));
                            break;
                        case float:
                            _playerPropertyInfoCache[idxFind].SetValue(playerTank.Properties, float.Parse(newValueOfProperty));
                            break;
                    }
                        
                    ChatSystem.SendMessage($"Modified property '{args[0]}' from {oldValue} to {args[1]}", Color.Green);
                }
                catch (TargetInvocationException targetInvex){
                    ChatSystem.SendMessage($"Property '{args[0]}' is not asssignable from the given argument.", Color.Red);
                    GameHandler.ClientLog.Write(targetInvex.ToString(), LogType.ErrorFatal, false);
                }
            })
    };
}
