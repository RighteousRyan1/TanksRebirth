using WiiPlayTanksRemake.Internals;
using WiiPlayTanksRemake.Internals.Common;
using WiiPlayTanksRemake.Internals.Common.Utilities;
using WiiPlayTanksRemake.Internals.Common.GameInput;
using Microsoft.Xna.Framework.Input;

namespace WiiPlayTanksRemake.GameContent
{
    class WiiPlayTanksRemake
    {
        public static Keybind PlaceMine = new("Place Mine", Keys.Space);

        public static Logger BaseLogger { get; } = new($"{TankGame.ExePath}", "client_logger");

        internal static void Update()
        {
            foreach (var music in Music.AllMusic)
                music?.Update();
            foreach (var tank in Tank.AllTanks)
            {
                tank?.Update();
                foreach (Mine mine in tank.minesLaid)
                    mine?.Update();
                foreach (Bullet bullet in tank.bulletsFired)
                    bullet?.Update();
            }
            if (TankGame.Instance.IsActive)
            {
                if (Input.MouseLeft && GameUtils.MouseOnScreenProtected)
                {
                    //shoot
                }

            }
        }

        internal static void Draw()
        {
            foreach (var tank in Tank.AllTanks)
            {
                tank?.Draw();
                foreach (Mine mine in tank.minesLaid)
                    mine?.Draw();
                foreach (Bullet bullet in tank.bulletsFired)
                    bullet?.Draw();
            }
        }
    }
}
