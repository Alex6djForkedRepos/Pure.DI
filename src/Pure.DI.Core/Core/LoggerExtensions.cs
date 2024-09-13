// ReSharper disable UnusedMember.Global
// ReSharper disable HeapView.ObjectAllocation.Evident
// ReSharper disable HeapView.ObjectAllocation
// ReSharper disable HeapView.ClosureAllocation
// ReSharper disable HeapView.DelegateAllocation

namespace Pure.DI.Core;

internal static class LoggerExtensions
{
    public static void CompileError<T>(this ILogger<T> logger, string errorMessage, in Location location, string id) =>
        logger.Log(new LogEntry(DiagnosticSeverity.Error, errorMessage, location, id));

    public static void CompileWarning<T>(this ILogger<T> logger, string waringMessage, in Location location, string id) =>
        logger.Log(new LogEntry(DiagnosticSeverity.Warning, waringMessage, location, id));

    public static void CompileInfo<T>(this ILogger<T> logger, string message, in Location location, string id) =>
        logger.Log(new LogEntry(DiagnosticSeverity.Info, message, location, id));
}