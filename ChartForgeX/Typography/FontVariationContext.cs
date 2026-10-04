using System;
using System.Collections.Generic;

namespace ChartForgeX.Typography;

/// <summary>One face's immutable normalized axis coordinates, shared by outlines and layout.</summary>
internal sealed class FontVariationContext {
    internal FontVariationSettings Settings { get; }
    internal double[] Coordinates { get; }
    internal string[] Tags { get; }
    internal bool HasNonzeroCoordinates { get { foreach (var value in Coordinates) if (value != 0) return true; return false; } }
    private FontVariationContext(FontVariationSettings settings, double[] coordinates, string[] tags) { Settings = settings; Coordinates = coordinates; Tags = tags; }
    internal static FontVariationContext? Create(byte[] data, IReadOnlyDictionary<string, int> tables, IReadOnlyDictionary<string, int> lengths, FontVariationSettings settings) {
        if (!tables.TryGetValue("fvar", out var offset) || !lengths.TryGetValue("fvar", out var length)) return null;
        try {
            var table = new FontTableReader(data, offset, length);
            if (table.U16(0) != 1) return null;
            var count = table.U16(8); var stride = table.U16(10); var first = table.U16(4);
            if (count == 0 || count > 32 || stride < 20) return null;
            table.Require(first, count * stride);
            var coordinates = new double[count];
            var tags = new string[count];
            for (var i = 0; i < count; i++) {
                var at = first + i * stride; var minimum = table.Fixed(at + 4); var normal = table.Fixed(at + 8); var maximum = table.Fixed(at + 12);
                tags[i] = table.Tag(at);
                if (minimum > normal || normal > maximum) return null;
                var value = settings.TryGetValue(table.Tag(at), out var selected) ? Math.Max(minimum, Math.Min(maximum, selected)) : normal;
                coordinates[i] = value == normal ? 0 : value < normal ? (value - normal) / (normal - minimum) : (value - normal) / (maximum - normal);
            }
            if (tables.TryGetValue("avar", out var avar) && lengths.TryGetValue("avar", out var avarLength)) MapAxes(new FontTableReader(data, avar, avarLength), coordinates);
            // OpenType normalized coordinates have 14 fractional bits, including after axis mapping.
            for (var i = 0; i < count; i++) coordinates[i] = Math.Floor(coordinates[i] * 16384 + 0.5) / 16384;
            return new FontVariationContext(settings, coordinates, tags);
        } catch (FontLayoutException) { return null; }
    }
    private static void MapAxes(FontTableReader table, double[] coordinates) {
        if (table.U16(0) != 1 || table.U16(6) != coordinates.Length) return;
        var p = 8;
        for (var axis = 0; axis < coordinates.Length; axis++) {
            var count = table.U16(p); p += 2;
            if (count < 3 || count > 4096) throw new FontLayoutException();
            table.Require(p, count * 4);
            var previous = table.F2Dot14(p); var mapped = table.F2Dot14(p + 2); var result = coordinates[axis];
            for (var i = 1; i < count; i++) {
                var next = table.F2Dot14(p + i * 4); var target = table.F2Dot14(p + i * 4 + 2);
                if (next <= previous) throw new FontLayoutException();
                if (coordinates[axis] >= previous && coordinates[axis] <= next) result = mapped + (target - mapped) * (coordinates[axis] - previous) / (next - previous);
                previous = next; mapped = target;
            }
            coordinates[axis] = Math.Max(-1, Math.Min(1, result)); p += count * 4;
        }
    }
    internal static double AxisScalar(double coordinate, double start, double peak, double end) {
        if (peak == 0 || start > peak || peak > end || start < 0 && end > 0) return 1;
        if (coordinate == peak) return 1;
        if (coordinate <= start || coordinate >= end) return 0;
        return coordinate < peak ? (coordinate - start) / (peak - start) : (end - coordinate) / (end - peak);
    }
}
