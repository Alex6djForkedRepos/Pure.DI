﻿/*
$v=true
$p=16
$d=Tracking disposable instances in delegates
*/

// ReSharper disable CheckNamespace
// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedParameterInPartialMethod
// ReSharper disable ArrangeTypeModifiers
namespace Pure.DI.UsageTests.Basics.TrackingDisposableInDelegatesScenario;

using Xunit;

// {
interface IDependency
{
    bool IsDisposed { get; }
}

class Dependency : IDependency, IDisposable
{
    public bool IsDisposed { get; private set; }

    public void Dispose() => IsDisposed = true;
}

interface IService
{
    public IDependency Dependency { get; }
}

class Service(Func<(IDependency dependency, Owned owned)> dependencyFactory)
    : IService, IDisposable
{
    private readonly (IDependency value, Owned owned) _dependency = dependencyFactory();

    public IDependency Dependency => _dependency.value;
    
    public void Dispose() => _dependency.owned.Dispose();
}

partial class Composition
{
    private void Setup() =>
        DI.Setup(nameof(Composition))
            .Bind<IDependency>().To<Dependency>()
            .Bind<IService>().To<Service>()
            .Root<Service>("Root");
}
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
// {            
        var composition = new Composition();

        var root1 = composition.Root;
        var root2 = composition.Root;
        
        root2.Dispose();
        
        // Checks that the disposable instances
        // associated with root1 have been disposed of
        root2.Dependency.IsDisposed.ShouldBeTrue();
        
        // Checks that the disposable instances
        // associated with root2 have not been disposed of
        root1.Dependency.IsDisposed.ShouldBeFalse();
        
        root1.Dispose();
        
        // Checks that the disposable instances
        // associated with root2 have been disposed of
        root1.Dependency.IsDisposed.ShouldBeTrue();
        // }
        new Composition().SaveClassDiagram();
    }
}