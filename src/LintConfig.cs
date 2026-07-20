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
    /// Loads configuration from a .sqlmigrationlint.json file.
    /// If the file doesn't exist, returns null (use default configuration).
    /// </summary>
    /// <param name="configPath">Path to the configuration file.</param>
    /// <returns>Configured LintConfig or null if file doesn't exist.</returns>
    public static LintConfig? Load(string configPath)
    {
        if (!File.Exists(configPath))
        {
            return null;
        }

        try
        {
            var json = File.ReadAllText(configPath);
            var config = JsonSerializer.Deserialize<LintConfig>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                Converters = { new JsonStringEnumConverter() }
            });

            return config;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to load configuration from {configPath}. Please ensure it's valid JSON.", ex);
        }
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
    /// Checks if a rule should be evaluated based on configuration.
    /// </summary>
    /// <param name="ruleName">The name of the rule.</param>
    /// <returns>True if the rule should be evaluated, false if it's disabled.</returns>
    public bool ShouldEvaluateRule(string ruleName)
    {
        return !Rules.TryGetValue(ruleName, out var configuredSeverity) || configuredSeverity != RuleSeverityConfig.Off;
    }
}
