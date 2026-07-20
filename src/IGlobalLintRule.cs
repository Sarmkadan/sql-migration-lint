namespace SqlMigrationLint;

/// <summary>
/// Represents a lint rule that operates on the entire set of migration files,
/// rather than on individual files.
/// </summary>
public interface IGlobalLintRule
{
    /// <summary>
    /// Gets the name of the rule.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the description of the rule.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Gets the severity level of the rule.
    /// </summary>
    LintSeverity Severity { get; }

    /// <summary>
    /// Evaluates the set of migration files and returns findings.
    /// </summary>
    /// <param name="migrationFiles">The collection of migration files to evaluate.</param>
    /// <returns>A collection of lint findings.</returns>
    IReadOnlyList<LintFinding> Evaluate(IReadOnlyList<MigrationFile> migrationFiles);
}