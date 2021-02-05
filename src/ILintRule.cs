namespace SqlMigrationLint;

public interface ILintRule
{
    string Name { get; }
    string Description { get; }
    LintSeverity Severity { get; }

    bool AppliesTo(MigrationOperation operation);
    LintFinding? Evaluate(MigrationOperation operation);
}
