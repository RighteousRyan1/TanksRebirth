using Microsoft.Xna.Framework;
using System;
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
            using var game = new TankGame();
                game.Run();
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