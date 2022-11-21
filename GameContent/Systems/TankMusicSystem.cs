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
using TanksRebirth.GameContent.ID;

namespace TanksRebirth.GameContent.Systems
{
    public static class TankMusicSystem
    {
        public static int TierHighest => AITank.GetHighestTierActive();

        public static OggMusic SnowLoop;

        public static float Pitch = 0f;
        public static float Pan = 0f;
        public static float VolumeMultiplier = 1f;

        public static void Update()
        {
            Pitch = 0f;
            Pan = 0f;
            if (MapRenderer.Theme == MapTheme.Christmas) {
                SnowLoop.SetVolume(TankGame.Settings.AmbientVolume);
                return;
            }

            SnowLoop.Volume = 0;

            if (GameShaders.LanternMode)
            {
                // Pitch -= 0.05f;
            }

            foreach (var song in Songs)
                if (song is not null)
                    song.SetVolume(0f);

            var musicVolume = TankGame.Settings.MusicVolume * VolumeMultiplier;


            if (TierHighest == TankID.Brown)
                brown.SetVolume(musicVolume);

            if (TierHighest == TankID.Ash && AITank.CountAll() < 3)
                ash1.SetVolume(musicVolume);
            else if (TierHighest == TankID.Ash && AITank.CountAll() >= 3)
                ash2.SetVolume(musicVolume);

            if (TierHighest == TankID.Marine && AITank.CountAll() == 1)
                marine1.SetVolume(musicVolume);
            else if (TierHighest == TankID.Marine && AITank.CountAll() >= 2) 
                marine2.SetVolume(musicVolume);

            if (TierHighest == TankID.Yellow && AITank.CountAll() == 1) 
                yellow1.SetVolume(musicVolume);
            else if (TierHighest == TankID.Yellow && (AITank.CountAll() == 2)) 
                yellow2.SetVolume(musicVolume);
            else if (TierHighest == TankID.Yellow && (AITank.CountAll() >= 3)) 
                yellow3.SetVolume(musicVolume);

            if (TierHighest == TankID.Pink && AITank.CountAll() == 1) 
                pink1.SetVolume(musicVolume);
            else if (TierHighest == TankID.Pink && (AITank.CountAll() == 2)) 
                pink2.SetVolume(musicVolume);
            else if (TierHighest == TankID.Pink && (AITank.CountAll() >= 3)) 
                pink3.SetVolume(musicVolume);

            if (TierHighest == TankID.Green && AITank.CountAll() == 1) 
                green1.SetVolume(musicVolume);
            else if (TierHighest == TankID.Green && (AITank.CountAll() == 2)) 
                green2.SetVolume(musicVolume);
            else if (TierHighest == TankID.Green && (AITank.CountAll() == 3)) 
                green3.SetVolume(musicVolume);
            else if (TierHighest == TankID.Green && (AITank.CountAll() >= 4)) 
                green4.SetVolume(musicVolume);

            if (TierHighest == TankID.Violet && AITank.CountAll() == 1)  
                purple1.SetVolume(musicVolume);
            else if (TierHighest == TankID.Violet && (AITank.CountAll() == 2)) 
                purple2.SetVolume(musicVolume);
            else if (TierHighest == TankID.Violet && (AITank.CountAll() >= 3)) 
                purple3.SetVolume(musicVolume);

            if (TierHighest == TankID.White && AITank.CountAll() == 1) 
                white1.SetVolume(musicVolume);
            else if (TierHighest == TankID.White && (AITank.CountAll() == 2)) 
                white2.SetVolume(musicVolume);
            else if (TierHighest == TankID.White && (AITank.CountAll() >= 3)) 
                white3.SetVolume(musicVolume);

            if (TierHighest == TankID.Black)
                black.SetVolume(musicVolume);


            // vanilla above, master below


            if (TierHighest == TankID.Bronze)
                bronze.SetVolume(musicVolume);

            if (TierHighest == TankID.Silver && AITank.CountAll() < 3)
                silver1.SetVolume(musicVolume);
            else if (TierHighest == TankID.Silver && (AITank.CountAll() >= 3))
                silver2.SetVolume(musicVolume);

            if (TierHighest == TankID.Sapphire && AITank.CountAll() == 1)
                sapphire1.SetVolume(musicVolume);
            else if (TierHighest == TankID.Sapphire && (AITank.CountAll() >= 2))
                sapphire2.SetVolume(musicVolume);

            if (TierHighest == TankID.Citrine && AITank.CountAll() == 1)
                citrine1.SetVolume(musicVolume);
            else if (TierHighest == TankID.Citrine && (AITank.CountAll() == 2))
                citrine2.SetVolume(musicVolume);
            else if (TierHighest == TankID.Citrine && (AITank.CountAll() >= 3))
                citrine3.SetVolume(musicVolume);

            if (TierHighest == TankID.Ruby && AITank.CountAll() == 1)
                ruby1.SetVolume(musicVolume);
            else if (TierHighest == TankID.Ruby && (AITank.CountAll() == 2))
                ruby2.SetVolume(musicVolume);
            else if (TierHighest == TankID.Ruby && (AITank.CountAll() >= 3))
                ruby3.SetVolume(musicVolume);

            if (TierHighest == TankID.Emerald && AITank.CountAll() == 1)
                emerald1.SetVolume(musicVolume);
            else if (TierHighest == TankID.Emerald && (AITank.CountAll() == 2))
                emerald2.SetVolume(musicVolume);
            else if (TierHighest == TankID.Emerald && (AITank.CountAll() == 3))
                emerald3.SetVolume(musicVolume);
            else if (TierHighest == TankID.Emerald && (AITank.CountAll() >= 4))
                emerald4.SetVolume(musicVolume);

            if (TierHighest == TankID.Amethyst && AITank.CountAll() == 1) 
                amethyst1.SetVolume(musicVolume);
            else if (TierHighest == TankID.Amethyst && (AITank.CountAll() == 2))
                amethyst2.SetVolume(musicVolume);
            else if (TierHighest == TankID.Amethyst && (AITank.CountAll() >= 3)) 
                amethyst3.SetVolume(musicVolume);

            if (TierHighest == TankID.Gold && AITank.CountAll() == 1) 
                gold1.SetVolume(musicVolume);
            else if (TierHighest == TankID.Gold && (AITank.CountAll() == 2)) 
                gold2.SetVolume(musicVolume);
            else if (TierHighest == TankID.Gold && (AITank.CountAll() >= 3)) 
                gold3.SetVolume(musicVolume);

            if (TierHighest == TankID.Obsidian)
                obsidian.SetVolume(musicVolume);

            if (TierHighest == TankID.Assassin)
                assassin.SetVolume(musicVolume);

            if (TierHighest == TankID.Granite)
                granite.SetVolume(musicVolume);

            if (TierHighest == TankID.Bubblegum && AITank.CountAll() < 3)
                bubblegum1.SetVolume(musicVolume);
            else if (TierHighest == TankID.Bubblegum && (AITank.CountAll() >= 3))
                bubblegum2.SetVolume(musicVolume);

            if (TierHighest == TankID.Water && AITank.CountAll() == 1)
                water1.SetVolume(musicVolume);
            else if (TierHighest == TankID.Water && (AITank.CountAll() >= 2))
                water2.SetVolume(musicVolume);

            if (TierHighest == TankID.Tiger && AITank.CountAll() == 1)
                tiger1.SetVolume(musicVolume);
            else if (TierHighest == TankID.Tiger && (AITank.CountAll() == 2))
                tiger2.SetVolume(musicVolume);
            else if (TierHighest == TankID.Tiger && (AITank.CountAll() >= 3))
                tiger3.SetVolume(musicVolume);

            if (TierHighest == TankID.Crimson && AITank.CountAll() == 1)
                crimson1.SetVolume(musicVolume);
            else if (TierHighest == TankID.Crimson && (AITank.CountAll() == 2))
                crimson2.SetVolume(musicVolume);
            else if (TierHighest == TankID.Crimson && (AITank.CountAll() >= 3))
                crimson3.SetVolume(musicVolume);

            if (TierHighest == TankID.Creeper && AITank.CountAll() == 1)
                creeper1.SetVolume(musicVolume);
            else if (TierHighest == TankID.Creeper && (AITank.CountAll() == 2))
                creeper2.SetVolume(musicVolume);
            else if (TierHighest == TankID.Creeper && (AITank.CountAll() == 3))
                creeper3.SetVolume(musicVolume);
            else if (TierHighest == TankID.Creeper && (AITank.CountAll() >= 4))
                creeper4.SetVolume(musicVolume);

            if (TierHighest == TankID.Fade && AITank.CountAll() == 1)
                fade1.SetVolume(musicVolume);
            else if (TierHighest == TankID.Fade && (AITank.CountAll() == 2))
                fade2.SetVolume(musicVolume);
            else if (TierHighest == TankID.Fade && (AITank.CountAll() >= 3))
                fade3.SetVolume(musicVolume);

            if (TierHighest == TankID.Gamma && AITank.CountAll() == 1)
                gamma1.SetVolume(musicVolume);
            else if (TierHighest == TankID.Gamma && (AITank.CountAll() == 2))
                gamma2.SetVolume(musicVolume);
            else if (TierHighest == TankID.Gamma && (AITank.CountAll() >= 3))
                gamma3.SetVolume(musicVolume);

            if (TierHighest == TankID.Marble)
                marble.SetVolume(musicVolume);


            if (TierHighest == TankID.Assassin)
                assassin.SetVolume(musicVolume);

            if (TierHighest == TankID.Commando)
                commando.SetVolume(musicVolume);

            int index = Array.FindIndex(Songs, x => x.Volume > 0);

            if (index > -1)
            {
                Songs[index].BackingAudio.Instance.Pitch = Pitch;
                Songs[index].BackingAudio.Instance.Pan = Pan;
                CurrentSong = Songs[index];
            }

            
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

        public static OggMusic granite;
        public static OggMusic bubblegum1;
        public static OggMusic bubblegum2;
        public static OggMusic water1;
        public static OggMusic water2;
        public static OggMusic crimson1;
        public static OggMusic crimson2;
        public static OggMusic crimson3;
        public static OggMusic tiger1;
        public static OggMusic tiger2;
        public static OggMusic tiger3;
        public static OggMusic fade1;
        public static OggMusic fade2;
        public static OggMusic fade3;
        public static OggMusic creeper1;
        public static OggMusic creeper2;
        public static OggMusic creeper3;
        public static OggMusic creeper4;
        public static OggMusic gamma1;
        public static OggMusic gamma2;
        public static OggMusic gamma3;
        public static OggMusic marble;

        public static OggMusic assassin;
        public static OggMusic commando;
        #endregion
        public static OggMusic CurrentSong;

        public static OggMusic[] Songs;

        // public static OggMusic[] songs = new OggMusic[Directory.GetFiles("Content/assets/music").Length];

        //public static MidiPlayer MusicMidi;
        //public static MidiFile MusicSoundFont;

        public static bool IsLoaded;

        public static void LoadMusic()
        {
            if (!IsLoaded)
                IsLoaded = true;
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

            // initialize songs in order, using base path "Assets/music/marble"
            granite = new OggMusic("GraniteTank", "Content/Assets/music/marble/granite", 0.5f);

            bubblegum1 = new OggMusic("BubblegumTank1", "Content/Assets/music/marble/bubblegum1", 0.5f);
            bubblegum2 = new OggMusic("BubblegumTank2", "Content/Assets/music/marble/bubblegum2", 0.5f);

            water1 = new OggMusic("WaterTank1", "Content/Assets/music/marble/water1", 0.5f);
            water2 = new OggMusic("WaterTank2", "Content/Assets/music/marble/water2", 0.5f);

            crimson1 = new OggMusic("CrimsonTank1", "Content/Assets/music/marble/crimson1", 0.5f);
            crimson2 = new OggMusic("CrimsonTank2", "Content/Assets/music/marble/crimson2", 0.5f);
            crimson3 = new OggMusic("CrimsonTank3", "Content/Assets/music/marble/crimson3", 0.5f);

            tiger1 = new OggMusic("TigerTank1", "Content/Assets/music/marble/tiger1", 0.5f);
            tiger2 = new OggMusic("TigerTank2", "Content/Assets/music/marble/tiger2", 0.5f);
            tiger3 = new OggMusic("TigerTank3", "Content/Assets/music/marble/tiger3", 0.5f);

            fade1 = new OggMusic("FadeTank1", "Content/Assets/music/marble/fade1", 0.5f);
            fade2 = new OggMusic("FadeTank2", "Content/Assets/music/marble/fade2", 0.5f);
            fade3 = new OggMusic("FadeTank3", "Content/Assets/music/marble/fade3", 0.5f);

            creeper1 = new OggMusic("CreeperTank1", "Content/Assets/music/marble/creeper1", 0.5f);
            creeper2 = new OggMusic("CreeperTank2", "Content/Assets/music/marble/creeper2", 0.5f);
            creeper3 = new OggMusic("CreeperTank3", "Content/Assets/music/marble/creeper3", 0.5f);
            creeper4 = new OggMusic("CreeperTank4", "Content/Assets/music/marble/creeper4", 0.5f);

            gamma1 = new OggMusic("GammaTank1", "Content/Assets/music/marble/gamma1", 0.5f);
            gamma2 = new OggMusic("GammaTank2", "Content/Assets/music/marble/gamma2", 0.5f);
            gamma3 = new OggMusic("GammaTank3", "Content/Assets/music/marble/gamma3", 0.5f);

            marble = new OggMusic("MarbleTank", "Content/Assets/music/marble/marble", 0.5f);

            obsidian = new OggMusic("ObsidianTank", "Content/Assets/music/obsidian", 0.5f);

            assassin = new OggMusic("AssassinTank", "Content/Assets/music/assassin", 0.5f);

            commando = new OggMusic("CommandoTank", "Content/Assets/music/commando", 0.5f);

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
            SnowLoop = new("Snow Breeze", "Content/Assets/sounds/ambient/snowfall", 1f);
        }

        public static void PlayAll()
        {
            //if (MusicMidi.State == Microsoft.Xna.Framework.Audio.SoundState.Stopped)
            //MusicMidi.Play(MusicSoundFont, true);

            // MusicMidi.NoteOffAll();


            foreach (var song in Songs)
                song?.Play();

            if (MapRenderer.Theme == MapTheme.Christmas)
                SnowLoop?.Play();
        }

        public static void PauseAll()
        {
            SnowLoop?.Pause();
            foreach (var song in Songs)
                if (!song.IsPaused())
                    song?.Pause();
        }

        public static void ResumeAll()
        {
            SnowLoop?.Play();
            foreach (var song in Songs)
                song?.Resume();
        }

        public static void StopAll()
        {
            SnowLoop?.Stop();
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
                    song.SetVolume(TankGame.Settings.MusicVolume);
                    if (MapRenderer.Theme == MapTheme.Christmas)
                        SnowLoop.SetVolume(TankGame.Settings.AmbientVolume);
                    else
                        SnowLoop.SetVolume(0);
                }
            }
        }
    }    
}