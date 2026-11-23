using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

/// <summary>Generates statistical, interval and financial chart examples.</summary>
internal static class StatisticalExamples {
    internal static void Write(string output, ChartPngOutputScale outputScale) {
        HorizontalStatisticalExamples.Write(output, outputScale);
        void SaveChart(Chart chart, string name) => ExampleArtifactWriter.SaveChart(chart, output, name, outputScale);
        var latencySpread = Chart.Create()
            .Title("Endpoint Latency Spread")
            .Subtitle("Box plots summarize min, quartiles, median, and max")
            .XAxis(x => x.Title("Probe"))
            .YAxis(y => y.Title("Response time"))
            .Theme(VisualTheme.Graphite()).ThemeMode(ThemeMode.Dark)
            .Size(800, 484)
            .DataLabels()
            .FormatValuesBy(value => value.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + " ms")
            .XLabels("DNS", "TCP", "TLS", "HTTP")
            .AddBoxPlot("Latency", new[] {
                new ChartBoxPlot(1, 18, 24, 31, 38, 48),
                new ChartBoxPlot(2, 42, 56, 64, 82, 104),
                new ChartBoxPlot(3, 86, 102, 118, 146, 188),
                new ChartBoxPlot(4, 112, 128, 146, 184, 228)
            }, ChartColor.FromRgb(96, 165, 250));

        SaveChart(latencySpread, "endpoint-latency-boxplot-dark");

        var measurements = ChartSeries.CreateBoxPlot(new ChartSeriesId("service-measurements"), "Measurements", new[] {
            new ChartBoxPlotSampleGroup(1, new[] { 18d, 20, 22, 24, 26, 28, 30, 32, 34, 95 }),
            new ChartBoxPlotSampleGroup(2, new[] { 26d, 28, 30, 32, 34, 36, 38, 40, 42, 112 }),
            new ChartBoxPlotSampleGroup(3, new[] { 42d, 46, 48, 50, 52, 54, 56, 60, 62, 145 })
        }).BoxPlotGroupIds(new[] { new ChartObservationId("dns"), new ChartObservationId("tcp"), new ChartObservationId("tls") })
          .BoxPlotStatistics(options => options.WhiskerPolicy = ChartBoxPlotWhiskerPolicy.Iqr);
        SaveChart(Chart.Create().Title("Service latency distributions")
            .Subtitle("Quartiles and IQR whiskers retain individual outlier measurements")
            .XLabels("DNS", "TCP", "TLS").YAxis(y => y.Title("Response time (ms)"))
            .Size(800, 484).Theme(VisualTheme.Graphite()).DataLabels(false)
            .AddSeries(measurements), "service-latency-iqr-boxplot-light");

        var exposureClusters = Chart.Create()
            .Title("Exposure Clusters")
            .Subtitle("Bubble size shows affected assets per cluster")
            .XAxis(x => x.Title("Reachability"))
            .YAxis(y => y.Title("Exploitability"))
            .Theme(VisualTheme.Graphite())
            .Size(800, 484)
            .DataLabels()
            .AddBubble("Assets", new[] {
                new ChartBubble(1.0, 24, 8),
                new ChartBubble(1.6, 42, 18),
                new ChartBubble(2.3, 31, 42),
                new ChartBubble(3.1, 68, 71),
                new ChartBubble(3.8, 54, 36)
            }, ChartColor.FromRgb(37, 99, 235))
            .Bubble(options => {
                options.SizeDimension.Name = "Affected assets";
                options.SizeDimension.ValueFormat = ChartValueFormat.Number("0");
                options.SizeDimension.ShowLegend = true;
            });

        SaveChart(exposureClusters, "exposure-clusters-bubble-light");

        var remediationTrendPoints = new[] {
            new ChartPoint(1, 118),
            new ChartPoint(2, 104),
            new ChartPoint(3, 96),
            new ChartPoint(4, 82),
            new ChartPoint(5, 74),
            new ChartPoint(6, 61)
        };
        var remediationTrend = Chart.Create()
            .Title("Observed Remediation Trend")
            .Subtitle("Scatter observations with computed least-squares trend line")
            .XAxis(x => x.Title("Week"))
            .YAxis(y => y.Title("Open findings"))
            .Theme(VisualTheme.Graphite())
            .Size(800, 484)
            .XLabels("W1", "W2", "W3", "W4", "W5", "W6")
            .AddScatter("Observed", remediationTrendPoints, ChartColor.FromRgb(37, 99, 235))
            .AddTrendLine("Trend", remediationTrendPoints, ChartColor.FromRgb(245, 158, 11))
            .AddMeanLine("mean", remediationTrendPoints, ChartColor.FromRgb(14, 165, 233))
            .AddStandardDeviationBand("1 sigma", remediationTrendPoints, 1, ChartColor.FromRgb(96, 165, 250), 0.12);

        SaveChart(remediationTrend, "observed-remediation-trend-light");

        var confidenceIntervals = Chart.Create()
            .Title("Detection Confidence")
            .Subtitle("Error bars show observed lower and upper bounds")
            .XAxis(x => x.Title("Run"))
            .YAxis(y => y.Title("Confidence"))
            .Theme(VisualTheme.Graphite()).ThemeMode(ThemeMode.Dark)
            .Size(800, 484)
            .DataLabels()
            .FormatValuesBy(value => value.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "%")
            .XLabels("Baseline", "Tuning", "Canary", "Rollout", "Steady")
            .AddErrorBar("Confidence", new[] {
                new ChartErrorBar(1, 42, yBounds: new ChartErrorBounds(35, 51)),
                new ChartErrorBar(2, 58, yBounds: new ChartErrorBounds(49, 66)),
                new ChartErrorBar(3, 63, yBounds: new ChartErrorBounds(54, 78)),
                new ChartErrorBar(4, 72, yBounds: new ChartErrorBounds(63, 83)),
                new ChartErrorBar(5, 81, yBounds: new ChartErrorBounds(74, 88))
            }, ChartColor.FromRgb(96, 165, 250));

        SaveChart(confidenceIntervals, "detection-confidence-errorbar-dark");

        var signalWindows = Chart.Create()
            .Title("Signal Windows")
            .Subtitle("Candlesticks summarize open, high, low, and close values")
            .XAxis(x => x.Title("Window"))
            .YAxis(y => y.Title("Signal"))
            .Theme(VisualTheme.Graphite())
            .Size(800, 484)
            .DataLabels()
            .XLabels("W1", "W2", "W3", "W4", "W5", "W6")
            .AddCandlestick("Signal", new[] {
                new ChartCandlestick(1, 42, 51, 35, 48),
                new ChartCandlestick(2, 58, 66, 49, 54),
                new ChartCandlestick(3, 63, 78, 54, 72),
                new ChartCandlestick(4, 72, 84, 67, 76),
                new ChartCandlestick(5, 76, 82, 68, 71),
                new ChartCandlestick(6, 71, 88, 69, 83)
            });

        SaveChart(signalWindows, "signal-windows-candlestick-light");

        var signalOhlc = Chart.Create()
            .Title("Signal Windows OHLC")
            .Subtitle("OHLC ticks keep dense open/high/low/close windows compact")
            .XAxis(x => x.Title("Window"))
            .YAxis(y => y.Title("Signal"))
            .Theme(VisualTheme.Graphite()).ThemeMode(ThemeMode.Dark)
            .Size(800, 484)
            .DataLabels()
            .XLabels("W1", "W2", "W3", "W4", "W5", "W6")
            .AddOhlc("Signal", new[] {
                new ChartCandlestick(1, 42, 51, 35, 48),
                new ChartCandlestick(2, 58, 66, 49, 54),
                new ChartCandlestick(3, 63, 78, 54, 72),
                new ChartCandlestick(4, 72, 84, 67, 76),
                new ChartCandlestick(5, 76, 82, 68, 71),
                new ChartCandlestick(6, 71, 88, 69, 83)
            });

        SaveChart(signalOhlc, "signal-windows-ohlc-dark");

        var forecastEnvelope = Chart.Create()
            .Title("Forecast Envelope")
            .Subtitle("Range bands show expected lower and upper bounds")
            .XAxis(x => x.Title("Run"))
            .YAxis(y => y.Title("Expected Range"))
            .Theme(VisualTheme.Graphite()).ThemeMode(ThemeMode.Dark)
            .Size(800, 484)
            .DataLabels()
            .XLabels("R1", "R2", "R3", "R4", "R5", "R6")
            .AddRangeBand("Expected", new[] {
                new ChartRangeBand(1, 32, 44),
                new ChartRangeBand(2, 38, 58),
                new ChartRangeBand(3, 51, 72),
                new ChartRangeBand(4, 63, 86),
                new ChartRangeBand(5, 68, 92),
                new ChartRangeBand(6, 74, 96)
            }, ChartColor.FromRgb(96, 165, 250));

        SaveChart(forecastEnvelope, "forecast-envelope-rangeband-dark");

        var forecastInterval = Chart.Create()
            .Title("Forecast Interval")
            .Subtitle("Range areas emphasize smoothed prediction intervals beside an observed trend")
            .XAxis(x => x.Title("Run"))
            .YAxis(y => y.Title("Expected Range"))
            .Theme(VisualTheme.Graphite())
            .Size(800, 484)
            .XLabels("R1", "R2", "R3", "R4", "R5", "R6", "R7")
            .AddRangeArea("Prediction interval", new[] {
                new ChartRangeBand(1, 28, 44),
                new ChartRangeBand(2, 34, 55),
                new ChartRangeBand(3, 42, 68),
                new ChartRangeBand(4, 54, 78),
                new ChartRangeBand(5, 61, 88),
                new ChartRangeBand(6, 66, 94),
                new ChartRangeBand(7, 72, 98)
            }, ChartColor.FromRgb(14, 165, 233))
            .AddSmoothLine("Observed", Points(36, 49, 61, 70, 80, 86, 91), ChartColor.FromRgb(37, 99, 235));

        SaveChart(forecastInterval, "forecast-interval-rangearea-light");

        // One series carries actuals and forecast; its stroke turns dashed at the last actual month.
        const double lastActual = 7;
        var demand = ChartSeries.CreateLine(new ChartSeriesId("demand"), "Demand",
                Points(412, 436, 451, 447, 472, 498, 506, 521, 538, 552, 570, 589), ChartColor.FromRgb(37, 99, 235))
            .StrokeStyleFrom(lastActual, new ChartStrokeStyle(dash: new[] { 6d, 5d }));
        var actualForecast = Chart.Create()
            .Title("Actual and Forecast")
            .Subtitle("Demand is solid through July and dashed where the forecast takes over")
            .XAxis(x => x.Title("Month"))
            .YAxis(y => y.Title("Orders"))
            .Theme(VisualTheme.Graphite())
            .Size(800, 484)
            .XLabels("Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec")
            .AddRangeArea("80% interval", new[] {
                new ChartRangeBand(7, 506, 506),
                new ChartRangeBand(8, 509, 533),
                new ChartRangeBand(9, 517, 559),
                new ChartRangeBand(10, 523, 581),
                new ChartRangeBand(11, 533, 607),
                new ChartRangeBand(12, 543, 635)
            }, ChartColor.FromRgb(147, 197, 253))
            .AddSeries(demand);

        SaveChart(actualForecast, "actual-forecast-segmented-line-light");

        var remediationLift = Chart.Create()
            .Title("Remediation Lift")
            .Subtitle("Dumbbells compare before and after values")
            .XAxis(x => x.Title("Control"))
            .YAxis(y => y.Title("Coverage"))
            .Theme(VisualTheme.Graphite())
            .Size(800, 484)
            .DataLabels()
            .FormatValuesBy(value => value.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "%")
            .XLabels("SPF", "DMARC", "DNSSEC", "MTA-STS", "TLS-RPT")
            .AddDumbbell("Before/after", new[] {
                new ChartDumbbell(1, 32, 44),
                new ChartDumbbell(2, 38, 58),
                new ChartDumbbell(3, 51, 72),
                new ChartDumbbell(4, 43, 67),
                new ChartDumbbell(5, 58, 81)
            }, ChartColor.FromRgb(37, 99, 235));

        SaveChart(remediationLift, "remediation-lift-dumbbell-light");

        var findingsPareto = Chart.Create()
            .Title("Findings Pareto")
            .Subtitle("Contribution bars plus cumulative percentage line")
            .XAxis(x => x.Title("Severity"))
            .YAxis(y => y.Title("Share"))
            .Theme(VisualTheme.Graphite()).ThemeMode(ThemeMode.Dark)
            .Size(800, 484)
            .DataLabels()
            .FormatValuesBy(value => value.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "%")
            .AddPareto("Findings", new[] {
                new ChartParetoItem("Critical", 50),
                new ChartParetoItem("High", 28),
                new ChartParetoItem("Medium", 14),
                new ChartParetoItem("Low", 8)
            }, ChartColor.FromRgb(96, 165, 250), ChartColor.FromRgb(251, 191, 36));

        SaveChart(findingsPareto, "findings-pareto-dark");
    }

    private static IEnumerable<ChartPoint> Points(params double[] y) {
        for (var i = 0; i < y.Length; i++) yield return new ChartPoint(i + 1, y[i]);
    }
}
