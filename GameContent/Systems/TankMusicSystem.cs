using WiiPlayTanksRemake;
using WiiPlayTanksRemake.GameContent;
using System;
using System.Linq;
using WiiPlayTanksRemake.Internals.Core.Interfaces;
using WiiPlayTanksRemake.Enums;
using Microsoft.Xna.Framework;
using WiiPlayTanksRemake.Internals.Common.Utilities;
using System.IO;
using WiiPlayTanksRemake.Internals.Common.Framework.Audio;

namespace WiiPlayTanksRemake.GameContent.Systems
{
    public static class TankMusicSystem
    {
        public static TankTier TierHighest => AITank.GetHighestTierActive();

        // TODO: ambience n stuff - remove music in forests

        public static Music forestAmbience;

        public static void Update()
        {
            /*
             * brown = 1 per
             * ash = 2 per
             * marine = 3 per
             * yellow = 4 per
             * pink = 5 per
             * purple = 6 per
             * green = 7 per
             * white = 8 per
             * black = 9 per
             * 
             * the same pattern should apply to master tanks
             * 
             */
            var maxSpike = AITank.GetHighestTierActive() switch
            {
                TankTier.Brown => 1f,
                TankTier.Ash => 5f,
                TankTier.Marine => 10f,
                _ => 1f
            };



            if (MapRenderer.Theme == MapTheme.Forest)
            {
                forestAmbience.volume = TankGame.Instance.Settings.AmbientVolume;
                return;
            }

            forestAmbience.volume = 0;

            var musicVolume = TankGame.Instance.Settings.MusicVolume;

            foreach (var song in songs.Where(sng => sng is not null))
                song.volume = 0f;


            if (TierHighest == TankTier.Brown)
                brown.volume = 0.5f * musicVolume;

            if (TierHighest == TankTier.Ash && AITank.GetTankCountOfType(TankTier.Ash) < 3)
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


            // vanilla above, master below



            if (TierHighest == TankTier.Bronze)
                bronze.volume = 0.5f * musicVolume;

            if (TierHighest == TankTier.Silver && AITank.GetTankCountOfType(TankTier.Silver) < 3)
                silver1.volume = 0.5f * musicVolume;
            else if (TierHighest == TankTier.Silver && (AITank.GetTankCountOfType(TankTier.Silver) >= 3)) //|| Tank.GetTankCountOfType(TankTier.Brown) >= 2))
                silver2.volume = 0.5f * musicVolume;

            if (TierHighest == TankTier.Sapphire && AITank.GetTankCountOfType(TankTier.Sapphire) == 1)
                sapphire1.volume = 0.5f * musicVolume;
            else if (TierHighest == TankTier.Sapphire && (AITank.GetTankCountOfType(TankTier.Sapphire) >= 2)) //|| Tank.GetTankCountOfType(TankTier.Brown | TankTier.Ash) >= 2))
                sapphire2.volume = 0.5f * musicVolume;

            if (TierHighest == TankTier.Citrine && AITank.GetTankCountOfType(TankTier.Citrine) == 1) //&& Tank.GetTankCountOfType(TankTier.Marine) == 0)
                citrine1.volume = 0.5f * musicVolume;
            else if (TierHighest == TankTier.Citrine && (AITank.GetTankCountOfType(TankTier.Citrine) == 2)) //|| Tank.GetTankCountOfType(TankTier.Marine) == 1))
                citrine2.volume = 0.5f * musicVolume;
            else if (TierHighest == TankTier.Citrine && (AITank.GetTankCountOfType(TankTier.Citrine) >= 3)) //|| Tank.GetTankCountOfType(TankTier.Marine) >= 3))
                citrine3.volume = 0.5f * musicVolume;

            if (TierHighest == TankTier.Ruby && AITank.GetTankCountOfType(TankTier.Ruby) == 1) //&& Tank.GetTankCountOfType(TankTier.Marine | TankTier.Yellow) == 0)
                ruby1.volume = 0.5f * musicVolume;
            else if (TierHighest == TankTier.Ruby && (AITank.GetTankCountOfType(TankTier.Ruby) == 2)) //|| Tank.GetTankCountOfType(TankTier.Marine | TankTier.Yellow) == 1))
                ruby2.volume = 0.5f * musicVolume;
            else if (TierHighest == TankTier.Ruby && (AITank.GetTankCountOfType(TankTier.Ruby) >= 3)) //|| Tank.GetTankCountOfType(TankTier.Marine | TankTier.Yellow) >= 2))
                ruby3.volume = 0.5f * musicVolume;

            if (TierHighest == TankTier.Emerald && AITank.GetTankCountOfType(TankTier.Emerald) == 1) //&& Tank.GetTankCountOfType(TankTier.Yellow | TankTier.Pink) == 0)
                emerald1.volume = 0.5f * musicVolume;
            else if (TierHighest == TankTier.Emerald && (AITank.GetTankCountOfType(TankTier.Emerald) == 2)) //|| Tank.GetTankCountOfType(TankTier.Yellow | TankTier.Pink) == 1))
                emerald2.volume = 0.5f * musicVolume;
            else if (TierHighest == TankTier.Emerald && (AITank.GetTankCountOfType(TankTier.Emerald) == 3)) //|| Tank.GetTankCountOfType(TankTier.Yellow | TankTier.Pink) == 3))
                emerald3.volume = 0.5f * musicVolume;
            else if (TierHighest == TankTier.Emerald && (AITank.GetTankCountOfType(TankTier.Emerald) >= 4)) //|| Tank.GetTankCountOfType(TankTier.Yellow | TankTier.Pink) >= 4))
                emerald4.volume = 0.5f * musicVolume;

            if (TierHighest == TankTier.Amethyst && AITank.GetTankCountOfType(TankTier.Amethyst) == 1) //&& Tank.GetTankCountOfType(TankTier.Pink | TankTier.Green) == 0)
                amethyst1.volume = 0.5f * musicVolume;
            else if (TierHighest == TankTier.Amethyst && (AITank.GetTankCountOfType(TankTier.Amethyst) == 2)) //|| Tank.GetTankCountOfType(TankTier.Pink | TankTier.Green) == 1))
                amethyst2.volume = 0.5f * musicVolume;
            else if (TierHighest == TankTier.Amethyst && (AITank.GetTankCountOfType(TankTier.Amethyst) >= 3)) //|| Tank.GetTankCountOfType(TankTier.Pink | TankTier.Green) >= 2))
                amethyst3.volume = 0.5f * musicVolume;

            if (TierHighest == TankTier.Gold && AITank.GetTankCountOfType(TankTier.Gold) == 1) //&& Tank.GetTankCountOfType(TankTier.Green | TankTier.Purple) == 0)
                gold1.volume = 0.5f * musicVolume;
            else if (TierHighest == TankTier.Gold && (AITank.GetTankCountOfType(TankTier.Gold) == 2)) //|| Tank.GetTankCountOfType(TankTier.Green | TankTier.Purple) == 1))
                gold2.volume = 0.5f * musicVolume;
            else if (TierHighest == TankTier.Gold && (AITank.GetTankCountOfType(TankTier.Gold) >= 3)) //|| Tank.GetTankCountOfType(TankTier.Green | TankTier.Purple) >= 2))
                gold3.volume = 0.5f * musicVolume;

            if (TierHighest == TankTier.Obsidian)
                obsidian.volume = 0.5f * musicVolume;

            // we call this hardcode hell in the west
        }

        #region Songs
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

        public static Music bronze;
        public static Music silver1;
        public static Music silver2;
        public static Music sapphire1;
        public static Music sapphire2;
        public static Music ruby1;
        public static Music ruby2;
        public static Music ruby3;
        public static Music citrine1;
        public static Music citrine2;
        public static Music citrine3;
        public static Music amethyst1;
        public static Music amethyst2;
        public static Music amethyst3;
        public static Music emerald1;
        public static Music emerald2;
        public static Music emerald3;
        public static Music emerald4;
        public static Music gold1;
        public static Music gold2;
        public static Music gold3;
        public static Music obsidian;
        #endregion

        public static Music[] songs;

        public static void LoadMusic()
        {
            #region Load
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




            bronze = Music.CreateMusicTrack("BronzeTank", "Assets/music/bronze", 0.5f);

            silver1 = Music.CreateMusicTrack("SilverTank1", "Assets/music/silver1", 0.5f);
            silver2 = Music.CreateMusicTrack("SilverTank2", "Assets/music/silver2", 0.5f);

            sapphire1 = Music.CreateMusicTrack("SapphireTank1", "Assets/music/sapphire1", 0.5f);
            sapphire2 = Music.CreateMusicTrack("SapphireTank2", "Assets/music/sapphire2", 0.5f);

            ruby1 = Music.CreateMusicTrack("RubyTank1", "Assets/music/ruby1", 0.5f);
            ruby2 = Music.CreateMusicTrack("RubyTank2", "Assets/music/ruby2", 0.5f); 
            ruby3 = Music.CreateMusicTrack("RubyTank3", "Assets/music/ruby3", 0.5f);

            citrine1 = Music.CreateMusicTrack("CitrineTank1", "Assets/music/citrine1", 0.5f);
            citrine2 = Music.CreateMusicTrack("CitrineTank2", "Assets/music/citrine2", 0.5f);
            citrine3 = Music.CreateMusicTrack("CitrineTank3", "Assets/music/citrine3", 0.5f);

            amethyst1 = Music.CreateMusicTrack("AmethystTank1", "Assets/music/amethyst1", 0.5f);
            amethyst2 = Music.CreateMusicTrack("AmethystTank2", "Assets/music/amethyst2", 0.5f);
            amethyst3 = Music.CreateMusicTrack("AmethystTank3", "Assets/music/amethyst3", 0.5f);

            emerald1 = Music.CreateMusicTrack("EmeraldTank1", "Assets/music/emerald1", 0.5f);
            emerald2 = Music.CreateMusicTrack("EmeraldTank2", "Assets/music/emerald2", 0.5f);
            emerald3 = Music.CreateMusicTrack("EmeraldTank3", "Assets/music/emerald3", 0.5f);
            emerald4 = Music.CreateMusicTrack("EmeraldTank4", "Assets/music/emerald4", 0.5f);

            gold1 = Music.CreateMusicTrack("GoldTank1", "Assets/music/gold1", 0.5f);
            gold2 = Music.CreateMusicTrack("GoldTank2", "Assets/music/gold2", 0.5f);
            gold3 = Music.CreateMusicTrack("GoldTank3", "Assets/music/gold3", 0.5f);

            obsidian = Music.CreateMusicTrack("ObsidianTank", "Assets/music/obsidian", 0.5f);

            #endregion

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
                black,

                bronze,
                silver1, silver2,
                sapphire1, sapphire2,
                ruby1, ruby2, ruby3,
                citrine1, citrine2, citrine3,
                amethyst1, amethyst2, amethyst3,
                emerald1, emerald2, emerald3, emerald4,
                gold1, gold2, gold3,
                obsidian
            };
        }

        public static void LoadAmbienceTracks()
        {
            forestAmbience = Music.CreateMusicTrack("Forest Ambient", "Assets/sounds/ambient/forestnight", 1f);
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
                black,

                bronze,
                silver1, silver2,
                sapphire1, sapphire2,
                ruby1, ruby2, ruby3,
                citrine1, citrine2, citrine3,
                amethyst1, amethyst2, amethyst3,
                emerald1, emerald2, emerald3, emerald4,
                gold1, gold2, gold3,
                obsidian
            };


            foreach (var song in songs)
                song?.Play();

            if (MapRenderer.Theme == MapTheme.Forest)
                forestAmbience?.Play();
        }

        public static void PauseAll()
        {
            forestAmbience?.Pause();
            foreach (var song in songs)
                if (!song.Track.IsPaused())
                    song?.Pause();
        }

        public static void ResumeAll()
        {
            forestAmbience?.Play();
            foreach (var song in songs)
                song?.Play();
        }

        public static void UpdateVolume()
        {
            foreach (var song in songs)
            {
                if (song.volume > 0)
                {
                    song.volume = 0.5f * TankGame.Instance.Settings.MusicVolume;
                    if (MapRenderer.Theme == MapTheme.Forest)
                        forestAmbience.volume = TankGame.Instance.Settings.AmbientVolume;
                    else
                        forestAmbience.volume = 0;
                }
            }
        }
    }
}