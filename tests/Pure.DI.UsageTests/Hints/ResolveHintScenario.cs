﻿/*
$v=true
$p=0
$d=Resolve Hint
$h=The _Resolve_ hint determines whether to generate _Resolve_ methods. By default a set of four _Resolve_ methods are generated. Set this hint to _Off_ to disable the generation of resolve methods. This will reduce class composition generation time and no private composition roots will be generated in this case. When the _Resolve_ hint is disabled, only the public root properties are available, so be sure to define them explicitly with the `Root<T>(...)` method.
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameterInPartialMethod
// ReSharper disable UnusedVariable
// ReSharper disable UnusedParameter.Local
namespace Pure.DI.UsageTests.Hints.ResolveHintScenario;

using Xunit;

// {
internal interface IDependency { }

internal class Dependency : IDependency { }

internal interface IService { }

internal class Service : IService
{
    public Service(IDependency dependency)
    {
    }
}
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
        // ToString = On
// {            
        // Resolve = Off
        DI.Setup("Composition")
            .Bind<IDependency>().To<Dependency>().Root<IDependency>("DependencyRoot")
            .Bind<IService>().To<Service>().Root<IService>("Root");

        var composition = new Composition();
        var service = composition.Root;
        var dependencyRoot = composition.DependencyRoot;
// }
        TestTools.SaveClassDiagram(composition, nameof(ResolveHintScenario));
    }
}