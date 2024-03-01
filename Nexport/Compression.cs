using ZstdSharp;

namespace Nexport;

public static class Compression
{
    public static byte[] Compress(byte[] b, int level)
    {
        using Compressor c = new Compressor(level);
        return c.Wrap(b).ToArray();
    }

    public static byte[] Decompress(byte[] b)
    {
        using Decompressor d = new Decompressor();
        return d.Unwrap(b).ToArray();
    }
}