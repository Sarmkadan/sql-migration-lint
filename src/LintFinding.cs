namespace SqlMigrationLint;

/// <summary>
/// A single problem reported by a lint rule for a migration.
/// </summary>
public sealed record LintFinding
{
    /// <summary>
    /// The id of the rule that produced this finding.
    /// </summary>
    public string RuleName { get; init; }

    /// <summary>
    /// The severity of this finding.
    /// </summary>
    public LintSeverity Severity { get; init; }

    /// <summary>
    /// A human-readable description of the problem.
    /// </summary>
    public string Message { get; init; }

    /// <summary>
    /// The migration file the finding relates to.
    /// </summary>
    public string? File { get; init; }

    /// <summary>
    /// The 1-based line number in <see cref="File"/>, if known.
    /// </summary>
    public int? Line { get; init; }

    /// <summary>
    /// Creates a finding. <paramref name="RuleName"/>, <paramref name="Message"/> and <paramref name="File"/> must be non-empty.
    /// </summary>
    /// <param name="RuleName">The id of the rule that produced the finding.</param>
    /// <param name="Severity">The severity of the finding.</param>
    /// <param name="Message">A human-readable description of the problem.</param>
    /// <param name="File">The migration file the finding relates to.</param>
    /// <param name="Line">The 1-based line number, if known.</param>
    public LintFinding(string RuleName, LintSeverity Severity, string Message, string? File, int? Line)
    {
        ArgumentException.ThrowIfNullOrEmpty(RuleName);
        ArgumentException.ThrowIfNullOrEmpty(Message);
        ArgumentException.ThrowIfNullOrEmpty(File);

        this.RuleName = RuleName;
        this.Severity = Severity;
        this.Message = Message;
        this.File = File;
        this.Line = Line;
    }
}

/// <summary>
/// The severity of a <see cref="LintFinding"/>, from most to least serious.
/// </summary>
public enum LintSeverity
{
    /// <summary>
    /// Must be resolved before the migration is deployed.
    /// </summary>
    Blocker,

    /// <summary>
    /// Likely to cause data loss or outages; should be reviewed carefully.
    /// </summary>
    Danger,

    /// <summary>
    /// Risky or inefficient, but usually safe to deploy.
    /// </summary>
    Warning
}
