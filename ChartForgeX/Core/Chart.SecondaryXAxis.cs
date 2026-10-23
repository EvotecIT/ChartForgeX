using System;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    private string _secondaryXAxisTitle = string.Empty;
    /// <summary>Gets or sets the secondary horizontal value-axis title.</summary>
    public string SecondaryXAxisTitle {
        get => _secondaryXAxisTitle;
        set => _secondaryXAxisTitle = value ?? throw new ArgumentNullException(nameof(value));
    }
    /// <summary>Configures the secondary horizontal value axis transactionally.</summary>
    public Chart ConfigureSecondaryXAxis(Action<ChartAxis> configure) {
        if (configure == null) throw new ArgumentNullException(nameof(configure));
        Options.SecondaryXAxis = ChartOptionConfiguration.Configure(Options.SecondaryXAxis, configure, axis => axis.Clone());
        return this;
    }
    /// <summary>Sets the secondary horizontal title and optional tick formatter.</summary>
    public Chart WithSecondaryXAxis(string title, Func<double, string>? formatter = null) {
        SecondaryXAxisTitle = title; Options.SecondaryXAxis.LabelFormatter = formatter; return this;
    }
    /// <summary>Sets the secondary horizontal value scale.</summary>
    public Chart WithSecondaryXAxisScale(ChartScaleKind scale) => ConfigureSecondaryXAxis(axis => axis.Scale = scale);
    /// <summary>Sets explicit secondary horizontal value bounds.</summary>
    public Chart WithSecondaryXAxisBounds(double minimum, double maximum) {
        Options.SecondaryXAxis.WithBounds(minimum, maximum); return this;
    }
    /// <summary>Restores automatic secondary horizontal value bounds.</summary>
    public Chart WithAutomaticSecondaryXAxisBounds() {
        Options.SecondaryXAxis.WithAutomaticBounds(); return this;
    }
}
