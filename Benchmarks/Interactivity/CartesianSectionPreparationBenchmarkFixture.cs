using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;

/// <summary>Measures current-only Y/XY preparation and validates aligned native domains without historical API probes.</summary>
public sealed class CartesianSectionPreparationBenchmarkFixture : SectionPreparationBenchmarkFixture {
    private readonly ChartCartesianViewport? _viewport;

    /// <summary>Creates the current fixture with typed source windows outside timing.</summary>
    public CartesianSectionPreparationBenchmarkFixture(string caseName, string tokenPath, string fontPath, string boldFontPath)
        : base(caseName, tokenPath, fontPath, boldFontPath, cartesian: true) {
        _viewport = Window switch {
            "y" => new ChartCartesianViewport(y: new ChartAxisWindow(12, 24)),
            "xy" => new ChartCartesianViewport(new ChartAxisWindow(2, 10), new ChartAxisWindow(12, 24)),
            _ => null
        };
    }

    /// <summary>Uses the current Y/XY API only for the explicitly labeled new-capability cases.</summary>
    protected override HtmlChartSection PrepareSection() => _viewport == null ? base.PrepareSection()
        : Renderer.PrepareSection(SourceChart, _viewport, ConfigureAdaptive);

    /// <summary>Requires aligned full domains and matching source/inverse mappings in both native drawings.</summary>
    protected override void ValidateSection(HtmlChartSection section) {
        base.ValidateSection(section);
        var wide = section.PreparedVisual; var narrow = section.NarrowPreparedVisual!;
        var x = wide.XProjection!; var narrowX = narrow.XProjection!;
        var y = wide.YProjection ?? throw new InvalidOperationException("Current preparation omitted the primary Y projection.");
        var narrowY = narrow.YProjection ?? throw new InvalidOperationException("Current narrow preparation omitted the primary Y projection.");
        if (x.FullMinimum != narrowX.FullMinimum || x.FullMaximum != narrowX.FullMaximum
            || y.FullMinimum != narrowY.FullMinimum || y.FullMaximum != narrowY.FullMaximum
            || (Window is "y" or "xy") && (y.Minimum != 12 || y.Maximum != 24 || narrowY.Minimum != 12 || narrowY.Maximum != 24)
            || Math.Abs(x.Unproject(x.Project(6)) - 6) > 1e-8 || Math.Abs(narrowX.Unproject(narrowX.Project(6)) - 6) > 1e-8
            || Math.Abs(y.Unproject(y.Project(20)) - 20) > 1e-8 || Math.Abs(narrowY.Unproject(narrowY.Project(20)) - 20) > 1e-8)
            throw new InvalidOperationException("Current adaptive preparation changed aligned full domains, source windows or inverse mappings.");
    }

    /// <summary>Retains actual Y domains alongside the shared projection receipt.</summary>
    protected override object Receipt(HtmlChartSection section) => new {
        shared = base.Receipt(section), wideY = section.PreparedVisual.YProjection,
        narrowY = section.NarrowPreparedVisual!.YProjection
    };
}
