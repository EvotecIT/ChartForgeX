using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.VisualBlocks;

/// <summary>
/// A deterministic cause-and-effect fishbone diagram.
/// </summary>
public sealed class FishboneDiagramBlock : VisualBlock<FishboneDiagramBlock> {
    private readonly List<FishboneCause> _causes = new();
    private string _effect = string.Empty;

    /// <summary>Gets or sets the effect shown at the fishbone head.</summary>
    public string Effect { get => _effect; set => _effect = value ?? throw new ArgumentNullException(nameof(value)); }

    internal bool UsesNaturalLayout { get; private set; }

    /// <summary>Measures a complete, wrapped cause tree at native typography before raster allocation.</summary>
    /// <remarks>The ordinary fixed layout remains unchanged until WithNaturalSize is called.</remarks>
    public ChartSize MeasureNaturalSize() => VisualBlockRendering.MeasureFishboneNatural(this).Size;

    /// <summary>Selects measured parent-connected cause lanes and sets their complete natural frame.</summary>
    /// <remarks>Call again after changing causes, padding or typography. A smaller subsequent explicit
    /// frame is rejected rather than dropping captions or reducing every label to a tiny font.</remarks>
    public FishboneDiagramBlock WithNaturalSize(int minimumWidth = 0, int minimumHeight = 0) {
        if (minimumWidth < 0) throw new ArgumentOutOfRangeException(nameof(minimumWidth));
        if (minimumHeight < 0) throw new ArgumentOutOfRangeException(nameof(minimumHeight));
        var size = MeasureNaturalSize();
        Options.Size = new ChartSize(Math.Max(size.Width, minimumWidth), Math.Max(size.Height, minimumHeight));
        UsesNaturalLayout = true; return this;
    }

    /// <summary>Gets top-level causes in source/render order.</summary>
    public IReadOnlyList<FishboneCause> Causes => _causes;

    /// <summary>Gets a concise accessibility label.</summary>
    public override string AccessibleName => Options.Accessibility.Name ?? (Title.Length == 0 ? (Effect.Length == 0 ? "Fishbone diagram" : Effect) : Title);

    /// <summary>Creates a fishbone diagram.</summary>
    public static FishboneDiagramBlock Create(string effect) => new() { Effect = effect };

    /// <summary>Adds a top-level cause.</summary>
    public FishboneCause AddCause(string label) {
        var cause = new FishboneCause(label);
        _causes.Add(cause);
        return cause;
    }
}

/// <summary>
/// Describes a cause or nested sub-cause in a fishbone diagram.
/// </summary>
public sealed class FishboneCause {
    private readonly List<FishboneCause> _children = new();
    private string _label;

    /// <summary>Initializes a cause.</summary>
    public FishboneCause(string label) {
        _label = label ?? throw new ArgumentNullException(nameof(label));
    }

    /// <summary>Gets or sets the display label.</summary>
    public string Label { get => _label; set => _label = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets nested sub-causes.</summary>
    public IReadOnlyList<FishboneCause> Children => _children;

    /// <summary>Adds a nested sub-cause.</summary>
    public FishboneCause AddChild(string label) {
        var child = new FishboneCause(label);
        _children.Add(child);
        return child;
    }
}
