using System;
using System.Collections.Generic;
using System.Globalization;

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

        // MigrationLinter itself has no state to validate beyond constructor argument validation
        // which is already handled by the constructor. The LintReport property contains the actual data.

        return problems;
    }

    /// <summary>
    /// Determines whether a <see cref="MigrationLinter"/> instance is in a valid state.
    /// </summary>
    /// <param name="value">The <see cref="MigrationLinter"/> instance to check.</param>
    /// <returns><see langword="true"/> if valid; otherwise, <see langword="false"/>.</returns>
    public static bool IsValid(this MigrationLinter value)
    {
        return value?.Validate().Count == 0;
    }

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