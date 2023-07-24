// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core.CSharp;

internal class ClassBuilder : IBuilder<CompositionCode, CompositionCode>
{
    
    private readonly IBuilder<CompositionCode, CompositionCode> _usingDeclarationsBuilder;
    private readonly IInformation _information;
    private readonly ImmutableArray<IBuilder<CompositionCode, CompositionCode>> _codeBuilders;
    
    public ClassBuilder(
        [Tag(WellknownTag.CSharpUsingDeclarationsBuilder)] IBuilder<CompositionCode, CompositionCode> usingDeclarationsBuilder,
        [Tag(WellknownTag.CSharpSingletonFieldsBuilder)] IBuilder<CompositionCode, CompositionCode> singletonFieldsBuilder,
        [Tag(WellknownTag.CSharpArgFieldsBuilder)] IBuilder<CompositionCode, CompositionCode> argFieldsBuilder,
        [Tag(WellknownTag.CSharpPrimaryConstructorBuilder)] IBuilder<CompositionCode, CompositionCode> primaryConstructorBuilder,
        [Tag(WellknownTag.CSharpDefaultConstructorBuilder)] IBuilder<CompositionCode, CompositionCode> defaultConstructorBuilder,
        [Tag(WellknownTag.CSharpChildConstructorBuilder)] IBuilder<CompositionCode, CompositionCode> childConstructorBuilder,
        [Tag(WellknownTag.CSharpRootPropertiesBuilder)] IBuilder<CompositionCode, CompositionCode> rootPropertiesBuilder,
        [Tag(WellknownTag.CSharpApiMembersBuilder)] IBuilder<CompositionCode, CompositionCode> apiMembersBuilder,
        [Tag(WellknownTag.CSharpDisposeMethodBuilder)] IBuilder<CompositionCode, CompositionCode> disposeMethodBuilder,
        [Tag(WellknownTag.CSharpToStringBuilder)] IBuilder<CompositionCode, CompositionCode> toStringBuilder,
        [Tag(WellknownTag.CSharpResolversFieldsBuilder)] IBuilder<CompositionCode, CompositionCode> resolversFieldsBuilder,
        [Tag(WellknownTag.CSharpStaticConstructorBuilder)] IBuilder<CompositionCode, CompositionCode> staticConstructorBuilder,
        [Tag(WellknownTag.CSharpResolverClassesBuilder)] IBuilder<CompositionCode, CompositionCode> resolversClassesBuilder,
        IInformation information)
    {
        _usingDeclarationsBuilder = usingDeclarationsBuilder;
        _information = information;
        _codeBuilders = ImmutableArray.Create(
            singletonFieldsBuilder,
            argFieldsBuilder,
            primaryConstructorBuilder,
            defaultConstructorBuilder,
            childConstructorBuilder,
            rootPropertiesBuilder,
            apiMembersBuilder,
            disposeMethodBuilder,
            toStringBuilder,
            resolversFieldsBuilder,
            staticConstructorBuilder,
            resolversClassesBuilder);
    }

    public CompositionCode Build(CompositionCode composition, CancellationToken cancellationToken)
    {
        var code = composition.Code;
        code.AppendLine("// <auto-generated/>");
        code.AppendLine($"// by {_information.Description}");
        code.AppendLine("#nullable enable");
        code.AppendLine("#pragma warning disable");
        code.AppendLine();

        composition = _usingDeclarationsBuilder.Build(composition, cancellationToken);
        
        var nsIndent = Disposables.Empty;
        if (!string.IsNullOrWhiteSpace(composition.Name.Namespace))
        {
            code.AppendLine($"namespace {composition.Name.Namespace}");
            code.AppendLine("{");
            nsIndent = code.Indent();
        }

        code.AppendLine($"[{Constant.SystemNamespace}Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]");
        var implementingInterfaces = composition.DisposableSingletonsCount > 0 ? $": {Constant.IDisposableInterfaceName}" : "";
        code.AppendLine($"partial class {composition.Name.ClassName}{implementingInterfaces}");
        code.AppendLine("{");

        using (code.Indent())
        {
            // Generate class members
            foreach (var builder in _codeBuilders)
            {
                cancellationToken.ThrowIfCancellationRequested();
                composition = builder.Build(composition, cancellationToken);
            }
        }

        code.AppendLine("}");

        // ReSharper disable once InvertIf
        if (!string.IsNullOrWhiteSpace(composition.Name.Namespace))
        {
            // ReSharper disable once RedundantAssignment
            nsIndent.Dispose();
            code.AppendLine("}");
        }
        
        code.AppendLine("#pragma warning restore");
        return composition;
    }
}