namespace SqlMigrationLint;

/// <summary>
/// A rule that inspects individual migration operations and reports findings.
/// </summary>
public interface ILintRule
{
    /// <summary>
    /// The rule id, used as the configuration key and in <c>--ignore</c>.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// A short human-readable explanation of what the rule detects.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// The default severity of findings produced by this rule.
    /// </summary>
    LintSeverity Severity { get; }

    /// <summary>
    /// Determines whether this rule should be evaluated for the given operation.
    /// </summary>
    /// <param name="operation">The operation to test.</param>
    /// <returns>True if <see cref="Evaluate"/> should be called for the operation.</returns>
    bool AppliesTo(MigrationOperation operation);

    /// <summary>
    /// Evaluates the operation.
    /// </summary>
    /// <param name="operation">The operation to evaluate.</param>
    /// <returns>A finding if the rule is violated; otherwise null.</returns>
    LintFinding? Evaluate(MigrationOperation operation);
}
