using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

/// <summary>Indexes occupied text and mark boxes in logical-pixel cells.</summary>
internal sealed class LabelSpatialIndex {
    private const double CellSize = 64;
    private readonly double _gap;
    private double _maximumLabelGap;
    private readonly Dictionary<(int X, int Y), List<Entry>> _cells = new();
    private readonly List<Entry> _large = new();
    public LabelSpatialIndex(double gap) { _gap = gap; }
    public void Add(ChartRect bounds, string? id, LabelMarkShape? shape = null, double? labelGap = null) {
        var gap = labelGap ?? _gap;
        if (id == null) _maximumLabelGap = Math.Max(_maximumLabelGap, gap);
        var entry = new Entry(bounds, id, shape, gap);
        var cells = Cells(bounds);
        if ((long)(cells.Right - cells.Left + 1) * (cells.Bottom - cells.Top + 1) > 4096) { _large.Add(entry); return; }
        for (var x = cells.Left; x <= cells.Right; x++) for (var y = cells.Top; y <= cells.Bottom; y++) {
            if (!_cells.TryGetValue((x, y), out var entries)) _cells[(x, y)] = entries = new List<Entry>();
            entries.Add(entry);
        }
    }
    /// <summary>Reserves an accepted source-to-caption segment so later text cannot cover it.</summary>
    internal void AddLeader(ChartPoint start, ChartPoint end) {
        var shape = new LabelMarkShape(new[] { new List<ChartPoint> { start, end } }, false, 1);
        // A non-null identity keeps this geometry distinct from caption rectangles.
        Add(shape.Bounds, string.Empty, shape);
    }

    /// <summary>Checks a proposed leader against already placed caption boxes, including their required separation.</summary>
    internal bool LeaderIntersectsLabel(ChartPoint start, ChartPoint end, double? labelGap = null) {
        var gap = labelGap ?? _gap;
        var bounds = new ChartRect(Math.Min(start.X, end.X), Math.Min(start.Y, end.Y), Math.Abs(end.X - start.X), Math.Abs(end.Y - start.Y));
        var expanded = Expand(bounds, Math.Max(_gap, Math.Max(gap, _maximumLabelGap)) + .5);
        foreach (var entry in _large) if (BlocksLeader(entry, start, end, gap)) return true;
        var cells = Cells(expanded);
        if ((long)(cells.Right - cells.Left + 1) * (cells.Bottom - cells.Top + 1) > 4096) {
            foreach (var entries in _cells.Values) foreach (var entry in entries) if (BlocksLeader(entry, start, end, gap)) return true;
            return false;
        }
        for (var x = cells.Left; x <= cells.Right; x++) for (var y = cells.Top; y <= cells.Bottom; y++) {
            if (!_cells.TryGetValue((x, y), out var entries)) continue;
            foreach (var entry in entries) if (BlocksLeader(entry, start, end, gap)) return true;
        }
        return false;
    }

    private static bool BlocksLeader(Entry entry, ChartPoint start, ChartPoint end, double gap) {
        if (entry.Id != null) return false;
        var box = Expand(entry.Bounds, Math.Max(gap, entry.Gap) + .5);
        var x1 = start.X; var y1 = start.Y; var x2 = end.X; var y2 = end.Y;
        return ChartPatternLineGeometry.ClipLineToRect(ref x1, ref y1, ref x2, ref y2, box.Left, box.Top, box.Right, box.Bottom);
    }

    public bool Intersects(ChartRect bounds, string? associated, double? labelGap = null, bool allowLabelOverlap = false) {
        var gap = labelGap ?? _gap;
        // A previously accepted label can require more separation than this request.
        // Retrieve those cells conservatively before applying the pair's actual gap.
        var expanded = Expand(bounds, allowLabelOverlap ? _gap : Math.Max(_gap, Math.Max(gap, _maximumLabelGap)));
        foreach (var entry in _large) if (Blocks(entry, bounds, associated, gap, allowLabelOverlap)) return true;
        var cells = Cells(expanded);
        if ((long)(cells.Right - cells.Left + 1) * (cells.Bottom - cells.Top + 1) > 4096) {
            foreach (var entries in _cells.Values) foreach (var entry in entries) if (Blocks(entry, bounds, associated, gap, allowLabelOverlap)) return true;
            return false;
        }
        for (var x = cells.Left; x <= cells.Right; x++) for (var y = cells.Top; y <= cells.Bottom; y++) {
            if (!_cells.TryGetValue((x, y), out var entries)) continue;
            foreach (var entry in entries) if (Blocks(entry, bounds, associated, gap, allowLabelOverlap)) return true;
        }
        return false;
    }
    private bool Blocks(Entry entry, ChartRect original, string? associated, double labelGap, bool allowLabelOverlap) {
        if (entry.Id == null && allowLabelOverlap) return false;
        var expanded = Expand(original, entry.Id == null ? Math.Max(labelGap, entry.Gap) : _gap);
        return !(associated != null && entry.Id == associated && (entry.Shape?.Contains(original) ?? LabelPlacementService.Contains(entry.Bounds, original)))
            && expanded.Left < entry.Bounds.Right && expanded.Right > entry.Bounds.Left && expanded.Top < entry.Bounds.Bottom && expanded.Bottom > entry.Bounds.Top
            && (entry.Shape?.Intersects(expanded) ?? true);
    }
    private static ChartRect Expand(ChartRect bounds, double gap) => new(bounds.X - gap, bounds.Y - gap, bounds.Width + gap * 2, bounds.Height + gap * 2);
    private static (int Left, int Top, int Right, int Bottom) Cells(ChartRect box) => (Cell(box.Left), Cell(box.Top), Cell(box.Right), Cell(box.Bottom));
    private static int Cell(double value) => (int)Math.Max(int.MinValue + 1d, Math.Min(int.MaxValue - 1d, Math.Floor(value / CellSize)));
    private readonly struct Entry {
        public Entry(ChartRect bounds, string? id, LabelMarkShape? shape, double gap) { Bounds = bounds; Id = id; Shape = shape; Gap = gap; }
        public ChartRect Bounds { get; }
        public string? Id { get; }
        public LabelMarkShape? Shape { get; }
        public double Gap { get; }
    }
}
