namespace SqlMigrationLint;

internal static class NonConcurrentIndexRule
{
    public static IReadOnlyList<ILintRule> All => [Instance];

    public static readonly NonConcurrentIndexRuleImpl Instance = new();

    internal sealed class NonConcurrentIndexRuleImpl : ILintRule
    {
        public string Name => "non-concurrent-index";
        public string Description => "Detects CREATE INDEX, DROP INDEX, REINDEX, and CLUSTER statements that lack CONCURRENTLY/ONLINE options (table-locking on Postgres).";
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

            // Skip if inside comments
            var sql = sqlOperation.Sql;
            var trimmedSql = sql.Trim();
            if (trimmedSql.StartsWith("//") || trimmedSql.StartsWith("/*") || trimmedSql.StartsWith("*"))
            {
                return null;
            }

            // Normalize to uppercase for case-insensitive matching
            var upperSql = sql.ToUpperInvariant();

            // Check for CREATE INDEX statements without CONCURRENTLY
            bool isCreateIndex = upperSql.Contains("CREATE INDEX");
            bool isDropIndex = upperSql.Contains("DROP INDEX");
            bool isReindex = upperSql.Contains("REINDEX");
            bool isCluster = upperSql.Contains("CLUSTER");

            if (!isCreateIndex && !isDropIndex && !isReindex && !isCluster)
            {
                return null;
            }

            // Check if the statement uses CONCURRENTLY or ONLINE options
            bool hasConcurrently = upperSql.Contains("CONCURRENTLY");
            bool hasOnline = upperSql.Contains("ONLINE");
            bool isConcurrent = hasConcurrently || hasOnline;

            if (!isConcurrent)
            {
                string statementType;
                if (isCreateIndex) statementType = "CREATE INDEX";
                else if (isDropIndex) statementType = "DROP INDEX";
                else if (isReindex) statementType = "REINDEX";
                else if (isCluster) statementType = "CLUSTER";
                else statementType = "index operation";

                return new LintFinding(
                    RuleName: Name,
                    Severity: Severity,
                    Message: $"Statement '{statementType}' lacks CONCURRENTLY/ONLINE option and will lock the table. Consider adding 'CONCURRENTLY' for PostgreSQL operations.",
                    File: sqlOperation?.File,
                    Line: sqlOperation?.Line
                );
            }

            return null;
        }
    }
}