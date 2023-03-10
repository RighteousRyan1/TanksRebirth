using LiteNetLib;
using LiteNetLib.Utils;
using Microsoft.Xna.Framework;

namespace TanksRebirth.Net
{
    public static class NetExtensions {
        /// <summary>
        /// Puts a <see cref="Vector2"/> into the current <see cref="NetDataWriter"/>.
        /// </summary>
        /// <param name="writer">The writer instance.</param>
        /// <param name="vector">The vector to write to the instance</param>
        /// <remarks>
        ///     This sends 2 float (or Single), which are sized 4 bytes each.
        /// </remarks>
        public static void Put(this NetDataWriter writer, Vector2 vector) {
            writer.Put(vector.X);
            writer.Put(vector.Y);
        }
        /// <summary>
        /// Puts a <see cref="Vector3"/> into the current <see cref="NetDataWriter"/>.
        /// </summary>
        /// <param name="writer">The writer instance.</param>
        /// <param name="vector">The vector to write to the instance</param>
        /// <remarks>
        ///     This sends 3 float (or Single), which are sized 4 bytes each.
        /// </remarks>
        public static void Put(this NetDataWriter writer, Vector3 vector) {
            writer.Put(vector.X);
            writer.Put(vector.Y);
            writer.Put(vector.Z);
        }
        /// <summary>
        /// Puts a <see cref="Color"/> into the current <see cref="NetDataWriter"/>.
        /// </summary>
        /// <param name="writer">The writer instance.</param>
        /// <param name="color">The color to write to the instance</param>
        /// <remarks>
        ///     This sends 3 bytes.
        /// </remarks>
        public static void Put(this NetDataWriter writer, Color color) {
            writer.Put(color.R);
            writer.Put(color.G);
            writer.Put(color.B);
        }
        /// <summary>
        ///     Reads a <see cref="Vector2"/> from the current <see cref="NetDataReader"/>.
        /// </summary>
        /// <param name="writer">The writer instance.</param>
        /// <remarks>
        ///     This reads a total of 2 float (or Single), which are sized 4 bytes each.
        /// </remarks>
        /// <returns>
        ///     The read <see cref="Vector2"/>.
        /// </returns>
        public static Vector2 GetVector2(this NetPacketReader reader) {
            var x = reader.GetFloat();
            var y = reader.GetFloat();

            return new Vector2(x, y);
        }
        /// <summary>
        /// Reads a <see cref="Vector3"/> from the current <see cref="NetDataReader"/>.
        /// </summary>
        /// <param name="writer">The writer instance.</param>
        /// <remarks>
        ///     This reads a total of 3 float (or Single), which are sized 4 bytes each.
        /// </remarks>
        /// <returns>
        ///     The read <see cref="Vector3"/>.
        /// </returns>
        public static Vector3 GetVector3(this NetPacketReader reader) {
            var x = reader.GetFloat();
            var y = reader.GetFloat();
            var z = reader.GetFloat();

            return new Vector3(x, y, z);
        }
        /// <summary>
        /// Reads a <see cref="Color"/> from the current <see cref="NetDataReader"/>.
        /// </summary>
        /// <param name="writer">The writer instance.</param>
        /// <remarks>
        /// This reads a total of 3 bytes.
        /// </remarks>
        /// <returns>
        /// The read <see cref="Color"/>.
        /// </returns>
        public static Color GetColor(this NetPacketReader reader) {
            var r = reader.GetByte();
            var g = reader.GetByte();
            var b = reader.GetByte();

            return new Color(r, g, b);
        }
    }
}
