using System;

internal static class Program {
    private static void Main() {
        PackageAssertions.CoreFixtures();
        PackageAssertions.Payload("ChartForgeX");
        Console.WriteLine("Core package boundaries and static exports passed.");
    }
}
