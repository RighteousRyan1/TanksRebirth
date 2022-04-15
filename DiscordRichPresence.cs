using DiscordRPC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TanksRebirth.GameContent;
using TanksRebirth.GameContent.Systems;
using TanksRebirth.GameContent.UI;

namespace TanksRebirth
{
    internal static class DiscordRichPresence
    {
        private static RichPresence _rp;

        private static DiscordRpcClient _client;

        private static Button _rpButtonGit;
        private static Button _rpButtonDiscord;

        public static void Load()
        {
            _client = new DiscordRpcClient("937910981844168744");

            _rpButtonGit = new Button
            {
                Label = "GitHub",
                Url = "https://github.com/RighteousRyan1/TanksRebirth"
            };
            _rpButtonDiscord = new Button
            {
                Label = "Discord",
                Url = "https://discord.gg/KhfzvbrrKx"
            };

            _rp = new RichPresence
            {
                Buttons = new Button[] { _rpButtonGit, _rpButtonDiscord },
            };

            _rp.Assets = new();
            
            _rp.Timestamps = new Timestamps()
            {
                Start = DateTime.UtcNow,
            };
            _client?.SetPresence(_rp);
            _client.Initialize();
        }
        private static string tnkCnt;
        public static void Update()
        {
            if (!_client.IsDisposed)
            {
                static string getArticle(string word)
                {
                    if (word.ToLower().StartsWith('a') || word.ToLower().StartsWith('e') || word.ToLower().StartsWith('i') || word.ToLower().StartsWith('o') || word.ToLower().StartsWith('u'))
                    {
                        return "an";
                    }
                    else
                    {
                        return "a";
                    }
                }

                if (MainMenu.Active)
                {
                    SetDetails($"Browsing the main menu");
                    SetLargeAsset("tank_ash_large", $"Gaming on version v{TankGame.Instance.GameVersion}");
                }
                else
                {
                    tnkCnt = $"Fighting {AITank.CountAll()} tank(s)";
                    // get the names of each difficulty mode active, then join them together
                    if (GameHandler.ShouldMissionsProgress)
                    {
                        SetDetails($"Playing campaign '{GameHandler.LoadedCampaign.Properties.Name}' on mission '{GameHandler.LoadedCampaign.CurrentMission.Name}'" +
                            $"\n{tnkCnt}");
                    }
                    else
                    {
                        if (TankGame.OverheadView)
                            SetDetails($"Editing a level");
                        else
                            SetDetails($"Playing freeplay | {tnkCnt}");
                    }

                    SetSmallAsset($"tank_{AITank.GetHighestTierActive().ToString().ToLower()}", $"Currently fighting {getArticle(AITank.GetHighestTierActive().ToString())} {AITank.GetHighestTierActive()} Tank");
                }
                
                _client?.SetPresence(_rp);
            }
        }

        public static void SetDetails(string details)
        {
            _rp.Details = details;
        }

        public static void SetLargeAsset(string key = null, string details = null)
        {
            if (key is not null)
                _rp.Assets.LargeImageKey = key;

            if (details is not null)
                _rp.Assets.LargeImageText = details;
        }

        public static void SetSmallAsset(string key = null, string details = null)
        {
            if (key is not null)
                _rp.Assets.SmallImageKey = key;

            if (details is not null)
                _rp.Assets.SmallImageText = details;
        }

        public static void SetParty(Party party, int size = -1)
        {
            _rp.Party = party;
            if (size > -1)
                _rp.Party.Size = size;
        }

        public static void SetState(string state)
        {
            _rp.State = state;
        }

        /// <summary>Stop handling Discord's Rich Presence feature. Disposes of the client and updates the endtime.</summary>
        public static void Terminate()
        {
            _client?.UpdateEndTime(DateTime.UtcNow);
            _client?.Dispose();
        }
    }
}
