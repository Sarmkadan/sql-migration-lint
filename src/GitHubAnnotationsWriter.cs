using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SqlMigrationLint;

/// <summary>
/// Writes lint findings in GitHub Actions workflow command format.
/// </summary>
public class GitHubAnnotationsWriter : IReportWriter
{
    private const int MaxAnnotationsPerStep = 10;

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

    private static string Format(LintFinding finding)
    {
        ArgumentNullException.ThrowIfNull(finding);

        var severityLevel = MapSeverityToGitHubLevel(finding.Severity);
        var escapedMessage = EscapeForWorkflowCommand(finding.Message);
        var escapedFile = finding.File is not null ? EscapeForWorkflowCommand(finding.File) : null;

        return finding.File is null
            ? $"::{severityLevel}::{escapedMessage}"
            : $"::{severityLevel} file={escapedFile},line={finding.Line ?? 0}::{escapedMessage}";
    }

    private static void WriteAll(IReadOnlyList<LintFinding> findings, TextWriter writer)
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

    /// <inheritdoc/>
    public void WriteReport(LintReport report, TextWriter writer)
    {
        WriteAll(report.Findings, writer);
    }
}
