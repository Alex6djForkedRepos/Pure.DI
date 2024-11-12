namespace Pure.DI.Core;

internal class InstanceDpProvider(
    ISemantic semantic,
    IAttributes attributes,
    IWildcardMatcher wildcardMatcher,
    IRegistryManager<MdInjectionSite> registryManager,
    IInjectionSiteFactory injectionSiteFactory) : IInstanceDpProvider
{
    public InstanceDp Get(
        MdSetup setup,
        Compilation compilation,
        INamedTypeSymbol implementationType)
    {
        var setupAttributesBuilder = ImmutableArray.CreateBuilder<IMdAttribute>(
            setup.OrdinalAttributes.Length
            + setup.TagAttributes.Length
            + setup.TypeAttributes.Length);
        setupAttributesBuilder.AddRange(setup.OrdinalAttributes);
        setupAttributesBuilder.AddRange(setup.TagAttributes);
        setupAttributesBuilder.AddRange(setup.TypeAttributes);
        var setupAttributes = setupAttributesBuilder.MoveToImmutable();
        
        var methods = new List<DpMethod>();
        var fields = new List<DpField>();
        var properties = new List<DpProperty>();
        foreach (var member in GetMembers(implementationType))
        {
            if (!semantic.IsAccessible(member))
            {
                continue;
            }

            switch (member)
            {
                case IMethodSymbol method:
                    if (method.MethodKind == MethodKind.Ordinary)
                    {
                        var ordinal = attributes.GetAttribute(setupAttributes, member, default(int?))
                                      ?? method.Parameters
                                          .Select(param => attributes.GetAttribute(setupAttributes, param, default(int?)))
                                          .FirstOrDefault(i => i.HasValue);

                        if (ordinal.HasValue)
                        {
                            methods.Add(new DpMethod(method, ordinal, GetParameters(setup, method.Parameters, compilation, setup.TypeConstructor)));
                        }
                    }

                    break;

                case IFieldSymbol field:
                    if (field is { IsReadOnly: false, IsStatic: false, IsConst: false })
                    {
                        var ordinal = attributes.GetAttribute(setupAttributes, member, default(int?));
                        if (field.IsRequired || ordinal.HasValue)
                        {
                            var type = field.Type.WithNullableAnnotation(NullableAnnotation.NotAnnotated);
                            fields.Add(
                                new DpField(
                                    field,
                                    ordinal,
                                    new Injection(
                                        InjectionKind.Field,
                                        attributes.GetAttribute(setup.TypeAttributes, field, setup.TypeConstructor?.Construct(setup, compilation, type) ?? type),
                                        GetTagAttribute(setup, field))));
                        }
                    }

                    break;

                case IPropertySymbol property:
                    if (property is { IsReadOnly: false, IsStatic: false, IsIndexer: false })
                    {
                        var ordinal = attributes.GetAttribute(setupAttributes, member, default(int?));
                        if (ordinal.HasValue || property.IsRequired)
                        {
                            var type = property.Type.WithNullableAnnotation(NullableAnnotation.NotAnnotated);
                            properties.Add(
                                new DpProperty(
                                    property,
                                    ordinal,
                                    new Injection(
                                        InjectionKind.Property,
                                        attributes.GetAttribute(setup.TypeAttributes, property, setup.TypeConstructor?.Construct(setup, compilation, type) ?? type),
                                        GetTagAttribute(setup, property))));
                        }
                    }

                    break;
            }
        }

        return new InstanceDp(
            methods.ToImmutableArray(),
            fields.ToImmutableArray(),
            properties.ToImmutableArray());
    }
    
    public ImmutableArray<DpParameter> GetParameters(
        in MdSetup setup,
        in ImmutableArray<IParameterSymbol> parameters,
        Compilation compilation,
        ITypeConstructor? typeConstructor)
    {
        if (parameters.Length == 0)
        {
            return ImmutableArray<DpParameter>.Empty;
        }

        var dependenciesBuilder = ImmutableArray.CreateBuilder<DpParameter>(parameters.Length);
        foreach (var parameter in parameters)
        {
            var type = parameter.Type.WithNullableAnnotation(NullableAnnotation.NotAnnotated);
            dependenciesBuilder.Add(
                new DpParameter(
                    parameter,
                    new Injection(
                        InjectionKind.Parameter,
                        attributes.GetAttribute(setup.TypeAttributes, parameter, typeConstructor?.Construct(setup, compilation, type) ?? type),
                        GetTagAttribute(setup, parameter))));
        }

        return dependenciesBuilder.MoveToImmutable();
    }
    
    private static IEnumerable<ISymbol> GetMembers(ITypeSymbol type)
    {
        var members = new List<ISymbol>();
        while (true)
        {
            members.AddRange(type.GetMembers());
            if (type.BaseType is { } baseType)
            {
                type = baseType;
                continue;
            }

            break;
        }

        return members;
    }
    
    private object? GetTagAttribute(
        MdSetup setup,
        ISymbol member) =>
        attributes.GetAttribute(setup.TagAttributes, member, default(object?))
        ?? TryCreateTagOnSite(setup, member);
    
    private object? TryCreateTagOnSite(
        MdSetup setup,
        ISymbol symbol)
    {
        var injectionSite = injectionSiteFactory.CreateInjectionSite(symbol.ContainingSymbol, symbol.Name);
        var injectionSiteSpan = injectionSite.AsSpan();
        foreach (var tagOnSite in setup.TagOn)
        {
            foreach (var site in tagOnSite.InjectionSites)
            {
                if (!wildcardMatcher.Match(site.Site.AsSpan(), injectionSiteSpan))
                {
                    continue;
                }

                registryManager.Register(setup, site);
                return tagOnSite;
            }
        }

        return default;
    }
}