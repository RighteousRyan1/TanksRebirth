using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WiiPlayTanksRemake.GameContent.Systems
{
    /// <summary>
    /// This is not static due to the ability of custom missions on the future, as well as campaigns.
    /// </summary>
    public class MissionSystem
    {
        public Mission[] CachedMissions { get; set; } = new Mission[100];
        public Mission CurrentMission { get; private set; }

        public int CurrentMissionId { get; private set; }

        public void LoadMission(Mission mission)
            => CurrentMission = mission;
        public void LoadMission(int id)
            => CurrentMission = CachedMissions[id];

        public void LoadMissionsToCache(params Mission[] missions)
        {
            var list = CachedMissions.ToList();

            list.AddRange(missions);

            CachedMissions = list.ToArray();
        }

        public void LoadNextMission()
            => CurrentMission = CachedMissions[++CurrentMissionId];

        public void SetupLoadedMission()
        {
            for (int a = 0; a < WPTR.AllTanks.Length; a++)
                WPTR.AllTanks[a] = null;

            for (int i = 0; i < CurrentMission.Tanks.Length; i++)
            {
                var tnk = CurrentMission.Tanks[i];

                WPTR.AllTanks[i] = tnk;
            }
        }
    }

    public struct Mission
    {
        public Tank[] Tanks { get; }

        public Vector3[] SpawnPositions { get; }

        public float[] SpawnOrientations { get; }

        public Cube[] Cubes { get; }

        public Mission(Tank[] tanks, Vector3[] spawnPositions, float[] spawnOrientations, Cube[] obstacles)
        {
            Tanks = tanks;
            SpawnPositions = spawnPositions;
            SpawnOrientations = spawnOrientations;
            Cubes = obstacles;
        }
    }

}
