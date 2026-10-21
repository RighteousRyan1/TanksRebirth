using System;
using System.IO;
using System.IO.Compression;
using System.Text.Json;

namespace TanksRebirth.IO;

// i should have done this sooner lol
/// <summary>Reads and writes JSON files compressed with gzip. Used by the save data and level files.</summary>
public static class GzipJson {
    /// <summary>Whether the stream starts with the gzip header (1F 8B). The stream position is left unchanged.</summary>
    public static bool IsGzip(Stream stream) {
        var start = stream.Position;
        Span<byte> header = stackalloc byte[2];
        var read = stream.ReadAtLeast(header, 2, throwOnEndOfStream: false);
        stream.Position = start;
        return read == 2 && header[0] == 0x1F && header[1] == 0x8B;
    }

    /// <summary>Reads JSON from <paramref name="stream"/>, decompressing it first if it's gzipped. The stream has to be seekable.</summary>
    public static T? Read<T>(Stream stream, JsonSerializerOptions options) {
        if (!IsGzip(stream))
            return JsonSerializer.Deserialize<T>(stream, options);

        using var gzip = new GZipStream(stream, CompressionMode.Decompress, leaveOpen: true);
        return JsonSerializer.Deserialize<T>(gzip, options);
    }

    /// <summary>Writes <paramref name="value"/> to <paramref name="path"/> as JSON, gzipped unless <paramref name="compress"/> is false.</summary>
    /// <remarks>The new file is written next to the old one and then moved over it, so a crash while saving doesn't eliminate the old file.</remarks>
    public static void Write<T>(string path, T value, JsonSerializerOptions options, bool compress = true) {
        var temp = path + ".tmp";
        using (var file = File.Create(temp)) {
            if (compress) {
                using var gzip = new GZipStream(file, CompressionLevel.Optimal);
                JsonSerializer.Serialize(gzip, value, options);
            }
            else
                JsonSerializer.Serialize(file, value, options);
        }
        File.Move(temp, path, overwrite: true);
    }
}
