using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

internal sealed partial class VisualSceneBuilder {
    /// <summary>Uses captured native semantic extents for script-free states and linked marker-free observations.</summary>
    private void CaptureMarkStateOutlines(Chart chart) {
        var active = false;
        foreach (var node in _nodes) if (node is VisualSceneGroup group && (group.Href != null
            || group.Metadata.ContainsKey("aria-current") || group.Metadata.ContainsKey("data-cfx-selected"))) { active = true; break; }
        if (!active) return;
        var regions = new Dictionary<string, ChartRect>(StringComparer.Ordinal);
        foreach (var region in _regions) regions[region.Id] = region.Bounds;
        var colors = chart.Options.Theme.Resolve(chart.Options.ThemeMode);
        var output = new List<VisualSceneNode>(_nodes.Count);
        var scopes = new Stack<MarkStateScope>();
        foreach (var node in _nodes) {
            if (node is VisualSceneGroup group) {
                if (group.Href != null) foreach (var ancestor in scopes) ancestor.NestedLink = true;
                var hidden = group.Metadata.TryGetValue("data-cfx-visible", out var visible) && visible == "false";
                var markClip = group.Metadata.ContainsKey("data-cfx-point") || group.Metadata.ContainsKey("data-cfx-source-point")
                    ? DataProjection?.Plot : null;
                var scope = new MarkStateScope(group, hidden || scopes.Count > 0 && scopes.Peek().Hidden, output.Count, markClip);
                scopes.Push(scope);
            }
            if (node.Id != null && regions.TryGetValue(node.Id, out var bounds) && (scopes.Count == 0 || !scopes.Peek().Hidden)) {
                foreach (var scope in scopes) if (!scope.Hidden) scope.Include(bounds);
            }
            if (node is VisualSceneMark && IsMarkerRole(node.Role)) foreach (var scope in scopes) scope.Marker = true;
            if (node is VisualSceneEndGroup && scopes.Count > 0) {
                var scope = scopes.Pop();
                if (scope.Bounds == null && scope.Group.Href != null && (scope.Group.Role == ChartDomRoles.ScheduleRow
                    || scope.Group.Metadata.ContainsKey("data-cfx-point") || scope.Group.Metadata.ContainsKey("data-cfx-source-point")))
                    output[scope.OutputIndex] = CopyMarkGroup(scope.Group, NonLinkMarkMetadata(scope.Group), null, scope.Group.Tooltip);
                if (!scope.Hidden && scope.Bounds is { } extent) {
                    if (scope.Group.Href != null) {
                        // Transparent native geometry gives a marker-free line/range point a real pointer target.
                        var hit = new VisualSceneRectangle(extent, 0, ChartColor.Transparent, null, 0, "mark-link-hit-area", null);
                        if (scope.NestedLink) {
                            // A linked row and its independently linked cells are siblings, never nested anchors.
                            // Paint the row target first so cell targets retain pointer precedence.
                            output[scope.OutputIndex] = CopyMarkGroup(scope.Group, NonLinkMarkMetadata(scope.Group), null, scope.Group.Tooltip);
                            var navigation = new Dictionary<string, string> { ["role"] = "link" };
                            foreach (var key in new[] { "aria-label", "aria-current", "data-cfx-current", "data-cfx-selected", "target" })
                                if (scope.Group.Metadata.TryGetValue(key, out var value)) navigation[key] = value;
                            output.InsertRange(scope.OutputIndex + 1, new VisualSceneNode[] {
                                new VisualSceneGroup("row-navigation", null, null, navigation, href: scope.Group.Href),
                                hit, new VisualSceneEndGroup()
                            });
                        } else output.Add(hit);
                    }
                    var current = scope.Group.Metadata.ContainsKey("data-cfx-current");
                    var selected = scope.Group.Metadata.ContainsKey("data-cfx-selected");
                    if ((current || selected) && scope.Group.Role != ChartDomRoles.ScheduleRow) AddStaticStateLook(output, extent, current, selected, colors, scope.Dot);
                }
            }
            output.Add(node);
        }
        _nodes.Clear(); _nodes.AddRange(output);
    }

    /// <summary>
    /// The static looks of design-language §3, identical in SVG, PNG and the enhance runtime, for every mark family (bars,
    /// cells, schedule bars, points): a 2px surface halo and a 2px outline offset 2px from the mark. Selected uses the
    /// primary ink, current the secondary ink, so the two never look alike and neither borrows a series colour or reads
    /// as focus (no dashes; focus rings stay a browser :focus-visible concern). Roles stay mark-selected-outline and
    /// mark-current-outline so hosts and the enhance runtime select them unchanged.
    /// </summary>
    private static void AddStaticStateLook(List<VisualSceneNode> output, ChartRect extent, bool current, bool selected, Themes.VisualThemeColors colors, bool dot) {
        if (dot) { AddPointStateLook(output, extent, current, selected, colors); return; }
        var ring = new ChartRect(extent.X - 2, extent.Y - 2, extent.Width + 4, extent.Height + 4);
        var path = VisualStateSceneTools.RoundedRect(ring, Math.Min(4, Math.Min(ring.Width, ring.Height) / 2));
        output.Add(new VisualScenePath(path, true, null, colors.Background, 4, "mark-state-halo", null,
            paint: VisualChartPaint.Stroke(Themes.SvgPaint.Of(colors.Background, Themes.SvgColorRole.Surface))));
        if (selected) output.Add(new VisualScenePath(path, true, null, colors.Foreground, 2, "mark-selected-outline", null,
            paint: VisualChartPaint.Stroke(Themes.SvgPaint.Of(colors.Foreground, Themes.SvgColorRole.Text))));
        if (current) {
            var inner = selected ? new ChartRect(extent.X + 2, extent.Y + 2, Math.Max(0, extent.Width - 4), Math.Max(0, extent.Height - 4)) : ring;
            output.Add(new VisualScenePath(VisualStateSceneTools.RoundedRect(inner, Math.Min(4, Math.Min(inner.Width, inner.Height) / 2)), true, null,
                colors.MutedForeground, 2, "mark-current-outline", null,
                paint: VisualChartPaint.Stroke(Themes.SvgPaint.Of(colors.MutedForeground, Themes.SvgColorRole.Text))));
        }
    }

    /// <summary>
    /// A point mark (a marker with its surface ring, or a marker-free line point) takes the same look as a ring: its radius
    /// is the marker's plus the 2px gap, so the outline circles the point instead of boxing it.
    /// </summary>
    private static void AddPointStateLook(List<VisualSceneNode> output, ChartRect extent, bool current, bool selected, Themes.VisualThemeColors colors) {
        var cx = extent.X + extent.Width / 2; var cy = extent.Y + extent.Height / 2; var radius = Math.Max(extent.Width, extent.Height) / 2 + 2;
        output.Add(new VisualSceneEllipse(cx, cy, radius, radius, null, colors.Background, 4, "mark-state-halo", null,
            VisualChartPaint.Stroke(Themes.SvgPaint.Of(colors.Background, Themes.SvgColorRole.Surface))));
        if (selected) output.Add(new VisualSceneEllipse(cx, cy, radius, radius, null, colors.Foreground, 2, "mark-selected-outline", null,
            VisualChartPaint.Stroke(Themes.SvgPaint.Of(colors.Foreground, Themes.SvgColorRole.Text))));
        if (current) {
            var inner = selected ? Math.Max(1, radius - 4) : radius;
            output.Add(new VisualSceneEllipse(cx, cy, inner, inner, null, colors.MutedForeground, 2, "mark-current-outline", null,
                VisualChartPaint.Stroke(Themes.SvgPaint.Of(colors.MutedForeground, Themes.SvgColorRole.Text))));
        }
    }

    private static bool IsMarkerRole(string? role) => role == "marker" || role != null && role.EndsWith("-marker", StringComparison.Ordinal);

    private static Dictionary<string, string> NonLinkMarkMetadata(VisualSceneGroup group) {
        var metadata = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var entry in group.Metadata) metadata.Add(entry.Key, entry.Value);
        metadata["role"] = "group"; metadata.Remove("target");
        return metadata;
    }

    private sealed class MarkStateScope {
        internal MarkStateScope(VisualSceneGroup group, bool hidden, int outputIndex, ChartRect? clip) { Group = group; Hidden = hidden; OutputIndex = outputIndex; Clip = clip; }
        internal VisualSceneGroup Group { get; }
        internal bool Hidden { get; }
        internal bool NestedLink { get; set; }
        internal bool Marker { get; set; }
        private bool Point { get; set; }
        /// <summary>Whether the mark is a point: a marker alone (a square extent), or a marker-free point given a synthetic extent.</summary>
        internal bool Dot => Point || Marker && Bounds is { } extent && Math.Abs(extent.Width - extent.Height) <= 1;
        internal int OutputIndex { get; }
        private ChartRect? Clip { get; }
        internal ChartRect? Bounds { get; private set; }
        internal void Include(ChartRect value) {
            if (value.Width < 0 || value.Height < 0) return;
            if (value.Width == 0 || value.Height == 0) {
                if (value.Width == 0 && value.Height == 0) Point = true;
                value = new ChartRect(value.X - (value.Width == 0 ? 4 : 0), value.Y - (value.Height == 0 ? 4 : 0),
                    Math.Max(8, value.Width), Math.Max(8, value.Height));
            }
            if (Clip is { } clip) {
                var left = Math.Max(value.Left, clip.Left); var top = Math.Max(value.Top, clip.Top);
                var right = Math.Min(value.Right, clip.Right); var bottom = Math.Min(value.Bottom, clip.Bottom);
                if (right <= left || bottom <= top) return;
                value = new ChartRect(left, top, right - left, bottom - top);
            }
            if (Bounds is not { } previous) { Bounds = value; return; }
            var x = Math.Min(previous.Left, value.Left); var y = Math.Min(previous.Top, value.Top);
            Bounds = new ChartRect(x, y, Math.Max(previous.Right, value.Right) - x, Math.Max(previous.Bottom, value.Bottom) - y);
        }
    }
}
