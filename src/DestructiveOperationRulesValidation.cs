using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SqlMigrationLint;

/// <summary>
/// Provides validation helpers for <see cref="DestructiveOperationRules"/> static class and its members.
/// </summary>
public static class DestructiveOperationRulesValidation
{
    /// <summary>
    /// Validates the <see cref="DestructiveOperationRules"/> static class members.
    /// </summary>
    /// <returns>A list of validation problems; empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <see cref="DestructiveOperationRules"/> is null.</exception>
    public static IReadOnlyList<string> Validate()
    {
        ArgumentNullException.ThrowIfNull(DestructiveOperationRules.All);

        var problems = new List<string>();

        // Validate All property
        if (DestructiveOperationRules.All.Count == 0)
        {
            problems.Add("The All property must contain at least one lint rule.");
        }

        // Validate each rule in All
        foreach (var rule in DestructiveOperationRules.All)
        {
            ArgumentNullException.ThrowIfNull(rule);

            if (string.IsNullOrWhiteSpace(rule.Name))
            {
                problems.Add($"Rule '{rule.GetType().Name}' has an invalid or empty Name.");
            }

            if (string.IsNullOrWhiteSpace(rule.Description))
            {
                problems.Add($"Rule '{rule.Name ?? rule.GetType().Name}' has an invalid or empty Description.");
            }

            if (rule.Severity is not (LintSeverity.Blocker or LintSeverity.Danger or LintSeverity.Warning))
            {
                problems.Add($"Rule '{rule.Name}' has an invalid Severity value: {rule.Severity}.");
            }
        }

        return problems;
    }

    /// <summary>
    /// Validates the <see cref="DestructiveOperationRules"/> static class.
    /// </summary>
    /// <returns>True if valid; otherwise, false.</returns>
    public static bool IsValid() => Validate().Count == 0;

    /// <summary>
    /// Ensures that the <see cref="DestructiveOperationRules"/> static class is valid.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown if validation fails, containing the list of problems.</exception>
    /// <exception cref="ArgumentNullException">Thrown if <see cref="DestructiveOperationRules"/> is null.</exception>
    public static void EnsureValid()
    {
        var problems = Validate();

        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"DestructiveOperationRules validation failed:{Environment.NewLine} - {string.Join($"{Environment.NewLine} - ", problems)}",
                nameof(DestructiveOperationRules));
        }
    }

    /// <summary>
    /// Post-processes destructive operation findings to detect drop-then-add patterns that may indicate
    /// column/table renames rather than actual data loss.
    /// </summary>
    /// <param name="findings">The raw findings from destructive operation rules.</param>
    /// <param name="migrationFiles">The migration files corresponding to the findings.</param>
    /// <returns>A new list of findings with downgraded severities for drop-then-add patterns.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="findings"/> is null.</exception>
    public static IReadOnlyList<LintFinding> DetectDestructiveRecreatePatterns(IReadOnlyList<LintFinding> findings, IReadOnlyList<MigrationFile> migrationFiles)
    {
        ArgumentNullException.ThrowIfNull(findings);

        // Group findings by file
        var findingsByFile = new Dictionary<string, List<LintFinding>>(StringComparer.OrdinalIgnoreCase);

        foreach (var finding in findings)
        {
            if (finding.File is null)
                continue;

            if (!findingsByFile.TryGetValue(finding.File, out var fileFindings))
            {
                fileFindings = new List<LintFinding>();
                findingsByFile[finding.File] = fileFindings;
            }

            fileFindings.Add(finding);
        }

        var result = new List<LintFinding>(findings.Count);

        foreach (var filePath in findingsByFile.Keys)
        {
            // Find the corresponding migration file
            var migrationFile = migrationFiles.FirstOrDefault(mf => mf.FilePath.Equals(filePath, StringComparison.Ordinal));

            if (!findingsByFile.TryGetValue(filePath, out var fileFindings))
                continue;

            // Process findings for this file
            result.AddRange(ProcessFileFindings(fileFindings, migrationFile));
        }

        return result;
    }

    /// <summary>
    /// Processes findings for a single file to detect drop-then-add patterns.
    /// </summary>
    /// <param name="fileFindings">Findings for a single file.</param>
    /// <param name="migrationFile">The migration file containing the SQL operations.</param>
    /// <returns>Processed findings with downgraded severities where appropriate.</returns>
    private static IEnumerable<LintFinding> ProcessFileFindings(IReadOnlyList<LintFinding> fileFindings, MigrationFile? migrationFile = null)
    {
        // Extract all destructive operations from the file
        var destructiveOps = new List<DestructiveOperation>();

        foreach (var finding in fileFindings)
        {
            if (finding.File is null)
                continue;

            var op = ParseDestructiveOperation(finding, migrationFile);
            if (op is not null)
            {
                destructiveOps.Add(op);
            }
        }

        // Check for drop-then-add patterns
        foreach (var finding in fileFindings)
        {
            if (finding.File is null)
            {
                // Keep findings without file information as-is
                yield return finding;
                continue;
            }

            var op = ParseDestructiveOperation(finding, migrationFile);
            if (op is null)
            {
                // Not a destructive operation finding, keep as-is
                yield return finding;
                continue;
            }

            // Check if this drop is followed by another operation on the same object in the same file
            // (which suggests a drop+add pattern)
            var isRecreated = IsObjectRecreated(op, destructiveOps);

            if (isRecreated)
            {
                // Downgrade the severity and change the message to suggest rename instead
                yield return new LintFinding(
                    RuleName: finding.RuleName,
                    Severity: LintSeverity.Warning,
                    Message: GetRecreateMessage(op),
                    File: finding.File,
                    Line: finding.Line
                );
            }
            else
            {
                // Keep the original finding
                yield return finding;
            }
        }
    }

    /// <summary>
    /// Parses a destructive operation from a finding.
    /// </summary>
    /// <param name="finding">The finding to parse.</param>
    /// <param name="migrationFile">The migration file containing the SQL, if available.</param>
    /// <returns>A <see cref="DestructiveOperation"/> if the finding represents a destructive operation, otherwise null.</returns>
    private static DestructiveOperation? ParseDestructiveOperation(LintFinding finding, MigrationFile? migrationFile = null)
    {
        if (finding.File is null || finding.RuleName is null)
            return null;

        // Try to extract object names from the finding's rule name and message
        string? objectName = null;
        string? tableName = null;

        if (finding.RuleName.Equals("drop-column", StringComparison.OrdinalIgnoreCase))
        {
            // Try to extract column name from message
            var columnMatch = Regex.Match(finding.Message ?? "", @"DROP\s+COLUMN\s+(\w+)", RegexOptions.IgnoreCase);
            if (columnMatch.Success && columnMatch.Groups.Count >= 2)
            {
                objectName = columnMatch.Groups[1].Value;
            }

            // Try to extract table name from the SQL in the migration file
            if (migrationFile?.UpBody is not null)
            {
                // Look for DROP COLUMN in context of a table
                var tableMatch = Regex.Match(migrationFile.UpBody, @"DROP\s+COLUMN\s+\w+\s+ON\s+TABLE\s+(\w+)", RegexOptions.IgnoreCase);
                if (tableMatch.Success && tableMatch.Groups.Count >= 2)
                {
                    tableName = tableMatch.Groups[1].Value;
                }
            }
        }
        else if (finding.RuleName.Equals("drop-table", StringComparison.OrdinalIgnoreCase))
        {
            // Try to extract table name from message
            var tableMatch = Regex.Match(finding.Message ?? "", @"DROP\s+TABLE\s+(\w+)", RegexOptions.IgnoreCase);
            if (tableMatch.Success && tableMatch.Groups.Count >= 2)
            {
                objectName = tableMatch.Groups[1].Value;
            }
        }

        if (objectName is null)
            return null;

        return new DestructiveOperation(
            OperationType.Drop,
            finding.RuleName.Equals("drop-column", StringComparison.OrdinalIgnoreCase) ? ObjectType.Column : ObjectType.Table,
            objectName,
            tableName,
            finding.Line
        );
    }

    /// <summary>
    /// Determines if a destructive operation is followed by another operation on the same object in the same file,
    /// suggesting a drop-then-add pattern.
    /// </summary>
    /// <param name="op">The destructive operation.</param>
    /// <param name="allDestructiveOps">All destructive operations in the file.</param>
    /// <returns>True if the object appears to be recreated, otherwise false.</returns>
    private static bool IsObjectRecreated(DestructiveOperation op, IReadOnlyList<DestructiveOperation> allDestructiveOps)
    {
        // Detect drop-then-add by checking if there's another operation on the same object type with the same name
        // This pattern suggests a rename operation rather than actual data loss

        foreach (var otherOp in allDestructiveOps)
        {
            if (op != otherOp &&
                op.ObjectType == otherOp.ObjectType &&
                op.ObjectName.Equals(otherOp.ObjectName, StringComparison.OrdinalIgnoreCase))
            {
                // Found another operation on the same object - this suggests drop+add pattern
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Gets a message suggesting rename instead of drop+add.
    /// </summary>
    /// <param name="op">The destructive operation.</param>
    /// <returns>A message suggesting rename.</returns>
    private static string GetRecreateMessage(DestructiveOperation op)
    {
        return op.ObjectType switch
        {
            ObjectType.Column => $"DROP COLUMN followed by recreation of the same column suggests a rename operation. Consider using sp_rename or ALTER TABLE ... RENAME COLUMN to preserve data instead of drop+add which loses data.",
            ObjectType.Table => $"DROP TABLE followed by recreation of the same table suggests a rename operation. Consider using sp_rename to preserve data instead of drop+recreate which loses data.",
            _ => $"Destructive operation followed by recreation suggests a rename. Consider using a non-destructive approach to preserve data."
        };
    }

    /// <summary>
    /// Represents a destructive operation detected by a rule.
    /// </summary>
    private sealed record DestructiveOperation(
        OperationType OperationType,
        ObjectType ObjectType,
        string ObjectName,
        string? TableName,
        int? Line
    );

    /// <summary>
    /// The type of operation.
    /// </summary>
    private enum OperationType
    {
        Drop
    }

    /// <summary>
    /// The type of database object (column or table).
    /// </summary>
    private enum ObjectType
    {
        Column,
        Table
    }
}