/// <summary>Authored hierarchy values and absolute subtree/root visibility use the incumbent native hierarchy owner.</summary>
internal static class NativeHierarchyMotionRecipe {
    internal static Chart Create(string family, double reading, int width, VisualThemeMode mode, string fontFamily) {
        if (family is not ("treemap" or "sunburst-leaf" or "sunburst-total")) throw new ArgumentOutOfRangeException(nameof(family));
        var authored = family == "sunburst-total";
        var items = new[] { new ChartHierarchyItem("revenue", "Revenue", value: authored ? 120 : null),
            new ChartHierarchyItem("recurring", "Recurring", "revenue", authored ? 80 : null),
            new ChartHierarchyItem("subscriptions", "Subscriptions", "recurring", reading),
            new ChartHierarchyItem("support", "Support", "recurring", 20), new ChartHierarchyItem("services", "Services", "revenue", 25) };
        var id = new ChartSeriesId("revenue"); var series = family == "treemap" ? ChartSeries.CreateTreemap(id, "Revenue", items) : ChartSeries.CreateSunburst(id, "Revenue", items);
        var chart = Chart.Create().WithTitle("Native hierarchy presentation").WithSubtitle("Values, source membership and native roots retain authored identity")
            .WithSize(width, 420).WithTheme(VisualTheme.Graphite().WithFontFamily(fontFamily), mode).WithLegend(true).WithDataLabels(true).AddSeries(series);
        chart.Options.ShowPointLegend = family == "treemap";
        if (family != "treemap") chart.ConfigureSunburst(options => options.ParentValuePolicy = authored ? ChartHierarchyValuePolicy.AuthoredTotal : ChartHierarchyValuePolicy.LeafAggregate);
        return chart;
    }
    internal static ChartVisibilityView View(bool hidden = false, bool subroot = false) => new(hidden
        ? new[] { ChartVisibilityTarget.HierarchyItem(new ChartSeriesId("revenue"), "recurring") } : Array.Empty<ChartVisibilityTarget>(), ChartVisibilityLayout.Reflow,
        ChartVisibilityLabelLayering.PreserveOverlay, subroot ? new ChartHierarchyView("recurring") : null);
}
