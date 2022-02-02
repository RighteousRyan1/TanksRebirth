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
                rpcClient?.SetPresence(_rpc);
            }
        }
        public static void SetDetails(string details)
        {
            rpcClient.UpdateDetails(details);
        }
        public static void Terminate()
        {
            rpcClient?.UpdateEndTime(DateTime.UtcNow);
            rpcClient?.Dispose();
        }
    }
}
