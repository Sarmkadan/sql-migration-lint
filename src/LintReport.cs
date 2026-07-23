using System;
using System.Collections.Generic;
using System.Linq;

namespace SqlMigrationLint;

/// <summary>
/// Represents the result of a lint run.
/// <para>This is a pure DTO (Data Transfer Object) that contains only the raw results of linting.
/// It does NOT contain any formatting, rendering, or output logic. All such concerns are delegated
/// to dedicated writer classes: <see cref="ConsoleReportWriter"/>, <see cref="GitHubAnnotationsWriter"/>, and <see cref="JsonReportWriter"/>.</para>
/// </summary>
public sealed class LintReport
{
    /// <summary>
    /// All findings produced by the lint run.
    /// </summary>
    public IReadOnlyList<LintFinding> Findings { get; }

    /// <summary>
    /// Number of migration files that were examined.
    /// </summary>
    public int MigrationsScanned { get; }

    /// <summary>
    /// Whether any blocker‑severity findings were reported.
    /// </summary>
    public bool HasBlockers { get; }

    /// <summary>
    /// The highest risk level observed among the findings.
    /// </summary>
    public RiskLevel MaxRisk { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="LintReport"/> class.
    /// </summary>
    /// <param name="findings">All findings produced by the lint run.</param>
    /// <param name="migrationsScanned">Number of migration files that were examined.</param>
    /// <param name="hasBlockers">Whether any blocker‑severity findings were reported.</param>
    /// <param name="maxRisk">The highest risk level observed among the findings.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="findings"/> is null.</exception>
    public LintReport(
        IReadOnlyList<LintFinding> findings,
        int migrationsScanned,
        bool hasBlockers,
        RiskLevel maxRisk)
    {
        Findings = findings ?? throw new ArgumentNullException(nameof(findings));
        MigrationsScanned = migrationsScanned;
        HasBlockers = hasBlockers;
        MaxRisk = maxRisk;
    }

    /// <summary>
    /// Computes the exit code based on the severity level at which to fail.
    /// </summary>
    /// <param name="failOnSeverity">The severity level at which to exit with error code 1.</param>
    /// <returns>0 if no findings exceed the fail threshold, 1 otherwise.</returns>
    public int ComputeExitCode(LintFindingSeverity failOnSeverity)
    {
        if (Findings.Count == 0)
        {
            return 0; // Success - no findings
        }

        var highestFindingSeverity = Findings.Max(f => MapToFindingSeverity(f.Severity));

        return highestFindingSeverity >= failOnSeverity ? 1 : 0;
    }

    /// <summary>
    /// Maps <see cref="LintSeverity"/> to <see cref="LintFindingSeverity"/> for exit code computation.
    /// </summary>
    /// <param name="severity">The severity level to map.</param>
    /// <returns>The mapped severity level.</returns>
    private static LintFindingSeverity MapToFindingSeverity(LintSeverity severity)
    {
        return severity switch
        {
            LintSeverity.Blocker => LintFindingSeverity.Error,
            LintSeverity.Danger => LintFindingSeverity.Error,
            LintSeverity.Warning => LintFindingSeverity.Warning,
            _ => LintFindingSeverity.Info
        };
    }
}

/// <summary>
/// Represents the severity levels used for the <see cref="LintReport.MaxRisk"/> calculation.
/// </summary>
public enum RiskLevel
{
    None = 0,
    Warning = 1,
    Danger = 2,
    Blocker = 3
}