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

        problems.AddRange(value.Options.AddColumnWithDefault.Validate());

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

    /// <summary>
    /// Validates the <c>options</c> object of a configuration document, before it is bound to <see cref="LintConfig"/>.
    /// Checks the shape of each rule's options, rejects unknown option names, and compiles every exempt table pattern,
    /// so a bad pattern is reported with its text and the regex error instead of a generic JSON failure.
    /// </summary>
    /// <param name="root">The root element of the configuration document.</param>
    /// <returns>A list of human-readable validation problems; empty if valid.</returns>
    /// <remarks>
    /// Structural problems (a non-object root or <c>options</c>) are reported here only when they are the shape this check
    /// handles; anything else is left to deserialization.
    /// </remarks>
    public static IReadOnlyList<string> ValidateOptions(JsonElement root)
    {
        var problems = new List<string>();

        if (root.ValueKind != JsonValueKind.Object)
        {
            return problems;
        }

        foreach (var property in root.EnumerateObject())
        {
            if (!string.Equals(property.Name, "options", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (property.Value.ValueKind != JsonValueKind.Object)
            {
                problems.Add($"'options' must be an object keyed by rule id, but was {property.Value.GetRawText()}.");
                continue;
            }

            foreach (var rule in property.Value.EnumerateObject())
            {
                if (!string.Equals(rule.Name, AddColumnWithDefaultRule.RuleId, StringComparison.OrdinalIgnoreCase))
                {
                    problems.Add(IsValidRuleId(rule.Name)
                        ? $"Rule '{rule.Name}' does not accept options. Rules that accept options: {AddColumnWithDefaultRule.RuleId}."
                        : UnknownRuleIdProblem(rule.Name));
                    continue;
                }

                problems.AddRange(ValidateAddColumnWithDefaultOptions(rule.Value));
            }
        }

        return problems;
    }

    private static IEnumerable<string> ValidateAddColumnWithDefaultOptions(JsonElement options)
    {
        var problems = new List<string>();
        var ruleId = AddColumnWithDefaultRule.RuleId;

        if (options.ValueKind != JsonValueKind.Object)
        {
            problems.Add($"Options for rule '{ruleId}' must be an object, but was {options.GetRawText()}.");
            return problems;
        }

        foreach (var option in options.EnumerateObject())
        {
            if (!string.Equals(option.Name, AddColumnWithDefaultOptionsValidation.ExemptTablePatternsOptionName, StringComparison.OrdinalIgnoreCase))
            {
                problems.Add($"Unknown option '{option.Name}' for rule '{ruleId}'. Allowed options: {AddColumnWithDefaultOptionsValidation.ExemptTablePatternsOptionName}.");
                continue;
            }

            if (option.Value.ValueKind != JsonValueKind.Array)
            {
                problems.Add($"Option '{option.Name}' of rule '{ruleId}' must be an array of strings, but was {option.Value.GetRawText()}.");
                continue;
            }

            var index = 0;
            foreach (var item in option.Value.EnumerateArray())
            {
                var problem = item.ValueKind == JsonValueKind.String
                    ? AddColumnWithDefaultOptionsValidation.ExemptPatternProblem(index, item.GetString())
                    : $"{AddColumnWithDefaultOptionsValidation.ExemptPatternPrefix(index)}: the pattern must be a string, but was {item.GetRawText()}.";

                if (problem is not null)
                {
                    problems.Add(problem);
                }

                index++;
            }
        }

        return problems;
    }

    private static bool IsValidRuleId(string ruleId) =>
        ValidRuleIds.Contains(ruleId, StringComparer.OrdinalIgnoreCase);

    private static string UnknownRuleIdProblem(string ruleId) =>
        $"Unknown rule id '{ruleId}'. Valid rule ids: {string.Join(", ", ValidRuleIds)}.";
}
