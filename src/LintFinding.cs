namespace SqlMigrationLint;

public sealed record LintFinding(
    string RuleName,
    LintSeverity Severity,
    string Message,
    string? File,
    int? Line
);

public enum LintSeverity
{
    Blocker,
    Danger,
    Warning
}
