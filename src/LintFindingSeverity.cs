namespace SqlMigrationLint;

/// <summary>
/// Severity levels for lint findings.
/// </summary>
public enum LintFindingSeverity
{
    /// <summary>
    /// Informational severity - the issue is minor or informational.
    /// </summary>
    Info,

    /// <summary>
    /// Warning severity - the issue should be reviewed but may not be critical.
    /// </summary>
    Warning,

    /// <summary>
    /// Error severity - the issue is critical and should be fixed.
    /// </summary>
    Error
}