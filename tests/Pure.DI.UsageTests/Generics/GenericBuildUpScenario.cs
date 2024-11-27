﻿/*
$v=true
$p=7
$d=Build up of an existing generic object
$h=In other words, injecting the necessary dependencies via methods, properties, or fields into an existing object.
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameter.Local
// ReSharper disable ArrangeTypeModifiers

namespace Pure.DI.UsageTests.Basics.GenericBuildUpScenario;

using Shouldly;
using Xunit;

// {
interface IDependency<out T>
    where T: struct
{
    string Name { get; }

    T Id { get; }
}

class Dependency<T> : IDependency<T>
    where T: struct
{
    // The Ordinal attribute specifies to perform an injection and its order
    [Ordinal(1)]
    public string Name { get; set; } = "";
    
    public T Id { get; private set; }

    // The Ordinal attribute specifies to perform an injection and its order
    [Ordinal(0)]
    public void SetId(T id) => Id = id;
}

interface IService<out T>
    where T: struct
{
    IDependency<T> Dependency { get; }
}

record Service<T>(IDependency<T> Dependency)
    : IService<T> where T: struct;
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
        // Resolve = Off
// {
        DI.Setup(nameof(Composition))
            .RootArg<string>("name")
            .Bind().To(_ => Guid.NewGuid())
            .Bind().To(ctx =>
            {
                var dependency = new Dependency<TTS>();
                ctx.BuildUp(dependency);
                return dependency;
            })
            .Bind().To<Service<TTS>>()

            // Composition root
            .Root<IService<Guid>>("GetMyService");

        var composition = new Composition();
        var service = composition.GetMyService("Some name");
        service.Dependency.Name.ShouldBe("Some name");
        service.Dependency.Id.ShouldNotBe(Guid.Empty);
// }
        composition.SaveClassDiagram();
    }
}