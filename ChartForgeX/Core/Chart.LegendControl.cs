using System;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>Adds a legend entry that does not come from a series, listed after the series entries and never toggleable.</summary>
    /// <param name="item">The entry, for example <see cref="ChartLegendItem.Line"/> for a reference line.</param>
    /// <returns>The current chart.</returns>
    public Chart AddLegendItem(ChartLegendItem item) { Options.Legend.Items.Add(item ?? throw new ArgumentNullException(nameof(item))); return this; }

    /// <summary>Sets the key series entries draw and, optionally, the key size for every entry.</summary>
    /// <param name="shape">The key shape; <see cref="ChartLegendKeyShape.Auto"/> keeps the family's own key.</param>
    /// <param name="size">Optional key size from 4 through 24 chart pixels; null keeps the default.</param>
    /// <returns>The current chart.</returns>
    public Chart WithLegendKey(ChartLegendKeyShape shape, double? size = null) {
        var validated = new ChartLegendOptions { KeyShape = shape, KeySize = size };
        Options.Legend.KeyShape = validated.KeyShape; Options.Legend.KeySize = validated.KeySize;
        return this;
    }

    /// <summary>Rewrites legend entry text, for example to append a series total.</summary>
    /// <param name="formatter">A pure formatter; a null result keeps the default label. Pass null to remove it.</param>
    /// <returns>The current chart.</returns>
    public Chart WithLegendFormatter(Func<ChartLegendItemContext, string?>? formatter) { Options.Legend.ItemFormatter = formatter; return this; }
}
