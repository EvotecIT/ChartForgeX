using System;
using System.Collections.ObjectModel;

namespace ChartForgeX.Core;

/// <summary>Configures legend keys, entry text and custom entries. Placement and density stay on <see cref="ChartOptions"/>.</summary>
public sealed class ChartLegendOptions {
    private ChartLegendKeyShape _keyShape;
    private double? _keySize;

    /// <summary>Gets or sets the key every series entry draws. <see cref="ChartLegendKeyShape.Auto"/> keeps the family's own key.</summary>
    public ChartLegendKeyShape KeyShape {
        get => _keyShape;
        set {
            if (!Enum.IsDefined(typeof(ChartLegendKeyShape), value)) throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown legend key shape.");
            _keyShape = value;
        }
    }

    /// <summary>Gets or sets the key size from 4 through 24 chart pixels, or null for the default ten-pixel key. A line key is 1.4 times as long.</summary>
    public double? KeySize {
        get => _keySize;
        set => _keySize = value.HasValue ? ValidateKeySize(value.Value, nameof(value)) : null;
    }

    /// <summary>Gets or sets a formatter that rewrites entry text. A null result keeps the default label.</summary>
    /// <remarks>The formatter runs during measurement and painting, so it must be a pure function of its context. Value and percentage columns are unchanged.</remarks>
    public Func<ChartLegendItemContext, string?>? ItemFormatter { get; set; }

    /// <summary>Gets the custom entries listed after the series entries.</summary>
    public Collection<ChartLegendItem> Items { get; } = new ItemCollection();

    internal bool HasCustomization => _keyShape != ChartLegendKeyShape.Auto || _keySize.HasValue || ItemFormatter != null || Items.Count > 0;

    /// <summary>Copies keys and items; the formatter is shared.</summary>
    internal ChartLegendOptions Clone() {
        var copy = new ChartLegendOptions { _keyShape = _keyShape, _keySize = _keySize, ItemFormatter = ItemFormatter };
        foreach (var item in Items) copy.Items.Add(item);
        return copy;
    }

    internal static double ValidateKeySize(double size, string parameterName) {
        ChartGuards.Finite(size, parameterName);
        if (size < 4 || size > 24) throw new ArgumentOutOfRangeException(parameterName, size, "Legend key size must be from 4 through 24 chart pixels.");
        return size;
    }

    private sealed class ItemCollection : Collection<ChartLegendItem> {
        protected override void InsertItem(int index, ChartLegendItem item) => base.InsertItem(index, item ?? throw new ArgumentNullException(nameof(item)));
        protected override void SetItem(int index, ChartLegendItem item) => base.SetItem(index, item ?? throw new ArgumentNullException(nameof(item)));
    }
}
