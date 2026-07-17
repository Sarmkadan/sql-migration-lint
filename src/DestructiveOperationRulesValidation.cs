using System;
using System.Collections.Generic;

namespace SqlMigrationLint;

/// <summary>
/// Provides validation helpers for <see cref="DestructiveOperationRules"/> static class and its members.
/// </summary>
public static class DestructiveOperationRulesValidation
{
    /// <summary>
    /// Validates the <see cref="DestructiveOperationRules"/> static class members.
    /// </summary>
    /// <returns>A list of validation problems; empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <see cref="DestructiveOperationRules"/> is null.</exception>
    public static IReadOnlyList<string> Validate()
    {
        ArgumentNullException.ThrowIfNull(DestructiveOperationRules.All);

        var problems = new List<string>();

        // Validate All property
        if (DestructiveOperationRules.All.Count == 0)
        {
            problems.Add("The All property must contain at least one lint rule.");
        }

        // Validate each rule in All
        foreach (var rule in DestructiveOperationRules.All)
        {
            ArgumentNullException.ThrowIfNull(rule);

            if (string.IsNullOrWhiteSpace(rule.Name))
            {
                problems.Add($"Rule '{rule.GetType().Name}' has an invalid or empty Name.");
            }

            if (string.IsNullOrWhiteSpace(rule.Description))
            {
                problems.Add($"Rule '{rule.Name ?? rule.GetType().Name}' has an invalid or empty Description.");
            }

            if (rule.Severity is not (LintSeverity.Blocker or LintSeverity.Danger or LintSeverity.Warning))
            {
                problems.Add($"Rule '{rule.Name}' has an invalid Severity value: {rule.Severity}.");
            }
        }

        return problems;
    }

    /// <summary>
    /// Determines whether the <see cref="DestructiveOperationRules"/> static class is valid.
    /// </summary>
    /// <returns>True if valid; otherwise, false.</returns>
    public static bool IsValid() => Validate().Count == 0;

    /// <summary>
    /// Ensures that the <see cref="DestructiveOperationRules"/> static class is valid.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown if validation fails, containing the list of problems.</exception>
    /// <exception cref="ArgumentNullException">Thrown if <see cref="DestructiveOperationRules"/> is null.</exception>
    public static void EnsureValid()
    {
        var problems = Validate();

        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"DestructiveOperationRules validation failed:{Environment.NewLine} - {string.Join($"{Environment.NewLine} - ", problems)}",
                nameof(DestructiveOperationRules));
        }
    }
}