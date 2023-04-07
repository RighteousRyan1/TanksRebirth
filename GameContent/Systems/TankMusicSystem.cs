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
        public static int TierHighest => AiHelpers.GetHighestTierActive();

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
        /// <summary>A dictionary of all stored/loaded tanks songs.</summary>
        public static Dictionary<string, OggMusic> Audio = new();

        public static void LoadVanillaAudio() {
            var filePath = "Content/Assets/music";
            foreach (var file in Directory.GetFiles(filePath).Where(s => s.EndsWith(".ogg"))) {
                var name = Path.GetFileNameWithoutExtension(file);
                Audio[name] = new OggMusic(name, file, 0.5f);
            }
            Audio["Snow Breeze"] = new("Snow Breeze", "Content/Assets/sounds/ambient/snowfall.ogg", 1f);
            SnowLoop = Audio["Snow Breeze"];
        }
        public static void SetAssetAssociations() {
            var filePath = "Content/Assets/music";
            foreach (var file in Directory.GetFiles(filePath).Where(s => s.EndsWith(".ogg"))) {
                var name = Path.GetFileNameWithoutExtension(file);
                Audio.Add(name, null);
            }
            Audio.Add("Snow Breeze", null);
        }
        public static void LoadSoundPack(string folder) {
            // TODO: verify this works.
            IsLoaded = true;
            LoadVanillaAudio();
            if (folder.ToLower() == "vanilla") {
                // LoadVanillaAudio();
                GameHandler.ClientLog.Write($"Loaded vanilla audio for Sound.", LogType.Info);
                return;
            }

            var baseRoot = Path.Combine(TankGame.SaveDirectory, "Resource Packs", "Music");
            var path = Path.Combine(baseRoot, folder);

            // ensure that these directories exist before dealing with them
            Directory.CreateDirectory(baseRoot);

            if (!Directory.Exists(path)) {
                GameHandler.ClientLog.Write($"Error: Directory '{path}' not found when attempting sound pack load.", LogType.Warn);
                return;
            }

            AssetRoot = path;
            foreach (var file in Directory.GetFiles(path)) {
                if (Audio.Any(type => type.Key == Path.GetFileNameWithoutExtension(file))) {
                    var name = Path.GetFileNameWithoutExtension(file);
                    var assetPath = Path.Combine(path, Path.GetFileName(file));
                    Audio[name] = new OggMusic(name, assetPath, 0.5f);
                    GameHandler.ClientLog.Write($"Sound pack '{folder}' overrided sound '{name}'", LogType.Info);
                }
            }
        }

        public static void Update() {
            Pitch = 0f;
            Pan = 0f;

            SnowLoop.Volume = 0;
            VolumeMultiplier = SteamworksUtils.IsOverlayActive ? 0.25f : 1f;

            foreach (var song in Audio)
                song.Value?.SetVolume(0f);

            if (MainMenu.Active && AiHelpers.CountAll() == 0 || TierHighest == TankID.None) {
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
            var all = AiHelpers.CountAll();
            string num = MaxSongNumPerTank[TierHighest] > 1 ? 
                (TierExclusionRule_Uses3ToUpgrade.Contains(TierHighest) ? 
                (all == 2 || all == 1 ? 1 : MaxSongNumPerTank[TierHighest]).ToString() : (Math.Min(all, MaxSongNumPerTank[TierHighest])).ToString()) 
                : string.Empty;

            var name = tierHighestName.ToLower() + num;

            Audio[name].SetVolume(musicVolume);

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

            int index = Audio.Values.ToList().FindIndex(x => x.Volume > 0);

            if (index > -1)
            {
                Audio.ElementAt(index).Value.BackingAudio.Instance.Pitch = Pitch;
                Audio.ElementAt(index).Value.BackingAudio.Instance.Pan = Pan;
                CurrentSong = Audio.ElementAt(index).Value;
            }

            
            // we call this hardcode hell in the west
        }
        public static OggMusic CurrentSong;

        //public static MidiPlayer MusicMidi;
        //public static MidiFile MusicSoundFont;

        public static bool IsLoaded;
        public static void PlayAll()
        {
            //if (MusicMidi.State == Microsoft.Xna.Framework.Audio.SoundState.Stopped)
            //MusicMidi.Play(MusicSoundFont, true);

            // MusicMidi.NoteOffAll();


            foreach (var song in Audio)
                song.Value?.Play();

            if (MapRenderer.Theme == MapTheme.Christmas)
                SnowLoop?.Play();
        }

        public static void PauseAll()
        {
            SnowLoop?.Pause();
            foreach (var song in Audio)
                if (!song.Value.IsPaused())
                    song.Value?.Pause();
        }

        public static void ResumeAll()
        {
            SnowLoop?.Play();
            foreach (var song in Audio)
                song.Value?.Resume();
        }

        public static void StopAll()
        {
            SnowLoop?.Stop();
            if (Audio is not null)
            {
                foreach (var song in Audio)
                    song.Value?.Stop();
            }
        }

        public static void UpdateVolume()
        {
            foreach (var song in Audio)
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