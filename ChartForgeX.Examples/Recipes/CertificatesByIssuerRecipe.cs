/// <summary>Builds certificates issued per month by issuer over the last year, as a certificate report shows it.</summary>
public static class CertificatesByIssuerRecipe {
    /// <summary>Creates stacked monthly issuance; the compact variant keeps the same data.</summary>
    public static Chart Create(string variant = "issuers", ThemeMode mode = ThemeMode.Light) {
        if (variant is not ("issuers" or "compact-issuers")) throw new ArgumentOutOfRangeException(nameof(variant));
        var months = Enumerable.Range(0, 12).Select(month => new DateTime(2025, 11, 1).AddMonths(month).ToString("MMM", CultureInfo.InvariantCulture)).ToArray();
        var chart = Chart.Create().XLabels(months).Bars(b => b.Stacked()).YAxis(y => y.Title("Certificates"))
            .ValueFormat(ChartValueFormat.Number("0", CultureInfo.InvariantCulture));
        var issuers = new[] { ("lets-encrypt", "Let's Encrypt", 64), ("digicert", "DigiCert", 18), ("sectigo", "Sectigo", 11), ("internal-ca", "Internal CA", 7) };
        foreach (var (id, name, typical) in issuers) {
            // Renewals cluster every third month; the values are deterministic so the gallery stays reproducible.
            var values = Enumerable.Range(0, 12).Select(month => new ChartPoint(month + 1, typical + (month % 3 == 0 ? typical / 3 : 0) - month % 4));
            chart.AddSeries(ChartSeries.CreateBar(new ChartSeriesId(id), name, values));
        }
        return chart;
    }
}
