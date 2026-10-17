using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

/// <summary>Generates statistical, interval and financial chart examples.</summary>
internal static class StatisticalExamples {
    internal static void Write(string output, ChartPngOutputScale outputScale) {
        HorizontalStatisticalExamples.Write(output, outputScale);
        void SaveChart(Chart chart, string name) => ExampleArtifactWriter.SaveChart(chart, output, name, outputScale);
        var latencySpread = Chart.Create()
            .WithTitle("Endpoint Latency Spread")
            .WithSubtitle("Box plots summarize min, quartiles, median, and max")
            .WithXAxis("Probe")
            .WithYAxis("Response time")
            .WithTheme(VisualTheme.Report(), VisualThemeMode.Dark)
            .WithSize(760, 460)
            .WithDataLabels()
            .WithValueFormatter(value => value.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + " ms")
            .WithXLabels("DNS", "TCP", "TLS", "HTTP")
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
        }).WithBoxPlotGroupIds(new[] { new ChartObservationId("dns"), new ChartObservationId("tcp"), new ChartObservationId("tls") })
          .ConfigureBoxPlotStatistics(options => options.WhiskerPolicy = ChartBoxPlotWhiskerPolicy.Iqr);
        SaveChart(Chart.Create().WithTitle("Service latency distributions")
            .WithSubtitle("Quartiles and IQR whiskers retain individual outlier measurements")
            .WithXLabels("DNS", "TCP", "TLS").WithYAxis("Response time (ms)")
            .WithSize(760, 460).WithTheme(VisualTheme.Graphite()).WithDataLabels(false)
            .AddSeries(measurements), "service-latency-iqr-boxplot-light");

        var exposureClusters = Chart.Create()
            .WithTitle("Exposure Clusters")
            .WithSubtitle("Bubble size shows affected assets per cluster")
            .WithXAxis("Reachability")
            .WithYAxis("Exploitability")
            .WithTheme(VisualTheme.Report())
            .WithSize(760, 460)
            .WithDataLabels()
            .AddBubble("Assets", new[] {
                new ChartBubble(1.0, 24, 8),
                new ChartBubble(1.6, 42, 18),
                new ChartBubble(2.3, 31, 42),
                new ChartBubble(3.1, 68, 71),
                new ChartBubble(3.8, 54, 36)
            }, ChartColor.FromRgb(37, 99, 235))
            .ConfigureBubble(options => {
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
            .WithTitle("Observed Remediation Trend")
            .WithSubtitle("Scatter observations with computed least-squares trend line")
            .WithXAxis("Week")
            .WithYAxis("Open findings")
            .WithTheme(VisualTheme.Report())
            .WithSize(760, 460)
            .WithXLabels("W1", "W2", "W3", "W4", "W5", "W6")
            .AddScatter("Observed", remediationTrendPoints, ChartColor.FromRgb(37, 99, 235))
            .AddTrendLine("Trend", remediationTrendPoints, ChartColor.FromRgb(245, 158, 11))
            .AddMeanLine("mean", remediationTrendPoints, ChartColor.FromRgb(14, 165, 233))
            .AddStandardDeviationBand("1 sigma", remediationTrendPoints, 1, ChartColor.FromRgb(96, 165, 250), 0.12);

        SaveChart(remediationTrend, "observed-remediation-trend-light");

        var confidenceIntervals = Chart.Create()
            .WithTitle("Detection Confidence")
            .WithSubtitle("Error bars show observed lower and upper bounds")
            .WithXAxis("Run")
            .WithYAxis("Confidence")
            .WithTheme(VisualTheme.Report(), VisualThemeMode.Dark)
            .WithSize(760, 460)
            .WithDataLabels()
            .WithValueFormatter(value => value.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "%")
            .WithXLabels("Baseline", "Tuning", "Canary", "Rollout", "Steady")
            .AddErrorBar("Confidence", new[] {
                new ChartErrorBar(1, 42, 35, 51),
                new ChartErrorBar(2, 58, 49, 66),
                new ChartErrorBar(3, 63, 54, 78),
                new ChartErrorBar(4, 72, 63, 83),
                new ChartErrorBar(5, 81, 74, 88)
            }, ChartColor.FromRgb(96, 165, 250));

        SaveChart(confidenceIntervals, "detection-confidence-errorbar-dark");

        var signalWindows = Chart.Create()
            .WithTitle("Signal Windows")
            .WithSubtitle("Candlesticks summarize open, high, low, and close values")
            .WithXAxis("Window")
            .WithYAxis("Signal")
            .WithTheme(VisualTheme.Report())
            .WithSize(760, 460)
            .WithDataLabels()
            .WithXLabels("W1", "W2", "W3", "W4", "W5", "W6")
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
            .WithTitle("Signal Windows OHLC")
            .WithSubtitle("OHLC ticks keep dense open/high/low/close windows compact")
            .WithXAxis("Window")
            .WithYAxis("Signal")
            .WithTheme(VisualTheme.Report(), VisualThemeMode.Dark)
            .WithSize(760, 460)
            .WithDataLabels()
            .WithXLabels("W1", "W2", "W3", "W4", "W5", "W6")
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
            .WithTitle("Forecast Envelope")
            .WithSubtitle("Range bands show expected lower and upper bounds")
            .WithXAxis("Run")
            .WithYAxis("Expected Range")
            .WithTheme(VisualTheme.Report(), VisualThemeMode.Dark)
            .WithSize(760, 460)
            .WithDataLabels()
            .WithXLabels("R1", "R2", "R3", "R4", "R5", "R6")
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
            .WithTitle("Forecast Interval")
            .WithSubtitle("Range areas emphasize smoothed prediction intervals beside an observed trend")
            .WithXAxis("Run")
            .WithYAxis("Expected Range")
            .WithTheme(VisualTheme.Report())
            .WithSize(760, 460)
            .WithXLabels("R1", "R2", "R3", "R4", "R5", "R6", "R7")
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

        var remediationLift = Chart.Create()
            .WithTitle("Remediation Lift")
            .WithSubtitle("Dumbbells compare before and after values")
            .WithXAxis("Control")
            .WithYAxis("Coverage")
            .WithTheme(VisualTheme.Report())
            .WithSize(760, 460)
            .WithDataLabels()
            .WithValueFormatter(value => value.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "%")
            .WithXLabels("SPF", "DMARC", "DNSSEC", "MTA-STS", "TLS-RPT")
            .AddDumbbell("Before/after", new[] {
                new ChartDumbbell(1, 32, 44),
                new ChartDumbbell(2, 38, 58),
                new ChartDumbbell(3, 51, 72),
                new ChartDumbbell(4, 43, 67),
                new ChartDumbbell(5, 58, 81)
            }, ChartColor.FromRgb(37, 99, 235));

        SaveChart(remediationLift, "remediation-lift-dumbbell-light");

        var findingsPareto = Chart.Create()
            .WithTitle("Findings Pareto")
            .WithSubtitle("Contribution bars plus cumulative percentage line")
            .WithXAxis("Severity")
            .WithYAxis("Share")
            .WithTheme(VisualTheme.Report(), VisualThemeMode.Dark)
            .WithSize(760, 460)
            .WithDataLabels()
            .WithValueFormatter(value => value.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "%")
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
