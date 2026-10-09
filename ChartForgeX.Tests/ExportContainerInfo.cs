using System.Text;
using ChartForgeX.Raster;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Reads exported PNG metadata and animation timing independently of renderer scheduling.</summary>
internal static class ExportContainerInfo {
    internal static IEnumerable<(string Name, byte[] Data)> PngChunks(byte[] bytes) {
        for (var offset = 8; offset < bytes.Length; offset += Big32(bytes, offset) + 12) {
            var length = Big32(bytes, offset);
            yield return (Encoding.ASCII.GetString(bytes, offset + 4, 4), bytes.AsSpan(offset + 8, length).ToArray());
        }
    }

    internal static (int[] DelaysCentiseconds, int PlayCount) Animation(byte[] bytes, RasterAnimationFormat format) {
        if (format == RasterAnimationFormat.Apng) {
            var chunks = PngChunks(bytes).ToArray();
            var delays = chunks.Where(chunk => chunk.Name == "fcTL").Select(chunk => {
                Assert.Equal(100, Big16(chunk.Data, 22));
                return Big16(chunk.Data, 20);
            }).ToArray();
            var animation = Assert.Single(chunks, chunk => chunk.Name == "acTL").Data;
            Assert.Equal(delays.Length, Big32(animation, 0));
            return (delays, Big32(animation, 4));
        }

        var gifDelays = new List<int>();
        var playCount = 1;
        var position = 13 + ((bytes[10] & 128) != 0 ? 3 * (1 << ((bytes[10] & 7) + 1)) : 0);
        while (bytes[position] != 0x3B) {
            var marker = bytes[position++];
            if (marker == 0x21) {
                var extension = bytes[position++];
                if (extension == 0xF9) gifDelays.Add(Little16(bytes, position + 2));
                if (extension == 0xFF && Encoding.ASCII.GetString(bytes, position + 1, bytes[position]) == "NETSCAPE2.0") {
                    playCount = Little16(bytes, position + 14) + 1;
                    if (Little16(bytes, position + 14) == 0) playCount = 0;
                }
            } else {
                Assert.Equal(0x2C, marker);
                var packed = bytes[position + 8];
                position += 9 + ((packed & 128) != 0 ? 3 * (1 << ((packed & 7) + 1)) : 0);
                position++;
            }
            while (bytes[position] != 0) position += bytes[position] + 1;
            position++;
        }
        return (gifDelays.ToArray(), playCount);
    }

    internal static int Big32(byte[] bytes, int offset) =>
        (bytes[offset] << 24) | (bytes[offset + 1] << 16) | (bytes[offset + 2] << 8) | bytes[offset + 3];
    private static int Big16(byte[] bytes, int offset) => (bytes[offset] << 8) | bytes[offset + 1];
    private static int Little16(byte[] bytes, int offset) => bytes[offset] | (bytes[offset + 1] << 8);
}
