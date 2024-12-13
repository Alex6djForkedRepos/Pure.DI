/*
$v=true
$p=301
$d=Serilog
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedType.Global
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedVariable

// ReSharper disable UnusedTypeParameter
// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedMemberInSuper.Global
#pragma warning disable CS9113 // Parameter is unread.
namespace Pure.DI.UsageTests.Advanced.SerilogScenario;

using Serilog;
using Serilog.Core;
using Serilog.Events;
using Xunit;

// {
interface IDependency;

class Dependency : IDependency
{
    public Dependency(ILogger<Dependency> log)
    {
        log.Information("created");
    }
}

interface IService
{
    IDependency Dependency { get; }
}

class Service : IService
{
    public Service(
        ILogger<Service> log,
        IDependency dependency)
    {
        Dependency = dependency;
        log.Information("created");
    }

    public IDependency Dependency { get; }
}

interface ILogger<T>: ILogger;

class Logger<T>(ILogger logger) : ILogger<T>
{
    private readonly ILogger _logger =
        logger.ForContext(typeof(T));
    
    public void Write(LogEvent logEvent) =>
        _logger.Write(logEvent);
}

class EventSink(ICollection<LogEvent> events)
    : ILogEventSink
{
    public void Emit(LogEvent logEvent) =>
        events.Add(logEvent);
}

partial class Composition
{
    private void Setup() =>
        DI.Setup(nameof(Composition))
            .Arg<ILogger>("logger")
            .Bind().As(Lifetime.Singleton).To<Logger<TT>>()

            .Bind().To<Dependency>()
            .Bind().To<Service>()
            .Root<IService>(nameof(Root));
}
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
// {
        var events = new List<LogEvent>(); 
        var serilogLogger = new LoggerConfiguration()
            .WriteTo.Sink(new EventSink(events))
            .CreateLogger();

        var composition = new Composition(logger: serilogLogger);
        var service = composition.Root;
        events.Count.ShouldBe(2);
// }
        composition.SaveClassDiagram();
    }
}