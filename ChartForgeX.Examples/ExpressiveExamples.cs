using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
internal static partial class ExpressiveExamples {
    public static void Write(string output, ChartPngOutputScale pngOutputScale) {
        StyledMultilingualTextExample.Write(output);
        SaveGrid(CreateThemeShowcaseGrid(), output, "theme-font-showcase-grid", pngOutputScale);
        SaveGrid(CreatePaletteSwatchGrid(), output, "palette-swatch-showcase-grid", pngOutputScale);
        SaveGrid(CreatePictorialSymbolShowcaseGrid(), output, "pictorial-symbol-showcase-grid", pngOutputScale);
        SaveGrid(CreatePictorialIsotypeShowcaseGrid(), output, "pictorial-isotype-showcase-grid", pngOutputScale);
        SaveGrid(CreatePeopleInfographicShowcaseGrid(), output, "people-infographic-showcase-grid", pngOutputScale);
        SaveGrid(CreateWordCloudControlShowcaseGrid(), output, "word-cloud-control-showcase-grid", pngOutputScale);
        SaveGrid(CreateDataLabelPlacementShowcaseGrid(), output, "data-label-placement-showcase-grid", pngOutputScale);
        SaveGrid(CreateMarkSurfaceShowcase(), output, "chart-mark-surfaces-showcase-grid", pngOutputScale);
        SaveGrid(CreateFunnelSurfaceShowcase(), output, "funnel-surfaces-showcase-grid", pngOutputScale);
        SaveGrid(CreatePointColorCustomizationGrid(), output, "point-color-customization-showcase-grid", pngOutputScale);
        SaveChart(CreateDashboardSegmentedColumnPreview(), output, "dashboard-segmented-column-style", pngOutputScale);
        SaveChart(CreateDashboardSegmentedHorizontalPreview(), output, "dashboard-segmented-horizontal-style", pngOutputScale);
        SaveChart(DashboardPremiumTrendRecipe.Create(), output, "dashboard-premium-trend-style", pngOutputScale);
        SaveChart(CreateTextStyleShowcase(), output, "text-style-showcase-editorial", pngOutputScale);
        SaveChart(CreateFontLayoutShowcase(), output, "font-layout-showcase", pngOutputScale);
        SaveChart(CreateFallbackFontShowcase(), output, "fallback-font-showcase", pngOutputScale);
        SaveChart(CreateScriptShapingShowcase(), output, "script-shaping-showcase", pngOutputScale);
        SaveChart(CreateSinhalaMyanmarShowcase(), output, "sinhala-myanmar-showcase", pngOutputScale);
        SaveChart(CreateLanguageFormsShowcase(), output, "language-forms-showcase", pngOutputScale);
        SaveChart(CreateSmallTextLayoutShowcase(), output, "small-text-layout-showcase", pngOutputScale);
        SaveChart(CreateVariableFontShowcase(), output, "variable-font-showcase", pngOutputScale);
        SaveChart(CreateColourEmojiShowcase(), output, "colour-emoji-showcase", pngOutputScale);
        SaveChart(CreateFontPaletteShowcase(), output, "font-palette-showcase", pngOutputScale);
        // Keep 12px labels at the authored 12px strike size in this small-text example.
        SaveChart(CreateEmbeddedBitmapShowcase(), output, "font-bitmap-showcase", ChartPngOutputScale.Standard);
        SaveChart(CreateControlPartition(), output, "control-partition-sunburst-aurora", pngOutputScale);
        SaveChart(CreateAudiencePictorial(), output, "audience-pictorial-candy", pngOutputScale);
        SaveChart(CreateSupportThemesWordCloud(), output, "support-themes-word-cloud-editorial", pngOutputScale);
    }

    private static Chart CreateDashboardSegmentedColumnPreview() => Chart.Create()
        .ShowHeader(false)
        .TransparentBackground(false)
        .DashboardBarPanelStyle()
        .Legend(false)
        .Padding(42, 26, 24, 54)
        .Theme(VisualTheme.Graphite())
        .Size(800, 400)
        .XLabels("Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday")
        .Bars(b => b.Stacked())
        .YAxis(y => y.Bounds(0, 120))
        .FocusXAxisCategory(4, paletteIndex: 3)
        .AddBar("Applied", Points(20, 25, 31, 37, 20, 31, 20))
        .AddBar("Screened", Points(30, 21, 12, 18, 18, 27, 21))
        .AddBar("Saved", Points(39, 25, 20, 25, 11, 20, 35))
        .AddBar("Outreach", Points(21, 24, 18, 41, 21, 15, 25));

    private static Chart CreateDashboardSegmentedHorizontalPreview() => Chart.Create()
        .ShowHeader(false)
        .TransparentBackground(false)
        .Theme(VisualTheme.Graphite())
        .DashboardBarPanelStyle()
        .Legend(false)
        .Padding(160, 34, 26, 34)
        .Size(800, 191)
        .XLabels("Software engineer", "Product designer", "Project manager", "Finance")
        .Bars(b => b.Stacked())
        .XAxis(x => x.Bounds(0, 100).Visible(false))
        .AddHorizontalBar("Applied", Points(24, 18, 20, 28))
        .AddHorizontalBar("Screened", Points(28, 34, 24, 22))
        .AddHorizontalBar("Saved", Points(30, 25, 36, 24))
        .AddHorizontalBar("Outreach", Points(18, 23, 20, 26));



    private static ChartGrid CreatePictorialSymbolShowcaseGrid() {
        var shapes = new[] {
            ChartPictorialShape.Circle,
            ChartPictorialShape.Square,
            ChartPictorialShape.Diamond,
            ChartPictorialShape.Triangle,
            ChartPictorialShape.Star,
            ChartPictorialShape.Heart,
            ChartPictorialShape.Shield,
            ChartPictorialShape.Check,
            ChartPictorialShape.Person,
            ChartPictorialShape.PersonDress
        };
        var palette = ChartPalettes.Pastel;
        var grid = ChartGrid.Create()
            .Title("Pictorial Symbol Showcase")
            .Subtitle("Built-in pictorial symbols for scorecards, ratings, audience mixes, and friendly summaries")
            .Theme(VisualTheme.Graphite().Configure(builder => { builder.CardRadius = 8; builder.PlotRadius = builder.BarRadius = 4; }))
            .Columns(3)
            .Padding(24)
            .PanelSize(360, 190);
        for (var i = 0; i < shapes.Length; i++) {
            grid.Add(CreatePictorialShapePreview(shapes[i], palette[i % palette.Length]));
        }

        return grid;
    }

    private static Chart CreatePictorialShapePreview(ChartPictorialShape shape, ChartColor color) => Chart.Create()
        .Title(shape.ToString())
        .Subtitle("Built-in symbol")
        .Theme(VisualTheme.Graphite().Configure(builder => { builder.CardRadius = 8; builder.PlotRadius = builder.BarRadius = 4; }))
        .Size(360, 190)
        .Legend(false).ShowPlotBackground(false)
        .Padding(44, 30, 24, 34)
        .FormatValuesBy(value => value.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + "/5")
        .Pictorial(p => p.Columns(5).Max(5).SymbolScale(1.36))
        .AddPictorial("Shape", new[] { new ChartPictorialItem(shape.ToString(), 4.5, color) }, shape);

    private static ChartGrid CreatePictorialIsotypeShowcaseGrid() {
        return ChartGrid.Create()
            .Title("Pictorial Isotype Showcase")
            .Subtitle("One-icon-per-unit rows and population-scale icon rows inspired by classic Isotype charts")
            .Theme(VisualTheme.Graphite())
            .Columns(2)
            .Padding(28)
            .PanelSize(600, 391)
            .Add(CreateSimplePeopleIsotype())
            .Add(CreatePopulationPeopleIsotype());
    }

    private static Chart CreateSimplePeopleIsotype() => Chart.Create()
        .Title("Audience Groups")
        .Subtitle("Each person icon represents one respondent")
        .Theme(VisualTheme.Graphite())
        .Size(600, 391)
        .Pictorial(p => p.Columns(25).ValuePerSymbol(1).ShowValues(false))
        .AddPictorial("Groups", new[] {
            new ChartPictorialItem("A", 24, ChartColor.FromRgb(220, 38, 38)),
            new ChartPictorialItem("B", 4, ChartColor.FromRgb(220, 38, 38)),
            new ChartPictorialItem("C", 1, ChartColor.FromRgb(220, 38, 38))
        }, ChartPictorialShape.Person);

    private static Chart CreatePopulationPeopleIsotype() => Chart.Create()
        .Title("Population of Barrie")
        .Subtitle("Each person icon represents 5,000 residents")
        .Theme(VisualTheme.Graphite())
        .Size(600, 391)
        .FormatValuesBy(value => value.ToString("N0", System.Globalization.CultureInfo.InvariantCulture))
        .Pictorial(p => p.Columns(26).ValuePerSymbol(5000).ShowValues(false))
        .AddPictorial("Population", new[] {
            new ChartPictorialItem("1981", 38900, ChartColor.FromRgb(14, 165, 233)),
            new ChartPictorialItem("1991", 62000, ChartColor.FromRgb(239, 68, 68)),
            new ChartPictorialItem("1996", 82800, ChartColor.FromRgb(14, 165, 233)),
            new ChartPictorialItem("2001", 103700, ChartColor.FromRgb(239, 68, 68)),
            new ChartPictorialItem("2006", 128400, ChartColor.FromRgb(14, 165, 233))
        }, ChartPictorialShape.Person);

    private static ChartGrid CreatePeopleInfographicShowcaseGrid() {
        var brand = VisualTheme.Graphite();
        const int panelWidth = 420;
        const int panelHeight = 320;
        const int gap = 18;
        const int widePanelWidth = panelWidth * 2 + gap;
        return ChartGrid.Create()
            .Title("People Infographic Showcase")
            .Subtitle("Demographic panels using pictorial people, donut split, rings, horizontal bars, and trend lines")
            .TitleStyle(style => style.Color(ChartColor.FromHex("#0F172A")).FontWeight("900"))
            .SubtitleStyle(style => style.Color(ChartColor.FromHex("#475569")))
            .Theme(brand)
            .Columns(3)
            .Padding(28)
            .Gap(gap)
            .PanelSize(panelWidth, panelHeight)
            .Add(CreateAgeHorizontalBars(widePanelWidth, panelHeight), 2)
            .Add(CreateDemographicDonut())
            .Add(CreateSurveyTrend(widePanelWidth, panelHeight), 2)
            .Add(CreateInfographicRings())
            .Add(CreatePopulationRows(widePanelWidth, panelHeight), 2)
            .Add(CreateGenderPictorialSplit(panelWidth, panelHeight))
            .Add(CreateInfographicSliderBars())
            .Add(CreateDressIconRows())
            .Add(CreateInfographicCompletionBars());
    }

    private static Chart CreateDemographicDonut() => Chart.Create()
        .Title("Demographic Split")
        .Subtitle("60.5% male / 39.5% female")
        .Theme(VisualTheme.Graphite())
        .Legend(false)
        .Pie(p => p.CenterValue("60.5%").CenterLabel("Male").InnerRadiusRatio(0.68))
        .Size(420, 300)
        .Padding(34, 34, 34, 24)
        .ShowPlotBackground(false)
        .FormatValuesBy(Percent)
        .XLabels("Male", "Female")
        .AddDonut("Audience", Points(60.5, 39.5));

    private static Chart CreateGenderPictorialSplit(int width, int height) => Chart.Create()
        .Title("Audience Icons")
        .Subtitle("Each icon represents 5%")
        .Theme(VisualTheme.Graphite())
        .Size(width, height)
        .Legend(false)
        .Pictorial(p => p.Columns(10).ValuePerSymbol(5).ShowValues(true).SymbolScale(1.0).EmptyOpacity(0.12))
        .FormatValuesBy(Percent)
        .AddPictorial("Audience", new[] {
            new ChartPictorialItem("Male", 40, ChartColor.FromRgb(6, 182, 212)),
            new ChartPictorialItem("Female", 60, ChartColor.FromRgb(219, 39, 119)),
            new ChartPictorialItem("Other", 17.5, ChartColor.FromRgb(45, 212, 191)),
            new ChartPictorialItem("Prefer not", 7.5, ChartColor.FromRgb(124, 58, 237))
        }, ChartPictorialShape.Person);

    private static Chart CreatePopulationRows(int width, int height) => Chart.Create()
        .Title("People Rows")
        .Subtitle("One icon equals one respondent")
        .Theme(VisualTheme.Graphite())
        .Size(width, height)
        .Legend(false)
        .Pictorial(p => p.Columns(20).ValuePerSymbol(1).ShowValues(false).SymbolScale(0.96).EmptyOpacity(0.08))
        .AddPictorial("Rows", new[] {
            new ChartPictorialItem("50/100", 50, ChartColor.FromRgb(6, 182, 212)),
            new ChartPictorialItem("25/100", 25, ChartColor.FromRgb(219, 39, 119))
        }, ChartPictorialShape.Person);

    private static Chart CreateAgeHorizontalBars(int width, int height) => Chart.Create()
        .Title("Age Bands")
        .Subtitle("Segment response by cohort")
        .Theme(VisualTheme.Graphite())
        .Size(width, height)
        .XLabels("18-24", "25-34", "35-44", "45-54", "55+")
        .FormatValuesBy(Percent)
        .AddHorizontalBar("Male", Points(30, 64, 82, 55, 41), ChartColor.FromRgb(6, 182, 212))
        .AddHorizontalBar("Female", Points(24, 58, 76, 61, 45), ChartColor.FromRgb(219, 39, 119));

    private static Chart CreateSurveyTrend(int width, int height) => Chart.Create()
        .Title("Survey Trend")
        .Subtitle("Two audience groups over time")
        .Theme(VisualTheme.Graphite())
        .Size(width, height)
        .XAxis(x => x.Title("Year"))
        .YAxis(y => y.Title("Share"))
        .XLabels("2016", "2017", "2018", "2019", "2020")
        .FormatValuesBy(Percent)
        .AddSmoothLine("Male", Points(22, 35, 33, 48, 64), ChartColor.FromRgb(6, 182, 212))
        .AddSmoothLine("Female", Points(35, 32, 44, 41, 72), ChartColor.FromRgb(219, 39, 119));

    private static Chart CreateInfographicRings() => Chart.Create()
        .Title("Awareness Ring")
        .Subtitle("Single KPI using the circle chart")
        .Theme(VisualTheme.Graphite())
        .Legend(false)
        .Circle(c => c.ShowStatusLabel(false))
        .Size(420, 300)
        .Padding(34, 34, 34, 24)
        .ShowPlotBackground(false)
        .Circle(c => c.RadiusScale(1.18).StrokeScale(1.32))
        .FormatValuesBy(Percent)
        .AddCircle("Awareness", 75, 0, 100, ChartColor.FromRgb(6, 182, 212));

    private static Chart CreateInfographicSliderBars() => Chart.Create()
        .Title("Preference Sliders")
        .Subtitle("Slider-style progress controls")
        .Theme(VisualTheme.Graphite())
        .Size(420, 300)
        .Legend(false)
        .Progress(p => p.BarThicknessRatio(0.28).TrackOpacity(0.20))
        .FormatValuesBy(Percent)
        .AddProgressBars("Preference", new[] {
            new ChartProgressItem("Male", 40, ChartColor.FromRgb(6, 182, 212)),
            new ChartProgressItem("Female", 60, ChartColor.FromRgb(219, 39, 119))
        });

    private static Chart CreateDressIconRows() => Chart.Create()
        .Title("Dress Icons")
        .Subtitle("Alternate built-in person silhouette")
        .Theme(VisualTheme.Graphite())
        .Size(420, 300)
        .Legend(false)
        .Pictorial(p => p.Columns(12).ValuePerSymbol(5).ShowValues(true).SymbolScale(1.0).EmptyOpacity(0.10))
        .FormatValuesBy(Percent)
        .AddPictorial("Audience", new[] {
            new ChartPictorialItem("Group A", 32.5, ChartColor.FromRgb(219, 39, 119)),
            new ChartPictorialItem("Group B", 27.5, ChartColor.FromRgb(244, 114, 182))
        }, ChartPictorialShape.PersonDress);

    private static Chart CreateInfographicCompletionBars() => Chart.Create()
        .Title("Completion Bars")
        .Subtitle("Compact progress rows")
        .Theme(VisualTheme.Graphite())
        .Size(420, 300)
        .Legend(false)
        .Progress(p => p.ShowHandles(false).BarThicknessRatio(0.46).TrackOpacity(0.14))
        .FormatValuesBy(Percent)
        .AddProgressBars("Completion", new[] {
            new ChartProgressItem("Awareness", 75, ChartColor.FromRgb(6, 182, 212)),
            new ChartProgressItem("Preference", 55, ChartColor.FromRgb(219, 39, 119)),
            new ChartProgressItem("Retention", 25, ChartColor.FromRgb(45, 212, 191))
        });

    private static ChartGrid CreateWordCloudControlShowcaseGrid() {
        return ChartGrid.Create()
            .Title("Word Cloud Control Showcase")
            .Subtitle("Term limits and density controls tune clouds from calm editorial summaries to dense poster layouts")
            .Theme(VisualTheme.Graphite())
            .UsePanelThemes()
            .Columns(2)
            .Padding(28)
            .PanelSize(600, 391)
            .Add(CreateWordCloudControlPreview(
                "Editorial Summary",
                "Top terms, low density, restrained rotation",
                VisualTheme.Graphite(),
                10,
                0.75,
                new[] { -10d, 0d, 8d }))
            .Add(CreateWordCloudControlPreview(
                "Poster Cloud",
                "More terms, tighter packing, playful angles",
                VisualTheme.Graphite(),
                18,
                1.65,
                new[] { -28d, -12d, 0d, 16d, 28d }));
    }

    private static ChartGrid CreateDataLabelPlacementShowcaseGrid() {
        return ChartGrid.Create()
            .Title("Callout Label Placement")
            .Subtitle("Tuned lanes keep labels readable without crowding the marks")
            .Theme(VisualTheme.Graphite())
            .Columns(1)
            .Padding(24)
            .PanelSize(600, 310)
            .Add(CreateOutsideDonutLabelPreview())
            .Add(CreateSideBarLabelPreview());
    }

    private static Chart CreateOutsideDonutLabelPreview() {
        var chart = Chart.Create()
            .Title("Audience Mix Callouts")
            .Subtitle("Offset slices and tuned leaders highlight key segments")
            .Theme(VisualTheme.Graphite())
            .Size(600, 310)
            .Padding(42, 46, 48, 42)
            .Legend(false)
            .ShowPlotBackground(false)
            .DataLabels()
            .DataLabels(d => d.Placement(ChartDataLabelPlacement.Outside).ConnectorColor((ChartColor?)null).ConnectorOpacity(0.58).ConnectorStrokeWidth(1.25).ConnectorStyle(ChartDataLabelConnectorStyle.Curve))
            .Pie(p => p.SliceLabelContent(ChartPieSliceLabelContent.LabelAndPercent).SliceLabelBy(slice => slice.Label + ": " + slice.FormattedPercent).OutsideLabelDistanceRatio(1.18).CenterValue("100%").CenterLabel("Audience").InnerRadiusRatio(0.60))
            .FormatValuesBy(Percent)
            .XLabels("New", "Returning", "Trial", "Advocates")
            .AddDonut("Audience", Points(42, 28, 18, 12));
        chart.Series[0].PointSliceOffset(1, 0.12);
        return chart;
    }

    private static Chart CreateSideBarLabelPreview() {
        var chart = Chart.Create()
            .Title("Scorecard Side Labels")
            .Subtitle("Values sit outside each bar with reserved label lanes")
            .Theme(VisualTheme.Graphite())
            .Size(600, 310)
            .Padding(42, 54, 44, 50)
            .ShowPlotBackground(false)
            .Legend(false)
            .ShowGrid(false)
            .XAxis(x => x.Visible(false))
            .ShowAxisLines(false)
            .DataLabels()
            .DataLabels(d => d.Placement(ChartDataLabelPlacement.Right))
            .XAxis(x => x.Title("Score"))
            .FormatValuesBy(Percent)
            .XLabels("Advocacy", "Retention", "Preference", "Awareness")
            .AddHorizontalBar("Score", Points(31, 48, 64, 82), ChartColor.FromRgb(20, 184, 166));
        chart.Series[0]
            .PointColor(0, "#F97316")
            .PointColor(1, "#F59E0B")
            .PointColor(2, "#8B5CF6")
            .PointColor(3, "#14B8A6");
        return chart;
    }

    private static ChartGrid CreatePointColorCustomizationGrid() {
        return ChartGrid.Create()
            .Title("Point Color Customization Showcase")
            .Subtitle("One API highlights individual intervals, comparisons, summaries, windows, and endpoints")
            .Theme(VisualTheme.Accessible())
            .Columns(2)
            .Padding(28)
            .PanelSize(600, 391)
            .Add(CreatePointColoredRangeBarPreview())
            .Add(CreatePointColoredDumbbellPreview())
            .Add(CreatePointColoredBoxPlotPreview())
            .Add(CreatePointColoredCandlestickPreview())
            .Add(CreatePointColoredOhlcPreview())
            .Add(CreatePointColoredSlopePreview());
    }

    private static Chart CreatePointColoredRangeBarPreview() {
        var chart = Chart.Create()
            .Title("Range Intervals")
            .Subtitle("Highlight one observed window")
            .Theme(VisualTheme.Graphite())
            .Size(600, 391)
            .Legend(l => l.ShowPoints().Position(ChartLegendPosition.Bottom))
            .DataLabels()
            .FormatValuesBy(value => value.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + " ms")
            .XLabels("DNS", "TCP", "TLS", "HTTP")
            .AddRangeBar("Observed", new[] {
                new ChartInterval(1, 18, 42),
                new ChartInterval(2, 44, 88),
                new ChartInterval(3, 96, 142),
                new ChartInterval(4, 128, 196)
            }, ChartColor.FromRgb(14, 165, 233));
        chart.Series[0].PointColor(2, "#F97316").PointDataLabelStyle(2, style => style.Color(ChartColor.FromHex("#9A3412")).FontWeight("900"));
        return chart;
    }

    private static Chart CreatePointColoredDumbbellPreview() {
        var chart = Chart.Create()
            .Title("Before / After")
            .Subtitle("Color a comparison without a new chart type")
            .Theme(VisualTheme.Graphite())
            .Size(600, 391)
            .Legend(l => l.ShowPoints().Position(ChartLegendPosition.TopRight))
            .DataLabels()
            .FormatValuesBy(Percent)
            .XLabels("SPF", "DMARC", "DNSSEC", "MTA")
            .AddDumbbell("Coverage", new[] {
                new ChartDumbbell(1, 32, 44),
                new ChartDumbbell(2, 38, 58),
                new ChartDumbbell(3, 51, 72),
                new ChartDumbbell(4, 43, 67)
            }, ChartColor.FromRgb(37, 99, 235));
        chart.Series[0].PointColor(1, "#0EA5E9").PointColor(2, "#8B5CF6");
        return chart;
    }

    private static Chart CreatePointColoredBoxPlotPreview() {
        var chart = Chart.Create()
            .Title("Spread Summary")
            .Subtitle("Quartile boxes can carry point emphasis")
            .Theme(VisualTheme.Graphite()).ThemeMode(ThemeMode.Dark)
            .Size(600, 391)
            .Legend(false)
            .DataLabels()
            .FormatValuesBy(value => value.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + " ms")
            .XLabels("DNS", "TCP", "TLS")
            .AddBoxPlot("Latency", new[] {
                new ChartBoxPlot(1, 18, 24, 31, 38, 48),
                new ChartBoxPlot(2, 42, 56, 64, 82, 104),
                new ChartBoxPlot(3, 86, 102, 118, 146, 188)
            }, ChartColor.FromRgb(96, 165, 250));
        chart.Series[0].PointColor(1, "#A78BFA").PointDataLabelStyle(1, style => style.Color(ChartColor.FromHex("#C4B5FD")).Underline());
        return chart;
    }

    private static Chart CreatePointColoredCandlestickPreview() {
        var chart = Chart.Create()
            .Title("Candlestick Windows")
            .Subtitle("Override semantic color for a notable window")
            .Theme(VisualTheme.Graphite())
            .Size(600, 391)
            .Legend(false)
            .DataLabels()
            .XLabels("W1", "W2", "W3", "W4")
            .AddCandlestick("Signal", SignalWindows());
        chart.Series[0].PointColor(2, "#DB2777").PointDataLabelStyle(2, style => style.Color(ChartColor.FromHex("#BE123C")).FontWeight("900"));
        return chart;
    }

    private static Chart CreatePointColoredOhlcPreview() {
        var chart = Chart.Create()
            .Title("OHLC Ticks")
            .Subtitle("Compact finance marks keep the same override")
            .Theme(VisualTheme.Graphite()).ThemeMode(ThemeMode.Dark)
            .Size(600, 391)
            .Legend(false)
            .DataLabels()
            .XLabels("W1", "W2", "W3", "W4")
            .AddOhlc("Signal", SignalWindows());
        chart.Series[0].PointColor(1, "#22D3EE").PointColor(3, "#F59E0B");
        return chart;
    }

    private static Chart CreatePointColoredSlopePreview() {
        var chart = Chart.Create()
            .Title("Endpoint Emphasis")
            .Subtitle("Start and end markers can differ")
            .Theme(VisualTheme.Graphite())
            .Size(600, 391)
            .Legend(false)
            .DataLabels()
            .FormatValuesBy(Percent)
            .AddSlope("Readiness", 46, 82, "Before", "After", ChartColor.FromRgb(20, 184, 166));
        chart.Series[0]
            .PointColor(0, "#F97316")
            .PointColor(1, "#14B8A6")
            .PointDataLabelStyle(1, style => style.Color(ChartColor.FromHex("#0F766E")).FontWeight("900").Underline());
        return chart;
    }

    private static ChartCandlestick[] SignalWindows() => new[] {
        new ChartCandlestick(1, 42, 51, 35, 48),
        new ChartCandlestick(2, 58, 66, 49, 54),
        new ChartCandlestick(3, 63, 78, 54, 72),
        new ChartCandlestick(4, 72, 84, 67, 76)
    };

    private static Chart CreateWordCloudControlPreview(string title, string subtitle, VisualTheme theme, int maximumTerms, double density, double[] angles) => Chart.Create()
        .Title(title)
        .Subtitle(subtitle)
        .Theme(theme)
        .Size(600, 391)
        .WordCloud(w => w.FontRange(12, 42).MaxTerms(maximumTerms).Density(density).Angles(angles))
        .AddWordCloud("Themes", WordCloudTerms());

    private static Chart CreateControlPartition() => Chart.Create()
        .DataLabels()
        .Title("Control Coverage Partition")
        .Subtitle("Sunburst hierarchy chart using the Aurora theme")
        .Theme(VisualTheme.Graphite())
        .Size(800, 487)
        .AddSunburst("Controls", new[] {
            new ChartHierarchyItem("Security posture", "Security posture"),
            new ChartHierarchyItem("Mail auth", "Mail auth", "Security posture", 42),
            new ChartHierarchyItem("Certificates", "Certificates", "Security posture", 30),
            new ChartHierarchyItem("DNS hygiene", "DNS hygiene", "Security posture", 28),
            new ChartHierarchyItem("SPF", "SPF", "Mail auth", 18),
            new ChartHierarchyItem("DKIM", "DKIM", "Mail auth", 14),
            new ChartHierarchyItem("DMARC", "DMARC", "Mail auth", 10),
            new ChartHierarchyItem("Expiry", "Expiry", "Certificates", 16),
            new ChartHierarchyItem("SANs", "SANs", "Certificates", 14),
            new ChartHierarchyItem("DNSSEC rollout", "DNSSEC rollout", "DNS hygiene", 16),
            new ChartHierarchyItem("Stale DNS", "Stale DNS", "DNS hygiene", 12)
        });
    private static Chart CreateAudiencePictorial() => Chart.Create()
        .Title("Audience Mix")
        .Subtitle("Pictorial stars turn proportions into a quick visual count")
        .Theme(VisualTheme.Graphite())
        .Size(800, 465)
        .FormatValuesBy(value => value.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "%")
        .Pictorial(p => p.Columns(10).Max(100).SvgPath("M12 2 L15 9 L22 9 L17 14 L19 22 L12 17 L5 22 L7 14 L2 9 L9 9 Z", ChartPictorialShape.Star))
        .AddPictorial("Segments", new[] {
            new ChartPictorialItem("New users", 84, ChartColor.FromRgb(14, 165, 233)),
            new ChartPictorialItem("Returning users", 62, ChartColor.FromRgb(236, 72, 153)),
            new ChartPictorialItem("Trial users", 48, ChartColor.FromRgb(168, 85, 247)),
            new ChartPictorialItem("Advocates", 29, ChartColor.FromRgb(20, 184, 166))
        }, ChartPictorialShape.Star);

    private static Chart CreateSupportThemesWordCloud() => Chart.Create()
        .Title("Support Themes")
        .Subtitle("Word cloud terms use deterministic placement across renderers")
        .Theme(VisualTheme.Graphite())
        .Size(800, 465)
        .WordCloud(w => w.FontRange(13, 50).Angles(new double[] { -18, 0, 10 }))
        .AddWordCloud("Themes", new[] {
            new ChartWordCloudItem("Automation", 100, ChartColor.FromRgb(30, 64, 175)),
            new ChartWordCloudItem("Onboarding", 82),
            new ChartWordCloudItem("Billing", 70, ChartColor.FromRgb(190, 18, 60)),
            new ChartWordCloudItem("Integrations", 64),
            new ChartWordCloudItem("Security", 56, ChartColor.FromRgb(5, 150, 105)),
            new ChartWordCloudItem("Performance", 48),
            new ChartWordCloudItem("Reporting", 40),
            new ChartWordCloudItem("Mobile", 34),
            new ChartWordCloudItem("Exports", 30),
            new ChartWordCloudItem("Access", 26),
            new ChartWordCloudItem("Alerts", 22),
            new ChartWordCloudItem("Polish", 18)
        });

    private static ChartWordCloudItem[] WordCloudTerms() => new[] {
        new ChartWordCloudItem("Automation", 100, ChartColor.FromRgb(30, 64, 175)),
        new ChartWordCloudItem("Onboarding", 82),
        new ChartWordCloudItem("Billing", 70, ChartColor.FromRgb(190, 18, 60)),
        new ChartWordCloudItem("Integrations", 64),
        new ChartWordCloudItem("Security", 56, ChartColor.FromRgb(5, 150, 105)),
        new ChartWordCloudItem("Performance", 48),
        new ChartWordCloudItem("Reporting", 40),
        new ChartWordCloudItem("Mobile", 34),
        new ChartWordCloudItem("Exports", 30),
        new ChartWordCloudItem("Access", 26),
        new ChartWordCloudItem("Alerts", 22),
        new ChartWordCloudItem("Polish", 18),
        new ChartWordCloudItem("Workflows", 16),
        new ChartWordCloudItem("Search", 14),
        new ChartWordCloudItem("Dashboards", 12),
        new ChartWordCloudItem("Teams", 10),
        new ChartWordCloudItem("Themes", 9),
        new ChartWordCloudItem("Charts", 8)
    };

    private static void SaveChart(Chart chart, string output, string name, ChartPngOutputScale pngOutputScale) {
        chart.Png(p => p.OutputScale((int)pngOutputScale));
        chart.SaveSvg(Path.Combine(output, name + ".svg"));
        chart.SaveHtml(Path.Combine(output, name + ".html"));
        chart.SavePng(Path.Combine(output, name + ".png"));
    }

    private static void SaveGrid(ChartGrid grid, string output, string name, ChartPngOutputScale pngOutputScale) {
        grid.PngOutputScale((int)pngOutputScale);
        grid.SaveSvg(Path.Combine(output, name + ".svg"));
        grid.SaveHtml(Path.Combine(output, name + ".html"));
        grid.SavePng(Path.Combine(output, name + ".png"));
    }

    private static IEnumerable<ChartPoint> Points(params double[] y) {
        for (var i = 0; i < y.Length; i++) yield return new ChartPoint(i + 1, y[i]);
    }

    private static string Percent(double value) => value.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + "%";
}
