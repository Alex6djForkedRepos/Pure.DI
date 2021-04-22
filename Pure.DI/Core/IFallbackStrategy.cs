﻿namespace Pure.DI.Core
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal interface IFallbackStrategy
    {
        ExpressionSyntax Build(SemanticModel semanticModel);
    }
}