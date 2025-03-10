using System.IO;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.RebirthUtils;
using TanksRebirth.GameContent.Systems;
using TanksRebirth.Internals.Common.Utilities;
using TanksRebirth.Internals;
using TanksRebirth.GameContent.Systems.Coordinates;

namespace TanksRebirth.GameContent.UI.MainMenu;

#pragma warning disable
public static partial class MainMenuUI {
    private static bool _firstTime = true;
    // time after nothing can happen untiul next mission is loaded
    private static float _newMisCd;
    // default time for above field
    private static float _timeToWait = 180;
    public static void UpdateGameplay() {
        if (!IntermissionSystem.IsAwaitingNewMission || IntermissionSystem.BlackAlpha <= 0f) {
            if (curMenuMission.Blocks != null) {
                var missionComplete = IntermissionHandler.NothingCanHappenAnymore(curMenuMission, out bool victory);

                if (missionComplete) {
                    // TODO: finish.
                    _newMisCd += TankGame.DeltaTime;
                    if (_newMisCd > _timeToWait)
                        LoadTemplateMission();
                }
                else
                    _newMisCd = 0;
            }
        }
    }
    public static void OpenGP() {
        SceneManager.CleanupEntities();
        SceneManager.ClearTankTracks();
        SceneManager.ClearTankDeathmarks();
        PlayerTank.TankKills.Clear();

        LoadTemplateMission();
    }
    public static void LeaveGP() {
        SceneManager.StartTnkScene();
        PlayerTank.SetLives(PlayerTank.StartingLives);
        SceneManager.CleanupEntities();
        PlacementSquare.ResetSquares();
        SceneManager.CleanupScene();
        Theme.Stop();
    }
    private static void LoadTemplateMission(bool autoSetup = true, bool loadForMenu = true) {
        try {
            if (_firstTime) {
                var attempt = 1;

            tryAgain:
                var linkTry = $"https://github.com/RighteousRyan1/tanks_rebirth_motds/blob/master/menu_missions/Menu{attempt}.mission?raw=true";
                var exists = WebUtils.RemoteFileExists(linkTry);

                if (exists) {
                    var bytes1 = WebUtils.DownloadWebFile(linkTry, out var name1);

                    using var reader1 = new BinaryReader(new MemoryStream(bytes1));

                    _cachedMissions.Add(Mission.Read(reader1));
                    attempt++;
                    goto tryAgain;
                }

                _firstTime = false;
            }

            SceneManager.CleanupScene();

            var rand = GameHandler.GameRand.Next(1, _cachedMissions.Count);

            var mission = _cachedMissions[rand];

            if (autoSetup) {
                CampaignGlobals.LoadedCampaign.LoadMission(mission);
                CampaignGlobals.LoadedCampaign.SetupLoadedMission(true);
            }
            if (loadForMenu)
                curMenuMission = mission;
        }
        catch {
            TankGame.ClientLog.Write("Unable to fetch map data via the internet. Oops!", LogType.Warn);
        }
    }

}
