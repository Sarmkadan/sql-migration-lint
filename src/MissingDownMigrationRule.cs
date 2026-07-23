using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SqlMigrationLint;

/// <summary>
/// Rule that checks if every Up operation has a corresponding Drop operation in the Down body.
/// </summary>
public sealed class MissingDownMigrationRule : ILintRule, IPerFileLintRule
{
    public string Name => "ML101";
    public string Description => "Checks that every Up operation has a corresponding Drop operation in the Down body.";
    public LintSeverity Severity => LintSeverity.Warning;

    public bool AppliesTo(MigrationOperation operation) => operation is SqlOperation;

    public LintFinding? Evaluate(MigrationOperation operation)
    {
        if (operation is not SqlOperation sqlOperation || sqlOperation.File is null)
        {
            return null;
        }

        MigrationFile? migrationFile = MigrationFile.TryParse(sqlOperation.File);
        if (migrationFile is null)
        {
            return null;
        }

        return EvaluateFile(migrationFile, sqlOperation.File, sqlOperation.Line);
    }

    /// <summary>
    /// Gets the unique rule name used for configuration lookups, identical to <see cref="Name"/>.
    /// </summary>
    string IPerFileLintRule.RuleName => Name;

    /// <summary>
    /// Checks whether every Up operation of the given migration file has a corresponding Drop
    /// operation in its Down body, using the already-parsed file instead of re-reading it from disk.
    /// </summary>
    /// <param name="file">The parsed migration file to check.</param>
    /// <param name="config">The active lint configuration, or null if none was loaded.</param>
    /// <returns>A collection of lint findings; empty if the file has no issues.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="file"/> is null.</exception>
    public IEnumerable<LintFinding> Check(MigrationFile file, LintConfig? config)
    {
        ArgumentNullException.ThrowIfNull(file);

        var finding = EvaluateFile(file, file.FilePath, line: 1);
        if (finding is not null)
        {
            yield return finding;
        }
    }

    /// <summary>
    /// Compares Up and Down operations of a parsed migration file and produces a finding when
    /// Up operations lack a corresponding Down counterpart.
    /// </summary>
    /// <param name="migrationFile">The parsed migration file.</param>
    /// <param name="file">The file path to attach to the finding.</param>
    /// <param name="line">The line number to attach to the finding.</param>
    /// <returns>A finding describing the missing Down operations, or null if none are missing.</returns>
    private LintFinding? EvaluateFile(MigrationFile migrationFile, string file, int? line)
    {
        if (string.IsNullOrWhiteSpace(migrationFile.UpBody))
        {
            return null;
        }

        var upOperations = ParseOperations(migrationFile.UpBody);
        var downOperations = ParseDownOperations(migrationFile.DownBody ?? string.Empty);

        var missingOperations = new List<string>();

        foreach (var op in upOperations)
        {
            if (!downOperations.Contains(op))
            {
                missingOperations.Add(op.ToString());
            }
        }

        if (missingOperations.Any())
        {
            return new LintFinding(
                RuleName: Name,
                Severity: Severity,
                Message: $"Migration '{migrationFile.MigrationName}' is missing Down operations for: {string.Join(", ", missingOperations)}",
                File: file,
                Line: line);
        }

        return null;
    }

    private record MigrationOperationIdentifier(string Type, string Name)
    {
        public override string ToString() => $"{Type} '{Name}'";
    }

    private static List<MigrationOperationIdentifier> ParseOperations(string body)
    {
        var operations = new List<MigrationOperationIdentifier>();

        // CreateTable
        foreach (Match match in Regex.Matches(body, @"CreateTable\(name:\s*""(?<name>[^""]+)"""))
        {
            operations.Add(new MigrationOperationIdentifier("Table", match.Groups["name"].Value));
        }

        // CreateIndex
        foreach (Match match in Regex.Matches(body, @"CreateIndex\(name:\s*""(?<name>[^""]+)"""))
        {
            operations.Add(new MigrationOperationIdentifier("Index", match.Groups["name"].Value));
        }

        // AddColumn
        foreach (Match match in Regex.Matches(body, @"AddColumn<\w+>\(name:\s*""(?<name>[^""]+)"",\s*table:\s*""(?<table>[^""]+)"""))
        {
            operations.Add(new MigrationOperationIdentifier("Column", $"{match.Groups["table"].Value}.{match.Groups["name"].Value}"));
        }

        return operations;
    }

    private static HashSet<MigrationOperationIdentifier> ParseDownOperations(string body)
    {
        var operations = new HashSet<MigrationOperationIdentifier>();

        // DropTable
        foreach (Match match in Regex.Matches(body, @"DropTable\(name:\s*""(?<name>[^""]+)"""))
        {
            operations.Add(new MigrationOperationIdentifier("Table", match.Groups["name"].Value));
        }

        // DropIndex
        foreach (Match match in Regex.Matches(body, @"DropIndex\(name:\s*""(?<name>[^""]+)"""))
        {
            operations.Add(new MigrationOperationIdentifier("Index", match.Groups["name"].Value));
        }

        // DropColumn
        foreach (Match match in Regex.Matches(body, @"DropColumn\(name:\s*""(?<name>[^""]+)"",\s*table:\s*""(?<table>[^""]+)"""))
        {
            operations.Add(new MigrationOperationIdentifier("Column", $"{match.Groups["table"].Value}.{match.Groups["name"].Value}"));
        }

        return operations;
    }
}
