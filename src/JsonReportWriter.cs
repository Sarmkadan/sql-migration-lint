using System.IO;
using System.Linq;
using System.Text.Json;

namespace SqlMigrationLint;

/// <summary>
/// Serialises a <see cref="LintReport"/> to JSON and writes it to a <see cref="TextWriter"/>.
/// </summary>
public class JsonReportWriter : IReportWriter
{
    private readonly bool _indented;

    /// <summary>
    /// Creates a new <see cref="JsonReportWriter"/>.
    /// </summary>
    /// <param name="indented">Whether the JSON output should be indented.</param>
    public JsonReportWriter(bool indented = false)
    {
        _indented = indented;
    }

    /// <inheritdoc/>
    public void WriteReport(LintReport report, TextWriter writer)
    {
        var options = new JsonSerializerOptions { WriteIndented = _indented };

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

        var json = JsonSerializer.Serialize(serialisable, options);
        writer.WriteLine(json);
    }
}
