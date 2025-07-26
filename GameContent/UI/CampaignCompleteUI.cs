using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TanksRebirth.Enums;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.Systems;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common;
using TanksRebirth.Internals.Common.Framework.Audio;
using TanksRebirth.Internals.Common.GameUI;
using TanksRebirth.Internals.Common.Utilities;

using Microsoft.Xna.Framework.Input;
using TanksRebirth.GameContent.ID;
using TanksRebirth.GameContent.RebirthUtils;
using TanksRebirth.GameContent.UI.MainMenu;
using TanksRebirth.GameContent.Systems.AI;

namespace TanksRebirth.GameContent.UI;

#pragma warning disable
public static class CampaignCompleteUI {
    public static bool IsViewingResults;
    public static Rectangle ParseGradeRect(Grade grade) {
        // 128 being the width of each section.
        int col = (int)grade / 3;
        int row = (int)grade % 3;

        return new(col * 128, row * 128, 128, 128);
    }

    static float _animationTime;

    static readonly float _delayPerTank = 30f;
    //static readonly TimeSpan _delayPerTank = TimeSpan.FromMilliseconds(500);

    public static Dictionary<int, int> KillsPerType = [];

    public static Grade Grade;

    static float _gradeAlpha;
    static float _gradeScale;
    static float _gradeScaleDefault = 3f;
    static float _gradeFadeSpeed = 0.1f;
    static float _gradeTargetScale = 1f;

    static float _panelAlpha = 0f;
    static float _panelAlphaMax = 1f;
    static float _panelFadeSpeed = 0.03f;

    static float _defaultSize = 2.5f;
    static float _appearSpeed = 0.1f;
    static float _tankSize = 1.5f;
    static float _spacePerTank = 30f;
    static float _tnkDrawYOff = 200f;

    static int _curTier;

    static bool[] _tierDisplays;
    static float[] _tierAlphas;
    static float[] _tierScales;

    public static bool ForceSkip;
    static bool _shouldShowGrade;
    private static Color _textColor = Color.DarkGray;

    public static int TanksPerColumn = 18;

    private static MissionEndContext _lastContext;

    public static Dictionary<MissionEndContext, (OggAudio, TimeSpan)> FanfaresAndDurations = new() {
        [MissionEndContext.CampaignCompleteMinor] = (new OggAudio("Content/Assets/music/fanfares/mission_complete_M20.ogg"), TimeSpan.FromMilliseconds(3400)), // 4327
        [MissionEndContext.CampaignCompleteMajor] = (new OggAudio("Content/Assets/music/fanfares/mission_complete_M100.ogg"), TimeSpan.FromMilliseconds(2600)), // 3350
        [MissionEndContext.GameOver] = (new OggAudio("Content/Assets/music/fanfares/gameover_playerdeath.ogg"), TimeSpan.FromMilliseconds(1400)) // 1883
    };
    public static OggMusic ResultsFanfare = new("Results Fanfare", "Content/Assets/music/fanfares/endresults.ogg", 0.6f);

    /// <summary>Perform a multithreaded operation that will display tanks killed and their kill counts for the player.</summary>
    public static void PerformSequence(MissionEndContext context) {
        IsViewingResults = true;

        _curTier = 0;
        _animationTime = 0;

        _lastContext = context;

        _animationTime = 0f;

        _shouldShowGrade = false;
        _gradeScale = _gradeScaleDefault;
        _gradeAlpha = 0f;
        _soundPlayed = false;

        if (context != MissionEndContext.GameOver)
            TankGame.SaveFile.CampaignsCompleted++;

        // TierDisplays?.Clear();
        // then reinitialize with the proper tank-to-kill values.
        // *insert code here*

        // finish this tomorrow i am very tired murder me.
        SetStats(CampaignGlobals.LoadedCampaign, PlayerTank.PlayerStatistics, PlayerTank.TankKills);
        Grade = FormulateGradeLevel(true);

        ForceSkip = false;

        /*Task.Run(async () => {
            while (_panelAlpha < 1f)
                await Task.Delay(TankGame.LastGameTime.ElapsedGameTime).ConfigureAwait(false);
            if (!ResultsFanfare.IsPlaying()) {
                ResultsFanfare.SetVolume(1f);
                ResultsFanfare.Play();
            }
            // sleep the thread for the duration of the fanfare.
            Vector2 basePos = new(WindowUtils.WindowWidth / 2, WindowUtils.WindowHeight * 0.2f);
            for (int i = 0; i < KillsPerType.Count; i++) {
                ModifyTracks();

                var killData = KillsPerType.ElementAt(i);

                _tierDisplays[i] = true;

                //ChatSystem.SendMessage($"{killData.Key}:{killData.Value}", Color.White);

                if (!ForceSkip) {
                    if (i % 4 == 3) // i think this is the value
                        SoundPlayer.PlaySoundInstance("Assets/sounds/results/whistle_double.ogg", SoundContext.Effect, 1f);
                    else
                        SoundPlayer.PlaySoundInstance("Assets/sounds/results/whistle_singular.ogg", SoundContext.Effect, 1f);

                    await Task.Delay(_delayPerTank).ConfigureAwait(false);

                    //ChatSystem.SendMessage("sleep", Color.White);
                }
            }

            //ChatSystem.SendMessage("whistle", Color.White);
            SoundPlayer.PlaySoundInstance("Assets/sounds/results/whistle_full.ogg", SoundContext.Effect, 1f);

            ForceSkip = true;
            _shouldShowGrade = true;

            // TODO: make this thread-safe. it's probably causing tank models to become null somehow
            while (!InputUtils.KeyJustPressed(Keys.Enter)) {
                ModifyTracks();
                await Task.Delay(TankGame.LastGameTime.ElapsedGameTime).ConfigureAwait(false);
            }

            _panelAlpha = 0f;
            _gradeAlpha = 0f;
            IsViewingResults = false;
            if (!ResultsFanfare.IsStopped())
                ResultsFanfare.Stop();
            MainMenuUI.Open();
        });*/
        // changing bools in other threads = safe
        // - ryan
    }

    public static void Update() {
        if (!ResultsFanfare.IsPlaying()) {
            ResultsFanfare.SetVolume(TankGame.Settings.MusicVolume);
            ResultsFanfare.Play();
        }
        if (_panelAlpha < _panelAlphaMax)
            return;
        // sleep the thread for the duration of the fanfare.
        Vector2 basePos = new(WindowUtils.WindowWidth / 2, WindowUtils.WindowHeight * 0.2f);

        if (_curTier < KillsPerType.Count) {
            if (ForceSkip ? true : _animationTime % _delayPerTank < RuntimeData.DeltaTime) {
                var killData = KillsPerType.ElementAt(_curTier);

                _tierDisplays[_curTier] = true;

                //ChatSystem.SendMessage($"{killData.Key}:{killData.Value}", Color.White);

                if (!ForceSkip) {
                    if (_curTier % 4 == 3) // i think this is the value
                        SoundPlayer.PlaySoundInstance("Assets/sounds/results/whistle_double.ogg", SoundContext.Effect, 1f);
                    else
                        SoundPlayer.PlaySoundInstance("Assets/sounds/results/whistle_singular.ogg", SoundContext.Effect, 1f);
                }
                if (_curTier == KillsPerType.Count - 1) {
                    ForceSkip = true;
                    _shouldShowGrade = true;
                    SoundPlayer.PlaySoundInstance("Assets/sounds/results/whistle_full.ogg", SoundContext.Effect, 1f);
                }

                _curTier++;
            }
        }

        if (DebugManager.DebuggingEnabled) {
            if (InputUtils.AreKeysJustPressed(Keys.OemOpenBrackets, Keys.OemCloseBrackets)) {
                ResetThings();

                PerformSequence(_lastContext);
            }
            else if (InputUtils.KeyJustPressed(Keys.P))
                ResetThings();
        }

        ResultsFanfare.SetVolume(TankGame.Instance.IsActive ? TankGame.Settings.MusicVolume : 0f);
        _animationTime += RuntimeData.DeltaTime;

        if (InputUtils.KeyJustPressed(Keys.Enter)) {
            SkipOrExit();
        }
    }
    static void SkipOrExit() {
        if (_curTier < KillsPerType.Count) {
            ForceSkip = true;
            _shouldShowGrade = true;

            return;
        }

        _curTier = 0;
        _panelAlpha = 0f;
        _gradeAlpha = 0f;
        IsViewingResults = false;

        // :(
        IntermissionSystem.IsAwaitingNewMission = false;
        if (!ResultsFanfare.IsStopped())
            ResultsFanfare.Stop();

        MainMenuUI.Open();
    }

    private static void ModifyTracks() {
        try {
            ResultsFanfare.SetVolume(TankGame.Instance.IsActive ? TankGame.Settings.MusicVolume : 0f);
        } catch { }
    }

    private static void ResetThings() {
        _appearSpeed = 0.1f;
        _defaultSize = 4f;
        _tankSize = 1.5f;
        _spacePerTank = 30f;
        _textColor = Color.DarkGray;
        _tnkDrawYOff = 200f;
        for (int j = 0; j < _tierDisplays.Length; j++) {
            _tierScales[j] = _defaultSize;
            _tierAlphas[j] = 0;
        }
    }
    private static bool _soundPlayed;
    public static void Render() {
        if (InputUtils.KeyJustPressed(Keys.Enter))
            ForceSkip = true;

        _panelAlpha += _panelFadeSpeed;
        if (_panelAlpha > _panelAlphaMax)
            _panelAlpha = _panelAlphaMax;
        float width = 400;
        // IntermissionSystem.BackgroundColor looks too dull
        TankGame.SpriteRenderer.Draw(TextureGlobals.Pixels[Color.White], new Vector2(0, WindowUtils.WindowHeight / 3), null, Color.Beige * _panelAlpha, 0f, Vector2.Zero, new Vector2(width.ToResolutionX(), WindowUtils.WindowHeight / 2), default, 0f);
        TankGame.SpriteRenderer.Draw(TextureGlobals.Pixels[Color.White], new Vector2(0, WindowUtils.WindowHeight / 3 + 50.ToResolutionY()), null, Color.Gold * _panelAlpha, 0f, Vector2.Zero, new Vector2(width, 5).ToResolution(), default, 0f);
        var txt = TankGame.GameLanguage.FunFacts;
        var measure = FontGlobals.RebirthFont.MeasureString(txt);
        DrawUtils.DrawStringWithShadow(TankGame.SpriteRenderer, FontGlobals.RebirthFont, new Vector2(width.ToResolutionX() / 2, WindowUtils.WindowHeight / 3 + 5.ToResolutionY()), Vector2.One,
            txt, Color.DeepSkyBlue, Vector2.One.ToResolution(), 1f, Anchor.TopCenter, 0.4f);

        string[] funFacts =
        {
            $"% Shots Hit: {ShotToKillRatio * 100:0}% ({ShellHits}/{ShellsFired})",
            $"% Mine Effect: {MineToKillRatio * 100:0}% ({MineHits}/{MinesLaid})",
            $"% Lives Earned: {LifeRatio * 100:0}% ({LivesRemaining}/{TotalPossibleLives})",
            $"% Missions Complete: {MissionRatio * 100:0}% ({CampaignGlobals.LoadedCampaign.CurrentMissionId + 1}/{CampaignGlobals.LoadedCampaign.CachedMissions.Length})"
        };
        for (int i = 0; i < funFacts.Length; i++) {
            var ff = funFacts[i];

            DrawUtils.DrawStringWithShadow(TankGame.SpriteRenderer, FontGlobals.RebirthFont, new Vector2(8.ToResolutionX(), WindowUtils.WindowHeight / 3 + (75 + (i * 25)).ToResolutionY()), Vector2.One,
                ff, Color.DeepSkyBlue, new Vector2(0.75f).ToResolution(), 1f, Anchor.LeftCenter, 0.4f);
        }

        TankGame.SpriteRenderer.Draw(TextureGlobals.Pixels[Color.White], new Vector2(WindowUtils.WindowWidth / 3, 0), null, Color.Beige * _panelAlpha, 0f, Vector2.Zero, new Vector2(WindowUtils.WindowWidth / 3, WindowUtils.WindowHeight), default, 0f);
        TankGame.SpriteRenderer.Draw(TextureGlobals.Pixels[Color.White], new Vector2(WindowUtils.WindowWidth / 3, (_tnkDrawYOff - 50f).ToResolutionY()), null, Color.Gold * _panelAlpha, 0f, Vector2.Zero, new Vector2(WindowUtils.WindowWidth / 3, 10.ToResolutionY()), default, 0f);

        if (_shouldShowGrade) {
            _gradeAlpha += _gradeFadeSpeed / 2 * RuntimeData.DeltaTime;
            _gradeScale -= _gradeFadeSpeed * RuntimeData.DeltaTime;
            if (_gradeAlpha > 1f)
                _gradeAlpha = 1f;
            if (_gradeScale <= _gradeTargetScale) {
                _gradeScale = _gradeTargetScale;

                if (!_soundPlayed) {
                    SoundPlayer.PlaySoundInstance("Assets/sounds/results/punch.ogg", SoundContext.Effect);
                    _soundPlayed = true;
                }
            }
            var tex = GameResources.GetGameResource<Texture2D>("Assets/textures/ui/grades");
            TankGame.SpriteRenderer.Draw(tex, new Vector2(WindowUtils.WindowWidth / 3 * 2, 250.ToResolutionY()), ParseGradeRect(Grade), Color.White * _gradeAlpha, 0f, new Vector2(64, 64), new Vector2(_gradeScale).ToResolution(), default, 0f);
        }

        #region Tank Graphics
        float offY = 0;
        int xAdjust = 200;
        for (int i = 0; i < KillsPerType.Count; i++) {
            var display = KillsPerType.ElementAt(i);

            if (_tierDisplays[i]) {
                #region Handle Appearance

                _tierScales[i] -= _appearSpeed * RuntimeData.DeltaTime;
                _tierAlphas[i] += _appearSpeed * RuntimeData.DeltaTime;

                if (_tierScales[i] < _tankSize)
                    _tierScales[i] = _tankSize;

                if (_tierAlphas[i] > 1f)
                    _tierAlphas[i] = 1f;
                #endregion

                var xAdjustCount = (int)Math.Floor((float)i / TanksPerColumn);

                var texture = GameResources.GetGameResource<Texture2D>("Assets/textures/ui/tank2d");

                // draw the tank graphic
                // i love weird math.
                // GameUtils.ToResolution is kinda wack here, so i'll exclude it.
                DrawUtils.DrawTextureWithShadow(TankGame.SpriteRenderer, texture,
                    new Vector2(
                        WindowUtils.WindowWidth / 3 + WindowUtils.WindowWidth * 0.025f + xAdjust * xAdjustCount * ((float)WindowUtils.WindowWidth / 1920),
                        _tnkDrawYOff.ToResolutionY() + offY - (xAdjustCount * _spacePerTank * TanksPerColumn * _tankSize * ((float)WindowUtils.WindowHeight / 1080))),
                    Vector2.One, AITank.TankDestructionColors[display.Key],
                    new Vector2(_tierScales[i]) * (WindowUtils.WindowBounds / new Vector2(1920, 1080)),
                    _tierAlphas[i], Anchor.Center, shadowDistScale: 0.4f, shadowAlpha: 0.5f);
                // draw the name of the tank just to make things nicer.
                DrawUtils.DrawStringWithShadow(TankGame.SpriteRenderer, FontGlobals.RebirthFont,
                    new Vector2(
                        WindowUtils.WindowWidth / 3 + WindowUtils.WindowWidth * 0.025f + xAdjust * xAdjustCount * ((float)WindowUtils.WindowWidth / 1920),
                        _tnkDrawYOff.ToResolutionY() + offY - (-5f + xAdjustCount * _spacePerTank * TanksPerColumn * _tankSize * ((float)WindowUtils.WindowHeight / 1080))),
                    Vector2.One, $"{TankID.Collection.GetKey(display.Key)}", Color.LightGray, new Vector2(_tierScales[i]) * (WindowUtils.WindowBounds / new Vector2(1920, 1080)) * 0.3f, _tierAlphas[i], Anchor.Center,
                    shadowDistScale: 0.4f, shadowAlpha: 0.5f);
                // draw the kill count text
                DrawUtils.DrawStringWithShadow(TankGame.SpriteRenderer, FontGlobals.RebirthFont,
                    new Vector2(
                        WindowUtils.WindowWidth / 3 + WindowUtils.WindowWidth * 0.025f + (xAdjust / 2).ToResolutionX() + xAdjust * xAdjustCount * ((float)WindowUtils.WindowWidth / 1920),
                        _tnkDrawYOff.ToResolutionY() - 2f.ToResolutionY() + offY - (xAdjustCount * _spacePerTank * TanksPerColumn * _tankSize * ((float)WindowUtils.WindowHeight / 1080))),
                    Vector2.One, $"{display.Value}", _textColor, new Vector2(_tierScales[i]) * (WindowUtils.WindowBounds / new Vector2(1920, 1080)), _tierAlphas[i], Anchor.Center,
                    shadowDistScale: 0.2f, shadowAlpha: 0.5f);

                offY += _spacePerTank * _tankSize * ((float)WindowUtils.WindowHeight / 1080);
            }
        }
        #endregion
        txt = ForceSkip ? "Press 'Enter' to exit." : "Press 'Enter' to skip.";
        DrawUtils.DrawStringWithShadow(TankGame.SpriteRenderer, FontGlobals.RebirthFont, new Vector2(WindowUtils.WindowWidth / 2, WindowUtils.WindowHeight - 8), Vector2.One, txt, Color.Black, Vector2.One.ToResolution(), 1f, Anchor.BottomCenter, 0.4f);

        txt = "Campaign Results";
        DrawUtils.DrawStringWithShadow(TankGame.SpriteRenderer, FontGlobals.RebirthFontLarge, new Vector2(WindowUtils.WindowWidth / 2, 8), Vector2.One, txt, Color.DeepSkyBlue, new Vector2(0.5f).ToResolution(), _panelAlpha, Anchor.TopCenter);
    }
    // TODO: probably support multiple players L + ratio me
    public static void SetStats(Campaign campaign, PlayerTank.CampaignStats stats, Dictionary<int, int> killCounts, bool orderByTier = true) {
        // set everything properly...
        KillsPerType = killCounts;
        _tierAlphas = new float[killCounts.Count];
        _tierScales = new float[killCounts.Count];
        _tierDisplays = new bool[killCounts.Count];

        Array.Fill(_tierScales, _defaultSize);

        ShellsFired = stats.ShellsShot;
        ShellHits = stats.ShellHits;

        MinesLaid = stats.MinesLaid;
        MineHits = stats.MineHits;

        SuicideCount = stats.Suicides;

        LivesRemaining = PlayerTank.GetMyLives();

        // then, we determine the number of possible lives by checking how many extra life missions
        // there were this campaign, along with adding the starting life count.
        TotalPossibleLives = PlayerTank.StartingLives + campaign.CachedMissions.Count(x => x.GrantsExtraLife);

        if (orderByTier)
            KillsPerType = KillsPerType.OrderBy(tier => tier.Key).ToDictionary(x => x.Key, y => y.Value);
    }

    public static Grade FormulateGradeLevel(bool useMissionCompletionRatio) {
        var grade = Grade.APlus;

        ShotToKillRatio = (float)ShellHits / ShellsFired;
        MineToKillRatio = (float)MineHits / MinesLaid;
        LifeRatio = (float)LivesRemaining / TotalPossibleLives;
        MissionRatio = (float)(CampaignGlobals.LoadedCampaign.CurrentMissionId + 1) / CampaignGlobals.LoadedCampaign.CachedMissions.Length;

        if (float.IsNaN(ShotToKillRatio))
            ShotToKillRatio = 1f;
        if (float.IsNaN(MineToKillRatio))
            MineToKillRatio = 1f;

        // (min, max]
        bool isBetween(float input, float min, float max) => input >= min && input < max;
        // redundant code but whatever i love men
        if (ShotToKillRatio > 0.4f)
            grade += 0;
        else if (isBetween(ShotToKillRatio, 0.2f, 0.4f))
            grade += 1;
        else if (isBetween(ShotToKillRatio, 0.1f, 0.2f))
            grade += 2;
        else
            grade += 3;

        // too harsh i think.
        /*if (MineToKillRatio >= 0.25f)
            grade += 0;
        else
            grade += 1;*/

        // check >= just incase something goofy happens.
        // 
        if (LifeRatio >= 0.5f)
            grade += 0;
        else if (isBetween(LifeRatio, 0.3f, 0.5f))
            grade += 1;
        else if (isBetween(LifeRatio, 0.15f, 0.3f))
            grade += 2;
        else if (isBetween(LifeRatio, 0.05f, 0.15f))
            grade += 3;
        else if (LifeRatio < 0.05f)
            grade += 4;

        if (useMissionCompletionRatio) {
            if (MissionRatio >= 0.75f)
                grade += 0;
            else if (isBetween(LifeRatio, 0.5f, 0.75f))
                grade += 1;
            else if (isBetween(LifeRatio, 0.3f, 0.5f))
                grade += 2;
            else if (isBetween(LifeRatio, 0.2f, 0.3f))
                grade += 3;
            else if (isBetween(LifeRatio, 0.1f, 0.2f))
                grade += 4;
            else
                grade += 5;
        }

        grade += SuicideCount / 2;

        if (grade > Grade.FMinus)
            grade = Grade.FMinus;

        return grade;
    }

    // overall stats throughout the campaign
    // these will factor into the grade level

    public static int ShellsFired;
    public static int ShellHits;

    // considering not using this...
    public static int MinesLaid;
    public static int MineHits;

    public static int TotalPossibleLives;
    public static int LivesRemaining;

    public static int SuicideCount;

    public static float ShotToKillRatio;
    public static float MineToKillRatio;
    public static float LifeRatio;
    public static float MissionRatio;
}
