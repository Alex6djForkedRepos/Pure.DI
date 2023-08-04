namespace Pure.DI.Core;

internal sealed class NamespacesSyntaxWalker: CSharpSyntaxWalker, IEnumerable<string>
{
    private readonly SemanticModel _semanticModel;
    private readonly HashSet<string> _namespaces = new();

    public NamespacesSyntaxWalker(SemanticModel semanticModel)
    {
        _semanticModel = semanticModel;
    }

    public override void VisitIdentifierName(IdentifierNameSyntax node)
    {
        AddNamespace(_semanticModel.GetSymbolInfo(node).Symbol);
        base.VisitIdentifierName(node);
    }

    public override void VisitGenericName(GenericNameSyntax node)
    {
        AddNamespace(_semanticModel.GetSymbolInfo(node).Symbol);
        base.VisitGenericName(node);
    }

    private void AddNamespace(ISymbol? symbol)
    {
        var depth = 256;
        while (depth-- > 0)
        {
            switch (symbol)
            {
                case INamedTypeSymbol type when type.ContainingNamespace.IsGlobalNamespace:
                    return;

                case INamedTypeSymbol type:
                {
                    _namespaces.Add(type.ContainingNamespace.ToString());
                    if (!type.IsGenericType)
                    {
                        return;
                    }

                    foreach (var typeSymbol in type.TypeArguments)
                    {
                        AddNamespace(typeSymbol);
                    }

                    break;
                }

                case IMethodSymbol methodSymbol:
                    symbol = methodSymbol.ContainingSymbol;
                    continue;
            }

            break;
        }
    }

    public IEnumerator<string> GetEnumerator() => _namespaces.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}