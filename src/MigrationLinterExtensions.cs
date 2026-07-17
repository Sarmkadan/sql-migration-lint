using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SqlMigrationLint;

/// <summary>
/// Provides extension methods for <see cref="MigrationLinter"/> and <see cref="LintReport"/>
/// to enhance their functionality with common operations like filtering, grouping, and reporting.
/// </summary>
public static class MigrationLinterExtensions
{
    /// <summary>
    /// Runs the linter and returns a report with findings filtered by the specified severity level.
    /// </summary>
    /// <param name="linter">The linter instance.</param>
    /// <param name="rootPath">The directory that contains the Migrations folder.</param>
    /// <param name="severity">The severity level to filter by.</param>
    /// <returns>An enumerable of findings with the specified severity.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="linter"/> or <paramref name="rootPath"/> is null.</exception>
    public static IEnumerable<LintFinding> GetFindingsBySeverity(
        this MigrationLinter linter,
        string rootPath,
        LintSeverity severity)
    {
        ArgumentNullException.ThrowIfNull(linter);
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);

        var report = linter.Lint(rootPath);
        return report.GetFindingsBySeverity(severity);
    }

    /// <summary>
    /// Runs the linter and returns a report with findings grouped by their associated file path.
    /// </summary>
    /// <param name="linter">The linter instance.</param>
    /// <param name="rootPath">The directory that contains the Migrations folder.</param>
    /// <returns>A dictionary mapping file paths to collections of findings for that file.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="linter"/> or <paramref name="rootPath"/> is null.</exception>
    public static IReadOnlyDictionary<string, IReadOnlyList<LintFinding>> GroupFindingsByFile(
        this MigrationLinter linter,
        string rootPath)
    {
        ArgumentNullException.ThrowIfNull(linter);
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);

        var report = linter.Lint(rootPath);
        return report.GroupFindingsByFile();
    }

    /// <summary>
    /// Runs the linter and returns a report with the total count of findings.
    /// </summary>
    /// <param name="linter">The linter instance.</param>
    /// <param name="rootPath">The directory that contains the Migrations folder.</param>
    /// <returns>The total number of findings.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="linter"/> or <paramref name="rootPath"/> is null.</exception>
    public static int GetTotalFindingsCount(
        this MigrationLinter linter,
        string rootPath)
    {
        ArgumentNullException.ThrowIfNull(linter);
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);

        var report = linter.Lint(rootPath);
        return report.GetTotalFindingsCount();
    }

    /// <summary>
    /// Runs the linter and returns a report with findings grouped by severity level.
    /// </summary>
    /// <param name="linter">The linter instance.</param>
    /// <param name="rootPath">The directory that contains the Migrations folder.</param>
    /// <returns>A dictionary mapping severity levels to their respective finding counts.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="linter"/> or <paramref name="rootPath"/> is null.</exception>
    public static IReadOnlyDictionary<LintSeverity, int> GetFindingsCountBySeverity(
        this MigrationLinter linter,
        string rootPath)
    {
        ArgumentNullException.ThrowIfNull(linter);
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);

        var report = linter.Lint(rootPath);
        return report.GetFindingsCountBySeverity();
    }

    /// <summary>
    /// Runs the linter and determines whether the report has any findings of the specified severity level.
    /// </summary>
    /// <param name="linter">The linter instance.</param>
    /// <param name="rootPath">The directory that contains the Migrations folder.</param>
    /// <param name="severity">The severity level to check for.</param>
    /// <returns>True if findings of the specified severity exist; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="linter"/> or <paramref name="rootPath"/> is null.</exception>
    public static bool HasFindingsOfSeverity(
        this MigrationLinter linter,
        string rootPath,
        LintSeverity severity)
    {
        ArgumentNullException.ThrowIfNull(linter);
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);

        var report = linter.Lint(rootPath);
        return report.HasFindingsOfSeverity(severity);
    }

    /// <summary>
    /// Runs the linter and returns the percentage of migrations that have findings.
    /// </summary>
    /// <param name="linter">The linter instance.</param>
    /// <param name="rootPath">The directory that contains the Migrations folder.</param>
    /// <returns>The percentage of migrations with findings, or 0 if no migrations were scanned.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="linter"/> or <paramref name="rootPath"/> is null.</exception>
    public static double GetFindingsPercentage(
        this MigrationLinter linter,
        string rootPath)
    {
        ArgumentNullException.ThrowIfNull(linter);
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);

        var report = linter.Lint(rootPath);
        return report.GetFindingsPercentage();
    }

    /// <summary>
    /// Runs the linter and returns the highest severity level among all findings.
    /// </summary>
    /// <param name="linter">The linter instance.</param>
    /// <param name="rootPath">The directory that contains the Migrations folder.</param>
    /// <returns>The highest severity level found, or <see cref="RiskLevel.None"/> if no findings exist.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="linter"/> or <paramref name="rootPath"/> is null.</exception>
    public static RiskLevel GetMaxFindingSeverity(
        this MigrationLinter linter,
        string rootPath)
    {
        ArgumentNullException.ThrowIfNull(linter);
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);

        var report = linter.Lint(rootPath);
        return report.GetMaxFindingSeverity();
    }

    /// <summary>
    /// Runs the linter and returns all unique rule names that produced findings.
    /// </summary>
    /// <param name="linter">The linter instance.</param>
    /// <param name="rootPath">The directory that contains the Migrations folder.</param>
    /// <returns>An enumerable of unique rule names.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="linter"/> or <paramref name="rootPath"/> is null.</exception>
    public static IEnumerable<string> GetRuleNamesWithFindings(
        this MigrationLinter linter,
        string rootPath)
    {
        ArgumentNullException.ThrowIfNull(linter);
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);

        var report = linter.Lint(rootPath);
        return report.GetRuleNamesWithFindings();
    }

    /// <summary>
    /// Runs the linter and returns all findings that are blockers.
    /// </summary>
    /// <param name="linter">The linter instance.</param>
    /// <param name="rootPath">The directory that contains the Migrations folder.</param>
    /// <returns>An enumerable of blocker-level findings.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="linter"/> or <paramref name="rootPath"/> is null.</exception>
    public static IEnumerable<LintFinding> GetBlockerFindings(
        this MigrationLinter linter,
        string rootPath)
    {
        ArgumentNullException.ThrowIfNull(linter);
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);

        var report = linter.Lint(rootPath);
        return report.GetBlockerFindings();
    }

    /// <summary>
    /// Runs the linter and returns all findings that are dangers.
    /// </summary>
    /// <param name="linter">The linter instance.</param>
    /// <param name="rootPath">The directory that contains the Migrations folder.</param>
    /// <returns>An enumerable of danger-level findings.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="linter"/> or <paramref name="rootPath"/> is null.</exception>
    public static IEnumerable<LintFinding> GetDangerFindings(
        this MigrationLinter linter,
        string rootPath)
    {
        ArgumentNullException.ThrowIfNull(linter);
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);

        var report = linter.Lint(rootPath);
        return report.GetDangerFindings();
    }

    /// <summary>
    /// Runs the linter and returns all findings that are warnings.
    /// </summary>
    /// <param name="linter">The linter instance.</param>
    /// <param name="rootPath">The directory that contains the Migrations folder.</param>
    /// <returns>An enumerable of warning-level findings.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="linter"/> or <paramref name="rootPath"/> is null.</exception>
    public static IEnumerable<LintFinding> GetWarningFindings(
        this MigrationLinter linter,
        string rootPath)
    {
        ArgumentNullException.ThrowIfNull(linter);
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);

        var report = linter.Lint(rootPath);
        return report.GetWarningFindings();
    }

    /// <summary>
    /// Gets all findings of the specified severity level from a lint report.
    /// </summary>
    /// <param name="report">The lint report.</param>
    /// <param name="severity">The severity level to filter by.</param>
    /// <returns>An enumerable of findings with the specified severity.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="report"/> is null.</exception>
    public static IEnumerable<LintFinding> GetFindingsBySeverity(
        this LintReport report,
        LintSeverity severity)
    {
        ArgumentNullException.ThrowIfNull(report);

        return report.Findings.Where(f => f.Severity == severity);
    }

    /// <summary>
    /// Groups findings by their associated file path.
    /// </summary>
    /// <param name="report">The lint report.</param>
    /// <returns>A dictionary mapping file paths to collections of findings for that file.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="report"/> is null.</exception>
    public static IReadOnlyDictionary<string, IReadOnlyList<LintFinding>> GroupFindingsByFile(
        this LintReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        return report.Findings
            .Where(f => !string.IsNullOrEmpty(f.File))
            .GroupBy(f => f.File!, StringComparer.Ordinal)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<LintFinding>)g.ToArray(),
                StringComparer.Ordinal);
    }

    /// <summary>
    /// Gets the total count of findings across all severity levels.
    /// </summary>
    /// <param name="report">The lint report.</param>
    /// <returns>The total number of findings.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="report"/> is null.</exception>
    public static int GetTotalFindingsCount(this LintReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        return report.Findings.Count;
    }

    /// <summary>
    /// Gets the count of findings grouped by severity level.
    /// </summary>
    /// <param name="report">The lint report.</param>
    /// <returns>A dictionary mapping severity levels to their respective finding counts.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="report"/> is null.</exception>
    public static IReadOnlyDictionary<LintSeverity, int> GetFindingsCountBySeverity(
        this LintReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        return report.Findings
            .GroupBy(f => f.Severity)
            .ToDictionary(
                g => g.Key,
                g => g.Count());
    }

    /// <summary>
    /// Determines whether the report has any findings of the specified severity level.
    /// </summary>
    /// <param name="report">The lint report.</param>
    /// <param name="severity">The severity level to check for.</param>
    /// <returns>True if findings of the specified severity exist; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="report"/> is null.</exception>
    public static bool HasFindingsOfSeverity(
        this LintReport report,
        LintSeverity severity)
    {
        ArgumentNullException.ThrowIfNull(report);

        return report.Findings.Any(f => f.Severity == severity);
    }

    /// <summary>
    /// Gets the percentage of migrations that have findings.
    /// </summary>
    /// <param name="report">The lint report.</param>
    /// <returns>The percentage of migrations with findings, or 0 if no migrations were scanned.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="report"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="report"/>.MigrationsScanned is 0.</exception>
    public static double GetFindingsPercentage(this LintReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        if (report.MigrationsScanned <= 0)
        {
            return 0.0;
        }

        int findingsCount = report.Findings.Count;
        return Math.Round((double)findingsCount / report.MigrationsScanned * 100, 2);
    }

    /// <summary>
    /// Gets the highest severity level among all findings.
    /// </summary>
    /// <param name="report">The lint report.</param>
    /// <returns>The highest severity level found, or <see cref="RiskLevel.None"/> if no findings exist.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="report"/> is null.</exception>
    public static RiskLevel GetMaxFindingSeverity(this LintReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        if (report.Findings.Count == 0)
        {
            return RiskLevel.None;
        }

        return report.Findings
            .Select(f => f.Severity switch
            {
                LintSeverity.Blocker => RiskLevel.Blocker,
                LintSeverity.Danger => RiskLevel.Danger,
                LintSeverity.Warning => RiskLevel.Warning,
                _ => RiskLevel.None
            })
            .Max();
    }

    /// <summary>
    /// Gets all unique rule names that produced findings.
    /// </summary>
    /// <param name="report">The lint report.</param>
    /// <returns>An enumerable of unique rule names.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="report"/> is null.</exception>
    public static IEnumerable<string> GetRuleNamesWithFindings(this LintReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        return report.Findings
            .Select(f => f.RuleName)
            .Distinct(StringComparer.Ordinal);
    }

    /// <summary>
    /// Gets all findings that are blockers.
    /// </summary>
    /// <param name="report">The lint report.</param>
    /// <returns>An enumerable of blocker-level findings.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="report"/> is null.</exception>
    public static IEnumerable<LintFinding> GetBlockerFindings(this LintReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        return report.Findings.Where(f => f.Severity == LintSeverity.Blocker);
    }

    /// <summary>
    /// Gets all findings that are dangers.
    /// </summary>
    /// <param name="report">The lint report.</param>
    /// <returns>An enumerable of danger-level findings.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="report"/> is null.</exception>
    public static IEnumerable<LintFinding> GetDangerFindings(this LintReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        return report.Findings.Where(f => f.Severity == LintSeverity.Danger);
    }

    /// <summary>
    /// Gets all findings that are warnings.
    /// </summary>
    /// <param name="report">The lint report.</param>
    /// <returns>An enumerable of warning-level findings.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="report"/> is null.</exception>
    public static IEnumerable<LintFinding> GetWarningFindings(this LintReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        return report.Findings.Where(f => f.Severity == LintSeverity.Warning);
    }
}