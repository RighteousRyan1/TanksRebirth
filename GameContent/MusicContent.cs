
using Microsoft.Xna.Framework.Audio;

namespace WiiPlayTanksRemake.GameContent
{
    public static class MusicContent
    {
        public static Music brown;
        public static Music ash1;
        public static Music ash2;
        public static Music teal1;
        public static Music teal2;
        public static Music red1;
        public static Music red2;
        public static Music red3;
        public static Music yellow1;
        public static Music yellow2;
        public static Music yellow3;
        public static Music purple1;
        public static Music purple2;
        public static Music purple3;
        public static Music green1;
        public static Music green2;
        public static Music green3;
        public static Music green4;
        public static Music white1;
        public static Music white2;
        public static Music white3;
        public static Music black;

        public static Music[] songs =
        {
            brown,
            ash1, ash2,
            teal1, teal2,
            red1, red2, red3,
            yellow1, yellow2, yellow3,
            purple1, purple2, purple3,
            green1, green2, green3, green4,
            white1, white2, white3,
            black
        };

        public static void LoadMusic()
        {
            green1 = Music.CreateMusicTrack("Green1", "Assets/music/green1", 1f);
            green1.Play();

            // WiiPlayTanksRemake.BaseLogger.Write(green1, Internals.Logger.LogType.Debug);
        }
    }
}