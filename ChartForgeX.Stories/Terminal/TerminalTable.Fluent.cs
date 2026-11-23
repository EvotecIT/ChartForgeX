using PowerForge.Fluent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System;

namespace ChartForgeX.Terminal;

/// <summary>Hand-written fluent methods of <see cref="TerminalTable"/> that do more than set one property.</summary>
public static partial class TerminalTableFluent {
    /// <summary>Sets table columns.</summary>
    public static global::ChartForgeX.Terminal.TerminalTable Columns(this global::ChartForgeX.Terminal.TerminalTable terminalTable, params string[] columns)
        => (terminalTable ?? throw new System.ArgumentNullException(nameof(terminalTable))).SetColumns(columns);
}
