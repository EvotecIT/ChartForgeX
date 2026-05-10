using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

internal static class ExpressiveExamples {
    public static void Write(string output, ChartPngOutputScale pngOutputScale) {
        SaveGrid(CreateThemeShowcaseGrid(), output, "theme-font-showcase-grid", pngOutputScale);
        SaveGrid(CreateBrandKitShowcaseGrid(), output, "brand-kit-showcase-grid", pngOutputScale);
        SaveGrid(CreatePaletteSwatchGrid(), output, "palette-swatch-showcase-grid", pngOutputScale);
        SaveGrid(CreatePictorialSymbolShowcaseGrid(), output, "pictorial-symbol-showcase-grid", pngOutputScale);
        SaveGrid(CreatePictorialIsotypeShowcaseGrid(), output, "pictorial-isotype-showcase-grid", pngOutputScale);
        SaveGrid(CreatePeopleInfographicShowcaseGrid(), output, "people-infographic-showcase-grid", pngOutputScale);
        SaveGrid(CreateWordCloudControlShowcaseGrid(), output, "word-cloud-control-showcase-grid", pngOutputScale);
        SaveGrid(CreateDataLabelPlacementShowcaseGrid(), output, "data-label-placement-showcase-grid", pngOutputScale);
        SaveGrid(CreatePointColorCustomizationGrid(), output, "point-color-customization-showcase-grid", pngOutputScale);
        SaveChart(CreateDashboardSegmentedColumnPreview(), output, "dashboard-segmented-column-style", pngOutputScale);
        SaveChart(CreateDashboardSegmentedHorizontalPreview(), output, "dashboard-segmented-horizontal-style", pngOutputScale);
        SaveChart(CreateDashboardPremiumTrendPreview(), output, "dashboard-premium-trend-style", pngOutputScale);
        SaveChart(CreateTextStyleShowcase(), output, "text-style-showcase-editorial", pngOutputScale);
        SaveChart(CreateControlPartition(), output, "control-partition-sunburst-aurora", pngOutputScale);
        SaveChart(CreateAudiencePictorial(), output, "audience-pictorial-candy", pngOutputScale);
        SaveChart(CreateSupportThemesWordCloud(), output, "support-themes-word-cloud-editorial", pngOutputScale);
    }

    private static ChartGrid CreateThemeShowcaseGrid() {
        var auroraThemePreview = Chart.Create()
            .WithTitle("Aurora")
            .WithSubtitle("Geometric sans with vivid dark-mode color")
            .WithXAxis("Week")
            .WithYAxis("Signal")
            .WithTheme(ChartTheme.Aurora())
            .WithSize(420, 260)
            .WithXLabels("W1", "W2", "W3", "W4")
            .AddSmoothArea("Observed", Points(32, 48, 43, 66));

        var editorialThemePreview = Chart.Create()
            .WithTitle("Editorial")
            .WithSubtitle("Serif typography for publication-style output")
            .WithXAxis("Issue")
            .WithYAxis("Readers")
            .WithTheme(ChartTheme.Editorial())
            .WithSize(420, 260)
            .WithXLabels("A", "B", "C", "D")
            .AddBar("Readership", Points(44, 58, 51, 72));

        var candyThemePreview = Chart.Create()
            .WithTitle("Candy")
            .WithSubtitle("Rounded type and playful contrast")
            .WithXAxis("Cohort")
            .WithYAxis("Joy")
            .WithTheme(ChartTheme.Candy())
            .WithSize(420, 260)
            .WithXLabels("New", "Trial", "Paid", "Fans")
            .AddSmoothLine("Score", Points(48, 64, 70, 88));

        var terminalThemePreview = Chart.Create()
            .WithTitle("Terminal")
            .WithSubtitle("Monospace operations dashboard styling")
            .WithXAxis("Run")
            .WithYAxis("Pass")
            .WithTheme(ChartTheme.Terminal())
            .WithSize(420, 260)
            .WithXLabels("01", "02", "03", "04")
            .AddStepArea("Passed", Points(55, 61, 58, 74));

        return ChartGrid.Create()
            .WithTitle("Theme and Font Showcase")
            .WithSubtitle("Built-in themes combine palettes, typography, strokes, and radii")
            .WithColumns(2)
            .WithPadding(28)
            .WithPanelSize(420, 260)
            .Add(auroraThemePreview)
            .Add(editorialThemePreview)
            .Add(candyThemePreview)
            .Add(terminalThemePreview);
    }

    private static ChartGrid CreateBrandKitShowcaseGrid() {
        var executive = CreateBrandKitPreview(
            "Executive",
            "Restrained report palette for board-ready snapshots",
            ChartBrandKit.Executive(),
            new[] { "Q1", "Q2", "Q3", "Q4" },
            Points(52, 61, 67, 74));

        var product = CreateBrandKitPreview(
            "Product",
            "Vivid geometric styling for launch and adoption views",
            ChartBrandKit.Product(),
            new[] { "Beta", "GA", "Scale", "Retain" },
            Points(38, 58, 76, 88));

        var editorial = CreateBrandKitPreview(
            "Editorial",
            "Publication-style color and serif typography",
            ChartBrandKit.Editorial(),
            new[] { "North", "South", "East", "West" },
            Points(44, 53, 62, 68));

        var accessible = CreateBrandKitPreview(
            "Accessible",
            "Colorblind-friendly high-contrast comparisons",
            ChartBrandKit.Accessible(),
            new[] { "A", "B", "C", "D" },
            Points(31, 46, 59, 72));

        return ChartGrid.Create()
            .WithTitle("Brand Kit Showcase")
            .WithSubtitle("Reusable brand kits apply palette, font, surface, guide, and semantic tokens")
            .WithBrandKit(ChartBrandKit.Executive())
            .WithColumns(2)
            .WithPadding(28)
            .WithPanelSize(420, 260)
            .Add(executive)
            .Add(product)
            .Add(editorial)
            .Add(accessible);
    }

    private static Chart CreateTextStyleShowcase() {
        var chart = Chart.Create()
            .WithTitle("Styled Report Typography")
            .WithSubtitle("Role-based color, italic, underline, weight, and font-family overrides")
            .WithTheme(ChartTheme.Editorial())
            .WithSize(860, 480)
            .WithXAxis("Audience cohort")
            .WithYAxis("Engagement")
            .WithLegendPosition(ChartLegendPosition.TopRight)
            .WithDataLabels()
            .WithValueFormatter(value => value.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "%")
            .WithTitleStyle(style => style.WithColor("#be123c").WithFontFamily("Georgia, 'Times New Roman', serif").WithWeight("900").WithItalic().WithUnderline())
            .WithSubtitleStyle(style => style.WithColor("#0e7490").WithItalic())
            .WithAxisTitleStyle(style => style.WithColor("#7c3aed").WithUnderline())
            .WithTickLabelStyle(style => style.WithColor("#2563eb").WithItalic())
            .WithLegendStyle(style => style.WithColor("#15803d").WithUnderline())
            .WithDataLabelStyle(style => style.WithColor("#b45309").WithWeight("800"))
            .WithXLabels("Trial", "First value", "Power user", "Advocate")
            .AddBar("Activation share", Points(38, 54, 72, 84), ChartColor.FromHex("#f472b6"))
            .AddSmoothLine("Referral lift", Points(24, 42, 61, 78), ChartColor.FromHex("#14b8a6"));
        chart.Series[1].WithDataLabelStyle(style => style.WithColor("#0f766e").WithWeight("900").WithUnderline());
        return chart;
    }

    private static Chart CreateBrandKitPreview(string title, string subtitle, ChartBrandKit brandKit, string[] labels, IEnumerable<ChartPoint> points) => Chart.Create()
        .WithTitle(title)
        .WithSubtitle(subtitle)
        .WithXAxis("Stage")
        .WithYAxis("Score")
        .WithBrandKit(brandKit)
        .WithSize(420, 260)
        .WithXLabels(labels)
        .AddSmoothArea("Momentum", points);

    private static ChartGrid CreatePaletteSwatchGrid() {
        return ChartGrid.Create()
            .WithTitle("Palette Swatch Showcase")
            .WithSubtitle("Reusable palettes rendered as equal-weight color wheels for quick visual selection")
            .WithBrandKit(ChartBrandKit.Executive())
            .WithColumns(2)
            .WithPadding(28)
            .WithPanelSize(420, 260)
            .Add(CreatePalettePreview("Report", "Balanced default reporting colors", ChartPalettes.Report, ChartTheme.ReportLight()))
            .Add(CreatePalettePreview("Colorblind", "Accessible categorical comparison", ChartPalettes.Colorblind, ChartTheme.Colorblind()))
            .Add(CreatePalettePreview("Vivid", "High-energy launch and product views", ChartPalettes.Vivid, ChartTheme.Aurora()))
            .Add(CreatePalettePreview("Pastel", "Soft playful scorecards", ChartPalettes.Pastel, ChartTheme.Candy()))
            .Add(CreatePalettePreview("Editorial", "Refined publication tones", ChartPalettes.Editorial, ChartTheme.Editorial()))
            .Add(CreatePalettePreview("Terminal", "Technical command-center contrast", ChartPalettes.Terminal, ChartTheme.Terminal()));
    }

    private static Chart CreatePalettePreview(string title, string subtitle, ChartColor[] palette, ChartTheme theme) => Chart.Create()
        .WithTitle(title)
        .WithSubtitle(subtitle)
        .WithTheme(theme)
        .WithPalette(palette)
        .WithLegend(false)
        .WithSize(420, 260)
        .WithXLabels("1", "2", "3", "4", "5", "6", "7", "8")
        .AddPolarArea("Palette", Points(1, 1, 1, 1, 1, 1, 1, 1));

    private static Chart CreateDashboardSegmentedColumnPreview() => Chart.Create()
        .WithHeader(false)
        .WithTransparentBackground(false)
        .WithDashboardBarPanelStyle()
        .WithLegend(false)
        .WithPadding(42, 26, 24, 54)
        .WithTheme(ChartTheme.DashboardLight())
        .WithSize(920, 460)
        .WithXLabels("Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday")
        .WithStackedBars()
        .WithYAxisBounds(0, 120)
        .WithFocusedXAxisCategory(4, paletteIndex: 3)
        .AddBar("Applied", Points(20, 25, 31, 37, 20, 31, 20))
        .AddBar("Screened", Points(30, 21, 12, 18, 18, 27, 21))
        .AddBar("Saved", Points(39, 25, 20, 25, 11, 20, 35))
        .AddBar("Outreach", Points(21, 24, 18, 41, 21, 15, 25));

    private static Chart CreateDashboardSegmentedHorizontalPreview() => Chart.Create()
        .WithHeader(false)
        .WithTransparentBackground(false)
        .WithTheme(ChartTheme.DashboardLight())
        .WithDashboardBarPanelStyle()
        .WithLegend(false)
        .WithPadding(160, 34, 26, 34)
        .WithSize(920, 220)
        .WithXLabels("Software engineer", "Product designer", "Project manager", "Finance")
        .WithStackedHorizontalBars()
        .WithXAxisBounds(0, 100)
        .WithXAxisVisible(false)
        .AddHorizontalBar("Applied", Points(24, 18, 20, 28))
        .AddHorizontalBar("Screened", Points(28, 34, 24, 22))
        .AddHorizontalBar("Saved", Points(30, 25, 36, 24))
        .AddHorizontalBar("Outreach", Points(18, 23, 20, 26));

    private static Chart CreateDashboardPremiumTrendPreview() => Chart.Create()
        .WithHeader(false)
        .WithTransparentBackground(false)
        .WithTheme(ChartTheme.SaasDashboardLight()
            .WithPalette(new[] { ChartColor.FromHex("#2563EB"), ChartColor.FromHex("#14B8A6") })
            .WithGuideColors(ChartColor.FromHex("#D7DEE8"), ChartColor.FromHex("#94A3B8"))
            .WithMarkerRadius(3.2)
            .WithTextColors(ChartColor.FromHex("#64748B"), ChartColor.FromHex("#8792A5")))
        .WithDashboardPanelStyle()
        .WithLegend(false)
        .WithPadding(36, 30, 26, 58)
        .WithSize(920, 360)
        .WithXLabels("Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday")
        .WithYAxisBounds(0, 100)
        .WithHighlightedXAxisLabel(4, paletteIndex: 0)
        .AddSmoothArea("Saved", Points(24, 36, 48, 58, 46, 68, 76))
        .AddSmoothLine("Outreach", Points(18, 28, 36, 48, 40, 58, 66));

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
            .WithTitle("Pictorial Symbol Showcase")
            .WithSubtitle("Built-in pictorial symbols for scorecards, ratings, audience mixes, and friendly summaries")
            .WithTheme(ChartTheme.Candy().WithCornerRadius(8, 4))
            .WithColumns(3)
            .WithPadding(24)
            .WithPanelSize(360, 190);
        for (var i = 0; i < shapes.Length; i++) {
            grid.Add(CreatePictorialShapePreview(shapes[i], palette[i % palette.Length]));
        }

        return grid;
    }

    private static Chart CreatePictorialShapePreview(ChartPictorialShape shape, ChartColor color) => Chart.Create()
        .WithTitle(shape.ToString())
        .WithSubtitle("Built-in symbol")
        .WithTheme(ChartTheme.Candy().WithCornerRadius(8, 4))
        .WithSize(360, 190)
        .WithLegend(false).WithPlotBackground(false)
        .WithPadding(44, 30, 24, 34)
        .WithValueFormatter(value => value.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + "/5")
        .WithPictorialColumns(5).WithPictorialMaximum(5)
        .WithPictorialSymbolScale(1.36)
        .AddPictorial("Shape", new[] { new ChartPictorialItem(shape.ToString(), 4.5, color) }, shape);

    private static ChartGrid CreatePictorialIsotypeShowcaseGrid() {
        return ChartGrid.Create()
            .WithTitle("Pictorial Isotype Showcase")
            .WithSubtitle("One-icon-per-unit rows and population-scale icon rows inspired by classic Isotype charts")
            .WithTheme(ChartTheme.Minimal())
            .WithColumns(2)
            .WithPadding(28)
            .WithPanelSize(430, 280)
            .Add(CreateSimplePeopleIsotype())
            .Add(CreatePopulationPeopleIsotype());
    }

    private static Chart CreateSimplePeopleIsotype() => Chart.Create()
        .WithTitle("Audience Groups")
        .WithSubtitle("Each person icon represents one respondent")
        .WithTheme(ChartTheme.Minimal())
        .WithSize(430, 280)
        .WithPictorialColumns(25)
        .WithPictorialValuePerSymbol(1)
        .WithPictorialValues(false)
        .AddPictorial("Groups", new[] {
            new ChartPictorialItem("A", 24, ChartColor.FromRgb(220, 38, 38)),
            new ChartPictorialItem("B", 4, ChartColor.FromRgb(220, 38, 38)),
            new ChartPictorialItem("C", 1, ChartColor.FromRgb(220, 38, 38))
        }, ChartPictorialShape.Person);

    private static Chart CreatePopulationPeopleIsotype() => Chart.Create()
        .WithTitle("Population of Barrie")
        .WithSubtitle("Each person icon represents 5,000 residents")
        .WithTheme(ChartTheme.Editorial())
        .WithSize(430, 280)
        .WithValueFormatter(value => value.ToString("N0", System.Globalization.CultureInfo.InvariantCulture))
        .WithPictorialColumns(26)
        .WithPictorialValuePerSymbol(5000)
        .WithPictorialValues(false)
        .AddPictorial("Population", new[] {
            new ChartPictorialItem("1981", 38900, ChartColor.FromRgb(14, 165, 233)),
            new ChartPictorialItem("1991", 62000, ChartColor.FromRgb(239, 68, 68)),
            new ChartPictorialItem("1996", 82800, ChartColor.FromRgb(14, 165, 233)),
            new ChartPictorialItem("2001", 103700, ChartColor.FromRgb(239, 68, 68)),
            new ChartPictorialItem("2006", 128400, ChartColor.FromRgb(14, 165, 233))
        }, ChartPictorialShape.Person);

    private static ChartGrid CreatePeopleInfographicShowcaseGrid() {
        var brand = ChartBrandKit.PeopleInfographic();
        const int panelWidth = 420;
        const int panelHeight = 320;
        const int gap = 18;
        const int widePanelWidth = panelWidth * 2 + gap;
        return ChartGrid.Create()
            .WithTitle("People Infographic Showcase")
            .WithSubtitle("Demographic panels using pictorial people, donut split, rings, horizontal bars, and trend lines")
            .WithTitleStyle(style => style.WithColor("#0F172A").WithWeight("900"))
            .WithSubtitleStyle(style => style.WithColor("#475569"))
            .WithBrandKit(brand)
            .WithColumns(3)
            .WithPadding(28)
            .WithGap(gap)
            .WithPanelSize(panelWidth, panelHeight)
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
        .WithTitle("Demographic Split")
        .WithSubtitle("60.5% male / 39.5% female")
        .WithTheme(ChartTheme.PeopleInfographic())
        .WithLegend(false)
        .WithDonutCenterText("60.5%", "Male")
        .WithDonutInnerRadiusRatio(0.68)
        .WithSize(420, 300)
        .WithPadding(34, 34, 34, 24)
        .WithPlotBackground(false)
        .WithValueFormatter(Percent)
        .WithXLabels("Male", "Female")
        .AddDonut("Audience", Points(60.5, 39.5));

    private static Chart CreateGenderPictorialSplit(int width, int height) => Chart.Create()
        .WithTitle("Audience Icons")
        .WithSubtitle("Each icon represents 5%")
        .WithTheme(ChartTheme.PeopleInfographic())
        .WithSize(width, height)
        .WithLegend(false)
        .WithPictorialColumns(10)
        .WithPictorialValuePerSymbol(5)
        .WithPictorialValues(true)
        .WithPictorialSymbolScale(1.0)
        .WithPictorialEmptyOpacity(0.12)
        .WithValueFormatter(Percent)
        .AddPictorial("Audience", new[] {
            new ChartPictorialItem("Male", 40, ChartColor.FromRgb(6, 182, 212)),
            new ChartPictorialItem("Female", 60, ChartColor.FromRgb(219, 39, 119)),
            new ChartPictorialItem("Other", 17.5, ChartColor.FromRgb(45, 212, 191)),
            new ChartPictorialItem("Prefer not", 7.5, ChartColor.FromRgb(124, 58, 237))
        }, ChartPictorialShape.Person);

    private static Chart CreatePopulationRows(int width, int height) => Chart.Create()
        .WithTitle("People Rows")
        .WithSubtitle("One icon equals one respondent")
        .WithTheme(ChartTheme.PeopleInfographic())
        .WithSize(width, height)
        .WithLegend(false)
        .WithPictorialColumns(20)
        .WithPictorialValuePerSymbol(1)
        .WithPictorialValues(false)
        .WithPictorialSymbolScale(0.96)
        .WithPictorialEmptyOpacity(0.08)
        .AddPictorial("Rows", new[] {
            new ChartPictorialItem("50/100", 50, ChartColor.FromRgb(6, 182, 212)),
            new ChartPictorialItem("25/100", 25, ChartColor.FromRgb(219, 39, 119))
        }, ChartPictorialShape.Person);

    private static Chart CreateAgeHorizontalBars(int width, int height) => Chart.Create()
        .WithTitle("Age Bands")
        .WithSubtitle("Segment response by cohort")
        .WithTheme(ChartTheme.PeopleInfographic())
        .WithSize(width, height)
        .WithXLabels("18-24", "25-34", "35-44", "45-54", "55+")
        .WithValueFormatter(Percent)
        .AddHorizontalBar("Male", Points(30, 64, 82, 55, 41), ChartColor.FromRgb(6, 182, 212))
        .AddHorizontalBar("Female", Points(24, 58, 76, 61, 45), ChartColor.FromRgb(219, 39, 119));

    private static Chart CreateSurveyTrend(int width, int height) => Chart.Create()
        .WithTitle("Survey Trend")
        .WithSubtitle("Two audience groups over time")
        .WithTheme(ChartTheme.PeopleInfographic())
        .WithSize(width, height)
        .WithXAxis("Year")
        .WithYAxis("Share")
        .WithXLabels("2016", "2017", "2018", "2019", "2020")
        .WithValueFormatter(Percent)
        .AddSmoothLine("Male", Points(22, 35, 33, 48, 64), ChartColor.FromRgb(6, 182, 212))
        .AddSmoothLine("Female", Points(35, 32, 44, 41, 72), ChartColor.FromRgb(219, 39, 119));

    private static Chart CreateInfographicRings() => Chart.Create()
        .WithTitle("Awareness Ring")
        .WithSubtitle("Single KPI using the circle chart")
        .WithTheme(ChartTheme.PeopleInfographic())
        .WithLegend(false)
        .WithCircleStatusLabel(false)
        .WithSize(420, 300)
        .WithPadding(34, 34, 34, 24)
        .WithPlotBackground(false)
        .WithCircleRadiusScale(1.18)
        .WithCircleStrokeScale(1.32)
        .WithValueFormatter(Percent)
        .AddCircle("Awareness", 75, 0, 100, ChartColor.FromRgb(6, 182, 212));

    private static Chart CreateInfographicSliderBars() => Chart.Create()
        .WithTitle("Preference Sliders")
        .WithSubtitle("Slider-style progress controls")
        .WithTheme(ChartTheme.PeopleInfographic())
        .WithSize(420, 300)
        .WithLegend(false)
        .WithProgressBarThickness(0.28)
        .WithProgressTrackOpacity(0.20)
        .WithValueFormatter(Percent)
        .AddProgressBars("Preference", new[] {
            new ChartProgressItem("Male", 40, ChartColor.FromRgb(6, 182, 212)),
            new ChartProgressItem("Female", 60, ChartColor.FromRgb(219, 39, 119))
        });

    private static Chart CreateDressIconRows() => Chart.Create()
        .WithTitle("Dress Icons")
        .WithSubtitle("Alternate built-in person silhouette")
        .WithTheme(ChartTheme.PeopleInfographic())
        .WithSize(420, 300)
        .WithLegend(false)
        .WithPictorialColumns(12)
        .WithPictorialValuePerSymbol(5)
        .WithPictorialValues(true)
        .WithPictorialSymbolScale(1.0)
        .WithPictorialEmptyOpacity(0.10)
        .WithValueFormatter(Percent)
        .AddPictorial("Audience", new[] {
            new ChartPictorialItem("Group A", 32.5, ChartColor.FromRgb(219, 39, 119)),
            new ChartPictorialItem("Group B", 27.5, ChartColor.FromRgb(244, 114, 182))
        }, ChartPictorialShape.PersonDress);

    private static Chart CreateInfographicCompletionBars() => Chart.Create()
        .WithTitle("Completion Bars")
        .WithSubtitle("Compact progress rows")
        .WithTheme(ChartTheme.PeopleInfographic())
        .WithSize(420, 300)
        .WithLegend(false)
        .WithProgressHandles(false)
        .WithProgressBarThickness(0.46)
        .WithProgressTrackOpacity(0.14)
        .WithValueFormatter(Percent)
        .AddProgressBars("Completion", new[] {
            new ChartProgressItem("Awareness", 75, ChartColor.FromRgb(6, 182, 212)),
            new ChartProgressItem("Preference", 55, ChartColor.FromRgb(219, 39, 119)),
            new ChartProgressItem("Retention", 25, ChartColor.FromRgb(45, 212, 191))
        });

    private static ChartGrid CreateWordCloudControlShowcaseGrid() {
        return ChartGrid.Create()
            .WithTitle("Word Cloud Control Showcase")
            .WithSubtitle("Term limits and density controls tune clouds from calm editorial summaries to dense poster layouts")
            .WithBrandKit(ChartBrandKit.Editorial())
            .WithColumns(2)
            .WithPadding(28)
            .WithPanelSize(430, 280)
            .Add(CreateWordCloudControlPreview(
                "Editorial Summary",
                "Top terms, low density, restrained rotation",
                ChartTheme.Editorial(),
                10,
                0.75,
                new[] { -10d, 0d, 8d }))
            .Add(CreateWordCloudControlPreview(
                "Poster Cloud",
                "More terms, tighter packing, playful angles",
                ChartTheme.Candy(),
                18,
                1.65,
                new[] { -28d, -12d, 0d, 16d, 28d }));
    }

    private static ChartGrid CreateDataLabelPlacementShowcaseGrid() {
        return ChartGrid.Create()
            .WithTitle("Callout Label Placement")
            .WithSubtitle("Tuned lanes keep labels readable without crowding the marks")
            .WithBrandKit(ChartBrandKit.Product())
            .WithColumns(1)
            .WithPadding(24)
            .WithPanelSize(620, 320)
            .Add(CreateOutsideDonutLabelPreview())
            .Add(CreateSideBarLabelPreview());
    }

    private static Chart CreateOutsideDonutLabelPreview() {
        var chart = Chart.Create()
            .WithTitle("Audience Mix Callouts")
            .WithSubtitle("Offset slices and tuned leaders highlight key segments")
            .WithTheme(ChartTheme.Aurora())
            .WithSize(620, 320)
            .WithPadding(42, 46, 48, 42)
            .WithLegend(false)
            .WithPlotBackground(false)
            .WithDataLabels()
            .WithDataLabelPlacement(ChartDataLabelPlacement.Outside)
            .WithDataLabelConnectorColor((ChartColor?)null)
            .WithDataLabelConnectorOpacity(0.58)
            .WithDataLabelConnectorStrokeWidth(1.25)
            .WithDataLabelConnectorStyle(ChartDataLabelConnectorStyle.Curve)
            .WithPieSliceLabelContent(ChartPieSliceLabelContent.LabelAndPercent)
            .WithPieSliceLabelFormatter(slice => slice.Label + ": " + slice.FormattedPercent)
            .WithPieOutsideLabelDistance(1.18)
            .WithDonutCenterText("100%", "Audience")
            .WithDonutInnerRadiusRatio(0.60)
            .WithValueFormatter(Percent)
            .WithXLabels("New", "Returning", "Trial", "Advocates")
            .AddDonut("Audience", Points(42, 28, 18, 12));
        chart.Series[0].WithPointSliceOffset(1, 0.12);
        return chart;
    }

    private static Chart CreateSideBarLabelPreview() {
        var chart = Chart.Create()
            .WithTitle("Scorecard Side Labels")
            .WithSubtitle("Values sit outside each bar with reserved label lanes")
            .WithTheme(ChartTheme.ReportLight())
            .WithSize(620, 320)
            .WithPadding(42, 54, 44, 50)
            .WithPlotBackground(false)
            .WithLegend(false)
            .WithGrid(false)
            .WithXAxisVisible(false)
            .WithAxisLines(false)
            .WithDataLabels()
            .WithDataLabelPlacement(ChartDataLabelPlacement.Right)
            .WithXAxis("Score")
            .WithValueFormatter(Percent)
            .WithXLabels("Advocacy", "Retention", "Preference", "Awareness")
            .AddHorizontalBar("Score", Points(31, 48, 64, 82), ChartColor.FromRgb(20, 184, 166));
        chart.Series[0]
            .WithPointColor(0, "#F97316")
            .WithPointColor(1, "#F59E0B")
            .WithPointColor(2, "#8B5CF6")
            .WithPointColor(3, "#14B8A6");
        return chart;
    }

    private static ChartGrid CreatePointColorCustomizationGrid() {
        return ChartGrid.Create()
            .WithTitle("Point Color Customization Showcase")
            .WithSubtitle("One API highlights individual intervals, comparisons, summaries, windows, and endpoints")
            .WithBrandKit(ChartBrandKit.Accessible())
            .WithColumns(2)
            .WithPadding(28)
            .WithPanelSize(430, 280)
            .Add(CreatePointColoredRangeBarPreview())
            .Add(CreatePointColoredDumbbellPreview())
            .Add(CreatePointColoredBoxPlotPreview())
            .Add(CreatePointColoredCandlestickPreview())
            .Add(CreatePointColoredOhlcPreview())
            .Add(CreatePointColoredSlopePreview());
    }

    private static Chart CreatePointColoredRangeBarPreview() {
        var chart = Chart.Create()
            .WithTitle("Range Intervals")
            .WithSubtitle("Highlight one observed window")
            .WithTheme(ChartTheme.ReportLight())
            .WithSize(430, 280)
            .WithPointLegend()
            .WithLegendPosition(ChartLegendPosition.Bottom)
            .WithDataLabels()
            .WithValueFormatter(value => value.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + " ms")
            .WithXLabels("DNS", "TCP", "TLS", "HTTP")
            .AddRangeBar("Observed", new[] {
                new ChartInterval(1, 18, 42),
                new ChartInterval(2, 44, 88),
                new ChartInterval(3, 96, 142),
                new ChartInterval(4, 128, 196)
            }, ChartColor.FromRgb(14, 165, 233));
        chart.Series[0].WithPointColor(2, "#F97316").WithPointDataLabelStyle(2, style => style.WithColor("#9A3412").WithWeight("900"));
        return chart;
    }

    private static Chart CreatePointColoredDumbbellPreview() {
        var chart = Chart.Create()
            .WithTitle("Before / After")
            .WithSubtitle("Color a comparison without a new chart type")
            .WithTheme(ChartTheme.ReportLight())
            .WithSize(430, 280)
            .WithPointLegend()
            .WithLegendPosition(ChartLegendPosition.TopRight)
            .WithDataLabels()
            .WithValueFormatter(Percent)
            .WithXLabels("SPF", "DMARC", "DNSSEC", "MTA")
            .AddDumbbell("Coverage", new[] {
                new ChartDumbbell(1, 32, 44),
                new ChartDumbbell(2, 38, 58),
                new ChartDumbbell(3, 51, 72),
                new ChartDumbbell(4, 43, 67)
            }, ChartColor.FromRgb(37, 99, 235));
        chart.Series[0].WithPointColor(1, "#0EA5E9").WithPointColor(2, "#8B5CF6");
        return chart;
    }

    private static Chart CreatePointColoredBoxPlotPreview() {
        var chart = Chart.Create()
            .WithTitle("Spread Summary")
            .WithSubtitle("Quartile boxes can carry point emphasis")
            .WithTheme(ChartTheme.ReportDark())
            .WithSize(430, 280)
            .WithLegend(false)
            .WithDataLabels()
            .WithValueFormatter(value => value.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + " ms")
            .WithXLabels("DNS", "TCP", "TLS")
            .AddBoxPlot("Latency", new[] {
                new ChartBoxPlot(1, 18, 24, 31, 38, 48),
                new ChartBoxPlot(2, 42, 56, 64, 82, 104),
                new ChartBoxPlot(3, 86, 102, 118, 146, 188)
            }, ChartColor.FromRgb(96, 165, 250));
        chart.Series[0].WithPointColor(1, "#A78BFA").WithPointDataLabelStyle(1, style => style.WithColor("#C4B5FD").WithUnderline());
        return chart;
    }

    private static Chart CreatePointColoredCandlestickPreview() {
        var chart = Chart.Create()
            .WithTitle("Candlestick Windows")
            .WithSubtitle("Override semantic color for a notable window")
            .WithTheme(ChartTheme.ReportLight())
            .WithSize(430, 280)
            .WithLegend(false)
            .WithDataLabels()
            .WithXLabels("W1", "W2", "W3", "W4")
            .AddCandlestick("Signal", SignalWindows());
        chart.Series[0].WithPointColor(2, "#DB2777").WithPointDataLabelStyle(2, style => style.WithColor("#BE123C").WithWeight("900"));
        return chart;
    }

    private static Chart CreatePointColoredOhlcPreview() {
        var chart = Chart.Create()
            .WithTitle("OHLC Ticks")
            .WithSubtitle("Compact finance marks keep the same override")
            .WithTheme(ChartTheme.ReportDark())
            .WithSize(430, 280)
            .WithLegend(false)
            .WithDataLabels()
            .WithXLabels("W1", "W2", "W3", "W4")
            .AddOhlc("Signal", SignalWindows());
        chart.Series[0].WithPointColor(1, "#22D3EE").WithPointColor(3, "#F59E0B");
        return chart;
    }

    private static Chart CreatePointColoredSlopePreview() {
        var chart = Chart.Create()
            .WithTitle("Endpoint Emphasis")
            .WithSubtitle("Start and end markers can differ")
            .WithTheme(ChartTheme.Minimal())
            .WithSize(430, 280)
            .WithLegend(false)
            .WithDataLabels()
            .WithValueFormatter(Percent)
            .AddSlope("Readiness", 46, 82, "Before", "After", ChartColor.FromRgb(20, 184, 166));
        chart.Series[0]
            .WithPointColor(0, "#F97316")
            .WithPointColor(1, "#14B8A6")
            .WithPointDataLabelStyle(1, style => style.WithColor("#0F766E").WithWeight("900").WithUnderline());
        return chart;
    }

    private static ChartCandlestick[] SignalWindows() => new[] {
        new ChartCandlestick(1, 42, 51, 35, 48),
        new ChartCandlestick(2, 58, 66, 49, 54),
        new ChartCandlestick(3, 63, 78, 54, 72),
        new ChartCandlestick(4, 72, 84, 67, 76)
    };

    private static Chart CreateWordCloudControlPreview(string title, string subtitle, ChartTheme theme, int maximumTerms, double density, double[] angles) => Chart.Create()
        .WithTitle(title)
        .WithSubtitle(subtitle)
        .WithTheme(theme)
        .WithSize(430, 280)
        .WithWordCloudFontRange(12, 42)
        .WithWordCloudMaximumTerms(maximumTerms)
        .WithWordCloudDensity(density)
        .WithWordCloudAngles(angles)
        .AddWordCloud("Themes", WordCloudTerms());

    private static Chart CreateControlPartition() => Chart.Create()
        .WithTitle("Control Coverage Partition")
        .WithSubtitle("Sunburst hierarchy chart using the Aurora theme")
        .WithTheme(ChartTheme.Aurora())
        .WithSize(920, 560)
        .AddSunburst("Controls", new[] {
            new ChartTreeLink("Security posture", "Mail auth", 42),
            new ChartTreeLink("Security posture", "Certificates", 30),
            new ChartTreeLink("Security posture", "DNS hygiene", 28),
            new ChartTreeLink("Mail auth", "SPF", 18),
            new ChartTreeLink("Mail auth", "DKIM", 14),
            new ChartTreeLink("Mail auth", "DMARC", 10),
            new ChartTreeLink("Certificates", "Expiry", 16),
            new ChartTreeLink("Certificates", "SANs", 14),
            new ChartTreeLink("DNS hygiene", "DNSSEC rollout", 16),
            new ChartTreeLink("DNS hygiene", "Stale DNS", 12)
        });

    private static Chart CreateAudiencePictorial() => Chart.Create()
        .WithTitle("Audience Mix")
        .WithSubtitle("Pictorial stars turn proportions into a quick visual count")
        .WithTheme(ChartTheme.Candy())
        .WithSize(860, 500)
        .WithValueFormatter(value => value.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "%")
        .WithPictorialColumns(10)
        .WithPictorialMaximum(100)
        .WithPictorialSvgPath("M12 2 L15 9 L22 9 L17 14 L19 22 L12 17 L5 22 L7 14 L2 9 L9 9 Z", ChartPictorialShape.Star)
        .AddPictorial("Segments", new[] {
            new ChartPictorialItem("New users", 84, ChartColor.FromRgb(14, 165, 233)),
            new ChartPictorialItem("Returning users", 62, ChartColor.FromRgb(236, 72, 153)),
            new ChartPictorialItem("Trial users", 48, ChartColor.FromRgb(168, 85, 247)),
            new ChartPictorialItem("Advocates", 29, ChartColor.FromRgb(20, 184, 166))
        }, ChartPictorialShape.Star);

    private static Chart CreateSupportThemesWordCloud() => Chart.Create()
        .WithTitle("Support Themes")
        .WithSubtitle("Word cloud terms use deterministic placement across renderers")
        .WithTheme(ChartTheme.Editorial())
        .WithSize(860, 500)
        .WithWordCloudFontRange(13, 50)
        .WithWordCloudAngles(-18, 0, 10)
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
        chart.WithPngOutputScale(pngOutputScale);
        chart.SaveSvg(Path.Combine(output, name + ".svg"));
        chart.SaveHtml(Path.Combine(output, name + ".html"));
        chart.SavePng(Path.Combine(output, name + ".png"));
    }

    private static void SaveGrid(ChartGrid grid, string output, string name, ChartPngOutputScale pngOutputScale) {
        grid.WithPngOutputScale(pngOutputScale);
        grid.SaveSvg(Path.Combine(output, name + ".svg"));
        grid.SaveHtml(Path.Combine(output, name + ".html"));
        grid.SavePng(Path.Combine(output, name + ".png"));
    }

    private static IEnumerable<ChartPoint> Points(params double[] y) {
        for (var i = 0; i < y.Length; i++) yield return new ChartPoint(i + 1, y[i]);
    }

    private static string Percent(double value) => value.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + "%";
}
