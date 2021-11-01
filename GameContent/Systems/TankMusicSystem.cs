using WiiPlayTanksRemake;
using WiiPlayTanksRemake.GameContent;
using System;
using System.Linq;
using WiiPlayTanksRemake.Internals.Core.Interfaces;
using WiiPlayTanksRemake.Enums;
using Microsoft.Xna.Framework;
using WiiPlayTanksRemake.Internals.Common.Utilities;

namespace WiiPlayTanksRemake.GameContent.Systems
{
    public sealed class TankMusicSystem : IGameSystem
    {
        public TankTier TierHighest => Tank.GetHighestTierActive();

        public float totalSpike;

        public float brownSpike;
        public float ashSpike;
        public float marineSpike;
        public float pinkSpike;
        public float greenSpike;
        public float purpleSpike;
        public float whiteSpike;
        public float blackSpike;

        public void Update()
        {
            GameUtils.Clamp(ref brownSpike, 0f, 1f);
            GameUtils.Clamp(ref ashSpike, 0f, 2f);
            GameUtils.Clamp(ref marineSpike, 0f, 4f);
            GameUtils.Clamp(ref pinkSpike, 0f, 8f);
            GameUtils.Clamp(ref greenSpike, 0f, 16f);
            GameUtils.Clamp(ref purpleSpike, 0f, 24f);
            GameUtils.Clamp(ref whiteSpike, 0f, 30f);
            GameUtils.Clamp(ref blackSpike, 0f, 50f);


            foreach (var song in songs.Where(sng => sng is not null))
                song.volume = 0f;

            if (TierHighest == TankTier.Brown)
                brown.volume = 0.5f;

            if (TierHighest == TankTier.Ash && Tank.GetTankCountOfType(TankTier.Ash) == 1)
                ash1.volume = 0.5f;
            else if (TierHighest == TankTier.Ash && (Tank.GetTankCountOfType(TankTier.Ash) >= 2 || Tank.GetTankCountOfType(TankTier.Brown) >= 2))
                ash2.volume = 0.5f;

            if (TierHighest == TankTier.Marine && Tank.GetTankCountOfType(TankTier.Marine) == 1)
                ash1.volume = 0.5f;
            else if (TierHighest == TankTier.Ash && (Tank.GetTankCountOfType(TankTier.Ash) >= 2 || Tank.GetTankCountOfType(TankTier.Brown) >= 2))
                ash2.volume = 0.5f;
        }

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

        public void LoadMusic()
        {
            brown = Music.CreateMusicTrack("BrownTankSong", "Assets/music/brown", 0.5f);
            ash1 = Music.CreateMusicTrack("AshTank1", "Assets/music/ash1", 0.5f);
            ash2 = Music.CreateMusicTrack("AshTank2", "Assets/music/ash2", 0.5f);

            brown.Play();
            ash1.Play();
            ash2.Play();
        }
    }
}