using System.IO;

namespace SqlMigrationLint;

/// <summary>
/// Represents a writer that can output a <see cref="LintReport"/> to a <see cref="TextWriter"/>.
/// Implementations decide the output format (e.g., JSON, GitHub annotations, console text).
/// </summary>
public interface IReportWriter
{
    /// <summary>
    /// Writes the supplied <paramref name="report"/> to the given <paramref name="writer"/>.
    /// </summary>
    /// <param name="report">The lint report to write.</param>
    /// <param name="writer">The destination writer.</param>
    void WriteReport(LintReport report, TextWriter writer);
}
