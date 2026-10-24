using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace SqlMigrationLint;

/// <summary>
/// Provides validation helpers for <see cref="LintConfig"/> instances loaded from .sqlmigrationlint.json.
/// </summary>
public static class LintConfigValidation
{
    /// <summary>
    /// Gets the severity values accepted for a rule in a configuration file.
    /// </summary>
    public static IReadOnlyList<string> AllowedSeverityValues { get; } = ["off", "warning", "error"];

    /// <summary>
    /// Gets the rule ids that a configuration file may reference, sorted alphabetically.
    /// </summary>
    public static IReadOnlyList<string> ValidRuleIds => LintRuleRegistry.RuleNames;

    /// <summary>
    /// Validates the rule ids and option values of a <see cref="LintConfig"/>.
    /// </summary>
    /// <param name="value">The configuration to validate.</param>
    /// <returns>A list of human-readable validation problems; empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this LintConfig value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        foreach (var ruleId in value.Rules.Keys)
        {
            if (!IsValidRuleId(ruleId))
            {
                problems.Add(UnknownRuleIdProblem(ruleId));
            }
        }

        if (value.MaxDegreeOfParallelism is < 1)
        {
            problems.Add($"MaxDegreeOfParallelism must be at least 1, but was {value.MaxDegreeOfParallelism}.");
        }

        return problems;
    }

    /// <summary>
    /// Validates the <c>rules</c> object of a configuration document, before it is bound to <see cref="LintConfig"/>.
    /// Checks each rule id against <see cref="ValidRuleIds"/> and each value against <see cref="AllowedSeverityValues"/>.
    /// Binding would otherwise stop at the first bad severity with a generic JSON error that names neither.
    /// </summary>
    /// <param name="root">The root element of the configuration document.</param>
    /// <returns>A list of human-readable validation problems; empty if valid.</returns>
    /// <remarks>
    /// Structural problems (a non-object root or <c>rules</c>) are left to deserialization to report.
    /// </remarks>
    public static IReadOnlyList<string> ValidateRules(JsonElement root)
    {
        var problems = new List<string>();

        if (root.ValueKind != JsonValueKind.Object)
        {
            return problems;
        }

        foreach (var property in root.EnumerateObject())
        {
            if (!string.Equals(property.Name, "rules", StringComparison.OrdinalIgnoreCase) ||
                property.Value.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            foreach (var rule in property.Value.EnumerateObject())
            {
                if (!IsValidRuleId(rule.Name))
                {
                    problems.Add(UnknownRuleIdProblem(rule.Name));
                }

                if (rule.Value.ValueKind != JsonValueKind.String ||
                    !AllowedSeverityValues.Contains(rule.Value.GetString(), StringComparer.OrdinalIgnoreCase))
                {
                    var shown = rule.Value.ValueKind == JsonValueKind.String
                        ? $"'{rule.Value.GetString()}'"
                        : rule.Value.GetRawText();
                    problems.Add($"Rule '{rule.Name}' has invalid severity {shown}. Allowed values: {string.Join(", ", AllowedSeverityValues)}.");
                }
            }
        }

        return problems;
    }

    private static bool IsValidRuleId(string ruleId) =>
        ValidRuleIds.Contains(ruleId, StringComparer.OrdinalIgnoreCase);

    private static string UnknownRuleIdProblem(string ruleId) =>
        $"Unknown rule id '{ruleId}'. Valid rule ids: {string.Join(", ", ValidRuleIds)}.";
}
