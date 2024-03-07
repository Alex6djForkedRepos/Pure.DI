#### Abstractions binding

[![CSharp](https://img.shields.io/badge/C%23-code-blue.svg)](../tests/Pure.DI.UsageTests/Basics/AbstractionsBindingScenario.cs)

You can use the `Bind(...)` method without type parameters. In this case binding will be performed for all abstract types implemented directly and for the implementation type itself.

```c#
interface IDependency;

interface IOtherDependency;

class Dependency: IDependency, IOtherDependency;

class Service(
    Dependency dependencyImpl,
    IDependency dependency,
    IOtherDependency otherDependency);

// Specifies to create a partial class "Composition"
DI.Setup("Composition")
    // Begins the definition of the binding for all abstract types
    // that are directly implemented and the implementation type itself.
    .Bind().As(Lifetime.PerBlock).To<Dependency>()
    // Specifies to create a property "MyService"
    .Root<Service>("MyService");
        
var composition = new Composition();
var service = composition.MyService;
```

<details open>
<summary>Class Diagram</summary>

```mermaid
classDiagram
  class Composition {
    +Service MyService
  }
  class Service {
    +Service(Dependency dependencyImpl, IDependency dependency, IOtherDependency otherDependency)
  }
  Dependency --|> IDependency : 
  Dependency --|> IOtherDependency : 
  class Dependency {
    +Dependency()
  }
  class IDependency {
    <<abstract>>
  }
  class IOtherDependency {
    <<abstract>>
  }
  Service o--  "PerBlock" Dependency : Dependency
  Service o--  "PerBlock" Dependency : IDependency
  Service o--  "PerBlock" Dependency : IOtherDependency
  Composition ..> Service : Service MyService
```

</details>

<details>
<summary>Pure.DI-generated partial class Composition</summary><blockquote>

```c#
/// <para>
/// Specifies to create a partial class "Composition"
/// </para>
/// <para>
/// Composition roots:<br/>
/// <list type="table">
/// <listheader>
/// <term>Root</term>
/// <description>Description</description>
/// </listheader>
/// <item>
/// <term>
/// <see cref="Pure.DI.UsageTests.Basics.AbstractionsBindingScenario.Service"/> MyService
/// </term>
/// <description>
/// Specifies to create a property "MyService"
/// </description>
/// </item>
/// </list>
/// </para>
/// <example>
/// This shows how to get an instance of type <see cref="Pure.DI.UsageTests.Basics.AbstractionsBindingScenario.Service"/> using the composition root <see cref="MyService"/>:
/// <code>
/// var composition = new Composition();
/// var instance = composition.MyService;
/// </code>
/// </example>
/// <a href="https://mermaid.live/view#pako:eNqlU8GOwiAQ_RUyZw9GD3W9bVsPPRg38cqFhclKLKWh3U2M8d-lYFdKMDF6mQx9M483j84ZuBYIa-A167pSsh_DFDW0cWdSaNXqTvZSN4T-zudZPmBDtsj3aP4kR7I93bIRyso7wVj0oNkdViW22Ahs-ImI_7RSbe3gglTJghHc9Qc0QYWenl3Zx1RbUO3g8h6zYojLzeRS9-mTPN0aSwr6vS0RFjoTKV-l9HuS6iHL0isp2HfXG8Z7r-sWU0wpwa_Qjc-tQ28IofCFJq81P1KIZ7e-BCO_zBGY8QZJ5MPAFK_Awg3uo33r8Ae3DMmlgBkoNIpJYRftTMFeopDCmoJgxiq5wOUKLcMyAw">Class diagram</a><br/>
/// This class was created by <a href="https://github.com/DevTeam/Pure.DI">Pure.DI</a> source code generator.
/// </summary>
/// <seealso cref="Pure.DI.DI.Setup"/>
[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
partial class Composition
{
  private readonly Composition _rootM03D07di;
  
  /// <summary>
  /// This constructor creates a new instance of <see cref="Composition"/>.
  /// </summary>
  public Composition()
  {
    _rootM03D07di = this;
  }
  
  /// <summary>
  /// This constructor creates a new instance of <see cref="Composition"/> scope based on <paramref name="baseComposition"/>. This allows the <see cref="Lifetime.Scoped"/> life time to be applied.
  /// </summary>
  /// <param name="baseComposition">Base composition.</param>
  internal Composition(Composition baseComposition)
  {
    _rootM03D07di = baseComposition._rootM03D07di;
  }
  
  #region Composition Roots
  /// <summary>
  /// Specifies to create a property "MyService"
  /// </summary>
  public Pure.DI.UsageTests.Basics.AbstractionsBindingScenario.Service MyService
  {
    #if NETSTANDARD2_0_OR_GREATER || NETCOREAPP || NET40_OR_GREATER || NET
    [global::System.Diagnostics.Contracts.Pure]
    #endif
    get
    {
      var perBlockM03D07di1_Dependency = new Pure.DI.UsageTests.Basics.AbstractionsBindingScenario.Dependency();
      return new Pure.DI.UsageTests.Basics.AbstractionsBindingScenario.Service(perBlockM03D07di1_Dependency, perBlockM03D07di1_Dependency, perBlockM03D07di1_Dependency);
    }
  }
  #endregion
  
  /// <summary>
  /// This method provides a class diagram in mermaid format. To see this diagram, simply call the method and copy the text to this site https://mermaid.live/.
  /// </summary>
  public override string ToString()
  {
    return
      "classDiagram\n" +
        "  class Composition {\n" +
          "    +Service MyService\n" +
        "  }\n" +
        "  class Service {\n" +
          "    +Service(Dependency dependencyImpl, IDependency dependency, IOtherDependency otherDependency)\n" +
        "  }\n" +
        "  Dependency --|> IDependency : \n" +
        "  Dependency --|> IOtherDependency : \n" +
        "  class Dependency {\n" +
          "    +Dependency()\n" +
        "  }\n" +
        "  class IDependency {\n" +
          "    <<abstract>>\n" +
        "  }\n" +
        "  class IOtherDependency {\n" +
          "    <<abstract>>\n" +
        "  }\n" +
        "  Service o--  \"PerBlock\" Dependency : Dependency\n" +
        "  Service o--  \"PerBlock\" Dependency : IDependency\n" +
        "  Service o--  \"PerBlock\" Dependency : IOtherDependency\n" +
        "  Composition ..> Service : Service MyService";
  }
}
```

</blockquote></details>
