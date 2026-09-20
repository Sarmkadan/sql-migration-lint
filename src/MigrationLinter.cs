using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SqlMigrationLint;

/// <summary>
/// Orchestrates linting of migration files using a set of <see cref="ILintRule"/> and <see cref="IGlobalLintRule"/> implementations.
/// </summary>
public sealed class MigrationLinter
{
    private readonly IReadOnlyList<ILintRule> _perFileRules;
    private readonly IReadOnlyList<IPerFileLintRule> _perFileFileRules;
    private readonly IReadOnlyList<IGlobalLintRule> _globalRules;
    private readonly LintConfig? _config;
    private LintReport? _lintReport;

    /// <summary>
    /// Gets the report from the most recently completed <see cref="Lint(string)"/> call on this instance, if any.
    /// </summary>
    /// <remarks>
    /// This compatibility property represents last-run state. When lint operations overlap, it may be replaced by
    /// whichever operation completes last; callers that need an operation's report should use the value returned by
    /// that <see cref="Lint(string)"/> call.
    /// </remarks>
    public LintReport? LintReport => _lintReport;

    /// <summary>
    /// Gets the configuration used for this linter instance.
    /// </summary>
    public LintConfig? Config => _config;

    /// <summary>
    /// Creates a new <see cref="MigrationLinter"/> with the supplied rules.
    /// </summary>
    /// <param name="perFileRules">The collection of operation-scoped per-file lint rules to apply.</param>
    /// <param name="globalRules">The collection of global lint rules to apply.</param>
    /// <param name="config">Optional configuration to override rule severities and disable rules.</param>
    /// <param name="fileScopedRules">
    /// Optional collection of <see cref="IPerFileLintRule"/> implementations that receive the already-parsed
    /// <see cref="MigrationFile"/> directly, instead of the <see cref="SqlOperation"/> abstraction used by
    /// <paramref name="perFileRules"/>.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="perFileRules"/> is null.</exception>
    public MigrationLinter(
        IEnumerable<ILintRule> perFileRules,
        IEnumerable<IGlobalLintRule>? globalRules = null,
        LintConfig? config = null,
        IEnumerable<IPerFileLintRule>? fileScopedRules = null)
    {
        ArgumentNullException.ThrowIfNull(perFileRules);
        _perFileRules = perFileRules.ToArray();
        _globalRules = globalRules?.ToArray() ?? Array.Empty<IGlobalLintRule>();
        _perFileFileRules = fileScopedRules?.ToArray() ?? Array.Empty<IPerFileLintRule>();
        _config = config;
    }

    /// <summary>
    /// Creates a <see cref="MigrationLinter"/> pre‑populated with all built‑in rules.
    /// </summary>
    /// <param name="configPath">Optional path to .sqlmigrationlint.json configuration file.</param>
    public static MigrationLinter CreateDefault(string? configPath = null)
    {
        var config = configPath != null ? LintConfig.Load(configPath) : null;
        var (allRules, fileScopedRules) = CreateBuiltInRuleLists();

        // No global rules by default
        return new MigrationLinter(allRules, config: config, fileScopedRules: fileScopedRules);
    }

    /// <summary>
    /// Creates a <see cref="MigrationLinter"/> pre‑populated with all built‑in rules including global rules.
    /// </summary>
    /// <param name="configPath">Optional path to .sqlmigrationlint.json configuration file.</param>
    public static MigrationLinter CreateDefaultWithGlobalRules(string? configPath = null)
    {
        var config = configPath != null ? LintConfig.Load(configPath) : null;
        var (allRules, fileScopedRules) = CreateBuiltInRuleLists();

        var globalRules = new List<IGlobalLintRule>();
        globalRules.Add(DuplicateMigrationVersionRule.Instance);

        return new MigrationLinter(allRules, globalRules, config: config, fileScopedRules: fileScopedRules);
    }

    private static (IReadOnlyList<ILintRule> PerFileRules, IReadOnlyList<IPerFileLintRule> FileScopedRules)
        CreateBuiltInRuleLists()
    {
        var fileScopedRules = new List<IPerFileLintRule>
        {
            new NamingConventionRule(),
            new MissingDownMigrationRule()
        };

        // Add the destructive operation rules (which implement IPerFileLintRule)
        fileScopedRules.AddRange(DestructiveOperationRules.All.Cast<IPerFileLintRule>());

        // The per-file rules (for ILintRule) should exclude the destructive operation rules
        // since they are now handled as file-scoped rules.
        var allRules = LintRuleRegistry.AllRules.ToList();
        var destructiveOperationRules = DestructiveOperationRules.All.ToList();
        var perFileRules = allRules.Except(destructiveOperationRules).ToList();

        return (perFileRules, fileScopedRules);
    }

    /// <summary>
    /// Lints all migration files under <c>/Migrations</c> relative to <paramref name="rootPath"/>.
    /// </summary>
    /// <param name="rootPath">The directory that contains the <c>Migrations</c> folder.</param>
    /// <returns>A report describing the findings.</returns>
    public LintReport Lint(string rootPath)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
            throw new ArgumentException("Root path must be provided.", nameof(rootPath));

        var migrationsFolder = Path.Combine(rootPath, "Migrations");
        if (!Directory.Exists(migrationsFolder))
            throw new DirectoryNotFoundException($"Migrations folder not found: {migrationsFolder}");

        // Find *.cs files, excluding Designer files and snapshot files.
        var migrationFiles = Directory.EnumerateFiles(
                migrationsFolder,
                "*.cs",
                SearchOption.AllDirectories)
            .Where(f => !f.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase))
            .Where(f => !Path.GetFileName(f).Contains("Snapshot", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        var findings = new List<LintFinding>();
        int migrationsScanned = 0;

        // Parse all migration files first
        var parsedMigrationFiles = new List<MigrationFile>();
        foreach (var file in migrationFiles)
        {
            var migrationFile = MigrationFile.TryParse(file);
            if (migrationFile is not null)
            {
                migrationsScanned++;
                parsedMigrationFiles.Add(migrationFile);
            }
        }

        // Apply global rules (operate on all migration files)
        foreach (var globalRule in _globalRules)
        {
            AddConfiguredFindings(
                findings,
                globalRule.Name,
                globalRule.Severity,
                () => globalRule.Evaluate(parsedMigrationFiles));
        }

        // Apply per-file rules
        foreach (var file in migrationFiles)
        {
            // Find the corresponding parsed migration file
            var migrationFile = parsedMigrationFiles.FirstOrDefault(mf => mf.FilePath.Equals(file, StringComparison.Ordinal));

            if (migrationFile is null)
                continue;

            // Apply file-scoped rules directly against the already-parsed migration file,
            // avoiding any redundant re-parsing of the file from disk.
            foreach (var fileRule in _perFileFileRules)
            {
                AddConfiguredFindings(
                    findings,
                    fileRule.RuleName,
                    defaultSeverity: null,
                    () => fileRule.Check(migrationFile, _config));
            }

            // Build a generic SqlOperation that represents the Up body of the migration.
            var sqlOperation = new SqlOperation
            {
                File = file,
                Line = 1,
                Sql = migrationFile.UpBody ?? string.Empty
            };

            foreach (var rule in _perFileRules)
            {
                AddConfiguredFindings(
                    findings,
                    rule.Name,
                    rule.Severity,
                    () => rule.AppliesTo(sqlOperation)
                        ? new[] { rule.Evaluate(sqlOperation) }.OfType<LintFinding>()
                        : Array.Empty<LintFinding>());
            }
        }

        bool hasBlockers = findings.Any(f => f.Severity == LintSeverity.Blocker);
        RiskLevel maxRisk = RiskLevel.None;

        foreach (var f in findings)
        {
            var level = f.Severity switch
            {
                LintSeverity.Blocker => RiskLevel.Blocker,
                LintSeverity.Danger => RiskLevel.Danger,
                LintSeverity.Warning => RiskLevel.Warning,
                _ => RiskLevel.None
            };

            if (level > maxRisk)
                maxRisk = level;
        }

        // Post-process findings to detect drop-then-add patterns
        var processedFindings = DestructiveOperationRulesValidation.DetectDestructiveRecreatePatterns(findings, parsedMigrationFiles);

        bool processedHasBlockers = processedFindings.Any(f => f.Severity == LintSeverity.Blocker);
        RiskLevel processedMaxRisk = RiskLevel.None;

        foreach (var f in processedFindings)
        {
            var level = f.Severity switch
            {
                LintSeverity.Blocker => RiskLevel.Blocker,
                LintSeverity.Danger => RiskLevel.Danger,
                LintSeverity.Warning => RiskLevel.Warning,
                _ => RiskLevel.None
            };

            if (level > processedMaxRisk)
                processedMaxRisk = level;
        }

        return SetLintReport(new LintReport(processedFindings, migrationsScanned, processedHasBlockers, processedMaxRisk));
    }

    private void AddConfiguredFindings(
        ICollection<LintFinding> findings,
        string ruleName,
        LintSeverity? defaultSeverity,
        Func<IEnumerable<LintFinding>?> evaluate)
    {
        if (_config?.ShouldEvaluateRule(ruleName) == false)
            return;

        var ruleFindings = evaluate();
        if (ruleFindings is null)
            return;

        foreach (var finding in ruleFindings)
        {
            var severity = _config?.GetEffectiveSeverity(
                ruleName,
                defaultSeverity ?? finding.Severity) ?? finding.Severity;
            findings.Add(finding with { Severity = severity });
        }
    }

    private LintReport SetLintReport(LintReport report)
    {
        _lintReport = report;
        return report;
    }
}
