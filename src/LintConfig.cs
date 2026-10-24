using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SqlMigrationLint;

/// <summary>
/// Represents the severity level configuration for a lint rule.
/// </summary>
public enum RuleSeverityConfig
{
    /// <summary>
    /// Rule is disabled and will not run.
    /// </summary>
    Off,

    /// <summary>
    /// Rule runs but only generates warnings.
    /// </summary>
    Warning,

    /// <summary>
    /// Rule runs with error severity.
    /// </summary>
    Error
}

/// <summary>
/// Configuration for lint rules loaded from .sqlmigrationlint.json
/// </summary>
public sealed class LintConfig
{
    /// <summary>
    /// Mapping of rule IDs to their configured severity levels.
    /// </summary>
    public Dictionary<string, RuleSeverityConfig> Rules { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or sets the per-rule options, read from the <c>options</c> section keyed by rule id.
    /// </summary>
    public LintRuleOptions Options { get; set; } = new();

    /// <summary>
    /// Loads configuration from a .sqlmigrationlint.json file and validates it once, at load time.
    /// If the file doesn't exist, returns null (use default configuration).
    /// </summary>
    /// <param name="configPath">Path to the configuration file.</param>
    /// <returns>Configured LintConfig or null if file doesn't exist.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the file is not valid JSON, or when it names an unknown rule, uses a severity other than
    /// off, warning or error, or sets an invalid option. The message lists every problem and the valid values.
    /// </exception>
    public static LintConfig? Load(string configPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configPath);

        if (!File.Exists(configPath))
        {
            return null;
        }

        LintConfig? config = null;
        List<string> problems;
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(configPath), new JsonDocumentOptions
            {
                CommentHandling = JsonCommentHandling.Skip
            });

            // Rules and options are checked before binding: binding would fail on the first bad value without naming
            // the allowed set, and would hide the other problems. Both checks run, so their problems are reported together.
            problems = LintConfigValidation.ValidateRules(document.RootElement)
                .Concat(LintConfigValidation.ValidateOptions(document.RootElement))
                .ToList();
            if (problems.Count == 0)
            {
                config = document.RootElement.Deserialize<LintConfig>(new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    Converters = { new JsonStringEnumConverter() }
                });

                if (config is not null)
                {
                    config.Rules = new Dictionary<string, RuleSeverityConfig>(config.Rules, StringComparer.OrdinalIgnoreCase);
                    problems.AddRange(config.Validate());
                }
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to load configuration from {configPath}. Please ensure it's valid JSON.", ex);
        }

        if (problems.Count > 0)
        {
            throw new InvalidOperationException(
                $"Invalid configuration in {configPath}:{Environment.NewLine} - {string.Join($"{Environment.NewLine} - ", problems)}");
        }

        return config;
    }

    /// <summary>
    /// Gets the effective severity for a given rule based on configuration.
    /// Returns the configured severity if present, otherwise returns the rule's default severity.
    /// </summary>
    /// <param name="ruleName">The name of the rule.</param>
    /// <param name="defaultSeverity">The rule's default severity.</param>
    /// <returns>The effective severity level.</returns>
    public LintSeverity GetEffectiveSeverity(string ruleName, LintSeverity defaultSeverity)
    {
        if (Rules.TryGetValue(ruleName, out var configuredSeverity))
        {
            return configuredSeverity switch
            {
                RuleSeverityConfig.Off => LintSeverity.Blocker, // Treat Off as disabled - won't be evaluated
                RuleSeverityConfig.Warning => LintSeverity.Warning,
                RuleSeverityConfig.Error => LintSeverity.Blocker,
                _ => defaultSeverity
            };
        }

        return defaultSeverity;
    }

    /// <summary>
    /// Gets or sets the maximum degree of parallelism for parallel linting operations.
    /// Defaults to Environment.ProcessorCount if not specified.
    /// </summary>
    public int? MaxDegreeOfParallelism { get; set; }

    /// <summary>
    /// Checks if a rule should be evaluated based on configuration.
    /// </summary>
    /// <param name="ruleName">The name of the rule.</param>
    /// <returns>True if the rule should be evaluated, false if it's disabled.</returns>
    public bool ShouldEvaluateRule(string ruleName)
    {
        return !Rules.TryGetValue(ruleName, out var configuredSeverity) || configuredSeverity != RuleSeverityConfig.Off;
    }
}
