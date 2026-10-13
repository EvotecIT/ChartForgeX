using System;
using System.Collections.Generic;

namespace ChartForgeX.VisualBlocks;

/// <summary>
/// A deterministic packet-layout visual block made of contiguous labeled bit fields.
/// </summary>
public sealed class PacketLayoutBlock : VisualBlock<PacketLayoutBlock> {
    private readonly List<PacketLayoutField> _fields = new();
    private int _bitsPerRow = 32;
    private PacketBitOrder _bitOrder;
    private double? _bitWidth;
    private double? _rowHeight;
    private double? _fieldGap;
    private double? _rowGap;

    /// <summary>Gets packet fields in bit order.</summary>
    public IReadOnlyList<PacketLayoutField> Fields => _fields;

    /// <summary>Gets or sets how many bits are rendered per visual row.</summary>
    public int BitsPerRow {
        get => _bitsPerRow;
        set {
            if (value < 1 || value > 512) throw new ArgumentOutOfRangeException(nameof(value), value, "Packet layout bits per row must be between one and five hundred twelve.");
            _bitsPerRow = value;
        }
    }

    /// <summary>Gets or sets whether bit start and end numbers are rendered above fields when they fit the available space.</summary>
    public bool ShowBitNumbers { get; set; } = true;

    /// <summary>Gets or sets the preferred width of one bit in logical pixels; null fills the available row.</summary>
    public double? BitWidth { get => _bitWidth; set => _bitWidth = Dimension(value, 1, nameof(BitWidth)); }

    /// <summary>Gets or sets the preferred field height in logical pixels; null uses automatic row sizing.</summary>
    public double? RowHeight { get => _rowHeight; set => _rowHeight = Dimension(value, 1, nameof(RowHeight)); }

    /// <summary>Gets or sets the gap between fields in logical pixels; null uses the native one-pixel gap.</summary>
    public double? FieldGap { get => _fieldGap; set => _fieldGap = Dimension(value, 0, nameof(FieldGap)); }

    /// <summary>Gets or sets the gap between rows in logical pixels; null uses automatic spacing.</summary>
    public double? RowGap { get => _rowGap; set => _rowGap = Dimension(value, 0, nameof(RowGap)); }

    /// <summary>Gets or sets the visual bit direction within each row without changing field ranges.</summary>
    public PacketBitOrder BitOrder {
        get => _bitOrder;
        set {
            if (value != PacketBitOrder.Ascending && value != PacketBitOrder.Descending)
                throw new ArgumentOutOfRangeException(nameof(value), value, "Packet bit order must be ascending or descending.");
            _bitOrder = value;
        }
    }

    /// <summary>Gets a concise accessibility label.</summary>
    public override string AccessibleName => Options.Accessibility.Name ?? (Title.Length == 0 ? "Packet layout" : Title);

    /// <summary>Creates a packet layout block.</summary>
    public static PacketLayoutBlock Create() => new();

    /// <summary>Adds one contiguous field.</summary>
    public PacketLayoutBlock AddField(int startBit, int endBit, string label) {
        _fields.Add(new PacketLayoutField(startBit, endBit, label));
        return this;
    }

    /// <summary>Sets how many bits are rendered per visual row.</summary>
    public PacketLayoutBlock WithBitsPerRow(int bitsPerRow) { BitsPerRow = bitsPerRow; return this; }

    /// <summary>Sets whether bit start and end numbers are rendered above fields.</summary>
    public PacketLayoutBlock WithBitNumbers(bool visible = true) { ShowBitNumbers = visible; return this; }

    /// <summary>Sets the visual bit direction within each row.</summary>
    public PacketLayoutBlock WithBitOrder(PacketBitOrder order) { BitOrder = order; return this; }

    /// <summary>Sets the preferred bit width; the renderer fits it to the available row width.</summary>
    public PacketLayoutBlock WithBitWidth(double? width) { BitWidth = width; return this; }

    /// <summary>Sets the preferred field height; the renderer fits it to the available row height.</summary>
    public PacketLayoutBlock WithRowHeight(double? height) { RowHeight = height; return this; }

    /// <summary>Sets the preferred gap between fields; each field retains positive width.</summary>
    public PacketLayoutBlock WithFieldGap(double? gap) { FieldGap = gap; return this; }

    /// <summary>Sets the preferred gap between rows; the renderer fits it to the available height.</summary>
    public PacketLayoutBlock WithRowGap(double? gap) { RowGap = gap; return this; }

    private static double? Dimension(double? value, double minimum, string name) {
        if (value.HasValue && (double.IsNaN(value.Value) || double.IsInfinity(value.Value) || value.Value < minimum))
            throw new ArgumentOutOfRangeException(name, value, "Packet layout dimensions must be finite and meet their minimum size.");
        return value;
    }

}

/// <summary>Defines which bit appears at the left of each packet row.</summary>
public enum PacketBitOrder {
    /// <summary>The lowest bit appears at the left.</summary>
    Ascending,
    /// <summary>The highest bit appears at the left.</summary>
    Descending
}

/// <summary>
/// Describes one labeled contiguous field inside a packet layout.
/// </summary>
public sealed class PacketLayoutField {
    private string _label;

    /// <summary>Initializes a packet field.</summary>
    public PacketLayoutField(int startBit, int endBit, string label) {
        if (startBit < 0) throw new ArgumentOutOfRangeException(nameof(startBit), startBit, "Packet field start bit must be zero or greater.");
        if (endBit < startBit) throw new ArgumentOutOfRangeException(nameof(endBit), endBit, "Packet field end bit must be greater than or equal to the start bit.");
        StartBit = startBit;
        EndBit = endBit;
        _label = label ?? throw new ArgumentNullException(nameof(label));
    }

    /// <summary>Gets the first bit covered by this field.</summary>
    public int StartBit { get; }

    /// <summary>Gets the last bit covered by this field.</summary>
    public int EndBit { get; }

    /// <summary>Gets the number of covered bits.</summary>
    public int BitLength => EndBit - StartBit + 1;

    /// <summary>Gets or sets the display label.</summary>
    public string Label { get => _label; set => _label = value ?? throw new ArgumentNullException(nameof(value)); }
}
