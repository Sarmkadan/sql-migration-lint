using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SqlMigrationLint;

/// <summary>
/// Detects migration files that share the same version prefix.
/// Migration files typically follow naming patterns like "0001_AddUserEmail.cs",
/// "20240315_1234_AddPost.cs", etc. where the prefix before the first underscore
/// represents the version/timestamp. This rule flags files that share the same
/// version prefix, which could indicate duplicate migrations.
/// </summary>
internal sealed class DuplicateMigrationVersionRule : IGlobalLintRule
{
    public static readonly DuplicateMigrationVersionRule Instance = new();

    public string Name => "ML103";
    public string Description => "Detects migration files sharing the same version prefix.";
    public LintSeverity Severity => LintSeverity.Blocker;

    public IReadOnlyList<LintFinding> Evaluate(IReadOnlyList<MigrationFile> migrationFiles)
    {
        var findings = new List<LintFinding>();

        // Group migrations by their version prefix
        var versionGroups = new Dictionary<string, List<MigrationFile>>(StringComparer.Ordinal);

        foreach (var migrationFile in migrationFiles)
        {
            // Extract version prefix from file name
            string? versionPrefix = ExtractVersionPrefix(migrationFile.FilePath);

            if (versionPrefix is not null)
            {
                if (!versionGroups.TryGetValue(versionPrefix, out var group))
                {
                    group = new List<MigrationFile>();
                    versionGroups[versionPrefix] = group;
                }
                group.Add(migrationFile);
            }
        }

        // Report findings for any version prefix that has more than one migration
        foreach (var kvp in versionGroups)
        {
            if (kvp.Value.Count > 1)
            {
                // Sort by file path for consistent output
                var sortedFiles = kvp.Value.OrderBy(f => f.FilePath, StringComparer.Ordinal).ToList();

                // Report finding for the first file (primary location)
                var primaryFile = sortedFiles[0];
                findings.Add(new LintFinding(
                    RuleName: Name,
                    Severity: Severity,
                    Message: $"Multiple migrations share version prefix '{kvp.Key}': {kvp.Value.Count} files found.",
                    File: primaryFile.FilePath,
                    Line: 1
                ));

                // Add annotations for other files with the same version
                for (int i = 1; i < sortedFiles.Count; i++)
                {
                    findings.Add(new LintFinding(
                        RuleName: Name,
                        Severity: Severity,
                        Message: $"Duplicate version prefix '{kvp.Key}' also used in: {sortedFiles[i].FilePath}",
                        File: sortedFiles[i].FilePath,
                        Line: 1
                    ));
                }
            }
        }

        return findings;
    }

    /// <summary>
    /// Extracts the version prefix from a migration file path.
    /// Supports common migration naming patterns:
    /// - Numeric prefixes: "0001_AddUserEmail.cs" -> "0001"
    /// - Timestamp prefixes: "20240315_1234_AddPost.cs" -> "20240315_1234"
    /// - Any prefix before the first underscore
    /// </summary>
    /// <param name="filePath">The file path of the migration.</param>
    /// <returns>The version prefix, or null if not determinable.</returns>
    private static string? ExtractVersionPrefix(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return null;

        try
        {
            // Get just the file name without extension
            string fileName = System.IO.Path.GetFileNameWithoutExtension(filePath);

            // Find the first underscore
            int underscoreIndex = fileName.IndexOf('_');

            if (underscoreIndex > 0)
            {
                // Extract everything before the first underscore
                string prefix = fileName.Substring(0, underscoreIndex);

                // Validate it looks like a version/timestamp
                // Common patterns: all digits, or timestamp format (YYYYMMDD_HHMM)
                if (Regex.IsMatch(prefix, "^[0-9]+") ||
                    Regex.IsMatch(prefix, "^[0-9]{8}_[0-9]{4}"))
                {
                    return prefix;
                }
            }

            return null;
        }
        catch
        {
            // If anything goes wrong parsing, return null
            return null;
        }
    }
}