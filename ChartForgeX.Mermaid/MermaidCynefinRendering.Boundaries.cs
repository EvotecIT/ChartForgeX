using System;
using ChartForgeX.Primitives;
using ChartForgeX.Svg;
using ChartForgeX.Topology;

namespace ChartForgeX.Mermaid;

// Boundary equations and deterministic seed arithmetic adapted from Mermaid 12.2.0.
// Copyright (c) 2014 - 2022 Knut Sveidqvist; MIT. See Cynefin-Mermaid-LICENSE.txt.
public static partial class MermaidCynefinRendering {
    /// <summary>Projects the pinned renderer's numeric boundaries into the existing captured native path owner.</summary>
    private static void AddBoundaries(TopologyChart chart, MermaidCynefinSettings? settings, double x, double y, double width, double height) {
        var colors = chart.Theme.Resolve(chart.ThemeMode);
        var ink = settings?.Color("boundaryColor", colors.Border) ?? colors.Border;
        var cliff = settings?.Color("cliffColor", chart.ThemeMode == ChartForgeX.Themes.VisualThemeMode.Dark
            ? ChartColor.FromHex("#FF6B6B") : ChartColor.FromHex("#8B0000"))
            ?? (chart.ThemeMode == ChartForgeX.Themes.VisualThemeMode.Dark ? ChartColor.FromHex("#FF6B6B") : ChartColor.FromHex("#8B0000"));
        var amplitude = settings?.Number("boundaryAmplitude", 8) ?? 8;
        var seed = settings?.Number("seed", 0) ?? 0;
        if (seed == 0) seed = Hash(chart.Id ?? "mermaid-cynefin");
        chart.PathDecorations.Add(new TopologyPathDecoration("cynefin-fold", Boundary(true, seed), stroke: ink,
            strokeWidth: settings?.Number("boundaryWidth", 2) ?? 2, dashPattern: new[] { 6d, 3d }));
        chart.PathDecorations.Add(new TopologyPathDecoration("cynefin-horizontal", Boundary(false, seed + 100), stroke: ink,
            strokeWidth: settings?.Number("boundaryWidth", 2) ?? 2, dashPattern: new[] { 6d, 3d }));
        var cx = x + width / 2; var top = y + height / 2; var span = height / 2; var a = width * .03;
        var path = new SvgPathData().Add(new SvgPathCommand('M', new[] { cx, top }))
            .Add(new SvgPathCommand('C', new[] { cx + a, top + span * .2, cx - a * 1.5, top + span * .55, cx + a * .5, top + span * .75 }))
            .Add(new SvgPathCommand('C', new[] { cx - a, top + span * .85, cx + a * .3, top + span * .95, cx, y + height }));
        chart.PathDecorations.Add(new TopologyPathDecoration("cynefin-cliff", path, stroke: cliff, strokeWidth: settings?.Number("cliffWidth", 4) ?? 4));

        SvgPathData Boundary(bool vertical, double boundarySeed) {
            var points = new ChartPoint[8];
            for (var i = 0; i < points.Length; i++) {
                var jitter = Random(boundarySeed + i * (vertical ? 17 : 23)) * amplitude * 2 - amplitude;
                points[i] = vertical ? new ChartPoint(x + width / 2 + jitter, y + i * height / 7)
                    : new ChartPoint(x + i * width / 7, y + height / 2 + jitter);
            }
            var result = new SvgPathData().Add(new SvgPathCommand('M', new[] { points[0].X, points[0].Y }));
            for (var i = 0; i < 7; i++) {
                var first = points[i]; var second = points[i + 1];
                var offset = amplitude * 1.5 * (i % 2 == 0 ? 1 : -1) * Random(boundarySeed + i * (vertical ? 31 : 37) + (vertical ? 7 : 11));
                var middle = vertical ? (first.Y + second.Y) / 2 : (first.X + second.X) / 2;
                result.Add(new SvgPathCommand('C', vertical
                    ? new[] { first.X + offset, middle, second.X - offset, middle, second.X, second.Y }
                    : new[] { middle, first.Y + offset, middle, second.Y - offset, second.X, second.Y }));
            }
            return result;
        }
    }

    private static int Hash(string value) {
        var hash = 0;
        foreach (var ch in value) hash = unchecked(hash * 31 + ch);
        return hash;
    }

    // JavaScript's ToInt32 semantics preserve the pinned renderer's mulberry32 result,
    // including fractional and negative configured seeds, without process randomness.
    private static double Random(double seed) {
        var t = ToInt32(seed + 0x6d2b79f5);
        t = unchecked((t ^ (int)((uint)t >> 15)) * (t | 1));
        t ^= unchecked(t + unchecked((t ^ (int)((uint)t >> 7)) * (t | 61)));
        return (uint)(t ^ (int)((uint)t >> 14)) / 4294967296d;
    }
    private static int ToInt32(double value) => unchecked((int)(uint)((Math.Truncate(value) % 4294967296d + 4294967296d) % 4294967296d));
}
