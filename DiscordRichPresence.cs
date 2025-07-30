using DiscordRPC;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using TanksRebirth.GameContent;
using TanksRebirth.GameContent.Globals;
using TanksRebirth.GameContent.Systems;
using TanksRebirth.GameContent.Systems.AI;
using TanksRebirth.GameContent.UI.LevelEditor;
using TanksRebirth.GameContent.UI.MainMenu;
using TanksRebirth.Net;

namespace TanksRebirth;

public static class DiscordRichPresence {
    private static RichPresence? _rp;

    private static DiscordRpcClient? _client;

    private static Button? _rpButtonGit;
    private static Button? _rpButtonDiscord;

    public static void Load() {
        _client = new DiscordRpcClient("937910981844168744");

        _rpButtonGit = new Button {
            Label = "GitHub",
            Url = "https://github.com/RighteousRyan1/TanksRebirth"
        };
        _rpButtonDiscord = new Button {
            Label = "Discord",
            Url = "https://discord.gg/KhfzvbrrKx"
        };

        _rp = new RichPresence {
            Buttons = new Button[] { _rpButtonGit, _rpButtonDiscord },
        };

        _rp.Assets = new();

        _rp.Timestamps = new Timestamps() {
            Start = DateTime.UtcNow,
        };
        _client?.SetPresence(_rp);

        SetLargeAsset("tanks_physical_logo", $"v{RuntimeData.ShortVersion}");
        _client?.Initialize();
    }
    static string? tnkCnt;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static string GetArticle(string word) {
        return "aeiou".Contains(char.ToLower(word[0])) ? "an" : "a";
    }
    public static void Update() {
        if (!_client!.IsDisposed) {
            if (MainMenuUI.IsActive) {
                switch (MainMenuUI.MenuState) {
                    case MainMenuUI.UIState.PrimaryMenu:
                    case MainMenuUI.UIState.PlayList:
                    default:
                        SetDetails("Browsing the main menu");
                        break;
                    case MainMenuUI.UIState.StatsMenu:
                        SetDetails("Looking at their all-time stats");
                        break;
                    case MainMenuUI.UIState.Settings:
                        SetDetails("Making things juuuust right");
                        break;
                    case MainMenuUI.UIState.Difficulties:
                        var count = Difficulties.Types.Count(diff => diff.Value);


                        SetDetails($"Challenging themselves with {count} {(count == 1 ? "difficulty" : "difficulties")}");
                        break;
                    case MainMenuUI.UIState.Cosmetics:
                        SetDetails("Viewing what's to come");
                        break;
                    case MainMenuUI.UIState.Mulitplayer:
                        if (Client.IsConnected()) {
                            var name = NetPlay.CurrentServer is not null ? NetPlay.CurrentServer.Name : "Loading...";
                            if (Client.IsHost())
                                SetDetails($"Hosting a multiplayer lobby '{name}'");
                            else
                                SetDetails($"In a multiplayer lobby '{name}'");
                        }
                        else
                            SetDetails("Creating a multiplayer server");
                        break;
                    case MainMenuUI.UIState.Campaigns:
                        // subtract one because "Freeplay" counts as a campaign, even though it really isn't
                        SetDetails($"Choosing one of their {MainMenuUI.campaignNames.Count - 1} campaigns to play");
                        break;
                }
            }
            else {
                //tnkCnt = $"Fighting {AITank.CountAll()} tank(s)";
                // get the names of each difficulty mode active, then join them together
                if (CampaignGlobals.ShouldMissionsProgress) {
                    SetDetails($"Campaign: '{CampaignGlobals.LoadedCampaign.MetaData.Name}' on '{CampaignGlobals.LoadedCampaign.CurrentMission.Name}' | Lives: {PlayerTank.Lives[NetPlay.GetMyClientId()]}");
                }
                else {
                    if (LevelEditorUI.IsActive)
                        SetDetails($"Editing a level");
                    else if (!LevelEditorUI.IsEditing)
                        SetDetails($"Playing freeplay | {tnkCnt}");
                    else if (LevelEditorUI.IsEditing)
                        SetDetails($"Testing a level");
                }

                var highestTierTank = AIManager.GetHighestTierActive();
                SetSmallAsset($"tank_{highestTierTank.ToString().ToLower()}", $"Currently fighting {GetArticle(highestTierTank.ToString())} {highestTierTank} Tank");
            }
            _client?.SetPresence(_rp);
        }
    }

    public static void SetDetails(string? details) {
        var count = Server.ConnectedClients is not null ? Server.ConnectedClients.Count(x => x != null) : 0;
        _rp!.Details = details + (Client.IsConnected() ? $" ({count}/{Server.MaxClients})" : string.Empty);
    }

    public static void SetLargeAsset(string? key = null, string? details = null) {
        if (key is not null)
            _rp!.Assets.LargeImageKey = key;

        if (details is not null)
            _rp!.Assets.LargeImageText = details;
    }

    public static void SetSmallAsset(string? key = null, string? details = null) {
        if (key is not null)
            _rp!.Assets.SmallImageKey = key;

        if (details is not null)
            _rp!.Assets.SmallImageText = details;
    }

    public static void SetParty(Party party, int size = -1) {
        _rp!.Party = party;
        if (size > -1)
            _rp.Party.Size = size;
    }

    public static void SetState(string state) {
        _rp!.State = state;
    }

    /// <summary>Stop handling Discord's Rich Presence feature. Disposes of the client and updates the endtime.</summary>
    public static void Terminate() {
        _client?.UpdateEndTime(DateTime.UtcNow);
        _client?.Dispose();
    }
}
