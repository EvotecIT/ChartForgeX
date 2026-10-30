using System;
using System.Collections.Generic;
using System.Text;

namespace ChartForgeX.Topology;

/// <summary>What a painted topology cell shows.</summary>
public enum TopologyCellRole {
    /// <summary>Nothing.</summary>
    Empty,
    /// <summary>A group boundary line.</summary>
    GroupBorder,
    /// <summary>A group label on its top boundary.</summary>
    GroupLabel,
    /// <summary>An edge line or junction.</summary>
    Edge,
    /// <summary>An arrowhead.</summary>
    EdgeArrow,
    /// <summary>An edge label.</summary>
    EdgeLabel,
    /// <summary>A node border.</summary>
    NodeBorder,
    /// <summary>The blank inside of a node.</summary>
    NodeFill,
    /// <summary>A node label.</summary>
    NodeLabel,
    /// <summary>A node subtitle or live value.</summary>
    NodeDetail,
    /// <summary>A node status marker.</summary>
    NodeStatus,
    /// <summary>Legend text.</summary>
    LegendText,
    /// <summary>A legend marker.</summary>
    LegendMarker
}

/// <summary>How strongly a painted cell should be drawn.</summary>
public enum TopologyCellEmphasis {
    /// <summary>Ordinary drawing.</summary>
    Normal,
    /// <summary>Part of the active step or highlight.</summary>
    Highlighted,
    /// <summary>Outside the active highlight.</summary>
    Dimmed,
    /// <summary>The focused node.</summary>
    Focused,
    /// <summary>Part of a failed step.</summary>
    Failed
}

/// <summary>The glyphs used to paint a topology.</summary>
public enum TopologyCellGlyphSet {
    /// <summary>Box-drawing characters and arrows.</summary>
    Unicode,
    /// <summary>Plain ASCII for terminals and logs without Unicode.</summary>
    Ascii
}

/// <summary>One painted cell.</summary>
public readonly struct TopologyCell {
    internal TopologyCell(string glyph, int width, TopologyCellRole role, string? elementId, TopologyHealthStatus status, TopologyCellEmphasis emphasis) {
        Glyph = glyph; Width = width; Role = role; ElementId = elementId; Status = status; Emphasis = emphasis;
    }

    /// <summary>Gets the text of the cell. The second cell of a wide character is empty.</summary>
    public string Glyph { get; }
    /// <summary>Gets the display width: 1, 2 for a wide character, or 0 for the cell a wide character covers.</summary>
    public int Width { get; }
    /// <summary>Gets what the cell shows.</summary>
    public TopologyCellRole Role { get; }
    /// <summary>Gets the node, edge, or group id the cell belongs to, or <see langword="null"/>.</summary>
    public string? ElementId { get; }
    /// <summary>Gets the health status of the element.</summary>
    public TopologyHealthStatus Status { get; }
    /// <summary>Gets how strongly the cell should be drawn.</summary>
    public TopologyCellEmphasis Emphasis { get; }
}

/// <summary>Options for painting a cell layout.</summary>
public sealed class TopologyCellPaintOptions {
    /// <summary>Gets or sets the glyph set.</summary>
    public TopologyCellGlyphSet Glyphs { get; set; } = TopologyCellGlyphSet.Unicode;
    /// <summary>Gets node ids to highlight.</summary>
    public HashSet<string> HighlightNodeIds { get; } = new(StringComparer.Ordinal);
    /// <summary>Gets edge ids to highlight.</summary>
    public HashSet<string> HighlightEdgeIds { get; } = new(StringComparer.Ordinal);
    /// <summary>Gets highlighted edge ids travelled from target to source, so their arrow points back.</summary>
    public HashSet<string> ReversedEdgeIds { get; } = new(StringComparer.Ordinal);
    /// <summary>Gets edge ids whose highlight shows a failed call.</summary>
    public HashSet<string> FailedEdgeIds { get; } = new(StringComparer.Ordinal);
    /// <summary>Gets or sets whether elements outside the highlight are dimmed.</summary>
    public bool DimOthers { get; set; } = true;
    /// <summary>Gets or sets the focused node id.</summary>
    public string? FocusNodeId { get; set; }
    /// <summary>Gets live values shown on a node's second line instead of its subtitle.</summary>
    public Dictionary<string, string> NodeValues { get; } = new(StringComparer.Ordinal);
    /// <summary>Gets live health statuses that replace the node's own status.</summary>
    public Dictionary<string, TopologyHealthStatus> NodeStatuses { get; } = new(StringComparer.Ordinal);
    /// <summary>Gets or sets whether a status legend is painted below the diagram.</summary>
    public bool IncludeLegend { get; set; } = true;

    /// <summary>Creates paint options that highlight one resolved scenario step.</summary>
    public static TopologyCellPaintOptions ForStep(TopologyResolvedScenarioStep step) {
        if (step == null) throw new ArgumentNullException(nameof(step));
        var options = new TopologyCellPaintOptions();
        options.HighlightNodeIds.UnionWith(step.NodeIds);
        options.HighlightEdgeIds.UnionWith(step.EdgeIds);
        if (step.TravelsBackward) options.ReversedEdgeIds.UnionWith(step.EdgeIds);
        if (step.IsFailure) options.FailedEdgeIds.UnionWith(step.EdgeIds);
        return options;
    }
}

/// <summary>A painted character grid of a topology, with the role and emphasis of every cell.</summary>
public sealed class TopologyCellCanvas {
    private readonly TopologyCell[] _cells;

    internal TopologyCellCanvas(int columns, int rows) {
        Columns = columns; Rows = rows;
        _cells = new TopologyCell[columns * rows];
        for (var i = 0; i < _cells.Length; i++) _cells[i] = new TopologyCell(" ", 1, TopologyCellRole.Empty, null, TopologyHealthStatus.Unknown, TopologyCellEmphasis.Normal);
    }

    /// <summary>Gets the width in cells.</summary>
    public int Columns { get; }
    /// <summary>Gets the height in cells.</summary>
    public int Rows { get; }

    /// <summary>Gets a painted cell.</summary>
    public TopologyCell this[int column, int row] {
        get {
            if (column < 0 || column >= Columns) throw new ArgumentOutOfRangeException(nameof(column));
            if (row < 0 || row >= Rows) throw new ArgumentOutOfRangeException(nameof(row));
            return _cells[row * Columns + column];
        }
    }

    internal bool InBounds(int column, int row) => column >= 0 && column < Columns && row >= 0 && row < Rows;

    internal void Set(int column, int row, TopologyCell cell) {
        if (!InBounds(column, row)) return;
        var index = row * Columns + column;
        var existing = _cells[index];
        // Overwriting one half of a wide character clears the other half, so rows never shift.
        if (existing.Width == 2 && column + 1 < Columns) _cells[index + 1] = Blank;
        if (existing.Width == 0 && cell.Width != 0 && column > 0 && _cells[index - 1].Width == 2) _cells[index - 1] = Blank;
        _cells[index] = cell;
    }

    private static readonly TopologyCell Blank = new(" ", 1, TopologyCellRole.Empty, null, TopologyHealthStatus.Unknown, TopologyCellEmphasis.Normal);

    internal TopologyCell Get(int column, int row) => _cells[row * Columns + column];

    /// <summary>Returns one row as text without trailing spaces.</summary>
    public string GetRowText(int row) {
        if (row < 0 || row >= Rows) throw new ArgumentOutOfRangeException(nameof(row));
        var builder = new StringBuilder(Columns);
        for (var column = 0; column < Columns; column++) {
            var cell = _cells[row * Columns + column];
            if (cell.Width > 0) builder.Append(cell.Glyph);
        }

        return builder.ToString().TrimEnd();
    }

    /// <summary>Returns the whole grid as text, one line per row, using <c>\n</c> line breaks.</summary>
    public string ToText() {
        var lines = new List<string>(Rows);
        for (var row = 0; row < Rows; row++) lines.Add(GetRowText(row));
        while (lines.Count > 0 && lines[lines.Count - 1].Length == 0) lines.RemoveAt(lines.Count - 1);
        return string.Join("\n", lines);
    }
}
