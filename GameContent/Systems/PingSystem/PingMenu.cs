using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TanksRebirth.GameContent.ID;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common.Framework;
using TanksRebirth.Internals.Common.Framework.Audio;
using TanksRebirth.Internals.Common.Framework.Input;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Net;
using TanksRebirth.GameContent.UI.MainMenu;
using TanksRebirth.GameContent.UI.LevelEditor;

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
            if (MainMenuUI.Active || LevelEditorUI.Active || !CampaignGlobals.ShouldMissionsProgress || ChatSystem.ActiveHandle)
                return;
            IngamePing.CreateFromTankSender(MatrixUtils.GetWorldPosition(MouseUtils.MousePosition), PingID.Generic, NetPlay.GetMyClientId(), Client.IsConnected());
        }
    };
    public static Keybind PingStay = new(nameof(PingStay), Keys.D2) {
        KeybindPressAction = (bind) => {
            if (MainMenuUI.Active || LevelEditorUI.Active || !CampaignGlobals.ShouldMissionsProgress || ChatSystem.ActiveHandle)
                return;
            IngamePing.CreateFromTankSender(MatrixUtils.GetWorldPosition(MouseUtils.MousePosition), PingID.StayHere, NetPlay.GetMyClientId(), Client.IsConnected());
        }
    };
    public static Keybind PingWatch = new(nameof(PingWatch), Keys.D3) {
        KeybindPressAction = (bind) => {
            if (MainMenuUI.Active || LevelEditorUI.Active || !CampaignGlobals.ShouldMissionsProgress || ChatSystem.ActiveHandle)
                return;
            IngamePing.CreateFromTankSender(MatrixUtils.GetWorldPosition(MouseUtils.MousePosition), PingID.WatchHere, NetPlay.GetMyClientId(), Client.IsConnected());
        }
    };
    public static Keybind PingAvoid = new(nameof(PingAvoid), Keys.D4) {
        KeybindPressAction = (bind) => {
            if (MainMenuUI.Active || LevelEditorUI.Active || !CampaignGlobals.ShouldMissionsProgress || ChatSystem.ActiveHandle)
                return;
            IngamePing.CreateFromTankSender(MatrixUtils.GetWorldPosition(MouseUtils.MousePosition), PingID.AvoidHere, NetPlay.GetMyClientId(), Client.IsConnected());
        }
    };
    public static Keybind PingGo = new(nameof(PingGo), Keys.D5) {
        KeybindPressAction = (bind) => {
            if (MainMenuUI.Active || LevelEditorUI.Active || !CampaignGlobals.ShouldMissionsProgress || ChatSystem.ActiveHandle)
                return;
            IngamePing.CreateFromTankSender(MatrixUtils.GetWorldPosition(MouseUtils.MousePosition), PingID.GoHere, NetPlay.GetMyClientId(), Client.IsConnected());
        }
    };
    public static Keybind PingFocus = new(nameof(PingFocus), Keys.D6) {
        KeybindPressAction = (bind) => {
            if (MainMenuUI.Active || LevelEditorUI.Active || !CampaignGlobals.ShouldMissionsProgress || ChatSystem.ActiveHandle)
                return;
            IngamePing.CreateFromTankSender(MatrixUtils.GetWorldPosition(MouseUtils.MousePosition), PingID.FocusHere, NetPlay.GetMyClientId(), Client.IsConnected());
        }
    };
    public static Keybind PingGroup = new(nameof(PingGroup), Keys.D7) {
        KeybindPressAction = (bind) => {
            if (MainMenuUI.Active || LevelEditorUI.Active || !CampaignGlobals.ShouldMissionsProgress || ChatSystem.ActiveHandle)
                return;
            IngamePing.CreateFromTankSender(MatrixUtils.GetWorldPosition(MouseUtils.MousePosition), PingID.GroupHere, NetPlay.GetMyClientId(), Client.IsConnected());
        }
    };
    // for now just a graphic on the top right

    private static float _uiOpacity;
    public static void DrawPingHUD() {
        float offX = 0f;
        float scale = 0.15f;
        var cornerOff = new Vector2(-60, -100).ToResolution();
        var padding = 10f;
        var rect = new Rectangle() {
            X = (int)WindowUtils.WindowBottomRight.X - (int)padding,
            Y = (int)(WindowUtils.WindowBottomRight.Y + cornerOff.Y)
        };
        for (int i = 0; i < PingIdToTexture.Count; i++) {
            var texture = PingIdToTexture[i];
            // 20 is y diff between text and texture
            var h = (int)(texture.Height * scale);
            if (h > rect.Height) rect.Height = h + 20;
            int w = (int)((texture.Width * scale) + (int)padding).ToResolutionX();
            rect.Width += w;
            rect.X -= w;
        }
        if (rect.Contains(MouseUtils.MouseX, MouseUtils.MouseY)) {
            _uiOpacity += 0.04f * TankGame.DeltaTime;
            if (_uiOpacity > 1f) _uiOpacity = 1f;
        } else {
            _uiOpacity -= 0.04f * TankGame.DeltaTime;
            if (_uiOpacity < 0.1f) _uiOpacity = 0.1f;
        }
        for (int i = PingIdToName.Count - 1; i >= 0; i--) {
            var pos = WindowUtils.WindowBottomRight + cornerOff + new Vector2(offX, 0);
            DrawUtils.DrawTextWithBorder(TankGame.SpriteRenderer, TankGame.TextFontLarge, $"{i + 1}. {PingIdToName[i]}",
                pos, PlayerID.PlayerTankColors[NetPlay.GetMyClientId()].ToColor() * _uiOpacity, Color.White * _uiOpacity,
                0.75f * scale.ToResolution(), 0f, Anchor.TopCenter);
            TankGame.SpriteRenderer.Draw(PingIdToTexture[i], pos + new Vector2(0, 20), null, Color.White * _uiOpacity, 0f, 
                Anchor.TopCenter.GetAnchor(PingIdToTexture[i].Size()), scale.ToResolution(), default, 0f);
            offX -= (PingIdToTexture[i].Width * scale + padding).ToResolutionX();
        }
    }
    // i dont think a radial would be optimal. Scroll wheel would even be better
    //public static Radial RadialMenu = new(2, new Circle());

    //private static int _curPingId;

    // todo: finish impl
}
