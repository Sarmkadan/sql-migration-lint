namespace SqlMigrationLint;

using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Extension methods for <see cref="LintFinding"/>.
/// </summary>
public static class LintFindingExtensions
{
    /// <summary>
    /// Determines whether the finding's severity is at least the specified severity.
    /// </summary>
    /// <param name="finding">The lint finding.</param>
    /// <param name="minSeverity">The minimum severity to compare against.</param>
    /// <returns>True if the finding's severity is at least <paramref name="minSeverity"/>; otherwise, false.</returns>
    public static bool IsAtLeast(this LintFinding finding, LintSeverity minSeverity)
    {
        // Define severity order: Blocker > Danger > Warning
        int GetSeverityValue(LintSeverity severity) => severity switch
        {
            LintSeverity.Blocker => 2,
            LintSeverity.Danger => 1,
            LintSeverity.Warning => 0,
            _ => 0
        };

        return GetSeverityValue(finding.Severity) >= GetSeverityValue(minSeverity);
    }

    /// <summary>
    /// Returns a display string representation of the lint finding.
    /// </summary>
    /// <param name="finding">The lint finding.</param>
    /// <returns>A formatted string containing the rule name, severity, message, and location.</returns>
    public static string ToDisplayString(this LintFinding finding)
    {
        var location = finding.File != null
            ? $"{finding.File}"
            : "<unknown file>";

        if (finding.Line.HasValue)
        {
            location += $":{finding.Line.Value}";
        }

        return $"[{finding.Severity}] {finding.RuleName}: {finding.Message} ({location})";
    }

    /// <summary>
    /// Groups a sequence of lint findings by file path.
    /// </summary>
    /// <param name="findings">The sequence of lint findings.</param>
    /// <returns>
    /// A grouping of findings by file path. Findings with a null file path are grouped under null key.
    /// </returns>
    public static IEnumerable<IGrouping<string?, LintFinding>> GroupByFile(this IEnumerable<LintFinding> findings)
    {
        return findings.GroupBy(f => f.File);
    }
}