using System.IO;

namespace SqlMigrationLint;

/// <summary>
/// Adapter that implements <see cref="IReportWriter"/> by delegating to the existing
/// <see cref="ConsoleReportWriter"/> static class. This allows the console writer to be
/// used interchangeably with other <see cref="IReportWriter"/> implementations.
/// </summary>
public class ConsoleReportWriterAdapter : IReportWriter
{
    /// <inheritdoc/>
    public void WriteReport(LintReport report, TextWriter writer)
    {
        // ConsoleReportWriter writes directly to the console; the supplied writer is ignored.
        ConsoleReportWriter.WriteReport(report);
    }
}
