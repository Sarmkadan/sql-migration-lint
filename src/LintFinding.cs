namespace SqlMigrationLint;

public sealed record LintFinding
{
    public string RuleName { get; init; }
    public LintSeverity Severity { get; init; }
    public string Message { get; init; }
    public string? File { get; init; }
    public int? Line { get; init; }

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

public enum LintSeverity
{
    Blocker,
    Danger,
    Warning
}