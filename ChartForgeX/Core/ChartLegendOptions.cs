using System;
using System.Collections.ObjectModel;

namespace ChartForgeX.Core;

/// <summary>Configures legend keys, entry text and custom entries. Placement and density stay on <see cref="ChartOptions"/>.</summary>
[PowerForge.Fluent.Fluent(Copy = nameof(Clone))]
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
    [PowerForge.Fluent.Fluent("LabelBy")]
public Func<ChartLegendItemContext, string?>? ItemFormatter { get; set; }

    /// <summary>Gets the custom entries listed after the series entries.</summary>
    public Collection<ChartLegendItem> Items => _items;
    private ItemCollection _items = new();

    internal bool HasCustomization => _keyShape != ChartLegendKeyShape.Auto || _keySize.HasValue || ItemFormatter != null || Items.Count > 0;

    /// <summary>Copies every setting and the custom items; the formatter is shared.</summary>
    internal ChartLegendOptions Clone() {
        var copy = (ChartLegendOptions)MemberwiseClone();
        copy._items = new ItemCollection();
        foreach (var item in Items) copy._items.Add(item);
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
    private ChartLegendPosition _legendPosition = ChartLegendPosition.TopLeft;

    private bool _showLegend = true;


    /// <summary>
    /// Gets or sets whether the legend is enabled. A default legend with one entry is hidden;
    /// explicitly assigning true also displays a single entry.
    /// </summary>
    public bool Visible {
        get => _showLegend;
        set { _showLegend = value; HasExplicitVisibility = true; }
    }


    internal bool HasExplicitVisibility { get; private set; }


    /// <summary>
    /// Gets or sets a value indicating whether capable legends should list individual points instead of series.
    /// </summary>
    public bool ShowPoints { get; set; }


    /// <summary>
    /// Gets or sets where the legend is placed relative to the plot area.
    /// </summary>
    public ChartLegendPosition Position {
        get => _legendPosition;
        set {
            if (!Enum.IsDefined(typeof(ChartLegendPosition), value)) throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown legend position.");
            _legendPosition = value;
            HasExplicitPosition = true;
        }
    }


    internal bool HasExplicitPosition { get; private set; }

    internal void SetDefaultPosition(ChartLegendPosition position) { if (!HasExplicitPosition) _legendPosition = position; }

    private double _legendMaximumHeightFraction = 0.35;

    private int? _legendMaximumRows;


    /// <summary>Gets or sets the fraction of chart height available to a legend, including its spacing.</summary>
    /// <remarks>Defaults to 35 percent. At least one row is retained when the budget can fit a readable row; smaller budgets omit the legend while leaving plotted data unchanged.</remarks>
    public double MaxHeightFraction {
        get => _legendMaximumHeightFraction;
        set {
            if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0 || value > 1) throw new ArgumentOutOfRangeException(nameof(value), value, "Legend height fraction must be greater than zero and at most one.");
            _legendMaximumHeightFraction = value;
            HasExplicitBudget = true;
        }
    }


    /// <summary>Gets or sets an additional row limit, including the overflow summary row. Null uses the height budget alone.</summary>
    public int? MaxRows {
        get => _legendMaximumRows;
        set {
            if (value.HasValue && value.Value < 1) throw new ArgumentOutOfRangeException(nameof(value), value, "Legend row limit must be greater than zero.");
            _legendMaximumRows = value;
            HasExplicitBudget = true;
        }
    }

    internal bool HasExplicitBudget { get; private set; }
}
