# Architecture

This document describes the intended architecture for LegacyWcf.Configuration.

The architecture should stay simple enough for an MVP, while protecting the most important design goal: the library must be a trusted reader for legacy WCF `<system.serviceModel>` configuration.

## Goals

LegacyWcf.Configuration should allow a developer to point at a legacy config file and retrieve WCF configuration values through a modern .NET API.

The library should:

- load `app.config`, `web.config`, or external `.config` files
- locate `<system.serviceModel>`
- preserve the full XML tree under `<system.serviceModel>`
- preserve unknown elements and attributes
- expose common WCF concepts through typed models
- keep raw XML available as a fallback
- provide useful diagnostics
- avoid a dependency on CoreWCF in the core package

## Non-goals

The core package should not initially:

- host WCF services
- replace WCF
- automatically migrate WCF services to CoreWCF
- generate `Program.cs` or `Startup.cs`
- require the legacy application to build
- require the legacy WCF runtime to be available
- depend on CoreWCF
- perform strict schema enforcement by default

## Design principle: Reading, Understanding, Applying

The project separates three concerns:

```text
Reading
Understanding
Applying
```

### Reading

Read XML and preserve what exists.

This includes known and unknown elements.

### Understanding

Build typed models for common WCF concepts such as services, endpoints, bindings, behaviours, hosts, client endpoints, and service hosting environment.

### Applying

Applying values to CoreWCF or generating migration code belongs in optional future packages.

The core package is responsible only for reading and understanding.

## Package boundaries

The initial package is:

```text
LegacyWcf.Configuration
```

It should contain:

- raw XML reader
- raw model
- typed model
- parsers from raw model to typed model
- diagnostics
- retrieval APIs

It should not depend on CoreWCF.

Potential future packages:

```text
LegacyWcf.Configuration.CoreWcf
LegacyWcf.Configuration.CodeGeneration
```

`LegacyWcf.Configuration.CoreWcf` may depend on CoreWCF and map the neutral model to CoreWCF concepts.

`LegacyWcf.Configuration.CodeGeneration` may generate suggested startup/configuration snippets.

## Target frameworks

The core package should target:

```xml
<TargetFrameworks>netstandard2.0;net8.0</TargetFrameworks>
```

Rationale:

- `netstandard2.0` allows consumption from .NET Framework 4.8 applications.
- `net8.0` gives modern .NET applications a native target.
- The core reader should remain lightweight and broadly usable.

Implementation should avoid APIs unavailable to `netstandard2.0` unless conditional compilation is used.

## Source organisation

For the current stage of the project, public API files live directly under:

```text
src/LegacyWcf.Configuration/
```

Implementation-only helpers live under `Internal/`.

Current source layout:

```text
src/
└── LegacyWcf.Configuration/
    ├── LegacyWcf.Configuration.csproj
    ├── LegacyWcfConfiguration.cs
    ├── LegacyWcfConfigurationReader.cs
    ├── LegacyWcfConfigurationReadResult.cs
    ├── LegacyWcfDiagnostic.cs
    ├── LegacyWcfDiagnosticSeverity.cs
    ├── LegacyWcfElement.cs
    ├── LegacyWcfHost.cs
    ├── LegacyWcfHostTimeouts.cs
    ├── LegacyWcfBinding.cs
    ├── LegacyWcfBindingCollection.cs
    ├── LegacyWcfBindings.cs
    ├── LegacyWcfBehavior.cs
    ├── LegacyWcfBehaviorCollection.cs
    ├── LegacyWcfBehaviors.cs
    ├── LegacyWcfService.cs
    ├── LegacyWcfServiceEndpoint.cs
    ├── LegacyWcfServiceEndpoints.cs
    ├── LegacyWcfServices.cs
    └── Internal/
        ├── LegacyWcfRawElementBuilder.cs
        └── LegacyWcfTypedModelBuilder.cs
```

The current convention is:

```text
Top-level project folder = public API
Internal/                = implementation detail
```

Public API types use the root namespace:

```csharp
namespace LegacyWcf.Configuration;
```

Internal implementation-only types use:

```csharp
namespace LegacyWcf.Configuration.Internal;
```

Do not create folders such as `Reading/`, `Raw/`, `Model/`, or `Diagnostics/` just to group public API types. Folders should be introduced only when they communicate useful structure and reduce friction.

This keeps the maintainer mental model aligned with the consumer mental model: the public API is easy to find at the top level, while implementation details are clearly separated.

## Project file and code style settings

The project should enable nullable reference types at project level:

```xml
<Nullable>enable</Nullable>
```

Do not add `#nullable enable` at the top of every `.cs` file.

The project should disable implicit usings:

```xml
<ImplicitUsings>disable</ImplicitUsings>
```

Use explicit `using` directives in each file for namespaces the file actually depends on. For example, if a file uses `IReadOnlyList<T>`, `IReadOnlyDictionary<TKey, TValue>`, `Dictionary<TKey, TValue>`, or `List<T>`, it should include:

```csharp
using System.Collections.Generic;
```

The project should use an explicit modern language version:

```xml
<LangVersion>latest</LangVersion>
```

These conventions keep the code self-contained and Rider-friendly, especially while the library multi-targets `netstandard2.0;net8.0`.

## Phase 1 implementation slice

The first implementation slice is the full-fidelity raw reader.

Phase 1 has been implemented with only the minimum public API and model needed to load and preserve `<system.serviceModel>`:

```text
src/LegacyWcf.Configuration/
├── LegacyWcfConfigurationReader.cs
├── LegacyWcfConfigurationReadResult.cs
├── LegacyWcfConfiguration.cs
├── LegacyWcfElement.cs
├── LegacyWcfDiagnostic.cs
├── LegacyWcfDiagnosticSeverity.cs
└── Internal/
    └── LegacyWcfRawElementBuilder.cs
```

The Phase 1 public API should stay simple:

```csharp
var result = LegacyWcfConfigurationReader.Read("web.config");

if (result.Success)
{
    var raw = result.Configuration!.RawSystemServiceModel;
}
```

Phase 1 should not implement typed `LegacyWcfService`, typed `LegacyWcfEndpoint`, typed bindings, typed behaviours, lookup APIs, CoreWCF mapping, code generation, or CLI tooling. Those are later phases.

### Phase 1 read result

The read result should contain:

- `Success`
- `Configuration`
- `Diagnostics`

A missing file, unreadable file, malformed XML document, missing `<configuration>` root, or missing `<system.serviceModel>` section should return a result with diagnostics rather than throwing as part of normal control flow.

### Phase 1 raw model contract

The raw model should preserve:

- element name
- full element path
- attributes
- child elements
- text value where relevant
- raw XML
- source file path
- line number where practical
- whether the element is known or unknown

Unknown elements and attributes must be preserved even if `IsKnownElement` is conservative in the first implementation.

## Public API shape

The main entry point should be simple:

```csharp
var result = LegacyWcfConfigurationReader.Read("web.config");
```

The read result should contain:

- success/failure state
- typed configuration when available
- diagnostics

A possible shape:

```csharp
public sealed class LegacyWcfConfigurationReadResult
{
    public bool Success { get; init; }
    public LegacyWcfConfiguration? Configuration { get; init; }
    public IReadOnlyList<LegacyWcfDiagnostic> Diagnostics { get; init; } = [];
}
```

The exact type names may evolve, but the API should stay easy to discover.

## Raw model

The raw model preserves the full XML tree under `<system.serviceModel>`.

It should preserve:

- element name
- full path
- attributes
- child elements
- text value
- raw XML
- source file path
- line number if practical
- whether the element is known or unknown

Possible shape:

```csharp
public sealed class LegacyWcfElement
{
    public required string Name { get; init; }
    public required string Path { get; init; }

    public IReadOnlyDictionary<string, string> Attributes { get; init; }
        = new Dictionary<string, string>();

    public IReadOnlyList<LegacyWcfElement> Children { get; init; } = [];
    public string? Value { get; init; }
    public string? RawXml { get; init; }
    public string? SourceFilePath { get; init; }
    public int? LineNumber { get; init; }
    public bool IsKnownElement { get; init; }
}
```

The exact implementation can change, but the preservation contract should remain.

## Typed model

The typed model sits on top of the raw model.

It should expose high-value WCF concepts while retaining raw XML fallback.

Possible shape:

```csharp
public sealed class LegacyWcfConfiguration
{
    public required LegacyWcfServices Services { get; init; }
    public required LegacyWcfBindings Bindings { get; init; }
    public required LegacyWcfBehaviors Behaviors { get; init; }

    public LegacyWcfClient? Client { get; init; }
    public LegacyWcfServiceHostingEnvironment? ServiceHostingEnvironment { get; init; }

    public required LegacyWcfElement RawSystemServiceModel { get; init; }
    public IReadOnlyList<LegacyWcfDiagnostic> Diagnostics { get; init; } = [];
}
```

### Phase 2 Stage 1 typed model boundary

The first typed-model slice should be deliberately small and additive. It should add typed services and typed service endpoints only, while keeping the raw model as the source of truth.

Stage 1 public API should include:

```text
LegacyWcfService
LegacyWcfServiceEndpoint
LegacyWcfServices
LegacyWcfServiceEndpoints
LegacyWcfConfiguration.Services
```

`LegacyWcfServices` and `LegacyWcfServiceEndpoints` should be typed enumerable collections with `Count`, indexer support, `foreach`, and LINQ support through `IEnumerable`/`IReadOnlyList`.

Stage 1 did not add host models, host base addresses, bindings, behaviours, client endpoints, lookup helpers, validation diagnostics, CoreWCF mapping, code generation, or CLI tooling.

Typed parsing should be built from `LegacyWcfElement` rather than directly from `XDocument` or `XElement`. The recommended internal helper is:

```text
src/LegacyWcf.Configuration/Internal/LegacyWcfTypedModelBuilder.cs
```

The reader flow becomes:

```text
File path
  -> load XML document
  -> locate <configuration>
  -> locate <system.serviceModel>
  -> build full raw LegacyWcfElement tree
  -> build typed services and endpoints from the raw tree
  -> return LegacyWcfConfiguration with RawSystemServiceModel and Services
```

Every typed object should expose its raw element:

```csharp
public sealed class LegacyWcfService
{
    public required string Name { get; init; }
    public string? BehaviorConfiguration { get; init; }
    public LegacyWcfHost? Host { get; init; }
    public IReadOnlyList<LegacyWcfEndpoint> Endpoints { get; init; } = [];
    public required LegacyWcfElement RawElement { get; init; }
}
```


### Phase 2 Stage 2 typed model boundary

The second typed-model slice is implemented and adds service host support on top of the Stage 1 service model.

Stage 2 public API includes:

```text
LegacyWcfHost
LegacyWcfHostTimeouts
LegacyWcfService.Host
```

`LegacyWcfHost` exposes typed host base addresses, optional typed host timeout settings, and the raw `<host>` element:

```csharp
public sealed class LegacyWcfHost
{
    public IReadOnlyList<string> BaseAddresses { get; }
    public LegacyWcfHostTimeouts? Timeouts { get; }
    public LegacyWcfElement RawElement { get; }
}
```

`LegacyWcfHostTimeouts` keeps timeout values as strings and exposes the raw `<timeouts>` element:

```csharp
public sealed class LegacyWcfHostTimeouts
{
    public string? CloseTimeout { get; }
    public string? OpenTimeout { get; }
    public LegacyWcfElement RawElement { get; }
}
```

Stage 2 parsing is built from `LegacyWcfElement` in `LegacyWcfTypedModelBuilder`. It reads direct service `<host>` children, reads `<baseAddresses>/<add baseAddress="..." />` values in source order, ignores missing `baseAddress` attributes in the typed list while preserving them in raw XML, and preserves unknown host children.

Stage 2 does not add bindings, behaviours, client endpoints, lookup helpers, validation diagnostics, CoreWCF mapping, code generation, or CLI tooling.


### Phase 2 Stage 3 typed binding model boundary

The third typed-model slice is implemented and adds initial typed binding support only. It remains additive on top of the preserved raw model and does not validate endpoint-to-binding references.

Stage 3 public API includes:

```text
LegacyWcfBinding
LegacyWcfBindingCollection
LegacyWcfBindings
LegacyWcfConfiguration.Bindings
```

`LegacyWcfBinding` should expose:

```csharp
public sealed class LegacyWcfBinding
{
    public string BindingType { get; }
    public string? Name { get; }
    public IReadOnlyDictionary<string, string> Attributes { get; }
    public LegacyWcfElement RawElement { get; }
}
```

`BindingType` should contain the parent binding group name, such as `basicHttpBinding`, `wsHttpBinding`, `netTcpBinding`, or `customBinding`. `Name` should come from the `<binding name="..." />` attribute. `Attributes` should preserve all attributes from the `<binding>` element, including `name`. `RawElement` should point to the preserved raw `<binding>` element.

`LegacyWcfBindingCollection` should be a typed enumerable collection with `Count`, indexer support, `foreach`, LINQ support through `IEnumerable`/`IReadOnlyList`, and an `Empty` static instance. It should not add `Find(...)` or `GetRequired(...)` in Stage 3.

`LegacyWcfBindings` should expose:

```text
BasicHttp
WsHttp
NetTcp
Custom
```

Each property should return a `LegacyWcfBindingCollection`. `LegacyWcfBindings.Empty` should be used when `<bindings>` is missing.

Stage 3 parsing is built from `LegacyWcfElement` in `LegacyWcfTypedModelBuilder`. It reads known binding groups under `<bindings>`:

```text
basicHttpBinding
wsHttpBinding
netTcpBinding
customBinding
```

For each known group, direct `<binding>` children become typed `LegacyWcfBinding` objects. Missing binding names do not fail the read and do not emit diagnostics in Stage 3. Unknown binding groups remain preserved in the raw tree but are not surfaced through Stage 3 typed binding collections. Unknown child elements inside a `<binding>` remain preserved through `binding.RawElement.Children`.

Stage 3 does not add behaviours, service behaviours, endpoint behaviours, client endpoints, serviceHostingEnvironment, lookup helpers, validation diagnostics, CoreWCF mapping, code generation, or CLI tooling.


### Phase 2 Stage 4 typed behaviour model boundary

The fourth typed-model slice is implemented and adds initial typed behaviour support only. It should remain additive on top of the preserved raw model and should not validate service or endpoint references to behaviour configurations.

Stage 4 public API includes:

```text
LegacyWcfBehavior
LegacyWcfBehaviorCollection
LegacyWcfBehaviors
LegacyWcfConfiguration.Behaviors
```

`LegacyWcfBehavior` exposes:

```csharp
public sealed class LegacyWcfBehavior
{
    public string BehaviorType { get; }
    public string? Name { get; }
    public IReadOnlyDictionary<string, string> Attributes { get; }
    public LegacyWcfElement RawElement { get; }
}
```

`BehaviorType` uses the normalized singular values `serviceBehavior` and `endpointBehavior`. `Name` should come from the source `<behavior name="..." />` or `<behaviour name="..." />` attribute and should be `null` when the attribute is missing. `Attributes` should preserve all attributes from the source behaviour element, including `name`. `RawElement` should point to the preserved raw behaviour element.

`LegacyWcfBehaviorCollection` is a typed enumerable collection with `Count`, indexer support, `foreach`, LINQ support through `IEnumerable`/`IReadOnlyList`, and an `Empty` static instance. It should not add `Find(...)` or `GetRequired(...)` in Stage 4.

`LegacyWcfBehaviors` exposes:

```text
ServiceBehaviors
EndpointBehaviors
```

Stage 4 parsing is built from `LegacyWcfElement` in `LegacyWcfTypedModelBuilder`. It should read known behaviour groups under `<behaviors>` and the British legacy/custom spelling `<behaviours>`:

```text
serviceBehaviors / serviceBehaviours
endpointBehaviors / endpointBehaviours
behavior / behaviour
```

Unknown behaviour groups remain preserved in the raw tree but should not be surfaced through Stage 4 typed behaviour collections. Unknown child elements inside a behaviour should remain preserved through `behavior.RawElement.Children`.

Stage 4 does not add client endpoints, serviceHostingEnvironment, lookup helpers, validation diagnostics, CoreWCF mapping, code generation, or CLI tooling.

## Typed collections

Major collections should be typed and enumerable.

For example:

```text
config.Services
- enumerable collection of typed LegacyWcfService objects
- supports foreach
- supports LINQ
- supports Find(...)
- supports GetRequired(...)
```

Possible shape:

```csharp
public sealed class LegacyWcfServices : IReadOnlyList<LegacyWcfService>
{
    private readonly IReadOnlyList<LegacyWcfService> _services;
    private readonly IReadOnlyDictionary<string, LegacyWcfService> _byName;

    public int Count => _services.Count;

    public LegacyWcfService this[int index] => _services[index];

    public IEnumerator<LegacyWcfService> GetEnumerator()
        => _services.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public LegacyWcfService? Find(string serviceName)
    {
        return _byName.TryGetValue(serviceName, out var service)
            ? service
            : null;
    }

    public LegacyWcfService GetRequired(string serviceName)
    {
        return Find(serviceName)
            ?? throw new LegacyWcfConfigurationException(
                $"The WCF service '{serviceName}' was not found.");
    }
}
```

This pattern should be considered for:

- services
- service endpoints
- client endpoints
- binding collections
- service behaviours
- endpoint behaviours

## Diagnostics philosophy

The reader should be permissive by default.

Guiding rule:

```text
Read what can be read.
Preserve what is unknown.
Report diagnostics.
Only fail when the file cannot be read or the XML cannot be loaded.
```

Unknown WCF elements should not cause failure by default.

Bad:

```text
Unknown element found => throw exception
```

Better:

```text
Unknown element found => preserve in RawElement and add a diagnostic
```

Diagnostics should help users understand:

- what was read
- what was understood
- what was preserved but not strongly modelled
- what may need manual review
- what may affect CoreWCF migration later

## CoreWCF boundary

The core package is not a CoreWCF package.

It should not provide CoreWCF hosting setup directly.

However, the typed model should make future CoreWCF mapping possible. For example:

```csharp
foreach (var service in config.Services)
{
    foreach (var endpoint in service.Endpoints)
    {
        var binding = config.Bindings.Find(
            endpoint.Binding,
            endpoint.BindingConfiguration);

        var behavior = config.Behaviors.EndpointBehaviors.Find(
            endpoint.BehaviorConfiguration);

        // Future optional mapper package could use these values.
    }
}
```

A future CoreWCF adapter may classify elements as:

- directly mappable
- mappable in code
- partially mappable
- unsupported
- requires manual review
- informational

## Parsing approach

Recommended Phase 1 flow:

```text
File path
  -> load XML document
  -> locate <configuration>
  -> locate <system.serviceModel>
  -> build full raw LegacyWcfElement tree
  -> attach diagnostics
  -> return result
```

A later typed-model phase should parse typed concepts from the raw tree. The raw model must continue to be created before typed parsing so that preservation does not depend on typed support.



### Test file organisation and cleanup

Tests should be behaviour-focused rather than automatically split one file per production type. For Phase 1, `LegacyWcfConfigurationReaderTests.cs` covers the public reader behaviour end-to-end:

```text
file path
  -> reader
  -> result
  -> configuration
  -> raw element tree
  -> diagnostics
```

Simple DTO/model types do not need separate test files unless they gain behaviour of their own.

Tests that write temporary config files must clean them up. Use a per-test temporary directory and delete it in `Dispose()`. Avoid writing directly to `Path.GetTempPath()` without cleanup.

Current Phase 1 reader tests cover valid XML, missing files, malformed XML, missing `<configuration>`, missing `<system.serviceModel>`, and unknown XML preservation.

## Licensing

LegacyWcf.Configuration should use the Apache License, Version 2.0, January 2004.

The repository should include a root-level `LICENSE` file. NuGet package metadata should use the SPDX license expression:

```xml
<PackageLicenseExpression>Apache-2.0</PackageLicenseExpression>
```

## Testing approach

Tests should be built around representative configuration files.

Test categories:

- file loading
- missing file
- malformed XML
- missing `<configuration>`
- missing `<system.serviceModel>`
- raw XML preservation
- services parsing
- endpoint parsing
- host/baseAddress parsing
- bindings parsing
- behaviours parsing
- client endpoint parsing
- duplicate names
- unknown elements preserved
- unknown attributes preserved
- diagnostics behaviour
- `Find(...)` and `GetRequired(...)` behaviour

Configuration examples in `docs/configuration-spec.md` should inform test data.


Phase 1 tests currently cover:

- reading a valid `<system.serviceModel>` section
- preserving service and endpoint raw elements and attributes
- preserving unknown custom elements and nested unknown attributes
- returning an error diagnostic for a missing file
- returning an error diagnostic for malformed XML
- returning a clear diagnostic for a missing `<configuration>` root
- returning a clear diagnostic for a missing `<system.serviceModel>` section
- populating source file path and line number where practical

