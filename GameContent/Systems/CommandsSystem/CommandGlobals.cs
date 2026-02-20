using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using tainicom.Aether.Physics2D;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.ModSupport;
using TanksRebirth.GameContent.RebirthUtils;
using TanksRebirth.GameContent.Systems.TankSystem;
using TanksRebirth.GameContent.UI;
using TanksRebirth.GameContent.UI.LevelEditor;
using TanksRebirth.GameContent.UI.MainMenu;
using TanksRebirth.Graphics;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common.Framework.Audio;
using TanksRebirth.Internals.Common.Framework.Input;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Localization;
using TanksRebirth.Net;

namespace TanksRebirth.GameContent.Systems.CommandsSystem;
#pragma warning disable

// TODO: maybe implement command *assists*? where parameters are also suggested?
public static class CommandGlobals {

    private static PropertyInfo[] _playerPropertyInfoCache = null; // Cached PropertyInfo[] for the PlayerTank class. Used in changetankproperty
    public static bool AreCheatsEnabled;

    public static bool DrawMeshShadows = true;

    /// <summary>The expected prefix to prepend before writing down a command.</summary>
    public const char ExpectedPrefix = '/';
    /// <summary>Commands for the chat. Feel free to add your own here.</summary>
    public static Dictionary<CommandInput, CommandOutput> Commands = new() {
        // general
        [new CommandInput(name: "chat_corner", description: "Set the chat corner. (TopLeft, TopRight, BottomLeft, BottomRight)")] = new CommandOutput(netSync: false, false, (args) => {
            var names = Enum.GetNames<ChatMessageCorner>();
            var corner = args[0];
            for (int i = 0; i < names.Length; i++) {
                if (corner == names[i])
                    ChatSystem.Corner = (ChatMessageCorner)i;
            }
        }),
        [new CommandInput(name: "destroy", description: "Destroy yourself.")] = new CommandOutput(netSync: false, false, (args) => {
            PlayerTank.ClientTank?.Destroy(new TankHurtContextOther(null, TankHurtContextOther.HurtContext.FromOther, "Used commands."), true);
        }),
        [new CommandInput(name: "cmd_clear", description: "Clear the console.")] = new CommandOutput(netSync: false, false, (args) => {
            TankGame.IngameConsole.Clear();
        }),
        [new CommandInput(name: "cmd_color", description: "Clear the console.")] = new CommandOutput(netSync: false, false, (args) => {
            var color = args[0];
            var isGoodColor = ColorUtils.ColorsByName.ContainsKey(color);
            if (isGoodColor) {
                TankGame.IngameConsole.ConsoleBaseColor = ColorUtils.ColorsByName[color];
            }
        }),

        [new CommandInput(name: "wiimote_toggle", description: "Attempt to connect a Wiimote if none are present, otherwise, disconnect.")] = new CommandOutput(netSync: false, false, (args) => {
            if (WiimoteSystem.IsConnected) {
                bool disconnected = WiimoteSystem.TryDisconnect();

                if (disconnected)
                    TankGame.ClientLog.Write("Wiimote disconnected.", LogType.Info);
                else
                    TankGame.ClientLog.Write("Wiimote cannot disconnect.", LogType.Warn);
            }
            else {
                bool connected = WiimoteSystem.TryConnect();

                if (connected)
                    TankGame.ClientLog.Write("Wiimote connected.", LogType.Info);
                else
                    TankGame.ClientLog.Write("Wiimote cannot connect.", LogType.Warn);
            }
        }),

        [new CommandInput(name: "mouse_draw_enabled", description: "Enable/disable drawing for mice.")] = new CommandOutput(netSync: false, false, (args) => {
            var enabled = bool.Parse(args[0]);
            TankGame.miceForceDrawOverride = enabled;

            TankGame.IngameConsole.Log($"Mouse drawing is now: {enabled}", enabled ? Color.Lime : Color.Red);
        }),

        // mods
        [new CommandInput(name: "mod_reload", description: "Reloads all mods, on the spot.")] = new CommandOutput(netSync: false, false, (args) => {
            ModLoader.LoadMods();
        }),
        [new CommandInput(name: "mod_toggle", description: "Toggles a specific mod on/off using the mod's internal name.")] = new CommandOutput(netSync: false, false, (args) => {
            var mod_find = args[0];

            var modIndex = ModLoader.modNames.IndexOf(mod_find);

            if (modIndex > -1) {
                ModLoader.modEnablement[modIndex] = !ModLoader.modEnablement[modIndex];
                TankGame.IngameConsole.Log($"Mod '{mod_find}': {ModLoader.modEnablement[modIndex]}", Color.Lime);
            } else {
                TankGame.IngameConsole.Log($"There is no mod by the given name '{mod_find}'.", Color.Red);
            }
        }),

        // cmd specific
        [new CommandInput(name: "cmd_scale", description: "Sets the scale of text in the console.")] = new CommandOutput(netSync: false, false, (args) => {
            if (float.TryParse(args[0], out var newScale)) {
                TankGame.IngameConsole.LogScale = newScale;
            }
        }),
        [new CommandInput(name: "bind_set", description: "Change a keybind by a given internal name.")] = new CommandOutput(netSync: false, false, (args) => {
            for (int i = 0; i < Keybind.AllKeybinds.Count; i++) {
                var bind = Keybind.AllKeybinds[i];
                if (bind.Name == args[0]) {
                    if (Enum.TryParse<Keys>(args[1], true, out var result)) {
                        bind.ForceReassign(result);
                        TankGame.IngameConsole.Log($"Changed keybind '{args[0]}' to '{args[1]}'", Color.DodgerBlue);
                        return;
                    }
                    else {
                        TankGame.IngameConsole.Log($"Invalid key code '{args[1]}'", Color.Red);
                    }
                }

                TankGame.IngameConsole.Log($"No keybind matches name '{args[0]}'", Color.Khaki);
            }
        }),
        [new CommandInput(name: "lang_set", description: "Set the game's language.")] = new CommandOutput(netSync: false, false, (args) => {
            var lang = args[0];

            var exists = File.Exists(Path.Combine("Localization", lang + ".loc"));
            if (exists) {
                var parseLang = LangCode.Parse(lang);
                Language.LoadLang(parseLang, out TankGame.GameLanguage);
                FontGlobals.LoadLocalizedFont(parseLang);
                TankGame.Settings.Language = parseLang;

                // TODO: try to only initialize the localization lol (causes UI to appear when it shouldn't)
                MainMenuUI.InitializeUI();
                GameUI.Initialize();
                VolumeUI.Initialize();
                GraphicsUI.Initialize();
                LevelEditorUI.Initialize();
                LevelEditorUI.InitializeSaveMenu();
                // ControlsUI.Initialize();
            }
        }),
        [new CommandInput(name: "snd_mus", description: "Set music volume.")] = new CommandOutput(netSync: false, false, (args) => {
            TankGame.Settings.MusicVolume = float.Parse(args[0]);
            VolumeUI.MusicVolume.Value = TankGame.Settings.MusicVolume;
        }),
        [new CommandInput(name: "snd_fx", description: "Set sound volume.")] = new CommandOutput(netSync: false, false, (args) => {
            TankGame.Settings.EffectsVolume = float.Parse(args[0]);
            VolumeUI.EffectsVolume.Value = TankGame.Settings.EffectsVolume;
        }),
        [new CommandInput(name: "snd_amb", description: "Set ambient volume.")] = new CommandOutput(netSync: false, false, (args) => {
            TankGame.Settings.AmbientVolume = float.Parse(args[0]);
            VolumeUI.AmbientVolume.Value = TankGame.Settings.AmbientVolume;
        }),



        // render engine
        [new CommandInput(name: "r_menu", description: "Disable/enable game rendering/updating in main menu.")] = new CommandOutput(netSync: false, false, (args) => {
            GameScene.UpdateAndRender = bool.Parse(args[0]);
        }),
        [new CommandInput(name: "r_gp_ui", description: "Disable/enable drawing gameplay UI.")] = new CommandOutput(netSync: false, false, (args) => {
            GameSceneUI.DrawingEnabled = bool.Parse(args[0]);
        }),
        [new CommandInput(name: "r_chromakey_enabled", description: "Enables/disables chroma key rendering.")] = new CommandOutput(netSync: false, false, (args) => {
            GameScene.UseCustomSceneColor = bool.Parse(args[0]);
        }),
        [new CommandInput(name: "r_mesh_shadows", description: "Whether or not mesh shadows are drawn.")] = new CommandOutput(netSync: false, false, (args) => {
            DrawMeshShadows = bool.Parse(args[0]);
        }),
        [new CommandInput(name: "r_chromakey", description: "Change the game scene to a custom color.")] = new CommandOutput(netSync: false, false, (args) => {
            var color = args[0];
            var isGoodColor = ColorUtils.ColorsByName.ContainsKey(color);
            if (isGoodColor) {
                GameScene.SceneRenderColor = ColorUtils.ColorsByName[color];
            }
        }),
        [new CommandInput(name: "r_room_draw_enable", description: "Whether or not to draw the room scene.")] = new CommandOutput(netSync: false, false, (args) => {
            RoomScene.EnableDraw = bool.Parse(args[0]);
        }),


        // main menu
        [new CommandInput(name: "snd_legacy_mus", description: "Switch to and from the legacy menu music.")] = new CommandOutput(netSync: false, false, (args) => {
            if (bool.Parse(args[0])) {
                MainMenuUI.Theme.Stop();
                MainMenuUI.Theme = null;
                MainMenuUI.Theme = new("Theme_Legacy", "Content/Assets/mainmenu/theme_legacy", 1f);
                MainMenuUI.Theme.Play();
                return;
            }
            MainMenuUI.Theme.Stop();
            MainMenuUI.Theme = null;
            MainMenuUI.Theme = MainMenuUI.GetAppropriateMusic();
            MainMenuUI.Theme.Play();
        }),
        // client side
        [new CommandInput(name: "c_show_teams", description: "Shows tank teams visually. Applies for each new tank.")] = new CommandOutput(netSync: true, false, (args) => {
            Tank.ShowTeamVisuals = bool.Parse(args[0]);
            TankGame.IngameConsole.Log("Tank team visuals are now " + (Tank.ShowTeamVisuals ? "enabled" : "disabled" + ".") + ".", Tank.ShowTeamVisuals ? Color.Green : Color.Red);
        }),
        // server side
        [new CommandInput(name: "s_cheats", description: "Enables cheats on the server")] = new CommandOutput(netSync: true, false, (args) => {
            AreCheatsEnabled = bool.Parse(args[0]);
            TankGame.IngameConsole.Log("Cheats are now " + (AreCheatsEnabled ? "enabled" : "disabled" + ".") + ".", AreCheatsEnabled ? Color.Green : Color.Red);
        }),
        [new CommandInput(name: "s_tnk_prop", description: "Changes a property parameter of your tank.")] = new CommandOutput(netSync: false, true, (args) => {
            PlayerTank? playerTank = null;
            if (NetPlay.GetMyClientId() <= GameHandler.AllPlayerTanks.Length)
                playerTank = GameHandler.AllPlayerTanks[NetPlay.GetMyClientId()];

            if (playerTank == null) { // The playerTank was out of range... Somehow...
                TankGame.ClientLog.Write(
                    $"'s_tnk_prop' command failed! The tank identifier was out of the range of the array! The tank identifier is {NetPlay.GetMyClientId()}, while the length of the list was {GameHandler.AllPlayerTanks.Length}",
                    LogType.ErrorFatal,
                    false);
                return;
            }
            // The class is not likely going to change dynamically, just set the props once.
            _playerPropertyInfoCache ??= playerTank.Properties.GetType().GetProperties();

            if (args.Length < 2) {
                TankGame.IngameConsole.Log("Usage: /s_tnk_prop <property name> <new property value>", Color.Red);
                return;
            }

            var tankProperty = args[0];
            var newValueOfProperty = args[1];
            var idxFind = -1;

            { // Use diff scope to not pollute outer scope.
                ref var searchSpace = ref MemoryMarshal.GetArrayDataReference(_playerPropertyInfoCache);
                for (int i = 0; i < _playerPropertyInfoCache.Length; i++) {
                    var currProp = Unsafe.Add(ref searchSpace, i);
                    if (currProp.Name != tankProperty) continue;
                    idxFind = i;
                    break;
                }
            }

            if (idxFind == -1) {
                TankGame.IngameConsole.Log($"No such field as \'{tankProperty}\' in the player tank.", Color.Maroon);
                return;
            }

            try {
                var oldValue = _playerPropertyInfoCache[idxFind].GetValue(playerTank.Properties);

                switch (oldValue) {
                    case int i:
                        _playerPropertyInfoCache[idxFind].SetValue(playerTank.Properties, i);
                        break;
                    case uint ui:
                        _playerPropertyInfoCache[idxFind].SetValue(playerTank.Properties, ui);
                        break;
                    case bool b:
                        _playerPropertyInfoCache[idxFind].SetValue(playerTank.Properties, b);
                        break;
                    case float f:
                        _playerPropertyInfoCache[idxFind].SetValue(playerTank.Properties, f);
                        break;
                }

                TankGame.IngameConsole.Log($"Modified property '{args[0]}' from {oldValue} to {args[1]}", Color.Green);
            } catch (TargetInvocationException targetInvex) {
                TankGame.IngameConsole.Log($"Property '{args[0]}' is not asssignable from the given argument.", Color.Red);
                TankGame.ClientLog.Write(targetInvex.ToString(), LogType.ErrorFatal, false);
            }
        }),

        // funny dev stuff

        [new CommandInput(name: "s_control_tanks", description: "Host only: Lets the host control a tank's movement with their mouse")] = new CommandOutput(netSync: false, true, (args) => {
            var enable = bool.Parse(args[0]);
            
            DebugManager.SuperSecretDevOption = enable;
        }),
        [new CommandInput(name: "s_rand_cosmetics", description: "Host only: Enables randomized cosmetics.")] = new CommandOutput(netSync: false, true, (args) => {
            var enable = bool.Parse(args[0]);

            DebugManager.SecretCosmeticSetting = enable;
            TankGame.IngameConsole.Log($"Random cosmetics: {DebugManager.SecretCosmeticSetting}", DebugManager.SecretCosmeticSetting ? Color.Lime : Color.Red);
        }),
    };
}
