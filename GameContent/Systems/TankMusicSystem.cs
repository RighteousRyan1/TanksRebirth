using WiiPlayTanksRemake;
using WiiPlayTanksRemake.GameContent;
using System;
using System.Linq;
using WiiPlayTanksRemake.Internals.Core.Interfaces;
using WiiPlayTanksRemake.Enums;
using Microsoft.Xna.Framework;
using WiiPlayTanksRemake.Internals.Common.Utilities;
using System.IO;

namespace WiiPlayTanksRemake.GameContent.Systems
{
    public static class TankMusicSystem
    {
        public static TankTier TierHighest => AITank.GetHighestTierActive();

        public static float totalSpike;

        public static float brownSpike;
        public static float ashSpike;
        public static float marineSpike;
        public static float pinkSpike;
        public static float greenSpike;
        public static float purpleSpike;
        public static float whiteSpike;
        public static float blackSpike;

        public static void Update()
        {
            /*brownSpike += 0.5f * Tank.GetTankCountOfType(TankTier.Brown);
            ashSpike += 1f * Tank.GetTankCountOfType(TankTier.Ash);
            brownSpike = GameUtils.Clamp(brownSpike, 0f, 1f);
            ashSpike = GameUtils.Clamp(ashSpike, 0f, 2f);
            marineSpike = GameUtils.Clamp(marineSpike, 0f, 4f);
            pinkSpike = GameUtils.Clamp(pinkSpike, 0f, 8f);
            greenSpike = GameUtils.Clamp(greenSpike, 0f, 16f);
            purpleSpike = GameUtils.Clamp(purpleSpike, 0f, 24f);
            whiteSpike = GameUtils.Clamp(whiteSpike, 0f, 30f);
            blackSpike = GameUtils.Clamp(blackSpike, 0f, 50f);
            totalSpike = brownSpike + ashSpike + marineSpike + pinkSpike + greenSpike + purpleSpike + whiteSpike + blackSpike;
            if (TierHighest == TankTier.Brown)
                brown.volume = 0.5f;
            else if (TierHighest == TankTier.Ash)
            {
                if (totalSpike > 2f)
                    ash2.volume = 0.5f;
                else
                    ash1.volume = 0.5f;
            }*/

            var musicVolume = SoundPlayer.MusicVolume;

            brown.volume = 0;
            ash1.volume = 0;
            ash2.volume = 0;
            marine1.volume = 0;
            marine2.volume = 0;
            yellow1.volume = 0;
            yellow2.volume = 0;
            yellow3.volume = 0;
            pink1.volume = 0;
            pink2.volume = 0;
            pink3.volume = 0;
            green1.volume = 0;
            green2.volume = 0;
            green3.volume = 0;
            green4.volume = 0;
            purple1.volume = 0;
            purple2.volume = 0;
            purple3.volume = 0;
            white1.volume = 0;
            white2.volume = 0;
            white3.volume = 0;
            black.volume = 0;

            if (TierHighest == TankTier.Brown)
                brown.volume = 0.5f * musicVolume;

            if (TierHighest == TankTier.Ash && AITank.GetTankCountOfType(TankTier.Ash) == 1)
                ash1.volume = 0.5f * musicVolume;
            else if (TierHighest == TankTier.Ash && (AITank.GetTankCountOfType(TankTier.Ash) >= 3)) //|| Tank.GetTankCountOfType(TankTier.Brown) >= 2))
                ash2.volume = 0.5f * musicVolume;

            if (TierHighest == TankTier.Marine && AITank.GetTankCountOfType(TankTier.Marine) == 1)
                marine1.volume = 0.5f * musicVolume;
            else if (TierHighest == TankTier.Marine && (AITank.GetTankCountOfType(TankTier.Marine) >= 2)) //|| Tank.GetTankCountOfType(TankTier.Brown | TankTier.Ash) >= 2))
                marine2.volume = 0.5f * musicVolume;

            if (TierHighest == TankTier.Yellow && AITank.GetTankCountOfType(TankTier.Yellow) == 1) //&& Tank.GetTankCountOfType(TankTier.Marine) == 0)
                yellow1.volume = 0.5f * musicVolume;
            else if (TierHighest == TankTier.Yellow && (AITank.GetTankCountOfType(TankTier.Yellow) == 2)) //|| Tank.GetTankCountOfType(TankTier.Marine) == 1))
                yellow2.volume = 0.5f * musicVolume;
            else if (TierHighest == TankTier.Yellow && (AITank.GetTankCountOfType(TankTier.Yellow) >= 3)) //|| Tank.GetTankCountOfType(TankTier.Marine) >= 3))
                yellow3.volume = 0.5f * musicVolume;

            if (TierHighest == TankTier.Pink && AITank.GetTankCountOfType(TankTier.Pink) == 1) //&& Tank.GetTankCountOfType(TankTier.Marine | TankTier.Yellow) == 0)
                pink1.volume = 0.5f * musicVolume;
            else if (TierHighest == TankTier.Pink && (AITank.GetTankCountOfType(TankTier.Pink) == 2)) //|| Tank.GetTankCountOfType(TankTier.Marine | TankTier.Yellow) == 1))
                pink2.volume = 0.5f * musicVolume;
            else if (TierHighest == TankTier.Pink && (AITank.GetTankCountOfType(TankTier.Pink) >= 3)) //|| Tank.GetTankCountOfType(TankTier.Marine | TankTier.Yellow) >= 2))
                pink3.volume = 0.5f * musicVolume;

            if (TierHighest == TankTier.Green && AITank.GetTankCountOfType(TankTier.Green) == 1) //&& Tank.GetTankCountOfType(TankTier.Yellow | TankTier.Pink) == 0)
                green1.volume = 0.5f * musicVolume;
            else if (TierHighest == TankTier.Green && (AITank.GetTankCountOfType(TankTier.Green) == 2)) //|| Tank.GetTankCountOfType(TankTier.Yellow | TankTier.Pink) == 1))
                green2.volume = 0.5f * musicVolume;
            else if (TierHighest == TankTier.Green && (AITank.GetTankCountOfType(TankTier.Green) == 3)) //|| Tank.GetTankCountOfType(TankTier.Yellow | TankTier.Pink) == 3))
                green3.volume = 0.5f * musicVolume;
            else if (TierHighest == TankTier.Green && (AITank.GetTankCountOfType(TankTier.Green) >= 4)) //|| Tank.GetTankCountOfType(TankTier.Yellow | TankTier.Pink) >= 4))
                green4.volume = 0.5f * musicVolume;

            if (TierHighest == TankTier.Purple && AITank.GetTankCountOfType(TankTier.Purple) == 1) //&& Tank.GetTankCountOfType(TankTier.Pink | TankTier.Green) == 0)
                purple1.volume = 0.5f * musicVolume;
            else if (TierHighest == TankTier.Purple && (AITank.GetTankCountOfType(TankTier.Purple) == 2)) //|| Tank.GetTankCountOfType(TankTier.Pink | TankTier.Green) == 1))
                purple2.volume = 0.5f * musicVolume;
            else if (TierHighest == TankTier.Purple && (AITank.GetTankCountOfType(TankTier.Purple) >= 3)) //|| Tank.GetTankCountOfType(TankTier.Pink | TankTier.Green) >= 2))
                purple3.volume = 0.5f * musicVolume;

            if (TierHighest == TankTier.White && AITank.GetTankCountOfType(TankTier.White) == 1) //&& Tank.GetTankCountOfType(TankTier.Green | TankTier.Purple) == 0)
                white1.volume = 0.5f * musicVolume;
            else if (TierHighest == TankTier.White && (AITank.GetTankCountOfType(TankTier.White) == 2)) //|| Tank.GetTankCountOfType(TankTier.Green | TankTier.Purple) == 1))
                white2.volume = 0.5f * musicVolume;
            else if (TierHighest == TankTier.White && (AITank.GetTankCountOfType(TankTier.White) >= 3)) //|| Tank.GetTankCountOfType(TankTier.Green | TankTier.Purple) >= 2))
                white3.volume = 0.5f * musicVolume;

            if (TierHighest == TankTier.Black)
                black.volume = 0.5f * musicVolume;

            // we call this hardcode hell in the west
        }

        public static Music brown;
        public static Music ash1;
        public static Music ash2;
        public static Music marine1;
        public static Music marine2;
        public static Music pink1;
        public static Music pink2;
        public static Music pink3;
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
            marine1, marine2,
            pink1, pink2, pink3,
            yellow1, yellow2, yellow3,
            purple1, purple2, purple3,
            green1, green2, green3, green4,
            white1, white2, white3,
            black
        };

        public static void LoadMusic()
        {

            brown = Music.CreateMusicTrack("BrownTank", "Assets/music/brown", 0.5f);

            ash1 = Music.CreateMusicTrack("AshTank1", "Assets/music/ash1", 0.5f);
            ash2 = Music.CreateMusicTrack("AshTank2", "Assets/music/ash2", 0.5f);

            marine1 = Music.CreateMusicTrack("MarineTank1", "Assets/music/marine1", 0.5f);
            marine2 = Music.CreateMusicTrack("MarineTank2", "Assets/music/marine2", 0.5f);

            yellow1 = Music.CreateMusicTrack("YellowTank1", "Assets/music/yellow1", 0.5f);
            yellow2 = Music.CreateMusicTrack("YellowTank2", "Assets/music/yellow2", 0.5f);
            yellow3 = Music.CreateMusicTrack("YellowTank3", "Assets/music/yellow3", 0.5f);

            pink1 = Music.CreateMusicTrack("PinkTank1", "Assets/music/pink1", 0.5f);
            pink2 = Music.CreateMusicTrack("PinkTank2", "Assets/music/pink2", 0.5f);
            pink3 = Music.CreateMusicTrack("PinkTank3", "Assets/music/pink3", 0.5f);

            green1 = Music.CreateMusicTrack("GreenTank1", "Assets/music/green1", 0.5f);
            green2 = Music.CreateMusicTrack("GreenTank2", "Assets/music/green2", 0.5f);
            green3 = Music.CreateMusicTrack("GreenTank3", "Assets/music/green3", 0.5f);
            green4 = Music.CreateMusicTrack("GreenTank4", "Assets/music/green4", 0.5f);

            purple1 = Music.CreateMusicTrack("PurpleTank1", "Assets/music/purple1", 0.5f);
            purple2 = Music.CreateMusicTrack("PurpleTank2", "Assets/music/purple2", 0.5f);
            purple3 = Music.CreateMusicTrack("PurpleTank3", "Assets/music/purple3", 0.5f);

            white1 = Music.CreateMusicTrack("WhiteTank1", "Assets/music/white1", 0.5f);
            white2 = Music.CreateMusicTrack("WhiteTank2", "Assets/music/white2", 0.5f);
            white3 = Music.CreateMusicTrack("WhiteTank3", "Assets/music/white3", 0.5f);

            black = Music.CreateMusicTrack("BlackTank", "Assets/music/black", 0.5f);
        }

        public static void PlayMusic()
        {
            songs = new Music[]
            {
                brown,
                ash1, ash2,
                marine1, marine2,
                pink1, pink2, pink3,
                yellow1, yellow2, yellow3,
                purple1, purple2, purple3,
                green1, green2, green3, green4,
                white1, white2, white3,
                black
            };

            foreach (var song in songs)
                song.Play();
        }

        public static void PauseAll()
        {
            foreach (var song in songs)
                if (!song.Track.IsPaused())
                    song.Pause();
        }

        public static void ResumeAll()
        {
            foreach (var song in songs)
                song.Play();
        }
    }
}