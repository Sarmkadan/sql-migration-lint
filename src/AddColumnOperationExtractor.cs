using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SqlMigrationLint;

/// <summary>
/// Extracts <see cref="AddColumnOperation"/> instances from raw <c>ALTER TABLE ... ADD COLUMN</c> statements in a
/// migration's Up body, so that column-level rules see them with their real line numbers.
/// <para>Only statements with a DEFAULT clause are extracted. Statements are split on <c>;</c>, so a final statement
/// without a terminating semicolon is not recognised.</para>
/// </summary>
internal static class AddColumnOperationExtractor
{
    private static readonly Regex AddColumnStatement = new(
        @"ALTER\s+TABLE\s+(?<table>[^\s]+)\s+ADD\s+(?!(?:CONSTRAINT|PRIMARY|FOREIGN|UNIQUE|CHECK|INDEX)\b)(?:COLUMN\s+)?(?<column>[^\s]+)\s+(?<definition>[^;]*);",
        RegexOptions.IgnoreCase);

    private static readonly Regex DefaultClause = new(@"\bDEFAULT\s+(?<value>[\s\S]+)$", RegexOptions.IgnoreCase);

    private static readonly Regex NotNullClause = new(@"\bNOT\s+NULL\b", RegexOptions.IgnoreCase);

    private static readonly Regex IdentifierQuoting = new(@"[""\[\]`]");

    /// <summary>
    /// Extracts the ADD COLUMN operations that carry a DEFAULT clause from the Up body of <paramref name="file"/>.
    /// </summary>
    /// <param name="file">The parsed migration file.</param>
    /// <returns>The extracted operations in source order; empty when the file has no Up body.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="file"/> is null.</exception>
    public static IReadOnlyList<AddColumnOperation> Extract(MigrationFile file)
    {
        ArgumentNullException.ThrowIfNull(file);

        var operations = new List<AddColumnOperation>();
        if (file.UpBody is null)
        {
            return operations;
        }

        int upBodyStartIndex = FindUpBodyStartIndex(file.Lines);
        if (upBodyStartIndex < 0)
        {
            return operations;
        }

        foreach (Match statement in AddColumnStatement.Matches(file.UpBody))
        {
            var definition = statement.Groups["definition"].Value;
            var defaultMatch = DefaultClause.Match(definition);
            if (!defaultMatch.Success)
            {
                continue;
            }

            int linesBeforeStatement = file.UpBody.Substring(0, statement.Index).Count(c => c == '\n');

            operations.Add(new AddColumnOperation
            {
                File = file.FilePath,
                Line = upBodyStartIndex + linesBeforeStatement + 1,
                TableName = Unquote(statement.Groups["table"].Value),
                ColumnName = Unquote(statement.Groups["column"].Value),
                TableExists = true,
                IsNullable = !NotNullClause.IsMatch(definition),
                DefaultValue = defaultMatch.Groups["value"].Value.Trim()
            });
        }

        return operations;
    }

    /// <summary>
    /// Returns the zero-based index of the first line of the Up body, mirroring how <see cref="MigrationFile"/> slices it.
    /// </summary>
    private static int FindUpBodyStartIndex(string[] lines)
    {
        int signatureIndex = -1;
        for (int i = 0; i < lines.Length; i++)
        {
            if (Regex.IsMatch(lines[i], @"\bUp\s*\(\s*MigrationBuilder\b"))
            {
                signatureIndex = i;
            }
        }

        if (signatureIndex < 0)
        {
            return -1;
        }

        int bodyStart = signatureIndex + 1;
        if (bodyStart < lines.Length && lines[bodyStart].Trim() == "{")
        {
            bodyStart++;
        }

        return bodyStart;
    }

    private static string Unquote(string identifier) => IdentifierQuoting.Replace(identifier, string.Empty);
}
