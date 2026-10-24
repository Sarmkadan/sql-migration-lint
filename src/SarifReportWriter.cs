using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SqlMigrationLint;

/// <summary>
/// Serialises a <see cref="LintReport"/> as a SARIF 2.1.0 log and writes it to a <see cref="TextWriter"/>.
/// </summary>
public class SarifReportWriter : IReportWriter
{
    private const string SarifVersion = "2.1.0";
    private const string SarifSchemaUri = "https://json-schema.org/sarif-2.1.0.json";
    private const string ToolName = "sql-migration-lint";
    private const string ToolInformationUri = "https://github.com/Sarmkadan/sql-migration-lint";

    /// <inheritdoc/>
    public void WriteReport(LintReport report, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(writer);

        var ruleIds = report.Findings
            .Select(f => f.RuleName)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        var ruleIndexes = ruleIds
            .Select((id, index) => (id, index))
            .ToDictionary(t => t.id, t => t.index, StringComparer.Ordinal);

        // "$schema" is not a valid C# identifier, so the log is built as a dictionary.
        var log = new Dictionary<string, object?>
        {
            ["$schema"] = SarifSchemaUri,
            ["version"] = SarifVersion,
            ["runs"] = new[]
            {
                new
                {
                    tool = new
                    {
                        driver = new
                        {
                            name = ToolName,
                            version = GetToolVersion(),
                            informationUri = ToolInformationUri,
                            rules = ruleIds.Select(id => new { id }).ToArray()
                        }
                    },
                    results = report.Findings
                        .Select(f => ToResult(f, ruleIndexes[f.RuleName]))
                        .ToArray()
                }
            }
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        writer.WriteLine(JsonSerializer.Serialize(log, options));
    }

    private static object ToResult(LintFinding finding, int ruleIndex)
    {
        return new
        {
            ruleId = finding.RuleName,
            ruleIndex,
            level = MapSeverityToLevel(finding.Severity),
            message = new { text = finding.Message },
            locations = finding.File is null ? null : new[]
            {
                new
                {
                    physicalLocation = new
                    {
                        artifactLocation = new { uri = ToArtifactUri(finding.File) },
                        region = finding.Line is >= 1 ? new { startLine = finding.Line.Value } : null
                    }
                }
            }
        };
    }

    private static string MapSeverityToLevel(LintSeverity severity)
    {
        return severity switch
        {
            LintSeverity.Blocker => "error",
            LintSeverity.Danger => "error",
            LintSeverity.Warning => "warning",
            _ => "note"
        };
    }

    private static string ToArtifactUri(string file)
    {
        // Rooted paths become file:// URIs; relative paths are emitted as relative references with forward slashes.
        return Path.IsPathRooted(file)
            ? new Uri(file).AbsoluteUri
            : file.Replace('\\', '/');
    }

    private static string GetToolVersion()
    {
        return typeof(SarifReportWriter).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";
    }
}
