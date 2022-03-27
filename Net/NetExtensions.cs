using LiteNetLib;
using LiteNetLib.Utils;
using Microsoft.Xna.Framework;

namespace WiiPlayTanksRemake.Net
{
    public static class NetExtensions
    {
        public static PacketType GetPacket(this NetPacketReader reader) 
            => (PacketType)reader.GetByte();
        public static void Put(this NetDataWriter writer, PacketType packet)
            => writer.Put((byte)packet);
        public static void Put(this NetDataWriter writer, Vector2 v)
        {
            writer.Put(v.X);
            writer.Put(v.Y);
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
        public static Color GetColor(this NetPacketReader reader)
        {
            var r = reader.GetByte();
            var g = reader.GetByte();
            var b = reader.GetByte();

            return new Color(r, g, b);
        }
    }
}
