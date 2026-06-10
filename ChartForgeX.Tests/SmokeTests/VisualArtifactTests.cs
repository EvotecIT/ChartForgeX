using System;
using System.Collections.Generic;
using ChartForgeX.VisualArtifacts;
using ChartForgeX.VisualBlocks;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void TableArtifactDeclaresNativeHostCapabilities() {
        var table = TableArtifact.Create("services")
            .WithTitle("Service Inventory")
            .WithSubtitle("Native-host table contract")
            .WithCapabilities(
                TableArtifactCapabilities.Search |
                TableArtifactCapabilities.Sort |
                TableArtifactCapabilities.Filter |
                TableArtifactCapabilities.MultiSelection |
                TableArtifactCapabilities.Copy |
                TableArtifactCapabilities.Export |
                TableArtifactCapabilities.Virtualization)
            .AddColumn("name", "Name")
            .AddColumn("status", "Status", TableArtifactColumnType.Status)
            .AddColumn("latency", "Latency", TableArtifactColumnType.Number, VisualTextAlignment.Right)
            .AddRow("api", "API", "Healthy", 24)
            .AddRow("worker", "Worker", "Warning", 91);

        table.Columns[1].Metadata["facet"] = "health";
        table.WithRow(1, row => {
            row.Status = VisualStatus.Warning;
            row.Metadata["source"] = "probe";
            row.Cells[1].Status = VisualStatus.Warning;
        });

        Assert(table.Supports(TableArtifactCapabilities.Search), "TableArtifact should declare full-table search capability.");
        Assert(table.Supports(TableArtifactCapabilities.Virtualization), "TableArtifact should declare virtualization capability.");
        Assert(table.SupportsExport(VisualArtifactExportFormat.Csv), "TableArtifact should declare data export formats independently from static previews.");
        Assert(table.Columns[2].Alignment == VisualTextAlignment.Right, "TableArtifact columns should preserve static preview alignment hints.");
        Assert(table.Columns[1].Metadata["facet"] == "health", "TableArtifact columns should preserve host metadata.");
        Assert(table.Rows[1].Status == VisualStatus.Warning, "TableArtifact rows should carry status for selection and preview hosts.");
        Assert(table.Rows[1].Cells[1].Status == VisualStatus.Warning, "TableArtifact cells should carry status for selection and preview hosts.");

        var artifact = table.ToVisualArtifact();
        Assert(artifact.Kind == VisualArtifactKind.Table, "TableArtifact should wrap into a product-neutral visual artifact envelope.");
        Assert(artifact.Model == table, "VisualArtifact should keep the typed table model for native hosts.");
        Assert(artifact.SupportsExport(VisualArtifactExportFormat.Png), "VisualArtifact should expose table preview export capabilities.");
        Assert(artifact.Metadata["table.capabilities"].Contains("Virtualization", StringComparison.Ordinal), "VisualArtifact metadata should expose declared table capabilities.");
    }

    private static void TableArtifactRendersStaticPreviewThroughVisualBlocks() {
        var table = TableArtifact.Create("accounts")
            .WithTitle("Accounts")
            .WithSubtitle("Static table preview")
            .AddColumn("displayName", "Display name")
            .AddColumn("state", "State", TableArtifactColumnType.Status)
            .AddRow("a", "Ada Lovelace", "Enabled")
            .AddRow("g", "Grace Hopper", "Disabled")
            .WithRow(1, row => {
                row.Status = VisualStatus.Negative;
                row.Cells[1].Status = VisualStatus.Negative;
            });

        var block = table.ToPreviewBlock();
        var svg = table.ToSvg();
        var png = table.ToPng();

        Assert(block.Title == "Accounts", "TableArtifact preview should preserve the table title.");
        Assert(svg.Contains("data-cfx-role=\"table-header\"", StringComparison.Ordinal), "TableArtifact static preview should reuse ChartTable SVG rendering.");
        Assert(svg.Contains("data-cfx-role=\"table-status\"", StringComparison.Ordinal), "TableArtifact static preview should surface row and cell status.");
        Assert(png.Length > 64, "TableArtifact static preview should render PNG output.");
    }

    private static void TableArtifactRejectsInvalidContractShapes() {
        AssertThrows<ArgumentNullException>(() => TableArtifact.Create(null!), "TableArtifact should reject null ids.");
        AssertThrows<ArgumentException>(() => TableArtifact.Create("bad").AddColumn("", "Bad"), "TableArtifact should reject empty column ids.");
        AssertThrows<ArgumentException>(() => TableArtifact.Create("bad").AddColumn("id", "ID").AddColumn("id", "Duplicate"), "TableArtifact should reject duplicate column ids.");
        AssertThrows<ArgumentException>(() => TableArtifact.Create("bad").AddColumn("id", "ID").AddRow("row", "a", "b"), "TableArtifact should reject row value count mismatches.");
        AssertThrows<InvalidOperationException>(() => TableArtifact.Create("bad").AddColumn("id", "ID").AddRow("row", "a").AddColumn("next", "Next"), "TableArtifact should reject adding columns after rows.");
        AssertThrows<ArgumentOutOfRangeException>(() => TableArtifact.Create("bad").WithCapabilities((TableArtifactCapabilities)1024), "TableArtifact should reject undefined capability flags.");
        AssertThrows<ArgumentOutOfRangeException>(() => TableArtifact.Create("bad").Capabilities = (TableArtifactCapabilities)1024, "TableArtifact should reject undefined capability flags through the public setter.");
        AssertThrows<ArgumentOutOfRangeException>(() => TableArtifact.Create("bad").ExportFormats = (VisualArtifactExportFormat)1024, "TableArtifact should reject undefined export format flags.");
        AssertThrows<ArgumentOutOfRangeException>(() => new TableArtifactColumn("bad", "Bad", (TableArtifactColumnType)999), "TableArtifactColumn should reject unknown data types.");
        AssertThrows<ArgumentOutOfRangeException>(() => new TableArtifactColumn("bad", "Bad", width: 0), "TableArtifactColumn should reject invalid width hints.");
        AssertThrows<ArgumentOutOfRangeException>(() => new TableArtifactColumn("bad", "Bad").Width = -1, "TableArtifactColumn should reject invalid width hints through the public setter.");
        AssertThrows<ArgumentOutOfRangeException>(() => new TableArtifactRow("bad").Status = (VisualStatus)999, "TableArtifactRow should reject unknown status values.");
        AssertThrows<ArgumentOutOfRangeException>(() => new TableArtifactCell("bad").Status = (VisualStatus)999, "TableArtifactCell should reject unknown status values.");
        AssertThrows<ArgumentOutOfRangeException>(() => new VisualArtifact().ExportFormats = (VisualArtifactExportFormat)1024, "VisualArtifact should reject undefined export format flags.");
        AssertThrows<ArgumentOutOfRangeException>(() => new TableArtifactQuery { Offset = -1 }, "TableArtifactQuery should reject negative virtual offsets.");
        AssertThrows<ArgumentOutOfRangeException>(() => new TableArtifactQuery { Limit = 0 }, "TableArtifactQuery should reject empty virtual windows.");
    }

    private static void TableArtifactVirtualQueryContractIsHostNeutral() {
        var query = new TableArtifactQuery {
            SearchText = "warning",
            Offset = 25,
            Limit = 50
        };
        query.Sorts.Add(new TableArtifactSort("latency", descending: true));
        query.Filters.Add(new TableArtifactFilter("status", "warning") { Operator = "equals" });

        var rows = new List<TableArtifactRow> {
            new("worker") {
                Cells = {
                    new TableArtifactCell("Worker"),
                    new TableArtifactCell("Warning") { Status = VisualStatus.Warning }
                },
                Status = VisualStatus.Warning
            }
        };
        var result = new TableArtifactQueryResult(rows, totalRowCount: 125);

        Assert(query.SearchText == "warning", "TableArtifactQuery should carry host-neutral search text.");
        Assert(query.Sorts[0].ColumnId == "latency" && query.Sorts[0].Descending, "TableArtifactQuery should carry host-neutral sort descriptors.");
        Assert(query.Filters[0].ColumnId == "status" && query.Filters[0].Operator == "equals", "TableArtifactQuery should carry host-neutral filter descriptors.");
        Assert(result.Rows.Count == 1 && result.TotalRowCount == 125, "TableArtifactQueryResult should carry a virtualized row window and total count.");
    }

    private static void SequenceArtifactRendersStaticPreviewAndEnvelope() {
        var sequence = SequenceArtifact.Create("incident")
            .WithTitle("Incident Flow")
            .WithSubtitle("Native sequence preview")
            .WithSize(760, 420)
            .AddParticipant("user", "User", SequenceArtifactParticipantKind.Actor)
            .AddParticipant("api", "API")
            .AddParticipant("db", "Database", SequenceArtifactParticipantKind.Database)
            .AddMessage("user", "api", "Request")
            .AddMessage("api", "db", "Store", SequenceArtifactMessageLineStyle.Dashed)
            .AddNote(SequenceArtifactNotePlacement.RightOf, new[] { "api" }, "Processing")
            .AddBlock(SequenceArtifactBlockKind.Loop, "Retry", 0, 1);

        var artifact = sequence.ToVisualArtifact();
        var svg = sequence.ToSvg();
        var png = sequence.ToPng();

        Assert(artifact.Kind == VisualArtifactKind.Sequence, "SequenceArtifact should wrap into a product-neutral sequence artifact envelope.");
        Assert(artifact.Model == sequence, "SequenceArtifact envelope should keep the typed sequence model.");
        Assert(artifact.Metadata["sequence.participants"] == "3", "SequenceArtifact envelope should expose participant counts.");
        Assert(artifact.SupportsExport(VisualArtifactExportFormat.Svg), "SequenceArtifact should declare SVG export support.");
        Assert(svg.Contains("data-cfx-role=\"sequence-message\"", StringComparison.Ordinal), "SequenceArtifact SVG should expose message regions.");
        Assert(svg.Contains("data-cfx-role=\"sequence-note\"", StringComparison.Ordinal), "SequenceArtifact SVG should expose note regions.");
        Assert(png.Length > 64 && png[0] == 0x89 && png[1] == 0x50 && png[2] == 0x4E && png[3] == 0x47, "SequenceArtifact PNG should emit a valid PNG.");
    }
}
