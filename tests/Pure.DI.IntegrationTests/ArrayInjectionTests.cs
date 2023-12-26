﻿namespace Pure.DI.IntegrationTests;

[Collection(nameof(IntegrationTestsCollectionDefinition))]
public class ArrayInjectionTests
{
    [Fact]
    public async Task ShouldSupportArrayInjection()
    {
        // Given

        // When
        var result = await """
using System;
using Pure.DI;

namespace Sample
{
    interface IDependency {}

    class Dependency: IDependency
    {        
        public Dependency()
        {
            Console.WriteLine("Dependency created");
        }
    }

    interface IService
    {                    
    }

    class Service: IService 
    {
        public Service(IDependency[] deps)
        { 
            Console.WriteLine("Service creating");            
        }                            
    }

    static class Setup
    {
        private static void SetupComposition()
        {
            DI.Setup("Composition")
                .Bind<IDependency>(1).To<Dependency>()
                .Bind<IDependency>(2).To<Dependency>()
                .Bind<IDependency>(3).To<Dependency>()
                .Bind<IService>().To<Service>()
                .Root<IService>("Service");
        }
    }

    public class Program
    {
        public static void Main()
        {
            var composition = new Composition();
            var service = composition.Service;                                                     
        }
    }                
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(ImmutableArray.Create("Dependency created", "Dependency created", "Dependency created", "Service creating"), result);
    }
    
    [Fact]
    public async Task ShouldOverrideDefaultArrayInjection()
    {
        // Given

        // When
        var result = await """
using System;
using Pure.DI;
using System.Collections.Generic;

namespace Sample
{
    interface IDependency {}

    class Dependency: IDependency
    {        
        public Dependency()
        {
            Console.WriteLine("Dependency created");
        }
    }

    interface IService
    {                    
    }

    class Service: IService 
    {
        public Service(IDependency[] deps)
        { 
            Console.WriteLine("Service creating");            
        }                            
    }

    static class Setup
    {
        private static void SetupComposition()
        {
            DI.Setup("Composition")
                .Bind<IDependency>(1).To<Dependency>()
                .Bind<IDependency>(2).To<Dependency>()
                .Bind<IDependency>(3).To<Dependency>()
                .Bind<IService>().To<Service>()
                .Bind<IDependency[]>().To(ctx => 
                {
                    ctx.Inject<IDependency>(1, out var dep1);
                    ctx.Inject<IDependency>(3, out var dep2);
                    return new IDependency[] { dep1, dep2 };
                })
                .Root<IService>("Service");
        }
    }

    public class Program
    {
        public static void Main()
        {
            var composition = new Composition();
            var service = composition.Service;                                                     
        }
    }                
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result);
        result.StdOut.ShouldBe(ImmutableArray.Create("Dependency created", "Dependency created", "Service creating"), result);
    }
}