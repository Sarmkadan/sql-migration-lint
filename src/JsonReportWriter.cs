using System.Text.Json;
using System.Linq;

namespace SqlMigrationLint;

/// <summary>
/// Serialises a <see cref="LintReport"/> to JSON. The output contains the overall
/// report metadata and a simplified list of findings (file, rule name, severity,
/// message and line when available).
/// </summary>
public static class JsonReportWriter
{
    /// <summary>
    /// Returns a JSON string representing the supplied <paramref name="report"/>.
    /// </summary>
    /// <param name="report">The lint report to serialise.</param>
    /// <param name="indented">Whether the JSON should be indented.</param>
    /// <returns>A JSON representation of the report.</returns>
    public static string WriteReport(LintReport report, bool indented = false)
    {
        var options = new JsonSerializerOptions { WriteIndented = indented };

        var serialisable = new
        {
            report.MigrationsScanned,
            report.HasBlockers,
            MaxRisk = report.MaxRisk.ToString(),
            Findings = report.Findings.Select(f => new
            {
                File = f.File,
                RuleName = f.RuleName,
                Severity = f.Severity.ToString(),
                Message = f.Message,
                Line = f.Line
            })
        };

        return JsonSerializer.Serialize(serialisable, options);
    }
}
