using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WiiPlayTanksRemake.GameContent.ModSupport
{
    public class ModTank
    {
        public virtual string TierName => string.Empty;
        public virtual Team Team => Team.NoTeam;

        public AITank Tank { get; }

        internal int internal_tier;
        public int GetTier() => internal_tier;

        public void Spawn(CubeMapPosition position)
        {
            var t = new AITank(Tank.modTier, false)
            {
                position = position,
            };
        }

        public virtual bool BulletFound() => true;

        public virtual bool MineFound() => true;

        public virtual bool MinePlaced() => true;

        public virtual bool ShellFired() => true;

        public virtual bool TargetLocked() => true;
    }
}
