using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TanksRebirth.GameContent.ID;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common.Framework;
using TanksRebirth.Internals.Common.Framework.Audio;

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

    public const int ALERT = 0;
    public const int FLAT = 1;
    public const int GENERIC = 2;
    public const int NOTICE = 3;

    public static Dictionary<int, OggAudio> PingIdToAudio;

    public static List<OggAudio> PingSounds = new();

    public static void Initialize() {
        static string specialReplace(string s) => s.Replace(' ', '_').ToLower();
        for (int i = 0; i < PingIdToName.Count; i++) {
            PingIdToTexture[i] = GameResources.GetGameResource<Texture2D>($"Assets/textures/ui/ping/{specialReplace(PingIdToName[i])}");
        }
        PingSounds.AddRange(new OggAudio[] {
            new("Content/Assets/sounds/ping/ping_alert.ogg"),
            new("Content/Assets/sounds/ping/ping_flat.ogg"),
            new("Content/Assets/sounds/ping/ping_generic.ogg"),
            new("Content/Assets/sounds/ping/ping_notice.ogg"),
        });
        PingIdToAudio = new() {
            [PingID.Generic] = PingSounds[GENERIC],
            [PingID.StayHere] = PingSounds[NOTICE],
            [PingID.WatchHere] = PingSounds[ALERT],
            [PingID.AvoidHere] = PingSounds[ALERT],
            [PingID.GoHere] = PingSounds[FLAT],
            [PingID.FocusHere] = PingSounds[FLAT],
            [PingID.GroupHere] = PingSounds[NOTICE]
        };
    }
    // i dont think a radial would be optimal. Scroll wheel would even be better
    public static Radial RadialMenu = new(2, new Circle());

    private static int _curPingId;
    
    // todo: finish impl
}
