using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TanksRebirth.GameContent.Systems
{
    public record Difficulties
    {
        public static Dictionary<string, bool> Types = new()
        {
            ["TanksAreCalculators"] = false,
            ["PieFactory"] = false,
            ["UltraMines"] = false,
            ["BulletHell"] = false,
            ["AllInvisible"] = false,
            ["AllStationary"] = false,
            ["AllHoming"] = false,
            ["Armored"] = false,
            ["BumpUp"] = false,
            ["MeanGreens"] = false,
            ["InfiniteLives"] = false,
            ["MasterModBuff"] = false,
            ["MarbleModBuff"] = false,
            ["MachineGuns"] = false,
            ["RandomizedTanks"] = false,
            ["ThunderMode"] = false,
            ["ThirdPerson"] = false,
            ["AiCompanion"] = false,
            ["Shotguns"] = false,
            ["Predictions"] = false,
        };
    }
}
