using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

/// <summary>Ephemeral typed emission target. Native owners name motion and caption bounds before export.</summary>
internal sealed class CartesianXZoomLayers {
    private readonly VisualSize _size;
    private readonly FontSpec _font;
    private readonly List<PreparedCartesianXLayer> _layers = new();
    private readonly List<(LabelObstacle Obstacle, CartesianXSourceAdmission? SourceX)> _movingMarks = new();
    private readonly List<(LabelObstacle Obstacle, CartesianXSourceAdmission? SourceX)> _fixedMarks = new();

    internal CartesianXZoomLayers(VisualSize size, FontSpec font) { _size = size; _font = font; }
    internal void Add(VisualScene scene, CartesianXMotion motion, ChartRect? clip = null) => _layers.Add(new PreparedCartesianXLayer(motion, clip, scene));
    internal void Draw(CartesianXMotion motion, ChartRect? clip, Action<VisualSceneBuilder> draw, bool marks = false) {
        var index = _layers.Count;
        var builder = new VisualSceneBuilder(_size, _font);
        draw(builder);
        var scene = builder.Build();
        _layers.Insert(index, new PreparedCartesianXLayer(motion, clip, scene));
        if (marks) RegisterMarks(scene, motion, clip);
    }

    internal void Caption(CartesianXMotion motion, ChartRect clip, string id, PlacedLabel label,
        Action<VisualSceneBuilder> draw, CartesianXSourceAdmission? sourceX = null) {
        var builder = new VisualSceneBuilder(_size, _font);
        if (!label.IsDropped) draw(builder);
        var caption = new CartesianXCaption(id, label.Request.Text, label.Bounds, sourceX,
            label.IsDropped ? "placement-exhausted" : label.IsEllipsized ? "ellipsized" : null, label.Request.AssociatedMarkId);
        if (motion == CartesianXMotion.XTrack && !label.IsDropped) {
            caption.MinimumDelta = clip.Left - label.Bounds.Left;
            caption.MaximumDelta = clip.Right - label.Bounds.Right;
        }
        builder.AddRegion(new VisualSemanticRegion(id + "-caption-fact", "x-zoom-caption", label.Bounds, label.Request.Text));
        _layers.Add(new PreparedCartesianXLayer(motion, clip, builder.Build(), caption));
    }

    internal void UnavailableCaption(CartesianXMotion motion, ChartRect clip, string id, string text, string reason) {
        var builder = new VisualSceneBuilder(_size, _font);
        builder.AddRegion(new VisualSemanticRegion(id + "-caption-fact", "x-zoom-caption", default, text));
        builder.AddDiagnostic(new VisualDiagnostic("cartesian.x-zoom-caption-unavailable", id + ": " + reason + "."));
        _layers.Add(new PreparedCartesianXLayer(motion, clip, builder.Build(), new CartesianXCaption(id, text, default, null, reason)));
    }

    internal PreparedCartesianXLevel Seal(CartesianXZoomCoordinates coordinates, IReadOnlyList<double> ticks) {
        var travel = coordinates.Travel;
        var fixedCaptions = _layers.Where(layer => layer.Motion == CartesianXMotion.Fixed && layer.Caption != null
            && layer.Caption.FittingReason != "placement-exhausted").Select(layer => layer.Caption!).ToArray();
        foreach (var layer in _layers) {
            if (layer.Caption is not CartesianXCaption caption || caption.FittingReason == "placement-exhausted") continue;
            var intervals = new List<CartesianXInterval>();
            if (layer.Motion == CartesianXMotion.Fixed) {
                foreach (var mark in _movingMarks) AddIntervals(intervals, mark.Obstacle, caption.Bounds, false, travel, "moving-mark-clearance:" + mark.Obstacle.Id, mark.SourceX);
            } else {
                foreach (var mark in _fixedMarks) AddIntervals(intervals, mark.Obstacle, caption.Bounds, true, travel, "fixed-mark-clearance:" + mark.Obstacle.Id, mark.SourceX);
                foreach (var other in fixedCaptions) AddIntervals(intervals, new LabelObstacle(other.Id, other.Bounds), caption.Bounds, true, travel, "fixed-caption-clearance:" + other.Id);
            }
            caption.Seal(Merge(intervals));
        }
        return new PreparedCartesianXLevel(_size, coordinates, _layers, ticks);
    }

    internal void RegisterMarks(VisualScene scene, CartesianXMotion motion, ChartRect? inkClip) {
        // Series owners supply native marks. Pattern strokes stay inside their marker's filled contour;
        // retaining them as additional obstacles would count clipped hatch overhang as ink.
        var destination = motion == CartesianXMotion.Fixed ? _fixedMarks : _movingMarks;
        var scopes = new Stack<(bool PatternClip, CartesianXSourceAdmission? SourceX, double Opacity)>();
        var patternClip = false; var opacity = 1d; CartesianXSourceAdmission? sourceX = null; var identity = "mark"; var counter = 0;
        foreach (var node in scene.Nodes) {
            if (node is VisualSceneGroup group) {
                scopes.Push((patternClip, sourceX, opacity)); patternClip |= group.PathClip != null; sourceX = group.SourceX ?? sourceX; opacity *= group.Opacity;
                if (group.Id != null) identity = group.Id;
            } else if (node is VisualSceneEndGroup) (patternClip, sourceX, opacity) = scopes.Pop();
            else if (!patternClip && opacity > 0) {
                var gradient = node as VisualSceneGradient;
                var mark = gradient?.Shape ?? node as VisualSceneMark;
                if (mark == null || mark.Opacity == 0) continue;
                var filled = gradient == null ? mark.Fill.HasValue && mark.Fill.Value.A > 0
                    : gradient.AuthoredFill?.ImageData is { } image ? image.AlphaBounds.HasValue
                    : gradient.Stops.Any(stop => stop.Color.A > 0);
                var stroked = mark.Stroke.HasValue && mark.Stroke.Value.A > 0 && mark.StrokeWidth > 0;
                if (!filled && !stroked) continue;
                var contours = VisualSceneGeometry.Flatten(mark, 12);
                destination.Add((new LabelObstacle(identity + "-ink-" + counter++, new LabelMarkShape(contours, filled,
                    stroked ? mark.StrokeWidth : 0, inkClip)), sourceX));
            }
        }
    }

    private static void AddIntervals(List<CartesianXInterval> destination, LabelObstacle obstacle, ChartRect caption,
        bool movingCaption, double travel, string reason, CartesianXSourceAdmission? sourceX = null) {
        var box = new ChartRect(caption.Left - 2, caption.Top - 2, caption.Width + 4, caption.Height + 4);
        var shape = obstacle.Shape ?? new LabelMarkShape(new[] { new List<ChartPoint> {
            new(obstacle.Bounds.Left, obstacle.Bounds.Top), new(obstacle.Bounds.Right, obstacle.Bounds.Top),
            new(obstacle.Bounds.Right, obstacle.Bounds.Bottom), new(obstacle.Bounds.Left, obstacle.Bounds.Bottom)
        } }, true, 0);
        foreach (var interval in shape.HorizontalTranslations(box, reason)) {
            var minimum = movingCaption ? -interval.Maximum : interval.Minimum;
            var maximum = movingCaption ? -interval.Minimum : interval.Maximum;
            minimum = Math.Max(-travel, minimum); maximum = Math.Min(0, maximum);
            if (minimum <= maximum) destination.Add(new CartesianXInterval(minimum, maximum, reason, sourceX));
        }
    }

    private static IEnumerable<CartesianXInterval> Merge(List<CartesianXInterval> intervals) {
        // Keep reason identity while coalescing a contour's adjacent translation intervals.
        foreach (var group in intervals.GroupBy(interval => interval.Reason, StringComparer.Ordinal)) {
            CartesianXInterval? pending = null;
            foreach (var interval in group.OrderBy(interval => interval.Minimum)) {
                if (pending.HasValue && interval.Minimum <= pending.Value.Maximum)
                    pending = new CartesianXInterval(pending.Value.Minimum, Math.Max(pending.Value.Maximum, interval.Maximum), interval.Reason, interval.SourceX);
                else { if (pending.HasValue) yield return pending.Value; pending = interval; }
            }
            if (pending.HasValue) yield return pending.Value;
        }
    }
}
