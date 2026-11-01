using System.IO;
using System.Text.Json;
using TanksRebirth.GameContent.Systems.ParticleSystem;
using TanksRebirth.Internals.Common.IO;

namespace TanksRebirth.Localization;

/// <summary>Localization to load a <see cref="Language"/> from a .json entry. Defaults to English if no <see cref="LangCode"/> is loaded.</summary>
public class Language {
    public struct BasicLocales {
        public string Yes { get; set; }
        public string No { get; set; }
        public string Enabled { get; set; }
        public string Disabled { get; set; }
        public string Status { get; set; }
    }
    public struct TeamLocales {
        public string NoTeam { get; set; }
        public string Red { get; set; }
        public string Blue { get; set; }
        public string Green { get; set; }
        public string Yellow { get; set; }
        public string Purple { get; set; }
        public string Orange { get; set; }
        public string Cyan { get; set; }
        public string Magenta { get; set; }
    }
    public struct GeneralLocales {
        public string Mission { get; set; }
        public string Campaign { get; set; }
        public string Resume { get; set; }
        public string StartOver { get; set; }
    }
    public struct MenuLocales {
        public string Play { get; set; }
        public string SinglePlayer { get; set; }
        public string LevelEditor { get; set; }
        public string Multiplayer { get; set; }
        public string ConnectToServer { get; set; }
        public string CreateServer { get; set; }
        public string Difficulties { get; set; }
        public string GameStats { get; set; }
        public string GameStatsFlavor { get; set; }
        public string CosmeticsMenu { get; set; }
        public string CosmeticsFlavor { get; set; }
        public string SinglePlayerFlavor { get; set; }
        public string DifficultiesFlavor { get; set; }
        public string LevelEditFlavor { get; set; }
        public string MultiplayerFlavor { get; set; }

        public string Back { get; set; }
        public string Return { get; set; }
        public string Quit { get; set; }
        public string Options { get; set; }
        public string Volume { get; set; }
        public string Graphics { get; set; }
        public string Controls { get; set; }
        public string PressAKey { get; set; }
    }
    public struct GameStatsLocales {
        public string TankKillsTotal { get; set; }
        public string TankKillsTotalBullets { get; set; }
        public string TankKillsTotalBulletsBounced { get; set; }
        public string TankKillsTotalMines { get; set; }
        public string MissionsCompleted { get; set; }
        public string CampaignsCompleted { get; set; }
        public string Deaths { get; set; }
        public string Suicides { get; set; }
        public string TimePlayedCurrent { get; set; }
        public string TimePlayedTotal { get; set; }
        public string TankKillsPerType { get; set; }
    }
    public struct LevelEditLocales {
        public string TestLevel { get; set; }
        public string Perspective { get; set; }
        public string PerspectiveFlavor { get; set; }
        public string Players { get; set; }
        public string Terrain { get; set; }
        public string AIControlled { get; set; }
        public string AutoOrientTanks { get; set; }
        public string AutoOrientTanksFlavor { get; set; }
        public string MissionList { get; set; }
        public string LevelInfo { get; set; }
        public string Properties { get; set; }
        public string Load { get; set; }
        public string LoadMissionFlavor { get; set; }
        public string PlaceInfo { get; set; }
        public string TankTeams { get; set; }
        public string PlacementTeamInfo { get; set; }
        public string PlacementStackInfo { get; set; }
        public string EnemyTankTotal { get; set; }
        public string DifficultyRating { get; set; }
        public string AddMissionFlavor { get; set; }
        public string RemoveMissionFlavor { get; set; }
        public string MoveMissionUpFlavor { get; set; }
        public string MoveMissionDownFlavor { get; set; }
        public string BlockStackFlavor { get; set; }

        public PropertiesMenu PropertyMenu { get; set; }
        public TankPlacement TankPlace { get; set; }
        public PlayerPlacement PlayerPlace { get; set; }
        public ObstaclePlacement ObstaclePlace { get; set; }

        public struct PropertiesMenu {
            public string Name { get; set; }
            public string Save { get; set; }
            public string MissionSaveFlavor { get; set; }
            public string CampaignSaveFlavor { get; set; }
            public string CampaignDetails { get; set; }
            public string MissionDetails { get; set; }
            public string CampaignNameFlavor { get; set; }
            public string Description { get; set; }
            public string DescriptionFlavor { get; set; }
            public string Author { get; set; }
            public string AuthorFlavor { get; set; }
            public string Tags { get; set; }
            public string TagsFlavor { get; set; }
            public string GrantsBonusLife { get; set; }
            public string GrantsBonusLifeFlavor { get; set; }
            public string Version { get; set; }
            public string VersionFlavor { get; set; }
            public string BGColor { get; set; }
            public string BGColorFlavor { get; set; }
            public string BannerColor { get; set; }
            public string BannerColorFlavor { get; set; }
            public string StartingLives { get; set; }
            public string StartingLivesFlavor { get; set; }
            public string HasMajorVictoryTheme { get; set; }
            public string HasMajorVictoryThemeFlavor { get; set; }
        }
        public struct TankPlacement {
            public string BrownFlavor { get; set; }
            public string AshFlavor { get; set; }
            public string MarineFlavor { get; set; }
            public string YellowFlavor { get; set; }
            public string PinkFlavor { get; set; }
            public string GreenFlavor { get; set; }
            public string VioletFlavor { get; set; }
            public string WhiteFlavor { get; set; }
            public string BlackFlavor { get; set; }
            public string BronzeFlavor { get; set; }
            public string SilverFlavor { get; set; }
            public string SapphireFlavor { get; set; }
            public string RubyFlavor { get; set; }
            public string CitrineFlavor { get; set; }
            public string AmethystFlavor { get; set; }
            public string EmeraldFlavor { get; set; }
            public string GoldFlavor { get; set; }
            public string ObsidianFlavor { get; set; }
        }
        public struct PlayerPlacement {
            public string P1TankFlavor { get; set; }
            public string P2TankFlavor { get; set; }
            public string P3TankFlavor { get; set; }
            public string P4TankFlavor { get; set; }
        }
        public struct ObstaclePlacement {
            public string WoodFlavor { get; set; }
            public string CorkFlavor { get; set; }
            public string HoleFlavor { get; set; }
        }
    }
    public struct GameplayLocales {
        public string Hit { get; set; }
        public string MissionReady { get; set; }
        public string MissionSet { get; set; }
        public string MissionStart { get; set; }
    }
    public struct SettingsLocales {
        public string PerPxLight { get; set; }
        public string PerPxLightDesc { get; set; }
        public string VSync { get; set; }
        public string VSyncDesc { get; set; }
        public string WindowKind { get; set; } // name is remaining the same for legacy purposes, this is actually just the dictation for fullscreen
        public string WindowKindDesc { get; set; } // same here
        public string Resolution { get; set; }
        public string ResolutionDesc { get; set; }
        public string FadeTracks { get; set; }
        public string FadeTracksDesc { get; set; }

        public string MenuGameplay { get; set; }
        public string MenuGameplayDesc { get; set; }
        public string ParticleIntensity { get; set; }
        public string ParticleIntensityDesc { get; set; }
        public string KeyboardPlayer { get; set; }

        public string MusicVolume { get; set; }
        public string EffectsVolume { get; set; }
        public string AmbientVolume { get; set; }

        public string PageAudio { get; set; }
        public string PageAudioDesc { get; set; }
        public string PageAudioHint { get; set; }
        public string PageVideo { get; set; }
        public string PageVideoDesc { get; set; }
        public string PageGameplay { get; set; }
        public string PageGameplayDesc { get; set; }
        public string PageControlsDesc { get; set; }
        public string PageControlsHint { get; set; }
        public string CycleHint { get; set; }
        public string PreviousPage { get; set; }
        public string NextPage { get; set; }

        public string Off { get; set; }
        public string ResetDefaults { get; set; }
        public string ResetPageDesc { get; set; }
        public string Apply { get; set; }
        public string Unlimited { get; set; }
        public string Unavailable { get; set; }
        public string QualityLow { get; set; }
        public string QualityMedium { get; set; }
        public string QualityHigh { get; set; }
        public string QualityUltra { get; set; }
        public string QualityCustom { get; set; }

        public string MusicVolumeDesc { get; set; }
        public string EffectsVolumeDesc { get; set; }
        public string AmbientVolumeDesc { get; set; }
        public string ResetVolumesDesc { get; set; }

        public string TrackLimit { get; set; }
        public string TrackLimitDesc { get; set; }
        public string TeamColors { get; set; }
        public string TeamColorsDesc { get; set; }
        public string FirstPersonFOV { get; set; }
        public string FirstPersonFOVDesc { get; set; }

        public string HeaderDisplay { get; set; }
        public string HeaderLighting { get; set; }
        public string HeaderQuality { get; set; }
        public string WindowModeDesc { get; set; }
        public string ResolutionInfoDesc { get; set; }
        public string WindowWindowed { get; set; }
        public string WindowBorderless { get; set; }
        public string WindowFullscreen { get; set; }
        public string FrameLimit { get; set; }
        public string FrameLimitDesc { get; set; }
        public string ApplyDesc { get; set; }

        public string DynamicLighting { get; set; }
        public string DynamicLightingDesc { get; set; }
        public string LightingQuality { get; set; }
        public string LightingQualityDesc { get; set; }
        public string SunShadows { get; set; }
        public string SunShadowsDesc { get; set; }
        public string UltraNoHiDef { get; set; }
        public string LightShadows { get; set; }
        public string LightShadowsDesc { get; set; }
        public string LightShafts { get; set; }
        public string LightShaftsDesc { get; set; }
        public string LightCount { get; set; }
        public string LightCountDesc { get; set; }
        public string RoomShadows { get; set; }
        public string RoomShadowsDesc { get; set; }
        public string TimeOfDay { get; set; }
        public string TimeOfDayDesc { get; set; }
        public string TimeSunrise { get; set; }
        public string TimeLateMorning { get; set; }
        public string TimeMidday { get; set; }
        public string TimeMidAfternoon { get; set; }
        public string TimeLateAfternoon { get; set; }
        public string TimeGoldenHour { get; set; }
        public string TimeEvening { get; set; }
        public string TimeDusk { get; set; }
        public string TimeMidnight { get; set; }
        public string TimeBlackout { get; set; }
        public string TimeDayCycle { get; set; }
        public string DayLength { get; set; }
        public string DayLengthDesc { get; set; }
        public string RealTime { get; set; }
        public string DayLengthMinutes { get; set; }
        public string GameplayLights { get; set; }
        public string GameplayLightsDesc { get; set; }

        public string AntiAliasing { get; set; }
        public string AntiAliasingDesc { get; set; }
        public string RainyWindows { get; set; }
        public string RainyWindowsDesc { get; set; }
        public string RainStrength { get; set; }
        public string RainStrengthDesc { get; set; }
        public string RainLight { get; set; }
        public string RainMedium { get; set; }
        public string RainHeavy { get; set; }
        public string BlobShadows { get; set; }
        public string BlobShadowsDesc { get; set; }
        public string MenuBlur { get; set; }
        public string MenuBlurDesc { get; set; }

        public string HeaderKeyboardMouse { get; set; }
        public string MoveUp { get; set; }
        public string MoveDown { get; set; }
        public string MoveLeft { get; set; }
        public string MoveRight { get; set; }
        public string PlaceMine { get; set; }
        public string ResetBindings { get; set; }
        public string ResetBindingsDesc { get; set; }
        public string KeyboardPlayerLocked { get; set; }
        public string KeyboardPlayerDesc { get; set; }
        public string Shoot { get; set; }
        public string PauseBack { get; set; }
        public string Unbindable { get; set; }
        public string Unbound { get; set; }
        public string RebindDesc { get; set; }
        public string MouseLeft { get; set; }
        public string MouseRight { get; set; }
        public string MouseMiddle { get; set; }
        public string MouseExtra { get; set; }
    }
    public struct MiscLocales {
        public string BonusTank { get; set; }
        public string CampaignResults { get; set; }
        public string FunFacts { get; set; }
        public string ToggleChat { get; set; }
        public string ToExit { get; set; }
        public string ShotsHit { get; set; }
        public string MineEffect { get; set; }
        public string LivesEarned { get; set; }
        public string MissionsComplete { get; set; }
        public string EnemyTanks { get; set; }
        public string Mods { get; set; }
        public string ReloadMods { get; set; }

        public string KeysCount { get; set; }
        public string KeysWarning { get; set; }
    }
    public struct CreditsLocales {
        public string Credits { get; set; }
        public string Programming { get; set; }
        public string DevelopedBy { get; set; }
        public string GraphicsDesign { get; set; }
        public string Contributors { get; set; }
        public string SpecialThanks { get; set; }
        public string Shoutouts { get; set; }
    }

    public BasicLocales Basic { get; set; }
    public TeamLocales Teams { get; set; }
    public GeneralLocales General { get; set; }
    public MenuLocales Menu { get; set; }
    public GameStatsLocales GameStats { get; set; }
    public LevelEditLocales LevelEdit { get; set; }
    public GameplayLocales Gameplay { get; set; }
    public SettingsLocales Settings { get; set; }
    public MiscLocales Misc { get; set; }
    public CreditsLocales Credits { get; set; }

    public LangCode ActiveLang;


#pragma warning restore

    /// <summary>
    /// Register a language localization file. 
    /// </summary>
    /// <param name="code">The language code to load.</param>
    /// <param name="lang">The <see cref="Language"/> instance to return.</param>
    public static void LoadLang(LangCode code, out Language lang) {
        // for example, it would be sane to have en_US or es_SP or jp_JA
        lang = new();
        var opts = new JsonSerializerOptions() { WriteIndented = true };
        try { 
            var path = Path.Combine(Path.Combine("Localization", $"{code}.json"));
            JsonHandler<Language> handler = new(lang, opts, path);

            TankGame.ClientLog.Write($"Loading language '{code}'... [ " + path + " ]", Internals.RebirthLogLevel.Debug);
            lang.ActiveLang = code;
            lang = handler.Deserialize();
        }
        catch {
            TankGame.ClientLog.Write($"Loading language '{code}'... Could not find localization file or error loading! Using default language '{LangCode.English}' instead.", Internals.RebirthLogLevel.Debug);
            var path = Path.Combine(Path.Combine("Localization", $"en_US.json"));
            JsonHandler<Language> handler = new(lang, opts, path);
            lang = handler.Deserialize();
        }
        lang.ActiveLang = code;
    }

    public static void GenerateLocalizationTemplate(string path) {
        var lang = new Language();
        JsonHandler<Language> handler = new(lang, new() { WriteIndented = true }, path);
        handler.Serialize(true);
    }

    public string GetEnablement(bool enabled) => enabled ? Basic.Enabled : Basic.Disabled;
    public string GetYesNo(bool state) => state ? Basic.Yes : Basic.No;
}