﻿namespace Pure.DI.IntegrationTests;

[Collection(nameof(NonParallelTestsCollectionDefinition))]
public class CtorTests
{
    [Fact]
    public async Task ShouldSelectValidCtor()
    {
        // Given

        // When
        var result = await """
using System;
using Pure.DI;

namespace Sample
{
    interface IBox<out T> { T Content { get; } }

    interface ICat { }

    class CardboardBox<T> : IBox<T>
    {
        public CardboardBox(T content) => Content = content;

        public T Content { get; }

        public override string ToString() => $"[{ Content}]";
    }

    class ShroedingersCat : ICat
    {
        public ShroedingersCat(int id) { }

        internal ShroedingersCat() { }        
    }

    static class Setup
    {
        private static void SetupComposition()
        {
            DI.Setup("Composition")
                .Bind<ICat>().To<ShroedingersCat>()
                .Bind<IBox<TT>>().To<CardboardBox<TT>>()                
                .Root<Program>("Root");
        }
    }

    public class Program
    {
        IBox<ICat> _box;

        internal Program(IBox<ICat> box) => _box = box;

        private void Run() => Console.WriteLine(_box);

        public static void Main()
        {
            var composition = new Composition();
            composition.Root.Run();
        }
    }                
}
""".RunAsync();

        // Then
        result.Success.ShouldBeTrue(result.GeneratedCode);
    }
}