using LiteNetLib;
using LiteNetLib.Utils;
using Microsoft.Xna.Framework;
using System.Linq;
using TanksRebirth.GameContent;
using TanksRebirth.GameContent.ID;

namespace TanksRebirth.Net;

public static class NetExtensions {
    public static void Put(this NetDataWriter writer, Vector2 v) {
        writer.Put(v.X);
        writer.Put(v.Y);
    }
    public static void Put(this NetDataWriter writer, Vector3 vector) {
        writer.Put(vector.X);
        writer.Put(vector.Y);
        writer.Put(vector.Z);
    }
    public static void Put(this NetDataWriter writer, Color c) {
        writer.Put(c.R);
        writer.Put(c.G);
        writer.Put(c.B);
    }
    // idk why i wrote these, they're bugged
    public static void Put(this NetDataWriter writer, ITankHurtContext cxt) {
        //var sourceExists = cxt.Source is not null;
        //writer.Put(sourceExists);

        //writer.Put(sourceExists ? cxt.Source!.WorldId : -1);
        // evil asf
        if (cxt is TankHurtContextShell s) {
            writer.Put(true); 
            writer.Put(false);
            writer.Put(false);

            writer.Put(s.Shell.Id);
        }
        else if (cxt is TankHurtContextExplosion m) {
            writer.Put(false); 
            writer.Put(true); 
            writer.Put(false);
            writer.Put(m.Explosion.Id);
        }
        else if (cxt is TankHurtContextOther o) {
            writer.Put(false); 
            writer.Put(false); 
            writer.Put(true);
            writer.Put((byte)o.Context);
            writer.Put(o.Reason);
        }
    }
    public static Vector2 GetVector2(this NetPacketReader reader) {
        var x = reader.GetFloat();
        var y = reader.GetFloat();

        return new Vector2(x, y);
    }
    public static Vector3 GetVector3(this NetPacketReader reader) {
        var x = reader.GetFloat();
        var y = reader.GetFloat();
        var z = reader.GetFloat();

        return new Vector3(x, y, z);
    }
    public static Color GetColor(this NetPacketReader reader) {
        var r = reader.GetByte();
        var g = reader.GetByte();
        var b = reader.GetByte();

        return new Color(r, g, b);
    }
    public static ITankHurtContext GetTankHurtContext(this NetDataReader reader) {
        bool wasShell = reader.GetBool();
        bool wasMine = reader.GetBool();
        bool wasOther = reader.GetBool();

        if (wasShell) {
            // either reads the forced shell id or the organic shell id
            int shellId = reader.GetInt();
            // should report null instead of crashing
            var shell = Shell.AllShells[shellId];
            return new TankHurtContextShell(shell);
        }

        if (wasMine) {
            var mineId = reader.GetInt();
            var explosion = Explosion.Explosions[mineId];
            return new TankHurtContextExplosion(explosion);
        }

        if (wasOther) {
            var ctxByte = reader.GetByte();
            var cxtType = (TankHurtContextOther.HurtContext)ctxByte;
            var reason = reader.GetString();
            return new TankHurtContextOther(null, cxtType, reason);
        }

        // Fallback
        return new TankHurtContextOther(null, TankHurtContextOther.HurtContext.FromOther, string.Empty);
    }
}
