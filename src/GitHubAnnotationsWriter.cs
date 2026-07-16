namespace SqlMigrationLint;

/// <summary>
/// Writes lint findings in GitHub Actions workflow command format.
/// </summary>
public static class GitHubAnnotationsWriter
{
    /// <summary>
    /// Formats a single lint finding as a GitHub Actions workflow command.
    /// </summary>
    /// <param name="finding">The finding to format.</param>
    /// <returns>A GitHub Actions workflow command string.</returns>
    public static string Format(LintFinding finding)
    {
        var severityPrefix = finding.Severity switch
        {
            LintSeverity.Blocker => "error",
            LintSeverity.Danger => "error",
            LintSeverity.Warning => "warning",
            _ => "notice"
        };

        var message = finding.Message.Replace("\n", " ").Replace("\r", " ");

        // "::error ::msg" (space, no parameters) is not a valid workflow command;
        // omit the space entirely when there is no file to attach.
        return finding.File is null
            ? $"::{severityPrefix}::{message}"
            : $"::{severityPrefix} file={finding.File},line={finding.Line ?? 0}::{message}";
    }

    /// <summary>
    /// Writes all findings to the specified text writer.
    /// </summary>
    /// <param name="findings">The findings to write.</param>
    /// <param name="writer">The text writer to write to.</param>
    public static void WriteAll(IReadOnlyList<LintFinding> findings, TextWriter writer)
    {
        if (findings is null)
        {
            throw new ArgumentNullException(nameof(findings));
        }

        if (writer is null)
        {
            throw new ArgumentNullException(nameof(writer));
        }

        foreach (var finding in findings)
        {
            writer.WriteLine(Format(finding));
        }
    }

    /// <summary>
    /// Writes all findings to the console output.
    /// </summary>
    /// <param name="findings">The findings to write.</param>
    public static void WriteToConsole(IReadOnlyList<LintFinding> findings)
    {
        WriteAll(findings, Console.Out);
    }
}