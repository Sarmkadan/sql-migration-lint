using System.Collections.Generic;

namespace SqlMigrationLint;

internal static class LintRuleRegistry
{
    public static IReadOnlyList<ILintRule> AllRules { get; } = CreateAllRules();

    private static IReadOnlyList<ILintRule> CreateAllRules()
    {
        var rules = new List<ILintRule>();
        rules.AddRange(DestructiveOperationRules.All);
        rules.AddRange(LockHeavyOperationRules.All);
        rules.Add(NonConcurrentIndexRule.Instance);
        rules.Add(EmptyDownRule.Instance);
        rules.Add(MissingWhereRule.Instance);
        return rules;
    }
}
