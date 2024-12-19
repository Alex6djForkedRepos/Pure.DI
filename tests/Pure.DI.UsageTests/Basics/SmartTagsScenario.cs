﻿/*
$v=true
$p=7
$d=Smart tags
$h=When you need to compose a large composition of objects, you may need a large number of tags. Strings or other constant values are not always convenient to use, because there can be infinitely many variants of numbers or strings. And if you specify one value in the binding, you can make a mistake and specify another value in the dependency, which will lead to a compilation error. The solution to this problem is to create an enumerable type and use its values as tags.  _Pure.DI_ makes it easier to solve this problem.
$h=
$h=When you specify a tag in a binding and the compiler can't determine what that value is, _Pure.DI_ will automatically create a constant for it inside the `Pure.DI.Tag` type. For the example below, the set of constants would look like this:
$h=
$h=```c#
$h=namespace Pure.DI
$h={
$h=  internal partial class Tag
$h=  {
$h=    public const string Abc = "Abc";
$h=    public const string Xyz = "Xyz";
$h=  }
$h=}
$h=```
$h=In this way you can apply refactoring in the development environment. And also changes of tags in bindings will be automatically checked by the compiler. This will reduce the number of errors.
$h=The example below also uses the `using static Pure.DI.Tag;` directive to access tags in `Pure.DI.Tag` without specifying a type name:
*/

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable CheckNamespace
// ReSharper disable UnusedType.Global
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable ClassNeverInstantiated.Global

// ReSharper disable PreferConcreteValueOverDefault
// ReSharper disable RedundantNameQualifier
namespace Pure.DI.UsageTests.Basics.SmartTagsScenario;

using Shouldly;
using Xunit;

// {
using static Pure.DI.Tag;
using static Pure.DI.Lifetime;

interface IDependency;

class AbcDependency : IDependency;

class XyzDependency : IDependency;

class Dependency : IDependency;

interface IService
{
    IDependency Dependency1 { get; }

    IDependency Dependency2 { get; }

    IDependency Dependency3 { get; }
}

class Service(
    [Tag(Abc)] IDependency dependency1,
    [Tag(Xyz)] IDependency dependency2,
    IDependency dependency3)
    : IService
{
    public IDependency Dependency1 { get; } = dependency1;

    public IDependency Dependency2 { get; } = dependency2;

    public IDependency Dependency3 { get; } = dependency3;
}
// }

public class Scenario
{
    [Fact]
    public void Run()
    {
        // Resolve = Off
// {
        DI.Setup(nameof(Composition))
            // The `default` tag is used to resolve dependencies
            // when the tag was not specified by the consumer
            .Bind<IDependency>(Abc, default).To<AbcDependency>()
            .Bind<IDependency>(Xyz).As(Singleton).To<XyzDependency>()
            .Bind<IService>().To<Service>()

            // "XyzRoot" is root name, Xyz is tag
            .Root<IDependency>("XyzRoot", Xyz)

            // Specifies to create the composition root named "Root"
            .Root<IService>("Root");

        var composition = new Composition();
        var service = composition.Root;
        service.Dependency1.ShouldBeOfType<AbcDependency>();
        service.Dependency2.ShouldBeOfType<XyzDependency>();
        service.Dependency2.ShouldBe(composition.XyzRoot);
        service.Dependency3.ShouldBeOfType<AbcDependency>();
// }
        composition.SaveClassDiagram();
    }
}