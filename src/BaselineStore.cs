using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SqlMigrationLint;

/// <summary>
/// Provides support for baseline files that contain fingerprints of previously‑seen lint findings.
/// A fingerprint is a deterministic hash of the rule name, file path and line number of a finding.
/// The baseline can be written to disk and later used to filter out known findings from a report.
/// </summary>
public static class BaselineStore
{
    /// <summary>
    /// Writes a baseline file containing the fingerprints of all findings in <paramref name="report"/>.
    /// </summary>
    /// <param name="path">The file path where the baseline JSON should be written.</param>
    /// <param name="report">The lint report whose findings are to be fingerprinted.</param>
    /// <param name="indented">Whether the JSON should be indented.</param>
    public static void WriteBaseline(string path, LintReport report, bool indented = false)
    {
        if (report == null) throw new ArgumentNullException(nameof(report));

        var fingerprints = report.Findings
            .Select(ComputeFingerprint)
            .ToArray();

        var options = new JsonSerializerOptions { WriteIndented = indented };
        var json = JsonSerializer.Serialize(fingerprints, options);
        File.WriteAllText(path, json);
    }

    /// <summary>
    /// Loads a baseline file and returns the set of stored fingerprints.
    /// If the file does not exist or cannot be parsed, an empty set is returned.
    /// </summary>
    /// <param name="path">The baseline file path.</param>
    /// <returns>A read‑only set of fingerprint strings.</returns>
    public static IReadOnlySet<string> LoadBaseline(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return new HashSet<string>();
        }

        try
        {
            var json = File.ReadAllText(path);
            var fingerprints = JsonSerializer.Deserialize<string[]>(json) ?? Array.Empty<string>();
            return new HashSet<string>(fingerprints);
        }
        catch
        {
            // If anything goes wrong (corrupt JSON, IO error, etc.) we fall back to an empty baseline.
            return new HashSet<string>();
        }
    }

    /// <summary>
    /// Filters the findings of <paramref name="report"/> by removing any that are present in the baseline
    /// located at <paramref name="baselinePath"/>.
    /// </summary>
    /// <param name="baselinePath">Path to the baseline JSON file.</param>
    /// <param name="report">The original lint report.</param>
    /// <returns>An <see cref="IEnumerable{LintFinding}"/> containing only new findings.</returns>
    public static IEnumerable<LintFinding> FilterFindings(string baselinePath, LintReport report)
    {
        if (report == null) throw new ArgumentNullException(nameof(report));

        var baseline = LoadBaseline(baselinePath);
        return report.Findings
            .Where(f => !baseline.Contains(ComputeFingerprint(f)));
    }

    /// <summary>
    /// Computes a deterministic fingerprint for a single <see cref="LintFinding"/>.
    /// The fingerprint is a Base64‑encoded SHA‑256 hash of the concatenated rule name,
    /// file path and line number (or -1 when the line is null).
    /// </summary>
    private static string ComputeFingerprint(LintFinding finding)
    {
        // Guard against nulls – the properties used for the fingerprint are expected to be non‑null,
        // but we defensively handle the case where they are.
        var rule = finding.RuleName ?? string.Empty;
        var file = finding.File ?? string.Empty;
        var line = finding.Line?.ToString() ?? "-1";

        var raw = $"{rule}|{file}|{line}";
        var bytes = Encoding.UTF8.GetBytes(raw);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }
}
