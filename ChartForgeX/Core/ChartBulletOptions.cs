using System;

namespace ChartForgeX.Core;

/// <summary>Controls shared or independent numeric bullet axes and the direction of their tracks.</summary>
public sealed class ChartBulletOptions {
    private ChartOrientation _orientation = ChartOrientation.Horizontal;
    /// <summary>Gets or sets track direction. Horizontal increases to the right; vertical increases upward before axis reversal.</summary>
    public ChartOrientation Orientation {
        get => _orientation;
        set { if (!Enum.IsDefined(typeof(ChartOrientation), value)) throw new ArgumentOutOfRangeException(nameof(value)); _orientation = value; }
    }
    /// <summary>Gets or sets whether every row uses its authored reading domain and optional local axis. False retains the shared displayed axis.</summary>
    public bool IndependentAxes { get; set; }
    internal ChartBulletOptions Clone() => (ChartBulletOptions)MemberwiseClone();
}

public sealed partial class ChartOptions {
    /// <summary>Gets bullet track orientation and numeric axis ownership.</summary>
    public ChartBulletOptions Bullet { get; internal set; } = new();
}

public sealed partial class Chart {
    /// <summary>Configures bullet layout atomically, without changing readings, targets or qualitative boundaries.</summary>
    public Chart ConfigureBullet(Action<ChartBulletOptions> configure) {
        Options.Bullet = ChartOptionConfiguration.Configure(Options.Bullet, configure, options => options.Clone());
        return this;
    }
}
