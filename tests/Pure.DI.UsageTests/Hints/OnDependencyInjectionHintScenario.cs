﻿/*
$v=true
$p=2
$d=OnDependencyInjection hint
$h=Hints are used to fine-tune code generation. The _OnDependencyInjection_ hint determines whether to generate partial _OnDependencyInjection_ method to control of dependency injection.
$h=In addition, setup hints can be comments before the _Setup_ method in the form ```hint = value```, for example: `// OnDependencyInjection = On`.
$f=The `OnDependencyInjectionContractTypeNameRegularExpression` hint helps identify the set of types that require injection control. You can use it to specify a regular expression to filter the full name of a type.
$f=For more hints, see [this](https://github.com/DevTeam/Pure.DI/blob/master/README.md#setup-hints) page.
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedParameterInPartialMethod
// ReSharper disable UnusedVariable
// ReSharper disable UnusedMemberInSuper.Global
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable UnusedMember.Global
namespace Pure.DI.UsageTests.Hints.OnDependencyInjectionHintScenario;

using System.Collections.Immutable;
using Shouldly;
using Xunit;

// {
using static Hint;

interface IDependency;

class Dependency : IDependency;

interface IService
{
    IDependency Dependency { get; }
}

class Service(IDependency dependency) : IService
{
    public IDependency Dependency { get; } = dependency;
}

partial class Composition
{
    private readonly List<string> _log = [];

    public Composition(List<string> log) : this() =>
        _log = log;

    private partial T OnDependencyInjection<T>(
        in T value,
        object? tag,
        Lifetime lifetime)
    {
        _log.Add($"{value?.GetType().Name} injected");
        return value;
    }
}
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
// {            
        // OnDependencyInjection = On
        DI.Setup(nameof(Composition))
            .Hint(OnDependencyInjectionContractTypeNameRegularExpression, nameof(IDependency))
            .Bind().To<Dependency>()
            .RootBind<IService>("Root").To<Service>();

        var log = new List<string>();
        var composition = new Composition(log);
        var service = composition.Root;
        
        log.ShouldBe(ImmutableArray.Create("Dependency injected"));
// }
        composition.SaveClassDiagram();
    }
}