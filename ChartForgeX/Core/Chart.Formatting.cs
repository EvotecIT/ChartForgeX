using System;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>Sets the shared numeric display policy used by value labels, totals and numeric vertical axes.</summary>
    public Chart WithValueFormat(ChartValueFormat format) {
        Options.ValueFormat = format ?? throw new ArgumentNullException(nameof(format));
        return this;
    }
}

public sealed partial class ChartOptions {
    private ChartValueFormat _valueFormat = ChartValueFormat.ExistingValue;

    /// <summary>Gets or sets the shared numeric display policy. Existing chart defaults retain grouped values below ten thousand.</summary>
    public ChartValueFormat ValueFormat {
        get => _valueFormat;
        set => _valueFormat = value ?? throw new ArgumentNullException(nameof(value));
    }
}
