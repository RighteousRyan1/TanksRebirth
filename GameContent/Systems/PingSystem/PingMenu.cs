using Microsoft.Xna.Framework;
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
        [PingID.Generic] = "Generic",
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
            if (GameProperties.InMission || IntermissionSystem.IsAwaitingNewMission || ChatSystem.ActiveHandle)
                return;
            IngamePing.CreateFromTankSender(MatrixUtils.GetWorldPosition(MouseUtils.MousePosition), PingID.Generic, NetPlay.GetMyClientId(), Client.IsConnected());
        }
    };
    public static Keybind PingStay = new(nameof(PingStay), Keys.D2) {
        KeybindPressAction = (bind) => {
            if (GameProperties.InMission || IntermissionSystem.IsAwaitingNewMission || ChatSystem.ActiveHandle)
                return;
            IngamePing.CreateFromTankSender(MatrixUtils.GetWorldPosition(MouseUtils.MousePosition), PingID.StayHere, NetPlay.GetMyClientId(), Client.IsConnected());
        }
    };
    public static Keybind PingWatch = new(nameof(PingWatch), Keys.D3) {
        KeybindPressAction = (bind) => {
            if (GameProperties.InMission || IntermissionSystem.IsAwaitingNewMission || ChatSystem.ActiveHandle)
                return;
            IngamePing.CreateFromTankSender(MatrixUtils.GetWorldPosition(MouseUtils.MousePosition), PingID.WatchHere, NetPlay.GetMyClientId(), Client.IsConnected());
        }
    };
    public static Keybind PingAvoid = new(nameof(PingAvoid), Keys.D4) {
        KeybindPressAction = (bind) => {
            if (GameProperties.InMission || IntermissionSystem.IsAwaitingNewMission || ChatSystem.ActiveHandle)
                return;
            IngamePing.CreateFromTankSender(MatrixUtils.GetWorldPosition(MouseUtils.MousePosition), PingID.AvoidHere, NetPlay.GetMyClientId(), Client.IsConnected());
        }
    };
    public static Keybind PingGo = new(nameof(PingGo), Keys.D5) {
        KeybindPressAction = (bind) => {
            if (GameProperties.InMission || IntermissionSystem.IsAwaitingNewMission || ChatSystem.ActiveHandle)
                return;
            IngamePing.CreateFromTankSender(MatrixUtils.GetWorldPosition(MouseUtils.MousePosition), PingID.GoHere, NetPlay.GetMyClientId(), Client.IsConnected());
        }
    };
    public static Keybind PingFocus = new(nameof(PingFocus), Keys.D6) {
        KeybindPressAction = (bind) => {
            if (GameProperties.InMission || IntermissionSystem.IsAwaitingNewMission || ChatSystem.ActiveHandle)
                return;
            IngamePing.CreateFromTankSender(MatrixUtils.GetWorldPosition(MouseUtils.MousePosition), PingID.FocusHere, NetPlay.GetMyClientId(), Client.IsConnected());
        }
    };
    public static Keybind PingGroup = new(nameof(PingGroup), Keys.D7) {
        KeybindPressAction = (bind) => {
            if (GameProperties.InMission || IntermissionSystem.IsAwaitingNewMission || ChatSystem.ActiveHandle)
                return;
            IngamePing.CreateFromTankSender(MatrixUtils.GetWorldPosition(MouseUtils.MousePosition), PingID.GroupHere, NetPlay.GetMyClientId(), Client.IsConnected());
        }
    };
    // for now just a graphic on the top right

    public static void DrawPingHUD() {
        float offX = new();
        float scale = 0.2f;
        var cornerOff = new Vector2(-90, -120);
        var padding = 10f;
        for (int i = PingIdToName.Count - 1; i >= 0; i--) {
            var pos = WindowUtils.WindowBottomRight + cornerOff + new Vector2(offX, 0);
            SpriteFontUtils.DrawBorderedText(TankGame.SpriteRenderer, TankGame.TextFontLarge, $"{i + 1}. {PingIdToName[i]}",
                pos, PlayerID.PlayerTankColors[NetPlay.GetMyClientId()].ToColor(), Color.White,
                Vector2.One * 0.75f * scale, 0f, Anchor.TopCenter);
            TankGame.SpriteRenderer.Draw(PingIdToTexture[i], pos + new Vector2(0, 20), null, Color.White, 0f, 
                Anchor.TopCenter.GetAnchor(PingIdToTexture[i].Size()), 1f * scale, default, 0f);
            offX -= PingIdToTexture[i].Width * scale + padding;
        }
    }
    // i dont think a radial would be optimal. Scroll wheel would even be better
    //public static Radial RadialMenu = new(2, new Circle());

    //private static int _curPingId;

    // todo: finish impl
}
