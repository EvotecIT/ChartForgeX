using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Reports tests of ordinary Unix filesystem behavior as skipped on other hosts.</summary>
public sealed class UnixFactAttribute : FactAttribute {
    public UnixFactAttribute() {
        if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS()) Skip = "This contract exercises Unix filesystem behavior.";
    }
}

/// <summary>Reports Unix-specific theory rows as skipped on other hosts.</summary>
public sealed class UnixTheoryAttribute : TheoryAttribute {
    public UnixTheoryAttribute() {
        if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS()) Skip = "This contract exercises Unix filesystem behavior.";
    }
}

/// <summary>Reports installed Windows font fixtures as missing instead of passing an unexecuted contract.</summary>
public sealed class WindowsFontFactAttribute : FactAttribute {
    public WindowsFontFactAttribute(params string[] files) {
        var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Fonts");
        foreach (var file in files) {
            if (!File.Exists(Path.Combine(root, file))) {
                Skip = "This contract requires the installed Windows font " + file + ".";
                return;
            }
        }
    }
}
