using TanksRebirth;
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
using Microsoft.Xna.Framework.Graphics;
using TanksRebirth.Internals;
using TanksRebirth.Graphics;
using TanksRebirth.GameContent.UI.MainMenu;
using TanksRebirth.GameContent.Tanks.AI;

namespace TanksRebirth.GameContent.Systems;

public static class TankMusicSystem {
    public static OggMusic SnowLoop;
    public static string AssetRoot;
    public static int TierHighest => AIManager.GetHighestTierActive(x => !NoSongRule.Contains(x.AiTankType));

    public static float Pitch = 0f;
    public static float Pan = 0f;
    public static float VolumeMultiplier = 1f;

    public static HashSet<int> NoSongRule = [];
    /// <summary>Makes instrumental changes start at tank 3 instead of tank 2.</summary>
    public static HashSet<int> Skip2Rule = [TankID.Ash, TankID.Silver];
    /// <summary>Makes instrumental changes achieved by tank 2 persist until tank 4.</summary>
    public static HashSet<int> Skip3Rule = [TankID.Marine, TankID.Sapphire];

    public static readonly Dictionary<int, int> MaxSongNumPerTank = new() {
        [TankID.Brown] = 1,
        [TankID.Ash] = 2,
        [TankID.Marine] = 3, // used to be 2 based on goofy ahh data, same with sapphire
        [TankID.Yellow] = 3,
        [TankID.Pink] = 3,
        [TankID.Green] = 4,
        [TankID.Violet] = 3,
        [TankID.White] = 3,
        [TankID.Black] = 1,
        [TankID.Bronze] = 1,
        [TankID.Silver] = 2,
        [TankID.Sapphire] = 3,
        [TankID.Citrine] = 3,
        [TankID.Ruby] = 3,
        [TankID.Emerald] = 4,
        [TankID.Amethyst] = 3,
        [TankID.Gold] = 3,
        [TankID.Obsidian] = 1,
    };
    /// <summary>A dictionary of all stored/loaded tanks songs.</summary>
    public static readonly Dictionary<string, OggMusic> Audio = [];

    public static void LoadVanillaAudio() {
        var filePath = "Content/Assets/music";
        foreach (var file in Directory.GetFiles(filePath).Where(s => s.EndsWith(".ogg"))) {
            var name = Path.GetFileNameWithoutExtension(file);
            Audio[name] = new OggMusic(name, file.Replace("\\", "/"), 0.5f);
        }
        Audio["Snow Breeze"] = new("Snow Breeze", "Content/Assets/sounds/ambient/snowfall.ogg", 1f);
        SnowLoop = Audio["Snow Breeze"];
    }
    public static void SetAssetAssociations() {
        var filePath = "Content/Assets/music";
        foreach (var file in Directory.GetFiles(filePath).Where(s => s.EndsWith(".ogg"))) {
            var name = Path.GetFileNameWithoutExtension(file);
            Audio.Add(name, null!);
        }
        Audio.Add("Snow Breeze", null!);
    }
    public static void LoadSoundPack(string folder) {
        // TODO: verify this works.
        IsLoaded = true;
        LoadVanillaAudio();
        if (folder.Equals("vanilla", StringComparison.CurrentCultureIgnoreCase)) {
            TankGame.ClientLog.Write($"Loaded vanilla audio for Sound.", LogType.Info);
            return;
        }

        var baseRoot = Path.Combine(TankGame.SaveDirectory, "Resource Packs", "Music");
        var path = Path.Combine(baseRoot, folder);

        // ensure that these directories exist before dealing with them
        Directory.CreateDirectory(baseRoot);

        if (!Directory.Exists(path)) {
            TankGame.ClientLog.Write($"Error: Directory '{path}' not found when attempting sound pack load.", LogType.Warn);
            return;
        }

        AssetRoot = path;
        foreach (var file in Directory.GetFiles(path)) {
            if (Audio.Any(type => type.Key == Path.GetFileNameWithoutExtension(file))) {
                var name = Path.GetFileNameWithoutExtension(file);
                var assetPath = Path.Combine(path, Path.GetFileName(file));
                Audio[name] = new OggMusic(name, assetPath, 0.5f);
                TankGame.ClientLog.Write($"Sound pack '{folder}' overrided sound '{name}'", LogType.Info);
            }
        }
    }

    public static void Update() {
        Pitch = 0f;
        Pan = 0f;

        SnowLoop.Volume = 0;
        VolumeMultiplier = SteamworksUtils.IsOverlayActive ? 0.25f : 1f;

        // if this starts causing dogshit... put .ToList()
        foreach (var song in Audio)
            song.Value?.SetVolume(0f);

        // i feel like && -> || ?
        if (MainMenuUI.IsActive && AIManager.CountAll() == 0 || TierHighest == TankID.None) return;

        if (GameScene.Theme == MapTheme.Christmas) {
            SnowLoop.SetVolume(TankGame.Settings.AmbientVolume);
            return;
        }

        // maybe only need to update some of these things in the event of an ai tank death?

        var musicVolume = TankGame.Settings.MusicVolume * VolumeMultiplier;
        var tierHighestName = TankID.Collection.GetKey(TierHighest);

        // only count the tanks that exist and are below the highest tier.
        var all = AIManager.CountAll(x => x.AiTankType <= TierHighest);
        string num = GetSongSuffix(TierHighest, all);

        var name = tierHighestName!.ToLower() + num;

        Audio[name].SetVolume(musicVolume);

        OggMusic activeSong = null;
        foreach (var song in Audio.Values) {
            if (song != null && song.Volume > 0) {
                activeSong = song;
                break;
            }
        }

        if (activeSong != null) {
            activeSong.BackingAudio.Instance.Pitch = Pitch;
            activeSong.BackingAudio.Instance.Pan = Pan;
            CurrentSong = activeSong;
        }
    }
    static string GetSongSuffix(int tankId, int tankCount) {
        int maxSongs = MaxSongNumPerTank.TryGetValue(tankId, out int max) ? max : 1;

        if (maxSongs <= 1)
            return string.Empty;

        if (Skip2Rule.Contains(tankId)) {
            // 1, 1, 2
            return tankCount >= 3 ? maxSongs.ToString() : "1";
        }
        else if (Skip3Rule.Contains(tankId)) {
            // 1, 2, 2, 3
            if (tankCount == 1) return "1";
            else if (tankCount == 2 || tankCount == 3) return "2";
        }
        
        return Math.Min(tankCount, maxSongs).ToString();
    }
    public static OggMusic CurrentSong;

    //public static MidiPlayer MusicMidi;
    //public static MidiFile MusicSoundFont;

    public static bool IsLoaded;
    public static void PlayAll() {
        //if (MusicMidi.State == Microsoft.Xna.Framework.Audio.SoundState.Stopped)
        //MusicMidi.Play(MusicSoundFont, true);

        // MusicMidi.NoteOffAll();


        foreach (var song in Audio)
            song.Value?.Play();

        if (GameScene.Theme == MapTheme.Christmas)
            SnowLoop?.Play();
    }

    public static void PauseAll() {
        SnowLoop?.Pause();
        foreach (var song in Audio)
            song.Value.Pause();
    }

    public static void ResumeAll() {
        SnowLoop?.Play();
        foreach (var song in Audio)
            song.Value?.Resume();
    }

    public static void StopAll() {
        SnowLoop?.Stop();
        if (Audio is not null) {
            foreach (var song in Audio)
                song.Value?.Stop();
        }
    }

    public static void UpdateVolume() {
        foreach (var song in Audio) {
            if (song.Value.Volume > 0) {
                song.Value.SetVolume(TankGame.Settings.MusicVolume);
                if (GameScene.Theme == MapTheme.Christmas)
                    SnowLoop.SetVolume(TankGame.Settings.AmbientVolume);
                else
                    SnowLoop.SetVolume(0);
            }
        }
    }
}