using LiteNetLib;
using LiteNetLib.Utils;
using Microsoft.Xna.Framework;

namespace TanksRebirth.Net
{
    public static class NetExtensions
    {
        public static void Put(this NetDataWriter writer, Vector2 v)
        {
            writer.Put(v.X);
            writer.Put(v.Y);
        }
        public static void Put(this NetDataWriter writer, Vector3 vector)
        {
            writer.Put(vector.X);
            writer.Put(vector.Y);
            writer.Put(vector.Z);
        }
        public static void Put(this NetDataWriter writer, Color c)
        {
            writer.Put(c.R);
            writer.Put(c.G);
            writer.Put(c.B);
        }
        public static Vector2 GetVector2(this NetPacketReader reader)
        {
            var x = reader.GetFloat();
            var y = reader.GetFloat();

            return new Vector2(x, y);
        }
        public static Vector3 GetVector3(this NetPacketReader reader)
        {
            var x = reader.GetFloat();
            var y = reader.GetFloat();
            var z = reader.GetFloat();

            return new Vector3(x, y, z);
        }
        public static Color GetColor(this NetPacketReader reader)
        {
            var r = reader.GetByte();
            var g = reader.GetByte();
            var b = reader.GetByte();

            return new Color(r, g, b);
        }
    }
}
