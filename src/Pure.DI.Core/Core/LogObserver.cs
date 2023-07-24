// ReSharper disable InvertIf
namespace Pure.DI.Core;

internal class LogObserver: ILogObserver
{
    private readonly DiagnosticSeverity _severity;
    private readonly IBuilder<LogEntry, LogInfo> _logInfoBuilder;
    private readonly IDiagnostic _diagnostic;
    private readonly HashSet<DiagnosticInfo> _diagnostics = new();
    private readonly bool _hasLogFile;

    public LogObserver(
        IGlobalOptions globalOptions,
        IBuilder<LogEntry, LogInfo> logInfoBuilder,
        IDiagnostic diagnostic)
    {
        _logInfoBuilder = logInfoBuilder;
        _diagnostic = diagnostic;
        _severity = globalOptions.Severity;
        _hasLogFile = string.IsNullOrWhiteSpace(globalOptions.LogFile);
    }

    public StringBuilder Log { get; } = new();
    
    public StringBuilder Outcome { get; } = new();

    public void OnNext(LogEntry logEntry)
    {
        if (logEntry.Severity >= _severity)
        {
            var logInfo = _logInfoBuilder.Build(logEntry, CancellationToken.None);
            if (logInfo.DiagnosticDescriptor is { } descriptor)
            {
                lock (_diagnostics)
                {
                    _diagnostics.Add(new DiagnosticInfo(descriptor, logEntry.Location));
                }
            }

            if (_hasLogFile)
            {
                foreach (var line in logInfo.Lines)
                {
                    Log.AppendLine(line);
                }
            }
        }
        else
        {
            if (_hasLogFile && logEntry.IsOutcome)
            {
                var logInfo = _logInfoBuilder.Build(logEntry, CancellationToken.None);
                Outcome.AppendLine(logInfo.Outcome);
            }
        }
    }

    public void OnError(Exception error)
    {
    }

    public void OnCompleted()
    {
    }

    public void Flush()
    {
        lock (_diagnostics)
        {
            foreach (var (descriptor, location) in _diagnostics)
            {
                _diagnostic.ReportDiagnostic(Diagnostic.Create(descriptor, location));
            }

            _diagnostics.Clear();
        }
    }

    private record DiagnosticInfo(
        DiagnosticDescriptor Descriptor,
        Location? Location);
}