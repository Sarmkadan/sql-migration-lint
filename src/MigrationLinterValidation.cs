using System;
using System.Collections.Generic;

namespace SqlMigrationLint;

/// <summary>
/// Provides validation helpers for <see cref="MigrationLinter"/> instances.
/// </summary>
public static class MigrationLinterValidation
{
    /// <summary>
    /// Validates that a <see cref="MigrationLinter"/> instance is in a valid state.
    /// </summary>
    /// <param name="value">The <see cref="MigrationLinter"/> instance to validate.</param>
    /// <returns>An immutable list of human-readable validation problems; empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this MigrationLinter value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // Validate the LintReport if one has been generated
        if (value.LintReport is { } report)
        {
            if (report.Findings is null)
            {
                problems.Add("LintReport.Findings collection is null.");
            }

            if (report.MigrationsScanned < 0)
            {
                problems.Add("LintReport.MigrationsScanned cannot be negative.");
            }

            if (report.MaxRisk is < RiskLevel.None or > RiskLevel.Blocker)
            {
                problems.Add("LintReport.MaxRisk has an invalid RiskLevel value.");
            }
        }

        return problems;
    }

    /// <summary>
    /// Determines whether a <see cref="MigrationLinter"/> instance is in a valid state.
    /// </summary>
    /// <param name="value">The <see cref="MigrationLinter"/> instance to check.</param>
    /// <returns><see langword="true"/> if valid; otherwise, <see langword="false"/>.</returns>
    public static bool IsValid(this MigrationLinter value) => value?.Validate().Count == 0;

    /// <summary>
    /// Ensures that a <see cref="MigrationLinter"/> instance is in a valid state.
    /// </summary>
    /// <param name="value">The <see cref="MigrationLinter"/> instance to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is not valid, containing a list of problems.</exception>
    public static void EnsureValid(this MigrationLinter value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = value.Validate();
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"MigrationLinter is not valid. Problems: {string.Join(", ", problems)}",
                nameof(value));
        }
    }
}