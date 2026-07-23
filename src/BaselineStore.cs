using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using System.Diagnostics.CodeAnalysis;

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
    /// The write is performed atomically: the baseline is first written to a temporary file in the same directory,
    /// then moved to the target path using <see cref="File.Move(string, string, bool)"/> with overwrite.
    /// This ensures that a crash mid-write cannot corrupt the baseline file.
    /// </summary>
    /// <param name="path">The file path where the baseline JSON should be written.</param>
    /// <param name="report">The lint report whose findings are to be fingerprinted.</param>
    /// <param name="indented">Whether the JSON should be indented.</param>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> or <paramref name="report"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is empty or consists only of whitespace.</exception>
    /// <exception cref="IOException">An IO error occurs while writing the baseline.</exception>
    public static void WriteBaseline(string path, LintReport report, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(report);

        // Sort findings deterministically to ensure stable baseline diffs in git
        // Order by File (alphabetically), then Line (numerically), then RuleName (alphabetically)
        var fingerprints = report.Findings
            .OrderBy(f => f.File ?? string.Empty)
            .ThenBy(f => f.Line ?? -1)
            .ThenBy(f => f.RuleName ?? string.Empty)
            .Select(ComputeFingerprint)
            .ToArray();

        var options = new JsonSerializerOptions { WriteIndented = indented };
        var json = JsonSerializer.Serialize(fingerprints, options);

        // Atomic write: write to temp file in same directory, then move atomically
        var tempPath = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempPath, json);
            File.Move(tempPath, path, overwrite: true);
        }
        finally
        {
            // Clean up temp file if it still exists (e.g., if move failed)
            try
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
            catch
            {
                // Best-effort cleanup; ignore errors during cleanup
            }
        }
    }

    /// <summary>
    /// Loads a baseline file and returns the set of stored fingerprints.
    /// If the file does not exist, is empty, or cannot be parsed, an empty set is returned.
    /// </summary>
    /// <param name="path">The baseline file path.</param>
    /// <returns>A read‑only set of fingerprint strings.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is empty or consists only of whitespace.</exception>
    public static IReadOnlySet<string> LoadBaseline(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (!File.Exists(path))
        {
            return new HashSet<string>();
        }

        try
        {
            var json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json))
            {
                // Empty file - treat as missing baseline
                return new HashSet<string>();
            }

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
    /// <exception cref="ArgumentNullException"><paramref name="baselinePath"/> or <paramref name="report"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="baselinePath"/> is empty or consists only of whitespace.</exception>
    public static IEnumerable<LintFinding> FilterFindings(string baselinePath, LintReport report)
    {
        ArgumentNullException.ThrowIfNull(baselinePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(baselinePath);
        ArgumentNullException.ThrowIfNull(report);

        var baseline = LoadBaseline(baselinePath);
        return report.Findings
            .Where(f => !baseline.Contains(ComputeFingerprint(f)));
    }

    /// <summary>
    /// Computes a deterministic fingerprint for a single <see cref="LintFinding"/>.
    /// The fingerprint is a Base64‑encoded SHA‑256 hash of the concatenated rule name,
    /// file path and line number (or -1 when the line is null).
    /// </summary>
    /// <param name="finding">The finding to fingerprint.</param>
    /// <returns>A deterministic fingerprint string.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="finding"/> is <see langword="null"/>.</exception>
    private static string ComputeFingerprint(LintFinding finding)
    {
        ArgumentNullException.ThrowIfNull(finding);

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
