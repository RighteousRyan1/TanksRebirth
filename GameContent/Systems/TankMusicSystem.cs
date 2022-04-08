using TanksRebirth;
using TanksRebirth.GameContent;
using System;
using System.Linq;
using TanksRebirth.Internals.Core.Interfaces;
using TanksRebirth.Enums;
using Microsoft.Xna.Framework;
using TanksRebirth.Internals.Common.Utilities;
using System.IO;
using TanksRebirth.Internals.Common.Framework.Audio;
using MeltySynth;

namespace TanksRebirth.GameContent.Systems
{
    public static class TankMusicSystem
    {
        public static TankTier TierHighest => AITank.GetHighestTierActive();

        // TODO: ambience n stuff - remove music in forests

        public static Music forestAmbience;

        public static void Update()
        {
            if (MapRenderer.Theme == MapTheme.Forest)
            {
                forestAmbience.Volume = TankGame.Settings.AmbientVolume;
                return;
            }

            forestAmbience.Volume = 0;

            var musicVolume = TankGame.Settings.MusicVolume;

            foreach (var song in Songs)
                if (song is not null)
                    song.Volume = 0f;


            if (TierHighest == TankTier.Brown)
                brown.Volume = musicVolume;

            if (TierHighest == TankTier.Ash && AITank.CountAll() < 3)
                ash1.Volume = musicVolume;
            else if (TierHighest == TankTier.Ash && AITank.CountAll() >= 3)
                ash2.Volume = musicVolume;

            if (TierHighest == TankTier.Marine && AITank.CountAll() == 1)
                marine1.Volume = musicVolume;
            else if (TierHighest == TankTier.Marine && AITank.CountAll() >= 2)
                marine2.Volume = musicVolume;

            if (TierHighest == TankTier.Yellow && AITank.CountAll() == 1)
                yellow1.Volume = musicVolume;
            else if (TierHighest == TankTier.Yellow && (AITank.CountAll() == 2))
                yellow2.Volume = musicVolume;
            else if (TierHighest == TankTier.Yellow && (AITank.CountAll() >= 3))
                yellow3.Volume = musicVolume;

            if (TierHighest == TankTier.Pink && AITank.CountAll() == 1)
                pink1.Volume = musicVolume;
            else if (TierHighest == TankTier.Pink && (AITank.CountAll() == 2))
                pink2.Volume = musicVolume;
            else if (TierHighest == TankTier.Pink && (AITank.CountAll() >= 3))
                pink3.Volume = musicVolume;

            if (TierHighest == TankTier.Green && AITank.CountAll() == 1)
                green1.Volume = musicVolume;
            else if (TierHighest == TankTier.Green && (AITank.CountAll() == 2))
                green2.Volume = musicVolume;
            else if (TierHighest == TankTier.Green && (AITank.CountAll() == 3))
                green3.Volume = musicVolume;
            else if (TierHighest == TankTier.Green && (AITank.CountAll() >= 4))
                green4.Volume = musicVolume;

            if (TierHighest == TankTier.Purple && AITank.CountAll() == 1)
                purple1.Volume = musicVolume;
            else if (TierHighest == TankTier.Purple && (AITank.CountAll() == 2))
                purple2.Volume = musicVolume;
            else if (TierHighest == TankTier.Purple && (AITank.CountAll() >= 3))
                purple3.Volume = musicVolume;

            if (TierHighest == TankTier.White && AITank.CountAll() == 1)
                white1.Volume = musicVolume;
            else if (TierHighest == TankTier.White && (AITank.CountAll() == 2))
                white2.Volume = musicVolume;
            else if (TierHighest == TankTier.White && (AITank.CountAll() >= 3))
                white3.Volume = musicVolume;

            if (TierHighest == TankTier.Black)
                black.Volume = musicVolume;


            // vanilla above, master below



            if (TierHighest == TankTier.Bronze)
                bronze.Volume = musicVolume;

            if (TierHighest == TankTier.Silver && AITank.CountAll() < 3)
                silver1.Volume = musicVolume;
            else if (TierHighest == TankTier.Silver && (AITank.CountAll() >= 3))
                silver2.Volume = musicVolume;

            if (TierHighest == TankTier.Sapphire && AITank.CountAll() == 1)
                sapphire1.Volume = musicVolume;
            else if (TierHighest == TankTier.Sapphire && (AITank.CountAll() >= 2))
                sapphire2.Volume = musicVolume;

            if (TierHighest == TankTier.Citrine && AITank.CountAll() == 1)
                citrine1.Volume = musicVolume;
            else if (TierHighest == TankTier.Citrine && (AITank.CountAll() == 2))
                citrine2.Volume = musicVolume;
            else if (TierHighest == TankTier.Citrine && (AITank.CountAll() >= 3))
                citrine3.Volume = musicVolume;

            if (TierHighest == TankTier.Ruby && AITank.CountAll() == 1)
                ruby1.Volume = musicVolume;
            else if (TierHighest == TankTier.Ruby && (AITank.CountAll() == 2))
                ruby2.Volume = musicVolume;
            else if (TierHighest == TankTier.Ruby && (AITank.CountAll() >= 3))
                ruby3.Volume = musicVolume;

            if (TierHighest == TankTier.Emerald && AITank.CountAll() == 1)
                emerald1.Volume = musicVolume;
            else if (TierHighest == TankTier.Emerald && (AITank.CountAll() == 2))
                emerald2.Volume = musicVolume;
            else if (TierHighest == TankTier.Emerald && (AITank.CountAll() == 3))
                emerald3.Volume = musicVolume;
            else if (TierHighest == TankTier.Emerald && (AITank.CountAll() >= 4))
                emerald4.Volume = musicVolume;

            if (TierHighest == TankTier.Amethyst && AITank.CountAll() == 1)
                amethyst1.Volume = musicVolume;
            else if (TierHighest == TankTier.Amethyst && (AITank.CountAll() == 2))
                amethyst2.Volume = musicVolume;
            else if (TierHighest == TankTier.Amethyst && (AITank.CountAll() >= 3))
                amethyst3.Volume = musicVolume;

            if (TierHighest == TankTier.Gold && AITank.CountAll() == 1)
                gold1.Volume = musicVolume;
            else if (TierHighest == TankTier.Gold && (AITank.CountAll() == 2))
                gold2.Volume = musicVolume;
            else if (TierHighest == TankTier.Gold && (AITank.CountAll() >= 3))
                gold3.Volume = musicVolume;

            if (TierHighest == TankTier.Obsidian)
                obsidian.Volume = musicVolume;


            if (TierHighest == TankTier.Granite)
                granite.Volume = musicVolume;

            if (TierHighest == TankTier.Bubblegum && AITank.CountAll() < 3)
                bubblegum1.Volume = musicVolume;
            else if (TierHighest == TankTier.Bubblegum && (AITank.CountAll() >= 3))
                bubblegum2.Volume = musicVolume;

            if (TierHighest == TankTier.Water && AITank.CountAll() == 1)
                water1.Volume = musicVolume;
            else if (TierHighest == TankTier.Water && (AITank.CountAll() >= 2))
                water2.Volume = musicVolume;

            if (TierHighest == TankTier.Tiger && AITank.CountAll() == 1)
                tiger1.Volume = musicVolume;
            else if (TierHighest == TankTier.Tiger && (AITank.CountAll() == 2))
                tiger2.Volume = musicVolume;
            else if (TierHighest == TankTier.Tiger && (AITank.CountAll() >= 3))
                tiger3.Volume = musicVolume;

            if (TierHighest == TankTier.Crimson && AITank.CountAll() == 1)
                crimson1.Volume = musicVolume;
            else if (TierHighest == TankTier.Crimson && (AITank.CountAll() == 2))
                crimson2.Volume = musicVolume;
            else if (TierHighest == TankTier.Crimson && (AITank.CountAll() >= 3))
                crimson3.Volume = musicVolume;
            
            if (TierHighest == TankTier.Creeper && AITank.CountAll() == 1)
                creeper1.Volume = musicVolume;
            else if (TierHighest == TankTier.Creeper && (AITank.CountAll() == 2))
                creeper2.Volume = musicVolume;
            else if (TierHighest == TankTier.Creeper && (AITank.CountAll() == 3))
                creeper3.Volume = musicVolume;
            else if (TierHighest == TankTier.Creeper && (AITank.CountAll() >= 4))
                creeper4.Volume = musicVolume;

            if (TierHighest == TankTier.Fade && AITank.CountAll() == 1)
                fade1.Volume = musicVolume;
            else if (TierHighest == TankTier.Fade && (AITank.CountAll() == 2))
                fade2.Volume = musicVolume;
            else if (TierHighest == TankTier.Fade && (AITank.CountAll() >= 3))
                fade3.Volume = musicVolume;

            if (TierHighest == TankTier.Gamma && AITank.CountAll() == 1)
                gamma1.Volume = musicVolume;
            else if (TierHighest == TankTier.Gamma && (AITank.CountAll() == 2))
                gamma2.Volume = musicVolume;
            else if (TierHighest == TankTier.Gamma && (AITank.CountAll() >= 3))
                gamma3.Volume = musicVolume;

            if (TierHighest == TankTier.Marble)
                marble.Volume = musicVolume;


            if (TierHighest == TankTier.Assassin)
                assassin.Volume = musicVolume;

            if (TierHighest == TankTier.Commando)
                commando.Volume = musicVolume;

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
        
        public static Music granite;
        public static Music bubblegum1;
        public static Music bubblegum2;
        public static Music water1;
        public static Music water2;
        public static Music crimson1;
        public static Music crimson2;
        public static Music crimson3;
        public static Music tiger1;
        public static Music tiger2;
        public static Music tiger3;
        public static Music fade1;
        public static Music fade2;
        public static Music fade3;
        public static Music creeper1;
        public static Music creeper2;
        public static Music creeper3;
        public static Music creeper4;
        public static Music gamma1;
        public static Music gamma2;
        public static Music gamma3;
        public static Music marble;
        
        public static Music commando;

        public static Music assassin;
        #endregion

        public static Music[] Songs;

        public static MidiPlayer MusicMidi;
        public static MidiFile MusicSoundFont;

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

            // initialize songs in order, using base path "Assets/music/marble"
            granite = Music.CreateMusicTrack("GraniteTank", "Assets/music/marble/granite", 0.5f);
            
            bubblegum1 = Music.CreateMusicTrack("BubblegumTank1", "Assets/music/marble/bubblegum1", 0.5f);
            bubblegum2 = Music.CreateMusicTrack("BubblegumTank2", "Assets/music/marble/bubblegum2", 0.5f);
            
            water1 = Music.CreateMusicTrack("WaterTank1", "Assets/music/marble/water1", 0.5f);
            water2 = Music.CreateMusicTrack("WaterTank2", "Assets/music/marble/water2", 0.5f);

            crimson1 = Music.CreateMusicTrack("CrimsonTank1", "Assets/music/marble/crimson1", 0.5f);
            crimson2 = Music.CreateMusicTrack("CrimsonTank2", "Assets/music/marble/crimson2", 0.5f);
            crimson3 = Music.CreateMusicTrack("CrimsonTank3", "Assets/music/marble/crimson3", 0.5f);

            tiger1 = Music.CreateMusicTrack("TigerTank1", "Assets/music/marble/tiger1", 0.5f);
            tiger2 = Music.CreateMusicTrack("TigerTank2", "Assets/music/marble/tiger2", 0.5f);
            tiger3 = Music.CreateMusicTrack("TigerTank3", "Assets/music/marble/tiger3", 0.5f);
            
            fade1 = Music.CreateMusicTrack("FadeTank1", "Assets/music/marble/fade1", 0.5f);
            fade2 = Music.CreateMusicTrack("FadeTank2", "Assets/music/marble/fade2", 0.5f);
            fade3 = Music.CreateMusicTrack("FadeTank3", "Assets/music/marble/fade3", 0.5f);

            creeper1 = Music.CreateMusicTrack("CreeperTank1", "Assets/music/marble/creeper1", 0.5f);
            creeper2 = Music.CreateMusicTrack("CreeperTank2", "Assets/music/marble/creeper2", 0.5f);
            creeper3 = Music.CreateMusicTrack("CreeperTank3", "Assets/music/marble/creeper3", 0.5f);
            creeper4 = Music.CreateMusicTrack("CreeperTank4", "Assets/music/marble/creeper4", 0.5f);

            gamma1 = Music.CreateMusicTrack("GammaTank1", "Assets/music/marble/gamma1", 0.5f);
            gamma2 = Music.CreateMusicTrack("GammaTank2", "Assets/music/marble/gamma2", 0.5f);
            gamma3 = Music.CreateMusicTrack("GammaTank3", "Assets/music/marble/gamma3", 0.5f);
            
            marble = Music.CreateMusicTrack("MarbleTank", "Assets/music/marble/marble", 0.5f);


            obsidian = Music.CreateMusicTrack("ObsidianTank", "Assets/music/obsidian", 0.5f);

            assassin = Music.CreateMusicTrack("AssassinTank", "Assets/music/assassin", 0.5f);

            commando = Music.CreateMusicTrack("CommandoTank", "Assets/music/commando", 0.5f);

            #endregion

            //MusicMidi = new MidiPlayer(@"C:\Users\ryanr\Desktop\Git Repositories\WiiPlayTanksRemake\Content\Assets\music\Wii_tanks_bgm.sf2", new(44100) { EnableReverbAndChorus = false });
            // MusicSoundFont = new MidiFile(@"C:\Users\ryanr\Desktop\Git Repositories\WiiPlayTanksRemake\Content\Assets\music\Wii_tanks_bgm.mid", 3200);

            Songs = new Music[]
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
                obsidian,

                granite,
                bubblegum1, bubblegum2,
                water1, water2,
                crimson1, crimson2, crimson3,
                tiger1, tiger2, tiger3,
                fade1, fade2, fade3,
                creeper1, creeper2, creeper3, creeper4,
                gamma1, gamma2, gamma3,
                marble,

                assassin,

                commando
            };
        }

        public static void LoadAmbienceTracks()
        {
            forestAmbience = Music.CreateMusicTrack("Forest Ambient", "Assets/sounds/ambient/forestnight", 1f);
        }

        public static void PlayAll()
        {
            //if (MusicMidi.State == Microsoft.Xna.Framework.Audio.SoundState.Stopped)
                //MusicMidi.Play(MusicSoundFont, true);

            // MusicMidi.NoteOffAll();


            foreach (var song in Songs)
                song?.Play();

            if (MapRenderer.Theme == MapTheme.Forest)
                forestAmbience?.Play();
        }

        public static void PauseAll()
        {
            forestAmbience?.Pause();
            foreach (var song in Songs)
                //if (!song.IsPaused())
                    song?.Pause();
        }

        public static void ResumeAll()
        {
            forestAmbience?.Play();
            foreach (var song in Songs)
                song?.Play();
        }

        public static void StopAll()
        {
            forestAmbience?.Stop();
            if (Songs is not null)
            {
                foreach (var song in Songs)
                    song?.Stop();
            }
        }

        public static void UpdateVolume()
        {
            foreach (var song in Songs)
            {
                if (song.Volume > 0)
                {
                    song.Volume = TankGame.Settings.MusicVolume;
                    if (MapRenderer.Theme == MapTheme.Forest)
                        forestAmbience.Volume = TankGame.Settings.AmbientVolume;
                    else
                        forestAmbience.Volume = 0;
                }
            }
        }
    }
    /*public static class TankMusicSystem
    {
        public static TankTier TierHighest => AITank.GetHighestTierActive();

        // TODO: ambience n stuff - remove music in forests

        public static Music forestAmbience;

        public static void Update()
        {
            if (MapRenderer.Theme == MapTheme.Forest)
            {
                forestAmbience.Volume = TankGame.Settings.AmbientVolume;
                return;
            }

            forestAmbience.Volume = 0;

            var musicVolume = TankGame.Settings.MusicVolume;

            foreach (var song in Songs)
                if (song is not null)
                    song.SetVolume(0f);


            if (TierHighest == TankTier.Brown)
                brown.SetVolume(musicVolume);

            if (TierHighest == TankTier.Ash && AITank.CountAll() < 3)
                ash1.SetVolume(musicVolume);
            else if (TierHighest == TankTier.Ash && AITank.CountAll() >= 3) //|| Tank.GetTankCountOfType(TankTier.Brown) >= 2))
                ash2.SetVolume(musicVolume);

            if (TierHighest == TankTier.Marine && AITank.CountAll() == 1)
                marine1.SetVolume(musicVolume);
            else if (TierHighest == TankTier.Marine && AITank.CountAll() >= 2) //|| Tank.GetTankCountOfType(TankTier.Brown | TankTier.Ash) >= 2))
                marine2.SetVolume(musicVolume);

            if (TierHighest == TankTier.Yellow && AITank.CountAll() == 1) //&& Tank.GetTankCountOfType(TankTier.Marine) == 0)
                yellow1.SetVolume(musicVolume);
            else if (TierHighest == TankTier.Yellow && (AITank.CountAll() == 2)) //|| Tank.GetTankCountOfType(TankTier.Marine) == 1))
                yellow2.SetVolume(musicVolume);
            else if (TierHighest == TankTier.Yellow && (AITank.CountAll() >= 3)) //|| Tank.GetTankCountOfType(TankTier.Marine) >= 3))
                yellow3.SetVolume(musicVolume);

            if (TierHighest == TankTier.Pink && AITank.CountAll() == 1) //&& Tank.GetTankCountOfType(TankTier.Marine | TankTier.Yellow) == 0)
                pink1.SetVolume(musicVolume);
            else if (TierHighest == TankTier.Pink && (AITank.CountAll() == 2)) //|| Tank.GetTankCountOfType(TankTier.Marine | TankTier.Yellow) == 1))
                pink2.SetVolume(musicVolume);
            else if (TierHighest == TankTier.Pink && (AITank.CountAll() >= 3)) //|| Tank.GetTankCountOfType(TankTier.Marine | TankTier.Yellow) >= 2))
                pink3.SetVolume(musicVolume);

            if (TierHighest == TankTier.Green && AITank.CountAll() == 1) //&& Tank.GetTankCountOfType(TankTier.Yellow | TankTier.Pink) == 0)
                green1.SetVolume(musicVolume);
            else if (TierHighest == TankTier.Green && (AITank.CountAll() == 2)) //|| Tank.GetTankCountOfType(TankTier.Yellow | TankTier.Pink) == 1))
                green2.SetVolume(musicVolume);
            else if (TierHighest == TankTier.Green && (AITank.CountAll() == 3)) //|| Tank.GetTankCountOfType(TankTier.Yellow | TankTier.Pink) == 3))
                green3.SetVolume(musicVolume);
            else if (TierHighest == TankTier.Green && (AITank.CountAll() >= 4)) //|| Tank.GetTankCountOfType(TankTier.Yellow | TankTier.Pink) >= 4))
                green4.SetVolume(musicVolume);

            if (TierHighest == TankTier.Purple && AITank.CountAll() == 1) //&& Tank.GetTankCountOfType(TankTier.Pink | TankTier.Green) == 0)
                purple1.SetVolume(musicVolume);
            else if (TierHighest == TankTier.Purple && (AITank.CountAll() == 2)) //|| Tank.GetTankCountOfType(TankTier.Pink | TankTier.Green) == 1))
                purple2.SetVolume(musicVolume);
            else if (TierHighest == TankTier.Purple && (AITank.CountAll() >= 3)) //|| Tank.GetTankCountOfType(TankTier.Pink | TankTier.Green) >= 2))
                purple3.SetVolume(musicVolume);

            if (TierHighest == TankTier.White && AITank.CountAll() == 1) //&& Tank.GetTankCountOfType(TankTier.Green | TankTier.Purple) == 0)
                white1.SetVolume(musicVolume);
            else if (TierHighest == TankTier.White && (AITank.CountAll() == 2)) //|| Tank.GetTankCountOfType(TankTier.Green | TankTier.Purple) == 1))
                white2.SetVolume(musicVolume);
            else if (TierHighest == TankTier.White && (AITank.CountAll() >= 3)) //|| Tank.GetTankCountOfType(TankTier.Green | TankTier.Purple) >= 2))
                white3.SetVolume(musicVolume);

            if (TierHighest == TankTier.Black)
                black.SetVolume(musicVolume);


            // vanilla above, master below



            if (TierHighest == TankTier.Bronze)
                bronze.SetVolume(musicVolume);

            if (TierHighest == TankTier.Silver && AITank.CountAll() < 3)
                silver1.SetVolume(musicVolume);
            else if (TierHighest == TankTier.Silver && (AITank.CountAll() >= 3)) //|| Tank.GetTankCountOfType(TankTier.Brown) >= 2))
                silver2.SetVolume(musicVolume);

            if (TierHighest == TankTier.Sapphire && AITank.CountAll() == 1)
                sapphire1.SetVolume(musicVolume);
            else if (TierHighest == TankTier.Sapphire && (AITank.CountAll() >= 2)) //|| Tank.GetTankCountOfType(TankTier.Brown | TankTier.Ash) >= 2))
                sapphire2.SetVolume(musicVolume);

            if (TierHighest == TankTier.Citrine && AITank.CountAll() == 1) //&& Tank.GetTankCountOfType(TankTier.Marine) == 0)
                citrine1.SetVolume(musicVolume);
            else if (TierHighest == TankTier.Citrine && (AITank.CountAll() == 2)) //|| Tank.GetTankCountOfType(TankTier.Marine) == 1))
                citrine2.SetVolume(musicVolume);
            else if (TierHighest == TankTier.Citrine && (AITank.CountAll() >= 3)) //|| Tank.GetTankCountOfType(TankTier.Marine) >= 3))
                citrine3.SetVolume(musicVolume);

            if (TierHighest == TankTier.Ruby && AITank.CountAll() == 1) //&& Tank.GetTankCountOfType(TankTier.Marine | TankTier.Yellow) == 0)
                ruby1.SetVolume(musicVolume);
            else if (TierHighest == TankTier.Ruby && (AITank.CountAll() == 2)) //|| Tank.GetTankCountOfType(TankTier.Marine | TankTier.Yellow) == 1))
                ruby2.SetVolume(musicVolume);
            else if (TierHighest == TankTier.Ruby && (AITank.CountAll() >= 3)) //|| Tank.GetTankCountOfType(TankTier.Marine | TankTier.Yellow) >= 2))
                ruby3.SetVolume(musicVolume);

            if (TierHighest == TankTier.Emerald && AITank.CountAll() == 1) //&& Tank.GetTankCountOfType(TankTier.Yellow | TankTier.Pink) == 0)
                emerald1.SetVolume(musicVolume);
            else if (TierHighest == TankTier.Emerald && (AITank.CountAll() == 2)) //|| Tank.GetTankCountOfType(TankTier.Yellow | TankTier.Pink) == 1))
                emerald2.SetVolume(musicVolume);
            else if (TierHighest == TankTier.Emerald && (AITank.CountAll() == 3)) //|| Tank.GetTankCountOfType(TankTier.Yellow | TankTier.Pink) == 3))
                emerald3.SetVolume(musicVolume);
            else if (TierHighest == TankTier.Emerald && (AITank.CountAll() >= 4)) //|| Tank.GetTankCountOfType(TankTier.Yellow | TankTier.Pink) >= 4))
                emerald4.SetVolume(musicVolume);

            if (TierHighest == TankTier.Amethyst && AITank.CountAll() == 1) //&& Tank.GetTankCountOfType(TankTier.Pink | TankTier.Green) == 0)
                amethyst1.SetVolume(musicVolume);
            else if (TierHighest == TankTier.Amethyst && (AITank.CountAll() == 2)) //|| Tank.GetTankCountOfType(TankTier.Pink | TankTier.Green) == 1))
                amethyst2.SetVolume(musicVolume);
            else if (TierHighest == TankTier.Amethyst && (AITank.CountAll() >= 3)) //|| Tank.GetTankCountOfType(TankTier.Pink | TankTier.Green) >= 2))
                amethyst3.SetVolume(musicVolume);

            if (TierHighest == TankTier.Gold && AITank.CountAll() == 1) //&& Tank.GetTankCountOfType(TankTier.Green | TankTier.Purple) == 0)
                gold1.SetVolume(musicVolume);
            else if (TierHighest == TankTier.Gold && (AITank.CountAll() == 2)) //|| Tank.GetTankCountOfType(TankTier.Green | TankTier.Purple) == 1))
                gold2.SetVolume(musicVolume);
            else if (TierHighest == TankTier.Gold && (AITank.CountAll() >= 3)) //|| Tank.GetTankCountOfType(TankTier.Green | TankTier.Purple) >= 2))
                gold3.SetVolume(musicVolume);

            if (TierHighest == TankTier.Obsidian)
                obsidian.SetVolume(musicVolume);

            if (TierHighest == TankTier.Assassin)
                assassin.SetVolume(musicVolume);

            // we call this hardcode hell in the west
        }

        #region Songs
        public static OggMusic brown;
        public static OggMusic ash1;
        public static OggMusic ash2;
        public static OggMusic marine1;
        public static OggMusic marine2;
        public static OggMusic pink1;
        public static OggMusic pink2;
        public static OggMusic pink3;
        public static OggMusic yellow1;
        public static OggMusic yellow2;
        public static OggMusic yellow3;
        public static OggMusic purple1;
        public static OggMusic purple2;
        public static OggMusic purple3;
        public static OggMusic green1;
        public static OggMusic green2;
        public static OggMusic green3;
        public static OggMusic green4;
        public static OggMusic white1;
        public static OggMusic white2;
        public static OggMusic white3;
        public static OggMusic black;

        public static OggMusic bronze;
        public static OggMusic silver1;
        public static OggMusic silver2;
        public static OggMusic sapphire1;
        public static OggMusic sapphire2;
        public static OggMusic ruby1;
        public static OggMusic ruby2;
        public static OggMusic ruby3;
        public static OggMusic citrine1;
        public static OggMusic citrine2;
        public static OggMusic citrine3;
        public static OggMusic amethyst1;
        public static OggMusic amethyst2;
        public static OggMusic amethyst3;
        public static OggMusic emerald1;
        public static OggMusic emerald2;
        public static OggMusic emerald3;
        public static OggMusic emerald4;
        public static OggMusic gold1;
        public static OggMusic gold2;
        public static OggMusic gold3;
        public static OggMusic obsidian;

        public static OggMusic assassin;
        #endregion

        public static OggMusic[] Songs;

        // public static OggMusic[] songs = new OggMusic[Directory.GetFiles("Content/assets/music").Length];

        //public static MidiPlayer MusicMidi;
        //public static MidiFile MusicSoundFont;

        private static bool _loaded;

        public static void LoadMusic()
        {
            if (!_loaded)
                _loaded = true;
            else
                return;
            #region Load
            brown = new OggMusic("BrownTank", "Content/Assets/music/brown", 0.5f);

            ash1 = new OggMusic("AshTank1", "Content/Assets/music/ash1", 0.5f);
            ash2 = new OggMusic("AshTank2", "Content/Assets/music/ash2", 0.5f);

            marine1 = new OggMusic("MarineTank1", "Content/Assets/music/marine1", 0.5f);
            marine2 = new OggMusic("MarineTank2", "Content/Assets/music/marine2", 0.5f);

            yellow1 = new OggMusic("YellowTank1", "Content/Assets/music/yellow1", 0.5f);
            yellow2 = new OggMusic("YellowTank2", "Content/Assets/music/yellow2", 0.5f);
            yellow3 = new OggMusic("YellowTank3", "Content/Assets/music/yellow3", 0.5f);

            pink1 = new OggMusic("PinkTank1", "Content/Assets/music/pink1", 0.5f);
            pink2 = new OggMusic("PinkTank2", "Content/Assets/music/pink2", 0.5f);
            pink3 = new OggMusic("PinkTank3", "Content/Assets/music/pink3", 0.5f);

            green1 = new OggMusic("GreenTank1", "Content/Assets/music/green1", 0.5f);
            green2 = new OggMusic("GreenTank2", "Content/Assets/music/green2", 0.5f);
            green3 = new OggMusic("GreenTank3", "Content/Assets/music/green3", 0.5f);
            green4 = new OggMusic("GreenTank4", "Content/Assets/music/green4", 0.5f);

            purple1 = new OggMusic("PurpleTank1", "Content/Assets/music/purple1", 0.5f);
            purple2 = new OggMusic("PurpleTank2", "Content/Assets/music/purple2", 0.5f);
            purple3 = new OggMusic("PurpleTank3", "Content/Assets/music/purple3", 0.5f);

            white1 = new OggMusic("WhiteTank1", "Content/Assets/music/white1", 0.5f);
            white2 = new OggMusic("WhiteTank2", "Content/Assets/music/white2", 0.5f);
            white3 = new OggMusic("WhiteTank3", "Content/Assets/music/white3", 0.5f);

            black = new OggMusic("BlackTank", "Content/Assets/music/black", 0.5f);




            bronze = new OggMusic("BronzeTank", "Content/Assets/music/bronze", 0.5f);

            silver1 = new OggMusic("SilverTank1", "Content/Assets/music/silver1", 0.5f);
            silver2 = new OggMusic("SilverTank2", "Content/Assets/music/silver2", 0.5f);

            sapphire1 = new OggMusic("SapphireTank1", "Content/Assets/music/sapphire1", 0.5f);
            sapphire2 = new OggMusic("SapphireTank2", "Content/Assets/music/sapphire2", 0.5f);

            ruby1 = new OggMusic("RubyTank1", "Content/Assets/music/ruby1", 0.5f);
            ruby2 = new OggMusic("RubyTank2", "Content/Assets/music/ruby2", 0.5f);
            ruby3 = new OggMusic("RubyTank3", "Content/Assets/music/ruby3", 0.5f);

            citrine1 = new OggMusic("CitrineTank1", "Content/Assets/music/citrine1", 0.5f);
            citrine2 = new OggMusic("CitrineTank2", "Content/Assets/music/citrine2", 0.5f);
            citrine3 = new OggMusic("CitrineTank3", "Content/Assets/music/citrine3", 0.5f);

            amethyst1 = new OggMusic("AmethystTank1", "Content/Assets/music/amethyst1", 0.5f);
            amethyst2 = new OggMusic("AmethystTank2", "Content/Assets/music/amethyst2", 0.5f);
            amethyst3 = new OggMusic("AmethystTank3", "Content/Assets/music/amethyst3", 0.5f);

            emerald1 = new OggMusic("EmeraldTank1", "Content/Assets/music/emerald1", 0.5f);
            emerald2 = new OggMusic("EmeraldTank2", "Content/Assets/music/emerald2", 0.5f);
            emerald3 = new OggMusic("EmeraldTank3", "Content/Assets/music/emerald3", 0.5f);
            emerald4 = new OggMusic("EmeraldTank4", "Content/Assets/music/emerald4", 0.5f);

            gold1 = new OggMusic("GoldTank1", "Content/Assets/music/gold1", 0.5f);
            gold2 = new OggMusic("GoldTank2", "Content/Assets/music/gold2", 0.5f);
            gold3 = new OggMusic("GoldTank3", "Content/Assets/music/gold3", 0.5f);

            obsidian = new OggMusic("ObsidianTank", "Content/Assets/music/obsidian", 0.5f);

            assassin = new OggMusic("AssassinTank", "Content/Assets/music/assassin", 0.5f);

            #endregion

            //MusicMidi = new MidiPlayer(@"C:\Users\ryanr\Desktop\Git Repositories\WiiPlayTanksRemake\Content\Assets\music\Wii_tanks_bgm.sf2", new(44100) { EnableReverbAndChorus = false });
            // MusicSoundFont = new MidiFile(@"C:\Users\ryanr\Desktop\Git Repositories\WiiPlayTanksRemake\Content\Assets\music\Wii_tanks_bgm.mid", 3200);

            Songs = new OggMusic[]
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
                obsidian,

                assassin
            };
        }

        public static void LoadAmbienceTracks()
        {
            forestAmbience = Music.CreateMusicTrack("Forest Ambient", "Assets/sounds/ambient/forestnight", 1f);
        }

        public static void PlayAll()
        {
            //if (MusicMidi.State == Microsoft.Xna.Framework.Audio.SoundState.Stopped)
            //MusicMidi.Play(MusicSoundFont, true);

            // MusicMidi.NoteOffAll();


            foreach (var song in Songs)
                song?.Play();

            if (MapRenderer.Theme == MapTheme.Forest)
                forestAmbience?.Play();
        }

        public static void PauseAll()
        {
            forestAmbience?.Pause();
            foreach (var song in Songs)
                if (!song.IsPaused())
                    song?.Pause();
        }

        public static void ResumeAll()
        {
            forestAmbience?.Play();
            foreach (var song in Songs)
                song?.Play();
        }

        public static void StopAll()
        {
            forestAmbience?.Stop();
            if (Songs is not null)
            {
                foreach (var song in Songs)
                    song?.Stop();
            }
        }

        public static void UpdateVolume()
        {
            foreach (var song in Songs)
            {
                if (song.Volume > 0)
                {
                    song.Volume = TankGame.Settings.MusicVolume;
                    if (MapRenderer.Theme == MapTheme.Forest)
                        forestAmbience.Volume = TankGame.Settings.AmbientVolume;
                    else
                        forestAmbience.Volume = 0;
                }
            }
        }
    }    */
}