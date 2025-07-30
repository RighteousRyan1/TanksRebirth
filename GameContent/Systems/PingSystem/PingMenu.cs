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
using TanksRebirth.Internals.Common;

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
        OnPress = () => {
            if (MainMenuUI.IsActive || LevelEditorUI.IsActive || !CampaignGlobals.ShouldMissionsProgress || ChatSystem.ActiveHandle)
                return;
            IngamePing.CreateFromTankSender(MatrixUtils.GetWorldPosition(MouseUtils.MousePosition), PingID.Generic, NetPlay.GetMyClientId(), Client.IsConnected());
        }
    };
    public static Keybind PingStay = new(nameof(PingStay), Keys.D2) {
        OnPress = () => {
            if (MainMenuUI.IsActive || LevelEditorUI.IsActive || !CampaignGlobals.ShouldMissionsProgress || ChatSystem.ActiveHandle)
                return;
            IngamePing.CreateFromTankSender(MatrixUtils.GetWorldPosition(MouseUtils.MousePosition), PingID.StayHere, NetPlay.GetMyClientId(), Client.IsConnected());
        }
    };
    public static Keybind PingWatch = new(nameof(PingWatch), Keys.D3) {
        OnPress = () => {
            if (MainMenuUI.IsActive || LevelEditorUI.IsActive || !CampaignGlobals.ShouldMissionsProgress || ChatSystem.ActiveHandle)
                return;
            IngamePing.CreateFromTankSender(MatrixUtils.GetWorldPosition(MouseUtils.MousePosition), PingID.WatchHere, NetPlay.GetMyClientId(), Client.IsConnected());
        }
    };
    public static Keybind PingAvoid = new(nameof(PingAvoid), Keys.D4) {
        OnPress = () => {
            if (MainMenuUI.IsActive || LevelEditorUI.IsActive || !CampaignGlobals.ShouldMissionsProgress || ChatSystem.ActiveHandle)
                return;
            IngamePing.CreateFromTankSender(MatrixUtils.GetWorldPosition(MouseUtils.MousePosition), PingID.AvoidHere, NetPlay.GetMyClientId(), Client.IsConnected());
        }
    };
    public static Keybind PingGo = new(nameof(PingGo), Keys.D5) {
        OnPress = () => {
            if (MainMenuUI.IsActive || LevelEditorUI.IsActive || !CampaignGlobals.ShouldMissionsProgress || ChatSystem.ActiveHandle)
                return;
            IngamePing.CreateFromTankSender(MatrixUtils.GetWorldPosition(MouseUtils.MousePosition), PingID.GoHere, NetPlay.GetMyClientId(), Client.IsConnected());
        }
    };
    public static Keybind PingFocus = new(nameof(PingFocus), Keys.D6) {
        OnPress = () => {
            if (MainMenuUI.IsActive || LevelEditorUI.IsActive || !CampaignGlobals.ShouldMissionsProgress || ChatSystem.ActiveHandle)
                return;
            IngamePing.CreateFromTankSender(MatrixUtils.GetWorldPosition(MouseUtils.MousePosition), PingID.FocusHere, NetPlay.GetMyClientId(), Client.IsConnected());
        }
    };
    public static Keybind PingGroup = new(nameof(PingGroup), Keys.D7) {
        OnPress = () => {
            if (MainMenuUI.IsActive || LevelEditorUI.IsActive || !CampaignGlobals.ShouldMissionsProgress || ChatSystem.ActiveHandle)
                return;
            IngamePing.CreateFromTankSender(MatrixUtils.GetWorldPosition(MouseUtils.MousePosition), PingID.GroupHere, NetPlay.GetMyClientId(), Client.IsConnected());
        }
    };
    // for now just a graphic on the top right

    static float _uiOpacity;
    static int _pickedPingId;
    public static void DrawPingHUD() {
        if (InputUtils.MouseMiddle && !InputUtils.OldMouseMiddle) {
            if (MainMenuUI.IsActive || LevelEditorUI.IsActive || !CampaignGlobals.ShouldMissionsProgress || ChatSystem.ActiveHandle)
                return;
            IngamePing.CreateFromTankSender(MatrixUtils.GetWorldPosition(MouseUtils.MousePosition), _pickedPingId, NetPlay.GetMyClientId(), Client.IsConnected());
        }

        _pickedPingId = Math.Abs(InputUtils.DeltaScrollWheel) % 7;

        if (_pickedPingId >= PingIdToName.Count)
            _pickedPingId = 0;
        else if (_pickedPingId < 0)
            _pickedPingId = PingIdToName.Count - 1;

        float offY = 0f;
        float scale = 0.15f;
        var basePos = WindowUtils.WindowRight - new Vector2(60, 300).ToResolution();
        var padding = 10f;
        var rect = new Rectangle() {
            X = (int)(basePos.X - padding * 4),
            Y = (int)basePos.Y,
            Width = 0
        };
        for (int i = 0; i < PingIdToTexture.Count; i++) {
            var texture = PingIdToTexture[i];

            if (texture.Width > rect.Width) rect.Width = (int)(texture.Width * scale);

            rect.Height += (int)(texture.Height * scale + padding);

            var isPicked = i == _pickedPingId;

            float pickOpacity = isPicked ? 0.8f : _uiOpacity;

            // draw the texture and the name of the ping type
            var pos = basePos + new Vector2(0, offY);
            DrawUtils.DrawStringWithBorder(TankGame.SpriteRenderer, FontGlobals.RebirthFontLarge, $"{i + 1}. {PingIdToName[i]}",
                pos, PlayerID.PlayerTankColors[NetPlay.GetMyClientId()] * pickOpacity, Color.White * pickOpacity,
                scale.ToResolution(), 0f, Anchor.TopCenter, 0.75f);
            TankGame.SpriteRenderer.Draw(PingIdToTexture[i], pos + new Vector2(0, 15), null, Color.White * pickOpacity, 0f,
                Anchor.TopCenter.GetAnchor(PingIdToTexture[i].Size()), scale.ToResolution(), default, 0f);

            offY += (PingIdToTexture[i].Height * scale + padding).ToResolutionX();
        }
        if (rect.Contains(MouseUtils.MouseX, MouseUtils.MouseY)) {
            _uiOpacity += 0.04f * RuntimeData.DeltaTime;
            if (_uiOpacity > 0.85f) _uiOpacity = 0.85f;
        }
        else {
            _uiOpacity -= 0.04f * RuntimeData.DeltaTime;
            if (_uiOpacity < 0.1f) _uiOpacity = 0.1f;
        }
    }
}
    // i dont think a radial would be optimal. Scroll wheel would even be better
    //public static Radial RadialMenu = new(2, new Circle());

    //private static int _curPingId;

    // todo: finish impl
