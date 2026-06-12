using System;
using ChartForgeX.Core;
using ChartForgeX.Markup;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void MarkupChartParserParsesMultiSeriesTablesAndAnnotations() {
        const string source = @"```chartforgex chart v1 {#quarterly title=""Quarterly Trend"" type=""line""}
| Month | Revenue | Cost |
| ----- | ------- | ---- |
| Jan | 12 | 7 |
| Feb | 18 | 9 |
| Mar | 16 | 8 |
annotation hLine 15 ""Target"" color:#16A34A
annotation hBand 4 6 color=#F59E0B opacity=0.25
```";

        var result = new MarkupChartParser().Parse(source);

        Assert(!result.HasErrors, "Multi-series chart markup should parse without errors: " + Diagnostics(result));
        var chart = result.Document!.Chart;
        Assert(chart.Series.Count == 2, "Multi-series chart tables should create one series per numeric value column.");
        Assert(chart.Series[0].Name == "Revenue" && chart.Series[1].Name == "Cost", "Multi-series chart tables should preserve value column names as series names.");
        Assert(chart.Series[0].Kind == ChartSeriesKind.Line && chart.Series[1].Kind == ChartSeriesKind.Line, "Multi-series chart tables should inherit the requested chart kind.");
        Assert(chart.Options.XAxisLabels.Count == 3 && chart.Options.XAxisLabels[2].Text == "Mar", "Multi-series chart tables should use the label column as x-axis labels.");
        Assert(chart.Annotations.Count == 2 && chart.Annotations[0].Kind == ChartAnnotationKind.HorizontalLine, "Chart markup should parse reusable annotation commands.");
        Assert(chart.Annotations[1].Kind == ChartAnnotationKind.HorizontalBand && chart.Annotations[1].Color.ToHex() == "#F59E0B" && IsClose(chart.Annotations[1].Opacity, 0.25), "Chart markup annotations should accept documented key=value attributes.");
        Assert(chart.ToSvg().Contains("data-cfx-role=\"annotation-line\"", StringComparison.Ordinal), "Chart markup annotations should render through the shared SVG annotation layer.");
    }

    private static void MarkupChartParserParsesCommandSeries() {
        const string source = @"```chartforgex chart v1 {#mixed title=""Mixed Series""}
labels Jan Feb Mar
series Revenue type smoothLine color #2563EB values 12 18 16
series Incidents type bar color #EF4444 values 3 4 2
```";

        var result = new MarkupChartParser().Parse(source);

        Assert(!result.HasErrors, "Command-style chart series should parse without errors: " + Diagnostics(result));
        var chart = result.Document!.Chart;
        Assert(chart.Series.Count == 2, "Command-style series should create distinct named series.");
        Assert(chart.Series[0].Name == "Revenue" && chart.Series[0].Kind == ChartSeriesKind.Line && chart.Series[0].Smooth, "Command-style series should map smooth line aliases.");
        Assert(chart.Series[1].Name == "Incidents" && chart.Series[1].Kind == ChartSeriesKind.Bar, "Command-style series should map per-series chart kinds.");
        Assert(chart.Series[0].Points.Count == 3 && chart.Series[1].Points.Count == 3, "Command-style series should preserve all numeric values.");
    }

    private static void MarkupChartParserReportsInvalidSeriesContracts() {
        const string missingValues = @"```chartforgex chart v1
labels Jan Feb
series Revenue type line
```";
        var missingValuesResult = new MarkupChartParser().Parse(missingValues);

        Assert(missingValuesResult.HasErrors, "Command-style series without values should produce a parse diagnostic.");
        Assert(Diagnostics(missingValuesResult).Contains("must declare at least one numeric value", StringComparison.Ordinal), "Missing series values diagnostic should explain the contract.");

        const string invalidColor = @"```chartforgex chart v1
labels Jan Feb
series Revenue color nope values 1 2
annotation hLine 1 ""Target"" color=nope
```";
        var invalidColorResult = new MarkupChartParser().Parse(invalidColor);

        Assert(invalidColorResult.HasErrors, "Invalid series or annotation colors should produce parse diagnostics.");
        Assert(Diagnostics(invalidColorResult).Contains("valid hex color", StringComparison.Ordinal), "Invalid chart colors should be reported as markup diagnostics.");

        const string unknownTypes = @"```chartforgex chart v1
type spline
series Revenue type=ribbon values 1 2
```";
        var unknownTypesResult = new MarkupChartParser().Parse(unknownTypes);

        Assert(unknownTypesResult.HasErrors, "Unknown chart and series types should produce parse diagnostics.");
        Assert(Diagnostics(unknownTypesResult).Contains("Unknown chart type", StringComparison.Ordinal), "Unknown chart type diagnostics should be reported during parsing.");

        const string invalidTickCount = @"```chartforgex chart v1
labels Jan Feb
values 1 2
options:
| option | value |
| ------ | ----- |
| tickCount | 1 |
```";
        var tickCountResult = new MarkupChartParser().Parse(invalidTickCount);

        Assert(tickCountResult.HasErrors, "Invalid chart tick counts should produce parser errors.");
        Assert(Diagnostics(tickCountResult).Contains("tickCount", StringComparison.Ordinal), "Invalid tick count diagnostics should name the option.");

        const string invalidFenceSize = @"```chartforgex chart v1 {width=-1 height=0}
values 1 2
```";
        var invalidFenceSizeResult = new MarkupChartParser().Parse(invalidFenceSize);

        Assert(invalidFenceSizeResult.HasErrors, "Invalid chart fence dimensions should produce parse diagnostics.");
        Assert(Diagnostics(invalidFenceSizeResult).Contains("positive", StringComparison.Ordinal), "Invalid chart fence dimensions should explain positive-size validation.");

        const string invalidAxisBounds = @"```chartforgex chart v1
values 1 2
xAxisBounds 10 5
yAxisBounds NaN 5
padding -1
```";
        var invalidAxisBoundsResult = new MarkupChartParser().Parse(invalidAxisBounds);

        Assert(invalidAxisBoundsResult.HasErrors, "Invalid chart bounds and padding should produce parse diagnostics.");
        Assert(Diagnostics(invalidAxisBoundsResult).Contains("maximum must be greater than minimum", StringComparison.Ordinal) && Diagnostics(invalidAxisBoundsResult).Contains("non-negative finite", StringComparison.Ordinal), "Invalid chart option diagnostics should report bad bounds and padding.");
    }

    private static void MarkupFlowParserParsesTopologyCompatibleFlow() {
        const string source = @"```chartforgex flow v1 {#pipeline}
id pipeline
title ""Processing Flow""
layout layered lr
lane ops ""Operations""
start intake ""Intake"" lane=ops status=healthy
decision review ""Review"" lane=ops status=warning
connect intake -> review ""handoff"" status=healthy color=#EF4444
```";

        var result = new MarkupFlowParser().Parse(source);

        Assert(!result.HasErrors, "Flow markup should parse native flow syntax without errors: " + Diagnostics(result));
        Assert(result.Document != null, "Flow markup should produce a flow artifact.");
        Assert(result.Document!.Id == "pipeline" && result.Document.Title == "Processing Flow", "Flow markup should preserve id and title.");
        Assert(result.Document.Lanes.Count == 1, "Flow markup should preserve lanes.");
        Assert(result.Document.Steps.Count == 2 && result.Document.Connectors.Count == 1, "Flow markup should preserve steps and connectors.");
        Assert(result.Document.Steps[1].Kind == FlowArtifactStepKind.Decision, "Flow markup should preserve flow-specific step kinds.");
        Assert(result.Document.Steps[0].LaneId == "ops" && result.Document.Steps[0].Status == VisualStatus.Positive, "Flow markup should accept documented key=value step attributes.");
        Assert(result.Document.Connectors[0].Color == "#EF4444" && result.Document.Connectors[0].Status == VisualStatus.Positive, "Flow markup should accept documented key=value connector attributes.");
    }

    private static void VisualMarkupParserMapsFlowFencesToArtifacts() {
        const string source = @"# Visual

```chartforgex flow v1 {#pipeline}
id pipeline
title ""Processing Flow""
start intake ""Intake"" status:healthy
decision review ""Review"" status:warning
connect intake -> review ""handoff"" status:healthy
```";

        var result = new VisualMarkupParser().Parse(source);

        Assert(!result.HasErrors, "Visual markup parser should parse supported flow fences without errors: " + Diagnostics(result));
        Assert(result.Artifacts.Count == 1, "Visual markup parser should emit one artifact for one flow fence.");
        Assert(result.Artifacts[0].Kind == VisualArtifactKind.Flow, "Flow fences should map to flow visual artifacts.");
        Assert(result.Artifacts[0].Id == "pipeline", "Flow visual artifact ids should come from flow markup.");
        Assert(result.Artifacts[0].Model is FlowArtifact, "Flow visual artifacts should keep the typed flow model.");
        Assert(result.Artifacts[0].Metadata["render.model"] == nameof(FlowArtifact), "Flow visual artifacts should expose the flow render model.");
        Assert(result.Artifacts[0].Metadata["render.previewModel"] == nameof(TopologyChart), "Flow visual artifacts should document the static preview projection.");
        Assert(result.Artifacts[0].ToSvg().Contains("data-cfx-role=\"topology\"", StringComparison.Ordinal), "Flow visual artifacts should render through deterministic topology SVG output.");
    }

    private static void MarkupFlowParserReportsInvalidViewportDimensions() {
        const string source = @"```chartforgex flow v1 {#bad width=-1 padding=NaN}
start intake ""Intake""
```";

        var result = new VisualMarkupParser().Parse(source);

        Assert(result.HasErrors, "Invalid flow viewport dimensions should produce parse diagnostics instead of throwing during artifact conversion.");
        Assert(Diagnostics(result).Contains("finite", StringComparison.Ordinal), "Invalid flow dimensions should report finite/positive validation.");
    }

    private static void MarkupFlowParserReportsInvalidColors() {
        const string source = @"```chartforgex flow v1
lane ops ""Operations"" color=nope
start intake ""Intake"" color:nope
connect intake -> done ""handoff"" color:nope
```";

        var result = new MarkupFlowParser().Parse(source);

        Assert(result.HasErrors, "Invalid flow colors should produce parse diagnostics before preview export.");
        Assert(Diagnostics(result).Contains("valid hex color", StringComparison.Ordinal), "Invalid flow colors should be reported as markup diagnostics.");
    }

    private static void MarkupSequenceParserParsesSequenceMarkup() {
        const string source = @"```chartforgex sequence v1 {#incident title=""Incident Sequence"" width=900 height=520}
actor user ""User""
participant api ""API""
database db ""Database""
message user -> api ""Submit request""
message api -> db ""Store event"" style:dashed activate:true
note rightOf api ""Processing""
block loop ""Retry"" 0 1
```";

        var result = new MarkupSequenceParser().Parse(source);

        Assert(!result.HasErrors, "Sequence markup should parse native sequence syntax without errors: " + Diagnostics(result));
        Assert(result.Document != null, "Sequence markup should produce a sequence artifact.");
        Assert(result.Document!.Id == "incident" && result.Document.Title == "Incident Sequence", "Sequence fence attributes should seed id and title.");
        Assert(result.Document.Participants.Count == 3, "Sequence markup should preserve participants.");
        Assert(result.Document.Messages.Count == 2 && result.Document.Notes.Count == 1 && result.Document.Blocks.Count == 1, "Sequence markup should preserve messages, notes, and blocks.");
        Assert(result.Document.Messages[1].LineStyle == SequenceArtifactMessageLineStyle.Dashed, "Sequence message attributes should preserve dashed line style.");
        Assert(result.Document.ToVisualArtifact(VisualArtifactSourceLanguage.ChartForgeX).ToSvg().Contains("data-cfx-role=\"sequence-message\"", StringComparison.Ordinal), "Sequence markup should render through deterministic sequence SVG output.");
    }

    private static void VisualMarkupParserMapsSequenceFencesToArtifacts() {
        const string source = @"# Visual

```chartforgex sequence v1 {#incident}
actor user ""User""
participant api ""API""
message user -> api ""Submit request""
note rightOf api ""Accepted""
```";

        var result = new VisualMarkupParser().Parse(source);

        Assert(!result.HasErrors, "Visual markup parser should parse supported sequence fences without errors: " + Diagnostics(result));
        Assert(result.Artifacts.Count == 1, "Visual markup parser should emit one artifact for one sequence fence.");
        Assert(result.Artifacts[0].Kind == VisualArtifactKind.Sequence, "Sequence fences should map to sequence visual artifacts.");
        Assert(result.Artifacts[0].Id == "incident", "Sequence visual artifact ids should come from sequence markup.");
        Assert(result.Artifacts[0].Model is SequenceArtifact, "Sequence visual artifacts should keep the typed sequence model.");
        Assert(result.Artifacts[0].SourceLanguage == VisualArtifactSourceLanguage.ChartForgeX, "Native sequence markup should preserve ChartForgeX as the artifact source language.");
        Assert(result.Artifacts[0].SupportsExport(VisualArtifactExportFormat.Html), "Sequence visual artifacts should support standalone HTML export.");
        Assert(result.Artifacts[0].Metadata["render.model"] == nameof(SequenceArtifact), "Sequence visual artifacts should expose the sequence render model.");
        Assert(result.Artifacts[0].ToHtmlPage().Contains("<!doctype html>", StringComparison.OrdinalIgnoreCase), "Sequence visual artifacts should render through deterministic standalone HTML output.");
        Assert(result.Artifacts[0].ToSvg().Contains("data-cfx-role=\"sequence-message\"", StringComparison.Ordinal), "Sequence visual artifacts should render through deterministic sequence SVG output.");
    }

    private static void MarkupTimelineParserParsesTimelineAndGanttMarkup() {
        const string timelineSource = @"```chartforgex timeline v1 {#release title=""Release Plan"" type=""timeline"" width=720 height=360}
item ""Design"" 2026-01-01 2026-01-07 color:#2563EB
milestone ""Ship"" 2026-01-14 color:#16A34A
```";

        var timelineResult = new MarkupTimelineParser().Parse(timelineSource);

        Assert(!timelineResult.HasErrors, "Timeline markup should parse item and milestone commands without errors: " + Diagnostics(timelineResult));
        var timelineChart = timelineResult.Document!.Chart;
        Assert(timelineResult.Document.Id == "release", "Timeline fence id attributes should seed artifact ids.");
        Assert(timelineChart.Title == "Release Plan" && timelineChart.Options.Size.Width == 720 && timelineChart.Options.Size.Height == 360, "Timeline fence attributes should seed chart options.");
        Assert(timelineChart.Series.Count == 2 && timelineChart.Series[0].Kind == ChartSeriesKind.Timeline, "Timeline markup should build native timeline series.");
        Assert(timelineChart.ToSvg().Contains("data-cfx-role=\"timeline-item\"", StringComparison.Ordinal), "Timeline markup should render through native timeline SVG output.");

        const string ganttSource = @"```chartforgex timeline v1 {#launch type=""gantt"" today=""2026-01-10""}
| kind | label | start | end | progress | dependsOn |
| ---- | ----- | ----- | --- | -------- | --------- |
| task | Build | 2026-01-01 | 2026-01-12 | 0.5 | |
| milestone | Launch | 2026-01-14 | | | 0 |
```";

        var ganttResult = new MarkupTimelineParser().Parse(ganttSource);

        Assert(!ganttResult.HasErrors, "Timeline markup should parse Gantt table rows without errors: " + Diagnostics(ganttResult));
        var ganttChart = ganttResult.Document!.Chart;
        Assert(ganttChart.Series.Count == 2 && ganttChart.Series[0].Kind == ChartSeriesKind.Gantt, "Gantt timeline markup should build native Gantt series.");
        Assert(IsClose(ganttChart.Series[0].Points[1].X, 0.5) && IsClose(ganttChart.Series[0].Points[1].Y, -1), "Gantt table rows should preserve progress and default dependencies.");
        Assert(ganttChart.ToSvg().Contains("data-cfx-role=\"gantt-task\"", StringComparison.Ordinal), "Gantt timeline markup should render through native Gantt SVG output.");

        const string commandGanttSource = @"```chartforgex timeline v1 {#launch type=""gantt""}
task Design 1 5 progress=1 color=#2563EB
task Build 5 10 progress=0.72 dependsOn=0 color=#14B8A6
milestone Release 11 dependsOn=1 color=#059669
```";
        var commandGanttResult = new MarkupTimelineParser().Parse(commandGanttSource);

        Assert(!commandGanttResult.HasErrors, "Timeline command attributes using '=' should parse without errors: " + Diagnostics(commandGanttResult));
        var commandGanttChart = commandGanttResult.Document!.Chart;
        Assert(IsClose(commandGanttChart.Series[0].Points[0].X, 1) && IsClose(commandGanttChart.Series[0].Points[0].Y, 5), "Numeric timeline command values should stay numeric axis positions.");
        Assert(IsClose(commandGanttChart.Series[1].Points[1].X, 0.72) && IsClose(commandGanttChart.Series[1].Points[1].Y, 0), "Timeline command '=' attributes should preserve task progress and dependencies.");
        Assert(commandGanttChart.Series[1].Color.HasValue, "Timeline command color= attributes should preserve task colors.");
    }

    private static void MarkupTimelineParserReportsInvalidColors() {
        const string source = @"```chartforgex timeline v1
item ""Design"" 2026-01-01 2026-01-07 color:nope
```";

        var result = new MarkupTimelineParser().Parse(source);

        Assert(result.HasErrors, "Invalid timeline item colors should produce parse diagnostics.");
        Assert(Diagnostics(result).Contains("valid hex color", StringComparison.Ordinal), "Invalid timeline colors should be reported as markup diagnostics.");

        const string invalidSize = @"```chartforgex timeline v1 {#bad width=-1 height=0}
item ""Design"" 1 2
```";
        var invalidSizeResult = new MarkupTimelineParser().Parse(invalidSize);

        Assert(invalidSizeResult.HasErrors, "Invalid timeline dimensions should produce parse diagnostics instead of throwing during chart construction.");
        Assert(Diagnostics(invalidSizeResult).Contains("positive", StringComparison.Ordinal), "Invalid timeline dimension diagnostics should explain positive-size validation.");

        const string invalidRangeAndProgress = @"```chartforgex timeline v1 {type=gantt}
task Build 1 5 progress=2
task Deploy 5 1
```";
        var invalidRangeAndProgressResult = new MarkupTimelineParser().Parse(invalidRangeAndProgress);

        Assert(invalidRangeAndProgressResult.HasErrors, "Invalid timeline ranges and progress values should produce parse diagnostics.");
        Assert(Diagnostics(invalidRangeAndProgressResult).Contains("between 0 and 1", StringComparison.Ordinal) && Diagnostics(invalidRangeAndProgressResult).Contains("end must be greater than or equal to start", StringComparison.Ordinal), "Timeline diagnostics should report bad progress and reversed ranges.");
    }

    private static void MarkupSequenceParserReportsMalformedArrowMessages() {
        const string source = @"```chartforgex sequence v1
participant user ""User""
participant api ""API""
message user ->
```";

        var result = new MarkupSequenceParser().Parse(source);

        Assert(result.HasErrors, "Sequence messages using arrow syntax without a target should produce parse diagnostics.");
        Assert(Diagnostics(result).Contains("target participant", StringComparison.Ordinal), "Malformed sequence arrow diagnostics should explain the missing target.");
    }

    private static void VisualMarkupParserMapsTimelineFencesToArtifacts() {
        const string source = @"# Visual

```chartforgex timeline v1 {#release title=""Release Plan"" type=""timeline""}
item ""Design"" 1 4
item ""Build"" 5 9
```";

        var result = new VisualMarkupParser().Parse(source);

        Assert(!result.HasErrors, "Visual markup parser should parse supported timeline fences without errors: " + Diagnostics(result));
        Assert(result.Artifacts.Count == 1, "Visual markup parser should emit one artifact for one timeline fence.");
        Assert(result.Artifacts[0].Kind == VisualArtifactKind.Timeline, "Timeline fences should map to timeline visual artifacts.");
        Assert(result.Artifacts[0].Id == "release", "Timeline visual artifact ids should come from timeline markup.");
        Assert(result.Artifacts[0].Model is Chart, "Timeline visual artifacts should keep a typed Chart model.");
        Assert(result.Artifacts[0].ToSvg().Contains("data-cfx-role=\"timeline\"", StringComparison.Ordinal), "Timeline visual artifacts should render through deterministic chart SVG output.");
    }
}
