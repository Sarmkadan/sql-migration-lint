using System.Collections.Generic;

namespace SqlMigrationLint;

/// <summary>
/// Represents a lint rule that operates directly on a single parsed <see cref="MigrationFile"/>,
/// as opposed to <see cref="IGlobalLintRule"/> (which operates on the entire set of migration files)
/// or <see cref="ILintRule"/> (which operates on an individual <see cref="MigrationOperation"/>).
/// </summary>
/// <remarks>
/// Rules that implement this interface receive the already-parsed <see cref="MigrationFile"/> instance
/// produced once by <see cref="MigrationLinter"/>, avoiding redundant re-parsing of the same file from
/// disk that ad-hoc, file-aware rules previously performed on their own.
/// </remarks>
public interface IPerFileLintRule
{
    /// <summary>
    /// Gets the unique name of the rule, used for configuration lookups (enabling/disabling the rule)
    /// and for tagging produced findings.
    /// </summary>
    string RuleName { get; }

    /// <summary>
    /// Checks a single migration file and returns any findings.
    /// </summary>
    /// <param name="file">The parsed migration file to check.</param>
    /// <param name="config">The active lint configuration, or null if no configuration file was loaded.</param>
    /// <returns>A collection of lint findings; empty if the file has no issues for this rule.</returns>
    IEnumerable<LintFinding> Check(MigrationFile file, LintConfig? config);
}
