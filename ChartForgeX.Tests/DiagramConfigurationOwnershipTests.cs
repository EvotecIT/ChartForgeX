using ChartForgeX.Accessibility;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class DiagramConfigurationOwnershipTests {
    [Fact]
    public void TopologyMetadataCallbacksDetachCandidatesAndRollBackBeforeRendering() {
        var chart = TopologyChart.Create().AddNode("service", "Service", 100, 100);
        var previousAccessibility = chart.Accessibility;
        var previousLabels = chart.Labels;
        VisualAccessibility? accessibilityCandidate = null;
        TopologyLabels? labelsCandidate = null;
        chart.ConfigureAccessibility(options => {
            accessibilityCandidate = options;
            options.Name = "Mapa usług"; options.Language = "pl-PL";
        }).ConfigureLabels(options => {
            labelsCandidate = options;
            options.UntitledTopology = "Mapa";
            options.AccessibleTextFormatter = facts => "Opis " + facts.Count;
        });
        var svg = chart.ToSvg();
        Assert.Contains("Mapa usług", svg);
        Assert.Contains("Opis 1", svg);

        previousAccessibility.Name = "Previous name";
        previousLabels.UntitledTopology = "Previous label";
        Assert.IsType<VisualAccessibility>(accessibilityCandidate).Name = "Retained callback name";
        Assert.IsType<TopologyLabels>(labelsCandidate).AccessibleTextFormatter = _ => "Retained callback description";
        Assert.Equal("Mapa usług", chart.Accessibility.Name);
        Assert.Equal("Mapa", chart.Labels.UntitledTopology);
        Assert.Throws<InvalidOperationException>(() => chart.ConfigureAccessibility(options => {
            options.Name = "Partial name";
            throw new InvalidOperationException();
        }));
        Assert.Throws<InvalidOperationException>(() => chart.ConfigureLabels(options => {
            options.AccessibleTextFormatter = _ => "Partial description";
            throw new InvalidOperationException();
        }));
        Assert.Equal(svg, chart.ToSvg());
    }

    [Fact]
    public void FlowCallbacksPreserveFactsAndDetachStepConnectorMetadata() {
        var flow = FlowArtifact.Create("workflow").AddLane("team", "Team")
            .AddStep("start", "Original step", FlowArtifactStepKind.Start, "team")
            .AddStep("end", "End", FlowArtifactStepKind.End, "team")
            .AddConnector("start", "end", "Original connector");
        var previousStep = flow.Steps[0];
        previousStep.Subtitle = "Original subtitle";
        previousStep.Metadata["source"] = "Original fact";
        var previousConnector = flow.Connectors[0];
        previousConnector.Metadata["source"] = "Original link fact";
        FlowArtifactStep? stepCandidate = null;
        FlowArtifactConnector? connectorCandidate = null;
        flow.ConfigureStep("start", step => {
            stepCandidate = step;
            step.Label = "Configured step"; step.Metadata["configured"] = "Step fact";
        }).ConfigureConnector(0, connector => {
            connectorCandidate = connector;
            connector.Label = "Configured connector"; connector.Metadata["configured"] = "Link fact";
        });
        var json = flow.ToVisualArtifact().ToInterchangeJson();
        var svg = flow.ToSvg();
        Assert.Contains("Configured step", svg);
        Assert.Contains("Configured connector", svg);
        Assert.Equal("Original subtitle", flow.Steps[0].Subtitle);
        Assert.Equal("Original fact", flow.Steps[0].Metadata["source"]);
        Assert.Equal("Original link fact", flow.Connectors[0].Metadata["source"]);

        previousStep.Label = "Previous label"; previousStep.Metadata.Clear();
        previousConnector.Label = "Previous connector"; previousConnector.Metadata.Clear();
        Assert.IsType<FlowArtifactStep>(stepCandidate).Metadata.Clear();
        Assert.IsType<FlowArtifactConnector>(connectorCandidate).Metadata.Clear();
        Assert.Throws<InvalidOperationException>(() => flow.ConfigureStep("start", step => {
            step.Label = "Partial step"; step.Metadata.Clear();
            throw new InvalidOperationException();
        }));
        Assert.Throws<InvalidOperationException>(() => flow.ConfigureConnector(0, connector => {
            connector.Label = "Partial connector"; connector.Metadata.Clear();
            throw new InvalidOperationException();
        }));
        Assert.Equal("Step fact", flow.Steps[0].Metadata["configured"]);
        Assert.Equal("Link fact", flow.Connectors[0].Metadata["configured"]);
        Assert.Equal(json, flow.ToVisualArtifact().ToInterchangeJson());
    }

    [Fact]
    public void TableRowCallbacksDetachNestedCellsAndPreserveExportAfterFailure() {
        var table = TableArtifact.Create("table").AddColumn("service", "Service").AddRow("row", "Original cell");
        var previous = table.Rows[0];
        previous.Metadata["source"] = "Row fact";
        previous.Cells[0].SortText = "Original sort";
        previous.Cells[0].Metadata["source"] = "Cell fact";
        TableArtifactRow? candidate = null;
        table.ConfigureRow(0, row => {
            candidate = row;
            row.Cells[0].DisplayText = "Configured cell";
            row.Cells[0].Metadata["configured"] = "Nested fact";
        });
        var svg = table.ToSvg();
        var png = table.ToPng();
        Assert.Contains("Configured cell", svg);
        Assert.Equal("Configured cell", table.Rows[0].Cells[0].DisplayText);
        Assert.Equal("Original sort", table.Rows[0].Cells[0].SortText);
        Assert.Equal("Row fact", table.Rows[0].Metadata["source"]);
        Assert.Equal("Cell fact", table.Rows[0].Cells[0].Metadata["source"]);

        previous.Cells[0].DisplayText = "Previous cell";
        previous.Cells[0].Metadata.Clear(); previous.Metadata.Clear();
        Assert.IsType<TableArtifactRow>(candidate).Cells[0].Metadata.Clear();
        Assert.IsType<TableArtifactRow>(candidate).Cells.Clear();
        Assert.Throws<InvalidOperationException>(() => table.ConfigureRow(0, row => {
            row.Cells[0].DisplayText = "Partial cell";
            row.Cells.Clear(); row.Metadata.Clear();
            throw new InvalidOperationException();
        }));
        Assert.Equal("Nested fact", table.Rows[0].Cells[0].Metadata["configured"]);
        Assert.Equal(svg, table.ToSvg());
        Assert.Equal(png, table.ToPng());
    }
}
