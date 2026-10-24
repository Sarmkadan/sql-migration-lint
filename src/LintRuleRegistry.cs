using System;
using System.Collections.Generic;
using System.Linq;

namespace SqlMigrationLint;

internal static class LintRuleRegistry
{
    /// <summary>
    /// Gets the collection of operation-scoped lint rules.
    /// </summary>
    public static IReadOnlyList<ILintRule> PerOperationRules { get; } = CreatePerOperationRules(options: null);

    /// <summary>
    /// Gets the collection of file-scoped lint rules.
    /// </summary>
    public static IReadOnlyList<IPerFileLintRule> PerFileRules { get; } = CreatePerFileRules();

    /// <summary>
    /// Gets the collection of global lint rules.
    /// </summary>
    public static IReadOnlyList<IGlobalLintRule> GlobalRules { get; } = CreateGlobalRules();

    /// <summary>
    /// Gets the sorted, distinct ids of every rule the built-in linter can evaluate, including the global rule
    /// that <see cref="MigrationLinter.CreateDefaultWithGlobalRules"/> adds. Used to validate configuration keys.
    /// </summary>
    public static IReadOnlyList<string> RuleNames { get; } = CreateRuleNames();

    private static IReadOnlyList<string> CreateRuleNames()
    {
        return PerOperationRules.Select(rule => rule.Name)
            .Concat(PerFileRules.Select(rule => rule.RuleName))
            .Concat(GlobalRules.Select(rule => rule.Name))
            .Append(DuplicateMigrationVersionRule.Instance.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    /// <summary>
    /// Creates the operation-scoped rules, applying the per-rule options from configuration where a rule accepts them.
    /// </summary>
    /// <param name="options">The per-rule options, or null to use each rule's defaults.</param>
    /// <returns>The operation-scoped rules.</returns>
    public static IReadOnlyList<ILintRule> CreatePerOperationRules(LintRuleOptions? options)
    {
        var rules = new List<ILintRule>();
        rules.AddRange(LockHeavyOperationRules.Create(options?.AddColumnWithDefault));
        rules.Add(NonConcurrentIndexRule.Instance);
        rules.Add(EmptyDownRule.Instance);
        rules.Add(MissingWhereRule.Instance);
        return rules;
    }

    private static IReadOnlyList<IPerFileLintRule> CreatePerFileRules()
    {
        var rules = new List<IPerFileLintRule>
        {
            new NamingConventionRule(),
            new MissingDownMigrationRule()
        };
        rules.AddRange(DestructiveOperationRules.All.Cast<IPerFileLintRule>());
        return rules;
    }

    private static IReadOnlyList<IGlobalLintRule> CreateGlobalRules()
    {
        return Array.Empty<IGlobalLintRule>();
    }
}
