using Microsoft.Xna.Framework;
using System.IO;

namespace TanksRebirth.Internals.Common.Utilities;

public static class BinaryUtils
{
    /// <summary>Writes a color to binary in RGB order.</summary>
    public static void Write(this BinaryWriter writer, Color color)
    {
        writer.Write(color.R);
        writer.Write(color.G);
        writer.Write(color.B);
    }
    /// <summary>Reads a color from a binary sequence.</summary>
    public static Color ReadColor(this BinaryReader reader) => new(reader.ReadByte(), reader.ReadByte(), reader.ReadByte());
}
