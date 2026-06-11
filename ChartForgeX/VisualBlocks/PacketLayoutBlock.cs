using System;
using System.Collections.Generic;

namespace ChartForgeX.VisualBlocks;

/// <summary>
/// A deterministic packet-layout visual block made of contiguous labeled bit fields.
/// </summary>
public sealed class PacketLayoutBlock : VisualBlock<PacketLayoutBlock> {
    private readonly List<PacketLayoutField> _fields = new();
    private int _bitsPerRow = 32;

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

    /// <summary>Gets or sets whether bit start and end numbers are rendered above fields.</summary>
    public bool ShowBitNumbers { get; set; } = true;

    /// <summary>Gets a concise accessibility label.</summary>
    public override string AccessibleName => Title.Length == 0 ? "Packet layout" : Title;

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
