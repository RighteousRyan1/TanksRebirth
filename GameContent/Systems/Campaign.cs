using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WiiPlayTanksRemake.Internals;

namespace WiiPlayTanksRemake.GameContent.Systems
{
    /// <summary>A campaign for players to play on with <see cref="AITank"/>s, or even <see cref="PlayerTank"/>s if supported.</summary>
    public class Campaign
    {
        public Mission[] CachedMissions { get; set; } = new Mission[100];
        public Mission CurrentMission { get; private set; }

        public int CurrentMissionId { get; private set; }

        public void LoadMission(Mission mission)
            => CurrentMission = mission;
        public void LoadMission(int id)
            => CurrentMission = CachedMissions[id];

        /// <summary>Loads an array of <see cref="Mission"/>s into the cache.</summary>
        public void LoadMissionsToCache(params Mission[] missions)
        {
            var list = CachedMissions.ToList();

            list.AddRange(missions);

            CachedMissions = list.ToArray();
        }

        /// <summary>Loads the next mission in the <see cref="Campaign"/>.</summary>
        public void LoadNextMission()
        {
            if (CachedMissions[++CurrentMissionId].Tanks is null || ++CurrentMissionId >= 100)
            {
                WPTR.ClientLog.Write($"CachedMissions[{++CurrentMissionId}] is not existent.", LogType.Warn);
                return;
            }

            CurrentMission = CachedMissions[++CurrentMissionId];
        }

        /// <summary>Sets up the <see cref="Mission"/> that is loaded.</summary>
        public void SetupLoadedMission()
        {
            if (CurrentMission.Tanks is null && CurrentMission.Cubes is null)
            {
                WPTR.ClientLog.Write("No mission loaded. Mission setup canceled.", LogType.Error);
                return;
            }

            for (int a = 0; a < WPTR.AllTanks.Length; a++)
                WPTR.AllTanks[a] = null;

            for (int i = 0; i < CurrentMission.Tanks.Length; i++)
            {
                var tnk = CurrentMission.Tanks[i];

                tnk.position = CurrentMission.SpawnPositions[i];
                tnk.TankRotation = CurrentMission.SpawnOrientations[i];
                if (tnk is AITank ai)
                    ai.targetTankRotation = CurrentMission.SpawnOrientations[i] + MathHelper.Pi;
                tnk.TurretRotation = CurrentMission.SpawnOrientations[i] - MathHelper.TwoPi;
                tnk.Dead = false;

                WPTR.AllTanks[i] = tnk;

                WPTR.ClientLog.Write($"loaded: {(tnk as AITank).tier}", LogType.Debug);
            }

            if (CurrentMission.Cubes.Length > 0)
            {
                for (int a = 0; a < Cube.cubes.Length; a++)
                    Cube.cubes[a] = null;

                for (int b = 0; b < CurrentMission.Cubes.Length; b++)
                {
                    var cube = CurrentMission.Cubes[b];

                    cube.position = CurrentMission.CubePositions[b];


                    Cube.cubes[b] = cube;
                }
            }
        }
    }

    public struct Mission
    {
        /// <summary>The <see cref="Tank"/>s that will be spawned.</summary>
        public Tank[] Tanks { get; }

        /// <summary>The position of the spawned <see cref="Tank"/>s.</summary>
        public Vector3[] SpawnPositions { get; }

        /// <summary>The rotation of the spawned <see cref="Tank"/>s.</summary>
        public float[] SpawnOrientations { get; }

        /// <summary>The obstacles in the <see cref="Mission"/>.</summary>
        public Cube[] Cubes { get; }

        public CubeMapPosition[] CubePositions { get; }

        public Mission(Tank[] tanks, Vector3[] spawnPositions, float[] spawnOrientations, Cube[] obstacles, CubeMapPosition[] cubePositions)
        {
            sbyte cBlue = 0;
            sbyte cRed = 0;

            if (tanks.Any(tnk => tnk is PlayerTank && (tnk as PlayerTank).PlayerType == Enums.PlayerType.Blue))
                cBlue++;

            if (tanks.Any(tnk => tnk is PlayerTank && (tnk as PlayerTank).PlayerType == Enums.PlayerType.Red))
                cRed++;

            if (cBlue > 1 || cRed > 1)
                WPTR.ClientLog.Write("Only one color allowed per-player.", LogType.Error, true);

            if (cBlue + cRed > 2)
                WPTR.ClientLog.Write("As of now, only 2 local players are supported.", LogType.Error, true);

            if (obstacles.Length > 0 && cubePositions.Length == 0)
                WPTR.ClientLog.Write("Obstacles are present but not assigned positions.", LogType.Error, true);

            if (tanks.Length > 0 && spawnPositions.Length == 0)
                WPTR.ClientLog.Write("Tanks are present but not assigned positions.", LogType.Error, true);

            Tanks = tanks;
            SpawnPositions = spawnPositions;
            SpawnOrientations = spawnOrientations;
            Cubes = obstacles;
            CubePositions = cubePositions;
        }
    }

}
