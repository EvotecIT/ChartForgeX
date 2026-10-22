using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Svg;

namespace ChartForgeX.VisualBlocks;

internal static partial class VisualBlockRendering {
    internal static VennDiagramLayout VennLayout(VennDiagramBlock block, ChartRect rect) {
        ValidateVennDiagram(block);
        var maximum = block.Sets.Max(set => set.Size); var divisor = maximum > 0 ? maximum : 1;
        var circles = block.Sets.Select(set => new VennCircle(0, 0, Math.Sqrt(set.Size / divisor / Math.PI))).ToArray();
        if (circles.Length > 1) circles[1] = new VennCircle(VennDistance(circles[0].R, circles[1].R, PairTarget(0, 1)), 0, circles[1].R);
        if (circles.Length == 3) {
            var ac = VennDistance(circles[0].R, circles[2].R, PairTarget(0, 2));
            var bc = VennDistance(circles[1].R, circles[2].R, PairTarget(1, 2));
            var ab = circles[1].X;
            var x = ab > 1e-12 ? (ac * ac + ab * ab - bc * bc) / (2 * ab) : 0;
            x = Math.Max(-2, Math.Min(2, x));
            circles[2] = new VennCircle(x, Math.Sqrt(Math.Max(0, ac * ac - x * x)), circles[2].R);
            FitVennThree(circles, block, divisor);
        }
        var left = circles.Min(circle => circle.X - circle.R); var right = circles.Max(circle => circle.X + circle.R);
        var top = circles.Min(circle => circle.Y - circle.R); var bottom = circles.Max(circle => circle.Y + circle.R);
        var stroke = block.Sets.Select(set => set.StrokeWidth).Concat(block.Intersections.Select(region => region.StrokeWidth)).Max();
        if (stroke >= rect.Width || stroke >= rect.Height) throw new InvalidOperationException("Venn outlines must fit within the diagram content frame.");
        var scale = Math.Min(Math.Max(1, rect.Width - stroke - 2) / Math.Max(.001, right - left), Math.Max(1, rect.Height - stroke - 2) / Math.Max(.001, bottom - top));
        var dx = rect.X + rect.Width / 2 - (left + right) * scale / 2;
        var dy = rect.Y + rect.Height / 2 - (top + bottom) * scale / 2;
        var placed = circles.Select((circle, index) => new VennCirclePlacement(block.Sets[index].Id, new ChartPoint(dx + circle.X * scale, dy + circle.Y * scale), circle.R * scale)).ToArray();
        var regions = new List<VennRegionPlacement>(); var error = 0.0;
        foreach (var region in block.Intersections) {
            var indices = region.SetIds.Select(id => Index(id)).ToArray();
            var raw = VennIntersection(circles, indices);
            var normalizedActual = Math.Max(0, Math.Min(raw.Area, indices.Min(index => block.Sets[index].Size / divisor)));
            var actual = normalizedActual * divisor;
            error = Math.Max(error, Math.Abs(normalizedActual - region.Size / divisor));
            var fitted = VennIntersection(circles.Select(circle => new VennCircle(dx + circle.X * scale, dy + circle.Y * scale, circle.R * scale)).ToArray(), indices);
            regions.Add(new VennRegionPlacement(region.SetIds, region.Size, actual, fitted.Path, fitted.Anchor));
        }
        return new VennDiagramLayout(placed, regions, error);

        int Index(string id) { for (var i = 0; i < block.Sets.Count; i++) if (block.Sets[i].Id == id) return i; throw new InvalidOperationException("Unknown Venn set."); }
        double PairTarget(int a, int b) {
            var ids = new[] { block.Sets[a].Id, block.Sets[b].Id };
            foreach (var region in block.Intersections) if (region.SetIds.Count == 2 && ids.All(region.SetIds.Contains)) return region.Size / divisor;
            var implied = 0.0;
            foreach (var region in block.Intersections) if (region.SetIds.Count == 3) implied = Math.Max(implied, region.Size / divisor);
            if (implied > 0) return implied;
            foreach (var text in block.TextNodes) if (ids.All(text.SetIds.Contains) && text.SetIds.Count >= 2)
                implied = Math.Max(implied, Math.Min(block.Sets[a].Size, block.Sets[b].Size) / divisor * (text.SetIds.Count == 3 ? .35 : .12));
            return implied;
        }
    }

    private static void FitVennThree(VennCircle[] circles, VennDiagramBlock block, double divisor) {
        if (block.Intersections.Count == 0) return;
        var parameters = new[] { circles[1].X, circles[2].X, circles[2].Y };
        var step = Math.Max(.1, circles.Max(circle => circle.R)); var best = Error(parameters);
        for (var iteration = 0; iteration < 100; iteration++) {
            var improved = false;
            for (var axis = 0; axis < parameters.Length; axis++) for (var sign = -1; sign <= 1; sign += 2) {
                var saved = parameters[axis]; parameters[axis] = axis == 0 ? Math.Max(0, saved + sign * step) : saved + sign * step;
                var candidate = Error(parameters);
                if (candidate + 1e-15 < best) { best = candidate; improved = true; } else parameters[axis] = saved;
            }
            if (!improved) step *= .55;
            if (step < 1e-7) break;
        }
        Apply(parameters);
        double Error(double[] values) {
            Apply(values); var result = 0.0;
            foreach (var region in block.Intersections) {
                var ids = region.SetIds.Select(id => { for (var i = 0; i < block.Sets.Count; i++) if (block.Sets[i].Id == id) return i; return -1; }).ToArray();
                var residual = VennIntersection(circles, ids).Area - region.Size / divisor;
                result += residual * residual;
            }
            return result;
        }
        void Apply(double[] values) { circles[1] = new VennCircle(values[0], 0, circles[1].R); circles[2] = new VennCircle(values[1], values[2], circles[2].R); }
    }

    private static double VennDistance(double a, double b, double area) {
        if (area <= 0) return a + b;
        if (area >= Math.PI * Math.Min(a, b) * Math.Min(a, b) - 1e-14) return Math.Abs(a - b);
        var low = Math.Abs(a - b); var high = a + b;
        for (var i = 0; i < 72; i++) { var middle = (low + high) / 2; if (VennPairArea(a, b, middle) > area) low = middle; else high = middle; }
        return (low + high) / 2;
    }

    private static double VennPairArea(double a, double b, double distance) {
        if (distance >= a + b || a == 0 || b == 0) return 0;
        if (distance <= Math.Abs(a - b)) return Math.PI * Math.Min(a, b) * Math.Min(a, b);
        var alpha = Math.Acos(Math.Max(-1, Math.Min(1, (distance * distance + a * a - b * b) / (2 * distance * a))));
        var beta = Math.Acos(Math.Max(-1, Math.Min(1, (distance * distance + b * b - a * a) / (2 * distance * b))));
        return a * a * alpha + b * b * beta - .5 * Math.Sqrt(Math.Max(0, (-distance + a + b) * (distance + a - b) * (distance - a + b) * (distance + a + b)));
    }

    private readonly struct VennCircle {
        public VennCircle(double x, double y, double radius) { X = x; Y = y; R = radius; }
        public double X { get; } public double Y { get; } public double R { get; }
    }

    private readonly struct VennIntersectionGeometry {
        public VennIntersectionGeometry(double area, string path, ChartPoint anchor) { Area = area; Path = path; Anchor = anchor; }
        public double Area { get; } public string Path { get; } public ChartPoint Anchor { get; }
    }

    // Circle boundary arcs are shared by quantitative measurement, SVG paint and native rasterization.
    private static VennIntersectionGeometry VennIntersection(VennCircle[] all, int[] indices) {
        var circles = indices.Select(index => all[index]).ToArray(); var vertices = new List<ChartPoint>();
        if (circles.Any(circle => circle.R <= 0)) return new VennIntersectionGeometry(0, string.Empty, new ChartPoint(0, 0));
        for (var a = 0; a < circles.Length; a++) for (var b = a + 1; b < circles.Length; b++) {
            var first = circles[a]; var second = circles[b]; var dx = second.X - first.X; var dy = second.Y - first.Y;
            var distance = Math.Sqrt(dx * dx + dy * dy);
            if (distance <= 1e-10 || distance >= first.R + second.R - 1e-10 || distance <= Math.Abs(first.R - second.R) + 1e-10) continue;
            var along = (first.R * first.R - second.R * second.R + distance * distance) / (2 * distance);
            var height = Math.Sqrt(Math.Max(0, first.R * first.R - along * along));
            for (var sign = -1; sign <= 1; sign += 2) {
                var point = new ChartPoint(first.X + along * dx / distance - sign * height * dy / distance, first.Y + along * dy / distance + sign * height * dx / distance);
                if (circles.All(circle => Inside(circle, point)) && !vertices.Any(vertex => Squared(vertex, point) < 1e-14)) vertices.Add(point);
            }
        }
        if (vertices.Count < 2) {
            foreach (var circle in circles.OrderBy(circle => circle.R)) if (circles.All(other => Math.Sqrt((circle.X - other.X) * (circle.X - other.X) + (circle.Y - other.Y) * (circle.Y - other.Y)) + circle.R <= other.R + 1e-8)) {
                var path = new SvgPathDataBuilder().MoveTo(circle.X + circle.R, circle.Y).ArcTo(circle.R, circle.R, 0, false, true, circle.X - circle.R, circle.Y).ArcTo(circle.R, circle.R, 0, false, true, circle.X + circle.R, circle.Y).Close().Build();
                return new VennIntersectionGeometry(Math.PI * circle.R * circle.R, path, new ChartPoint(circle.X, circle.Y));
            }
            return new VennIntersectionGeometry(0, string.Empty, new ChartPoint(circles.Average(circle => circle.X), circles.Average(circle => circle.Y)));
        }
        var anchor = new ChartPoint(vertices.Average(point => point.X), vertices.Average(point => point.Y));
        vertices.Sort((a, b) => Math.Atan2(a.Y - anchor.Y, a.X - anchor.X).CompareTo(Math.Atan2(b.Y - anchor.Y, b.X - anchor.X)));
        var builder = new SvgPathDataBuilder().MoveTo(vertices[0].X, vertices[0].Y); var area = 0.0;
        for (var i = 0; i < vertices.Count; i++) {
            var start = vertices[i]; var end = vertices[(i + 1) % vertices.Count];
            foreach (var circle in circles) {
                if (Math.Abs(Squared(start, new ChartPoint(circle.X, circle.Y)) - circle.R * circle.R) > 1e-6 * Math.Max(1, circle.R * circle.R) || Math.Abs(Squared(end, new ChartPoint(circle.X, circle.Y)) - circle.R * circle.R) > 1e-6 * Math.Max(1, circle.R * circle.R)) continue;
                var angle = Math.Atan2(start.Y - circle.Y, start.X - circle.X); var finish = Math.Atan2(end.Y - circle.Y, end.X - circle.X);
                var delta = finish - angle; if (delta <= 0) delta += Math.PI * 2;
                var middle = new ChartPoint(circle.X + circle.R * Math.Cos(angle + delta / 2), circle.Y + circle.R * Math.Sin(angle + delta / 2));
                if (!circles.All(other => Inside(other, middle))) continue;
                builder.ArcTo(circle.R, circle.R, 0, delta > Math.PI, true, end.X, end.Y);
                area += .5 * (circle.R * circle.R * delta + circle.R * circle.X * (Math.Sin(finish) - Math.Sin(angle)) - circle.R * circle.Y * (Math.Cos(finish) - Math.Cos(angle)));
                break;
            }
        }
        return new VennIntersectionGeometry(Math.Max(0, area), builder.Close().Build(), anchor);
    }

    private static bool Inside(VennCircle circle, ChartPoint point) => Squared(point, new ChartPoint(circle.X, circle.Y)) <= circle.R * circle.R + 1e-8;
    private static double Squared(ChartPoint a, ChartPoint b) => (a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y);
}
