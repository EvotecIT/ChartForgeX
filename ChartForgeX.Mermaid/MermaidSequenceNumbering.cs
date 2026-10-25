using System;
using System.Collections.Generic;
using System.Globalization;

namespace ChartForgeX.Mermaid;

/// <summary>Evaluates located numbering controls once per message occurrence without changing message identity.</summary>
internal static class MermaidSequenceNumbering {
    internal readonly struct Value {
        internal Value(decimal number, bool visible) { Number = number; Visible = visible; }
        internal decimal Number { get; }
        internal bool Visible { get; }
    }

    /// <summary>Resolves counters at message spans; false identifies the authored control whose consumed range is invalid.</summary>
    internal static bool TryResolve(MermaidSequenceDocument document, out List<Value> values, out MermaidSourceSpan invalidSpan) {
        values = new List<Value>(); invalidSpan = default;
        decimal current = 1, increment = 1;
        var enabled = false; var nextControl = 0;
        var forceNumbers = document.Configuration.Sequence?.ShowSequenceNumbers == true;
        for (var index = 0; index < document.Messages.Count; index++) {
            var pendingIncrement = index > 0;
            var message = document.Messages[index];
            while (nextControl < document.AutonumberStatements.Count && Before(document.AutonumberStatements[nextControl].Span, message.Span)) {
                var control = document.AutonumberStatements[nextControl++];
                enabled = control.Enabled;
                if (control.Start != null) {
                    invalidSpan = control.Span;
                    if (!decimal.TryParse(control.Start, NumberStyles.Float, CultureInfo.InvariantCulture, out current)) return false;
                    pendingIncrement = false;
                    increment = 1;
                }
                if (control.Increment != null) {
                    invalidSpan = control.Span;
                    if (!decimal.TryParse(control.Increment, NumberStyles.Float, CultureInfo.InvariantCulture, out increment)) return false;
                }
                if (invalidSpan.Line == 0) invalidSpan = control.Span;
            }
            try { if (pendingIncrement) current = checked(current + increment); }
            catch (OverflowException) { return false; }
            values.Add(new Value(current, enabled || forceNumbers));
        }
        return true;
    }

    internal static bool Before(MermaidSourceSpan left, MermaidSourceSpan right) =>
        left.Line < right.Line || left.Line == right.Line && left.Column < right.Column;
}
