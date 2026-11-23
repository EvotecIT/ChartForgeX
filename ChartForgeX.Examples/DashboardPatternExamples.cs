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
        Save(ChannelShareCapsuleLoop(scale), output, "dashboard-channel-share-capsule-loop");
        Save(CertificateInventoryCompositionStrip(scale), output, "dashboard-certificate-inventory-composition-strip");
        Save(ContentPerformanceRows(scale), output, "dashboard-content-performance-rows");
        Save(ConversionFunnelColumns(scale), output, "dashboard-conversion-funnel-columns");
        Save(ProjectTaskCompositionCard(scale), output, "dashboard-project-task-composition-card");
        Save(ProjectTrackCard().Png(p => p.OutputScale((int)pngOutputScale)), output, "dashboard-project-track-card");
        Save(ProjectScheduleTimeline().PngOutputScale((int)pngOutputScale), output, "dashboard-project-schedule-timeline");
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
        .Title("Appointment Operations Dashboard")
        .Subtitle("Heatmap, peak-hour bars, workload rows, and availability list from the supplied operations design.")
        .Columns(4)
        .Gap(16)
        .Padding(24)
        .FrameVisible()
        .PanelSize(320, 220)
        .PanelFit(VisualPanelFit.Stretch)
        .Theme(OperationsTheme())
        .PngOutputScale(outputScale)
        .Add(AppointmentVolume().Size(600, 357), 2, 2)
        .Add(PeakHourReview().Size(600, 329), 2, 2)
        .Add(StaffWorkload().Size(600, 302), 2, 2)
        .Add(AppointmentTotalCard().PngOutputScale(outputScale))
        .Add(AppointmentUnassignedCard().PngOutputScale(outputScale))
        .Add(AvailableStaffList().Size(600, 238), 2, 2)
        .Add(AppointmentBacklogList().Size(600, 238), 2, 2);

    private static HeatmapInsightCard AppointmentVolume() => HeatmapInsightCard.Create()
            .Title("Appointment Volume")
            .Subtitle("Appointments by weekday and hour")
            .Theme(OperationsTheme())
            .Padding(28, 26, 28, 24)
            .PeriodLabel("Jan 7 - Jan 13, 2024")
            .Columns("S", "M", "T", "W", "T", "F", "S")
            .Minimum(0).Maximum(12).LowColor(Mint.WithAlpha(70)).HighColor(TealDark)
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
            .Title("Review Peak Hour")
            .Subtitle("Highlighted review concentration by hour")
            .Theme(OperationsTheme())
            .Legend(false)
            .DashboardBarPanelStyle()
            .YAxis(y => y.Visible(false))
            .ShowAxisLines(false)
            .FormatValuesBy(value => value.ToString("0", System.Globalization.CultureInfo.InvariantCulture))
            .XLabels("8am", "9am", "10am", "11am", "12pm", "1pm", "2pm", "3pm", "4pm", "5pm", "6pm", "7pm", "8pm")
            .HighlightXAxisRange(7.5, 11.5, Red, 0.08, "Peak review window")
            .AddBar("Reviews", Points(3, 1, 3, 1, 0, 0, 0, 9, 10, 9, 7, 0, 0), SoftGray);

        chart.Series[0].PointColorRange(7, 4, Red);
        return chart;
    }

    private static WorkloadListBlock StaffWorkload() => WorkloadListBlock.Create()
        .Title("Today Staff Workload")
        .Subtitle("Capacity share by staff member")
        .Theme(OperationsTheme())
        .AddPerson("Panji Dwi", "Zumba Trainer", 4, 8, Tone.Neutral, "PD", "4/8", TealDark)
        .AddPerson("Raihan Fikri", "Aerobik Trainer", 10, 8, Tone.Danger, "RF", "10/8", Red, "Overload")
        .AddPerson("Rijal Jatnika", "Personal Trainer", 7, 8, Tone.Neutral, "RJ", "7/8", TealDark)
        .AddPerson("Mufti Hidayat", "Massage Specialist", 6, 8, Tone.Neutral, "MH", "6/8", TealDark);

    private static MetricCard AppointmentTotalCard() => MetricCard.Create()
        .Title("Upcoming Appointment")
        .Metric("Total Appointment", "24")
        .Trend("+12%")
        .Caption("vs last week")
        .Status(Tone.Success)
        .Theme(OperationsTheme())
        .MiniBars(new[] { 42d, 58d, 61d, 72d, 83d }, maximum: 100, color: Teal);

    private static MetricCard AppointmentUnassignedCard() => MetricCard.Create()
        .Title("Unassigned")
        .Metric("Needs owner", "6")
        .Trend("-12%")
        .Caption("vs last week")
        .Status(Tone.Danger)
        .Theme(OperationsTheme())
        .MiniBars(new[] { 31d, 28d, 24d, 22d, 18d }, maximum: 100, color: Red);

    private static ChartList AvailableStaffList() => ChartList.Create()
        .Title("Available Staff")
        .Subtitle("Shift-ready staff list with service labels.")
        .Theme(OperationsTheme())
        .Marker(VisualListMarker.Status)
        .Dense()
        .AddStatusItem("Raihan Fikri", Tone.Success, "Zumba")
        .AddStatusItem("Wildan Rizal", Tone.Info, "Special Zumba Package")
        .AddStatusItem("Panji Dwi", Tone.Success, "Special Massage")
        .AddStatusItem("Wildan Rizal", Tone.Neutral, "Reflexology Therapy");

    private static ChartList AppointmentBacklogList() => ChartList.Create()
        .Title("AI Prediction")
        .Subtitle("Peak 1h30m from 14:00-18:00.")
        .Theme(OperationsTheme())
        .Marker(VisualListMarker.Status)
        .Dense()
        .AddStatusItem("Stay estimate", Tone.Info, "1-2h")
        .AddStatusItem("Busy time", Tone.Warning, "Fri 5 PM - 6 PM")
        .AddStatusItem("Second peak", Tone.Neutral, "Mon 7 PM - 9 PM")
        .AddStatusItem("Suggested action", Tone.Success, "Manage shift");

    private static SegmentedMetricBlock ProjectProgressCard(int outputScale) => SegmentedMetricBlock.Create(SegmentedMetricStyle.ProgressRows)
        .Title("Project Progress")
        .Subtitle("Overall completion rate all projects.")
        .Size(800, 351)
        .Theme(ProjectTheme())
        .PngOutputScale(outputScale)
        .HeaderSymbol("%")
        .AddItem("Performing Progress", 89, segments: 44, color: Green, delta: "+10.2%", status: Tone.Success)
        .AddItem("Target Sales", 67, segments: 44, color: Blue, delta: "+2.2%", status: Tone.Info)
        .Subtitle("Up by 6% compared to last week, great momentum.");

    private static SegmentedMetricBlock CertificateInventoryCompositionStrip(int outputScale) => SegmentedMetricBlock.Create(SegmentedMetricStyle.CompositionStrip)
        .Title("Certificate Count")
        .Subtitle("Inventory split by lifecycle state.")
        .Size(800, 332)
        .Theme(ProjectTheme())
        .PngOutputScale(outputScale)
        .Metric("Certificates", 277)
        .AddItem("Valid", 164, color: Green, displayValue: "164")
        .AddItem("Expiring", 48, color: Orange, displayValue: "48")
        .AddItem("Revoked", 24, color: ChartColor.FromHex("#EF5DA8"), displayValue: "24")
        .AddItem("Unknown", 41, color: ChartColor.FromHex("#6D83F2"), displayValue: "41");

    private static SegmentedMetricBlock ChannelShareCapsuleLoop(int outputScale) => SegmentedMetricBlock.Create(SegmentedMetricStyle.CapsuleLoop)
        .Title("Channel Share")
        .Subtitle("Balanced source mix rendered as a capsule loop.")
        .Size(800, 332)
        .Theme(ProjectTheme())
        .PngOutputScale(outputScale)
        .AddItem("Direct", 40, color: Green, displayValue: "24,000")
        .AddItem("Partner", 35, color: Orange, displayValue: "21,000")
        .AddItem("Referral", 15, color: ChartColor.FromHex("#EF5DA8"), displayValue: "9,000")
        .AddItem("Other", 10, color: ChartColor.FromHex("#6D83F2"), displayValue: "6,000");

    private static SegmentedMetricBlock ContentPerformanceRows(int outputScale) => SegmentedMetricBlock.Create(SegmentedMetricStyle.ProgressRows)
        .Title("Content Performance")
        .Subtitle("Metric rows with exact values, deltas, and segmented progress.")
        .Size(800, 449)
        .Theme(ProjectTheme())
        .PngOutputScale(outputScale)
        .AddItem(new SegmentedMetricItem("Posts", 86).Maximum(100).Segments(44).Color(Blue).DisplayValue("132,034").Delta("+4.3%").Status(Tone.Success))
        .AddItem(new SegmentedMetricItem("Replies", 62).Maximum(100).Segments(44).Color(ChartColor.FromHex("#7894F8")).DisplayValue("56,234").Delta("-6.4%").Status(Tone.Danger))
        .AddItem(new SegmentedMetricItem("Likes", 84).Maximum(100).Segments(44).Color(ChartColor.FromHex("#8B7AF4")).DisplayValue("125,485,958").Delta("+12.34%").Status(Tone.Success))
        .AddItem(new SegmentedMetricItem("Reposts", 79).Maximum(100).Segments(44).Color(Purple).DisplayValue("94,344").Delta("+10.02%").Status(Tone.Success))
        .AddItem(new SegmentedMetricItem("Bookmarks", 61).Maximum(100).Segments(44).Color(ChartColor.FromHex("#9A7CF0")).DisplayValue("12,344").Delta("+4%").Status(Tone.Success))
        .AddItem(new SegmentedMetricItem("Shares", 48).Maximum(100).Segments(44).Color(ChartColor.FromHex("#A75AEF")).DisplayValue("42,344").Delta("-3.4%").Status(Tone.Danger));

    private static SegmentedMetricBlock ConversionFunnelColumns(int outputScale) => SegmentedMetricBlock.Create(SegmentedMetricStyle.FunnelColumns)
        .Title("Conversion Funnel")
        .Subtitle("Ordered stage counts rendered as reusable segmented columns.")
        .Size(800, 322)
        .Theme(ProjectTheme())
        .PngOutputScale(outputScale)
        .AddItem("Clicks", 82000, segments: 24, color: Green, displayValue: "82,000")
        .AddItem("Added to Cart", 7200, segments: 16, color: Orange, displayValue: "7,200")
        .AddItem("Payment", 1230, segments: 12, color: ChartColor.FromHex("#6D83F2"), displayValue: "1,230")
        .AddItem("Abandoned Cart", 5970, segments: 15, color: ChartColor.FromHex("#EF5DA8"), displayValue: "5,970");

    private static SegmentedMetricBlock ProjectTaskCompositionCard(int outputScale) => SegmentedMetricBlock.Create(SegmentedMetricStyle.CompositionStrip)
        .Title("Overall Tasks")
        .Subtitle("Spread across 6 projects.")
        .Size(800, 390)
        .Theme(ProjectTheme())
        .PngOutputScale(outputScale)
        .ShowCard(false)
        .Metric("Tasks", 23, "Task")
        .AddItem("On Going", 12, color: Blue, status: Tone.Info, pattern: ChartFillPattern.DiagonalForward)
        .AddItem("Under Review", 6, color: Orange, status: Tone.Warning, pattern: ChartFillPattern.DiagonalBackward)
        .AddItem("Finish", 4, color: Green, status: Tone.Success);

    private static Chart ProjectTrackCard() => Chart.Create()
        .Title("Project Track")
        .Subtitle("4892 Referral  +12.2% vs last month")
        .Theme(ProjectTheme())
        .Size(800, 410)
        .Legend(false)
        .DashboardBarPanelStyle()
        .YAxis(y => y.Visible(false))
        .ShowGrid(false)
        .XLabels("Jan", "Feb", "Mar", "Apr")
        .AddBar("New", Points(46, 62, 58, 47), Orange)
        .AddBar("Active", Points(70, 92, 78, 74), Blue)
        .AddBar("Complete", Points(78, 69, 42, 57), Green);

    private static ScheduleTimelineBlock ProjectScheduleTimeline() => ScheduleTimelineBlock.Create()
        .Title("Project Timeline")
        .Subtitle("Visualize your project schedule, key milestones, and deadlines in a chronological view.")
        .Theme(ProjectTheme())
        .Size(800, 436)
        .ShowCard(false)
        .Start(8).End(17).TickInterval(1)
        .CurrentTime(14.2)
        .Subtitle("12/Feb/2025")
        .AddEvent("Meeting Brief Project", 8.0, 10.0, 0, Blue, Tone.Info, avatars: new[] { "AM", "RF", "PD", "MR" })
        .AddEvent("Research Analyze Content", 9.0, 11.0, 1, Purple, Tone.Info, avatars: new[] { "SC", "MR", "RF" })
        .AddEvent("Build Website & Mobile Responsive", 8.0, 11.0, 2, Green, Tone.Success, avatars: new[] { "RJ", "MH", "PD" })
        .AddEvent("Review & Feedback", 8.0, 11.0, 3, Orange, Tone.Warning, avatars: new[] { "AM", "SC", "MR" })
        .AddEvent("Review & Feedback", 10.0, 13.0, 4, Orange, Tone.Warning, avatars: new[] { "RF", "PD" })
        .AddEvent("Build Website & Mobile Responsive", 11.0, 14.0, 5, Green, Tone.Success, avatars: new[] { "RJ", "MH" })
        .AddEvent("Internal Meeting", 11.0, 13.0, 0, Blue, Tone.Info, avatars: new[] { "AM", "SC", "RF" })
        .AddEvent("Design System", 13.0, 16.0, 2, Green, Tone.Success, avatars: new[] { "SC", "MR" })
        .AddEvent("Review & Feedback", 13.0, 15.0, 1, Orange, Tone.Warning, avatars: new[] { "PD", "RF" })
        .AddEvent("Branding Project", 13.5, 15.5, 3, Green, Tone.Success, avatars: new[] { "AM", "RJ" })
        .AddEvent("Animation", 13.0, 17.0, 4, Green, Tone.Success, avatars: new[] { "MH", "RJ", "RF" })
        .AddEvent("Internal Meeting", 15.0, 17.0, 5, Blue, Tone.Info, avatars: new[] { "AM", "SC", "RF", "PD" })
        .AddEvent("Report Review", 16.0, 17.2, 0, Blue, Tone.Info, badge: "Report", avatars: new[] { "MR", "SC" })
        .AddEvent("Research Analyze Content", 12.8, 14.8, 6, Purple, Tone.Info, avatars: new[] { "SC", "RF", "PD" })
        .AddEvent("Internal Meeting", 15.9, 17.0, 6, Blue, Tone.Info, avatars: new[] { "AM", "MR", "RJ" })
        .AddEvent("Animation", 8.0, 12.0, 7, Green, Tone.Success, avatars: new[] { "MH", "RJ", "RF" })
        .AddEvent("Review & Feedback", 11.0, 14.0, 8, Orange, Tone.Warning, avatars: new[] { "RF", "PD", "SC" });

    private static ActivityTimelineBlock ShipmentActivityPanel(int outputScale) => ActivityTimelineBlock.Create()
        .Theme(ShipmentTheme())
        .Size(340, 470)
        .Padding(18, 18, 18, 18)
        .TransparentBackground()
        .ShowCard(false)
        .PngOutputScale(outputScale)
        .ShowEventSurfaces(false)
        .AddSection("In-progress")
        .AddEvent("Shipment", status: Tone.Info, detail: "Delivery by: Royal Mail (Standard)", symbol: "S")
        .AddChecklistItem("Estimated dispatch: Today", completed: false)
        .AddChecklistItem("Estimated delivery: 2-3 business days", completed: false)
        .AddEvent("Shipment 1", status: Tone.Neutral, symbol: "1")
        .AddChecklistItem("Carrier: Royal Mail (Standard)", completed: true, muted: true)
        .AddChecklistItem("Shipping label generated", completed: true, muted: true)
        .AddChecklistItem("Packing in progress", completed: false)
        .AddEvent("Shipment 2", status: Tone.Neutral, symbol: "2")
        .AddSection("Completed")
        .AddHiddenSummary(6, "items hidden")
        .AddEvent("Order created", status: Tone.Success, detail: "UK#2337 created via Shopify", symbol: "OK");

    private static VisualGrid HrOperationsGrid(int outputScale) => VisualGrid.Create()
        .Title("HR Operations Dashboard")
        .Subtitle("Metric cards, department stacked bars, candidate status, attendance trend, schedule list, and vacancies.")
        .Columns(4)
        .Gap(16)
        .Padding(24)
        .FrameVisible()
        .PanelSize(320, 230)
        .PanelFit(VisualPanelFit.Stretch)
        .Theme(HrTheme())
        .PngOutputScale(outputScale)
        .Add(HrMetric("Open Positions", "56", "+5%", Tone.Success, Purple))
        .Add(HrMetric("Applications", "5625", "-3%", Tone.Danger, Mint))
        .Add(HrMetric("Shortlisted", "125", "-3%", Tone.Warning, Orange))
        .Add(HrMetric("Onboarding", "66", "+5%", Tone.Success, Green))
        .Add(DepartmentBars().Size(600, 284), 2, 2)
        .Add(CandidateStatus().Size(600, 284), 2, 2)
        .Add(AttendanceRate().Size(600, 329), 2, 2)
        .Add(HrScheduleList().Size(320, 360), 1, 2)
        .Add(NewsList().Size(320, 360), 1, 2)
        .Add(RecentVacancies().Size(800, 151), 4, 2);

    private static MetricCard HrMetric(string label, string value, string trend, Tone status, ChartColor color) => MetricCard.Create()
        .Title(label)
        .Metric(label, value)
        .Trend(trend)
        .Status(status)
        .Theme(HrTheme())
        .MiniBars(new[] { 30d, 44d, 52d, 49d, 62d }, maximum: 100, color: color, mutedColor: ChartColor.FromHex("#E9EEF8"));

    private static Chart DepartmentBars() => Chart.Create()
        .Title("Employer by Department")
        .Subtitle("All employees, terminated, and new hires")
        .Theme(HrTheme())
        .DashboardStackedRowStyle(showTotals: true, showLegend: true)
        .XLabels("Engineering", "Maintenance", "Human Resources", "IT", "HSEQ")
        .AddHorizontalBar("All employee", Points(68, 62, 65, 70, 74), Purple)
        .AddHorizontalBar("Terminated", Points(25, 28, 27, 25, 24), Mint)
        .AddHorizontalBar("New hires", Points(14, 12, 13, 15, 15), Orange);

    private static Chart CandidateStatus() => Chart.Create()
        .Title("Candidate Status")
        .Subtitle("5625 employers")
        .Theme(HrTheme())
        .Bars(b => b.Stacked())
        .Legend(false)
        .Axes(false)
        .ShowPlotBackground(false)
        .Padding(32, 86, 32, 50)
        .XAxis(x => x.Bounds(0, 100))
        .AddHorizontalBar("Total Applications", Points(65), Purple)
        .AddHorizontalBar("Shortlisted", Points(25), Mint)
        .AddHorizontalBar("Rejected", Points(10), Orange);

    private static Chart AttendanceRate() => Chart.Create()
        .Title("Attendance Rate")
        .Subtitle("6.15% total attendance rate")
        .Theme(HrTheme())
        .DashboardTrendPanelStyle(showLegend: true, showYAxis: false)
        .XLabels("Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul")
        .AddSmoothLine("On-time", Points(21, 23, 25, 37, 31, 28, 47), Purple)
        .AddSmoothLine("Late attend", Points(24, 20, 28, 22, 30, 34, 39), Orange)
        .AddSmoothLine("Absent", Points(18, 22, 17, 14, 23, 20, 31), Mint)
        .DashboardTrendFocus(4, 37, "Apr", Purple, ChartDataLabelPlacement.Right);

    private static ChartList HrScheduleList() => ChartList.Create()
        .Title("Schedule")
        .Subtitle("Aug 2023")
        .Theme(HrTheme())
        .Dense()
        .Marker(VisualListMarker.None)
        .AddStatusItem("Meeting with Developer", Tone.Info, "10:00 AM - 10:30 AM")
        .AddStatusItem("Meeting with Developer", Tone.Info, "10:00 AM - 10:30 AM")
        .AddStatusItem("Meeting with Developer", Tone.Warning, "10:00 AM - 10:30 AM")
        .AddStatusItem("Meeting with Developer", Tone.Success, "10:00 AM - 10:30 AM");

    private static ChartList NewsList() => ChartList.Create()
        .Title("News & Events")
        .Theme(HrTheme())
        .Marker(VisualListMarker.None)
        .AddItem("High Employee Satisfaction", "Achieved 92% employee")
        .AddItem("Recruiting Pipeline", "125 shortlisted candidates")
        .AddItem("Payroll Review", "March approval ready")
        .AddItem("Mobile App", "Design sync scheduled");

    private static ChartTable RecentVacancies() => ChartTable.Create()
        .Title("Recent Vacancies")
        .Theme(HrTheme())
        .Dense()
        .Columns("Company", "Job Title", "Location", "Applications", "New", "Trend")
        .AddRow("Google", "Software Engineer", "New York", "92", "", "")
        .AddRow("Microsoft", "Software Engineer", "New York", "92", "", "")
        .AddRow("Asana", "Software Engineer", "New York", "92", "", "")
        .AddRow("Google", "Software Engineer", "New York", "92", "", "")
        .UpdateRow(0, row => { row.Cells[4].Badge("22 new", Tone.Info, Purple); row.Cells[5].Sparkline(new[] { 12d, 16d, 13d, 19d, 22d }, color: Purple); })
        .UpdateRow(1, row => { row.Cells[4].Badge("12 new", Tone.Info, Purple); row.Cells[5].Sparkline(new[] { 10d, 14d, 12d, 11d, 12d }, color: Purple); })
        .UpdateRow(2, row => { row.Cells[4].Badge("2 new", Tone.Neutral, SoftGray); row.Cells[5].MiniBars(new[] { 2d, 4d, 3d, 2d, 5d }, color: Purple); })
        .UpdateRow(3, row => { row.Cells[4].Badge("32 new", Tone.Info, Purple); row.Cells[5].Sparkline(new[] { 18d, 24d, 22d, 29d, 32d }, color: Purple); });

    private static VisualGrid PaymentAnalyticsGrid(int outputScale) => VisualGrid.Create()
        .Title("Payment Analytics Dashboard")
        .Subtitle("Overview sparklines, currency distribution, map, merchants, and transaction table.")
        .Columns(4)
        .Gap(16)
        .Padding(24)
        .FrameVisible()
        .PanelSize(330, 230)
        .PanelFit(VisualPanelFit.Stretch)
        .Theme(PaymentTheme())
        .PngOutputScale(outputScale)
        .Add(PaymentMetric("Total transaction", "EUR 56,980", "+12%", Tone.Success, Green))
        .Add(PaymentMetric("Revenue growth", "EUR 26,980", "-7%", Tone.Danger, Red))
        .Add(PaymentMetric("Fees earned", "EUR 23,980", "+13%", Tone.Success, Green))
        .Add(PaymentMetric("Chargeback rate", "EUR 30,980", "-8%", Tone.Danger, Red))
        .Add(CurrencyDistribution().Size(600, 371), 2, 2)
        .Add(TransactionMap().Size(600, 371), 2, 2)
        .Add(TopMerchants().Size(600, 265), 2, 2)
        .Add(TransactionCountryList().Size(600, 265), 2, 2);

    private static MetricCard PaymentMetric(string label, string value, string trend, Tone status, ChartColor color) => MetricCard.Create()
        .Title(label)
        .Metric(label, value)
        .Trend(trend)
        .Status(status)
        .Theme(PaymentTheme())
        .MiniSparkline(new[] { 20d, 26d, 27d, 35d, 33d, 31d, 38d, 42d }, color: color, fillColor: color.WithAlpha(32));

    private static SegmentedMetricBlock CurrencyDistribution() => SegmentedMetricBlock.Create(SegmentedMetricStyle.DistributionRows)
        .Title("Net Earning")
        .Subtitle("Currency split with row-level shares")
        .Theme(PaymentTheme())
        .Padding(34, 28, 34, 28)
        .Metric("Net earning", "EUR 56,980.00", "Last month")
        .AddItem("Russian Ruble (RUB)", 9.74, color: ChartColor.FromHex("#FF3B13"), symbol: "RUB", displayValue: "EUR 12.23", status: Tone.Warning)
        .AddItem("Euro (EUR)", 38.48, color: ChartColor.FromHex("#1389F2"), symbol: "EUR", displayValue: "EUR 20.23", status: Tone.Info)
        .AddItem("Japanese Yen (JPY)", 12.55, color: ChartColor.FromHex("#AE14E8"), symbol: "JPY", displayValue: "EUR 10.00", status: Tone.Info)
        .AddItem("United States Dollar (USD)", 14.11, color: ChartColor.FromHex("#24D47B"), symbol: "USD", displayValue: "EUR 12.00", status: Tone.Success)
        .AddItem("Ukrainian Hryvnia (UAH)", 12.55, color: ChartColor.FromHex("#F5D318"), symbol: "UAH", displayValue: "EUR 14.00", status: Tone.Warning)
        .AddItem("British Pound Sterling (GBP)", 12.55, color: Mint, symbol: "GBP", displayValue: "EUR 10.00", status: Tone.Info);

    private static Chart TransactionMap() => Chart.Create()
        .Title("Transaction by Country")
        .Subtitle("Dotted world map with valued country markers")
        .Theme(PaymentTheme())
        .Legend(false)
        .Map(m => m.Viewport(ChartMapViewport.World()))
        .AddDottedMap("Transactions", new[] {
            new ChartMapPoint("Canada", -106.3468, 56.1304, 92, Blue),
            new ChartMapPoint("United Kingdom", -3.4360, 55.3781, 76, Teal),
            new ChartMapPoint("India", 78.9629, 20.5937, 68, Orange),
            new ChartMapPoint("United States", -95.7129, 37.0902, 88, Purple),
            new ChartMapPoint("Japan", 138.2529, 36.2048, 72, Red),
            new ChartMapPoint("Bangladesh", 90.3563, 23.6850, 44, Green)
        });

    private static ChartTable TopMerchants() => ChartTable.Create()
        .Title("Top Performing Merchants")
        .Theme(PaymentTheme())
        .Dense()
        .Columns("Merchant", "Email", "Earning")
        .AddRow("Test", "jennings@example.com", "EUR 430,871.00")
        .AddRow("Floyd Miles", "floyd@gmail.com", "EUR 361,253.00")
        .AddRow("Ronald Richards", "tanya.hill@example.com", "EUR 12,893.00")
        .AddRow("Dianne Russell", "michael.mitt@example.com", "EUR 297,105.00");

    private static ChartList TransactionCountryList() => ChartList.Create()
        .Title("Currency Details")
        .Theme(PaymentTheme())
        .Marker(VisualListMarker.Status)
        .AddStatusItem("Russian Ruble (RUB)", Tone.Danger, "9.74%")
        .AddStatusItem("Euro (EUR)", Tone.Info, "38.48%")
        .AddStatusItem("Japanese Yen (JPY)", Tone.Neutral, "12.55%")
        .AddStatusItem("United States Dollar (USD)", Tone.Success, "14.11%")
        .AddStatusItem("Ukrainian Hryvnia (UAH)", Tone.Warning, "12.55%")
        .AddStatusItem("British Pound Sterling (GBP)", Tone.Info, "12.55%");

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

    private static VisualTheme OperationsTheme() => VisualTheme.Graphite().Configure(builder => {
            builder.Light.Background = builder.Dark.Background = ChartColor.FromHex("#F6F8F7");
            builder.Light.ElevatedSurface = builder.Dark.ElevatedSurface = ChartColor.White;
            builder.Light.Surface = builder.Dark.Surface = ChartColor.White;
            builder.Light.Border = builder.Dark.Border = ChartColor.FromHex("#EBF1F1");
            builder.Light.PlotBorder = builder.Dark.PlotBorder = ChartColor.FromHex("#DDE7E8");
            builder.Light.Foreground = builder.Dark.Foreground = ChartColor.FromHex("#101820");
            builder.Light.MutedForeground = builder.Dark.MutedForeground = ChartColor.FromHex("#70777B");
            builder.Light.Palette = builder.Dark.Palette = new[] { Teal, Mint, Green, Red };
            builder.CardRadius = 16;
            builder.PlotRadius = builder.BarRadius = 8;
            builder.CardShadowOpacity = 0.04;
        });

    private static VisualTheme ProjectTheme() => VisualTheme.Graphite().Configure(builder => {
            builder.Light.Background = builder.Dark.Background = ChartColor.FromHex("#F2F2F3");
            builder.Light.ElevatedSurface = builder.Dark.ElevatedSurface = ChartColor.White;
            builder.Light.Surface = builder.Dark.Surface = ChartColor.White;
            builder.Light.Border = builder.Dark.Border = ChartColor.FromHex("#ECEEF2");
            builder.Light.PlotBorder = builder.Dark.PlotBorder = ChartColor.FromHex("#DCDDE3");
            builder.Light.Foreground = builder.Dark.Foreground = ChartColor.FromHex("#202235");
            builder.Light.MutedForeground = builder.Dark.MutedForeground = ChartColor.FromHex("#7A7D86");
            builder.Light.Palette = builder.Dark.Palette = new[] { Blue, Orange, Green, Purple };
            builder.CardRadius = 18;
            builder.PlotRadius = builder.BarRadius = 8;
            builder.CardShadowOpacity = 0.05;
        });

    private static VisualTheme ShipmentTheme() => ProjectTheme().Configure(builder => {
            builder.Typography = new VisualTypography(builder.Typography.Family, titleSize: 20, subtitleSize: 12, axisTitleSize: 10, axisSize: 11, legendSize: 12, dataLabelSize: 11);
            builder.Light.Palette = builder.Dark.Palette = new[] { Blue, Green, SoftGray, Purple };
        });

    private static VisualTheme HrTheme() => VisualTheme.Graphite().Configure(builder => {
            builder.Light.Background = builder.Dark.Background = ChartColor.FromHex("#F4F7FB");
            builder.Light.ElevatedSurface = builder.Dark.ElevatedSurface = ChartColor.White;
            builder.Light.Surface = builder.Dark.Surface = ChartColor.White;
            builder.Light.Border = builder.Dark.Border = ChartColor.FromHex("#EDF1F7");
            builder.Light.PlotBorder = builder.Dark.PlotBorder = ChartColor.FromHex("#DCE3EF");
            builder.Light.Foreground = builder.Dark.Foreground = ChartColor.FromHex("#171927");
            builder.Light.MutedForeground = builder.Dark.MutedForeground = ChartColor.FromHex("#717682");
            builder.Light.Palette = builder.Dark.Palette = new[] { Purple, Mint, Orange, Green, Blue };
            builder.CardRadius = 18;
            builder.PlotRadius = builder.BarRadius = 8;
            builder.CardShadowOpacity = 0.04;
            builder.MarkerRadius = 4.2;
        });

    private static VisualTheme PaymentTheme() => VisualTheme.Graphite().Configure(builder => {
            builder.Light.Background = builder.Dark.Background = ChartColor.FromHex("#F6F6F5");
            builder.Light.ElevatedSurface = builder.Dark.ElevatedSurface = ChartColor.White;
            builder.Light.Surface = builder.Dark.Surface = ChartColor.White;
            builder.Light.Border = builder.Dark.Border = ChartColor.FromHex("#EFEFEE");
            builder.Light.PlotBorder = builder.Dark.PlotBorder = ChartColor.FromHex("#DCDDDD");
            builder.Light.Foreground = builder.Dark.Foreground = ChartColor.FromHex("#111827");
            builder.Light.MutedForeground = builder.Dark.MutedForeground = ChartColor.FromHex("#68707A");
            builder.Light.Palette = builder.Dark.Palette = new[] { Blue, Teal, Green, Orange, Red, Purple };
            builder.CardRadius = 14;
            builder.PlotRadius = builder.BarRadius = 7;
            builder.CardShadowOpacity = 0.035;
        });
}
