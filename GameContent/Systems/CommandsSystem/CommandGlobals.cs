using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TanksRebirth.GameContent.UI;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Net;

namespace TanksRebirth.GameContent.Systems.CommandsSystem;
#pragma warning disable
public static class CommandGlobals {

    public static bool AreCheatsEnabled;

    /// <summary>The expected prefix to prepend before writing down a command.</summary>
    public const char ExpectedPrefix = '/';
    /// <summary>Commands for the chat. Feel free to add your own here.</summary>
    public static Dictionary<CommandInput, CommandOutput> Commands = new() {
        // general
        [new CommandInput(name: "help", description: "Get a list of all commands.")] = new CommandOutput(netSync: false, (args) => {
            for (int i = 0; i < Commands.Count; i++) {
                var elem = Commands.ElementAt(i);

                ChatSystem.SendMessage($"{elem.Key.Name}: {elem.Key.Description}", Color.Khaki);
            }
        }),
        [new CommandInput(name: "musicvolume", description: "Set music volume.")] = new CommandOutput(netSync: false, (args) => {
            TankGame.Settings.MusicVolume = float.Parse(args[0]);
        }),
        [new CommandInput(name: "soundvolume", description: "Set sound volume.")] = new CommandOutput(netSync: false, (args) => {
            TankGame.Settings.EffectsVolume = float.Parse(args[0]);
        }),
        [new CommandInput(name: "ambvolume", description: "Set ambient volume.")] = new CommandOutput(netSync: false, (args) => {
            TankGame.Settings.AmbientVolume = float.Parse(args[0]);
        }),
        // render engine
        [new CommandInput(name: "rendermenu", description: "Disable game rendering/updating in main menu.")] = new CommandOutput(netSync: false, (args) => {
                MapRenderer.ShouldRender = bool.Parse(args[0]);
        }),
        // main menu
        [new CommandInput(name: "uselegacymenumusic", description: "Switch to and from the legacy menu music.")] = new CommandOutput(netSync: false, (args) => {
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
        [new CommandInput(name: "showtankteams", description: "Shows tank teams visually. Applies for each new tank.")] = new CommandOutput(netSync: true, (args) => {
            Tank.ShowTeamVisuals = bool.Parse(args[0]);
            ChatSystem.SendMessage("Tank team visuals are now " + (Tank.ShowTeamVisuals ? "enabled" : "disabled" + ".") + ".", Tank.ShowTeamVisuals ? Color.Green : Color.Red);
        }),
        // server side
        [new CommandInput(name: "cheats", description: "Enables cheats on the server")] = new CommandOutput(netSync: true, (args) => {
                AreCheatsEnabled = bool.Parse(args[0]);
                ChatSystem.SendMessage("Cheats are now " + (AreCheatsEnabled ? "enabled" : "disabled" + ".") + ".", AreCheatsEnabled ? Color.Green : Color.Red);
        }),
        [new CommandInput(name: "changetankproperty", description: "Changes a property parameter of your tank.")] = new CommandOutput(netSync: false, (args) => {
                if (!AreCheatsEnabled) {
                    ChatSystem.SendMessage("In order to use this command, cheats must be True.", Color.Red);
                    return;
                }
                var tnk = GameHandler.AllPlayerTanks[NetPlay.GetMyClientId()];
                if (tnk != null) {
                    var tnkprops = tnk.Properties.GetType()
                    .GetProperties();

                    var idxFind = Array.FindIndex(tnkprops, prop => prop.Name == args[0]);

                    if (idxFind > -1) {
                        try {
                            var oldValue = tnkprops[idxFind].GetValue(tnk.Properties);

                            if (oldValue is int)
                                tnkprops[idxFind].SetValue(tnk.Properties, int.Parse(args[1]));
                            if (oldValue is bool)
                                tnkprops[idxFind].SetValue(tnk.Properties, bool.Parse(args[1]));
                            if (oldValue is float)
                                tnkprops[idxFind].SetValue(tnk.Properties, float.Parse(args[1]));
                            ChatSystem.SendMessage($"Modified property '{args[0]}' from {oldValue} to {args[1]}", Color.Green);
                        }
                        catch {
                            ChatSystem.SendMessage($"Property '{args[0]}' is not asssignable from the given argument.", Color.Red);
                        }
                    }
                }
        })
    };
}
