using System;
using System.Xml.Linq;
using ChartForgeX.Accessibility;
using ChartForgeX.Composition;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.VisualBlocks;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class OptionalConfigurationOwnershipTests {
    [Fact]
    public void TableRowCallbacksDetachCellsMicrovisualsAndBadgesAndRollBackBeforeExport() {
        var table = ChartTable.Create().WithSize(480, 180).WithColumns("Name", "Status", "History").AddRow("Original", "Status", "History");
        var previous = table.Rows[0];
        previous.Background = ChartColor.FromHex("#203040");
        previous.Foreground = ChartColor.FromHex("#F0F1F2");
        previous.Cells[0].Alignment = TextAlignment.Right;
        previous.Cells[1].WithBadge("Ready", VisualStatus.Positive, ChartColor.FromHex("#20D080"), VisualBadgeStyle.Outline);
        previous.Cells[2].WithSparkline(new SparklineData(new double?[] { 2, null, 5 }, minimum: 0, maximum: 6), ChartColor.FromHex("#10A0FF"));
        ChartTableRow? candidate = null;
        table.ConfigureRow(0, row => {
            candidate = row;
            row.Cells[0].Text = "Configured";
            row.Cells[0].Foreground = ChartColor.FromHex("#00FF80");
            row.Cells[0].Status = VisualStatus.Info;
            row.Cells[2].Background = ChartColor.FromHex("#102030");
        });
        var installed = table.Rows[0];
        Assert.Equal(previous.Background, installed.Background);
        Assert.Equal(previous.Foreground, installed.Foreground);
        Assert.Equal(TextAlignment.Right, installed.Cells[0].Alignment);
        Assert.Equal("Ready", installed.Cells[1].BadgeText);
        Assert.Equal(VisualStatus.Positive, installed.Cells[1].BadgeStatus);
        Assert.Equal(VisualBadgeStyle.Outline, installed.Cells[1].BadgeStyle);
        Assert.Equal(ChartColor.FromHex("#20D080"), installed.Cells[1].BadgeColor);
        Assert.Equal(VisualStatus.Info, installed.Cells[0].Status);
        Assert.Equal(ChartColor.FromHex("#00FF80"), installed.Cells[0].Foreground);
        Assert.Equal(ChartColor.FromHex("#102030"), installed.Cells[2].Background);
        Assert.Equal(ChartTableCellMicroVisualKind.Sparkline, installed.Cells[2].MicroVisualKind);
        Assert.Equal(new double?[] { 2, null, 5 }, installed.Cells[2].SparklineData!.Values);
        Assert.Equal(0d, installed.Cells[2].MicroVisualMinimum);
        Assert.Equal(6d, installed.Cells[2].MicroVisualMaximum);
        Assert.Equal(ChartColor.FromHex("#10A0FF"), installed.Cells[2].MicroVisualColor);
        var svg = table.ToSvg();
        var pixels = table.RenderRgba().Pixels;
        Assert.Contains("Configured", svg);
        Assert.Contains("Ready", svg);

        previous.Cells[0].Text = "Previous cell";
        previous.Cells[2].WithMiniBars(new[] { 99d });
        previous.Background = ChartColor.White;
        var retained = Assert.IsType<ChartTableRow>(candidate);
        retained.Cells[1].BadgeText = "Retained badge";
        retained.Cells[2].WithMiniBars(new[] { 88d });
        retained.Cells.Clear();
        Assert.Throws<InvalidOperationException>(() => table.ConfigureRow(0, row => {
            row.Cells[0].Text = "Partial cell";
            row.Cells[2].WithMiniBars(new[] { 77d });
            row.Cells.Clear();
            throw new InvalidOperationException();
        }));
        Assert.Equal(new[] { 2d, 5d }, table.Rows[0].Cells[2].MicroVisualValues);
        Assert.Equal(svg, table.ToSvg());
        Assert.Equal(pixels, table.RenderRgba().Pixels);
    }

    [Fact]
    public void CanvasCallbacksDetachPriorAndCandidateOptionsAndPreserveArtifactsAfterFailure() {
        var canvas = VisualCanvas.Create(360, 160).AddInfoTile(16, 16, 328, 120, "PC", "Host", "Workstation");
        var previousStyle = canvas.Style;
        var previousAccessibility = canvas.Accessibility;
        VisualCanvasStyleOptions? styleCandidate = null;
        VisualAccessibility? accessibilityCandidate = null;
        var glass = ChartColor.FromHex("#00D080");
        canvas.ConfigureStyle(options => {
            styleCandidate = options;
            options.TileGlassTop = glass;
            options.TileGlassBottom = glass;
        }).ConfigureAccessibility(options => {
            accessibilityCandidate = options;
            options.Name = "Configured canvas";
            options.Description = "Host and status";
            options.Language = "pl";
        });
        var svg = canvas.ToSvg();
        var pixels = canvas.RenderRgba().Pixels;
        var document = XDocument.Parse(svg);
        Assert.Equal("pl", document.Root!.Attribute("lang")!.Value);
        Assert.Contains(document.Descendants(), element => element.Name.LocalName == "title" && element.Value == "Configured canvas");
        Assert.Contains(document.Descendants(), element => element.Name.LocalName == "desc" && element.Value == "Host and status");
        Assert.Contains(glass.ToCss(), svg);

        previousStyle.TileGlassTop = ChartColor.White;
        previousAccessibility.Name = "Previous canvas";
        Assert.IsType<VisualCanvasStyleOptions>(styleCandidate).TileGlassBottom = ChartColor.White;
        Assert.IsType<VisualAccessibility>(accessibilityCandidate).IsDecorative = true;
        Assert.Throws<InvalidOperationException>(() => canvas.ConfigureStyle(options => {
            options.TileGlassTop = ChartColor.Transparent;
            throw new InvalidOperationException();
        }));
        Assert.Throws<InvalidOperationException>(() => canvas.ConfigureAccessibility(options => {
            options.Name = "Partial canvas";
            options.Language = "de";
            throw new InvalidOperationException();
        }));
        Assert.Equal(svg, canvas.ToSvg());
        Assert.Equal(pixels, canvas.RenderRgba().Pixels);
    }

    [Fact]
    public void SegmentedItemCallbackInstallsIndependentFactsAndDoesNotAppendAfterFailure() {
        var block = SegmentedMetricBlock.Create(SegmentedMetricStyle.ProgressRows).WithSize(440, 200);
        SegmentedMetricItem? candidate = null;
        block.AddItem("Ready", 8, item => {
            candidate = item;
            item.WithProgress(10, 10).WithDelta("+2").WithDisplayValue("8 of 10");
            item.Color = ChartColor.FromHex("#20A0FF");
        });
        var svg = block.ToSvg();
        var pixels = block.RenderRgba().Pixels;
        Assert.Contains("Ready", svg);
        Assert.Contains("8 of 10", svg);
        var retained = Assert.IsType<SegmentedMetricItem>(candidate);
        retained.Label = "Retained item";
        retained.Value = 0;
        retained.Segments = 1;
        retained.Color = ChartColor.White;
        Assert.Throws<InvalidOperationException>(() => block.AddItem("Partial item", 5, item => {
            item.WithProgress(5, 5);
            throw new InvalidOperationException();
        }));
        Assert.Single(block.Items);
        Assert.Equal("+2", block.Items[0].Delta);
        Assert.Equal(svg, block.ToSvg());
        Assert.Equal(pixels, block.RenderRgba().Pixels);
    }
}
