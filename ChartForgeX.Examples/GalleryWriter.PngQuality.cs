/// <summary>
/// PNG-specific artifact quality helpers for generated examples.
/// </summary>
public static partial class GalleryWriter {
    private const int PngEdgeCornerSampleSize = 8;
    private const int PngEdgeInkTolerance = 18;

    private static int PngEdgeBandSize(AssetDimensions dimensions) =>
        Math.Max(2, (int)Math.Round(Math.Min(dimensions.Width, dimensions.Height) * 0.01));

    private static int PngColorKey(byte r, byte g, byte b, byte a) => unchecked((r << 24) | (g << 16) | (b << 8) | a);

    private static void TrackPngEdgeSamples(AssetDimensions dimensions, int x, int y, int edgeBand, int key, Dictionary<int, int> cornerColors, List<int> edgeColors) {
        if (x < PngEdgeCornerSampleSize || x >= dimensions.Width - PngEdgeCornerSampleSize) {
            if (y < PngEdgeCornerSampleSize || y >= dimensions.Height - PngEdgeCornerSampleSize) {
                cornerColors.TryGetValue(key, out var count);
                cornerColors[key] = count + 1;
            }
        }

        if (x < edgeBand || y < edgeBand || x >= dimensions.Width - edgeBand || y >= dimensions.Height - edgeBand) edgeColors.Add(key);
    }

    private static int DominantPngCornerColor(Dictionary<int, int> cornerColors) {
        var bestKey = 0;
        var bestCount = -1;
        foreach (var item in cornerColors) {
            if (item.Value <= bestCount) continue;
            bestKey = item.Key;
            bestCount = item.Value;
        }

        return bestKey;
    }

    private static int DominantPngVisibleColor(int[] pixels, int fallback) {
        return DominantPngVisibleColor((IEnumerable<int>)pixels, fallback);
    }

    private static int DominantPngVisibleColor(IEnumerable<int> pixels, int fallback) {
        var colors = new Dictionary<int, int>();
        foreach (var color in pixels) {
            if ((color & 0xff) == 0) continue;
            if (colors.Count >= 4096 && !colors.ContainsKey(color)) continue;
            colors.TryGetValue(color, out var count);
            colors[color] = count + 1;
        }

        return colors.Count == 0 ? fallback : DominantPngCornerColor(colors);
    }

    private static long CountPngEdgeInk(List<int> edgeColors, int background) {
        var count = 0L;
        var transparentCorner = (background & 0xff) == 0;
        if (transparentCorner) background = DominantPngVisibleColor(edgeColors, background);
        foreach (var color in edgeColors) {
            if ((color & 0xff) == 0) continue;
            if (transparentCorner && (color & 0xff) < 255) continue;
            if (PngColorDistance(color, background) > PngEdgeInkTolerance) count++;
        }

        return count;
    }

    private static long CountPngForeground(int[] pixels, AssetDimensions dimensions, int background, out PngContentBounds bounds) {
        var count = 0L;
        var left = dimensions.Width;
        var top = dimensions.Height;
        var right = -1;
        var bottom = -1;
        for (var y = 0; y < dimensions.Height; y++) {
            for (var x = 0; x < dimensions.Width; x++) {
                var color = pixels[y * dimensions.Width + x];
                if ((color & 0xff) == 0 || PngColorDistance(color, background) <= PngEdgeInkTolerance) continue;
                count++;
                if (x < left) left = x;
                if (x > right) right = x;
                if (y < top) top = y;
                if (y > bottom) bottom = y;
            }
        }

        bounds = new PngContentBounds(left, top, right, bottom);
        return count;
    }

    private static string FormatPngContentBounds(PngContentBounds bounds) =>
        bounds.IsEmpty ? "empty" : bounds.Left.ToString(System.Globalization.CultureInfo.InvariantCulture) + "," + bounds.Top.ToString(System.Globalization.CultureInfo.InvariantCulture) + " " + bounds.Width.ToString(System.Globalization.CultureInfo.InvariantCulture) + "x" + bounds.Height.ToString(System.Globalization.CultureInfo.InvariantCulture);

    private static int PngColorDistance(int left, int right) {
        var distance = Math.Abs(((left >> 24) & 0xff) - ((right >> 24) & 0xff));
        distance = Math.Max(distance, Math.Abs(((left >> 16) & 0xff) - ((right >> 16) & 0xff)));
        distance = Math.Max(distance, Math.Abs(((left >> 8) & 0xff) - ((right >> 8) & 0xff)));
        return Math.Max(distance, Math.Abs((left & 0xff) - (right & 0xff)));
    }
}
