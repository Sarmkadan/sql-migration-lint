using System.Collections.Generic;

namespace SqlMigrationLint;

internal static class LintRuleRegistry
{
    /// <summary>
    /// Gets the collection of operation-scoped lint rules.
    /// </summary>
    public static IReadOnlyList<ILintRule> PerOperationRules { get; } = CreatePerOperationRules();

    /// <summary>
    /// Gets the collection of file-scoped lint rules.
    /// </summary>
    public static IReadOnlyList<IPerFileLintRule> PerFileRules { get; } = CreatePerFileRules();

    /// <summary>
    /// Gets the collection of global lint rules.
    /// </summary>
    public static IReadOnlyList<IGlobalLintRule> GlobalRules { get; } = CreateGlobalRules();

    private static IReadOnlyList<ILintRule> CreatePerOperationRules()
    {
        var rules = new List<ILintRule>();
        rules.AddRange(LockHeavyOperationRules.All);
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
