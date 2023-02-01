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
using System.Collections.Generic;
using TanksRebirth.GameContent.UI;
using Microsoft.Xna.Framework.Graphics;
using TanksRebirth.Internals;

namespace TanksRebirth.GameContent.Systems
{
    public static class TankMusicSystem
    {
        public static string AssetRoot;
        public static int TierHighest => AITank.GetHighestTierActive();

        public static OggMusic SnowLoop;

        public static float Pitch = 0f;
        public static float Pan = 0f;
        public static float VolumeMultiplier = 1f;

        public static List<int> TierExclusionRule_DoesntHaveSong = new() { TankID.Cherry, TankID.Electro, TankID.RocketDefender, TankID.Explosive };
        public static List<int> TierExclusionRule_Uses3ToUpgrade = new() { TankID.Ash, TankID.Silver, TankID.Bubblegum };

        public static Dictionary<int, int> MaxSongNumPerTank = new() {
            [TankID.Brown] = 1,
            [TankID.Ash] = 2,
            [TankID.Marine] = 2,
            [TankID.Yellow] = 3,
            [TankID.Pink] = 3,
            [TankID.Green] = 4,
            [TankID.Violet] = 3,
            [TankID.White] = 3,
            [TankID.Black] = 1,
            [TankID.Bronze] = 1,
            [TankID.Silver] = 2,
            [TankID.Sapphire] = 2,
            [TankID.Citrine] = 3,
            [TankID.Ruby] = 3,
            [TankID.Emerald] = 4,
            [TankID.Amethyst] = 3,
            [TankID.Gold] = 3,
            [TankID.Obsidian] = 1,
            [TankID.Granite] = 1,
            [TankID.Bubblegum] = 2,
            [TankID.Water] = 2,
            [TankID.Tiger] = 3,
            [TankID.Crimson] = 3,
            [TankID.Creeper] = 4,
            [TankID.Fade] = 3,
            [TankID.Gamma] = 3,
            [TankID.Marble] = 1,
            [TankID.Cherry] = 1,
            [TankID.Explosive] = 1,
            [TankID.Assassin] = 1,
            [TankID.RocketDefender] = 1,
            [TankID.Electro] = 1,
            [TankID.Commando] = 1
        };

        public static void LoadSoundPack(string folder) {
            // TODO: verify this works.
            if (folder.ToLower() == "vanilla") {
                LoadVanillaMusic();
                GameHandler.ClientLog.Write($"Loaded vanilla textures for Sound.", LogType.Info);
                return;
            }

            var baseRoot = Path.Combine(TankGame.SaveDirectory, "Sound Packs");
            var path = Path.Combine(baseRoot, folder);

            // ensure that these directories exist before dealing with them
            Directory.CreateDirectory(baseRoot);

            if (!Directory.Exists(path)) {
                GameHandler.ClientLog.Write($"Error: Directory '{path}' not found when attempting sound pack load.", LogType.Warn);
                return;
            }

            AssetRoot = path;
            foreach (var file in Directory.GetFiles(path)) {
                if (Songs.Any(type => type.Key == Path.GetFileNameWithoutExtension(file))) {
                    Songs[Path.GetFileNameWithoutExtension(file)] = new OggMusic(Path.GetFileNameWithoutExtension(file), Path.Combine(path, Path.GetFileName(file)), 0.5f);
                    GameHandler.ClientLog.Write($"Sound pack '{folder}' overrided sound '{Path.GetFileNameWithoutExtension(file)}'", LogType.Info);
                }
            }
        }

        public static void Update() {
            Pitch = 0f;
            Pan = 0f;

            SnowLoop.Volume = 0;
            VolumeMultiplier = SteamworksUtils.IsOverlayActive ? 0.25f : 1f;

            foreach (var song in Songs)
                song.Value?.SetVolume(0f);

            if (MainMenu.Active && AITank.CountAll() == 0 || TierHighest == TankID.None) {
                return;
            }

            if (MapRenderer.Theme == MapTheme.Christmas) {
                SnowLoop.SetVolume(TankGame.Settings.AmbientVolume);
                return;
            }

            var musicVolume = TankGame.Settings.MusicVolume * VolumeMultiplier;

            if (TierExclusionRule_DoesntHaveSong.Contains(TierHighest))
                return;

            var tierHighestName = TankID.Collection.GetKey(TierHighest);
            var all = AITank.CountAll();
            string num = (MaxSongNumPerTank[TierHighest] > 1 ? 
                (TierExclusionRule_Uses3ToUpgrade.Contains(TierHighest) ? 
                (all == 2 || all == 1 ? 1 : MaxSongNumPerTank[TierHighest]).ToString() : MaxSongNumPerTank[TierHighest].ToString()) 
                : string.Empty);

            var name = tierHighestName.ToLower() + num;

            Songs[name].SetVolume(musicVolume);

            /*if (TierHighest == TankID.Brown)
                Songs["brown"].SetVolume(musicVolume);

            if (TierHighest == TankID.Ash && AITank.CountAll() < 3)
                Songs["ash1"].SetVolume(musicVolume);
            else if (TierHighest == TankID.Ash && AITank.CountAll() >= 3)
                Songs["ash2"].SetVolume(musicVolume);

            if (TierHighest == TankID.Marine && AITank.CountAll() == 1)
                Songs["marine1"].SetVolume(musicVolume);
            else if (TierHighest == TankID.Marine && AITank.CountAll() >= 2)
                Songs["marine2"].SetVolume(musicVolume);

            if (TierHighest == TankID.Yellow && AITank.CountAll() == 1)
                Songs["yellow1"].SetVolume(musicVolume);
            else if (TierHighest == TankID.Yellow && (AITank.CountAll() == 2))
                Songs["yellow2"].SetVolume(musicVolume);
            else if (TierHighest == TankID.Yellow && (AITank.CountAll() >= 3))
                Songs["yellow3"].SetVolume(musicVolume);

            if (TierHighest == TankID.Pink && AITank.CountAll() == 1)
                Songs["pink1"].SetVolume(musicVolume);
            else if (TierHighest == TankID.Pink && (AITank.CountAll() == 2))
                Songs["pink2"].SetVolume(musicVolume);
            else if (TierHighest == TankID.Pink && (AITank.CountAll() >= 3))
                Songs["pink3"].SetVolume(musicVolume);

            if (TierHighest == TankID.Green && AITank.CountAll() == 1)
                Songs["GreenTank1"].SetVolume(musicVolume);
            else if (TierHighest == TankID.Green && (AITank.CountAll() == 2))
                Songs["GreenTank2"].SetVolume(musicVolume);
            else if (TierHighest == TankID.Green && (AITank.CountAll() == 3))
                Songs["GreenTank3"].SetVolume(musicVolume);
            else if (TierHighest == TankID.Green && (AITank.CountAll() >= 4))
                Songs["GreenTank4"].SetVolume(musicVolume);

            if (TierHighest == TankID.Violet && AITank.CountAll() == 1)
                Songs["PurpleTank1"].SetVolume(musicVolume);
            else if (TierHighest == TankID.Violet && (AITank.CountAll() == 2))
                Songs["PurpleTank2"].SetVolume(musicVolume);
            else if (TierHighest == TankID.Violet && (AITank.CountAll() >= 3))
                Songs["PurpleTank3"].SetVolume(musicVolume);

            if (TierHighest == TankID.White && AITank.CountAll() == 1)
                Songs["WhiteTank1"].SetVolume(musicVolume);
            else if (TierHighest == TankID.White && (AITank.CountAll() == 2))
                Songs["WhiteTank2"].SetVolume(musicVolume);
            else if (TierHighest == TankID.White && (AITank.CountAll() >= 3))
                Songs["WhiteTank3"].SetVolume(musicVolume);

            if (TierHighest == TankID.Black)
                Songs["BlackTank"].SetVolume(musicVolume);


            // vanilla above, master below


            if (TierHighest == TankID.Bronze)
                Songs["BronzeTank"].SetVolume(musicVolume);

            if (TierHighest == TankID.Silver && AITank.CountAll() < 3)
                Songs["SilverTank1"].SetVolume(musicVolume);
            else if (TierHighest == TankID.Silver && (AITank.CountAll() >= 3))
                Songs["SilverTank2"].SetVolume(musicVolume);

            if (TierHighest == TankID.Sapphire && AITank.CountAll() == 1)
                Songs["SapphireTank1"].SetVolume(musicVolume);
            else if (TierHighest == TankID.Sapphire && (AITank.CountAll() >= 2))
                Songs["SapphireTank2"].SetVolume(musicVolume);

            if (TierHighest == TankID.Citrine && AITank.CountAll() == 1)
                Songs["CitrineTank1"].SetVolume(musicVolume);
            else if (TierHighest == TankID.Citrine && (AITank.CountAll() == 2))
                Songs["CitrineTank2"].SetVolume(musicVolume);
            else if (TierHighest == TankID.Citrine && (AITank.CountAll() >= 3))
                Songs["CitrineTank3"].SetVolume(musicVolume);

            if (TierHighest == TankID.Ruby && AITank.CountAll() == 1)
                Songs["RubyTank1"].SetVolume(musicVolume);
            else if (TierHighest == TankID.Ruby && (AITank.CountAll() == 2))
                Songs["RubyTank2"].SetVolume(musicVolume);
            else if (TierHighest == TankID.Ruby && (AITank.CountAll() >= 3))
                Songs["RubyTank3"].SetVolume(musicVolume);

            if (TierHighest == TankID.Emerald && AITank.CountAll() == 1)
                Songs["EmeraldTank1"].SetVolume(musicVolume);
            else if (TierHighest == TankID.Emerald && (AITank.CountAll() == 2))
                Songs["EmeraldTank2"].SetVolume(musicVolume);
            else if (TierHighest == TankID.Emerald && (AITank.CountAll() == 3))
                Songs["EmeraldTank3"].SetVolume(musicVolume);
            else if (TierHighest == TankID.Emerald && (AITank.CountAll() >= 4))
                Songs["EmeraldTank4"].SetVolume(musicVolume);

            if (TierHighest == TankID.Amethyst && AITank.CountAll() == 1)
                Songs["AmethystTank1"].SetVolume(musicVolume);
            else if (TierHighest == TankID.Amethyst && (AITank.CountAll() == 2))
                Songs["AmethystTank2"].SetVolume(musicVolume);
            else if (TierHighest == TankID.Amethyst && (AITank.CountAll() >= 3))
                Songs["AmethystTank3"].SetVolume(musicVolume);

            if (TierHighest == TankID.Gold && AITank.CountAll() == 1)
                Songs["GoldTank1"].SetVolume(musicVolume);
            else if (TierHighest == TankID.Gold && (AITank.CountAll() == 2))
                Songs["GoldTank2"].SetVolume(musicVolume);
            else if (TierHighest == TankID.Gold && (AITank.CountAll() >= 3))
                Songs["GoldTank3"].SetVolume(musicVolume);

            if (TierHighest == TankID.Granite)
                Songs["GraniteTank"].SetVolume(musicVolume);

            if (TierHighest == TankID.Bubblegum && AITank.CountAll() < 3)
                Songs["BubblegumTank1"].SetVolume(musicVolume);
            else if (TierHighest == TankID.Bubblegum && (AITank.CountAll() >= 3))
                Songs["BubblegumTan2"].SetVolume(musicVolume);

            if (TierHighest == TankID.Water && AITank.CountAll() == 1)
                Songs["WaterTank1"].SetVolume(musicVolume);
            else if (TierHighest == TankID.Water && (AITank.CountAll() >= 2))
                Songs["WaterTank2"].SetVolume(musicVolume);

            if (TierHighest == TankID.Tiger && AITank.CountAll() == 1)
                Songs["TigerTank1"].SetVolume(musicVolume);
            else if (TierHighest == TankID.Tiger && (AITank.CountAll() == 2))
                Songs["TigerTank2"].SetVolume(musicVolume);
            else if (TierHighest == TankID.Tiger && (AITank.CountAll() >= 3))
                Songs["TigerTank3"].SetVolume(musicVolume);

            if (TierHighest == TankID.Crimson && AITank.CountAll() == 1)
                Songs["CrimsonTank1"].SetVolume(musicVolume);
            else if (TierHighest == TankID.Crimson && (AITank.CountAll() == 2))
                Songs["CrimsonTank2"].SetVolume(musicVolume);
            else if (TierHighest == TankID.Crimson && (AITank.CountAll() >= 3))
                Songs["CrimsonTank3"].SetVolume(musicVolume);

            if (TierHighest == TankID.Creeper && AITank.CountAll() == 1)
                Songs["CreeperTank1"].SetVolume(musicVolume);
            else if (TierHighest == TankID.Creeper && (AITank.CountAll() == 2))
                Songs["CreeperTank2"].SetVolume(musicVolume);
            else if (TierHighest == TankID.Creeper && (AITank.CountAll() == 3))
                Songs["CreeperTank3"].SetVolume(musicVolume);
            else if (TierHighest == TankID.Creeper && (AITank.CountAll() >= 4))
                Songs["CreeperTank4"].SetVolume(musicVolume);

            if (TierHighest == TankID.Fade && AITank.CountAll() == 1)
                Songs["FadeTank1"].SetVolume(musicVolume);
            else if (TierHighest == TankID.Fade && (AITank.CountAll() == 2))
                Songs["FadeTank2"].SetVolume(musicVolume);
            else if (TierHighest == TankID.Fade && (AITank.CountAll() >= 3))
                Songs["FadeTank3"].SetVolume(musicVolume);

            if (TierHighest == TankID.Gamma && AITank.CountAll() == 1)
                Songs["GammaTank1"].SetVolume(musicVolume);
            else if (TierHighest == TankID.Gamma && (AITank.CountAll() == 2))
                Songs["GammaTank2"].SetVolume(musicVolume);
            else if (TierHighest == TankID.Gamma && (AITank.CountAll() >= 3))
                Songs["GammaTank3"].SetVolume(musicVolume);

            if (TierHighest == TankID.Marble)
                Songs["MarbleTank"].SetVolume(musicVolume);

            if (TierHighest == TankID.Assassin)
                Songs["AssassinTank"].SetVolume(musicVolume);

            if (TierHighest == TankID.Commando)
                Songs["CommandoTank"].SetVolume(musicVolume);*/

            int index = Songs.Values.ToList().FindIndex(x => x.Volume > 0);

            if (index > -1)
            {
                Songs.ElementAt(index).Value.BackingAudio.Instance.Pitch = Pitch;
                Songs.ElementAt(index).Value.BackingAudio.Instance.Pan = Pan;
                CurrentSong = Songs.ElementAt(index).Value;
            }

            
            // we call this hardcode hell in the west
        }
        public static OggMusic CurrentSong;

        // public static OggMusic[] Songs;

        // public static OggMusic[] songs = new OggMusic[Directory.GetFiles("Content/assets/music").Length];

        //public static MidiPlayer MusicMidi;
        //public static MidiFile MusicSoundFont;

        public static bool IsLoaded;

        /// <summary>A dictionary of all stored/loaded tanks songs.</summary>
        public static Dictionary<string, OggMusic> Songs = new();

        public static void LoadVanillaMusic() {
            var filePath = "Content/Assets/music";

            foreach (var file in Directory.GetFiles(filePath).Where(s => s.EndsWith(".ogg"))) {
                var name = Path.GetFileNameWithoutExtension(file);
                Songs.Add(name, new OggMusic(name, file, 0.5f));
            }
        }

        public static void LoadMusic()
        {
            if (!IsLoaded)
                IsLoaded = true;
            else
                return;
            #region Load

            // var filePath = Path.Combine(TankGame.SaveDirectory, "Sound Packs");

            LoadVanillaMusic();

            /*Songs.Add("BrownTank", new OggMusic("BrownTank", "Content/Assets/music/brown", 0.5f));

            Songs.Add("AshTank1", new OggMusic("AshTank1", "Content/Assets/music/ash1", 0.5f));
            Songs.Add("AshTank2", new OggMusic("AshTank2", "Content/Assets/music/ash2", 0.5f));

            Songs.Add("MarineTank1", new OggMusic("MarineTank1", "Content/Assets/music/marine1", 0.5f));
            Songs.Add("MarineTank2", new OggMusic("MarineTank2", "Content/Assets/music/marine2", 0.5f));

            Songs.Add("YellowTank1", new OggMusic("YellowTank1", "Content/Assets/music/yellow1", 0.5f));
            Songs.Add("YellowTank2", new OggMusic("YellowTank2", "Content/Assets/music/yellow2", 0.5f));
            Songs.Add("YellowTank3", new OggMusic("YellowTank3", "Content/Assets/music/yellow3", 0.5f));

            Songs.Add("PinkTank1", new OggMusic("PinkTank1", "Content/Assets/music/pink1", 0.5f));
            Songs.Add("PinkTank2", new OggMusic("PinkTank2", "Content/Assets/music/pink2", 0.5f));
            Songs.Add("PinkTank3", new OggMusic("PinkTank3", "Content/Assets/music/pink3", 0.5f));

            Songs.Add("GreenTank1", new OggMusic("GreenTank1", "Content/Assets/music/green1", 0.5f));
            Songs.Add("GreenTank2", new OggMusic("GreenTank2", "Content/Assets/music/green2", 0.5f));
            Songs.Add("GreenTank3", new OggMusic("GreenTank3", "Content/Assets/music/green3", 0.5f));
            Songs.Add("GreenTank4", new OggMusic("GreenTank4", "Content/Assets/music/green4", 0.5f));

            Songs.Add("PurpleTank1", new OggMusic("PurpleTank1", "Content/Assets/music/purple1", 0.5f));
            Songs.Add("PurpleTank2", new OggMusic("PurpleTank2", "Content/Assets/music/purple2", 0.5f));
            Songs.Add("PurpleTank3", new OggMusic("PurpleTank3", "Content/Assets/music/purple3", 0.5f));

            Songs.Add("WhiteTank1", new OggMusic("WhiteTank1", "Content/Assets/music/white1", 0.5f));
            Songs.Add("WhiteTank2", new OggMusic("WhiteTank2", "Content/Assets/music/white2", 0.5f));
            Songs.Add("WhiteTank3", new OggMusic("WhiteTank3", "Content/Assets/music/white3", 0.5f));

            Songs.Add("BlackTank", new OggMusic("BlackTank", "Content/Assets/music/black", 0.5f));




            Songs.Add("BronzeTank", new OggMusic("BronzeTank", "Content/Assets/music/bronze", 0.5f));

            Songs.Add("SilverTank1", new OggMusic("SilverTank1", "Content/Assets/music/silver1", 0.5f));
            Songs.Add("SilverTank2", new OggMusic("SilverTank2", "Content/Assets/music/silver2", 0.5f));

            Songs.Add("SapphireTank1", new OggMusic("SapphireTank1", "Content/Assets/music/sapphire1", 0.5f));
            Songs.Add("SapphireTank2", new OggMusic("SapphireTank2", "Content/Assets/music/sapphire2", 0.5f));

            Songs.Add("RubyTank1", new OggMusic("RubyTank1", "Content/Assets/music/ruby1", 0.5f));
            Songs.Add("RubyTank2", new OggMusic("RubyTank2", "Content/Assets/music/ruby2", 0.5f));
            Songs.Add("RubyTank3", new OggMusic("RubyTank3", "Content/Assets/music/ruby3", 0.5f));

            Songs.Add("CitrineTank1", new OggMusic("CitrineTank1", "Content/Assets/music/citrine1", 0.5f));
            Songs.Add("CitrineTank2", new OggMusic("CitrineTank2", "Content/Assets/music/citrine2", 0.5f));
            Songs.Add("CitrineTank3", new OggMusic("CitrineTank3", "Content/Assets/music/citrine3", 0.5f));

            Songs.Add("AmethystTank1", new OggMusic("AmethystTank1", "Content/Assets/music/amethyst1", 0.5f));
            Songs.Add("AmethystTank2", new OggMusic("AmethystTank2", "Content/Assets/music/amethyst2", 0.5f));
            Songs.Add("AmethystTank3", new OggMusic("AmethystTank3", "Content/Assets/music/amethyst3", 0.5f));

            Songs.Add("EmeraldTank1", new OggMusic("EmeraldTank1", "Content/Assets/music/emerald1", 0.5f));
            Songs.Add("EmeraldTank2", new OggMusic("EmeraldTank2", "Content/Assets/music/emerald2", 0.5f));
            Songs.Add("EmeraldTank3", new OggMusic("EmeraldTank3", "Content/Assets/music/emerald3", 0.5f));
            Songs.Add("EmeraldTank4", new OggMusic("EmeraldTank4", "Content/Assets/music/emerald4", 0.5f));

            Songs.Add("GoldTank1", new OggMusic("GoldTank1", "Content/Assets/music/gold1", 0.5f));
            Songs.Add("GoldTank2", new OggMusic("GoldTank2", "Content/Assets/music/gold2", 0.5f));
            Songs.Add("GoldTank3", new OggMusic("GoldTank3", "Content/Assets/music/gold3", 0.5f));

            // initialize songs in order, using base path "Assets/music/marble"
            Songs.Add("GraniteTank", new OggMusic("GraniteTank", "Content/Assets/music/marble/granite", 0.5f));

            Songs.Add("BubblegumTank1", new OggMusic("BubblegumTank1", "Content/Assets/music/marble/bubblegum1", 0.5f));
            Songs.Add("BubblegumTank2", new OggMusic("BubblegumTank2", "Content/Assets/music/marble/bubblegum2", 0.5f));

            water1 = new OggMusic("WaterTank1", "Content/Assets/music/marble/water1", 0.5f);
            water2 = new OggMusic("WaterTank2", "Content/Assets/music/marble/water2", 0.5f);

            Songs.Add("CrimsonTank1", new OggMusic("CrimsonTank1", "Content/Assets/music/marble/crimson1", 0.5f));
            Songs.Add("CrimsonTank2", new OggMusic("CrimsonTank2", "Content/Assets/music/marble/crimson2", 0.5f));
            Songs.Add("CrimsonTank3", new OggMusic("CrimsonTank3", "Content/Assets/music/marble/crimson3", 0.5f));

            Songs.Add("TigerTank1", new OggMusic("TigerTank1", "Content/Assets/music/marble/tiger1", 0.5f));
            Songs.Add("TigerTank2", new OggMusic("TigerTank2", "Content/Assets/music/marble/tiger2", 0.5f));
            Songs.Add("TigerTank3", new OggMusic("TigerTank3", "Content/Assets/music/marble/tiger3", 0.5f));

            Songs.Add("FadeTank1", new OggMusic("FadeTank1", "Content/Assets/music/marble/fade1", 0.5f));
            Songs.Add("FadeTank2", new OggMusic("FadeTank2", "Content/Assets/music/marble/fade2", 0.5f));
            Songs.Add("FadeTank3", new OggMusic("FadeTank3", "Content/Assets/music/marble/fade3", 0.5f));

            Songs.Add("CreeperTank1", new OggMusic("CreeperTank1", "Content/Assets/music/marble/creeper1", 0.5f));
            Songs.Add("CreeperTank2", new OggMusic("CreeperTank2", "Content/Assets/music/marble/creeper2", 0.5f));
            Songs.Add("CreeperTank3", new OggMusic("CreeperTank3", "Content/Assets/music/marble/creeper3", 0.5f));
            Songs.Add("CreeperTank4", new OggMusic("CreeperTank4", "Content/Assets/music/marble/creeper4", 0.5f));

            Songs.Add("GammaTank1", new OggMusic("GammaTank1", "Content/Assets/music/marble/gamma1", 0.5f));
            Songs.Add("GammaTank2", new OggMusic("GammaTank2", "Content/Assets/music/marble/gamma2", 0.5f));
            Songs.Add("GammaTank3", new OggMusic("GammaTank3", "Content/Assets/music/marble/gamma3", 0.5f));

            Songs.Add("MarbleTank", new OggMusic("MarbleTank", "Content/Assets/music/marble/marble", 0.5f));

            Songs.Add("ObsidianTank", new OggMusic("ObsidianTank", "Content/Assets/music/obsidian", 0.5f));

            Songs.Add("AssassinTank", new OggMusic("AssassinTank", "Content/Assets/music/assassin", 0.5f));

            Songs.Add("CommandoTank", new OggMusic("CommandoTank", "Content/Assets/music/commando", 0.5f));*/

            #endregion

            //MusicMidi = new MidiPlayer(@"C:\Users\ryanr\Desktop\Git Repositories\WiiPlayTanksRemake\Content\Assets\music\Wii_tanks_bgm.sf2", new(44100) { EnableReverbAndChorus = false });
            // MusicSoundFont = new MidiFile(@"C:\Users\ryanr\Desktop\Git Repositories\WiiPlayTanksRemake\Content\Assets\music\Wii_tanks_bgm.mid", 3200);

            /*Songs = new OggMusic[]
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
            };*/
        }

        public static void LoadAmbienceTracks()
        {
            SnowLoop = new("Snow Breeze", "Content/Assets/sounds/ambient/snowfall.ogg", 1f);
        }

        public static void PlayAll()
        {
            //if (MusicMidi.State == Microsoft.Xna.Framework.Audio.SoundState.Stopped)
            //MusicMidi.Play(MusicSoundFont, true);

            // MusicMidi.NoteOffAll();


            foreach (var song in Songs)
                song.Value?.Play();

            if (MapRenderer.Theme == MapTheme.Christmas)
                SnowLoop?.Play();
        }

        public static void PauseAll()
        {
            SnowLoop?.Pause();
            foreach (var song in Songs)
                if (!song.Value.IsPaused())
                    song.Value?.Pause();
        }

        public static void ResumeAll()
        {
            SnowLoop?.Play();
            foreach (var song in Songs)
                song.Value?.Resume();
        }

        public static void StopAll()
        {
            SnowLoop?.Stop();
            if (Songs is not null)
            {
                foreach (var song in Songs)
                    song.Value?.Stop();
            }
        }

        public static void UpdateVolume()
        {
            foreach (var song in Songs)
            {
                if (song.Value.Volume > 0)
                {
                    song.Value.SetVolume(TankGame.Settings.MusicVolume);
                    if (MapRenderer.Theme == MapTheme.Christmas)
                        SnowLoop.SetVolume(TankGame.Settings.AmbientVolume);
                    else
                        SnowLoop.SetVolume(0);
                }
            }
        }
    }    
}