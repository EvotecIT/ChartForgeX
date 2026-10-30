using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Svg;

namespace ChartForgeX.Rendering;

internal static partial class VisualSceneSvgRenderer {
    /// <summary>Admits fluid geometry before effect capture and records logical transforms without changing the scene.</summary>
    private static FluidSvgLayout? PrepareFluidLayout(VisualScene scene, VisualSvgOptions? options) =>
        options?.FluidWidth == true ? new FluidSvgLayout(scene, options) : null;

    /// <summary>The root has no viewBox: one vertical user unit and each fixed-size viewport unit is one CSS pixel.</summary>
    private static void WriteFluidRootAttributes(SvgMarkupWriter writer, VisualScene scene) =>
        writer.Attribute("width", "100%").Attribute("height", scene.Size.Height)
            .Attribute("style", "width:100%;height:" + N(scene.Size.Height) + "px;display:block");

    private static Dictionary<int, int> WriteFluidClips(SvgMarkupWriter writer, VisualScene scene, string prefix,
        VisualSvgOptions? options, VisualSvgOptions?[]? colorScopes, FluidSvgLayout layout) {
        var aliases = new Dictionary<int, int>();
        var clips = new Dictionary<(double X, double Y, double W, double H, double Radius), int>();
        var opened = false;
        for (var i = 0; i < scene.Nodes.Count; i++) {
            if (scene.Nodes[i] is VisualSceneGradient gradient) {
                OpenDefinitions();
                WriteFluidGradientDefinition(writer, gradient, prefix, i, colorScopes == null ? options : colorScopes[i], layout);
            }
            if (!(scene.Nodes[i] is VisualSceneGroup) || !layout.Clips.TryGetValue(i, out var clip)) continue;
            var transform = layout.Transforms[i];
            var x = clip.Bounds.X + transform.E; var y = clip.Bounds.Y + transform.F;
            var key = (x, y, clip.Bounds.Width, clip.Bounds.Height, clip.Radius);
            if (clips.TryGetValue(key, out var canonical)) { aliases.Add(i, canonical); continue; }
            clips.Add(key, i);
            OpenDefinitions();
            writer.StartElement("clipPath").Attribute("id", prefix + "-clip-" + i.ToString(CultureInfo.InvariantCulture))
                .Attribute("clipPathUnits", "userSpaceOnUse").EndStartElement();
            WriteFluidRectangleGeometry(writer, new ChartRect(x, y, clip.Bounds.Width, clip.Bounds.Height), clip.Radius, layout.Width);
            writer.EndEmptyElement(); writer.EndElement();
        }
        if (opened) writer.EndElement();
        return aliases;

        void OpenDefinitions() {
            if (opened) return;
            writer.StartElement("defs").EndStartElement(); opened = true;
        }
    }

    private static void WriteFluidGradientDefinition(SvgMarkupWriter writer, VisualSceneGradient gradient, string prefix,
        int index, VisualSvgOptions? options, FluidSvgLayout layout) {
        // Paths retain their logical viewport. Percentage rectangle paints live directly in the root's coordinates.
        var transform = gradient.Shape is VisualScenePath ? VisualSceneTransform.Identity : layout.Transforms[index];
        writer.StartElement("linearGradient").Attribute("id", GradientId(prefix, index)).Attribute("gradientUnits", "userSpaceOnUse")
            .Attribute("x1", FluidPercent(gradient.Start.X + transform.E, layout.Width)).Attribute("y1", gradient.Start.Y + transform.F)
            .Attribute("x2", FluidPercent(gradient.End.X + transform.E, layout.Width)).Attribute("y2", gradient.End.Y + transform.F).EndStartElement();
        WriteGradientStops(writer, gradient.Stops, options);
        writer.EndElement();
    }

    /// <summary>Writes leaves only; the primary serializer continues to own link, metadata, opacity and scope structure.</summary>
    private static bool WriteFluidNode(SvgMarkupWriter writer, VisualScene scene, int index, string prefix,
        VisualSvgOptions? options, FluidSvgLayout layout, PngTextResources? pngText, bool capturePngText,
        VisualSvgFontResources? fonts, NativeLineInkResources? textImages) {
        var node = scene.Nodes[index];
        if (node is VisualSceneGroup || node is VisualSceneEndGroup) return false;
        var transform = layout.Transforms[index];
        if (node is VisualSceneText text) {
            WriteFluidFixedViewport(writer, new ChartPoint(text.X, text.Baseline), transform, layout);
            WriteText(writer, text, prefix, index, options, pngText, capturePngText, fonts, textImages);
            writer.EndElement(); writer.EndElement(); return true;
        }
        var gradient = node as VisualSceneGradient;
        var mark = gradient?.Shape ?? (VisualSceneMark)node;
        var fill = gradient == null ? null : "url(#" + GradientId(prefix, index) + ")";
        var viewport = false;
        if (mark is VisualSceneRectangle rect) {
            WriteFluidRectangleGeometry(writer, new ChartRect(rect.Bounds.X + transform.E, rect.Bounds.Y + transform.F,
                rect.Bounds.Width, rect.Bounds.Height), rect.Radius, layout.Width);
        } else if (mark is VisualSceneEllipse ellipse) {
            writer.StartElement("ellipse").Attribute("cx", FluidPercent(ellipse.Cx + transform.E, layout.Width))
                .Attribute("cy", ellipse.Cy + transform.F).Attribute("rx", ellipse.Rx).Attribute("ry", ellipse.Ry);
        } else if (mark is VisualSceneLine line) {
            writer.StartElement("line").Attribute("x1", FluidPercent(line.Start.X + transform.E, layout.Width)).Attribute("y1", line.Start.Y + transform.F)
                .Attribute("x2", FluidPercent(line.End.X + transform.E, layout.Width)).Attribute("y2", line.End.Y + transform.F);
            if (line.Dash != null) {
                writer.Attribute("stroke-dasharray", DashValue(line.Dash));
                var phase = ChartStrokeDashes.NormalizeOffset(line.Dash, line.DashOffset);
                if (phase != 0) writer.Attribute("stroke-dashoffset", ChartStrokeDashes.FormatNumber(phase));
            }
        } else if (mark is VisualScenePath path) {
            viewport = true;
            if (layout.FixedMarks[index]) {
                var bounds = new ChartPath(path.Commands).Bounds();
                var anchor = new ChartPoint(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
                // Schedule chevrons meet their stretched bar/connector at the middle command (the arrow tip).
                if (IsFluidArrow(path.Role) && path.Commands.Count == 3) anchor = new ChartPoint(path.Commands[1].X, path.Commands[1].Y);
                WriteFluidFixedViewport(writer, anchor, transform, layout);
            } else {
                writer.StartElement("svg").Attribute("width", "100%").Attribute("height", layout.Height)
                    .Attribute("viewBox", "0 0 " + N(layout.Width) + " " + N(layout.Height))
                    .Attribute("preserveAspectRatio", "none").Attribute("overflow", "visible").EndStartElement();
                writer.StartElement("g").Attribute("transform", "translate(" + N(transform.E) + " " + N(transform.F) + ")").EndStartElement();
            }
            writer.StartElement("path").Attribute("d", PathData(path));
        } else throw FluidUnsupported("geometry " + mark.GetType().Name);
        writer.Attribute("vector-effect", "non-scaling-stroke");
        Paint(writer, mark, prefix, index, fill, options); writer.EndEmptyElement();
        if (viewport) { writer.EndElement(); writer.EndElement(); }
        return true;
    }

    private static void WriteFluidRectangleGeometry(SvgMarkupWriter writer, ChartRect bounds, double radius, double width) =>
        writer.StartElement("rect").Attribute("x", FluidPercent(bounds.X, width)).Attribute("y", bounds.Y)
            .Attribute("width", FluidPercent(bounds.Width, width)).Attribute("height", bounds.Height).Attribute("rx", radius).Attribute("ry", radius);

    /// <summary>Anchors a pixel-sized viewport at a fluid position, then applies rigid orientation relative to that anchor.</summary>
    private static void WriteFluidFixedViewport(SvgMarkupWriter writer, ChartPoint anchor, VisualSceneTransform transform, FluidSvgLayout layout) {
        var position = transform.Apply(anchor);
        writer.StartElement("svg").Attribute("x", FluidPercent(position.X, layout.Width)).Attribute("y", position.Y)
            .Attribute("width", layout.Width).Attribute("height", layout.Height)
            .Attribute("viewBox", "0 0 " + N(layout.Width) + " " + N(layout.Height))
            .Attribute("overflow", "visible").EndStartElement();
        writer.StartElement("g").Attribute("transform", "matrix(" + N(transform.A) + " " + N(transform.B) + " " + N(transform.C) + " " + N(transform.D)
            + " " + N(-transform.A * anchor.X - transform.C * anchor.Y) + " " + N(-transform.B * anchor.X - transform.D * anchor.Y) + ")").EndStartElement();
    }

    private static string FluidPercent(double value, double width) => N(value / width * 100) + "%";
    private static bool IsFluidArrow(string? role) => role is "gantt-dependency-arrow" or "gantt-lane-open-end";
    private static NotSupportedException FluidUnsupported(string feature) =>
        new NotSupportedException("Script-free fluid SVG width does not support " + feature + ". Use fixed-width or proportional responsive SVG for this scene.");

    /// <summary>Logical transforms are flattened; only text may retain a rotation without stretching its glyphs.</summary>
    private sealed class FluidSvgLayout {
        internal readonly double Width, Height;
        internal readonly VisualSceneTransform[] Transforms;
        internal readonly bool[] FixedMarks;
        internal readonly Dictionary<int, (ChartRect Bounds, double Radius)> Clips = new();

        internal FluidSvgLayout(VisualScene scene, VisualSvgOptions options) {
            Width = scene.Size.Width; Height = scene.Size.Height;
            Transforms = new VisualSceneTransform[scene.Nodes.Count]; FixedMarks = new bool[scene.Nodes.Count];
            if (options.PatternTiles) throw FluidUnsupported("pattern tiles");
            if (scene.HasSurfaceEffects || scene.HasRetainedEffects || scene.HasProceduralSources) throw FluidUnsupported("captured effects or procedural sources");
            var stack = new Stack<(VisualSceneTransform Transform, bool Rotated, bool FixedMark, string? SeriesKind)>();
            var transform = VisualSceneTransform.Identity; var rotated = false; var fixedMark = false; string? seriesKind = null;
            for (var i = 0; i < scene.Nodes.Count; i++) {
                var node = scene.Nodes[i];
                if (node is VisualSceneEndGroup) {
                    if (stack.Count == 0) throw new InvalidOperationException("Unbalanced fluid SVG scene scopes.");
                    (transform, rotated, fixedMark, seriesKind) = stack.Pop();
                } else if (node is VisualSceneGroup group) {
                    if (group.Role is "radar-series" or "polar-series" or "radial-value-axis" or "radial-point" or "sunburst-segment")
                        throw FluidUnsupported("polar chart geometry");
                    stack.Push((transform, rotated, fixedMark, seriesKind));
                    if (group.Metadata.TryGetValue("data-cfx-kind", out var kind)) seriesKind = kind;
                    if (group.Rotation is {} rotation) { transform = transform.Rotate(rotation); rotated |= rotation.Degrees != 0; }
                    if (group.Translation is {} offset) transform = transform.Translate(offset);
                    fixedMark |= IsPointGroup(group, seriesKind);
                    if (group.ImageResource != null) throw FluidUnsupported("image resources");
                    if (group.Clip.HasValue || group.PathClip != null) {
                        if (rotated) throw FluidUnsupported("rotated clipping scopes");
                        if (group.PathClip is {} pathClip) {
                            if (!TryFluidRectangleClip(pathClip, out var clip)) throw FluidUnsupported("arbitrary path clips");
                            Clips.Add(i, clip);
                        } else Clips.Add(i, (group.Clip!.Value, 0));
                    }
                } else {
                    if (node is VisualSceneImage || node is VisualSceneProceduralSource) throw FluidUnsupported("images or procedural sources");
                    if (!(node is VisualSceneMark) && !(node is VisualSceneGradient) && !(node is VisualSceneText)) throw FluidUnsupported("unknown scene commands");
                    if (rotated && !(node is VisualSceneText)) throw FluidUnsupported("rotated non-text geometry");
                    var mark = (node as VisualSceneGradient)?.Shape ?? node as VisualSceneMark;
                    if (mark is VisualSceneSlice) throw FluidUnsupported("polar slices");
                    FixedMarks[i] = fixedMark || IsPointRole(node.Role);
                    if (node is VisualSceneGradient && mark is VisualScenePath && FixedMarks[i]) throw FluidUnsupported("gradient point markers");
                }
                Transforms[i] = transform;
            }
            if (stack.Count != 0) throw new InvalidOperationException("Unbalanced fluid SVG scene scopes.");
        }

        // A custom semantic role may replace "marker"; the native point group still identifies its quantitative mark.
        private static bool IsPointGroup(VisualSceneGroup group, string? seriesKind) => group.Role is "scatter-point" or "bubble-point" or "line-marker"
            || group.Role == "point" && (seriesKind == null || seriesKind is "Line" or "StepLine" or "Area" or "StepArea" or "StackedArea" or "Scatter" or "Bubble");
        private static bool IsPointRole(string? role) => role != null && (role == "marker" || role.EndsWith("-marker", StringComparison.Ordinal)
            || role == "gantt-milestone-shape" || role == "legend-marker" || role == "legend-symbol" || IsFluidArrow(role));
    }

    private static bool TryFluidRectangleClip(VisualScenePath path, out (ChartRect Bounds, double Radius) clip) {
        var bounds = new ChartPath(path.Commands).Bounds();
        var radius = path.Commands.Count == 0 ? 0 : path.Commands[0].X - bounds.Left;
        clip = (bounds, radius);
        if (!path.Close || radius < 0) return false;
        var expected = ChartPathBuilder.RoundedRectangle(bounds, radius).Commands;
        if (expected.SequenceEqual(path.Commands)) return true;
        // A stack envelope rounded at its data end only (design rule 4) clips as its rectangle in fluid output.
        var candidates = path.Commands.SelectMany(command => new[] { command.X - bounds.Left, bounds.Right - command.X, command.Y - bounds.Top, bounds.Bottom - command.Y })
            .Where(value => value > 0 && value <= Math.Min(bounds.Width, bounds.Height) / 2 + 1e-9).Distinct().ToArray();
        foreach (var corners in new[] { VisualCorners.Top, VisualCorners.Bottom, VisualCorners.Left, VisualCorners.Right })
            foreach (var candidate in candidates) {
                var partial = ChartPathBuilder.RoundedRectangle(bounds, (corners & VisualCorners.TopLeft) != 0 ? candidate : 0, (corners & VisualCorners.TopRight) != 0 ? candidate : 0,
                    (corners & VisualCorners.BottomRight) != 0 ? candidate : 0, (corners & VisualCorners.BottomLeft) != 0 ? candidate : 0).Commands;
                if (SameCommands(partial, path.Commands)) { clip = (bounds, 0); return true; }
            }
        return false;
    }

    private static bool SameCommands(IReadOnlyList<ChartPathCommand> left, IReadOnlyList<ChartPathCommand> right) {
        if (left.Count != right.Count) return false;
        for (var index = 0; index < left.Count; index++) {
            var a = left[index]; var b = right[index];
            if (a.Kind != b.Kind || Math.Abs(a.X - b.X) > 1e-6 || Math.Abs(a.Y - b.Y) > 1e-6 || Math.Abs(a.Control1X - b.Control1X) > 1e-6
                || Math.Abs(a.Control1Y - b.Control1Y) > 1e-6 || Math.Abs(a.Control2X - b.Control2X) > 1e-6 || Math.Abs(a.Control2Y - b.Control2Y) > 1e-6) return false;
        }
        return true;
    }
}
