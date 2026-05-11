using System;
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.VisualBlocks;

internal static class DashboardPatternExamples {
    private static readonly ChartColor Teal = ChartColor.FromHex("#10A7BD");
    private static readonly ChartColor TealDark = ChartColor.FromHex("#08798C");
    private static readonly ChartColor Mint = ChartColor.FromHex("#5FD3D9");
    private static readonly ChartColor Green = ChartColor.FromHex("#36C47D");
    private static readonly ChartColor Blue = ChartColor.FromHex("#5EA2F6");
    private static readonly ChartColor Purple = ChartColor.FromHex("#7057E6");
    private static readonly ChartColor Orange = ChartColor.FromHex("#FFB05C");
    private static readonly ChartColor Red = ChartColor.FromHex("#DE442F");
    private static readonly ChartColor SoftGray = ChartColor.FromHex("#D9DCE3");

    public static void Write(string output, ChartPngOutputScale pngOutputScale) {
        var scale = (int)pngOutputScale;

        Save(AppointmentOperationsGrid(scale), output, "dashboard-appointment-operations-grid");
        Save(ProjectProgressCard(scale), output, "dashboard-project-progress-card");
        Save(ProjectTaskCompositionCard(scale), output, "dashboard-project-task-composition-card");
        Save(ProjectTrackCard().WithPngOutputScale(pngOutputScale), output, "dashboard-project-track-card");
        Save(ProjectScheduleTimeline().WithPngOutputScale(pngOutputScale), output, "dashboard-project-schedule-timeline");
        Save(ShipmentActivityPanel(scale), output, "dashboard-shipment-activity-panel");
        Save(HrOperationsGrid(scale), output, "dashboard-hr-operations-grid");
        Save(PaymentAnalyticsGrid(scale), output, "dashboard-payment-analytics-grid");
    }

    public static void WriteShipmentActivityPanel(string output, ChartPngOutputScale pngOutputScale) {
        Save(ShipmentActivityPanel((int)pngOutputScale), output, "dashboard-shipment-activity-panel");
    }

    public static void WriteProjectProgressCard(string output, ChartPngOutputScale pngOutputScale) {
        Save(ProjectProgressCard((int)pngOutputScale), output, "dashboard-project-progress-card");
    }

    private static VisualGrid AppointmentOperationsGrid(int outputScale) => VisualGrid.Create()
        .WithTitle("Appointment Operations Dashboard")
        .WithSubtitle("Heatmap, peak-hour bars, workload rows, and availability list from the supplied operations design.")
        .WithColumns(4)
        .WithGap(16)
        .WithPadding(24)
        .WithFrame()
        .WithPanelSize(320, 220)
        .WithPanelFit(VisualGridPanelFit.Stretch)
        .WithTheme(OperationsTheme())
        .WithPngOutputScale(outputScale)
        .Add(AppointmentVolume().WithSize(656, 390), 2, 2)
        .Add(PeakHourReview().WithSize(656, 360), 2, 2)
        .Add(StaffWorkload().WithSize(656, 330), 2, 2)
        .Add(AppointmentTotalCard().WithPngOutputScale(outputScale))
        .Add(AppointmentUnassignedCard().WithPngOutputScale(outputScale))
        .Add(AvailableStaffList().WithSize(656, 260), 2, 2)
        .Add(AppointmentBacklogList().WithSize(656, 260), 2, 2);

    private static HeatmapInsightCard AppointmentVolume() => HeatmapInsightCard.Create()
            .WithTitle("Appointment Volume")
            .WithSubtitle("Week 1, Jan 1 - Jan 7")
            .WithTheme(OperationsTheme())
            .WithPadding(28, 26, 28, 24)
            .WithControls("Day", "Week", "Week 1 (Jan 1 - Jan 7, 2024)")
            .WithColumns("S", "M", "T", "W", "T", "F", "S")
            .WithColorKey(0, 12, Mint.WithAlpha(70), TealDark)
            .AddRow("9 AM", 9, 3, 2, 6, 4, 4, 12)
            .AddRow("10 AM", 11, 2, 4, 6, 7, 6, 9)
            .AddRow("11 AM", 9, 3, 2, 4, 4, 6, 12)
            .AddRow("12 PM", 9, 2, 4, 6, 4, 4, 11)
            .AddRow("1 PM", 12, 4, 3, 5, 7, 9, 10)
            .AddRow("2 PM", 9, 2, 4, 5, 7, 6, 11)
            .AddRow("3 PM", 12, 1, 3, 6, 6, 4, 9)
            .AddRow("4 PM", 11, 5, 2, 6, 7, 6, 12)
            .AddRow("5 PM", 9, 2, 4, 4, 4, 6, 4)
            .AddInsight("Fri, 5 PM - 6 PM", "16 appointments")
            .AddInsight("Mon, 7 PM - 9 PM", "12 appointments")
            .AddInsight("Sun, 8 PM - 10 PM", "8 appointments");

    private static Chart PeakHourReview() {
        var chart = Chart.Create()
            .WithTitle("Review Peak Hour")
            .WithSubtitle("Highlighted review concentration by hour")
            .WithTheme(OperationsTheme())
            .WithLegend(false)
            .WithDashboardBarPanelStyle()
            .WithYAxisVisible(false)
            .WithAxisLines(false)
            .WithValueFormatter(value => value.ToString("0", System.Globalization.CultureInfo.InvariantCulture))
            .WithXLabels("8am", "9am", "10am", "11am", "12pm", "1pm", "2pm", "3pm", "4pm", "5pm", "6pm", "7pm", "8pm")
            .WithHighlightedXAxisRange(7.5, 11.5, Red, 0.08, "review-peak-window")
            .AddBar("Reviews", Points(3, 1, 3, 1, 0, 0, 0, 9, 10, 9, 7, 0, 0), SoftGray);

        chart.Series[0].WithPointColorRange(7, 4, Red);
        return chart;
    }

    private static WorkloadListBlock StaffWorkload() => WorkloadListBlock.Create()
        .WithTitle("Today Staff Workload")
        .WithSubtitle("Capacity share by staff member")
        .WithTheme(OperationsTheme())
        .AddPerson("Panji Dwi", "Zumba Trainer", 4, 8, VisualStatus.Neutral, "PD", "4/8", TealDark)
        .AddPerson("Raihan Fikri", "Aerobik Trainer", 10, 8, VisualStatus.Negative, "RF", "10/8", Red, "Overload")
        .AddPerson("Rijal Jatnika", "Personal Trainer", 7, 8, VisualStatus.Neutral, "RJ", "7/8", TealDark)
        .AddPerson("Mufti Hidayat", "Massage Specialist", 6, 8, VisualStatus.Neutral, "MH", "6/8", TealDark);

    private static MetricCard AppointmentTotalCard() => MetricCard.Create()
        .WithTitle("Upcoming Appointment")
        .WithMetric("Total Appointment", "24")
        .WithTrend("+12%")
        .WithCaption("vs last week")
        .WithStatus(VisualStatus.Positive)
        .WithTheme(OperationsTheme())
        .WithMiniBars(new[] { 42d, 58d, 61d, 72d, 83d }, maximum: 100, color: Teal);

    private static MetricCard AppointmentUnassignedCard() => MetricCard.Create()
        .WithTitle("Unassigned")
        .WithMetric("Needs owner", "6")
        .WithTrend("-12%")
        .WithCaption("vs last week")
        .WithStatus(VisualStatus.Negative)
        .WithTheme(OperationsTheme())
        .WithMiniBars(new[] { 31d, 28d, 24d, 22d, 18d }, maximum: 100, color: Red);

    private static ChartList AvailableStaffList() => ChartList.Create()
        .WithTitle("Available Staff")
        .WithSubtitle("Shift-ready staff list with service labels.")
        .WithTheme(OperationsTheme())
        .WithMarker(VisualListMarker.Status)
        .WithDenseMode()
        .AddStatusItem("Raihan Fikri", VisualStatus.Positive, "Zumba")
        .AddStatusItem("Wildan Rizal", VisualStatus.Info, "Special Zumba Package")
        .AddStatusItem("Panji Dwi", VisualStatus.Positive, "Special Massage")
        .AddStatusItem("Wildan Rizal", VisualStatus.Neutral, "Reflexology Therapy");

    private static ChartList AppointmentBacklogList() => ChartList.Create()
        .WithTitle("AI Prediction")
        .WithSubtitle("Peak 1h30m from 14:00-18:00.")
        .WithTheme(OperationsTheme())
        .WithMarker(VisualListMarker.Status)
        .WithDenseMode()
        .AddStatusItem("Stay estimate", VisualStatus.Info, "1-2h")
        .AddStatusItem("Busy time", VisualStatus.Warning, "Fri 5 PM - 6 PM")
        .AddStatusItem("Second peak", VisualStatus.Neutral, "Mon 7 PM - 9 PM")
        .AddStatusItem("Suggested action", VisualStatus.Positive, "Manage shift");

    private static SegmentedProgressCard ProjectProgressCard(int outputScale) => SegmentedProgressCard.Create()
        .WithTitle("Project Progress")
        .WithSubtitle("Overall completion rate all projects.")
        .WithSize(820, 360)
        .WithTheme(ProjectTheme())
        .WithPngOutputScale(outputScale)
        .WithHeaderSymbol("%")
        .WithMenu()
        .AddRow("Performing Progress", 89, segments: 44, color: Green, delta: "+10.2%", status: VisualStatus.Positive)
        .AddRow("Target Sales", 67, segments: 44, color: Blue, delta: "+2.2%", status: VisualStatus.Info)
        .WithAction("Up by 6% compared to last week, great momentum.")
        .WithActionStyle(Green.WithAlpha(38), ChartColor.FromHex("#16A36A"));

    private static CompositionStatusCard ProjectTaskCompositionCard(int outputScale) => CompositionStatusCard.Create()
        .WithTitle("Overall Tasks")
        .WithSubtitle("Spread across 6 projects.")
        .WithSize(820, 360)
        .WithTheme(ProjectTheme())
        .WithPngOutputScale(outputScale)
        .WithCard(false)
        .WithMetric("Tasks", 23, "Task")
        .AddSegment("On Going", 12, Blue, VisualStatus.Info, ChartFillPattern.DiagonalForward)
        .AddSegment("Under Review", 6, Orange, VisualStatus.Warning, ChartFillPattern.DiagonalBackward)
        .AddSegment("Finish", 4, Green, VisualStatus.Positive)
        .WithAction("View details task");

    private static Chart ProjectTrackCard() => Chart.Create()
        .WithTitle("Project Track")
        .WithSubtitle("4892 Referral  +12.2% vs last month")
        .WithTheme(ProjectTheme())
        .WithSize(820, 420)
        .WithLegend(false)
        .WithDashboardBarPanelStyle()
        .WithYAxisVisible(false)
        .WithGrid(false)
        .WithXLabels("Jan", "Feb", "Mar", "Apr")
        .AddBar("New", Points(46, 62, 58, 47), Orange)
        .AddBar("Active", Points(70, 92, 78, 74), Blue)
        .AddBar("Complete", Points(78, 69, 42, 57), Green);

    private static ScheduleTimelineBlock ProjectScheduleTimeline() => ScheduleTimelineBlock.Create()
        .WithTitle("Project Timeline")
        .WithSubtitle("Visualize your project schedule, key milestones, and deadlines in a chronological view.")
        .WithTheme(ProjectTheme())
        .WithSize(1320, 720)
        .WithCard(false)
        .WithTimeRange(8, 17, 1)
        .WithCurrentTime(14.2)
        .WithHeaderActions("12/Feb/2025", "Filter", "+ Add Schedule", "...")
        .AddEvent("Meeting Brief Project", 8.0, 10.0, 0, Blue, VisualStatus.Info, avatars: new[] { "AM", "RF", "PD", "MR" })
        .AddEvent("Research Analyze Content", 9.0, 11.0, 1, Purple, VisualStatus.Info, avatars: new[] { "SC", "MR", "RF" })
        .AddEvent("Build Website & Mobile Responsive", 8.0, 11.0, 2, Green, VisualStatus.Positive, avatars: new[] { "RJ", "MH", "PD" })
        .AddEvent("Review & Feedback", 8.0, 11.0, 3, Orange, VisualStatus.Warning, avatars: new[] { "AM", "SC", "MR" })
        .AddEvent("Review & Feedback", 10.0, 13.0, 4, Orange, VisualStatus.Warning, avatars: new[] { "RF", "PD" })
        .AddEvent("Build Website & Mobile Responsive", 11.0, 14.0, 5, Green, VisualStatus.Positive, avatars: new[] { "RJ", "MH" })
        .AddEvent("Internal Meeting", 11.0, 13.0, 0, Blue, VisualStatus.Info, avatars: new[] { "AM", "SC", "RF" })
        .AddEvent("Design System", 13.0, 16.0, 2, Green, VisualStatus.Positive, avatars: new[] { "SC", "MR" })
        .AddEvent("Review & Feedback", 13.0, 15.0, 1, Orange, VisualStatus.Warning, avatars: new[] { "PD", "RF" })
        .AddEvent("Branding Project", 13.5, 15.5, 3, Green, VisualStatus.Positive, avatars: new[] { "AM", "RJ" })
        .AddEvent("Animation", 13.0, 17.0, 4, Green, VisualStatus.Positive, avatars: new[] { "MH", "RJ", "RF" })
        .AddEvent("Internal Meeting", 15.0, 17.0, 5, Blue, VisualStatus.Info, avatars: new[] { "AM", "SC", "RF", "PD" })
        .AddEvent("Report Review", 16.0, 17.2, 0, Blue, VisualStatus.Info, badge: "Report", avatars: new[] { "MR", "SC" })
        .AddEvent("Research Analyze Content", 12.8, 14.8, 6, Purple, VisualStatus.Info, avatars: new[] { "SC", "RF", "PD" })
        .AddEvent("Internal Meeting", 15.9, 17.0, 6, Blue, VisualStatus.Info, avatars: new[] { "AM", "MR", "RJ" })
        .AddEvent("Animation", 8.0, 12.0, 7, Green, VisualStatus.Positive, avatars: new[] { "MH", "RJ", "RF" })
        .AddEvent("Review & Feedback", 11.0, 14.0, 8, Orange, VisualStatus.Warning, avatars: new[] { "RF", "PD", "SC" });

    private static ActivityTimelineBlock ShipmentActivityPanel(int outputScale) => ActivityTimelineBlock.Create()
        .WithTheme(ShipmentTheme())
        .WithSize(340, 470)
        .WithPadding(18, 18, 18, 18)
        .WithTransparentBackground()
        .WithCard(false)
        .WithPngOutputScale(outputScale)
        .WithEventSurfaces(false)
        .AddSection("In-progress")
        .AddEvent("Shipment", status: VisualStatus.Info, detail: "Delivery by: Royal Mail (Standard)", symbol: "S")
        .AddChecklistItem("Estimated dispatch: Today", completed: false)
        .AddChecklistItem("Estimated delivery: 2-3 business days", completed: false)
        .AddEvent("Shipment 1", status: VisualStatus.Neutral, symbol: "1")
        .AddChecklistItem("Carrier: Royal Mail (Standard)", completed: true, muted: true)
        .AddChecklistItem("Shipping label generated", completed: true, muted: true)
        .AddChecklistItem("Packing in progress", completed: false)
        .AddEvent("Shipment 2", status: VisualStatus.Neutral, symbol: "2")
        .AddSection("Completed")
        .AddHiddenSummary(6, "items hidden")
        .AddEvent("Order created", status: VisualStatus.Positive, detail: "UK#2337 created via Shopify", symbol: "OK");

    private static VisualGrid HrOperationsGrid(int outputScale) => VisualGrid.Create()
        .WithTitle("HR Operations Dashboard")
        .WithSubtitle("Metric cards, department stacked bars, candidate status, attendance trend, schedule list, and vacancies.")
        .WithColumns(4)
        .WithGap(16)
        .WithPadding(24)
        .WithFrame()
        .WithPanelSize(320, 230)
        .WithPanelFit(VisualGridPanelFit.Stretch)
        .WithTheme(HrTheme())
        .WithPngOutputScale(outputScale)
        .Add(HrMetric("Open Positions", "56", "+5%", VisualStatus.Positive, Purple))
        .Add(HrMetric("Applications", "5625", "-3%", VisualStatus.Negative, Mint))
        .Add(HrMetric("Shortlisted", "125", "-3%", VisualStatus.Warning, Orange))
        .Add(HrMetric("Onboarding", "66", "+5%", VisualStatus.Positive, Green))
        .Add(DepartmentBars().WithSize(656, 310), 2, 2)
        .Add(CandidateStatus().WithSize(656, 310), 2, 2)
        .Add(AttendanceRate().WithSize(656, 360), 2, 2)
        .Add(HrScheduleList().WithSize(320, 360), 1, 2)
        .Add(NewsList().WithSize(320, 360), 1, 2)
        .Add(RecentVacancies().WithSize(1328, 250), 4, 2);

    private static MetricCard HrMetric(string label, string value, string trend, VisualStatus status, ChartColor color) => MetricCard.Create()
        .WithTitle(label)
        .WithMetric(label, value)
        .WithTrend(trend)
        .WithStatus(status)
        .WithTheme(HrTheme())
        .WithMiniBars(new[] { 30d, 44d, 52d, 49d, 62d }, maximum: 100, color: color, mutedColor: ChartColor.FromHex("#E9EEF8"));

    private static Chart DepartmentBars() => Chart.Create()
        .WithTitle("Employer by Department")
        .WithSubtitle("All employees, terminated, and new hires")
        .WithTheme(HrTheme())
        .WithDashboardStackedRowStyle(showTotals: true, showLegend: true)
        .WithXLabels("Engineering", "Maintenance", "Human Resources", "IT", "HSEQ")
        .AddHorizontalBar("All employee", Points(68, 62, 65, 70, 74), Purple)
        .AddHorizontalBar("Terminated", Points(25, 28, 27, 25, 24), Mint)
        .AddHorizontalBar("New hires", Points(14, 12, 13, 15, 15), Orange);

    private static Chart CandidateStatus() => Chart.Create()
        .WithTitle("Candidate Status")
        .WithSubtitle("5625 employers")
        .WithTheme(HrTheme())
        .WithStackedHorizontalBars()
        .WithLegend(false)
        .WithAxes(false)
        .WithPlotBackground(false)
        .WithPadding(32, 86, 32, 50)
        .WithXAxisBounds(0, 100)
        .AddHorizontalBar("Total Applications", Points(65), Purple)
        .AddHorizontalBar("Shortlisted", Points(25), Mint)
        .AddHorizontalBar("Rejected", Points(10), Orange);

    private static Chart AttendanceRate() => Chart.Create()
        .WithTitle("Attendance Rate")
        .WithSubtitle("6.15% total attendance rate")
        .WithTheme(HrTheme())
        .WithDashboardTrendPanelStyle(showLegend: true, showYAxis: false)
        .WithXLabels("Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul")
        .AddSmoothLine("On-time", Points(21, 23, 25, 37, 31, 28, 47), Purple)
        .AddSmoothLine("Late attend", Points(24, 20, 28, 22, 30, 34, 39), Orange)
        .AddSmoothLine("Absent", Points(18, 22, 17, 14, 23, 20, 31), Mint)
        .WithDashboardTrendFocus(4, 37, "Apr", Purple, ChartDataLabelPlacement.Right);

    private static ChartList HrScheduleList() => ChartList.Create()
        .WithTitle("Schedule")
        .WithSubtitle("Aug 2023")
        .WithTheme(HrTheme())
        .WithDenseMode()
        .WithMarker(VisualListMarker.None)
        .AddStatusItem("Meeting with Developer", VisualStatus.Info, "10:00 AM - 10:30 AM")
        .AddStatusItem("Meeting with Developer", VisualStatus.Info, "10:00 AM - 10:30 AM")
        .AddStatusItem("Meeting with Developer", VisualStatus.Warning, "10:00 AM - 10:30 AM")
        .AddStatusItem("Meeting with Developer", VisualStatus.Positive, "10:00 AM - 10:30 AM");

    private static ChartList NewsList() => ChartList.Create()
        .WithTitle("News & Events")
        .WithTheme(HrTheme())
        .WithMarker(VisualListMarker.None)
        .AddItem("High Employee Satisfaction", "Achieved 92% employee")
        .AddItem("Recruiting Pipeline", "125 shortlisted candidates")
        .AddItem("Payroll Review", "March approval ready")
        .AddItem("Mobile App", "Design sync scheduled");

    private static ChartTable RecentVacancies() => ChartTable.Create()
        .WithTitle("Recent Vacancies")
        .WithTheme(HrTheme())
        .WithDenseMode()
        .WithColumns("Company", "Job Title", "Location", "Applications", "New", "Trend")
        .AddRow("Google", "Software Engineer", "New York", "92", "", "")
        .AddRow("Microsoft", "Software Engineer", "New York", "92", "", "")
        .AddRow("Asana", "Software Engineer", "New York", "92", "", "")
        .AddRow("Google", "Software Engineer", "New York", "92", "", "")
        .WithRow(0, row => { row.Cells[4].WithBadge("22 new", VisualStatus.Info, Purple); row.Cells[5].WithSparkline(new[] { 12d, 16d, 13d, 19d, 22d }, color: Purple); })
        .WithRow(1, row => { row.Cells[4].WithBadge("12 new", VisualStatus.Info, Purple); row.Cells[5].WithSparkline(new[] { 10d, 14d, 12d, 11d, 12d }, color: Purple); })
        .WithRow(2, row => { row.Cells[4].WithBadge("2 new", VisualStatus.Neutral, SoftGray); row.Cells[5].WithMiniBars(new[] { 2d, 4d, 3d, 2d, 5d }, color: Purple); })
        .WithRow(3, row => { row.Cells[4].WithBadge("32 new", VisualStatus.Info, Purple); row.Cells[5].WithSparkline(new[] { 18d, 24d, 22d, 29d, 32d }, color: Purple); });

    private static VisualGrid PaymentAnalyticsGrid(int outputScale) => VisualGrid.Create()
        .WithTitle("Payment Analytics Dashboard")
        .WithSubtitle("Overview sparklines, currency distribution, map, merchants, and transaction table.")
        .WithColumns(4)
        .WithGap(16)
        .WithPadding(24)
        .WithFrame()
        .WithPanelSize(330, 230)
        .WithPanelFit(VisualGridPanelFit.Stretch)
        .WithTheme(PaymentTheme())
        .WithPngOutputScale(outputScale)
        .Add(PaymentMetric("Total transaction", "EUR 56,980", "+12%", VisualStatus.Positive, Green))
        .Add(PaymentMetric("Revenue growth", "EUR 26,980", "-7%", VisualStatus.Negative, Red))
        .Add(PaymentMetric("Fees earned", "EUR 23,980", "+13%", VisualStatus.Positive, Green))
        .Add(PaymentMetric("Chargeback rate", "EUR 30,980", "-8%", VisualStatus.Negative, Red))
        .Add(CurrencyDistribution().WithSize(680, 420), 2, 2)
        .Add(TransactionMap().WithSize(680, 420), 2, 2)
        .Add(TopMerchants().WithSize(680, 300), 2, 2)
        .Add(TransactionCountryList().WithSize(680, 300), 2, 2);

    private static MetricCard PaymentMetric(string label, string value, string trend, VisualStatus status, ChartColor color) => MetricCard.Create()
        .WithTitle(label)
        .WithMetric(label, value)
        .WithTrend(trend)
        .WithStatus(status)
        .WithTheme(PaymentTheme())
        .WithMiniSparkline(new[] { 20d, 26d, 27d, 35d, 33d, 31d, 38d, 42d }, color: color, fillColor: color.WithAlpha(32));

    private static DistributionStripCard CurrencyDistribution() => DistributionStripCard.Create()
        .WithTitle("Net Earning")
        .WithSubtitle("Currency split with row-level shares")
        .WithTheme(PaymentTheme())
        .WithPadding(34, 28, 34, 28)
        .WithMetric("Net earning", "EUR 56,980.00", "Last month")
        .AddSegment("Russian Ruble (RUB)", 9.74, ChartColor.FromHex("#FF3B13"), "RUB", "EUR 12.23", VisualStatus.Warning)
        .AddSegment("Euro (EUR)", 38.48, ChartColor.FromHex("#1389F2"), "EUR", "EUR 20.23", VisualStatus.Info)
        .AddSegment("Japanese Yen (JPY)", 12.55, ChartColor.FromHex("#AE14E8"), "JPY", "EUR 10.00", VisualStatus.Info)
        .AddSegment("United States Dollar (USD)", 14.11, ChartColor.FromHex("#24D47B"), "USD", "EUR 12.00", VisualStatus.Positive)
        .AddSegment("Ukrainian Hryvnia (UAH)", 12.55, ChartColor.FromHex("#F5D318"), "UAH", "EUR 14.00", VisualStatus.Warning)
        .AddSegment("British Pound Sterling (GBP)", 12.55, Mint, "GBP", "EUR 10.00", VisualStatus.Info);

    private static Chart TransactionMap() => Chart.Create()
        .WithTitle("Transaction by Country")
        .WithSubtitle("Dotted world map with valued country markers")
        .WithTheme(PaymentTheme())
        .WithLegend(false)
        .WithMapViewport(ChartMapViewport.World())
        .AddDottedMap("Transactions", new[] {
            new ChartMapPoint("Canada", -106.3468, 56.1304, 92, Blue),
            new ChartMapPoint("United Kingdom", -3.4360, 55.3781, 76, Teal),
            new ChartMapPoint("India", 78.9629, 20.5937, 68, Orange),
            new ChartMapPoint("United States", -95.7129, 37.0902, 88, Purple),
            new ChartMapPoint("Japan", 138.2529, 36.2048, 72, Red),
            new ChartMapPoint("Bangladesh", 90.3563, 23.6850, 44, Green)
        });

    private static ChartTable TopMerchants() => ChartTable.Create()
        .WithTitle("Top Performing Merchants")
        .WithTheme(PaymentTheme())
        .WithDenseMode()
        .WithColumns("Merchant", "Email", "Earning")
        .AddRow("Test", "jennings@example.com", "EUR 430,871.00")
        .AddRow("Floyd Miles", "floyd@gmail.com", "EUR 361,253.00")
        .AddRow("Ronald Richards", "tanya.hill@example.com", "EUR 12,893.00")
        .AddRow("Dianne Russell", "michael.mitt@example.com", "EUR 297,105.00");

    private static ChartList TransactionCountryList() => ChartList.Create()
        .WithTitle("Currency Details")
        .WithTheme(PaymentTheme())
        .WithMarker(VisualListMarker.Status)
        .AddStatusItem("Russian Ruble (RUB)", VisualStatus.Negative, "9.74%")
        .AddStatusItem("Euro (EUR)", VisualStatus.Info, "38.48%")
        .AddStatusItem("Japanese Yen (JPY)", VisualStatus.Neutral, "12.55%")
        .AddStatusItem("United States Dollar (USD)", VisualStatus.Positive, "14.11%")
        .AddStatusItem("Ukrainian Hryvnia (UAH)", VisualStatus.Warning, "12.55%")
        .AddStatusItem("British Pound Sterling (GBP)", VisualStatus.Info, "12.55%");

    private static void Save(Chart chart, string output, string name) {
        chart.SaveSvg(Path.Combine(output, name + ".svg"));
        chart.SaveHtml(Path.Combine(output, name + ".html"));
        chart.SavePng(Path.Combine(output, name + ".png"));
    }

    private static void Save(IVisualBlock block, string output, string name) {
        block.SaveSvg(Path.Combine(output, name + ".svg"));
        block.SaveHtml(Path.Combine(output, name + ".html"));
        block.SavePng(Path.Combine(output, name + ".png"));
    }

    private static void Save(VisualGrid grid, string output, string name) {
        grid.SaveSvg(Path.Combine(output, name + ".svg"));
        grid.SaveHtml(Path.Combine(output, name + ".html"));
        grid.SavePng(Path.Combine(output, name + ".png"));
    }

    private static IEnumerable<ChartPoint> Points(params double[] y) {
        for (var i = 0; i < y.Length; i++) yield return new ChartPoint(i + 1, y[i]);
    }

    private static ChartTheme OperationsTheme() => ChartTheme.DashboardLight()
        .WithSurfaceColors(ChartColor.FromHex("#F6F8F7"), ChartColor.White, ChartColor.White, ChartColor.FromHex("#EBF1F1"), ChartColor.FromHex("#DDE7E8"))
        .WithTextColors(ChartColor.FromHex("#101820"), ChartColor.FromHex("#70777B"))
        .WithPalette(Teal.ToHex(), Mint.ToHex(), Green.ToHex(), Red.ToHex())
        .WithCornerRadius(16, 8)
        .WithShadowOpacity(0.04);

    private static ChartTheme ProjectTheme() => ChartTheme.DashboardLight()
        .WithSurfaceColors(ChartColor.FromHex("#F2F2F3"), ChartColor.White, ChartColor.White, ChartColor.FromHex("#ECEEF2"), ChartColor.FromHex("#DCDDE3"))
        .WithTextColors(ChartColor.FromHex("#202235"), ChartColor.FromHex("#7A7D86"))
        .WithPalette(Blue.ToHex(), Orange.ToHex(), Green.ToHex(), Purple.ToHex())
        .WithCornerRadius(18, 8)
        .WithShadowOpacity(0.05);

    private static ChartTheme ShipmentTheme() => ProjectTheme()
        .WithTypography(20, 12, 10, 11, 12, 11)
        .WithPalette(Blue.ToHex(), Green.ToHex(), SoftGray.ToHex(), Purple.ToHex());

    private static ChartTheme HrTheme() => ChartTheme.DashboardLight()
        .WithSurfaceColors(ChartColor.FromHex("#F4F7FB"), ChartColor.White, ChartColor.White, ChartColor.FromHex("#EDF1F7"), ChartColor.FromHex("#DCE3EF"))
        .WithTextColors(ChartColor.FromHex("#171927"), ChartColor.FromHex("#717682"))
        .WithPalette(Purple.ToHex(), Mint.ToHex(), Orange.ToHex(), Green.ToHex(), Blue.ToHex())
        .WithCornerRadius(18, 8)
        .WithShadowOpacity(0.04)
        .WithMarkerRadius(4.2);

    private static ChartTheme PaymentTheme() => ChartTheme.SaasDashboardLight()
        .WithSurfaceColors(ChartColor.FromHex("#F6F6F5"), ChartColor.White, ChartColor.White, ChartColor.FromHex("#EFEFEE"), ChartColor.FromHex("#DCDDDD"))
        .WithTextColors(ChartColor.FromHex("#111827"), ChartColor.FromHex("#68707A"))
        .WithPalette(Blue.ToHex(), Teal.ToHex(), Green.ToHex(), Orange.ToHex(), Red.ToHex(), Purple.ToHex())
        .WithCornerRadius(14, 7)
        .WithShadowOpacity(0.035);
}
