using DiscordRPC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WiiPlayTanksRemake
{
    internal static class DiscordRichPresence
    {
        private static RichPresence _rpc;

        private static DiscordRpcClient rpcClient;

        private static Button rp_Button_Git;
        private static Button rp_Button_Disc;

        public static void Load()
        {
            rpcClient = new DiscordRpcClient("937910981844168744");

            rp_Button_Git = new Button
            {
                Label = "GitHub",
                Url = "https://github.com/RighteousRyan1/WiiPlayTanksRemake"
            };
            rp_Button_Disc = new Button
            {
                Label = "Discord",
                Url = "https://discord.gg/KhfzvbrrKx"
            };

            _rpc = new RichPresence
            {
                Buttons = new Button[] { rp_Button_Git, rp_Button_Disc } 
            };

            _rpc.Assets = new();

            _rpc.Timestamps = new Timestamps()
            {
                Start = DateTime.UtcNow,
            };
            rpcClient?.SetPresence(_rpc);
            rpcClient.Initialize();
        }

        public static void Update()
        {
            if (!rpcClient.IsDisposed)
            {
                static string getGoodGrammar(string word)
                {
                    if (word.StartsWith('a') || word.StartsWith('e') || word.StartsWith('i') || word.StartsWith('o') || word.StartsWith('u'))
                    {
                        return "an";
                    }
                    else
                    {
                        return "a";
                    }
                }

                var curTank = GameContent.AITank.GetHighestTierActive();

                SetLargeAsset("tank_ash_large");
                SetSmallAsset($"tank_{curTank.ToString().ToLower()}", $"Currently fighting {getGoodGrammar(curTank.ToString())} {GameContent.AITank.GetHighestTierActive()} Tank");
                SetDetails($"Fighting a grand total of {GameContent.AITank.CountAll()} tanks!");
                
                rpcClient?.SetPresence(_rpc);
            }
        }

        public static void SetDetails(string details)
        {
            _rpc.Details = details;
        }

        public static void SetLargeAsset(string key, string details = null)
        {
            _rpc.Assets.LargeImageKey = key;

            if (details is not null)
                _rpc.Assets.LargeImageText = details;
        }

        public static void SetSmallAsset(string key, string details = null)
        {
            _rpc.Assets.SmallImageKey = key;

            if (details is not null)
                _rpc.Assets.SmallImageText = details;
        }

        public static void SetParty(Party party, int size = -1)
        {
            _rpc.Party = party;
            if (size > -1)
                _rpc.Party.Size = size;
        }

        public static void SetState(string state)
        {
            _rpc.State = state;
        }

        /// <summary>Stop handling Discord's Rich Presence feature. Disposes of the client and updates the endtime.</summary>
        public static void Terminate()
        {
            rpcClient?.UpdateEndTime(DateTime.UtcNow);
            rpcClient?.Dispose();
        }
    }
}
