using System;
using System.Collections.Generic;

namespace ChartForgeX.Raster;

internal sealed class GifPalette {
    public GifPalette(byte[] colors, int transparentIndex = -1) {
        if (colors == null) throw new ArgumentNullException(nameof(colors));
        if (colors.Length != 768) throw new ArgumentException("GIF palettes must contain 256 RGB colors.", nameof(colors));
        if (transparentIndex < -1 || transparentIndex >= 256) throw new ArgumentOutOfRangeException(nameof(transparentIndex), transparentIndex, "GIF transparent palette index must fit the palette.");
        Colors = colors;
        TransparentIndex = transparentIndex;
    }

    public byte[] Colors { get; }
    public int TransparentIndex { get; }
    public bool HasTransparency => TransparentIndex >= 0;
}

internal static class GifPaletteQuantizer {
    private const int PaletteSize = 256;
    private const int HistogramSize = 65536;

    public static GifPalette BuildPalette(IReadOnlyList<RgbaImage> frames) {
        var transparentIndex = HasTransparentPixels(frames) ? PaletteSize - 1 : -1;
        var colorSlots = transparentIndex >= 0 ? PaletteSize - 1 : PaletteSize;
        var histogram = BuildHistogram(frames);
        var samples = new List<ColorSample>();
        for (var key = 0; key < histogram.Counts.Length; key++) {
            var count = histogram.Counts[key];
            if (count == 0) continue;
            samples.Add(new ColorSample(histogram.Red[key] / count, histogram.Green[key] / count, histogram.Blue[key] / count, count));
        }

        if (samples.Count == 0) samples.Add(new ColorSample(0, 0, 0, 1));
        var boxes = new List<ColorBox> { ColorBox.Create(samples, 0, samples.Count) };
        while (boxes.Count < colorSlots) {
            var index = LargestSplittableBox(boxes);
            if (index < 0) break;
            var split = boxes[index].Split(samples);
            boxes[index] = split.First;
            boxes.Add(split.Second);
        }

        var colors = new byte[PaletteSize * 3];
        for (var i = 0; i < boxes.Count && i < colorSlots; i++) WriteAverageColor(colors, i, samples, boxes[i]);
        var fallback = Math.Max(0, boxes.Count - 1);
        for (var i = boxes.Count; i < colorSlots; i++) {
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

    public static byte[] Quantize(RgbaImage frame, GifPalette palette) {
        var indexed = new byte[frame.Width * frame.Height];
        var cache = new int[HistogramSize];
        for (var i = 0; i < cache.Length; i++) cache[i] = -1;
        var currentRed = new double[frame.Width + 2];
        var currentGreen = new double[frame.Width + 2];
        var currentBlue = new double[frame.Width + 2];
        var nextRed = new double[frame.Width + 2];
        var nextGreen = new double[frame.Width + 2];
        var nextBlue = new double[frame.Width + 2];

        for (var y = 0; y < frame.Height; y++) {
            var row = y * frame.Width;
            for (var x = 0; x < frame.Width; x++) {
                var source = (row + x) * 4;
                if (palette.HasTransparency && IsTransparent(frame.Pixels[source + 3])) {
                    indexed[row + x] = (byte)palette.TransparentIndex;
                    continue;
                }

                var r = ClampToByte(frame.Pixels[source] + currentRed[x + 1]);
                var g = ClampToByte(frame.Pixels[source + 1] + currentGreen[x + 1]);
                var b = ClampToByte(frame.Pixels[source + 2] + currentBlue[x + 1]);
                var index = NearestColorIndex(palette, r, g, b, cache);
                indexed[row + x] = (byte)index;
                var offset = index * 3;
                var er = r - palette.Colors[offset];
                var eg = g - palette.Colors[offset + 1];
                var eb = b - palette.Colors[offset + 2];
                Diffuse(currentRed, nextRed, x, er);
                Diffuse(currentGreen, nextGreen, x, eg);
                Diffuse(currentBlue, nextBlue, x, eb);
            }

            Swap(ref currentRed, ref nextRed);
            Swap(ref currentGreen, ref nextGreen);
            Swap(ref currentBlue, ref nextBlue);
            Array.Clear(nextRed, 0, nextRed.Length);
            Array.Clear(nextGreen, 0, nextGreen.Length);
            Array.Clear(nextBlue, 0, nextBlue.Length);
        }

        return indexed;
    }

    private static GifHistogram BuildHistogram(IReadOnlyList<RgbaImage> frames) {
        var histogram = new GifHistogram();
        foreach (var frame in frames) {
            var pixels = frame.Pixels;
            for (var i = 0; i < frame.Width * frame.Height; i++) {
                var source = i * 4;
                if (IsTransparent(pixels[source + 3])) continue;
                var r = pixels[source];
                var g = pixels[source + 1];
                var b = pixels[source + 2];
                var key = ((r & 0xF8) << 8) | ((g & 0xFC) << 3) | (b >> 3);
                histogram.Counts[key]++;
                histogram.Red[key] += r;
                histogram.Green[key] += g;
                histogram.Blue[key] += b;
            }
        }

        return histogram;
    }

    private static bool HasTransparentPixels(IReadOnlyList<RgbaImage> frames) {
        foreach (var frame in frames) {
            var pixels = frame.Pixels;
            for (var i = 0; i < frame.Width * frame.Height; i++) {
                if (IsTransparent(pixels[i * 4 + 3])) return true;
            }
        }

        return false;
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

    private static int NearestColorIndex(GifPalette palette, double red, double green, double blue, int[] cache) {
        var key = (((int)red & 0xF8) << 8) | (((int)green & 0xFC) << 3) | ((int)blue >> 3);
        var cached = cache[key];
        if (cached >= 0) return cached;
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

        cache[key] = best;
        return best;
    }

    private static void Diffuse(double[] current, double[] next, int x, double error) {
        current[x + 2] += error * 7.0 / 16.0;
        next[x] += error * 3.0 / 16.0;
        next[x + 1] += error * 5.0 / 16.0;
        next[x + 2] += error / 16.0;
    }

    private static double ClampToByte(double value) =>
        value < 0 ? 0 : value > 255 ? 255 : value;

    private static bool IsTransparent(byte alpha) => alpha < 128;

    private static void Swap(ref double[] first, ref double[] second) {
        var temp = first;
        first = second;
        second = temp;
    }

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
            if (RedRange >= GreenRange && RedRange >= BlueRange) comparison = (left, right) => left.Red.CompareTo(right.Red);
            else if (GreenRange >= BlueRange) comparison = (left, right) => left.Green.CompareTo(right.Green);
            else comparison = (left, right) => left.Blue.CompareTo(right.Blue);
            samples.Sort(Start, Length, Comparer<ColorSample>.Create(comparison));
            var midpoint = WeightedMidpoint(samples);
            return new ColorBoxSplit(Create(samples, Start, midpoint), Create(samples, midpoint, End));
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
