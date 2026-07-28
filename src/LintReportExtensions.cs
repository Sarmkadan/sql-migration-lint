using System;
using System.Collections.Generic;
using System.Linq;

namespace SqlMigrationLint;

/// <summary>
/// Provides extension methods for <see cref="LintReport"/> to facilitate common analysis operations.
/// </summary>
public static class LintReportExtensions
{
    /// <summary>
    /// Checks if the lint report contains any findings that are considered errors (Blocker or Danger).
    /// </summary>
    /// <param name="report">The lint report.</param>
    /// <returns>True if there are any blocker or danger findings; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="report"/> is null.</exception>
    public static bool HasErrors(this LintReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        return report.Findings.Any(f => f.Severity == LintSeverity.Blocker || f.Severity == LintSeverity.Danger);
    }

    /// <summary>
    /// Filters the lint report findings for a specific file.
    /// </summary>
    /// <param name="report">The lint report.</param>
    /// <param name="path">The file path to filter by.</param>
    /// <returns>An enumerable of findings associated with the specified file.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="report"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="path"/> is null or empty.</exception>
    public static IEnumerable<LintFinding> ForFile(this LintReport report, string path)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return report.Findings.Where(f => string.Equals(f.File, path, StringComparison.Ordinal));
    }

    /// <summary>
    /// Counts the findings grouped by their rule name.
    /// </summary>
    /// <param name="report">The lint report.</param>
    /// <returns>A dictionary mapping rule names to their finding counts.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="report"/> is null.</exception>
    public static IReadOnlyDictionary<string, int> CountByRule(this LintReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        return report.Findings
            .GroupBy(f => f.RuleName, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);
    }

    /// <summary>
    /// Generates a summary line for the lint report.
    /// </summary>
    /// <param name="report">The lint report.</param>
    /// <returns>A string summary of errors, warnings, and files affected.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="report"/> is null.</exception>
    public static string ToSummaryLine(this LintReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        var blockerCount = report.Findings.Count(f => f.Severity == LintSeverity.Blocker);
        var dangerCount = report.Findings.Count(f => f.Severity == LintSeverity.Danger);
        var warningCount = report.Findings.Count(f => f.Severity == LintSeverity.Warning);
        var filesWithIssues = report.Findings
            .Where(f => !string.IsNullOrEmpty(f.File))
            .Select(f => f.File!)
            .Distinct(StringComparer.Ordinal)
            .Count();
        
        var errorCount = blockerCount + dangerCount;
        
        return $"{errorCount} error{(errorCount == 1 ? "" : "s")}, {warningCount} warning{(warningCount == 1 ? "" : "s")} across {filesWithIssues} file{(filesWithIssues == 1 ? "" : "s")}";
    }
}
