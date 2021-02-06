using System;
using System.Text.RegularExpressions;

namespace SqlMigrationLint;

/// <summary>
/// Detects migration files where DownBody is empty or contains only throw statements/comments,
/// while UpBody is non-empty. This indicates an irreversible migration.
/// </summary>
internal sealed class EmptyDownRule : ILintRule
{
    public static readonly EmptyDownRule Instance = new();

    public string Name => "ML100";
    public string Description => "Detects migrations with empty Down body that are not reversible.";
    public LintSeverity Severity => LintSeverity.Warning;

    public bool AppliesTo(MigrationOperation operation)
    {
        return operation is SqlOperation;
    }

    public LintFinding? Evaluate(MigrationOperation operation)
    {
        if (operation is not SqlOperation sqlOperation)
        {
            return null;
        }

        if (sqlOperation.File is null)
        {
            return null;
        }

        MigrationFile? migrationFile = MigrationFile.TryParse(sqlOperation.File);
        if (migrationFile is null)
        {
            return null;
        }

        // Check if DownBody is empty or contains only throw/whitespace/comments
        bool isDownEmpty = string.IsNullOrWhiteSpace(migrationFile.DownBody);

        if (!isDownEmpty)
        {
            // Check if DownBody contains only throw statements, comments, or whitespace
            string downBodyTrimmed = migrationFile.DownBody.Trim();
            bool containsOnlyThrowOrComments = IsOnlyThrowOrComments(downBodyTrimmed);

            if (containsOnlyThrowOrComments)
            {
                isDownEmpty = true;
            }
        }

        // Only flag if UpBody is non-empty
        if (isDownEmpty && !string.IsNullOrWhiteSpace(migrationFile.UpBody))
        {
            return new LintFinding(
                RuleName: Name,
                Severity: Severity,
                Message: $"Migration '{migrationFile.MigrationName}' has empty Down body and is not reversible.",
                File: sqlOperation.File,
                Line: sqlOperation.Line);
        }

        return null;
    }

    private static bool IsOnlyThrowOrComments(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return true;
        }

        // Remove comments (both // and /* */ style)
        string withoutComments = Regex.Replace(text, @"/\*.*?\*/|//.*?(?=\r?\n|$)", string.Empty, RegexOptions.Singleline | RegexOptions.IgnoreCase);

        // Trim whitespace
        withoutComments = withoutComments.Trim();

        // Check if empty after removing comments
        if (string.IsNullOrWhiteSpace(withoutComments))
        {
            return true;
        }

        // Check if it's just a throw statement
        string normalized = withoutComments.Replace("\r\n", " ").Replace('\n', ' ').Replace('\r', ' ').Trim();

        if (normalized.StartsWith("throw", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }
}
