using System;

namespace SqlMigrationLint;

/// <summary>
/// Writes lint findings to the console in a human-readable format.
/// </summary>
public static class ConsoleReportWriter
{
    /// <summary>
    /// Writes all findings to the console output with appropriate colors.
    /// </summary>
    /// <param name="report">The lint report to write.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="report"/> is null.</exception>
    public static void WriteReport(LintReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        if (report.Findings.Count == 0)
        {
            Console.WriteLine("No issues found. All migrations passed linting.");
            return;
        }

        foreach (var finding in report.Findings)
        {
            var color = GetSeverityColor(finding.Severity);
            Console.ForegroundColor = color;
            Console.WriteLine($"[{finding.Severity}] {finding.RuleName}: {finding.Message} ({finding.File}:{finding.Line ?? 0 })");
            Console.ResetColor();
        }
    }

    /// <summary>
    /// Gets the appropriate console color for a given severity level.
    /// </summary>
    /// <param name="severity">The severity level.</param>
    /// <returns>The console color.</returns>
    private static ConsoleColor GetSeverityColor(LintSeverity severity)
    {
        return severity switch
        {
            LintSeverity.Blocker => ConsoleColor.Red,
            LintSeverity.Danger => ConsoleColor.DarkYellow,
            LintSeverity.Warning => ConsoleColor.Yellow,
            _ => ConsoleColor.White
        };
    }
}