namespace SqlMigrationLint;

/// <summary>
/// Writes lint findings in GitHub Actions workflow command format.
/// </summary>
public static class GitHubAnnotationsWriter
{
    private const int MaxAnnotationsPerStep = 10;

    /// <summary>
    /// Escapes a string for use in GitHub Actions workflow command properties or messages.
    /// Replaces special characters with their percent-encoded equivalents:
    /// - '%' -> %25
    /// - '\r' -> %0D
    /// - '\n' -> %0A
    /// - ',' -> %2C
    /// - ':' -> %3A
    /// </summary>
    /// <param name="value">The string to escape.</param>
    /// <returns>The escaped string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    private static string EscapeForWorkflowCommand(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return value
            .Replace("%", "%25")
            .Replace("\r", "%0D")
            .Replace("\n", "%0A")
            .Replace(",", "%2C")
            .Replace(":", "%3A");
    }

    /// <summary>
    /// Maps a <see cref="LintSeverity"/> to the corresponding GitHub Actions workflow command severity level.
    /// </summary>
    /// <param name="severity">The severity level to map.</param>
    /// <returns>The GitHub Actions severity level.</returns>
    private static string MapSeverityToGitHubLevel(LintSeverity severity)
    {
        return severity switch
        {
            LintSeverity.Blocker => "error",
            LintSeverity.Danger => "error",
            LintSeverity.Warning => "warning",
            _ => "notice"
        };
    }

    /// <summary>
    /// Formats a single lint finding as a GitHub Actions workflow command.
    /// </summary>
    /// <param name="finding">The finding to format.</param>
    /// <returns>A GitHub Actions workflow command string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="finding"/> is null.</exception>
    public static string Format(LintFinding finding)
    {
        ArgumentNullException.ThrowIfNull(finding);

        var severityLevel = MapSeverityToGitHubLevel(finding.Severity);
        var escapedMessage = EscapeForWorkflowCommand(finding.Message);
        var escapedFile = finding.File is not null ? EscapeForWorkflowCommand(finding.File) : null;

        // "::error ::msg" (space, no parameters) is not a valid workflow command;
        // omit the space entirely when there is no file to attach.
        return finding.File is null
            ? $"::{severityLevel}::{escapedMessage}"
            : $"::{severityLevel} file={escapedFile},line={finding.Line ?? 0}::{escapedMessage}";
    }

    /// <summary>
    /// Writes all findings to the specified text writer, respecting GitHub's annotation limits.
    /// GitHub Actions limits annotations to 10 per step per type. When more findings are present,
    /// only the top <see cref="MaxAnnotationsPerStep"/> findings by severity are emitted,
    /// followed by a summary annotation indicating the total count.
    /// </summary>
    /// <param name="findings">The findings to write.</param>
    /// <param name="writer">The text writer to write to.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="findings"/> or <paramref name="writer"/> is null.</exception>
    public static void WriteAll(IReadOnlyList<LintFinding> findings, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(findings);
        ArgumentNullException.ThrowIfNull(writer);

        if (findings.Count == 0)
        {
            return;
        }

        // Sort findings by severity (Blocker/Danger first, then Warning, then others)
        // Within same severity, maintain original order (stable sort)
        var indexedFindings = findings
            .Select((finding, index) => (Finding: finding, OriginalIndex: index))
            .OrderByDescending(t => t.Finding.Severity)
            .ThenBy(t => t.OriginalIndex)
            .ToList();

        var sortedFindings = indexedFindings.Select(t => t.Finding).ToList();

        var findingsToWrite = sortedFindings.Count <= MaxAnnotationsPerStep
            ? sortedFindings
            : sortedFindings.Take(MaxAnnotationsPerStep).ToList();

        foreach (var finding in findingsToWrite)
        {
            writer.WriteLine(Format(finding));
        }

        // Add summary annotation if there were more findings than we could emit
        if (sortedFindings.Count > MaxAnnotationsPerStep)
        {
            var truncatedCount = sortedFindings.Count - MaxAnnotationsPerStep;
            var summaryMessage = $"Truncated {truncatedCount} additional finding{(truncatedCount == 1 ? "" : "s")}. See full report.";
            var severityLevel = MapSeverityToGitHubLevel(sortedFindings[MaxAnnotationsPerStep].Severity);
            writer.WriteLine($"::{severityLevel}::{EscapeForWorkflowCommand(summaryMessage)}");
        }
    }

    /// <summary>
    /// Writes all findings to the console output.
    /// </summary>
    /// <param name="findings">The findings to write.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="findings"/> is null.</exception>
    public static void WriteToConsole(IReadOnlyList<LintFinding> findings)
    {
        WriteAll(findings, Console.Out);
    }
}
