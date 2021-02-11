using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SqlMigrationLint;

/// <summary>
/// Rule that checks if every Up operation has a corresponding Drop operation in the Down body.
/// </summary>
public sealed class MissingDownMigrationRule : ILintRule
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
                File: sqlOperation.File,
                Line: sqlOperation.Line);
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
