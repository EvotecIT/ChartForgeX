using System;
using System.Collections.Generic;
using System.Threading;

namespace ChartForgeX.Raster;

internal sealed class GifPalette {
    public GifPalette(byte[] colors, int transparentIndex = -1, bool exact = false) {
        if (colors == null) throw new ArgumentNullException(nameof(colors));
        if (colors.Length != 768) throw new ArgumentException("GIF palettes must contain 256 RGB colors.", nameof(colors));
        if (transparentIndex < -1 || transparentIndex >= 256) throw new ArgumentOutOfRangeException(nameof(transparentIndex), transparentIndex, "GIF transparent palette index must fit the palette.");
        IsExact = exact;
        Colors = colors;
        TransparentIndex = transparentIndex;
    }

    public bool IsExact { get; }
    public byte[] Colors { get; }
    public int TransparentIndex { get; }
    public bool HasTransparency => TransparentIndex >= 0;
}

internal static class GifPaletteQuantizer {
    private const int PaletteSize = 256;
    private const int HistogramSize = 65536;

    public static GifPalette BuildPalette(IEnumerable<RgbaImage> frames, CancellationToken cancellationToken = default) {
        var histogram = BuildHistogram(frames, cancellationToken, out var transparent, out var actualColors);
        var exact = ExactPalette(actualColors, transparent);
        if (exact != null) return exact;
        var transparentIndex = transparent ? PaletteSize - 1 : -1;
        var colorSlots = transparentIndex >= 0 ? PaletteSize - 1 : PaletteSize;
        var samples = new List<ColorSample>();
        for (var key = 0; key < histogram.Counts.Length; key++) {
            var count = histogram.Counts[key];
            if (count == 0) continue;
            samples.Add(new ColorSample(histogram.Red[key] / count, histogram.Green[key] / count, histogram.Blue[key] / count, count));
        }

        if (samples.Count == 0) samples.Add(new ColorSample(0, 0, 0, 1));
        // Preserve frequent source colours (including flat fills and glyph ink) before allocating gradient shades.
        var frequent = new List<KeyValuePair<int, long>>(actualColors);
        frequent.Sort((left, right) => { var order = right.Value.CompareTo(left.Value); return order != 0 ? order : left.Key.CompareTo(right.Key); });
        var reserved = Math.Min(64, frequent.Count);
        var boxes = new List<ColorBox> { ColorBox.Create(samples, 0, samples.Count) };
        while (boxes.Count < colorSlots - reserved) {
            cancellationToken.ThrowIfCancellationRequested();
            var index = LargestSplittableBox(boxes);
            if (index < 0) break;
            var split = boxes[index].Split(samples);
            boxes[index] = split.First;
            boxes.Add(split.Second);
        }

        var colors = new byte[PaletteSize * 3];
        for (var i = 0; i < reserved; i++) WriteColor(colors, i, frequent[i].Key);
        for (var i = 0; i < boxes.Count && i + reserved < colorSlots; i++) WriteAverageColor(colors, i + reserved, samples, boxes[i]);
        var fallback = Math.Max(0, boxes.Count + reserved - 1);
        for (var i = boxes.Count + reserved; i < colorSlots; i++) {
            colors[i * 3] = colors[fallback * 3];
            colors[i * 3 + 1] = colors[fallback * 3 + 1];
            colors[i * 3 + 2] = colors[fallback * 3 + 2];
        }

        if (transparentIndex >= 0) {
            colors[transparentIndex * 3] = 0;
            colors[transparentIndex * 3 + 1] = 0;
            colors[transparentIndex * 3 + 2] = 0;
        }

        return new GifPalette(colors, transparentIndex);
    }

    public static byte[] Quantize(RgbaImage frame, GifPalette palette, CancellationToken cancellationToken = default) {
        var indexed = new byte[frame.Width * frame.Height];
        var exact = new Dictionary<int, byte>();
        var slots = palette.HasTransparency ? palette.TransparentIndex : PaletteSize;
        for (var i = 0; i < slots; i++) {
            var offset = i * 3;
            var key = (palette.Colors[offset] << 16) | (palette.Colors[offset + 1] << 8) | palette.Colors[offset + 2];
            if (!exact.ContainsKey(key)) exact.Add(key, (byte)i);
        }
        for (var i = 0; i < indexed.Length; i++) {
            if ((i & 4095) == 0) cancellationToken.ThrowIfCancellationRequested();
            var source = i * 4;
            if (palette.HasTransparency && IsTransparent(frame.Pixels[source + 3])) {
                indexed[i] = (byte)palette.TransparentIndex;
                continue;
            }
            var r = frame.Pixels[source]; var g = frame.Pixels[source + 1]; var b = frame.Pixels[source + 2];
            // Never diffuse error across glyph edges. Exact colours bypass the approximate lookup entirely.
            var key = (r << 16) | (g << 8) | b;
            if (!exact.TryGetValue(key, out var match)) {
                match = (byte)NearestColorIndex(palette, r, g, b);
                if (exact.Count < 65536) exact.Add(key, match);
            }
            indexed[i] = match;
        }
        return indexed;
    }

    internal static GifPalette? TryExactPalette(RgbaImage image, CancellationToken token, bool reserveTransparency = false) {
        var colors = new Dictionary<int, long>();
        var transparent = reserveTransparency;
        var canvasBytes = checked(image.Width * image.Height * 4);
        for (var i = 0; i < canvasBytes; i += 4) {
            if ((i & 16383) == 0) token.ThrowIfCancellationRequested();
            if (IsTransparent(image.Pixels[i + 3])) { transparent = true; }
            else {
                var key = (image.Pixels[i] << 16) | (image.Pixels[i + 1] << 8) | image.Pixels[i + 2];
                colors[key] = 1;
            }
            if (colors.Count > (transparent ? 255 : 256)) return null;
        }
        return ExactPalette(colors, transparent);
    }

    private static GifPalette? ExactPalette(Dictionary<int, long> actualColors, bool transparent) {
        if (actualColors.Count > (transparent ? 255 : 256)) return null;
        var keys = new List<int>(actualColors.Keys);
        keys.Sort();
        var colors = new byte[768];
        for (var i = 0; i < keys.Count; i++) WriteColor(colors, i, keys[i]);
        return new GifPalette(colors, transparent ? 255 : -1, exact: true);
    }

    private static void WriteColor(byte[] colors, int index, int color) {
        colors[index * 3] = (byte)(color >> 16);
        colors[index * 3 + 1] = (byte)(color >> 8);
        colors[index * 3 + 2] = (byte)color;
    }

    private static GifHistogram BuildHistogram(IEnumerable<RgbaImage> frames, CancellationToken cancellationToken, out bool transparent, out Dictionary<int, long> actualColors) {
        var histogram = new GifHistogram();
        transparent = false;
        actualColors = new Dictionary<int, long>();
        foreach (var frame in frames) {
            var pixels = frame.Pixels;
            for (var i = 0; i < frame.Width * frame.Height; i++) {
                if ((i & 4095) == 0) cancellationToken.ThrowIfCancellationRequested();
                var source = i * 4;
                if (IsTransparent(pixels[source + 3])) { transparent = true; continue; }
                var r = pixels[source];
                var g = pixels[source + 1];
                var b = pixels[source + 2];
                var actual = (r << 16) | (g << 8) | b;
                if (actualColors.TryGetValue(actual, out var count)) actualColors[actual] = count + 1;
                else if (actualColors.Count < 65536) actualColors.Add(actual, 1);
                var key = ((r & 0xF8) << 8) | ((g & 0xFC) << 3) | (b >> 3);
                histogram.Counts[key]++;
                histogram.Red[key] += r;
                histogram.Green[key] += g;
                histogram.Blue[key] += b;
            }
        }

        return histogram;
    }

    private static int LargestSplittableBox(IReadOnlyList<ColorBox> boxes) {
        var index = -1;
        var score = -1.0;
        for (var i = 0; i < boxes.Count; i++) {
            var box = boxes[i];
            if (box.Length < 2 || box.Count < 2) continue;
            var candidate = box.Count * Math.Max(box.RedRange, Math.Max(box.GreenRange, box.BlueRange));
            if (candidate <= score) continue;
            score = candidate;
            index = i;
        }

        return index;
    }

    private static void WriteAverageColor(byte[] colors, int paletteIndex, IReadOnlyList<ColorSample> samples, ColorBox box) {
        long red = 0;
        long green = 0;
        long blue = 0;
        long count = 0;
        for (var i = box.Start; i < box.End; i++) {
            var sample = samples[i];
            red += sample.Red * sample.Count;
            green += sample.Green * sample.Count;
            blue += sample.Blue * sample.Count;
            count += sample.Count;
        }

        if (count == 0) count = 1;
        var offset = paletteIndex * 3;
        colors[offset] = (byte)Math.Max(0, Math.Min(255, red / count));
        colors[offset + 1] = (byte)Math.Max(0, Math.Min(255, green / count));
        colors[offset + 2] = (byte)Math.Max(0, Math.Min(255, blue / count));
    }

    private static int NearestColorIndex(GifPalette palette, double red, double green, double blue) {
        var best = 0;
        var bestDistance = double.MaxValue;
        var colorSlots = palette.HasTransparency ? palette.TransparentIndex : PaletteSize;
        for (var i = 0; i < colorSlots; i++) {
            var offset = i * 3;
            var dr = red - palette.Colors[offset];
            var dg = green - palette.Colors[offset + 1];
            var db = blue - palette.Colors[offset + 2];
            var distance = dr * dr + dg * dg + db * db;
            if (distance >= bestDistance) continue;
            bestDistance = distance;
            best = i;
        }

        return best;
    }

    private static bool IsTransparent(byte alpha) => alpha < 128;

    private sealed class GifHistogram {
        public readonly long[] Counts = new long[HistogramSize];
        public readonly long[] Red = new long[HistogramSize];
        public readonly long[] Green = new long[HistogramSize];
        public readonly long[] Blue = new long[HistogramSize];
    }

    private readonly struct ColorSample {
        public ColorSample(long red, long green, long blue, long count) {
            Red = red;
            Green = green;
            Blue = blue;
            Count = count;
        }

        public readonly long Red;
        public readonly long Green;
        public readonly long Blue;
        public readonly long Count;
    }

    private readonly struct ColorBox {
        private ColorBox(int start, int end, long count, long minimumRed, long maximumRed, long minimumGreen, long maximumGreen, long minimumBlue, long maximumBlue) {
            Start = start;
            End = end;
            Count = count;
            MinimumRed = minimumRed;
            MaximumRed = maximumRed;
            MinimumGreen = minimumGreen;
            MaximumGreen = maximumGreen;
            MinimumBlue = minimumBlue;
            MaximumBlue = maximumBlue;
        }

        public readonly int Start;
        public readonly int End;
        public readonly long Count;
        public readonly long MinimumRed;
        public readonly long MaximumRed;
        public readonly long MinimumGreen;
        public readonly long MaximumGreen;
        public readonly long MinimumBlue;
        public readonly long MaximumBlue;
        public int Length => End - Start;
        public long RedRange => MaximumRed - MinimumRed;
        public long GreenRange => MaximumGreen - MinimumGreen;
        public long BlueRange => MaximumBlue - MinimumBlue;

        public static ColorBox Create(IReadOnlyList<ColorSample> samples, int start, int end) {
            long count = 0;
            long minimumRed = 255;
            long maximumRed = 0;
            long minimumGreen = 255;
            long maximumGreen = 0;
            long minimumBlue = 255;
            long maximumBlue = 0;
            for (var i = start; i < end; i++) {
                var sample = samples[i];
                count += sample.Count;
                minimumRed = Math.Min(minimumRed, sample.Red);
                maximumRed = Math.Max(maximumRed, sample.Red);
                minimumGreen = Math.Min(minimumGreen, sample.Green);
                maximumGreen = Math.Max(maximumGreen, sample.Green);
                minimumBlue = Math.Min(minimumBlue, sample.Blue);
                maximumBlue = Math.Max(maximumBlue, sample.Blue);
            }

            return new ColorBox(start, end, count, minimumRed, maximumRed, minimumGreen, maximumGreen, minimumBlue, maximumBlue);
        }

        public ColorBoxSplit Split(List<ColorSample> samples) {
            Comparison<ColorSample> comparison;
            if (RedRange >= GreenRange && RedRange >= BlueRange) comparison = (left, right) => CompareSamples(left, right, left.Red.CompareTo(right.Red));
            else if (GreenRange >= BlueRange) comparison = (left, right) => CompareSamples(left, right, left.Green.CompareTo(right.Green));
            else comparison = (left, right) => CompareSamples(left, right, left.Blue.CompareTo(right.Blue));
            samples.Sort(Start, Length, Comparer<ColorSample>.Create(comparison));
            var midpoint = WeightedMidpoint(samples);
            return new ColorBoxSplit(Create(samples, Start, midpoint), Create(samples, midpoint, End));
        }

        /// <summary>Orders dominant-channel ties by color and weight so median cuts are independent of the host's sort algorithm.</summary>
        private static int CompareSamples(ColorSample left, ColorSample right, int dominantOrder) {
            if (dominantOrder != 0) {
                return dominantOrder;
            }

            var order = left.Red.CompareTo(right.Red);
            if (order != 0) {
                return order;
            }
            order = left.Green.CompareTo(right.Green);
            if (order != 0) {
                return order;
            }
            order = left.Blue.CompareTo(right.Blue);
            return order != 0 ? order : left.Count.CompareTo(right.Count);
        }

        private int WeightedMidpoint(IReadOnlyList<ColorSample> samples) {
            var half = Math.Max(1, Count / 2);
            long walked = 0;
            for (var i = Start; i < End - 1; i++) {
                walked += samples[i].Count;
                if (walked >= half) return i + 1;
            }

            return Start + Math.Max(1, Length / 2);
        }
    }

    private readonly struct ColorBoxSplit {
        public ColorBoxSplit(ColorBox first, ColorBox second) {
            First = first;
            Second = second;
        }

        public readonly ColorBox First;
        public readonly ColorBox Second;
    }
}
