using System.IO;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.RebirthUtils;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Internals;
using TanksRebirth.GameContent.Systems.Coordinates;
using TanksRebirth.Net;
using TanksRebirth.GameContent.Systems.LevelSystem;
using TanksRebirth.GameContent.Tanks;

namespace TanksRebirth.GameContent.UI.MainMenu;

#pragma warning disable
public static partial class MainMenuUI {
    static bool _firstTime = true;
    public static void UpdateGameplay() {
        if (!TankGame.Settings.Gameplay.MenuGameplayEnabled) return;

        if (IntermissionSystem.IsAwaitingNewMission || IntermissionSystem.BlackAlpha > 0) return;
        if (curMenuMission.Blocks is null) {
            LoadTemplateMission();
            return;
        }
        // do not count player tanks into the check
        var missionComplete = CampaignProgression.VanillaCheckCompletion(curMenuMission, out _, (t) => t is not PlayerTank);

        if (missionComplete)
            LoadTemplateMission();
    }
    public static void OpenGP() {
        SceneManager.CleanupScene();
        SceneManager.CleanupEntities();
        PlayerTank.TankKills.Clear();

        if (!TankGame.Settings.Gameplay.MenuGameplayEnabled) return;

        LoadTemplateMission();
    }
    public static void LeaveGP() {
        SceneManager.StartTnkScene();
        PlayerTank.SetLives(PlayerTank.StartingLives);
        SceneManager.CleanupEntities();
        EditorTile.ResetSquares();
        SceneManager.CleanupScene();
        Theme.Stop();
    }
    static void LoadTemplateMission(bool autoSetup = true, bool loadForMenu = true) {
        // something here or after here is causing players' games to crash
        if (_firstTime) {
            var initPath = Path.Combine("Content", "MenuMissions");
            for (int i = 1; ; i++) {
                var path = Path.Combine(initPath, $"Menu{i}.mission");
                var exists = File.Exists(path);
                if (!exists) break;

                _cachedMissions.Add(Mission.Load(path));
            }
            TankGame.ClientLog.Write($"Loaded {_cachedMissions.Count} missions from MenuMissions.", RebirthLogLevel.Info);
        }

        // now actually load a template
        var rand = Client.ClientRandom.Next(1, _cachedMissions.Count);

        var mission = _cachedMissions[rand];

        if (autoSetup) {
            CampaignGlobals.LoadedCampaign.LoadMission(mission);
            CampaignGlobals.LoadedCampaign.SetupLoadedMission(true);
        }
        if (loadForMenu)
            curMenuMission = mission;
    }

}
