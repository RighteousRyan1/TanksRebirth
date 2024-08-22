using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Linq;
using System.Threading.Tasks;
using TanksRebirth.Enums;
using TanksRebirth.GameContent.Properties;
using TanksRebirth.GameContent.Systems;
using TanksRebirth.GameContent.UI;
using TanksRebirth.Graphics;
using TanksRebirth.Internals;
using TanksRebirth.Internals.Common.Framework;
using TanksRebirth.Internals.Common.Framework.Audio;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Internals.UI;
using TanksRebirth.Net;

namespace TanksRebirth.GameContent.RebirthUtils; 
public static class SceneManager {

    public static Lighting.LightProfile GameLight = new() {
        Color = new(150, 150, 170),
        Brightness = 0.75f,
        //isNight = true
    };

    private static bool _musicLoaded;

    public delegate void LoadTankScene();
    public static event LoadTankScene? OnLoadTankScene;
    public delegate void MissionCleanupEvent();
    public static event MissionCleanupEvent? OnMissionCleanup;


    /// <summary>
    /// Uses a multithreaded approach to start the campaign results screen.
    /// </summary>
    /// <param name="delay">The delay of time before starting the results screen.</param>
    /// <param name="context">The context of which the campaign is ending.</param>
    public static void DoEndScene(TimeSpan delay, MissionEndContext context) {
        // i think this works.
        // only adjusts speed for when this is called.
        Task.Run(async () => {
            await Task.Delay(delay).ConfigureAwait(false);
            CampaignCompleteUI.PerformSequence(context);
        });
    }
    public static void HandleSceneVisuals() {
        if (Difficulties.Types["ThunderMode"])
            DoThunderStuff();
        else if (MapRenderer.Theme == MapTheme.Christmas) {
            GameLight.Color = new(50, 50, 50, 50);
            GameLight.Brightness = 0.4f;
            GameLight.Apply(false);
            TankGame.ClearColor = (Color.DeepSkyBlue.ToVector3() * 0.2f).ToColor();
            if (GameHandler.GameRand.NextFloat(0, 1) <= 0.3f) {

                // TODO: add some sort of snowflake limit because the damn renderer sucks ass.

                float y = 200f;

                float x = GameHandler.GameRand.NextFloat(-450f, 450f);
                float z = GameHandler.GameRand.NextFloat(-250f, 400f);

                int snowflake = GameHandler.GameRand.Next(0, 2);

                var p = GameHandler.Particles.MakeParticle(new Vector3(x, y, z), GameResources.GetGameResource<Texture2D>($"Assets/christmas/snowflake_{snowflake}"));

                p.Scale = new Vector3(GameHandler.GameRand.NextFloat(0.1f, 0.25f));

                Vector2 wind = new(0.05f, 0f);

                float weight = GameHandler.GameRand.NextFloat(0.05f, 0.15f);

                float rotFactor = GameHandler.GameRand.NextFloat(0.001f, 0.01f);

                p.UniqueBehavior = (a) => {
                    if (p.Position.Y <= 0) {
                        GeometryUtils.Add(ref p.Scale, -0.006f * TankGame.DeltaTime);
                        if (p.Scale.X <= 0)
                            p.Destroy();

                    }
                    else {
                        p.Position.X += wind.X * TankGame.DeltaTime;
                        p.Position.Y -= weight;
                        p.Position.Z += wind.Y * TankGame.DeltaTime;

                        p.Rotation2D += 0.01f * TankGame.DeltaTime;

                        p.Roll += rotFactor * TankGame.DeltaTime;

                        p.Pitch += (rotFactor / 2) * TankGame.DeltaTime;
                    }
                };
            }
        }
    }
    private static void DoThunderStuff() {
        if (IntermissionSystem.BlackAlpha > 0 || IntermissionSystem.Alpha >= 1f || MainMenu.Active || GameUI.Paused) {
            if (Thunder.SoftRain!.IsPlaying()) {
                Thunder.SoftRain.Instance.Stop();
                TankGame.ClearColor = Color.Black;

                GameLight.Color = new(150, 150, 170);
                SceneManager.GameLight.Brightness = 0.71f;

                GameLight.Apply(false);
            }
            return;
        }
        if (!Thunder.SoftRain.IsPlaying())
            Thunder.SoftRain.Instance.Play();
        Thunder.SoftRain.Instance.Volume = TankGame.Settings.AmbientVolume;


        // TODO: should the chance be scaled by tps?
        if (GameHandler.GameRand.NextFloat(0, 1f) <= 0.003f) {
            var rand = new Range<Thunder.ThunderType>(Thunder.ThunderType.Fast, Thunder.ThunderType.Instant2);
            var type = (Thunder.ThunderType)GameHandler.GameRand.Next((int)rand.Min, (int)rand.Max);

            if (!Thunder.Thunders.Any(x => x is not null && x.Type == type))
                new Thunder(type);
        }

        Thunder brightest = null;

        float minThresh = 0.005f;

        foreach (var thun in Thunder.Thunders) {
            if (thun is not null) {
                thun.Update();

                if (brightest is null)
                    brightest = thun;
                else
                    if (thun.CurBright > brightest.CurBright && thun.CurBright > minThresh)
                    brightest = thun;
            }
        }

        GameLight.Color = Color.Multiply(Color.DeepSkyBlue, 0.5f); // DeepSkyBlue


        if (brightest is not null) {
            TankGame.ClearColor = Color.DeepSkyBlue * brightest.CurBright;
            GameLight.Brightness = brightest.CurBright / 6;
        }
        else
            GameLight.Brightness = minThresh;

        GameLight.Apply(false);
    }
    public static void CleanupScene(bool sync = false) {
        if (sync)
            Client.SyncCleanup();

        foreach (var mine in Mine.AllMines)
            mine?.Remove();

        foreach (var bullet in Shell.AllShells)
            bullet?.Remove();

        foreach (var expl in Explosion.Explosions)
            expl?.Remove();

        foreach (var crate in Crate.crates)
            crate?.Remove();

        foreach (var pu in Powerup.Powerups)
            pu?.Remove();

        ClearTankDeathmarks();
        ClearTankTracks();

        OnMissionCleanup?.Invoke();
    }
    public static void LoadGameScene() {
        if (!_musicLoaded) {
            OnLoadTankScene?.Invoke();
            _musicLoaded = true;
        }
        else {
            foreach (var song in TankMusicSystem.Audio)
                song.Value.Stop();
            TankMusicSystem.SnowLoop.Stop();
            TankMusicSystem.SnowLoop.Play();
        }
    }
    public static void ClearTankDeathmarks() {
        for (int i = 0; i < TankDeathMark.deathMarks.Length; i++) {
            TankDeathMark.deathMarks[i]?.check?.Destroy();
            TankDeathMark.deathMarks[i] = null;
        }

        TankDeathMark.total_death_marks = 0;
    }
    public static void CleanupEntities() {
        for (int a = 0; a < Block.AllBlocks.Length; a++)
            Block.AllBlocks[a]?.Remove();
        for (int a = 0; a < GameHandler.AllTanks.Length; a++)
            GameHandler.AllTanks[a]?.Remove(true);
    }
    public static void ClearTankTracks() {
        for (int i = 0; i < TankFootprint.footprints.Length; i++) {
            TankFootprint.footprints[i]?.Remove();
            TankFootprint.footprints[i] = null;
        }
    }
    public static void StartTnkScene() {
        DebugManager.DebuggingEnabled = false;

        IntermissionSystem.TextAnimatorLarge?.Restart();
        IntermissionSystem.TextAnimatorSmall?.Restart();
        GameLight.Apply(false);
    }
}
