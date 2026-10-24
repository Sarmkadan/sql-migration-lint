using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace SqlMigrationLint;

/// <summary>
/// Detects ADD COLUMN operations with DEFAULT values on existing tables.
/// </summary>
public sealed class AddColumnWithDefaultRule : ILintRule
{
    /// <summary>
    /// The rule id, used as the configuration key under <c>rules</c> and <c>options</c>.
    /// </summary>
    public const string RuleId = "add-column-with-default";

    public static readonly AddColumnWithDefaultRule Instance = new();

    private readonly Regex[] _exemptTablePatterns;

    /// <summary>
    /// Creates the rule with default options: no exempt tables.
    /// </summary>
    public AddColumnWithDefaultRule()
        : this(new AddColumnWithDefaultOptions())
    {
    }

    /// <summary>
    /// Creates the rule with the supplied options.
    /// </summary>
    /// <param name="options">The options to apply. Their patterns are validated when the rule is created.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is null.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when an option is invalid. The message lists every problem, not just the first.
    /// </exception>
    public AddColumnWithDefaultRule(AddColumnWithDefaultOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var problems = options.Validate();
        if (problems.Count > 0)
        {
            throw new ArgumentException(string.Join(Environment.NewLine, problems), nameof(options));
        }

        _exemptTablePatterns = options.ExemptTablePatterns
            .Select(pattern => new Regex(pattern, AddColumnWithDefaultOptions.PatternOptions, AddColumnWithDefaultOptions.PatternMatchTimeout))
            .ToArray();
    }

    public string Name => RuleId;
    public string Description => "Detects ADD COLUMN operations with DEFAULT values on existing tables that cause table rewrites.";
    public LintSeverity Severity => LintSeverity.Warning;

    public bool AppliesTo(MigrationOperation operation)
    {
        ValidateOperation(operation);

        return operation is AddColumnOperation { TableExists: true, DefaultValue: not null } addColumn
            && !IsExemptTable(addColumn.TableName);
    }

    public LintFinding? Evaluate(MigrationOperation operation)
    {
        ValidateOperation(operation);

        if (operation is not AddColumnOperation addColumn || IsExemptTable(addColumn.TableName))
        {
            return null;
        }

        return new LintFinding(
            RuleName: Name,
            Severity: Severity,
            Message: $"Adding column '{addColumn.ColumnName}' with default value to existing table '{addColumn.TableName}' causes table rewrite. Consider using a nullable column without default or add default in a separate migration.",
            File: addColumn.File,
            Line: addColumn.Line);
    }

    private bool IsExemptTable(string tableName)
    {
        return _exemptTablePatterns.Any(pattern => pattern.IsMatch(tableName));
    }

    private static void ValidateOperation(MigrationOperation operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        if (operation is SqlOperation sqlOperation && string.IsNullOrWhiteSpace(sqlOperation.Sql))
        {
            throw new ArgumentException("SQL text cannot be empty or whitespace.", nameof(operation));
        }
    }
}
