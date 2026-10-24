using System.Text.Json.Serialization;

namespace SqlMigrationLint;

/// <summary>
/// Per-rule options from the <c>options</c> section of .sqlmigrationlint.json. Each property is keyed by a rule id.
/// </summary>
public sealed class LintRuleOptions
{
    /// <summary>
    /// Gets or sets the options for <see cref="AddColumnWithDefaultRule"/> (<c>add-column-with-default</c>).
    /// </summary>
    [JsonPropertyName(AddColumnWithDefaultRule.RuleId)]
    public AddColumnWithDefaultOptions AddColumnWithDefault { get; set; } = new();
}
