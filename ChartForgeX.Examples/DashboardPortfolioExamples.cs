using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.VisualBlocks;

internal static class DashboardPortfolioExamples {
    private static readonly ChartColor Lime = ChartColor.FromHex("#DDFB20");
    private static readonly ChartColor Green = ChartColor.FromHex("#27C26A");
    private static readonly ChartColor Blue = ChartColor.FromHex("#356AF4");
    private static readonly ChartColor Red = ChartColor.FromHex("#FF3B4F");
    private static readonly ChartColor Slate = ChartColor.FromHex("#667085");
    private static readonly ChartColor RestaurantBrown = ChartColor.FromHex("#C46A23");
    private static readonly ChartColor RestaurantOrange = ChartColor.FromHex("#EF7F22");
    private static readonly ChartColor RestaurantPeach = ChartColor.FromHex("#FFB074");
    private static readonly ChartColor RestaurantGold = ChartColor.FromHex("#D0A400");

    public static void Write(string output, ChartPngOutputScale pngOutputScale) {
        var scale = (int)pngOutputScale;
        var kpiSparkline = KpiSparkline().WithPngOutputScale(pngOutputScale);
        var deviceSplit = DeviceSplit().WithPngOutputScale(pngOutputScale);
        var projectActivity = ProjectActivity().WithPngOutputScale(pngOutputScale);
        var attendance = AttendanceHexbin().WithPngOutputScale(pngOutputScale);
        var mrrTrend = MrrTrend().WithPngOutputScale(pngOutputScale);
        var mrrDrivers = MrrDrivers().WithPngOutputScale(pngOutputScale);
        var reports = RestaurantReports().WithPngOutputScale(pngOutputScale);
        var orderStatus = RestaurantOrderStatus().WithPngOutputScale(pngOutputScale);
        var customers = RestaurantCustomersHexbin().WithPngOutputScale(pngOutputScale);
        var occupation = RestaurantOccupation().WithPngOutputScale(pngOutputScale);
        var weeklySummary = RestaurantWeeklySummary().WithPngOutputScale(pngOutputScale);
        var hrOverview = HrOverviewGrid(scale);
        var saasOverview = SaasOverviewGrid(scale);
        var restaurantOverview = RestaurantOverviewGrid(scale);

        Save(kpiSparkline, output, "dashboard-kpi-bar-sparkline");
        Save(deviceSplit, output, "dashboard-device-progress-bars");
        Save(projectActivity, output, "dashboard-project-activity-sparkline");
        Save(attendance, output, "dashboard-attendance-hexbin-heatmap");
        Save(mrrTrend, output, "dashboard-mrr-trend-card");
        Save(mrrDrivers, output, "dashboard-mrr-driver-bars");
        Save(reports, output, "dashboard-restaurant-reports-range-strip");
        Save(orderStatus, output, "dashboard-restaurant-order-status");
        Save(customers, output, "dashboard-restaurant-customers-hexbin");
        Save(occupation, output, "dashboard-restaurant-occupation-bars");
        Save(weeklySummary, output, "dashboard-restaurant-weekly-summary");
        Save(hrOverview, output, "dashboard-hr-overview-grid");
        Save(saasOverview, output, "dashboard-saas-mrr-grid");
        Save(restaurantOverview, output, "dashboard-restaurant-overview-grid");

        var grid = VisualGrid.Create()
            .WithTitle("Dashboard Chart Portfolio")
            .WithSubtitle("Reusable screenshot-inspired KPI, attendance, restaurant, and MRR patterns.")
            .WithColumns(3)
            .WithGap(18)
            .WithPadding(24)
            .WithFrame()
            .WithPanelSize(380, 260)
            .WithPanelFit(VisualGridPanelFit.Contain)
            .WithTheme(DashboardTheme())
            .WithPngOutputScale(scale)
            .Add(kpiSparkline)
            .Add(deviceSplit)
            .Add(projectActivity)
            .Add(attendance, columnSpan: 2, rowSpan: 2)
            .Add(reports, columnSpan: 2)
            .Add(orderStatus)
            .Add(customers)
            .Add(occupation)
            .Add(weeklySummary)
            .Add(mrrTrend, columnSpan: 2)
            .Add(mrrDrivers, columnSpan: 2);

        grid.SaveSvg(Path.Combine(output, "dashboard-chart-portfolio-grid.svg"));
        grid.SaveHtml(Path.Combine(output, "dashboard-chart-portfolio-grid.html"));
        grid.SavePng(Path.Combine(output, "dashboard-chart-portfolio-grid.png"));
    }

    private static Chart KpiSparkline() => Chart.Create()
        .WithTitle("Average KPI")
        .WithSubtitle("9/10 company standard  +20%")
        .WithSize(520, 260)
        .WithTheme(DashboardTheme())
        .WithLegend(false)
        .WithAxes(false)
        .WithGrid(false)
        .WithPlotBackground(false)
        .WithPadding(32, 80, 32, 26)
        .WithYAxisBounds(0, 10)
        .AddBar("KPI", Points(7.8, 8.1, 8.4, 8.2, 8.8, 8.6, 8.9, 9.1, 8.7, 8.4, 8.9, 9.0, 8.8, 9.2, 8.9, 9.3, 9.1, 9.0), Lime);

    private static Chart DeviceSplit() => Chart.Create()
        .WithTitle("Today Used Devices")
        .WithSubtitle("Completion rows with reusable progress bars")
        .WithSize(520, 260)
        .WithTheme(DashboardTheme())
        .WithLegend(false)
        .WithProgressValues(true)
        .WithProgressHandles(false)
        .WithProgressBarThickness(0.42)
        .WithProgressTrackOpacity(0.32)
        .WithValueFormatter(value => value.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "%")
        .AddProgressBars("Device split", new[] {
            new ChartProgressItem("Mobile", 78, Red),
            new ChartProgressItem("Desktop", 22, Lime)
        });

    private static Chart ProjectActivity() => Chart.Create()
        .WithTitle("Projects")
        .WithSubtitle("Compact activity bars for status cards")
        .WithSize(520, 260)
        .WithTheme(DashboardTheme())
        .WithLegend(false)
        .WithAxes(false)
        .WithGrid(false)
        .WithPlotBackground(false)
        .WithPadding(32, 80, 32, 28)
        .AddBar("Completed", Points(42, 48, 51, 46, 53, 58, 61, 56, 52, 60, 64, 59, 55, 57, 62, 66, 63, 58), Red);

    private static Chart AttendanceHexbin() {
        var chart = Chart.Create()
            .WithTitle("Attendance Overview")
            .WithSubtitle("Hexbin heatmap rows for day/time utilization")
            .WithSize(760, 520)
            .WithTheme(DashboardTheme())
            .WithLegend(false)
            .WithDataLabels(false)
            .WithHeatmapScaleLegend(false)
            .WithXLabels("Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun")
            .WithValueFormatter(value => value.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "%");

        chart.AddHexbinHeatmapRows(new[] {
            ChartHeatmapRow.CreateMasked("07:00", null, 58, 67, 71, 66, 38, null),
            ChartHeatmapRow.CreateMasked("09:00", 68, 82, 91, 94, 89, 58, null),
            ChartHeatmapRow.CreateMasked("11:00", 74, 88, 96, 98, 93, 64, 42),
            ChartHeatmapRow.Create("13:00", 70, 84, 92, 95, 91, 61, 40),
            ChartHeatmapRow.CreateMasked("15:00", 63, 79, 88, 91, 86, 55, null),
            ChartHeatmapRow.CreateMasked("17:00", null, 66, 78, 82, 76, 45, null),
            ChartHeatmapRow.CreateMasked("19:00", null, null, 58, 61, 52, null, null)
        }, Lime);
        return chart;
    }

    private static Chart MrrTrend() {
        var actual = new[] { 103400d, 106500d, 106500d, 114000d, 111800d, 119000d, 118800d, 122600d, 126400d };
        var target = new[] { new ChartPoint(1, 106000), new ChartPoint(9, 120800) };
        var chart = Chart.Create()
            .WithTitle("Monthly Recurring Revenue")
            .WithSubtitle("$120,400  +$8,000")
            .WithSize(920, 520)
            .WithTheme(SaasTheme())
            .WithAxisLines(false)
            .WithLegendPosition(ChartLegendPosition.TopRight)
            .WithXAxisLabelDensity(ChartLabelDensity.Dense)
            .WithXLabels("OCT 1", "OCT 4", "OCT 8", "OCT 11", "OCT 15", "OCT 18", "OCT 22", "OCT 25", "OCT 29")
            .WithYAxisBounds(100000, 130000)
            .WithValueFormatter(CurrencyK)
            .AddSmoothArea("Actual", Points(actual), Blue)
            .AddTrendLine("Target", target, Slate)
            .AddPointCallout("Oct 22  $119,000 MRR", 7, 118800, Blue)
            .AddVerticalLine(7, string.Empty, Slate);
        return chart;
    }

    private static Chart MrrDrivers() {
        var chart = Chart.Create()
            .WithTitle("MRR Drivers")
            .WithSubtitle("Net +$8.0k this month")
            .WithSize(920, 480)
            .WithTheme(SaasTheme())
            .WithAxisLines(false)
            .WithStackedHorizontalBars()
            .WithXAxisBounds(-10, 12)
            .WithTickCount(6)
            .WithValueFormatter(value => (value < 0 ? "-" : "+") + "$" + Math.Abs(value).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + "K")
            .WithXLabels("Churn", "Contraction", "Expansion", "New Business")
            .AddHorizontalBar("Growth", Points(0, 0, 4, 8), Green)
            .AddHorizontalBar("Growth buffer", Points(0, 0, 1.2, 1.1), Green)
            .AddHorizontalBar("Drag", Points(-8, -2, 0, 0), Red)
            .AddHorizontalBar("Drag buffer", Points(-1.2, -0.8, 0, 0), Red);
        chart.Series[1].WithFillPattern(ChartFillPattern.DiagonalBackward).WithLegendEntry(false);
        chart.Series[3].WithFillPattern(ChartFillPattern.DiagonalForward).WithLegendEntry(false);
        return chart;
    }

    private static Chart RestaurantReports() {
        var intervals = Enumerable.Range(1, 42)
            .Select(index => {
                var wave = Math.Sin(index * 0.47) * 9 + Math.Cos(index * 0.21) * 5;
                var center = 55 + wave;
                return new ChartInterval(index, center - 18 - index % 4, center + 18 + index % 3);
            })
            .ToArray();
        var chart = Chart.Create()
            .WithTitle("Reports")
            .WithSubtitle("Earnings $48,620   Bills $6,820")
            .WithSize(860, 320)
            .WithTheme(RestaurantTheme())
            .WithLegend(false)
            .WithGrid(false)
            .WithXAxisVisible(false)
            .WithYAxisVisible(false)
            .WithAxisLines(false)
            .WithPlotBackground(false)
            .WithPadding(34, 82, 34, 36)
            .WithYAxisBounds(15, 95)
            .AddRangeBar("Earnings and bills", intervals, ChartColor.FromHex("#CFCFCA"));
        chart.Series[0]
            .WithPointColor(20, RestaurantBrown)
            .WithPointColor(21, RestaurantBrown)
            .WithPointColor(22, RestaurantBrown)
            .WithFillPattern(ChartFillPattern.DiagonalBackward)
            .WithStrokeWidth(2);
        chart.AddPointCallout("Jan 12, 2026  $6,820 / $48,620", 21, 78, RestaurantBrown, ChartDataLabelPlacement.Above);
        return chart;
    }

    private static Chart RestaurantOrderStatus() {
        var chart = Chart.Create()
            .WithTitle("Order Status")
            .WithSubtitle("New Orders: 120")
            .WithSize(420, 300)
            .WithTheme(RestaurantTheme())
            .WithStackedHorizontalBars()
            .WithLegend(false)
            .WithAxes(false)
            .WithPlotBackground(false)
            .WithPadding(28, 108, 28, 54)
            .WithXAxisBounds(0, 100)
            .WithValueFormatter(value => value.ToString("0", System.Globalization.CultureInfo.InvariantCulture))
            .AddHorizontalBar("Earnings", Points(48), RestaurantBrown)
            .AddHorizontalBar("Preparing", Points(32), RestaurantOrange)
            .AddHorizontalBar("Served", Points(20), RestaurantPeach);
        foreach (var series in chart.Series) series.WithFillPattern(ChartFillPattern.DiagonalBackward);
        return chart;
    }

    private static Chart RestaurantCustomersHexbin() {
        var chart = Chart.Create()
            .WithTitle("Customers")
            .WithSubtitle("Madrid 6.1K   Barcelona 1.6K   Seville 0.4K")
            .WithSize(420, 300)
            .WithTheme(RestaurantTheme())
            .WithAxes(false)
            .WithLegend(false)
            .WithPlotBackground(false)
            .WithDataLabels(false)
            .WithHeatmapScaleLegend(false)
            .WithPadding(34, 70, 34, 26)
            .WithValueFormatter(value => value.ToString("0", System.Globalization.CultureInfo.InvariantCulture));
        chart.AddHexbinHeatmapRows(RestaurantCustomerRows(), RestaurantOrange);
        return chart;
    }

    private static Chart RestaurantOccupation() {
        var chart = Chart.Create()
            .WithTitle("Occupation")
            .WithSubtitle("Average 45%")
            .WithSize(420, 300)
            .WithTheme(RestaurantTheme())
            .WithLegend(false)
            .WithGrid(false)
            .WithAxisLines(false)
            .WithYAxisVisible(false)
            .WithPlotBackground(false)
            .WithPadding(34, 84, 34, 38)
            .WithYAxisBounds(0, 100)
            .WithXLabels("Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat")
            .AddBar("Occupancy", Points(62, 44, 24, 80, 12, 78, 56), ChartColor.FromHex("#E8E8E4"));
        for (var i = 0; i < 7; i++) chart.Series[0].WithPointColor(i, i == 3 ? RestaurantOrange : ChartColor.FromHex("#E8E8E4"));
        chart.Series[0].WithFillPattern(ChartFillPattern.DiagonalBackward);
        chart.AddPointCallout("80%", 4, 80, RestaurantOrange, ChartDataLabelPlacement.Above);
        return chart;
    }

    private static Chart RestaurantWeeklySummary() => Chart.Create()
        .WithTitle("Weekly Summary")
        .WithSubtitle("$3,397   +4.2%")
        .WithSize(420, 300)
        .WithTheme(RestaurantTheme())
        .WithLegend(false)
        .WithAxisLines(false)
        .WithPlotBackground(false)
        .WithXAxisLabelDensity(ChartLabelDensity.Relaxed)
        .WithYAxisBounds(300, 920)
        .WithXLabels("Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat")
        .WithValueFormatter(value => "$" + value.ToString("0", System.Globalization.CultureInfo.InvariantCulture))
        .AddSmoothLine("Current", Points(360, 480, 610, 540, 850, 880, 880), RestaurantGold)
            .AddTrendLine("Previous", new[] { new ChartPoint(1, 380), new ChartPoint(7, 750) }, Slate);

    private static VisualGrid RestaurantOverviewGrid(int outputScale) => VisualGrid.Create()
        .WithTitle("Restaurant Dashboard Composition")
        .WithSubtitle("Reports, order status, customers, occupation, and weekly trend panels.")
        .WithColumns(3)
        .WithGap(18)
        .WithPadding(28)
        .WithFrame()
        .WithPanelSize(420, 300)
        .WithPanelFit(VisualGridPanelFit.Contain)
        .WithTheme(RestaurantTheme())
        .WithPngOutputScale(outputScale)
        .Add(RestaurantReports().WithSize(860, 320), 2)
        .Add(RestaurantOrderStatus())
        .Add(RestaurantCustomersHexbin())
        .Add(RestaurantOccupation())
        .Add(RestaurantWeeklySummary());

    private static VisualGrid HrOverviewGrid(int outputScale) => VisualGrid.Create()
        .WithTitle("HR Dashboard Composition")
        .WithSubtitle("Reusable metrics, progress bars, honeycomb attendance, and table blocks.")
        .WithColumns(4)
        .WithGap(16)
        .WithPadding(22)
        .WithFrame()
        .WithPanelSize(300, 210)
        .WithPanelFit(VisualGridPanelFit.Stretch)
        .WithTheme(DashboardTheme())
        .WithPngOutputScale(outputScale)
        .Add(KpiSparkline().WithSize(300, 210))
        .Add(MetricCard.Create()
            .WithTitle("Employee")
            .WithMetric("Total employee", "1,218")
            .WithCaption("Active workforce snapshot")
            .WithSymbol("+")
            .WithStatus(VisualStatus.Positive)
            .AddDetail("Male", "782", VisualStatus.Positive)
            .AddDetail("Female", "436", VisualStatus.Info)
            .WithTheme(DashboardTheme()))
        .Add(DeviceSplit().WithSize(600, 260), 2)
        .Add(ProjectActivity().WithSize(600, 260), 2)
        .Add(AttendanceHexbin().WithSize(600, 420), 2, 2)
        .Add(WorkforceSummaryList().WithSize(300, 210))
        .Add(OperationsQueueList().WithSize(300, 210))
        .Add(AttendanceTable(), 4);

    private static VisualGrid SaasOverviewGrid(int outputScale) => VisualGrid.Create()
        .WithTitle("SaaS MRR Dashboard Composition")
        .WithSubtitle("Trend, target, and driver cards assembled from reusable chart primitives.")
        .WithColumns(3)
        .WithGap(20)
        .WithPadding(32)
        .WithFrame()
        .WithPanelSize(420, 190)
        .WithPanelFit(VisualGridPanelFit.Contain)
        .WithTheme(SaasTheme())
        .WithPngOutputScale(outputScale)
        .Add(MetricCard.Create().WithTitle("MRR").WithMetric("Monthly Recurring Revenue", "$120,400").WithStatus(VisualStatus.Positive).AddDetail("Change", "+$8K", VisualStatus.Positive).AddDetail("Plan", "$121K", VisualStatus.Info).WithTheme(SaasTheme()).WithSize(420, 190).WithPadding(26, 30, 26, 22))
        .Add(MetricCard.Create().WithTitle("Target").WithMetric("October target", "$121K").WithCaption("Tracking above plan").WithStatus(VisualStatus.Info).AddDetail("Gap", "$0.6K", VisualStatus.Warning).AddDetail("Runway", "98.3%", VisualStatus.Positive).WithTheme(SaasTheme()).WithSize(420, 190).WithPadding(26, 30, 26, 22))
        .Add(MetricCard.Create().WithTitle("Net Change").WithMetric("This month", "+$8.0K").WithCaption("New business offset churn").WithStatus(VisualStatus.Positive).AddDetail("Adds", "+$12K", VisualStatus.Positive).AddDetail("Drag", "-$4K", VisualStatus.Negative).WithTheme(SaasTheme()).WithSize(420, 190).WithPadding(26, 30, 26, 22))
        .Add(MrrTrend().WithSize(860, 400), 2, 2)
        .Add(DriverSummaryList(), 1, 2)
        .Add(MrrDrivers().WithSize(860, 400), 2, 2);

    private static ChartList DriverSummaryList() => ChartList.Create()
        .WithTitle("Driver Summary")
        .WithSubtitle("Status list by driver.")
        .WithTheme(SaasTheme())
        .WithSize(420, 400)
        .WithPadding(26, 30, 26, 24)
        .WithDenseMode()
        .WithMarker(VisualListMarker.Status)
        .AddStatusItem("New Business", VisualStatus.Positive, "+12%")
        .AddStatusItem("Expansion", VisualStatus.Positive, "+4%")
        .AddStatusItem("Contraction", VisualStatus.Negative, "-2%")
        .AddStatusItem("Churn", VisualStatus.Negative, "-8%")
        .AddStatusItem("Target gap", VisualStatus.Warning, "$0.6K")
        .AddStatusItem("Forecast", VisualStatus.Info, "$126.4K");

    private static ChartList WorkforceSummaryList() => ChartList.Create()
        .WithTitle("Workforce Mix")
        .WithSubtitle("Compact reusable status list.")
        .WithTheme(DashboardTheme())
        .WithSize(300, 210)
        .WithPadding(24, 28, 24, 20)
        .WithDenseMode()
        .WithMarker(VisualListMarker.Status)
        .AddStatusItem("Male employee", VisualStatus.Positive, "782")
        .AddStatusItem("Female employee", VisualStatus.Info, "436")
        .AddStatusItem("Active projects", VisualStatus.Warning, "73")
        .AddStatusItem("Done projects", VisualStatus.Positive, "3,089");

    private static ChartList OperationsQueueList() => ChartList.Create()
        .WithTitle("Operations Queue")
        .WithSubtitle("Requests and workflow load.")
        .WithTheme(DashboardTheme())
        .WithSize(300, 210)
        .WithPadding(24, 28, 24, 20)
        .WithDenseMode()
        .WithMarker(VisualListMarker.Status)
        .AddStatusItem("Notifications", VisualStatus.Negative, "99+")
        .AddStatusItem("Day-off requests", VisualStatus.Warning, "23")
        .AddStatusItem("Recruiting", VisualStatus.Info, "12")
        .AddStatusItem("Attendance alerts", VisualStatus.Positive, "4");

    private static ChartTable AttendanceTable() => ChartTable.Create()
        .WithTitle("Attendance Log")
        .WithSubtitle("Compact operational table block for dashboard grids.")
        .WithTheme(DashboardTheme())
        .WithSize(1220, 210)
        .WithDenseMode()
        .WithColumns("Employee ID", "Name", "Department", "Status", "Check-in", "Check-out")
        .WithStatusColumn("Status")
        .AddRow("54253", "Dianne Russell", "Marketing", "Attend", "03:44", "02:45")
        .AddRow("54288", "Marcus Stone", "Projects", "Attend", "03:57", "02:52")
        .AddRow("54312", "Anika Hall", "Recruiting", "Attend", "04:05", "03:10")
        .WithRow(0, row => row.Cells[3].Status = VisualStatus.Positive)
        .WithRow(1, row => row.Cells[3].Status = VisualStatus.Positive)
        .WithRow(2, row => row.Cells[3].Status = VisualStatus.Positive);

    private static void Save(Chart chart, string output, string name) {
        chart.SaveSvg(Path.Combine(output, name + ".svg"));
        chart.SaveHtml(Path.Combine(output, name + ".html"));
        chart.SavePng(Path.Combine(output, name + ".png"));
    }

    private static void Save(VisualGrid grid, string output, string name) {
        grid.SaveSvg(Path.Combine(output, name + ".svg"));
        grid.SaveHtml(Path.Combine(output, name + ".html"));
        grid.SavePng(Path.Combine(output, name + ".png"));
    }

    private static IEnumerable<ChartPoint> Points(params double[] y) {
        for (var i = 0; i < y.Length; i++) yield return new ChartPoint(i + 1, y[i]);
    }

    private static IEnumerable<ChartHeatmapRow> RestaurantCustomerRows() {
        const int size = 11;
        var center = (size - 1) / 2.0;
        for (var row = 0; row < size; row++) {
            var values = new double?[size];
            for (var column = 0; column < size; column++) {
                var dx = column - center + (row % 2 == 1 ? 0.42 : 0);
                var dy = (row - center) * 0.92;
                var distance = Math.Sqrt(dx * dx + dy * dy);
                var edgeWave = Math.Sin((row + 1) * 1.7) * 0.22 + Math.Cos((column + 2) * 1.3) * 0.16;
                if (distance > 5.12 + edgeWave) {
                    values[column] = null;
                    continue;
                }

                var density = Math.Max(8, 98 - distance * 17 + Math.Sin((row + 1) * (column + 2)) * 9);
                values[column] = Math.Min(100, density);
            }

            yield return ChartHeatmapRow.CreateMasked("R" + (row + 1).ToString(System.Globalization.CultureInfo.InvariantCulture), values);
        }
    }

    private static string CurrencyK(double value) => "$" + (value / 1000d).ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "K";

    private static ChartTheme DashboardTheme() => ChartTheme.DashboardLight();

    private static ChartTheme SaasTheme() => ChartTheme.SaasDashboardLight();

    private static ChartTheme RestaurantTheme() => ChartTheme.RestaurantDashboardLight();
}
