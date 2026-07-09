using System.Text.RegularExpressions;

namespace SqlMigrationLint;

/// <summary>
/// Rule to check for correct object naming conventions in SQL migrations.
/// </summary>
public sealed class NamingConventionRule : ILintRule
{
    private readonly string _indexPrefix;
    private readonly string _fkPrefix;
    private readonly string _pkPrefix;
    private readonly string _uqPrefix;

    public string Name => "ML102";
    public string Description => "Checks that database objects (indexes, foreign keys, primary keys, unique constraints) follow naming conventions.";
    public LintSeverity Severity => LintSeverity.Warning;

    public NamingConventionRule(string indexPrefix = "IX_", string fkPrefix = "FK_", string pkPrefix = "PK_", string uqPrefix = "UQ_")
    {
        _indexPrefix = indexPrefix;
        _fkPrefix = fkPrefix;
        _pkPrefix = pkPrefix;
        _uqPrefix = uqPrefix;
    }

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

        // We need to look for CREATE statements or ALTER statements defining these objects
        // A simple regex approach to find names of created objects.
        // E.g., CREATE [UNIQUE] INDEX [IX_Name] ...
        // E.g., ALTER TABLE ... ADD CONSTRAINT [PK_Name] ...
        // E.g., ALTER TABLE ... ADD CONSTRAINT [FK_Name] ...
        // E.g., ALTER TABLE ... ADD CONSTRAINT [UQ_Name] ...

        // This is a simplified check.
        // It might produce false positives, but it fulfills the spec.
        
        // Regex patterns for finding names
        // 1. CREATE [UNIQUE] INDEX Name ON Table(...)
        // 2. CONSTRAINT Name ...
        
        var sql = sqlOperation.Sql;

        // Check Index: CREATE [UNIQUE] INDEX ...
        // Need to extract the name.
        // Pattern: CREATE(?: UNIQUE)? INDEX (?:IF NOT EXISTS )?(\w+)
        var indexMatch = Regex.Match(sql, @"CREATE(?: UNIQUE)? INDEX (?:IF NOT EXISTS )?(\w+)", RegexOptions.IgnoreCase);
        if (indexMatch.Success)
        {
            var name = indexMatch.Groups[1].Value;
            if (!name.StartsWith(_indexPrefix, StringComparison.Ordinal))
            {
                return new LintFinding(
                    RuleName: Name,
                    Severity: Severity,
                    Message: $"Index '{name}' does not start with prefix '{_indexPrefix}'.",
                    File: sqlOperation.File,
                    Line: sqlOperation.Line);
            }
        }
        
        // Check Primary Key/Foreign Key/Unique: ALTER TABLE ... ADD CONSTRAINT Name ...
        // Pattern: CONSTRAINT (\w+) (?:PRIMARY KEY|FOREIGN KEY|UNIQUE)
        var constraintMatches = Regex.Matches(sql, @"CONSTRAINT (\w+) (PRIMARY KEY|FOREIGN KEY|UNIQUE)", RegexOptions.IgnoreCase);
        foreach (Match match in constraintMatches)
        {
            var name = match.Groups[1].Value;
            var type = match.Groups[2].Value.ToUpperInvariant();
            
            string expectedPrefix = type switch
            {
                "PRIMARY KEY" => _pkPrefix,
                "FOREIGN KEY" => _fkPrefix,
                "UNIQUE" => _uqPrefix,
                _ => string.Empty
            };
            
            if (!string.IsNullOrEmpty(expectedPrefix) && !name.StartsWith(expectedPrefix, StringComparison.Ordinal))
            {
                return new LintFinding(
                    RuleName: Name,
                    Severity: Severity,
                    Message: $"{type} constraint '{name}' does not start with prefix '{expectedPrefix}'.",
                    File: sqlOperation.File,
                    Line: sqlOperation.Line);
            }
        }

        return null;
    }
}
