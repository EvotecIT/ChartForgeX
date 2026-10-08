using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ChartForgeX.Terminal;

/// <summary>
/// Specifies alignment for a terminal table column.
/// </summary>
public enum TerminalColumnAlignment {
    /// <summary>Align values to the left.</summary>
    Left,
    /// <summary>Align values to the right.</summary>
    Right
}

/// <summary>
/// Models a compact monospace table within a terminal story.
/// </summary>
public sealed class TerminalTable {
    private readonly List<string> _columns = new();
    private readonly List<TerminalColumnAlignment> _alignments = new();
    private readonly List<IReadOnlyList<string>> _rows = new();

    /// <summary>Gets the column headings.</summary>
    public IReadOnlyList<string> Columns => _columns;

    /// <summary>Gets column alignments.</summary>
    public IReadOnlyList<TerminalColumnAlignment> Alignments => _alignments;

    /// <summary>Gets table rows.</summary>
    public IReadOnlyList<IReadOnlyList<string>> Rows => _rows;

    /// <summary>Creates an empty terminal table.</summary>
    public static TerminalTable Create() => new();

    /// <summary>Sets table columns.</summary>
    public TerminalTable WithColumns(params string[] columns) {
        if (columns == null) throw new ArgumentNullException(nameof(columns));
        if (columns.Length == 0 || columns.Length > 8) throw new ArgumentOutOfRangeException(nameof(columns), "Terminal tables require between one and eight columns.");
        var normalized = columns.Select((value, index) => Normalize(value, "Column " + index)).ToArray();
        _columns.Clear();
        _columns.AddRange(normalized);
        _alignments.Clear();
        for (var index = 0; index < normalized.Length; index++) _alignments.Add(TerminalColumnAlignment.Left);
        _rows.Clear();
        return this;
    }

    /// <summary>Sets one column alignment.</summary>
    public TerminalTable AlignColumn(int index, TerminalColumnAlignment alignment) {
        if (index < 0 || index >= _columns.Count) throw new ArgumentOutOfRangeException(nameof(index));
        if (!Enum.IsDefined(typeof(TerminalColumnAlignment), alignment)) throw new ArgumentOutOfRangeException(nameof(alignment));
        _alignments[index] = alignment;
        return this;
    }

    /// <summary>Adds one table row.</summary>
    public TerminalTable AddRow(params object[] values) {
        if (values == null) throw new ArgumentNullException(nameof(values));
        if (_columns.Count == 0) throw new InvalidOperationException("Configure terminal table columns before adding rows.");
        if (values.Length != _columns.Count) throw new ArgumentException("Terminal table rows must match the configured column count.", nameof(values));
        if (_rows.Count >= 40) throw new InvalidOperationException("Terminal tables support at most 40 rows.");
        _rows.Add(values.Select((value, index) => Normalize(InvariantText(value), "Value " + index)).ToArray());
        return this;
    }

    internal void Validate() {
        if (_columns.Count == 0) throw new InvalidOperationException("Terminal tables require at least one column.");
    }

    private static string Normalize(string value, string label) {
        return TerminalTextSanitizer.OneLine(value, label, " ", allowEmpty: true);
    }

    private static string InvariantText(object? value) {
        if (value == null) return string.Empty;
        if (value is IFormattable formattable) return formattable.ToString(null, CultureInfo.InvariantCulture) ?? string.Empty;
        return value.ToString() ?? string.Empty;
    }
}
