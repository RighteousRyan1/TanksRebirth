using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TanksRebirth.Internals.Common.Utilities;

public static class BitUtils
{
    public static int GetInt(this byte[] buffer, int offset) {
        if (offset >= buffer.Length)
            return 0;

        if (!BitConverter.IsLittleEndian) return BitConverter.ToInt32(buffer, offset);
        
        Span<byte> _buffer = stackalloc byte[0x4]; // 4 bytes 

        for (var i = 0x0; i < 0x4; i++)
            _buffer[i ^ 0x3] = buffer[offset + i];
        
        return BitConverter.ToInt32(_buffer.ToArray(), 0x0);
    }
}
