// ReSharper disable ClassNeverInstantiated.Global
namespace Pure.DI.Core.Code;

internal sealed class FieldsBuilder(ITypeResolver typeResolver)
    : IBuilder<CompositionCode, CompositionCode>
{
    public CompositionCode Build(CompositionCode composition)
    {
        var code = composition.Code;
        var membersCounter = composition.MembersCount;
        var nullable = composition.Source.Source.SemanticModel.Compilation.Options.NullableContextOptions == NullableContextOptions.Disable ? "" : "?";
        
        // _parent filed
        code.AppendLine($"private readonly {composition.Source.Source.Name.ClassName} {Names.RootFieldName};");
        membersCounter++;

        if (composition.IsThreadSafe)
        {
            // _lock field
            code.AppendLine($"private readonly object {Names.LockFieldName};");
            membersCounter++;
        }

        if (composition.TotalDisposablesCount > 0)
        {
            // _disposables field
            code.AppendLine($"private object[] {Names.DisposablesFieldName};");
            membersCounter++;
            
            // _disposeIndex field
            code.AppendLine($"private int {Names.DisposeIndexFieldName};");
            membersCounter++;
        }
        
        // Singleton fields
        foreach (var singletonField in composition.Singletons)
        {
            if (singletonField.InstanceType.IsValueType)
            {
                code.AppendLine($"private {typeResolver.Resolve(singletonField.InstanceType)} {singletonField.VariableDeclarationName};");
                membersCounter++;

                code.AppendLine($"private bool {singletonField.VariableDeclarationName}Created;");
                membersCounter++;
            }
            else
            {
                code.AppendLine($"private {typeResolver.Resolve(singletonField.InstanceType)}{nullable} {singletonField.VariableDeclarationName};");
                membersCounter++;
            }
        }
        
        return composition with { MembersCount = membersCounter };
    }
}