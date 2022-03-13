using LiteNetLib;
using LiteNetLib.Utils;

namespace WiiPlayTanksRemake.Net
{
    public static class NetExtensions
    {
        public static PacketType GetPacket(this NetPacketReader reader) 
            => (PacketType)reader.GetByte();
        public static void Put(this NetDataWriter writer, PacketType packet)
            => writer.Put((byte)packet);
    }
}
