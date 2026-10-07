using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

/// <summary>Indexes occupied text and mark boxes in logical-pixel cells.</summary>
internal sealed class LabelSpatialIndex {
    private const double CellSize = 64;
    private readonly double _gap;
    private readonly Dictionary<(int X, int Y), List<Entry>> _cells = new();
    private readonly List<Entry> _large = new();
    public LabelSpatialIndex(double gap) { _gap = gap; }
    public void Add(ChartRect bounds, string? id, LabelMarkShape? shape = null) {
        var entry = new Entry(bounds, id, shape);
        var cells = Cells(bounds);
        if ((long)(cells.Right - cells.Left + 1) * (cells.Bottom - cells.Top + 1) > 4096) { _large.Add(entry); return; }
        for (var x = cells.Left; x <= cells.Right; x++) for (var y = cells.Top; y <= cells.Bottom; y++) {
            if (!_cells.TryGetValue((x, y), out var entries)) _cells[(x, y)] = entries = new List<Entry>();
            entries.Add(entry);
        }
    }
    public bool Intersects(ChartRect bounds, string? associated) {
        var expanded = new ChartRect(bounds.X - _gap, bounds.Y - _gap, bounds.Width + _gap * 2, bounds.Height + _gap * 2);
        foreach (var entry in _large) if (Blocks(entry, bounds, expanded, associated)) return true;
        var cells = Cells(expanded);
        if ((long)(cells.Right - cells.Left + 1) * (cells.Bottom - cells.Top + 1) > 4096) {
            foreach (var entries in _cells.Values) foreach (var entry in entries) if (Blocks(entry, bounds, expanded, associated)) return true;
            return false;
        }
        for (var x = cells.Left; x <= cells.Right; x++) for (var y = cells.Top; y <= cells.Bottom; y++) {
            if (!_cells.TryGetValue((x, y), out var entries)) continue;
            foreach (var entry in entries) if (Blocks(entry, bounds, expanded, associated)) return true;
        }
        return false;
    }
    private static bool Blocks(Entry entry, ChartRect original, ChartRect expanded, string? associated) =>
        !(associated != null && entry.Id == associated && (entry.Shape?.Contains(original) ?? LabelPlacementService.Contains(entry.Bounds, original)))
        && expanded.Left < entry.Bounds.Right && expanded.Right > entry.Bounds.Left && expanded.Top < entry.Bounds.Bottom && expanded.Bottom > entry.Bounds.Top
        && (entry.Shape?.Intersects(expanded) ?? true);
    private static (int Left, int Top, int Right, int Bottom) Cells(ChartRect box) => (Cell(box.Left), Cell(box.Top), Cell(box.Right), Cell(box.Bottom));
    private static int Cell(double value) => (int)Math.Max(int.MinValue + 1d, Math.Min(int.MaxValue - 1d, Math.Floor(value / CellSize)));
    private readonly struct Entry {
        public Entry(ChartRect bounds, string? id, LabelMarkShape? shape) { Bounds = bounds; Id = id; Shape = shape; }
        public ChartRect Bounds { get; }
        public string? Id { get; }
        public LabelMarkShape? Shape { get; }
    }
}
