/// <summary>Builds a lollipop comparison of orders at each delivery stage.</summary>
public static class LollipopRecipe {
    /// <summary>Creates the selected lollipop example.</summary>
    public static Chart Create(string variant = "wide", VisualThemeMode mode = VisualThemeMode.Light) {
        if (variant is not ("wide" or "compact" or "sparse" or "options" or "compact-options"))
            throw new ArgumentOutOfRangeException(nameof(variant));
        var points = Enumerable.Range(1, 6).Where(index => variant != "sparse" || index is 1 or 3 or 6)
            .Select(index => new ChartPoint(index, 20 + index * 7 % 29, variant == "sparse" && index == 6));
        var orders = new ChartSeries(new ChartSeriesId("orders"), "Orders", ChartSeriesKind.Lollipop, points);
        return Chart.Create().WithXLabels("Received", "Packed", "Shipped", "In transit", "Delivered", "Reviewed").AddSeries(orders)
            .WithValueFormat(ChartValueFormat.Number("0.##", CultureInfo.InvariantCulture));
    }
}
