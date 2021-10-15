﻿namespace Pure.DI.Core
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    // ReSharper disable once ClassNeverInstantiated.Global
    internal class AutowiringObjectBuilder : IObjectBuilder
    {
        private const string InstanceVarName = "instance";
        private readonly IDiagnostic _diagnostic;
        private readonly IBuildContext _buildContext;
        private readonly IAttributesService _attributesService;
        private readonly ITracer _tracer;

        public AutowiringObjectBuilder(
            IDiagnostic diagnostic,
            IBuildContext buildContext, 
            IAttributesService attributesService,
            ITracer tracer)
        {
            _diagnostic = diagnostic;
            _buildContext = buildContext;
            _attributesService = attributesService;
            _tracer = tracer;
        }

        [SuppressMessage("ReSharper", "InvertIf")]
        public ExpressionSyntax? TryBuild(IBuildStrategy buildStrategy, Dependency dependency)
        {
            var objectCreationExpression = (
                from ctor in 
                    dependency.Implementation.Type is INamedTypeSymbol implementationType
                        ? implementationType.Constructors
                        : Enumerable.Empty<IMethodSymbol>()
                where ctor.DeclaredAccessibility is Accessibility.Internal or Accessibility.Public or Accessibility.Friend
                let order = GetOrder(ctor) ?? int.MaxValue
                let parameters = ResolveMethodParameters(_buildContext.TypeResolver, buildStrategy, dependency, ctor, true).ToList()
                where parameters.All(i => i.Result != default)
                let paramsResolvedCount = parameters.Count(i => i.DefaultType == DefaultType.ResolvedValue)
                let paramsResolveWeight = parameters.Sum(i => (int)i.ResolveType)
                let paramsTypeWeight = parameters.Sum(i => (int)i.DefaultType)
                let arguments = SyntaxFactory.SeparatedList(
                    from parameter in parameters
                    select SyntaxFactory.Argument(parameter.Result!))
                let expression = CreateObject(ctor, dependency, arguments)
                select (ctor, expression, order, paramsResolvedCount, paramsResolveWeight, paramsTypeWeight))
                // By an Order attribute
                .OrderBy(i => i.order)
                // By a number of resolved parameters
                .ThenByDescending(i => i.paramsResolvedCount)
                // By an access modifier
                .ThenByDescending(i => i.ctor.DeclaredAccessibility)
                // By resolving type
                .ThenByDescending(i => i.paramsResolveWeight)
                // By param type
                .ThenByDescending(i => i.paramsTypeWeight)
                .FirstOrDefault()
                .expression;
            
            if (objectCreationExpression == default)
            {
                _tracer.Save();
                return default;
            }

            var members = (
                from member in dependency.Implementation.Type.GetMembers()
                where member.MetadataName != ".ctor"
                let order = GetOrder(member)
                where order != default
                orderby order
                select member)
                .Distinct(SymbolEqualityComparer.Default)
                .ToList();

            if (members.Any())
            {
                List<StatementSyntax> factoryStatements = new();
                foreach (var member in members)
                {
                    if (member.IsStatic || member.DeclaredAccessibility != Accessibility.Public && member.DeclaredAccessibility != Accessibility.Internal)
                    {
                        var error = $"{member} is inaccessible in {dependency.Implementation}.";
                        _diagnostic.Error(Diagnostics.Error.MemberIsInaccessible, error, member.Locations.FirstOrDefault());
                        throw new HandledException(error);
                    }

                    switch (member)
                    {
                        case IMethodSymbol method:
                            var arguments =
                                from parameter in ResolveMethodParameters(_buildContext.TypeResolver, buildStrategy, dependency, method, false)
                                where parameter.Result != default
                                select SyntaxFactory.Argument(parameter.Result!);

                            var call = SyntaxFactory.InvocationExpression(
                                SyntaxFactory.MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    SyntaxFactory.ParseName(InstanceVarName),
                                    SyntaxFactory.Token(SyntaxKind.DotToken),
                                    SyntaxFactory.IdentifierName(method.Name)))
                                .AddArgumentListArguments(arguments.ToArray());

                            factoryStatements.Add(SyntaxFactory.ExpressionStatement(call));
                            break;

                        case IFieldSymbol filed:
                            if (!filed.IsReadOnly && !filed.IsStatic && !filed.IsConst)
                            {
                                var fieldValue = ResolveInstance(filed, dependency, filed.Type, _buildContext.TypeResolver, buildStrategy, filed.Locations, false);
                                var assignmentExpression = SyntaxFactory.AssignmentExpression(
                                    SyntaxKind.SimpleAssignmentExpression,
                                    SyntaxFactory.MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        SyntaxFactory.IdentifierName(InstanceVarName),
                                        SyntaxFactory.Token(SyntaxKind.DotToken),
                                        SyntaxFactory.IdentifierName(filed.Name)),
                                    fieldValue.Result!);

                                factoryStatements.Add(SyntaxFactory.ExpressionStatement(assignmentExpression));
                            }

                            break;

                        case IPropertySymbol property:
                            if (property.SetMethod != null && !property.IsReadOnly && !property.IsStatic)
                            {
                                var propertyValue = ResolveInstance(property, dependency, property.Type, _buildContext.TypeResolver, buildStrategy, property.Locations, false);
                                var assignmentExpression = SyntaxFactory.AssignmentExpression(
                                    SyntaxKind.SimpleAssignmentExpression,
                                    SyntaxFactory.MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        SyntaxFactory.IdentifierName(InstanceVarName),
                                        SyntaxFactory.Token(SyntaxKind.DotToken),
                                        SyntaxFactory.IdentifierName(property.Name)),
                                    propertyValue.Result!);

                                factoryStatements.Add(SyntaxFactory.ExpressionStatement(assignmentExpression));
                            }

                            break;
                    }
                }

                if (factoryStatements.Any())
                {
                    var memberKey = new MemberKey($"Create{dependency.Binding.Implementation}", dependency);
                    var factoryName = _buildContext.NameService.FindName(memberKey);
                    var type = dependency.Implementation.TypeSyntax;
                    var factoryMethod = SyntaxFactory.MethodDeclaration(type, SyntaxFactory.Identifier(factoryName))
                        .AddAttributeLists(SyntaxFactory.AttributeList().AddAttributes(SyntaxRepo.AggressiveInliningAttr))
                        .AddModifiers(SyntaxFactory.Token(SyntaxKind.PrivateKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword))
                        .AddBodyStatements(
                            SyntaxFactory.LocalDeclarationStatement(
                                SyntaxFactory.VariableDeclaration(type)
                                    .AddVariables(
                                        SyntaxFactory.VariableDeclarator(InstanceVarName)
                                            .WithInitializer(SyntaxFactory.EqualsValueClause(objectCreationExpression)))))
                        .AddBodyStatements(factoryStatements.ToArray())
                        .AddBodyStatements(
                            SyntaxFactory.ReturnStatement(
                                SyntaxFactory.IdentifierName(InstanceVarName)));

                    _buildContext.GetOrAddMember(memberKey, () => factoryMethod);
                    return SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName(factoryName))
                        .WithCommentBefore($"// {dependency.Binding}");
                }
            }

            return objectCreationExpression
                .WithCommentBefore($"// {dependency.Binding}");
        }

        private ResolveResult ResolveInstance(
            ISymbol target,
            Dependency targetDependency,
            ITypeSymbol defaultType,
            ITypeResolver typeResolver,
            IBuildStrategy buildStrategy,
            ImmutableArray<Location> resolveLocations,
            bool probe)
        {
            ExpressionSyntax? defaultValue = null;
            var defaultResolveType = DefaultType.ResolvedValue;
            if (target is IParameterSymbol parameter)
            {
                if (parameter.HasExplicitDefaultValue)
                {
                    defaultValue = parameter.ExplicitDefaultValue.ToLiteralExpression();
                    defaultResolveType = DefaultType.DefaultValue;
                }
                else
                {
                    if (parameter.Type.NullableAnnotation == NullableAnnotation.Annotated)
                    {
                        defaultValue = SyntaxFactory.DefaultExpression(new SemanticType(parameter.Type, targetDependency.Implementation).TypeSyntax);
                        defaultResolveType = DefaultType.NullableValue;
                    }
                }
            }
            
            var resolvingType = GetDependencyType(target, targetDependency.Implementation) ?? new SemanticType(defaultType, targetDependency.Implementation);
            var tag = (ExpressionSyntax?) _attributesService.GetAttributeArgumentExpressions(AttributeKind.Tag, target).FirstOrDefault();
            var dependency = typeResolver.Resolve(resolvingType, tag);
            if (!dependency.IsResolved && defaultValue != null)
            {
                return new ResolveResult(defaultValue, ResolveType.ResolvedUnbound, defaultResolveType);
            }

            switch (dependency.Implementation.Type)
            {
                case INamedTypeSymbol namedType:
                {
                    var type = new SemanticType(namedType, targetDependency.Implementation);
                    var constructedType = dependency.TypesMap.ConstructType(type);
                    if (!dependency.Implementation.Equals(constructedType))
                    {
                        _buildContext.AddBinding(new BindingMetadata(dependency.Binding, constructedType, null));
                    }

                    if (dependency.IsResolved)
                    {
                        return new ResolveResult(buildStrategy.TryBuild(dependency, resolvingType), ResolveType.Resolved, DefaultType.ResolvedValue);
                    }

                    if (probe)
                    {
                        return ResolveResult.NotResolved;
                    }

                    var dependencyType = dependency.Implementation.TypeSyntax;
                    return new ResolveResult(SyntaxFactory.CastExpression(dependencyType,
                        SyntaxFactory.InvocationExpression(SyntaxFactory.ParseName(nameof(IContext.Resolve)))
                            .AddArgumentListArguments(SyntaxFactory.Argument(SyntaxFactory.TypeOfExpression(dependencyType)))), ResolveType.ResolvedUnbound, DefaultType.ResolvedValue);
                }

                case IArrayTypeSymbol arrayType:
                {
                    var type = new SemanticType(arrayType, targetDependency.Implementation);
                    var arrayTypeDescription = typeResolver.Resolve(type, null);
                    if (arrayTypeDescription.IsResolved)
                    {
                        return new ResolveResult(buildStrategy.TryBuild(arrayTypeDescription, type), ResolveType.Resolved, DefaultType.ResolvedValue);
                    }

                    break;
                }
            }
            
            if (buildStrategy.TryBuild(dependency, resolvingType) == null)
            {
                return ResolveResult.NotResolved;
            }

            var error = $"Unsupported type {dependency.Implementation}.";
            _diagnostic.Error(Diagnostics.Error.Unsupported, error, resolveLocations.FirstOrDefault());
            throw new HandledException(error);
        }

        private IEnumerable<ResolveResult> ResolveMethodParameters(ITypeResolver typeResolver, IBuildStrategy buildStrategy, Dependency dependency, IMethodSymbol method, bool probe) =>
            from parameter in method.Parameters
            select ResolveInstance(parameter, dependency, parameter.Type, typeResolver, buildStrategy, parameter.Locations, probe);

        private SemanticType? GetDependencyType(ISymbol type, SemanticModel semanticModel) =>
        (
            from expression in _attributesService.GetAttributeArgumentExpressions(AttributeKind.Type, type)
            let typeExpression = (expression as TypeOfExpressionSyntax)?.Type
            where typeExpression != null
            let typeSemanticModel = typeExpression.GetSemanticModel(semanticModel)
            let typeSymbol = typeSemanticModel.GetTypeInfo(typeExpression).Type
            where typeSymbol != null
            select new SemanticType(typeSymbol, typeSemanticModel)).FirstOrDefault();

        // ReSharper disable once SuggestBaseTypeForParameter
        private ExpressionSyntax CreateObject(IMethodSymbol ctor, Dependency dependency, SeparatedSyntaxList<ArgumentSyntax> arguments)
        {
            var typeSyntax = dependency.Implementation.TypeSyntax;
            if (typeSyntax.IsKind(SyntaxKind.TupleType))
            {
                return SyntaxFactory.TupleExpression()
                    .WithArguments(arguments);
            }

            if (ctor.GetAttributes(typeof(ObsoleteAttribute), dependency.Implementation.SemanticModel).Any())
            {
                _diagnostic.Warning(Diagnostics.Warning.CtorIsObsoleted, $"The constructor {ctor} marked as obsoleted is used.", ctor.Locations.FirstOrDefault());
            }

            return SyntaxFactory.ObjectCreationExpression(typeSyntax)
                .WithArgumentList(SyntaxFactory.ArgumentList(arguments));
        }
        
        private IComparable? GetOrder(ISymbol method)
        {
            var orders =
                (from expression in _attributesService.GetAttributeArgumentExpressions(AttributeKind.Order, method)
                    let order = ((expression as LiteralExpressionSyntax)?.Token)?.Value as IComparable
                    select order).ToList();

            return orders.Any() ? orders.Max() : default;
        }
        
        private readonly struct ResolveResult
        {
            public readonly ExpressionSyntax? Result;
            public readonly ResolveType ResolveType;
            public readonly DefaultType DefaultType;
            public static readonly ResolveResult NotResolved = new(null, ResolveType.NotResolved, DefaultType.DefaultValue);

            public ResolveResult(ExpressionSyntax? result, ResolveType resolveType, DefaultType defaultType)
            {
                Result = result;
                ResolveType = resolveType;
                DefaultType = defaultType;
            }
        }

        private enum ResolveType
        {
            NotResolved = 0,
            ResolvedUnbound = 1,
            Resolved = 2
        }

        private enum DefaultType
        {
            NullableValue = 0,
            DefaultValue = 1,
            ResolvedValue = 2
        }
    }
}