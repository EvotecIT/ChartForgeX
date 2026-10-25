using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

/// <summary>Measures the canonical glyph parts and marks, including the existing native stroke outline.</summary>
internal static class TopologyGlyphBounds {
    internal readonly struct Plan {
        internal Plan(ChartRect ink, ChartRect geometry, double strokeOverflow) { Ink = ink; Geometry = geometry; StrokeOverflow = strokeOverflow; }
        internal ChartRect Ink { get; }
        internal ChartRect Geometry { get; }
        internal double StrokeOverflow { get; }
    }

    internal static Plan Measure(TopologyNode node, TopologyRenderOptions options, double axisStrokeWidth, double cornerRadius) {
        if (TopologyPreparedNodeBasis.ForPresentation(node) is {} basis) {
            var raw = Measure(basis.OriginalNode(node), options, axisStrokeWidth, cornerRadius);
            return new Plan(basis.Project(node, raw.Ink), basis.Project(node, raw.Geometry), raw.StrokeOverflow * basis.Factor);
        }
        var x = node.X + node.Width / 2; var y = node.Y + node.Height / 2;
        var scale = node.GlyphScale ?? 1;
        var icon = ResolveNodeIcon(node, options);
        var kind = icon?.NodeKind ?? node.Kind;
        var resolved = new TopologyNode { Kind = kind, IconId = node.IconId, IconShape = node.IconShape };
        var shape = EffectiveIconShape(resolved, options);
        var points = new List<ChartPoint>();
        var geometry = new List<ChartPoint>();
        var overflow = 0d;
        if (ResolveRenderableNodeArtwork(node, options) != null) {
            Include(new ChartRect(x - 13, y - 13, 26, 26));
        } else {
            var parts = TopologyGlyphSurfaceGeometry.Parts(node, shape, options, x, y, 1, cornerRadius);
            foreach (var part in parts) {
                var bounds = part.Type == TopologyGlyphSurfaceGeometry.Kind.Path ? part.Path!.Bounds()
                    : part.Type == TopologyGlyphSurfaceGeometry.Kind.Ellipse
                        ? new ChartRect(x + part.X - part.Width, y + part.Y - part.Height, 2 * part.Width, 2 * part.Height)
                        : new ChartRect(x - part.Width / 2, y - part.Height / 2, part.Width, part.Height);
                Include(bounds, axisStrokeWidth / 2);
            }
            if (shape != TopologyIconShape.Cloud && shape != TopologyIconShape.Database
                && !((node.IconShape is null or TopologyIconShape.Auto) && kind == TopologyNodeKind.Database)) {
                var marks = TopologyInfrastructureGlyphs.Build(shape, kind, x, y);
                if (marks == null) {
                    // Text uses this measured fitting cell; its native text owner keeps all ink inside it.
                    Include(new ChartRect(x - 12, y - 10, 24, 22));
                } else foreach (var mark in marks) {
                    if (mark.PathData == null) {
                        Include(new ChartRect(mark.Cx - mark.Rx, mark.Cy - mark.Ry, mark.Rx * 2, mark.Ry * 2), mark.Filled ? 0 : mark.StrokeWidth / 2);
                        continue;
                    }
                    foreach (var contour in ChartMapPathParser.ParseSubpaths(mark.PathData, 2)) {
                        points.AddRange(contour.Points); geometry.AddRange(contour.Points);
                        if (mark.Filled) continue;
                        var path = contour.Points.ToList();
                        if (contour.IsClosed && path.Count > 1) path.Add(path[0]);
                        var outline = new List<List<ChartPoint>>();
                        RasterStroker.AppendOutline(path, mark.StrokeWidth, mark.RoundCap ? RasterLineCap.Round : RasterLineCap.Butt,
                            mark.RoundJoin ? RasterLineJoin.Round : RasterLineJoin.Miter, 4, 1, outline);
                        foreach (var piece in outline) points.AddRange(piece);
                        overflow = Math.Max(overflow, mark.StrokeWidth / 2);
                    }
                }
            }
        }
        var left = points.Min(p => p.X); var top = points.Min(p => p.Y);
        var right = points.Max(p => p.X); var bottom = points.Max(p => p.Y);
        var ink = new ChartRect(x + (left - x) * scale, y + (top - y) * scale,
            (right - left) * scale, (bottom - top) * scale);
        var gx = geometry.Min(p => p.X); var gy = geometry.Min(p => p.Y);
        var gr = geometry.Max(p => p.X); var gb = geometry.Max(p => p.Y);
        return new Plan(ink, new ChartRect(x + (gx - x) * scale, y + (gy - y) * scale, (gr - gx) * scale, (gb - gy) * scale), overflow * scale);

        void Include(ChartRect bounds, double halfStroke = 0) {
            geometry.Add(new ChartPoint(bounds.Left, bounds.Top)); geometry.Add(new ChartPoint(bounds.Right, bounds.Bottom));
            points.Add(new ChartPoint(bounds.Left - halfStroke, bounds.Top - halfStroke));
            points.Add(new ChartPoint(bounds.Right + halfStroke, bounds.Bottom + halfStroke));
            overflow = Math.Max(overflow, halfStroke);
        }
    }
}
