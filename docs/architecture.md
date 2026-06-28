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
    ├── LegacyWcfClient.cs
    ├── LegacyWcfClientEndpoint.cs
    ├── LegacyWcfClientEndpoints.cs
    ├── LegacyWcfServiceHostingEnvironment.cs
    ├── LegacyWcfService.cs
    ├── LegacyWcfServiceEndpoint.cs
    ├── LegacyWcfServiceEndpoints.cs
    ├── LegacyWcfServices.cs
    └── Internal/
        ├── LegacyWcfRawElementBuilder.cs
        ├── LegacyWcfTypedModelBuilder.cs
        └── LegacyWcfConfigurationValidator.cs
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

The Stage 1 reader flow became:

```text
File path
  -> load XML document
  -> locate <configuration>
  -> locate <system.serviceModel>
  -> build full raw LegacyWcfElement tree
  -> build typed services and endpoints from the raw tree
  -> return LegacyWcfConfiguration with RawSystemServiceModel and Services
```

The current reader flow keeps that raw-first approach and then builds typed services, bindings, behaviours, client endpoint models, and serviceHostingEnvironment from the preserved raw tree before returning `LegacyWcfConfiguration`. Phase 3 adds retrieval helpers on top of those typed collections without changing the reader flow. Phase 4 adds validation diagnostics after raw and typed model construction, without changing raw XML preservation or making the typed model the source of truth.

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

### Phase 2 Stage 5 typed client endpoint model boundary

The fifth typed-model slice is implemented and adds WCF client endpoint support only. It remains additive on top of the preserved raw model and should not validate endpoint references to binding or behaviour configuration.

Stage 5 public API includes:

```text
LegacyWcfClient
LegacyWcfClientEndpoint
LegacyWcfClientEndpoints
LegacyWcfConfiguration.Client
```

`LegacyWcfClient` exposes:

```csharp
public sealed class LegacyWcfClient
{
    public LegacyWcfClientEndpoints Endpoints { get; }
    public LegacyWcfElement RawElement { get; }
}
```

`LegacyWcfClientEndpoint` exposes:

```csharp
public sealed class LegacyWcfClientEndpoint
{
    public string? Name { get; }
    public string? Address { get; }
    public string? Binding { get; }
    public string? BindingConfiguration { get; }
    public string? Contract { get; }
    public string? BehaviorConfiguration { get; }
    public IReadOnlyDictionary<string, string> Attributes { get; }
    public LegacyWcfElement RawElement { get; }
}
```

`LegacyWcfClientEndpoints` is a typed enumerable collection with `Count`, indexer support, `foreach`, LINQ support through `IEnumerable`/`IReadOnlyList`, and an `Empty` static instance. It should not add `Find(...)` or `GetRequired(...)` in Stage 5.

`LegacyWcfConfiguration.Client` should be `null` when the source `<system.serviceModel>` has no direct `<client>` child. If `<client>` exists but has no direct `<endpoint>` children, `Client` should be non-null and `Client.Endpoints.Count` should be `0`.

Stage 5 parsing is built from `LegacyWcfElement` in `LegacyWcfTypedModelBuilder`. It should read direct `<endpoint>` children under `<client>`, preserve source order, preserve missing optional attributes as `null` typed values, preserve all endpoint attributes through `Attributes`, and retain the raw endpoint element through `RawElement`. Unknown child elements under `<client>` remain preserved through `config.Client.RawElement.Children` but should not be surfaced as typed endpoints.

Stage 5 does not add serviceHostingEnvironment, lookup helpers, validation diagnostics, CoreWCF mapping, code generation, or CLI tooling.



### Phase 4 validation diagnostics boundary

Phase 4 validation diagnostics are implemented on top of the existing raw-first reader flow. They do not replace parsing, lookup, or raw preservation behaviour.

The preferred flow is:

```text
File path
  -> load XML document
  -> locate <configuration>
  -> locate <system.serviceModel>
  -> build full raw LegacyWcfElement tree
  -> build typed services, bindings, behaviours, client endpoints, and serviceHostingEnvironment from the raw tree
  -> run permissive validation over the raw and typed model
  -> attach diagnostics to the result and configuration
  -> return result
```

Validation is implemented as an internal concern. `LegacyWcfConfigurationValidator` lives under `src/LegacyWcf.Configuration/Internal/` and operates on `LegacyWcfElement` plus the typed model. Public API files remain directly under `src/LegacyWcf.Configuration/`; no new public validation type is required.

Phase 4 diagnostics report duplicate named services, duplicate named bindings, duplicate named service behaviours, duplicate named endpoint behaviours, duplicate direct `serviceHostingEnvironment` elements, missing binding references from service and client endpoints, missing endpoint behaviour references from service and client endpoints, missing service behaviour references from services, and unknown or unsupported raw elements.

The default mode remains permissive: well-formed legacy WCF XML with validation issues should still return `Success == true`. Existing failure behaviour should remain reserved for missing or blank paths, missing files, unreadable files, malformed XML, missing `<configuration>`, and missing `<system.serviceModel>`.

Lookup helpers are not validators. If duplicates exist, Phase 3 lookup helpers continue returning the first matching object; Phase 4 reports duplicates through diagnostics.

Phase 4 does not add CoreWCF mapping, code generation, CLI tooling, automatic migration, strict schema enforcement, or a dependency on CoreWCF in the core package.

Implemented Phase 4 validation diagnostic codes are:

| Code | Meaning |
|---|---|
| `LWC1001` | Unknown or unsupported WCF configuration element was preserved in raw XML. |
| `LWC1002` | Duplicate non-blank service name. |
| `LWC1003` | Duplicate non-blank binding name within the same binding type. |
| `LWC1004` | Duplicate non-blank service behaviour name. |
| `LWC1005` | Duplicate non-blank endpoint behaviour name. |
| `LWC1006` | Duplicate direct `serviceHostingEnvironment` element. |
| `LWC1007` | Endpoint references a missing binding configuration. |
| `LWC1008` | Endpoint references a missing endpoint behaviour configuration. |
| `LWC1009` | Service references a missing service behaviour configuration. |


### Phase 2 Stage 6 typed serviceHostingEnvironment model boundary

The sixth typed-model slice is implemented and adds WCF `serviceHostingEnvironment` support only. It remains additive on top of the preserved raw model and should not validate values or emit duplicate diagnostics.

Stage 6 public API includes:

```text
LegacyWcfServiceHostingEnvironment
LegacyWcfConfiguration.ServiceHostingEnvironment
```

`LegacyWcfServiceHostingEnvironment` should expose:

```csharp
public sealed class LegacyWcfServiceHostingEnvironment
{
    public string? AspNetCompatibilityEnabled { get; }
    public string? MultipleSiteBindingsEnabled { get; }
    public IReadOnlyDictionary<string, string> Attributes { get; }
    public LegacyWcfElement RawElement { get; }
}
```

`LegacyWcfConfiguration.ServiceHostingEnvironment` is `null` when the source `<system.serviceModel>` element has no direct `<serviceHostingEnvironment>` child.

Stage 6 parsing is built from `LegacyWcfElement` in `LegacyWcfTypedModelBuilder`. It reads the first direct `<serviceHostingEnvironment>` child under `<system.serviceModel>`, preserves all source attributes through `Attributes`, expose `aspNetCompatibilityEnabled` and `multipleSiteBindingsEnabled` as string values, and retain the raw element through `RawElement`. Unknown child elements under `<serviceHostingEnvironment>` remain preserved through `ServiceHostingEnvironment.RawElement.Children` but should not be surfaced as typed objects.

Stage 6 does not parse boolean values into `bool`, validate boolean strings, add lookup helpers, emit duplicate diagnostics, add CoreWCF mapping, generate code, or add CLI tooling. If more than one direct `<serviceHostingEnvironment>` element exists, Stage 6 parses the first direct element and preserves all elements in `RawSystemServiceModel`; duplicate diagnostics are emitted by Phase 4 validation.

### Phase 3 retrieval API boundary

Phase 3 is implemented and adds additive lookup helpers to the existing typed collections. It does not change XML parsing, raw XML preservation, diagnostics, or CoreWCF package boundaries.

Implemented lookup helpers include service lookup by name, service endpoint lookup by name and contract, binding lookup by name and by top-level binding type plus name, behaviour lookup by name, and client endpoint lookup by name and contract. Matching is case-insensitive. `Find...` helpers return `null` for missing values. `GetRequired...` helpers throw `InvalidOperationException` with context-rich messages. If duplicates exist, the first matching object is returned and duplicate diagnostics remain a later validation concern.

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

## Phase 3 retrieval API boundary

Phase 3 retrieval APIs are implemented. They add targeted retrieval APIs on top of the existing typed collections without changing how XML is read or typed models are built.

Phase 3 belongs in the public typed collection classes because those classes already own enumeration and indexing for their item types. The retrieval helpers should be additive convenience APIs over those lists, not a new parser layer and not a validation system.

Implemented collection-level helpers:

```text
LegacyWcfServices
- Find(string name)
- GetRequired(string name)

LegacyWcfServiceEndpoints
- FindByName(string name)
- GetRequiredByName(string name)
- FindByContract(string contract)
- GetRequiredByContract(string contract)

LegacyWcfBindingCollection
- Find(string? name)
- GetRequired(string? name)

LegacyWcfBehaviorCollection
- Find(string? name)
- GetRequired(string? name)

LegacyWcfClientEndpoints
- FindByName(string name)
- GetRequiredByName(string name)
- FindByContract(string contract)
- GetRequiredByContract(string contract)
```

Planned top-level binding helpers:

```text
LegacyWcfBindings
- Find(string? bindingType, string? name)
- GetRequired(string? bindingType, string? name)
```

The top-level binding helpers should route known binding type values to the existing typed binding collections:

```text
basicHttpBinding -> BasicHttp
wsHttpBinding    -> WsHttp
netTcpBinding    -> NetTcp
customBinding    -> Custom
```

Service behaviour and endpoint behaviour lookups should remain explicit. Collection-level usage is preferred:

```csharp
config.Behaviors.ServiceBehaviors.GetRequired("CustomerServiceBehavior");
config.Behaviors.EndpointBehaviors.GetRequired("CustomerEndpointBehavior");
```

Avoid an ambiguous `config.Behaviors.GetRequired(name)` helper because service behaviours and endpoint behaviours are separate WCF concepts. If top-level behaviour helpers are ever added, they should use explicit names such as `GetRequiredServiceBehavior(...)` and `GetRequiredEndpointBehavior(...)`.

Phase 3 matching rules:

- use case-insensitive matching for WCF names and identifiers
- return `null` from `Find...` helpers when no match exists
- throw `InvalidOperationException` from `GetRequired...` helpers when no match exists
- include useful lookup context in required-lookup exception messages
- return the first matching item if duplicates exist
- do not emit diagnostics from lookup helpers
- do not validate duplicate names or missing binding/behaviour references
- do not add lookup helpers to `LegacyWcfServiceHostingEnvironment`, because there is only one typed value on `LegacyWcfConfiguration`

The reader flow should not change for Phase 3. The raw `<system.serviceModel>` tree remains the source of truth, and typed models remain additive views over the preserved raw tree.

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
- validation diagnostics for duplicates and missing references
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
