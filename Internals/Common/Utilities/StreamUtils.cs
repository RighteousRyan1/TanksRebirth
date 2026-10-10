using System.IO;

namespace TanksRebirth.Internals.Common.Utilities; 
public static class StreamUtils {
    /// <summary>Resets a stream and makes it seekable.</summary>
    public static Stream MakeSeekable(Stream stream) {
        if (stream.CanSeek)
            return stream;
        var copy = new MemoryStream();
        stream.CopyTo(copy);
        copy.Position = 0;
        return copy;
    }
}
