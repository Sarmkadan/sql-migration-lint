using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SqlMigrationLint;

/// <summary>
/// Provides validation helpers for <see cref="AddColumnWithDefaultOptions"/>.
/// </summary>
public static class AddColumnWithDefaultOptionsValidation
{
    /// <summary>
    /// Gets the name of the option that holds the exempt table patterns, as written in .sqlmigrationlint.json.
    /// </summary>
    public const string ExemptTablePatternsOptionName = "exemptTablePatterns";

    /// <summary>
    /// Validates the exempt table patterns of an <see cref="AddColumnWithDefaultOptions"/>.
    /// Every invalid pattern is reported, not just the first.
    /// </summary>
    /// <param name="value">The options to validate.</param>
    /// <returns>A list of human-readable validation problems; empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this AddColumnWithDefaultOptions value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        if (value.ExemptTablePatterns is null)
        {
            problems.Add($"Option '{ExemptTablePatternsOptionName}' of rule '{AddColumnWithDefaultRule.RuleId}' must not be null.");
            return problems;
        }

        for (var index = 0; index < value.ExemptTablePatterns.Count; index++)
        {
            var problem = ExemptPatternProblem(index, value.ExemptTablePatterns[index]);
            if (problem is not null)
            {
                problems.Add(problem);
            }
        }

        return problems;
    }

    /// <summary>
    /// Checks one exempt table pattern and describes the problem, or returns null when the pattern is usable.
    /// Compiles the pattern with the same options the rule uses, so a pattern accepted here always compiles in the rule.
    /// </summary>
    /// <param name="index">The position of the pattern in the option's array, used in the message.</param>
    /// <param name="pattern">The pattern as written in the configuration.</param>
    /// <returns>A problem message naming the pattern and the regex error, or null if the pattern is valid.</returns>
    internal static string? ExemptPatternProblem(int index, string? pattern)
    {
        var prefix = ExemptPatternPrefix(index);

        if (pattern is null)
        {
            return $"{prefix}: the pattern must be a string, not null.";
        }

        if (string.IsNullOrWhiteSpace(pattern))
        {
            return $"{prefix}: the pattern is empty or whitespace, and would exempt every table.";
        }

        try
        {
            _ = new Regex(pattern, AddColumnWithDefaultOptions.PatternOptions, AddColumnWithDefaultOptions.PatternMatchTimeout);
            return null;
        }
        catch (ArgumentException ex)
        {
            return $"{prefix}: pattern '{pattern}' is not a valid regular expression: {ex.Message}";
        }
    }

    /// <summary>
    /// Describes the location of an exempt table pattern in the configuration, for use at the start of a problem message.
    /// </summary>
    /// <param name="index">The position of the pattern in the option's array.</param>
    /// <returns>The location prefix, without a trailing punctuation mark.</returns>
    internal static string ExemptPatternPrefix(int index) =>
        $"Option '{ExemptTablePatternsOptionName}' of rule '{AddColumnWithDefaultRule.RuleId}' has an invalid pattern at index {index}";
}
