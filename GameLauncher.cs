using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace TanksRebirth;

// GENERAL PROBLEMS:
// Something appears to be seriously eating at the game's performance. spikes happen but i'm not sure if it's just steam or what?
// Could be expensive calculations with AI Controlled tanks in the menu
public static partial class GameLauncher
{
    public static bool AutoLaunch = true;
    public static bool IsRunning { get; private set; }
    public static void LaunchGame() {
        IsRunning = true;
        using var game = new TankGame();
        game.Run();
    }

    [LibraryImport("Kernel32.dll", EntryPoint = "AllocConsole", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool AllocConsole();
    
    [Conditional("DEBUG")]
    public static void DebugCheck() {
        /*
         *  Boot up console for debugging purposes and other goods.
         *  ...Only if a debugger is attached.
         */
        if (!Debugger.IsAttached) return;
        AllocConsole();
        Thread.Sleep(1000);
        Console.OpenStandardOutput();
        Console.OpenStandardError();
    }
    
    //[STAThread]
    static void Main() {

        DebugCheck();
        
        if (AutoLaunch)
            LaunchGame();
    }

    public static void CloseGame() => TankGame.Instance.Exit();
}