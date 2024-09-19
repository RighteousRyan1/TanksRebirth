using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TanksRebirth.GameContent.ID;
using TanksRebirth.GameContent.Properties;
using TanksRebirth.GameContent.UI;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common.Framework;
using TanksRebirth.Internals.Common.Framework.Audio;
using TanksRebirth.Internals.Common.Framework.Input;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Net;

namespace TanksRebirth.GameContent.Systems.PingSystem;

public static class PingMenu {
    public static Dictionary<int, Texture2D> PingIdToTexture = new();

    // TODO: localize
    public static Dictionary<int, string> PingIdToName = new() {
        [PingID.Generic] = "General",
        [PingID.StayHere] = "Stay Here",
        [PingID.WatchHere] = "Watch Here",
        [PingID.AvoidHere] = "Avoid Here",
        [PingID.GoHere] = "Go Here",
        [PingID.FocusHere] = "Focus Here",
        [PingID.GroupHere] = "Group Here"
    };

    public static void Initialize() {
        static string specialReplace(string s) => s.Replace(' ', '_').ToLower();
        for (int i = 0; i < PingIdToName.Count; i++) {
            PingIdToTexture[i] = GameResources.GetGameResource<Texture2D>($"Assets/textures/ui/ping/{specialReplace(PingIdToName[i])}");
        }
    }
    public static Keybind PingGeneral = new(nameof(PingGeneral), Keys.D1) {
        KeybindPressAction = (bind) => {
            if (!GameProperties.InMission || !IntermissionSystem.IsAwaitingNewMission)
                return;
            IngamePing.CreateFromTankSender(MatrixUtils.GetWorldPosition(MouseUtils.MousePosition), PingID.Generic, NetPlay.GetMyClientId(), Client.IsConnected());
        }
    };
    public static Keybind PingStay = new(nameof(PingStay), Keys.D2) {
        KeybindPressAction = (bind) => {
            if (!GameProperties.InMission || !IntermissionSystem.IsAwaitingNewMission)
                return;
            IngamePing.CreateFromTankSender(MatrixUtils.GetWorldPosition(MouseUtils.MousePosition), PingID.StayHere, NetPlay.GetMyClientId(), Client.IsConnected());
        }
    };
    public static Keybind PingWatch = new(nameof(PingWatch), Keys.D3) {
        KeybindPressAction = (bind) => {
            if (!GameProperties.InMission || !IntermissionSystem.IsAwaitingNewMission)
                return;
            IngamePing.CreateFromTankSender(MatrixUtils.GetWorldPosition(MouseUtils.MousePosition), PingID.WatchHere, NetPlay.GetMyClientId(), Client.IsConnected());
        }
    };
    public static Keybind PingAvoid = new(nameof(PingAvoid), Keys.D4) {
        KeybindPressAction = (bind) => {
            if (!GameProperties.InMission || !IntermissionSystem.IsAwaitingNewMission)
                return;
            IngamePing.CreateFromTankSender(MatrixUtils.GetWorldPosition(MouseUtils.MousePosition), PingID.AvoidHere, NetPlay.GetMyClientId(), Client.IsConnected());
        }
    };
    public static Keybind PingGo = new(nameof(PingGo), Keys.D5) {
        KeybindPressAction = (bind) => {
            if (!GameProperties.InMission || !IntermissionSystem.IsAwaitingNewMission)
                return;
            IngamePing.CreateFromTankSender(MatrixUtils.GetWorldPosition(MouseUtils.MousePosition), PingID.GoHere, NetPlay.GetMyClientId(), Client.IsConnected());
        }
    };
    public static Keybind PingFocus = new(nameof(PingFocus), Keys.D6) {
        KeybindPressAction = (bind) => {
            if (!GameProperties.InMission || !IntermissionSystem.IsAwaitingNewMission)
                return;
            IngamePing.CreateFromTankSender(MatrixUtils.GetWorldPosition(MouseUtils.MousePosition), PingID.FocusHere, NetPlay.GetMyClientId(), Client.IsConnected());
        }
    };
    public static Keybind PingGroup = new(nameof(PingGroup), Keys.D7) {
        KeybindPressAction = (bind) => {
            if (!GameProperties.InMission || !IntermissionSystem.IsAwaitingNewMission)
                return;
            IngamePing.CreateFromTankSender(MatrixUtils.GetWorldPosition(MouseUtils.MousePosition), PingID.GroupHere, NetPlay.GetMyClientId(), Client.IsConnected());
        }
    };
    // i dont think a radial would be optimal. Scroll wheel would even be better
    //public static Radial RadialMenu = new(2, new Circle());

    //private static int _curPingId;

    // todo: finish impl
}
