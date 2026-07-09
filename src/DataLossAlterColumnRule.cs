using System;
using System.Text.RegularExpressions;

namespace SqlMigrationLint;

/// <summary>
/// Detects dangerous ALTER COLUMN operations that can cause data loss.
/// </summary>
internal sealed class DataLossAlterColumnRule : ILintRule
{
    public static readonly DataLossAlterColumnRule Instance = new();

    public string Name => "data-loss-alter-column";
    public string Description => "Detects ALTER COLUMN operations that may cause data truncation or loss.";
    public LintSeverity Severity => LintSeverity.Danger;

    public bool AppliesTo(MigrationOperation operation)
    {
        return operation is AlterColumnOperation;
    }

    public LintFinding? Evaluate(MigrationOperation operation)
    {
        if (operation is not AlterColumnOperation alterColumn)
        {
            return null;
        }

        // 1. Decrease of varchar/nvarchar length
        if (TryGetStringLengthDecrease(alterColumn, out var oldLength, out var newLength))
        {
            return new LintFinding(
                RuleName: Name,
                Severity: Severity,
                Message: $"ALTER COLUMN '{alterColumn.ColumnName}' reduces string length from {oldLength} to {newLength} in table '{alterColumn.TableName}', which may truncate existing data.",
                File: alterColumn?.File,
                Line: alterColumn?.Line);
        }

        // 2. Type narrowing (bigint→int, nvarchar→varchar, decimal precision reduction)
        if (IsTypeNarrowing(alterColumn))
        {
            return new LintFinding(
                RuleName: Name,
                Severity: Severity,
                Message: $"ALTER COLUMN '{alterColumn.ColumnName}' changes type from '{alterColumn.OldType}' to '{alterColumn.NewType}' in table '{alterColumn.TableName}', which may cause data truncation or loss.",
                File: alterColumn?.File,
                Line: alterColumn?.Line);
        }

        // 3. Adding NOT NULL without a DEFAULT value
        if (IsAddingNotNullWithoutDefault(alterColumn))
        {
            return new LintFinding(
                RuleName: Name,
                Severity: Severity,
                Message: $"ALTER COLUMN '{alterColumn.ColumnName}' adds NOT NULL constraint without providing a DEFAULT value in table '{alterColumn.TableName}', which will fail on existing NULL rows.",
                File: alterColumn?.File,
                Line: alterColumn?.Line);
        }

        return null;
    }

    private static bool TryGetStringLengthDecrease(AlterColumnOperation alterColumn, out int oldLength, out int newLength)
    {
        oldLength = -1;
        newLength = -1;

        // Detect old type like varchar(255) or nvarchar(100)
        if (alterColumn.OldType is string oldStoreType &&
            (oldStoreType.StartsWith("varchar", StringComparison.OrdinalIgnoreCase) ||
             oldStoreType.StartsWith("nvarchar", StringComparison.OrdinalIgnoreCase)))
        {
            var oldMatch = Regex.Match(oldStoreType, @"\((\d+)\)");
            if (oldMatch.Success && int.TryParse(oldMatch.Groups[1].Value, out oldLength))
            {
                // Detect new type with length
                if (alterColumn.NewType is string newStoreType &&
                    (newStoreType.StartsWith("varchar", StringComparison.OrdinalIgnoreCase) ||
                     newStoreType.StartsWith("nvarchar", StringComparison.OrdinalIgnoreCase)))
                {
                    var newMatch = Regex.Match(newStoreType, @"\((\d+)\)");
                    if (newMatch.Success && int.TryParse(newMatch.Groups[1].Value, out newLength) && newLength < oldLength)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private static bool IsTypeNarrowing(AlterColumnOperation alterColumn)
    {
        var oldType = alterColumn.OldType ?? string.Empty;
        var newType = alterColumn.NewType ?? string.Empty;

        if (string.Equals(oldType, newType, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // bigint → int
        if (oldType.StartsWith("bigint", StringComparison.OrdinalIgnoreCase) &&
            newType.StartsWith("int", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // nvarchar → varchar (loss of Unicode support)
        if (oldType.StartsWith("nvarchar", StringComparison.OrdinalIgnoreCase) &&
            newType.StartsWith("varchar", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // decimal precision reduction (e.g., decimal(18,2) → decimal(10,2))
        if (oldType.StartsWith("decimal", StringComparison.OrdinalIgnoreCase) &&
            newType.StartsWith("decimal", StringComparison.OrdinalIgnoreCase))
        {
            var oldMatch = Regex.Match(oldType, @"decimal\((\d+),\s*\d+\)");
            var newMatch = Regex.Match(newType, @"decimal\((\d+),\s*\d+\)");

            if (oldMatch.Success && newMatch.Success &&
                int.Parse(oldMatch.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) > int.Parse(newMatch.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsAddingNotNullWithoutDefault(AlterColumnOperation alterColumn)
    {
        // Detect a transition from a nullable definition to a NOT NULL definition.
        if (alterColumn.OldType is string oldType && alterColumn.NewType is string newType)
        {
            bool oldIsNullable = oldType.Contains("null", StringComparison.OrdinalIgnoreCase);
            bool newIsNotNull = newType.Contains("not null", StringComparison.OrdinalIgnoreCase);

            if (oldIsNullable && newIsNotNull)
            {
                // If the new definition does not contain a DEFAULT clause, we consider it risky.
                bool hasDefault = newType.Contains("default", StringComparison.OrdinalIgnoreCase);
                return !hasDefault;
            }
        }

        return false;
    }
}
