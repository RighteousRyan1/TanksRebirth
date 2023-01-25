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

public static class CommandGlobals {

    public static bool AreCheatsEnabled;

    /// <summary>The expected prefix to prepend before writing down a command.</summary>
    public const char ExpectedPrefix = '/';
    /// <summary>Commands for the chat. Feel free to add your own here.</summary>
    public static Dictionary<CommandInput, CommandOutput> Commands = new() {
        // render engine
        [new CommandInput(name: "rendermenu", description: "Disables the rendering and updating of the main menu. " +
            "Also disables all shaders pertaining to the game world.")] = new CommandOutput(netSync: false, (args) => {
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
        // server side
        [new CommandInput(name: "cheats", description: "Enables cheats on the server" +
            "Also disables all shaders pertaining to the game world.")] = new CommandOutput(netSync: true, (args) => {
                AreCheatsEnabled = bool.Parse(args[0]);
                ChatSystem.SendMessage("Cheats are now " + (AreCheatsEnabled ? "enabled" : "disabled" + ".") + ".", AreCheatsEnabled ? Color.Green : Color.Red);
            }),
        [new CommandInput(name: "changetnkproperty", description: "Changes a property parameter of your tank." +
            "Also disables all shaders pertaining to the game world.")] = new CommandOutput(netSync: false, (args) => {
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
                        } catch {
                            ChatSystem.SendMessage($"Property '{args[0]}' is not asssignable from the given argument.", Color.Red);
                        }
                    }
                }
            })
    };
}
