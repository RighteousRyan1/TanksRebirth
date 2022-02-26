using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WiiPlayTanksRemake.GameContent.Systems.Coordinates;

namespace WiiPlayTanksRemake.GameContent.ModSupport
{
    /// <summary>Represents a modded <see cref="GameContent.Tank"/>.</summary>
    public class ModTank
    {
        public virtual string TierName => string.Empty;
        public virtual Team Team => Team.NoTeam;

        public AITank Tank { get; }

        internal int internal_tier;
        public int GetTier() => internal_tier;

        public void Spawn(CubeMapPosition position)
        {
        }

        public virtual bool BulletFound() => true;

        public virtual bool MineFound() => true;

        public virtual bool MinePlaced() => true;

        public virtual bool ShellFired() => true;

        public virtual bool TargetLocked() => true;
    }
}
