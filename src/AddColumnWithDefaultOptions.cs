using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SqlMigrationLint;

/// <summary>
/// Options for <see cref="AddColumnWithDefaultRule"/>, read from the <c>options</c> section of .sqlmigrationlint.json.
/// </summary>
public sealed class AddColumnWithDefaultOptions
{
    /// <summary>
    /// Gets or sets regular expressions matched against the table name of each ADD COLUMN operation.
    /// Tables matching any pattern are not reported. Patterns are case-insensitive and unanchored,
    /// so use <c>^Name$</c> to match a single table exactly.
    /// </summary>
    public List<string> ExemptTablePatterns { get; set; } = new();

    /// <summary>
    /// Regex options shared by configuration validation and the rule, so a pattern that validates also matches the same way.
    /// </summary>
    internal const RegexOptions PatternOptions = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;

    /// <summary>
    /// Upper bound on a single pattern match, so a pathological pattern cannot stall the linter.
    /// </summary>
    internal static readonly TimeSpan PatternMatchTimeout = TimeSpan.FromSeconds(1);
}
