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
    private static readonly ChartColor TicketMint = ChartColor.FromHex("#67D6CC");
    private static readonly ChartColor TicketTeal = ChartColor.FromHex("#24B8A9");
    private static readonly ChartColor TicketLavender = ChartColor.FromHex("#8A7AE6");
    private static readonly ChartColor TicketAmber = ChartColor.FromHex("#F5BF72");
    private static readonly ChartColor TicketRose = ChartColor.FromHex("#F39AAF");

    public static void Write(string output, ChartPngOutputScale pngOutputScale) {
        var scale = (int)pngOutputScale;
        var kpiSparkline = KpiSparkline().Png(p => p.OutputScale((int)pngOutputScale));
        var deviceSplit = DeviceSplit().Png(p => p.OutputScale((int)pngOutputScale));
        var projectActivity = ProjectActivity().Png(p => p.OutputScale((int)pngOutputScale));
        var attendance = AttendanceHexbin().Png(p => p.OutputScale((int)pngOutputScale));
        var mrrTrend = MrrTrend().Png(p => p.OutputScale((int)pngOutputScale));
        var mrrDrivers = MrrDrivers().Png(p => p.OutputScale((int)pngOutputScale));
        var reports = RestaurantReports().Png(p => p.OutputScale((int)pngOutputScale));
        var orderStatus = RestaurantOrderStatus().Png(p => p.OutputScale((int)pngOutputScale));
        var customers = RestaurantCustomersHexbin().Png(p => p.OutputScale((int)pngOutputScale));
        var occupation = RestaurantOccupation().Png(p => p.OutputScale((int)pngOutputScale));
        var weeklySummary = RestaurantWeeklySummary().Png(p => p.OutputScale((int)pngOutputScale));
        var ticketingTrend = TicketingRevenueTrend().Png(p => p.OutputScale((int)pngOutputScale));
        var ticketingPackages = TicketingPackageMix().Png(p => p.OutputScale((int)pngOutputScale));
        var ticketingPromoters = TicketingPromoterAttribution().Png(p => p.OutputScale((int)pngOutputScale));
        var ticketingFunnel = TicketingGuestFunnel().Png(p => p.OutputScale((int)pngOutputScale));
        var hrOverview = HrOverviewGrid(scale);
        var saasOverview = SaasOverviewGrid(scale);
        var restaurantOverview = RestaurantOverviewGrid(scale);
        var ticketingOverview = TicketingAnalyticsGrid(scale);

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
        Save(ticketingTrend, output, "dashboard-ticketing-revenue-trend");
        Save(ticketingPackages, output, "dashboard-ticketing-package-mix");
        Save(ticketingPromoters, output, "dashboard-ticketing-promoter-attribution");
        Save(ticketingFunnel, output, "dashboard-ticketing-guest-funnel");
        Save(hrOverview, output, "dashboard-hr-overview-grid");
        Save(saasOverview, output, "dashboard-saas-mrr-grid");
        Save(restaurantOverview, output, "dashboard-restaurant-overview-grid");
        Save(ticketingOverview, output, "dashboard-ticketing-analytics-grid");

        var grid = VisualGrid.Create()
            .Title("Dashboard Chart Portfolio")
            .Subtitle("Reusable screenshot-inspired KPI, ticketing, attendance, restaurant, and MRR patterns.")
            .Columns(3)
            .Gap(18)
            .Padding(24)
            .FrameVisible()
            .PanelSize(380, 260)
            .PanelFit(VisualPanelFit.Contain)
            .Theme(DashboardTheme())
            .PngOutputScale(scale)
            .Add(kpiSparkline)
            .Add(deviceSplit)
            .Add(projectActivity)
            .Add(attendance, columnSpan: 2, rowSpan: 2)
            .Add(reports, columnSpan: 2)
            .Add(orderStatus)
            .Add(customers)
            .Add(occupation)
            .Add(weeklySummary)
            .Add(ticketingTrend, columnSpan: 2)
            .Add(ticketingPackages)
            .Add(ticketingPromoters)
            .Add(ticketingFunnel)
            .Add(mrrTrend, columnSpan: 2)
            .Add(mrrDrivers, columnSpan: 2);

        grid.SaveSvg(Path.Combine(output, "dashboard-chart-portfolio-grid.svg"));
        grid.SaveHtml(Path.Combine(output, "dashboard-chart-portfolio-grid.html"));
        grid.SavePng(Path.Combine(output, "dashboard-chart-portfolio-grid.png"));
    }

    private static Chart KpiSparkline() => Chart.Create()
        .Title("Average KPI")
        .Subtitle("9/10 company standard  +20%")
        .Size(600, 300)
        .Theme(DashboardTheme())
        .Legend(false)
        .Axes(false)
        .ShowGrid(false)
        .ShowPlotBackground(false)
        .Padding(32, 80, 32, 26)
        .YAxis(y => y.Bounds(0, 10))
        .AddBar("KPI", Points(7.8, 8.1, 8.4, 8.2, 8.8, 8.6, 8.9, 9.1, 8.7, 8.4, 8.9, 9.0, 8.8, 9.2, 8.9, 9.3, 9.1, 9.0), Lime);

    private static Chart DeviceSplit() => Chart.Create()
        .Title("Today Used Devices")
        .Subtitle("Completion rows with reusable progress bars")
        .Size(600, 200)
        .Theme(DashboardTheme())
        .Legend(false)
        .Progress(p => p.ShowValues(true).ShowHandles(false).BarThicknessRatio(0.42).TrackOpacity(0.32))
        .FormatValuesBy(value => value.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "%")
        .AddProgressBars("Device split", new[] {
            new ChartProgressItem("Mobile", 78, Red),
            new ChartProgressItem("Desktop", 22, Lime)
        });

    private static Chart ProjectActivity() => Chart.Create()
        .Title("Projects")
        .Subtitle("Compact activity bars for status cards")
        .Size(600, 300)
        .Theme(DashboardTheme())
        .Legend(false)
        .Axes(false)
        .ShowGrid(false)
        .ShowPlotBackground(false)
        .Padding(32, 80, 32, 28)
        .AddBar("Completed", Points(42, 48, 51, 46, 53, 58, 61, 56, 52, 60, 64, 59, 55, 57, 62, 66, 63, 58), Red);

    private static Chart AttendanceHexbin() {
        var chart = Chart.Create()
            .Title("Attendance Overview")
            .Subtitle("Hexbin heatmap rows for day/time utilization")
            .Size(800, 547)
            .Theme(DashboardTheme())
            .Legend(false)
            .DataLabels(false)
            .Heatmap(heatmap => heatmap.ColorDimension(options => options.ShowLegend = false))
            .XLabels("Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun")
            .FormatValuesBy(value => value.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "%");

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
            .Title("Monthly Recurring Revenue")
            .Subtitle("$120,400  +$8,000")
            .Size(800, 452)
            .Theme(SaasTheme())
            .ShowAxisLines(false)
            .Legend(l => l.Position(ChartLegendPosition.TopRight))
            .XAxis(x => x.LabelDensity(ChartLabelDensity.Dense))
            .XLabels("Oct 1", "Oct 4", "Oct 8", "Oct 11", "Oct 15", "Oct 18", "Oct 22", "Oct 25", "Oct 29")
            .YAxis(y => y.Bounds(100000, 130000))
            .FormatValuesBy(CurrencyK)
            .AddSmoothArea("Actual", Points(actual), Blue)
            .AddTrendLine("Target", target, Slate)
            .AddPointCallout("Oct 22  $119,000 MRR", 7, 118800, Blue)
            .AddVerticalLine(7, string.Empty, Slate);
        return chart;
    }

    private static Chart MrrDrivers() {
        var chart = Chart.Create()
            .Title("MRR Drivers")
            .Subtitle("Net +$8.0k this month")
            .Size(800, 417)
            .Theme(SaasTheme())
            .ShowAxisLines(false)
            .Bars(b => b.Stacked())
            .XAxis(x => x.Bounds(-10, 12))
            .TickCount(6)
            .FormatValuesBy(value => (value < 0 ? "-" : "+") + "$" + Math.Abs(value).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + "K")
            .XLabels("Churn", "Contraction", "Expansion", "New Business")
            .AddHorizontalBar("Growth", Points(0, 0, 4, 8), Green)
            .AddHorizontalBar("Growth buffer", Points(0, 0, 1.2, 1.1), Green)
            .AddHorizontalBar("Drag", Points(-8, -2, 0, 0), Red)
            .AddHorizontalBar("Drag buffer", Points(-1.2, -0.8, 0, 0), Red);
        chart.Series[1].FillPattern(ChartFillPattern.DiagonalBackward).ShowInLegend(false);
        chart.Series[3].FillPattern(ChartFillPattern.DiagonalForward).ShowInLegend(false);
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
            .Title("Reports")
            .Subtitle("Earnings $48,620   Bills $6,820")
            .Size(800, 298)
            .Theme(RestaurantTheme())
            .Legend(false)
            .ShowGrid(false)
            .XAxis(x => x.Visible(false))
            .YAxis(y => y.Visible(false))
            .ShowAxisLines(false)
            .ShowPlotBackground(false)
            .Padding(34, 82, 34, 36)
            .YAxis(y => y.Bounds(15, 95))
            .AddRangeBar("Earnings and bills", intervals, ChartColor.FromHex("#CFCFCA"));
        chart.Series[0]
            .PointColor(20, RestaurantBrown)
            .PointColor(21, RestaurantBrown)
            .PointColor(22, RestaurantBrown)
            .FillPattern(ChartFillPattern.DiagonalBackward)
            .StrokeWidth(2);
        chart.AddPointCallout("Jan 12, 2026  $6,820 / $48,620", 21, 78, RestaurantBrown, ChartDataLabelPlacement.Above);
        return chart;
    }

    private static Chart RestaurantOrderStatus() {
        var chart = Chart.Create()
            .Title("Order Status")
            .Subtitle("New Orders: 120")
            .Size(420, 300)
            .Theme(RestaurantTheme())
            .Bars(b => b.Stacked())
            .Legend(false)
            .Axes(false)
            .ShowPlotBackground(false)
            .Padding(28, 108, 28, 54)
            .XAxis(x => x.Bounds(0, 100))
            .FormatValuesBy(value => value.ToString("0", System.Globalization.CultureInfo.InvariantCulture))
            .AddHorizontalBar("Earnings", Points(48), RestaurantBrown)
            .AddHorizontalBar("Preparing", Points(32), RestaurantOrange)
            .AddHorizontalBar("Served", Points(20), RestaurantPeach);
        foreach (var series in chart.Series) series.FillPattern(ChartFillPattern.DiagonalBackward);
        return chart;
    }

    private static Chart RestaurantCustomersHexbin() {
        var chart = Chart.Create()
            .Title("Customers")
            .Subtitle("Madrid 6.1K   Barcelona 1.6K   Seville 0.4K")
            .Size(420, 300)
            .Theme(RestaurantTheme())
            .Axes(false)
            .Legend(false)
            .ShowPlotBackground(false)
            .DataLabels(false)
            .Heatmap(heatmap => heatmap.ColorDimension(options => options.ShowLegend = false))
            .Padding(34, 70, 34, 26)
            .FormatValuesBy(value => value.ToString("0", System.Globalization.CultureInfo.InvariantCulture));
        chart.AddHexbinHeatmapRows(RestaurantCustomerRows(), RestaurantOrange);
        return chart;
    }

    private static Chart RestaurantOccupation() {
        var chart = Chart.Create()
            .Title("Occupation")
            .Subtitle("Average 45%")
            .Size(420, 300)
            .Theme(RestaurantTheme())
            .Legend(false)
            .ShowGrid(false)
            .ShowAxisLines(false)
            .YAxis(y => y.Visible(false))
            .ShowPlotBackground(false)
            .Padding(34, 84, 34, 38)
            .YAxis(y => y.Bounds(0, 100))
            .XLabels("Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat")
            .AddBar("Occupancy", Points(62, 44, 24, 80, 12, 78, 56), ChartColor.FromHex("#E8E8E4"));
        for (var i = 0; i < 7; i++) chart.Series[0].PointColor(i, i == 3 ? RestaurantOrange : ChartColor.FromHex("#E8E8E4"));
        chart.Series[0].FillPattern(ChartFillPattern.DiagonalBackward);
        chart.AddPointCallout("80%", 4, 80, RestaurantOrange, ChartDataLabelPlacement.Above);
        return chart;
    }

    private static Chart RestaurantWeeklySummary() => Chart.Create()
        .Title("Weekly Summary")
        .Subtitle("$3,397   +4.2%")
        .Size(420, 300)
        .Theme(RestaurantTheme())
        .Legend(false)
        .ShowAxisLines(false)
        .ShowPlotBackground(false)
        .XAxis(x => x.LabelDensity(ChartLabelDensity.Relaxed))
        .YAxis(y => y.Bounds(300, 920))
        .XLabels("Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat")
        .FormatValuesBy(value => "$" + value.ToString("0", System.Globalization.CultureInfo.InvariantCulture))
        .AddSmoothLine("Current", Points(360, 480, 610, 540, 850, 880, 880), RestaurantGold)
            .AddTrendLine("Previous", new[] { new ChartPoint(1, 380), new ChartPoint(7, 750) }, Slate);

    private static Chart TicketingRevenueTrend() {
        var sales = new[] { 146d, 178d, 232d, 288d, 198d, 251d, 321d, 274d, 318d, 346d, 209d, 254d, 302d, 337d };
        var revenue = sales.Select(value => value * 4.15).ToArray();
        var chart = Chart.Create()
            .Title("Detailed Event Analytics")
            .Subtitle("May 12 focus | EUR 42.8K revenue | 2,884 tickets sold")
            .Size(800, 312)
            .Theme(TicketingTheme())
            .DashboardBarPanelStyle()
            .Legend(l => l.Position(ChartLegendPosition.TopRight))
            .YAxis(y => y.Bounds(0, 1550))
            .XAxis(x => x.LabelDensity(ChartLabelDensity.Relaxed))
            .FormatValuesBy(Euros)
            .XLabels("May 01", "May 02", "May 03", "May 04", "May 05", "May 06", "May 07", "May 08", "May 09", "May 10", "May 11", "May 12", "May 13", "May 14")
            .AddBar("Ticket revenue", Points(revenue), TicketMint)
            .AddSmoothLine("Sales pace", Points(sales.Select(value => value * 4.15 + 120).ToArray()), TicketLavender)
            .AddHorizontalLine(1200, "revenue goal", TicketTeal)
            .AddVerticalLine(12, "May 12", TicketTeal)
            .AddPointCallout("EUR 1.2K / 288 sales", 12, 1195, TicketTeal, ChartDataLabelPlacement.Above);

        chart.Series[0]
            .PointColor(3, TicketTeal)
            .PointColor(11, TicketTeal)
            .PointColor(13, TicketTeal);
        return chart;
    }

    private static Chart TicketingPackageMix() => Chart.Create()
        .Title("Package Selling")
        .Subtitle("Arrival-date packages and pick-and-choose inventory")
        .Size(600, 346)
        .Theme(TicketingTheme())
        .Legend(false)
        .Progress(p => p.ShowValues().ShowHandles(false).BarThicknessRatio(0.38).TrackOpacity(0.28))
        .FormatValuesBy(value => value.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "%")
        .AddProgressBars("Packages", new[] {
            new ChartProgressItem("Sunburn Pass", 92, TicketTeal),
            new ChartProgressItem("Beach Team", 76, TicketLavender),
            new ChartProgressItem("VIP Guest", 58, TicketAmber),
            new ChartProgressItem("Free Guest", 34, TicketRose)
        });

    private static Chart TicketingPromoterAttribution() => Chart.Create()
        .Title("Promoter Attribution")
        .Subtitle("Street teams, socials, and partner links by sold tickets")
        .Size(800, 400)
        .Theme(TicketingTheme())
        .DashboardStackedRowStyle(showTotals: true)
        .FormatValuesBy(value => value.ToString("0", System.Globalization.CultureInfo.InvariantCulture))
        .XLabels("Sara Iacino", "Insta Link", "Beach Team", "Partner Bar", "Walk-in")
        .AddHorizontalBar("Paid", Points(164, 182, 124, 88, 66), TicketTeal)
        .AddHorizontalBar("Guestlist", Points(42, 38, 51, 22, 14), TicketLavender)
        .AddHorizontalBar("Upsell", Points(82, 68, 42, 28, 12), TicketAmber);

    private static Chart TicketingGuestFunnel() => Chart.Create()
        .Title("Guest Management Flow")
        .Subtitle("Invite, confirmation, entry, and paid upgrade visibility")
        .Size(600, 346)
        .Theme(TicketingTheme())
        .Legend(false)
        .DashboardBarPanelStyle()
        .XAxis(x => x.Bounds(0, 650))
        .FormatValuesBy(value => value.ToString("0", System.Globalization.CultureInfo.InvariantCulture))
        .XLabels("Invited", "Confirmed", "Arrived", "Paid upgrade")
        .AddHorizontalBar("Guests", Points(620, 482, 376, 118), TicketLavender);

    private static VisualGrid TicketingAnalyticsGrid(int outputScale) => VisualGrid.Create()
        .Title("Event Ticketing Analytics Dashboard")
        .Subtitle("Screenshot-inspired ticket sales, package conversion, promoter attribution, and guest-flow panels.")
        .Columns(4)
        .Gap(16)
        .Padding(24)
        .FrameVisible()
        .PanelSize(320, 170)
        .PanelFit(VisualPanelFit.Stretch)
        .Theme(TicketingTheme())
        .PngOutputScale(outputScale)
        .Add(MetricCard.Create()
            .Title("Ticket Sales")
            .Metric("Sold today", "288")
            .Trend("+12%")
            .Caption("vs yesterday")
            .Tone(Tone.Success)
            .Theme(TicketingTheme())
            .Size(320, 170)
            .Padding(22, 24, 22, 18)
            .AddDetail("Paid", "184", Tone.Success)
            .AddDetail("Guestlist", "42", Tone.Info)
            .MiniBars(new[] { 146d, 178d, 232d, 288d, 198d, 251d }, maximum: 320, color: TicketTeal))
        .Add(MetricCard.Create()
            .Title("Collected")
            .Metric("Checked in", "288")
            .Caption("Beach arrival cohort")
            .Tone(Tone.Info)
            .Theme(TicketingTheme())
            .Size(320, 170)
            .Padding(22, 24, 22, 18)
            .AddDetail("Scan rate", "100%", Tone.Success)
            .AddDetail("No-show risk", "37", Tone.Warning)
            .MiniSparkline(new[] { 40d, 66d, 92d, 138d, 210d, 288d }, color: TicketLavender, fillColor: TicketLavender.WithAlpha(28)))
        .Add(MetricCard.Create()
            .Title("Total Revenue")
            .Metric("Gross", "EUR 42.8K")
            .Trend("+8.4%")
            .Tone(Tone.Success)
            .Theme(TicketingTheme())
            .Size(320, 170)
            .Padding(22, 24, 22, 18)
            .AddDetail("Goal", "EUR 40K", Tone.Success)
            .AddDetail("Avg ticket", "EUR 41.5", Tone.Info)
            .MiniSparkline(new[] { 14d, 18d, 23d, 31d, 36d, 42.8d }, color: TicketTeal, fillColor: TicketMint.WithAlpha(34)))
        .Add(TicketingGuestList().Size(320, 170))
        .Add(TicketingRevenueTrend().Size(800, 214), 4, 2)
        .Add(TicketingPromoterAttribution().Size(600, 326), 2, 2)
        .Add(TicketingPackageMix().Size(600, 326), 2, 2)
        .Add(TicketingGuestFunnel().Size(600, 326), 2, 2)
        .Add(TicketingChannelList().Size(600, 326), 2, 2);

    private static ChartTable TicketingGuestList() => ChartTable.Create()
        .Title("Guest State")
        .Subtitle("Current front-door status.")
        .Theme(TicketingTheme())
        .Dense()
        .ShowHeader(false)
        .RowStriping(false)
        .Padding(26, 28, 26, 24)
        .AddColumn("Segment", width: 136)
        .AddColumn("Type", alignment: TextAlignment.Center, width: 74)
        .AddColumn("Count", alignment: TextAlignment.Right)
        .AddRow("VIP Guests", "", "118")
        .AddRow("Paying Guests", "", "376")
        .AddRow("Free Guests", "", "104")
        .AddRow("No-show Risk", "", "37")
        .UpdateRow(0, row => row.Cells[1].Badge("VIP", Tone.Warning, TicketAmber, VisualBadgeStyle.Outline))
        .UpdateRow(1, row => row.Cells[1].Badge("Paid", Tone.Success, TicketTeal, VisualBadgeStyle.Outline))
        .UpdateRow(2, row => row.Cells[1].Badge("Free", Tone.Info, TicketLavender, VisualBadgeStyle.Outline))
        .UpdateRow(3, row => row.Cells[1].Badge("Risk", Tone.Danger, TicketRose, VisualBadgeStyle.Outline));

    private static WorkloadListBlock TicketingChannelList() => WorkloadListBlock.Create()
        .Title("Tracker Links")
        .Subtitle("Share links, deals, and payout-ready totals.")
        .Theme(TicketingTheme())
        .ShowProgressRails()
        .ShowDividers(false)
        .AddPerson("Instagram tracker", "shop.ticketapp.com/insta", 966, 1000, Tone.Success, "IG", "966", TicketTeal)
        .AddPerson("Beach team QR", "street team handoff", 288, 1000, Tone.Info, "QR", "288", TicketLavender)
        .AddPerson("VIP guest list", "perk invite flow", 118, 1000, Tone.Warning, "VIP", "118", TicketAmber)
        .AddPerson("Cash desk", "walk-in fallback", 66, 1000, Tone.Neutral, "POS", "66", Slate);

    private static VisualGrid RestaurantOverviewGrid(int outputScale) => VisualGrid.Create()
        .Title("Restaurant Dashboard Composition")
        .Subtitle("Reports, order status, customers, occupation, and weekly trend panels.")
        .Columns(3)
        .Gap(18)
        .Padding(28)
        .FrameVisible()
        .PanelSize(420, 300)
        .PanelFit(VisualPanelFit.Contain)
        .Theme(RestaurantTheme())
        .PngOutputScale(outputScale)
        .Add(RestaurantReports().Size(800, 298), 2)
        .Add(RestaurantOrderStatus())
        .Add(RestaurantCustomersHexbin())
        .Add(RestaurantOccupation())
        .Add(RestaurantWeeklySummary());

    private static VisualGrid HrOverviewGrid(int outputScale) => VisualGrid.Create()
        .Title("HR Dashboard Composition")
        .Subtitle("Reusable metrics, progress bars, honeycomb attendance, and table blocks.")
        .Columns(4)
        .Gap(16)
        .Padding(22)
        .FrameVisible()
        .PanelSize(300, 210)
        .PanelFit(VisualPanelFit.Stretch)
        .Theme(DashboardTheme())
        .PngOutputScale(outputScale)
        .Add(KpiSparkline().Size(300, 210))
        .Add(MetricCard.Create()
            .Title("Employee")
            .Metric("Total employee", "1,218")
            .Caption("Active workforce snapshot")
            .Symbol("+")
            .Tone(Tone.Success)
            .AddDetail("Male", "782", Tone.Success)
            .AddDetail("Female", "436", Tone.Info)
            .Theme(DashboardTheme()))
        .Add(DeviceSplit().Size(600, 260), 2)
        .Add(ProjectActivity().Size(600, 260), 2)
        .Add(AttendanceHexbin().Size(600, 420), 2, 2)
        .Add(WorkforceSummaryList().Size(300, 210))
        .Add(OperationsQueueList().Size(300, 210))
        .Add(AttendanceTable(), 4);

    private static VisualGrid SaasOverviewGrid(int outputScale) => VisualGrid.Create()
        .Title("SaaS MRR Dashboard Composition")
        .Subtitle("Trend, target, and driver cards assembled from reusable chart primitives.")
        .Columns(3)
        .Gap(20)
        .Padding(32)
        .FrameVisible()
        .PanelSize(420, 190)
        .PanelFit(VisualPanelFit.Contain)
        .Theme(SaasTheme())
        .PngOutputScale(outputScale)
        .Add(MetricCard.Create().Title("MRR").Metric("Monthly Recurring Revenue", "$120,400").Tone(Tone.Success).AddDetail("Change", "+$8K", Tone.Success).AddDetail("Plan", "$121K", Tone.Info).Theme(SaasTheme()).Size(420, 190).Padding(26, 30, 26, 22))
        .Add(MetricCard.Create().Title("Target").Metric("October target", "$121K").Caption("Tracking above plan").Tone(Tone.Info).AddDetail("Gap", "$0.6K", Tone.Warning).AddDetail("Runway", "98.3%", Tone.Success).Theme(SaasTheme()).Size(420, 190).Padding(26, 30, 26, 22))
        .Add(MetricCard.Create().Title("Net Change").Metric("This month", "+$8.0K").Caption("New business offset churn").Tone(Tone.Success).AddDetail("Adds", "+$12K", Tone.Success).AddDetail("Drag", "-$4K", Tone.Danger).Theme(SaasTheme()).Size(420, 190).Padding(26, 30, 26, 22))
        .Add(MrrTrend().Size(800, 372), 2, 2)
        .Add(DriverSummaryList(), 1, 2)
        .Add(MrrDrivers().Size(800, 372), 2, 2);

    private static ChartList DriverSummaryList() => ChartList.Create()
        .Title("Driver Summary")
        .Subtitle("Status list by driver.")
        .Theme(SaasTheme())
        .Size(420, 400)
        .Padding(26, 30, 26, 24)
        .Dense()
        .Marker(VisualListMarker.Tone)
        .AddStatusItem("New Business", Tone.Success, "+12%")
        .AddStatusItem("Expansion", Tone.Success, "+4%")
        .AddStatusItem("Contraction", Tone.Danger, "-2%")
        .AddStatusItem("Churn", Tone.Danger, "-8%")
        .AddStatusItem("Target gap", Tone.Warning, "$0.6K")
        .AddStatusItem("Forecast", Tone.Info, "$126.4K");

    private static ChartList WorkforceSummaryList() => ChartList.Create()
        .Title("Workforce Mix")
        .Subtitle("Compact reusable status list.")
        .Theme(DashboardTheme())
        .Size(300, 210)
        .Padding(24, 28, 24, 20)
        .Dense()
        .Marker(VisualListMarker.Tone)
        .AddStatusItem("Male employee", Tone.Success, "782")
        .AddStatusItem("Female employee", Tone.Info, "436")
        .AddStatusItem("Active projects", Tone.Warning, "73")
        .AddStatusItem("Done projects", Tone.Success, "3,089");

    private static ChartList OperationsQueueList() => ChartList.Create()
        .Title("Operations Queue")
        .Subtitle("Requests and workflow load.")
        .Theme(DashboardTheme())
        .Size(300, 210)
        .Padding(24, 28, 24, 20)
        .Dense()
        .Marker(VisualListMarker.Tone)
        .AddStatusItem("Notifications", Tone.Danger, "99+")
        .AddStatusItem("Day-off requests", Tone.Warning, "23")
        .AddStatusItem("Recruiting", Tone.Info, "12")
        .AddStatusItem("Attendance alerts", Tone.Success, "4");

    private static ChartTable AttendanceTable() => ChartTable.Create()
        .Title("Attendance Log")
        .Subtitle("Compact operational table block for dashboard grids.")
        .Theme(DashboardTheme())
        .Size(800, 138)
        .Dense()
        .Columns("Employee ID", "Name", "Department", "Status", "Check-in", "Check-out")
        .StatusColumn("Status")
        .AddRow("54253", "Dianne Russell", "Marketing", "Attend", "03:44", "02:45")
        .AddRow("54288", "Marcus Stone", "Projects", "Attend", "03:57", "02:52")
        .AddRow("54312", "Anika Hall", "Recruiting", "Attend", "04:05", "03:10")
        .UpdateRow(0, row => row.Cells[3].Tone = Tone.Success)
        .UpdateRow(1, row => row.Cells[3].Tone = Tone.Success)
        .UpdateRow(2, row => row.Cells[3].Tone = Tone.Success);

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

    private static VisualTheme DashboardTheme() => VisualTheme.Graphite();

    private static VisualTheme SaasTheme() => VisualTheme.Graphite();

    private static VisualTheme RestaurantTheme() => VisualTheme.Graphite();

    private static VisualTheme TicketingTheme() => VisualTheme.Graphite().Configure(builder => {
            builder.Light.Background = builder.Dark.Background = ChartColor.FromHex("#F7F8F8");
            builder.Light.ElevatedSurface = builder.Dark.ElevatedSurface = ChartColor.White;
            builder.Light.Surface = builder.Dark.Surface = ChartColor.White;
            builder.Light.Border = builder.Dark.Border = ChartColor.FromHex("#ECF3F2");
            builder.Light.PlotBorder = builder.Dark.PlotBorder = ChartColor.FromHex("#D8E7E5");
            builder.Light.Foreground = builder.Dark.Foreground = ChartColor.FromHex("#16191D");
            builder.Light.MutedForeground = builder.Dark.MutedForeground = ChartColor.FromHex("#656D76");
            builder.Light.Palette = builder.Dark.Palette = new[] { TicketTeal, TicketLavender, TicketAmber, TicketRose, TicketMint };
            builder.Typography = new VisualTypography(builder.Typography.Family, titleSize: 22, subtitleSize: 11.5, axisTitleSize: 10.5, axisSize: 11, legendSize: 11.5, dataLabelSize: 10.5);
            builder.CardRadius = 18;
            builder.PlotRadius = builder.BarRadius = 8;
            builder.CardShadowOpacity = 0.045;
            builder.MarkerRadius = 4.4;
        });

    private static string Euros(double value) => "EUR " + (value / 1000d).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + "K";
}
