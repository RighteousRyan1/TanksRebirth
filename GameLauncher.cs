using Microsoft.Xna.Framework;
using System;
using System.Diagnostics;
using TanksRebirth;

namespace TanksRebirth
{
    public static class GameLauncher
    {
        public static bool AutoLaunch = true;
        public static bool IsRunning { get; private set; }
        public static void LaunchGame()
        {
            IsRunning = true;
            //try {
                using var game = new TankGame();
                game.Run();
            //}
            //catch (Exception e) /*when (!Debugger.IsAttached)*/ {
                //TankGame.WriteError(e);
                // throw;
            //}
        }
        [STAThread]
        static void Main()
        {
            if (AutoLaunch)
                LaunchGame();
        }

        public static void CloseGame()
            => TankGame.Instance.Exit();
    }
}