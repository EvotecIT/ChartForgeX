using ChartForgeX;
using ChartForgeX.Composition;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.VisualBlocks;

internal static class WellnessDashboardExamples {
    private static readonly ChartColor Background = ChartColor.FromHex("#F5F6F8");
    private static readonly ChartColor Card = ChartColor.FromHex("#FFFFFF");
    private static readonly ChartColor Text = ChartColor.FromHex("#252936");
    private static readonly ChartColor Muted = ChartColor.FromHex("#8B8E96");
    private static readonly ChartColor Track = ChartColor.FromHex("#F1F2F6");
    private static readonly ChartColor Orange = ChartColor.FromHex("#FF9F4A");
    private static readonly ChartColor Yellow = ChartColor.FromHex("#FFCD62");
    private static readonly ChartColor Green = ChartColor.FromHex("#BDE765");
    private static readonly ChartColor Cyan = ChartColor.FromHex("#38BDF8");
    private static readonly ChartColor Emerald = ChartColor.FromHex("#34D399");

    public static void Write(string output, ChartPngOutputScale pngOutputScale) {
        SaveLayeredRadial(output, pngOutputScale);
        WritePowerBgInfoSocialPreview(output, (int)pngOutputScale);
        SaveWeightCard(output, (int)pngOutputScale);
        SaveCaloriesCard(output, (int)pngOutputScale);
        SavePowerBgInfoStatsSection(output, (int)pngOutputScale);
        SaveReportMetricStrip(output, (int)pngOutputScale);
        SaveTransparentReportMetricStrip(output, (int)pngOutputScale);
        SaveWeeklyProgressDashboard(output, (int)pngOutputScale);
    }

    public static void WritePowerBgInfoSocialPreview(string output, int outputScale) {
        var canvas = VisualCanvas.CreateSocialPreview()
            .WithTitle("PowerBGInfo social preview")
            .WithBackground(ChartColor.FromHex("#020713"), ChartColor.FromHex("#071A35"))
            .WithBackdrop(VisualCanvasBackdropStyle.TechHorizon)
            .WithPngOutputScale(outputScale)
            .AddInfoTile(48, 92, 300, 82, "PC", "HOSTNAME", "DEV-WKS-01", accent: ChartColor.FromHex("#2F80FF"))
            .AddInfoTile(48, 190, 300, 82, "IP", "IP ADDRESS", "10.0.0.42", "192.168.1.42", ChartColor.FromHex("#2F80FF"))
            .AddInfoTile(48, 288, 300, 82, "OS", "OS", "Windows 11", "24H2", ChartColor.FromHex("#2F80FF"))
            .AddInfoTile(852, 70, 300, 96, "CPU", "CPU", "Intel Core i7", accent: ChartColor.FromHex("#60A5FA"), progress: 0.23)
            .AddInfoTile(852, 184, 300, 96, "RAM", "RAM", "32.0 GB", accent: ChartColor.FromHex("#60A5FA"), progress: 0.41)
            .AddInfoTile(852, 298, 300, 82, "USR", "USER", "jdoe", accent: ChartColor.FromHex("#60A5FA"))
            .AddHeroBadge(538, 157, 124, 88, ">_", ChartColor.FromHex("#22A7FF"))
            .AddHeroTitle(312, 296, 576, 82, new[] {
                new VisualCanvasTextRun("Power", ChartColor.FromHex("#F8FAFC")),
                new VisualCanvasTextRun("BGInfo", ChartColor.FromHex("#2F80FF"))
            })
            .AddText(240, 402, 720, "Desktop background insights for Windows and PowerShell", 24, ChartColor.FromHex("#C6D3EA"), TextAlignment.Center)
            .AddFeatureStrip(290, 522, 620, 62, new[] {
                new VisualCanvasFeatureItem("PS", "LIGHTWEIGHT"),
                new VisualCanvasFeatureItem("OK", "SECURE"),
                new VisualCanvasFeatureItem("UI", "CUSTOMIZABLE"),
                new VisualCanvasFeatureItem("OS", "OPEN SOURCE")
            });

        canvas.SaveSvg(Path.Combine(output, "powerbginfo-social-preview-canvas.svg"));
        canvas.SaveHtml(Path.Combine(output, "powerbginfo-social-preview-canvas.html"));
        canvas.SavePng(Path.Combine(output, "powerbginfo-social-preview-canvas.png"));
    }

    private static void SaveLayeredRadial(string output, ChartPngOutputScale pngOutputScale) {
        var chart = Chart.Create()
            .WithTitle("Layered Radial Progress")
            .WithSubtitle("Generic radial layers with independent radius, stroke, color, and value scale.")
            .WithSize(560, 560)
            .WithPadding(48, 48, 48, 48)
            .WithTheme(WellnessTheme())
            .WithPlotBackground(false)
            .WithLegend(false)
            .WithPngOutputScale(pngOutputScale)
            .WithValueFormatter(value => value.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + " kcal")
            .AddLayeredRadial("Calories left", layers => layers
                .Add("Available area", 100, color: Track, configure: layer => layer
                    .WithGeometry(1.00, 0.18)
                    .WithLineCap(ChartRadialLayerCap.Butt))
                .Add("Target ring", 100, color: Yellow, configure: layer => layer
                    .WithGeometry(0.93, 0.035)
                    .WithLineCap(ChartRadialLayerCap.Butt))
                .Add("Current", 1240, maximum: 2700, color: Orange, configure: layer => layer
                    .WithGeometry(0.93, 0.14)
                    .WithAngles(-90, 360)));

        chart.SaveSvg(Path.Combine(output, "wellness-layered-radial-progress.svg"));
        chart.SaveHtml(Path.Combine(output, "wellness-layered-radial-progress.html"));
        chart.SavePng(Path.Combine(output, "wellness-layered-radial-progress.png"));
    }

    private static void SaveWeightCard(string output, int outputScale) {
        var card = RadialMetricCard.Create()
            .WithTitle("Weight Data")
            .WithSubtitle("Goal progress as reusable radial metric layers.")
            .WithSize(560, 420)
            .WithTheme(WellnessTheme())
            .WithPngOutputScale(outputScale)
            .WithMetric("Current Weight", "78 kg")
            .AddLayer("Current weight progress", 100, color: Orange, configure: layer => layer
                .WithGeometry(1.00, 0.265)
                .WithAngles(184, 74))
            .AddLayer("Remaining weight range", 100, color: Yellow, configure: layer => layer
                .WithGeometry(1.00, 0.265)
                .WithAngles(279, 77)
                .WithSeparators(8, Card, 3, 0));

        card.SaveSvg(Path.Combine(output, "wellness-weight-data-gauge.svg"));
        card.SaveHtml(Path.Combine(output, "wellness-weight-data-gauge.html"));
        card.SavePng(Path.Combine(output, "wellness-weight-data-gauge.png"));
    }

    private static void SaveCaloriesCard(string output, int outputScale) {
        var radial = RadialMetricCard.Create()
            .WithTitle("Calories left")
            .WithSize(360, 438)
            .WithTheme(WellnessTheme())
            .WithIcon(VisualIcon.Lightning)
            .WithMetric("Calories left", "1240 kcal")
            .AddLayer("Available area", 100, color: Track, configure: layer => layer
                .WithGeometry(1.00, 0.18)
                .WithLineCap(ChartRadialLayerCap.Butt))
            .AddLayer("Target ring", 100, color: Yellow, configure: layer => layer
                .WithGeometry(0.93, 0.035)
                .WithLineCap(ChartRadialLayerCap.Butt))
            .AddLayer("Current", 1240, maximum: 2700, color: Orange, configure: layer => layer
                .WithGeometry(0.93, 0.14)
                .WithAngles(-90, 360));

        var eaten = MetricCard.Create()
            .WithSize(360, 210)
            .WithTheme(WellnessTheme())
            .WithIcon(VisualIcon.ForkKnife)
            .WithMetric("Eaten calories", "1750 kcal")
            .WithMiniBars(new[] { 42d, 56d, 49d, 64d, 71d }, maximum: 100, color: Orange, mutedColor: Muted.WithAlpha(72))
            .WithCaption("Logged intake");

        var burned = MetricCard.Create()
            .WithSize(360, 210)
            .WithTheme(WellnessTheme())
            .WithIcon(VisualIcon.Flame)
            .WithMetric("Burned calories", "510 kcal")
            .WithMiniBars(new[] { 22d, 34d, 39d, 32d, 46d }, maximum: 60, color: Green, mutedColor: Muted.WithAlpha(72))
            .WithCaption("Activity estimate");

        var macros = Chart.Create()
            .WithTitle("Macro Balance")
            .WithSize(738, 210)
            .WithPadding(26, 48, 26, 24)
            .WithTheme(WellnessTheme())
            .WithLegend(false)
            .WithProgressValues(true)
            .WithProgressHandles(false)
            .WithProgressBarThickness(0.46)
            .AddProgressBars("Macros", new[] {
                new ChartProgressItem("Carbohydrates", 37, Green),
                new ChartProgressItem("Proteins", 93, Orange),
                new ChartProgressItem("Fats", 45, Yellow)
            });

        var grid = VisualGrid.Create()
            .WithTitle("Calories Intake")
            .WithSubtitle("Built from public visual blocks, icons, radial layers, and progress bars.")
            .WithColumns(3)
            .WithPanelSize(360, 210)
            .WithGap(18)
            .WithPadding(24)
            .WithTheme(WellnessTheme())
            .WithPngOutputScale(outputScale)
            .Add(radial, rowSpan: 2)
            .Add(eaten)
            .Add(burned)
            .Add(macros, columnSpan: 2);

        grid.SaveSvg(Path.Combine(output, "wellness-calories-intake-dashboard.svg"));
        grid.SaveHtml(Path.Combine(output, "wellness-calories-intake-dashboard.html"));
        grid.SavePng(Path.Combine(output, "wellness-calories-intake-dashboard.png"));
    }

    private static void SavePowerBgInfoStatsSection(string output, int outputScale) {
        var theme = VisualTheme.Report().Configure(builder => {
            builder.Light.Background = builder.Dark.Background = ChartColor.FromHex("#101319");
            builder.Light.ElevatedSurface = builder.Dark.ElevatedSurface = ChartColor.FromHex("#171B22");
            builder.Light.Surface = builder.Dark.Surface = ChartColor.FromHex("#171B22");
            builder.Light.Border = builder.Dark.Border = ChartColor.FromHex("#222834");
            builder.Light.PlotBorder = builder.Dark.PlotBorder = ChartColor.FromHex("#2A3240");
            builder.Light.Foreground = builder.Dark.Foreground = ChartColor.FromHex("#F8FAFC");
            builder.Light.MutedForeground = builder.Dark.MutedForeground = ChartColor.FromHex("#A6ADBB");
            builder.Light.Palette = builder.Dark.Palette = new[] { Emerald, Cyan, Yellow, Orange };
            builder.CardRadius = 18;
            builder.PlotRadius = builder.BarRadius = 10;
        });

        MetricCard Card(string label, string value, string trend, string caption, string symbol, VisualStatus status, double[] history, ChartColor color) =>
            MetricCard.Create()
                .WithSize(320, 176)
                .WithTheme(theme)
                .WithMetric(label, value)
                .WithTrend(trend)
                .WithCaption(caption)
                .WithSymbol(symbol)
                .WithBadgePlacement(MetricCardBadgePlacement.TopLeft)
                .WithStatus(status)
                .WithMiniBars(history, maximum: 100, color: color, mutedColor: ChartColor.FromHex("#64748B").WithAlpha(112));

        MetricCard SparkCard(string label, string value, string trend, string caption, string symbol, VisualStatus status, double[] history, ChartColor color) =>
            MetricCard.Create()
                .WithSize(320, 176)
                .WithTheme(theme)
                .WithMetric(label, value)
                .WithTrend(trend)
                .WithCaption(caption)
                .WithSymbol(symbol)
                .WithBadgePlacement(MetricCardBadgePlacement.TopLeft)
                .WithStatus(status)
                .WithMiniSparkline(history, color: color, fillColor: color.WithAlpha(42));

        var cards = new[] {
            SparkCard("CPU Load", "38%", "-6%", "5 minute trend", "CPU", VisualStatus.Positive, new[] { 52d, 48d, 44d, 41d, 38d }, Emerald),
            Card("Memory Used", "71%", "+4%", "since boot", "RAM", VisualStatus.Warning, new[] { 55d, 59d, 63d, 68d, 71d }, Yellow),
            Card("Disk Free", "128 GB", "-12 GB", "system volume", "SSD", VisualStatus.Positive, new[] { 92d, 87d, 82d, 76d, 71d }, Cyan),
            SparkCard("Network", "842 Mbps", "+18%", "active adapter", "LAN", VisualStatus.Info, new[] { 24d, 48d, 37d, 64d, 82d }, Orange)
        };

        var grid = VisualGrid.CreateMetricStrip("Endpoint Snapshot", cards)
            .WithSubtitle("PowerBGInfo-style reusable metric cards with embedded micro visuals.")
            .WithTheme(theme)
            .WithPngOutputScale(outputScale);

        grid.SaveSvg(Path.Combine(output, "powerbginfo-endpoint-snapshot.svg"));
        grid.SaveHtml(Path.Combine(output, "powerbginfo-endpoint-snapshot.html"));
        grid.SavePng(Path.Combine(output, "powerbginfo-endpoint-snapshot.png"));
    }

    private static void SaveReportMetricStrip(string output, int outputScale) {
        var theme = VisualTheme.Report().Configure(builder => {
            builder.Light.Background = builder.Dark.Background = ChartColor.FromHex("#F8FAFC");
            builder.Light.ElevatedSurface = builder.Dark.ElevatedSurface = ChartColor.FromHex("#FFFFFF");
            builder.Light.Surface = builder.Dark.Surface = ChartColor.FromHex("#FFFFFF");
            builder.Light.Border = builder.Dark.Border = ChartColor.FromHex("#EFF4FB");
            builder.Light.PlotBorder = builder.Dark.PlotBorder = ChartColor.FromHex("#D8E1EF");
            builder.Light.Foreground = builder.Dark.Foreground = ChartColor.FromHex("#172033");
            builder.Light.MutedForeground = builder.Dark.MutedForeground = ChartColor.FromHex("#647086");
            builder.Light.Palette = builder.Dark.Palette = ChartPalettes.FromHex("#2563EB", "#14B8A6", "#F59E0B", "#EF4444");
            builder.CardRadius = 14;
            builder.PlotRadius = builder.BarRadius = 8;
        });

        MetricCard Card(string label, string value, string trend, string caption, string symbol, VisualStatus status, double[] history, ChartColor color, bool sparkline) {
            var card = MetricCard.Create()
                .WithSize(300, 170)
                .WithTheme(theme)
                .WithMetric(label, value)
                .WithTrend(trend)
                .WithCaption(caption)
                .WithSymbol(symbol)
                .WithBadgePlacement(MetricCardBadgePlacement.TopLeft)
                .WithStatus(status);
            return sparkline
                ? card.WithMiniSparkline(history, color: color, fillColor: color.WithAlpha(36))
                : card.WithMiniBars(history, maximum: 100, color: color, mutedColor: ChartColor.FromHex("#CBD5E1").WithAlpha(160));
        }

        var cards = new[] {
            Card("Patch Rate", "94%", "+4 pp", "last 30 days", "OK", VisualStatus.Positive, new[] { 80d, 84d, 88d, 91d, 94d }, ChartColor.FromHex("#2563EB"), true),
            Card("Warnings", "18", "-7", "open controls", "WRN", VisualStatus.Warning, new[] { 72d, 64d, 51d, 42d, 35d }, ChartColor.FromHex("#F59E0B"), false),
            Card("Coverage", "1,284", "+96", "assets scanned", "INV", VisualStatus.Info, new[] { 44d, 58d, 63d, 77d, 86d }, ChartColor.FromHex("#14B8A6"), true)
        };

        var grid = VisualGrid.CreateMetricStrip("Report Summary", cards, columns: 3, panelWidth: 300, panelHeight: 170)
            .WithSubtitle("Light-theme metric strip for generated reports, email, and document surfaces.")
            .WithTheme(theme)
            .WithPngOutputScale(outputScale);

        grid.SaveSvg(Path.Combine(output, "report-summary-metric-strip.svg"));
        grid.SaveHtml(Path.Combine(output, "report-summary-metric-strip.html"));
        grid.SavePng(Path.Combine(output, "report-summary-metric-strip.png"));
    }

    private static void SaveTransparentReportMetricStrip(string output, int outputScale) {
        var theme = VisualTheme.TransparentOverlay().Configure(builder => {
            builder.Light.Background = builder.Dark.Background = ChartColor.Transparent;
            builder.Light.ElevatedSurface = builder.Dark.ElevatedSurface = ChartColor.Transparent;
            builder.Light.Surface = builder.Dark.Surface = ChartColor.Transparent;
            builder.Light.Border = builder.Dark.Border = ChartColor.FromRgba(148, 163, 184, 118);
            builder.Light.PlotBorder = builder.Dark.PlotBorder = ChartColor.FromRgba(148, 163, 184, 82);
            builder.Light.Foreground = builder.Dark.Foreground = ChartColor.FromHex("#F8FAFC");
            builder.Light.MutedForeground = builder.Dark.MutedForeground = ChartColor.FromRgba(226, 232, 240, 222);
            builder.Light.Palette = builder.Dark.Palette = ChartPalettes.FromHex("#2DD4BF", "#60A5FA", "#F59E0B", "#FB7185");
            builder.CardRadius = 14;
            builder.PlotRadius = builder.BarRadius = 8;
            builder.CardShadowOpacity = 0;
        });

        MetricCard Card(string label, string value, string trend, string caption, string symbol, VisualStatus status, double[] history, ChartColor color, bool sparkline) {
            var card = MetricCard.Create()
                .WithSize(300, 170)
                .WithTheme(theme)
                .WithMetric(label, value)
                .WithTrend(trend)
                .WithCaption(caption)
                .WithSymbol(symbol)
                .WithBadgePlacement(MetricCardBadgePlacement.TopLeft)
                .WithStatus(status);
            return sparkline
                ? card.WithMiniSparkline(history, color: color, fillColor: color.WithAlpha(46))
                : card.WithMiniBars(history, maximum: 100, color: color, mutedColor: ChartColor.FromRgba(148, 163, 184, 70));
        }

        var cards = new[] {
            Card("Patch Rate", "94%", "+4 pp", "last 30 days", "OK", VisualStatus.Positive, new[] { 80d, 84d, 88d, 91d, 94d }, ChartColor.FromHex("#60A5FA"), true),
            Card("Warnings", "18", "-7", "open controls", "WRN", VisualStatus.Warning, new[] { 72d, 64d, 51d, 42d, 35d }, ChartColor.FromHex("#F59E0B"), false),
            Card("Coverage", "1,284", "+96", "assets scanned", "INV", VisualStatus.Info, new[] { 44d, 58d, 63d, 77d, 86d }, ChartColor.FromHex("#2DD4BF"), true)
        };

        var grid = VisualGrid.CreateMetricStrip("Transparent Report Summary", cards, columns: 3, panelWidth: 300, panelHeight: 170)
            .WithSubtitle("Section and cards render as overlay chrome with transparent surfaces.")
            .WithTheme(theme)
            .WithPngOutputScale(outputScale);

        grid.SaveSvg(Path.Combine(output, "transparent-report-summary-metric-strip.svg"));
        grid.SaveHtml(Path.Combine(output, "transparent-report-summary-metric-strip.html"));
        grid.SavePng(Path.Combine(output, "transparent-report-summary-metric-strip.png"));
    }

    private static void SaveWeeklyProgressDashboard(string output, int outputScale) {
        var theme = WellnessTheme().Configure(builder => {
            builder.Light.Background = builder.Dark.Background = ChartColor.FromHex("#F6F7FA");
            builder.Light.ElevatedSurface = builder.Dark.ElevatedSurface = ChartColor.FromHex("#FFFFFF");
            builder.Light.Surface = builder.Dark.Surface = ChartColor.FromHex("#FFFFFF");
            builder.Light.Border = builder.Dark.Border = ChartColor.FromHex("#F0F2F6");
            builder.Light.PlotBorder = builder.Dark.PlotBorder = ChartColor.FromHex("#E4E7EF");
            builder.Light.Palette = builder.Dark.Palette = ChartPalettes.FromHex("#0F83F7", "#FF7A1A", "#E72CEB", "#52C7E9", "#C98A52");
            builder.CardRadius = 28;
            builder.PlotRadius = builder.BarRadius = 18;
            builder.CardShadowOpacity = 0.055;
        });
        var coolActivityTheme = WellnessTheme()
            .Configure(builder => { builder.Light.Background = builder.Dark.Background = ChartColor.FromHex("#F6F7FA"); builder.Light.ElevatedSurface = builder.Dark.ElevatedSurface = ChartColor.FromHex("#F8FBFF"); builder.Light.Surface = builder.Dark.Surface = ChartColor.FromHex("#FFFFFF"); builder.Light.Border = builder.Dark.Border = ChartColor.FromHex("#E9F3FF"); builder.Light.PlotBorder = builder.Dark.PlotBorder = ChartColor.FromHex("#E0EBF7"); })
            .WithPalette(ChartPalettes.FromHex("#0F83F7", "#FF7A1A", "#E72CEB", "#52C7E9", "#C98A52"))
            .Configure(builder => { builder.CardRadius = 28; builder.PlotRadius = builder.BarRadius = 18; })
            .Configure(builder => { builder.CardShadowOpacity = 0.055; });
        var warmTheme = WellnessTheme()
            .Configure(builder => { builder.Light.Background = builder.Dark.Background = ChartColor.FromHex("#F6F7FA"); builder.Light.ElevatedSurface = builder.Dark.ElevatedSurface = ChartColor.FromHex("#FFF9F3"); builder.Light.Surface = builder.Dark.Surface = ChartColor.FromHex("#FFF9F3"); builder.Light.Border = builder.Dark.Border = ChartColor.FromHex("#F2E7DA"); builder.Light.PlotBorder = builder.Dark.PlotBorder = ChartColor.FromHex("#F0E6DA"); })
            .WithPalette(ChartPalettes.FromHex("#FF7A1A", "#0F83F7", "#E72CEB", "#52C7E9", "#C98A52"))
            .Configure(builder => { builder.CardRadius = 28; builder.PlotRadius = builder.BarRadius = 18; })
            .Configure(builder => { builder.CardShadowOpacity = 0.055; });
        var blue = ChartColor.FromHex("#0F83F7");
        var orange = ChartColor.FromHex("#FF7A1A");

        var week = DateStripBlock.Create()
            .WithSize(760, 158)
            .WithTheme(theme)
            .WithPadding(18)
            .WithHeader("May 9, 2026")
            .AddItem("s", "9", selected: true, color: blue)
            .AddItem("s", "10")
            .AddItem("m", "10")
            .AddItem("t", "12")
            .AddItem("w", "13")
            .AddItem("t", "14")
            .AddItem("f", "15");

        var water = MetricCard.Create()
            .WithSize(360, 140)
            .WithTheme(theme)
            .WithIcon(VisualIcon.Droplet)
            .WithBadgePlacement(MetricCardBadgePlacement.TopLeft)
            .WithMicroVisualSurface(MetricCardMicroVisualSurface.Inset)
            .WithMetric("Litres of water", "4.5", unit: "Litres");

        var calories = MetricCard.Create()
            .WithSize(360, 140)
            .WithTheme(warmTheme)
            .WithIcon(VisualIcon.Flame)
            .WithBadgePlacement(MetricCardBadgePlacement.TopLeft)
            .WithMicroVisualSurface(MetricCardMicroVisualSurface.Inset)
            .WithMetric("Calories", "2.3k", unit: "Kcal");

        var running = MetricCard.Create()
            .WithSize(360, 300)
            .WithTheme(coolActivityTheme)
            .WithMetric("Running", "30 mins")
            .WithMiniSparkline(new[] { 18d, 30d, 34d, 25d, 28d, 43d, 45d, 44d, 48d }, minimum: 0, maximum: 62, color: blue)
            .WithSecondaryMiniSparkline(new[] { 15d, 27d, 31d, 23d, 25d, 40d, 42d, 41d, 45d }, blue.WithAlpha(210))
            .WithMiniSparklineStyle(ChartLineAreaForm.Line)
            .WithMicroVisualPlacement(MetricCardMicroVisualPlacement.Hero)
            .WithMicroVisualSurface(MetricCardMicroVisualSurface.Inset)
            .WithCaption("7-day trend");

        var cycling = MetricCard.Create()
            .WithSize(360, 300)
            .WithTheme(warmTheme)
            .WithMetric("Cycling", "40 mins")
            .WithMiniSparkline(new[] { 16d, 25d, 28d, 21d, 22d, 35d, 38d, 37d, 42d }, minimum: 0, maximum: 62, color: orange)
            .WithSecondaryMiniSparkline(new[] { 13d, 22d, 25d, 18d, 19d, 32d, 35d, 34d, 39d }, orange.WithAlpha(210))
            .WithMiniSparklineStyle(ChartLineAreaForm.Line)
            .WithMicroVisualPlacement(MetricCardMicroVisualPlacement.Hero)
            .WithMicroVisualSurface(MetricCardMicroVisualSurface.Inset)
            .WithCaption("7-day trend");

        var goalsHeader = SectionHeaderBlock.Create()
            .WithTitle("Today's Goals")
            .WithSize(760, 44)
            .WithTheme(theme)
            .WithPadding(0, 4, 0, 0)
            .WithTransparentBackground()
            .WithCard(false);

        var friends = EntityStripBlock.Create()
            .WithTitle("Duel with friends")
            .WithSize(760, 144)
            .WithTheme(theme)
            .WithPadding(18)
            .AddItem("Karrem", color: blue)
            .AddItem("Peter", color: orange)
            .AddItem("Pasel", color: ChartColor.FromHex("#E72CEB"))
            .AddItem("Libura", color: ChartColor.FromHex("#52C7E9"))
            .AddItem("Hakem", color: ChartColor.FromHex("#C98A52"));

        var grid = VisualGrid.Create()
            .WithTitle("Your Weekly Progress")
            .WithColumns(2)
            .WithAdaptiveRowHeights()
            .WithGap(20)
            .WithPadding(30)
            .WithTheme(theme)
            .WithPngOutputScale(outputScale)
            .Add(week, columnSpan: 2)
            .Add(water)
            .Add(calories)
            .Add(goalsHeader, columnSpan: 2)
            .Add(running)
            .Add(cycling)
            .Add(friends, columnSpan: 2);

        grid.SaveSvg(Path.Combine(output, "wellness-weekly-progress-dashboard.svg"));
        grid.SaveHtml(Path.Combine(output, "wellness-weekly-progress-dashboard.html"));
        grid.SavePng(Path.Combine(output, "wellness-weekly-progress-dashboard.png"));
    }

    private static VisualTheme WellnessTheme() => VisualTheme.Minimal().Configure(builder => {
            builder.Light.Background = builder.Dark.Background = Background;
            builder.Light.ElevatedSurface = builder.Dark.ElevatedSurface = Card;
            builder.Light.Surface = builder.Dark.Surface = Card;
            builder.Light.Border = builder.Dark.Border = Track;
            builder.Light.PlotBorder = builder.Dark.PlotBorder = Track;
            builder.Light.Foreground = builder.Dark.Foreground = Text;
            builder.Light.MutedForeground = builder.Dark.MutedForeground = Muted;
            builder.Light.Grid = builder.Dark.Grid = Track;
            builder.Light.Axis = builder.Dark.Axis = Track;
            builder.Light.Palette = builder.Dark.Palette = ChartPalettes.FromHex(Orange.ToHex(), Yellow.ToHex(), Green.ToHex(), "#6A7FDB");
            builder.Typography = new VisualTypography(builder.Typography.Family, titleSize: 28, subtitleSize: 14, axisTitleSize: 12, axisSize: 18, legendSize: 20, dataLabelSize: 20);
            builder.CardRadius = 30;
            builder.PlotRadius = builder.BarRadius = 14;
            builder.CardShadowOpacity = 0.03;
        });
}
