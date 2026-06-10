using System;
using System.Collections.Generic;

namespace ChartForgeX.VisualArtifacts;

/// <summary>
/// Describes a host-neutral table query for search, sorting, filtering, selection, and virtualization.
/// </summary>
public sealed class TableArtifactQuery {
    private string _searchText = string.Empty;
    private readonly List<TableArtifactSort> _sorts = new();
    private readonly List<TableArtifactFilter> _filters = new();
    private int _offset;
    private int _limit = 100;

    /// <summary>Gets or sets the full-table search text.</summary>
    public string SearchText { get => _searchText; set => _searchText = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets requested sort expressions.</summary>
    public List<TableArtifactSort> Sorts => _sorts;

    /// <summary>Gets requested filter expressions.</summary>
    public List<TableArtifactFilter> Filters => _filters;

    /// <summary>Gets or sets the zero-based virtualized row offset.</summary>
    public int Offset {
        get => _offset;
        set {
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value), value, "Table query offset must be non-negative.");
            _offset = value;
        }
    }

    /// <summary>Gets or sets the maximum number of rows requested.</summary>
    public int Limit {
        get => _limit;
        set {
            if (value < 1) throw new ArgumentOutOfRangeException(nameof(value), value, "Table query limit must be greater than zero.");
            _limit = value;
        }
    }
}

/// <summary>
/// Describes one table sort expression.
/// </summary>
public sealed class TableArtifactSort {
    private string _columnId;

    /// <summary>Initializes a table sort expression.</summary>
    public TableArtifactSort(string columnId, bool descending = false) {
        _columnId = columnId ?? throw new ArgumentNullException(nameof(columnId));
        Descending = descending;
    }

    /// <summary>Gets or sets the column id.</summary>
    public string ColumnId { get => _columnId; set => _columnId = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets whether sorting should be descending.</summary>
    public bool Descending { get; set; }
}

/// <summary>
/// Describes one table filter expression.
/// </summary>
public sealed class TableArtifactFilter {
    private string _columnId;
    private string _operator = "contains";
    private string _value = string.Empty;

    /// <summary>Initializes a table filter expression.</summary>
    public TableArtifactFilter(string columnId, string value) {
        _columnId = columnId ?? throw new ArgumentNullException(nameof(columnId));
        _value = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>Gets or sets the column id.</summary>
    public string ColumnId { get => _columnId; set => _columnId = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the product-neutral filter operator token.</summary>
    public string Operator { get => _operator; set => _operator = value ?? throw new ArgumentNullException(nameof(value)); }

    /// <summary>Gets or sets the filter value.</summary>
    public string Value { get => _value; set => _value = value ?? throw new ArgumentNullException(nameof(value)); }
}

/// <summary>
/// Provides virtualized table data for native hosts.
/// </summary>
public interface ITableArtifactDataProvider {
    /// <summary>Queries a virtualized table data window.</summary>
    TableArtifactQueryResult Query(TableArtifactQuery query);
}

/// <summary>
/// Describes a virtualized table query result.
/// </summary>
public sealed class TableArtifactQueryResult {
    /// <summary>Initializes a table query result.</summary>
    public TableArtifactQueryResult(IReadOnlyList<TableArtifactRow> rows, long totalRowCount) {
        Rows = rows ?? throw new ArgumentNullException(nameof(rows));
        if (totalRowCount < 0) throw new ArgumentOutOfRangeException(nameof(totalRowCount), totalRowCount, "Total row count must be non-negative.");
        TotalRowCount = totalRowCount;
    }

    /// <summary>Gets the returned rows.</summary>
    public IReadOnlyList<TableArtifactRow> Rows { get; }

    /// <summary>Gets the total row count before virtualization.</summary>
    public long TotalRowCount { get; }
}
