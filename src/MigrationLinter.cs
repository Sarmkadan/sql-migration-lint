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
    private readonly IReadOnlyList<ILintRule> _perOperationRules;
    private readonly IReadOnlyList<IPerFileLintRule> _perFileRules;
    private readonly IReadOnlyList<IGlobalLintRule> _globalRules;
    private readonly LintConfig? _config;
    private LintReport? _lintReport;

    /// <summary>
    /// Gets the report from the most recently completed <see cref="Lint(string)"/> call on this instance, if any.
    /// </summary>
    public LintReport? LintReport => _lintReport;

    /// <summary>
    /// Gets the configuration used for this linter instance.
    /// </summary>
    public LintConfig? Config => _config;

    /// <summary>
    /// Creates a new <see cref="MigrationLinter"/> with the supplied rules.
    /// </summary>
    /// <param name="perOperationRules">The collection of operation-scoped lint rules to apply.</param>
    /// <param name="globalRules">The collection of global lint rules to apply.</param>
    /// <param name="config">Optional configuration to override rule severities and disable rules.</param>
    /// <param name="perFileRules">
    /// Optional collection of <see cref="IPerFileLintRule"/> implementations that receive the already-parsed
    /// <see cref="MigrationFile"/> directly.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="perOperationRules"/> is null.</exception>
    public MigrationLinter(
        IEnumerable<ILintRule> perOperationRules,
        IEnumerable<IGlobalLintRule>? globalRules = null,
        LintConfig? config = null,
        IEnumerable<IPerFileLintRule>? perFileRules = null)
    {
        ArgumentNullException.ThrowIfNull(perOperationRules);
        _perOperationRules = perOperationRules.ToArray();
        _globalRules = globalRules?.ToArray() ?? Array.Empty<IGlobalLintRule>();
        _perFileRules = perFileRules?.ToArray() ?? Array.Empty<IPerFileLintRule>();
        _config = config;
    }

    /// <summary>
    /// Creates a <see cref="MigrationLinter"/> pre‑populated with all built‑in rules.
    /// </summary>
    /// <param name="configPath">Optional path to .sqlmigrationlint.json configuration file.</param>
    public static MigrationLinter CreateDefault(string? configPath = null)
    {
        var config = configPath != null ? LintConfig.Load(configPath) : null;
        return new MigrationLinter(
            perOperationRules: LintRuleRegistry.PerOperationRules,
            globalRules: LintRuleRegistry.GlobalRules,
            config: config,
            perFileRules: LintRuleRegistry.PerFileRules);
    }

    /// <summary>
    /// Creates a <see cref="MigrationLinter"/> pre‑populated with all built‑in rules including global rules.
    /// </summary>
    /// <param name="configPath">Optional path to .sqlmigrationlint.json configuration file.</param>
    public static MigrationLinter CreateDefaultWithGlobalRules(string? configPath = null)
    {
        var config = configPath != null ? LintConfig.Load(configPath) : null;
        var globalRules = LintRuleRegistry.GlobalRules.ToList();
        globalRules.Add(DuplicateMigrationVersionRule.Instance);

        return new MigrationLinter(
            perOperationRules: LintRuleRegistry.PerOperationRules,
            globalRules: globalRules,
            config: config,
            perFileRules: LintRuleRegistry.PerFileRules);
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

        var migrationFiles = Directory.EnumerateFiles(
                migrationsFolder,
                "*.cs",
                SearchOption.AllDirectories)
            .Where(f => !f.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase))
            .Where(f => !Path.GetFileName(f).Contains("Snapshot", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        var findings = new List<LintFinding>();
        int migrationsScanned = 0;

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

        foreach (var globalRule in _globalRules)
        {
            AddConfiguredFindings(
                findings,
                globalRule.Name,
                globalRule.Severity,
                () => globalRule.Evaluate(parsedMigrationFiles));
        }

        foreach (var file in migrationFiles)
        {
            var migrationFile = parsedMigrationFiles.FirstOrDefault(mf => mf.FilePath.Equals(file, StringComparison.Ordinal));
            if (migrationFile is null)
                continue;

            foreach (var fileRule in _perFileRules)
            {
                AddConfiguredFindings(
                    findings,
                    fileRule.RuleName,
                    defaultSeverity: null,
                    () => fileRule.Check(migrationFile, _config));
            }

            var sqlOperation = new SqlOperation
            {
                File = file,
                Line = 1,
                Sql = migrationFile.UpBody ?? string.Empty
            };

            foreach (var rule in _perOperationRules)
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
