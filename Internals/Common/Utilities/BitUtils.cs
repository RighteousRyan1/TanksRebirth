using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TanksRebirth.Internals.Common.Utilities;

public static class BitUtils {
    /// <summary>
    /// Reads 4 bytes as an integer, with an offset provided.
    /// </summary>
    /// <param name="buffer">The buffer of data that contains convertible data.</param>
    /// <param name="offset">The location in <paramref name="buffer"/> of the data to parse to <see cref="int"/>.</param>
    /// <returns>The converted <see cref="int"/>.</returns>
    public static int GetInt(this byte[] buffer, int offset) {
        if (offset >= buffer.Length)
            return 0;

        if (!BitConverter.IsLittleEndian) return BitConverter.ToInt32(buffer, offset);

        Span<byte> _buffer = stackalloc byte[0x4]; // 4 bytes 

        // reverse for big endian order. realistically this code should never run cuz computers know better
        for (var i = 0x0; i < 0x4; i++)
            _buffer[i ^ 0x3] = buffer[offset + i];

        return BitConverter.ToInt32(_buffer);
    }

    // googled a quick and efficient checksum lol
    /// <summary>
    /// Performs a checksum using the FNV-1a checksum algorithm.
    /// </summary>
    /// <param name="data">The bytes to perform the checksum on.</param>
    /// <returns>The checksum.</returns>
    public static uint ChecksumFNV(ReadOnlySpan<byte> data) {
        uint hash = 2166136261;

        foreach (byte b in data) {
            hash ^= b;
            hash *= 16777619;
        }

        return hash;
    }

    /// <summary>
    /// Performs a checksum using XOR on each byte.
    /// </summary>
    /// <param name="data">The bytes to perform the checksum on.</param>
    /// <returns>The checksum.</returns>
    public static byte ChecksumXOR(ReadOnlySpan<byte> data) {
        byte checksum = 0;

        foreach (byte b in data)
            checksum ^= b;

        return checksum;
    }
}