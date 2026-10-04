using System.Collections.Generic;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

/// <summary>
/// One stroked path, ring, or dot of a built-in node glyph. The SVG renderer writes it as an element
/// and the PNG renderer strokes the same geometry, so both outputs share widths, caps, and joins.
/// </summary>
internal readonly struct TopologyGlyphMark {
    private TopologyGlyphMark(string? pathData, bool circle, double cx, double cy, double rx, double ry, bool fillNone, bool filled, double strokeWidth, bool roundCap, bool roundJoin) {
        PathData = pathData;
        IsCircle = circle;
        Cx = cx;
        Cy = cy;
        Rx = rx;
        Ry = ry;
        FillNone = fillNone;
        Filled = filled;
        StrokeWidth = strokeWidth;
        RoundCap = roundCap;
        RoundJoin = roundJoin;
    }

    /// <summary>SVG path data for a stroked path, or null for a circle or ellipse.</summary>
    public string? PathData { get; }
    public bool IsCircle { get; }
    public double Cx { get; }
    public double Cy { get; }
    public double Rx { get; }
    public double Ry { get; }
    /// <summary>The SVG element carries <c>fill="none"</c>.</summary>
    public bool FillNone { get; }
    /// <summary>The mark is painted with the glyph colour and has no stroke.</summary>
    public bool Filled { get; }
    public double StrokeWidth { get; }
    public bool RoundCap { get; }
    public bool RoundJoin { get; }

    public static TopologyGlyphMark Path(string pathData, double strokeWidth, bool fillNone = true, bool roundCap = true, bool roundJoin = true) =>
        new(pathData, false, 0, 0, 0, 0, fillNone, false, strokeWidth, roundCap, roundJoin);

    public static TopologyGlyphMark Ring(double cx, double cy, double radius, double strokeWidth) =>
        new(null, true, cx, cy, radius, radius, true, false, strokeWidth, false, false);

    public static TopologyGlyphMark Ellipse(double cx, double cy, double rx, double ry, double strokeWidth) =>
        new(null, false, cx, cy, rx, ry, true, false, strokeWidth, false, false);

    public static TopologyGlyphMark Dot(double cx, double cy, double radius) =>
        new(null, true, cx, cy, radius, radius, false, true, 0, false, false);
}

/// <summary>Geometry of the built-in infrastructure glyphs drawn inside topology node icons.</summary>
internal static class TopologyInfrastructureGlyphs {
    /// <summary>The filled drum surface used for database node icons.</summary>
    internal static string DatabaseBodyPath(double cx, double cy) =>
        "M " + F(cx - 10) + " " + F(cy - 7) + " V " + F(cy + 7) + " A 10 4 0 0 0 " + F(cx + 10) + " " + F(cy + 7) + " V " + F(cy - 7);

    /// <summary>The server symbol drawn in white on monitoring-style dot nodes.</summary>
    public static TopologyGlyphMark[] DotServer(double cx, double cy) => new[] {
        TopologyGlyphMark.Path("M " + F(cx - 4.2) + " " + F(cy - 3.6) + " H " + F(cx + 4.2) + " V " + F(cy - 0.8) + " H " + F(cx - 4.2) + " Z M " + F(cx - 4.2) + " " + F(cy + 1.5) + " H " + F(cx + 4.2) + " V " + F(cy + 4.2) + " H " + F(cx - 4.2) + " Z", 1.15, roundCap: false),
        TopologyGlyphMark.Path("M " + F(cx + 2.1) + " " + F(cy - 2.2) + " H " + F(cx + 3.1) + " M " + F(cx + 2.1) + " " + F(cy + 2.9) + " H " + F(cx + 3.1), 1.2, fillNone: false, roundJoin: false)
    };

    /// <summary>Returns the marks of a built-in glyph, or null when the node shows a text glyph instead.</summary>
    public static TopologyGlyphMark[]? Build(TopologyIconShape shape, TopologyNodeKind kind, double cx, double cy) {
        switch (shape) {
            case TopologyIconShape.Site:
                return new[] { TopologyGlyphMark.Path("M " + F(cx - 6) + " " + F(cy + 7) + " V " + F(cy - 7) + " H " + F(cx + 6) + " V " + F(cy + 7) + " M " + F(cx - 2) + " " + F(cy + 7) + " V " + F(cy + 2) + " H " + F(cx + 2) + " V " + F(cy + 7) + " M " + F(cx - 3.5) + " " + F(cy - 3) + " H " + F(cx - 0.5) + " M " + F(cx + 2.5) + " " + F(cy - 3) + " H " + F(cx + 5.5) + " M " + F(cx - 3.5) + " " + F(cy + 1) + " H " + F(cx - 0.5) + " M " + F(cx + 2.5) + " " + F(cy + 1) + " H " + F(cx + 5.5), 1.7) };
            case TopologyIconShape.Server:
            case TopologyIconShape.DomainController:
            case TopologyIconShape.ReadOnlyDomainController:
                var server = TopologyGlyphMark.Path("M " + F(cx - 7) + " " + F(cy - 6) + " H " + F(cx + 7) + " V " + F(cy - 1) + " H " + F(cx - 7) + " Z M " + F(cx - 7) + " " + F(cy + 2) + " H " + F(cx + 7) + " V " + F(cy + 7) + " H " + F(cx - 7) + " Z M " + F(cx + 4.5) + " " + F(cy - 3.5) + " H " + F(cx + 5.5) + " M " + F(cx + 4.5) + " " + F(cy + 4.5) + " H " + F(cx + 5.5), 1.7);
                if (shape != TopologyIconShape.ReadOnlyDomainController) return new[] { server };
                return new[] { server, TopologyGlyphMark.Path("M " + F(cx - 8) + " " + F(cy + 8) + " L " + F(cx + 8) + " " + F(cy - 8), 1.4, fillNone: false, roundJoin: false) };
            case TopologyIconShape.Network:
                return new[] {
                    TopologyGlyphMark.Path("M " + F(cx - 7) + " " + F(cy + 5) + " L " + F(cx) + " " + F(cy - 6) + " L " + F(cx + 7) + " " + F(cy + 5) + " M " + F(cx - 7) + " " + F(cy + 5) + " H " + F(cx + 7), 1.6),
                    TopologyGlyphMark.Dot(cx, cy - 6, 2.2),
                    TopologyGlyphMark.Dot(cx - 7, cy + 5, 2.2),
                    TopologyGlyphMark.Dot(cx + 7, cy + 5, 2.2)
                };
            case TopologyIconShape.NetworkSwitch:
                return new[] { TopologyGlyphMark.Path("M " + F(cx - 8) + " " + F(cy - 4) + " H " + F(cx + 8) + " V " + F(cy + 4) + " H " + F(cx - 8) + " Z M " + F(cx - 5) + " " + F(cy) + " H " + F(cx - 2) + " M " + F(cx + 2) + " " + F(cy) + " H " + F(cx + 5) + " M " + F(cx - 4) + " " + F(cy - 7) + " L " + F(cx - 1) + " " + F(cy - 4) + " M " + F(cx + 4) + " " + F(cy + 7) + " L " + F(cx + 1) + " " + F(cy + 4), 1.5) };
            case TopologyIconShape.Router:
                return new[] { TopologyGlyphMark.Path("M " + F(cx) + " " + F(cy - 8) + " L " + F(cx + 8) + " " + F(cy) + " L " + F(cx) + " " + F(cy + 8) + " L " + F(cx - 8) + " " + F(cy) + " Z M " + F(cx - 4) + " " + F(cy) + " H " + F(cx + 4) + " M " + F(cx) + " " + F(cy - 4) + " V " + F(cy + 4), 1.5) };
            case TopologyIconShape.NetworkSegment:
                return new[] { TopologyGlyphMark.Path("M " + F(cx - 9) + " " + F(cy - 4) + " H " + F(cx + 9) + " M " + F(cx - 9) + " " + F(cy + 4) + " H " + F(cx + 9) + " M " + F(cx - 5) + " " + F(cy - 7) + " V " + F(cy + 7) + " M " + F(cx + 5) + " " + F(cy - 7) + " V " + F(cy + 7), 1.5, roundJoin: false) };
            case TopologyIconShape.LoadBalancer:
                return new[] { TopologyGlyphMark.Path("M " + F(cx) + " " + F(cy - 8) + " V " + F(cy + 8) + " M " + F(cx - 8) + " " + F(cy - 3) + " H " + F(cx) + " L " + F(cx + 6) + " " + F(cy - 7) + " M " + F(cx - 8) + " " + F(cy + 3) + " H " + F(cx) + " L " + F(cx + 6) + " " + F(cy + 7), 1.6) };
            case TopologyIconShape.Firewall:
                return new[] { TopologyGlyphMark.Path("M " + F(cx - 8) + " " + F(cy - 6) + " H " + F(cx + 8) + " V " + F(cy + 6) + " H " + F(cx - 8) + " Z M " + F(cx - 3) + " " + F(cy - 6) + " V " + F(cy - 1) + " M " + F(cx + 3) + " " + F(cy - 1) + " V " + F(cy + 6) + " M " + F(cx - 8) + " " + F(cy) + " H " + F(cx - 2) + " M " + F(cx + 2) + " " + F(cy) + " H " + F(cx + 8), 1.5) };
            case TopologyIconShape.Service:
                return new[] {
                    TopologyGlyphMark.Ring(cx, cy, 5.5, 1.7),
                    TopologyGlyphMark.Path("M " + F(cx) + " " + F(cy - 9) + " V " + F(cy - 7) + " M " + F(cx) + " " + F(cy + 7) + " V " + F(cy + 9) + " M " + F(cx - 9) + " " + F(cy) + " H " + F(cx - 7) + " M " + F(cx + 7) + " " + F(cy) + " H " + F(cx + 9), 1.7, fillNone: false, roundJoin: false)
                };
            case TopologyIconShape.Database:
                return new[] {
                    TopologyGlyphMark.Ellipse(cx, cy - 6, 8, 3.5, 1.5),
                    TopologyGlyphMark.Path("M " + F(cx - 8) + " " + F(cy - 6) + " V " + F(cy + 6) + " A 8 3.5 0 0 0 " + F(cx + 8) + " " + F(cy + 6) + " V " + F(cy - 6), 1.5, roundCap: false, roundJoin: false)
                };
            case TopologyIconShape.Person:
                return new[] { TopologyGlyphMark.Path("M " + F(cx) + " " + F(cy - 7) + " A 4 4 0 1 1 " + F(cx - 0.1) + " " + F(cy - 7) + " M " + F(cx - 8) + " " + F(cy + 8) + " A 8 7 0 0 1 " + F(cx + 8) + " " + F(cy + 8), 1.6, roundJoin: false) };
            case TopologyIconShape.Team:
                return new[] { TopologyGlyphMark.Path("M " + F(cx - 5) + " " + F(cy - 6) + " A 3 3 0 1 1 " + F(cx - 5.1) + " " + F(cy - 6) + " M " + F(cx + 5) + " " + F(cy - 6) + " A 3 3 0 1 1 " + F(cx + 4.9) + " " + F(cy - 6) + " M " + F(cx - 11) + " " + F(cy + 7) + " A 6 5 0 0 1 " + F(cx - 1) + " " + F(cy + 7) + " M " + F(cx + 1) + " " + F(cy + 7) + " A 6 5 0 0 1 " + F(cx + 11) + " " + F(cy + 7), 1.5, roundJoin: false) };
            case TopologyIconShape.Storage:
                return new[] {
                    TopologyGlyphMark.Path("M " + F(cx - 8) + " " + F(cy - 6) + " H " + F(cx + 8) + " V " + F(cy - 1) + " H " + F(cx - 8) + " Z M " + F(cx - 8) + " " + F(cy + 2) + " H " + F(cx + 8) + " V " + F(cy + 7) + " H " + F(cx - 8) + " Z M " + F(cx - 4.5) + " " + F(cy - 3.5) + " H " + F(cx + 1.5) + " M " + F(cx - 4.5) + " " + F(cy + 4.5) + " H " + F(cx + 1.5), 1.5),
                    TopologyGlyphMark.Dot(cx + 5, cy - 3.5, 1.2),
                    TopologyGlyphMark.Dot(cx + 5, cy + 4.5, 1.2)
                };
            case TopologyIconShape.Application:
                return new[] { TopologyGlyphMark.Path("M " + F(cx - 8) + " " + F(cy - 7) + " H " + F(cx + 8) + " V " + F(cy + 7) + " H " + F(cx - 8) + " Z M " + F(cx - 8) + " " + F(cy - 3) + " H " + F(cx + 8) + " M " + F(cx - 5) + " " + F(cy - 5) + " H " + F(cx - 4) + " M " + F(cx - 1.5) + " " + F(cy - 5) + " H " + F(cx - 0.5) + " M " + F(cx - 3) + " " + F(cy + 1) + " H " + F(cx + 3) + " M " + F(cx - 3) + " " + F(cy + 4) + " H " + F(cx + 3), 1.45) };
            case TopologyIconShape.Certificate:
                return new[] {
                    TopologyGlyphMark.Path("M " + F(cx - 6) + " " + F(cy - 8) + " H " + F(cx + 4) + " L " + F(cx + 8) + " " + F(cy - 4) + " V " + F(cy + 7) + " H " + F(cx - 6) + " Z M " + F(cx + 4) + " " + F(cy - 8) + " V " + F(cy - 4) + " H " + F(cx + 8) + " M " + F(cx - 3) + " " + F(cy - 1) + " H " + F(cx + 4) + " M " + F(cx - 3) + " " + F(cy + 2) + " H " + F(cx + 2), 1.4),
                    TopologyGlyphMark.Ring(cx - 4, cy + 7, 2.5, 1.3)
                };
            case TopologyIconShape.Desktop:
                return new[] { TopologyGlyphMark.Path("M " + F(cx - 8) + " " + F(cy - 7) + " H " + F(cx + 8) + " V " + F(cy + 4) + " H " + F(cx - 8) + " Z M " + F(cx) + " " + F(cy + 4) + " V " + F(cy + 8) + " M " + F(cx - 5) + " " + F(cy + 8) + " H " + F(cx + 5), 1.5) };
            case TopologyIconShape.Laptop:
                return new[] { TopologyGlyphMark.Path("M " + F(cx - 7) + " " + F(cy - 7) + " H " + F(cx + 7) + " V " + F(cy + 3) + " H " + F(cx - 7) + " Z M " + F(cx - 10) + " " + F(cy + 7) + " H " + F(cx + 10) + " L " + F(cx + 7) + " " + F(cy + 3) + " H " + F(cx - 7) + " Z", 1.45) };
            case TopologyIconShape.Forest:
                return new[] { TopologyGlyphMark.Path("M " + F(cx) + " " + F(cy - 9) + " L " + F(cx + 6) + " " + F(cy) + " H " + F(cx + 2.5) + " L " + F(cx + 8) + " " + F(cy + 8) + " H " + F(cx - 8) + " L " + F(cx - 2.5) + " " + F(cy) + " H " + F(cx - 6) + " Z M " + F(cx) + " " + F(cy) + " V " + F(cy + 8), 1.4) };
            case TopologyIconShape.Domain:
                return new[] { TopologyGlyphMark.Path("M " + F(cx) + " " + F(cy - 9) + " L " + F(cx + 8) + " " + F(cy - 3) + " V " + F(cy + 5) + " L " + F(cx) + " " + F(cy + 9) + " L " + F(cx - 8) + " " + F(cy + 5) + " V " + F(cy - 3) + " Z M " + F(cx - 8) + " " + F(cy - 3) + " L " + F(cx) + " " + F(cy + 2) + " L " + F(cx + 8) + " " + F(cy - 3) + " M " + F(cx) + " " + F(cy + 2) + " V " + F(cy + 9), 1.4) };
            case TopologyIconShape.Badge:
                return null;
            default:
                if (kind != TopologyNodeKind.Queue) return null;
                return new[] { TopologyGlyphMark.Path("M " + F(cx - 7) + " " + F(cy - 6) + " H " + F(cx + 7) + " M " + F(cx - 7) + " " + F(cy) + " H " + F(cx + 7) + " M " + F(cx - 7) + " " + F(cy + 6) + " H " + F(cx + 7), 2, fillNone: false, roundJoin: false) };
        }
    }
}
