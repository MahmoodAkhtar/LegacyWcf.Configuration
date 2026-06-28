# AI Context

This file is a handover document for AI-assisted development of LegacyWcf.Configuration.

When starting a new ChatGPT session for this project, provide this file together with the README and relevant documentation/source files. Its purpose is to give the assistant enough context to understand the project identity, design decisions, package boundaries, roadmap, and implementation constraints.

## Project identity

Project name:

```text
LegacyWcf.Configuration
```

Intended NuGet package:

```text
LegacyWcf.Configuration
```

Root namespace:

```csharp
LegacyWcf.Configuration
```

Current solution file:

```text
LegacyWcf.Configuration.slnx
```

This project is separate from LegacyLens.NET.

## Core purpose

LegacyWcf.Configuration is a modern .NET library for reading, preserving, modelling, and querying legacy WCF `<system.serviceModel>` configuration from `app.config`, `web.config`, and external `.config` files.

The initial goal is:

```text
Become a trusted reader for legacy WCF <system.serviceModel> configuration.
```

The first goal is not automatic migration and not full CoreWCF setup generation.

## Problem being solved

Legacy WCF applications often store important runtime configuration in XML, especially under:

```text
configuration/system.serviceModel/**
```

During modernisation, developers need to retrieve these values reliably even if the original solution cannot be built or executed.

Some legacy WCF configuration elements are not directly supported by CoreWCF configuration support. For example, `<host>` configuration may still contain useful information, such as base addresses, even if it needs to be applied manually in modern hosting code.

## Design principle

Separate three concerns:

```text
Reading
Understanding
Applying
```

### Reading

Read XML and preserve what exists.

### Understanding

Expose typed models and diagnostics for known WCF concepts.

### Applying

Future optional packages may map values to CoreWCF concepts or generate code.

The core package handles reading and understanding only.

## Package boundaries

The core package must not depend on:

- CoreWCF
- ASP.NET Core hosting
- Roslyn code generation
- legacy WCF runtime assemblies
- the target legacy application building successfully

Future optional packages may include:

```text
LegacyWcf.Configuration.CoreWcf
LegacyWcf.Configuration.CodeGeneration
```

## Target frameworks

The core package should target:

```xml
<TargetFrameworks>netstandard2.0;net8.0</TargetFrameworks>
```

Rationale:

- `netstandard2.0` supports .NET Framework 4.8 consumers.
- `net8.0` gives modern .NET consumers a native target.
- Shared code should avoid APIs unavailable in `netstandard2.0` unless conditional compilation is used.


## Source organisation and code style conventions

These conventions capture the agreed working rules for namespaces, source layout, usings, project settings, and tests. Future implementation chats should follow these conventions so the project does not repeatedly revisit the same decisions.

### Public API file placement

For the current stage of the project, public API types should live directly under:

```text
src/LegacyWcf.Configuration/
```

Do not create folders such as `Reading/`, `Raw/`, `Model/`, or `Diagnostics/` just to group public API types.

The current convention is:

```text
Top-level project folder = public API
Internal/                = implementation detail
```

Example:

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

This keeps the maintainer mental model aligned with the consumer mental model: the public API is easy to find at the top level, while implementation details are separated.

### Namespace convention

Public API types should use the root namespace:

```csharp
namespace LegacyWcf.Configuration;
```

Internal implementation-only types should use:

```csharp
namespace LegacyWcf.Configuration.Internal;
```

Avoid introducing sub-namespaces for public API types unless the public API has grown enough that the added structure clearly improves maintainability.

### Folder rule

Use folders only when they communicate something useful.

Good:

```text
Internal/
```

because it clearly separates implementation details from the public API.

Avoid:

```text
Reading/
Raw/
Model/
Diagnostics/
```

for the current Phase 1 public API because this adds friction when locating public types and does not currently provide enough benefit.

### Project file settings

Enable nullable reference types at project level:

```xml
<Nullable>enable</Nullable>
```

Do not add `#nullable enable` at the top of every `.cs` file.

Disable implicit usings for this library:

```xml
<ImplicitUsings>disable</ImplicitUsings>
```

Use a modern language version explicitly:

```xml
<LangVersion>latest</LangVersion>
```

These settings keep the code clear and Rider-friendly, especially while the library multi-targets `netstandard2.0;net8.0`.

### Explicit usings

Use explicit `using` directives in each file for namespaces the file actually depends on.

Do not remove a `using` simply because implicit usings could theoretically provide it.

For example, if a file uses:

```csharp
IReadOnlyList<T>
IReadOnlyDictionary<TKey, TValue>
Dictionary<TKey, TValue>
List<T>
```

then keep:

```csharp
using System.Collections.Generic;
```

The file should clearly show its real dependencies.

### Test file organisation

Do not automatically create one test file per production type.

Prefer behaviour-focused tests. For Phase 1, it is acceptable for `LegacyWcfConfigurationReaderTests.cs` to test the public read behaviour end-to-end:

```text
file path
  -> reader
  -> result
  -> configuration
  -> raw element tree
  -> diagnostics
```

Simple DTO/model types such as these do not need separate test files unless they gain behaviour of their own:

```text
LegacyWcfConfigurationReadResult
LegacyWcfConfiguration
LegacyWcfElement
LegacyWcfDiagnostic
LegacyWcfDiagnosticSeverity
```

### Test temp-file cleanup

If tests write temporary config files, they must clean them up.

Use a per-test temporary directory and delete it in `Dispose()`:

```csharp
public sealed class LegacyWcfConfigurationReaderTests : IDisposable
{
    private readonly string _tempDirectory;

    public LegacyWcfConfigurationReaderTests()
    {
        _tempDirectory = Path.Combine(
            Path.GetTempPath(),
            "LegacyWcf.Configuration.Tests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
}
```

Avoid writing directly to `Path.GetTempPath()` without cleanup.

### Line endings

Generated or modified files should use Windows-style CRLF line endings.

## MVP scope

The MVP should include:

- full-fidelity reader for `<system.serviceModel>`
- raw XML preservation
- typed models for common WCF concepts
- typed enumerable collections
- permissive diagnostics (Phase 4 next)
- source file path preservation
- line number preservation if practical
- tests using representative WCF config files

High-value typed model areas:

- services
- service endpoints
- service host settings
- host base addresses
- host timeouts
- bindings
- basicHttpBinding
- wsHttpBinding
- netTcpBinding
- customBinding
- behaviours
- service behaviours
- endpoint behaviours
- client endpoints
- serviceHostingEnvironment


## Current implementation status

**Phase 1: Full-fidelity reader is implemented.**

The current code reads and preserves the raw `<system.serviceModel>` XML tree.

**Phase 2 Stage 1: Typed services and service endpoints are implemented.**

The current code exposes typed WCF services and service endpoints through:

```text
LegacyWcfConfiguration.Services
LegacyWcfService
LegacyWcfServiceEndpoint
LegacyWcfServices
LegacyWcfServiceEndpoints
```

**Phase 2 Stage 2: Typed service hosts, host base addresses, and host timeouts are implemented.**

**Phase 2 Stage 3: Initial typed binding support is implemented.**

**Phase 2 Stage 4: Initial typed behaviour support is implemented.**

**Phase 2 Stage 5: Typed client endpoint support is implemented.**

**Phase 2 Stage 6: Typed `serviceHostingEnvironment` support is implemented.**

The current code exposes typed service host configuration through:

```text
LegacyWcfService.Host
LegacyWcfHost
LegacyWcfHostTimeouts
```

Implemented typed host behaviour:

- `service.Host` is `null` when a service has no direct `<host>` child.
- `service.Host.BaseAddresses` preserves `<baseAddresses>/<add baseAddress="..." />` values in source order.
- `<add>` entries without a `baseAddress` attribute are ignored by the typed `BaseAddresses` list but preserved in raw XML.
- `service.Host.Timeouts` reads optional `<timeouts openTimeout="..." closeTimeout="..." />` values as strings.
- unknown host child elements remain available through `service.Host.RawElement.Children`.
- every typed service, endpoint, host, and timeout object retains access to its source `LegacyWcfElement`.

Current source files include:

```text
src/LegacyWcf.Configuration/
├── LegacyWcfConfigurationReader.cs
├── LegacyWcfConfigurationReadResult.cs
├── LegacyWcfConfiguration.cs
├── LegacyWcfElement.cs
├── LegacyWcfDiagnostic.cs
├── LegacyWcfDiagnosticSeverity.cs
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
    └── LegacyWcfTypedModelBuilder.cs
```

Current diagnostics cover:

- missing or blank file path
- file not found
- file cannot be read
- malformed XML
- missing `<configuration>`
- missing `<system.serviceModel>`

Current diagnostic codes:

| Code | Meaning |
|---|---|
| `LWC0001` | Missing/blank path or file not found. |
| `LWC0002` | File could not be read. |
| `LWC0003` | XML could not be loaded or parsed. |
| `LWC0004` | Root `<configuration>` element was not found. |
| `LWC0005` | `<system.serviceModel>` was not found under `<configuration>`. |

Current tests cover:

- valid `<system.serviceModel>` raw tree preservation
- missing file diagnostics
- malformed XML diagnostics
- missing `<configuration>` diagnostics
- missing `<system.serviceModel>` diagnostics
- unknown custom element and attribute preservation
- typed service parsing
- typed service endpoint parsing
- missing `<services>`
- service missing `name`
- unknown service child preservation
- typed host base address parsing
- multiple typed host base addresses in source order
- missing host
- host without base addresses
- `<add>` without `baseAddress`
- unknown host child preservation
- typed `basicHttpBinding` binding parsing
- typed `wsHttpBinding` binding parsing
- typed `netTcpBinding` binding parsing
- typed `customBinding` binding parsing
- missing `<bindings>` returns empty typed binding collections
- unnamed bindings are preserved in typed binding collections
- unknown binding child preservation
- unknown binding groups remain raw-only
- typed client endpoint parsing
- client endpoint behaviour configuration parsing
- multiple client endpoints preserved in source order
- missing `<client>` produces `config.Client == null`
- empty `<client>` produces an empty typed endpoint collection
- missing optional client endpoint attributes are preserved as `null` typed values
- unknown client endpoint attributes are preserved through `endpoint.Attributes`
- unknown client child elements remain raw-only
- typed serviceHostingEnvironment parsing
- serviceHostingEnvironment common attribute preservation
- missing optional serviceHostingEnvironment attributes preserved as `null` typed values
- missing `<serviceHostingEnvironment>` produces `config.ServiceHostingEnvironment == null`
- unknown serviceHostingEnvironment attributes are preserved through `ServiceHostingEnvironment.Attributes`
- unknown serviceHostingEnvironment child elements remain raw-only
- multiple direct serviceHostingEnvironment elements use the first typed element and preserve all raw elements

Historical Phase 2 Stage 6 test status:

- provided full test run after Phase 2 Stage 6: 49 total, 49 passed, 0 failed, 0 skipped
- Phase 2 Stage 6 added 7 tests to the provided test file

Historical Phase 3 test status: 70 total, 70 passed, 0 failed, 0 skipped.

Future AI implementation chats should preserve this boundary: raw XML preservation first, typed parsing only as additive views over the raw tree.

## Current implementation status: Phase 3 retrieval APIs

Phase 3 retrieval APIs are implemented.

Current NuGet package version: `0.4.0`.

Latest provided test status after Phase 4: 78 total, 78 passed, 0 failed, 0 skipped.

Phase 3 adds targeted lookup helpers on top of the existing typed model. This is an API convenience phase, not a parser phase and not a validation phase.

Implemented Phase 3 public API additions include:

```text
LegacyWcfServices
- LegacyWcfService? Find(string name)
- LegacyWcfService GetRequired(string name)

LegacyWcfServiceEndpoints
- LegacyWcfServiceEndpoint? FindByName(string name)
- LegacyWcfServiceEndpoint GetRequiredByName(string name)
- LegacyWcfServiceEndpoint? FindByContract(string contract)
- LegacyWcfServiceEndpoint GetRequiredByContract(string contract)

LegacyWcfBindingCollection
- LegacyWcfBinding? Find(string? name)
- LegacyWcfBinding GetRequired(string? name)

LegacyWcfBindings
- LegacyWcfBinding? Find(string? bindingType, string? name)
- LegacyWcfBinding GetRequired(string? bindingType, string? name)

LegacyWcfBehaviorCollection
- LegacyWcfBehavior? Find(string? name)
- LegacyWcfBehavior GetRequired(string? name)

LegacyWcfClientEndpoints
- LegacyWcfClientEndpoint? FindByName(string name)
- LegacyWcfClientEndpoint GetRequiredByName(string name)
- LegacyWcfClientEndpoint? FindByContract(string contract)
- LegacyWcfClientEndpoint GetRequiredByContract(string contract)
```

Phase 3 lookup rules:

- uses case-insensitive matching for WCF names and identifiers
- `Find...` methods return `null` when no item matches
- `GetRequired...` methods throw `InvalidOperationException` when no item matches
- required-lookup exception messages should include useful context, such as service name, endpoint name, endpoint contract, binding type, binding name, behaviour name, and whether the lookup was for service or client configuration
- service, service endpoint, and client endpoint lookup values that are `null`, blank, or whitespace should not match
- binding and behaviour name lookups may accept `null` because unnamed bindings and behaviours are preserved
- top-level binding lookup supports `basicHttpBinding`, `wsHttpBinding`, `netTcpBinding`, and `customBinding`
- if duplicates exist, return the first matching object and leave duplicate diagnostics to Phase 4 validation
- lookup helpers do not emit diagnostics
- lookup helpers do not validate missing binding or behaviour references
- Phase 3 does not modify `LegacyWcfConfigurationReader` or XML parsing
- Phase 3 does not add lookup helpers to `LegacyWcfServiceHostingEnvironment`

Phase 3 should not implement validation diagnostics, CoreWCF mapping, code generation, CLI tooling, richer binding-specific models, richer behaviour-specific models, automatic migration, or strict schema enforcement.



## Current implementation status: Phase 4 validation diagnostics

Current release context after Phase 4:

- current NuGet package version: `0.4.0`
- latest provided test status: 78 total, 78 passed, 0 failed, 0 skipped

Phase 4 validation diagnostics are implemented. They add permissive validation and useful diagnostics over the existing raw and typed WCF configuration model.

Phase 4 builds on Phase 3 and remains additive:

- do not break existing public APIs
- do not change lookup helper signatures
- do not change raw XML preservation rules
- do not make lookup helpers emit diagnostics
- do not change duplicate lookup behaviour; duplicates should still return the first match from `Find(...)` and `GetRequired(...)`
- report duplicates and unresolved references through diagnostics
- keep `Success == true` for well-formed XML that contains validation warnings

Phase 4 diagnostics include:

- duplicate non-blank service names
- duplicate named bindings within the same binding type
- duplicate named service behaviours
- duplicate named endpoint behaviours
- duplicate direct `serviceHostingEnvironment` elements
- service endpoints referencing missing binding configurations
- client endpoints referencing missing binding configurations
- service endpoints referencing missing endpoint behaviour configurations
- client endpoints referencing missing endpoint behaviour configurations
- services referencing missing service behaviour configurations
- unknown or unsupported raw elements preserved for review

Phase 4 does not implement CoreWCF mapping, code generation, CLI tooling, automatic migration, strict schema enforcement, richer binding-specific models, or richer behaviour-specific models.

Implementation notes for Phase 4:

- validation is an internal post-processing step after the raw and typed model have been built
- use the preserved `LegacyWcfElement` tree and current typed model as validation inputs
- keep public API files directly under `src/LegacyWcf.Configuration/`
- `LegacyWcfConfigurationValidator` is an implementation-only validation helper under `src/LegacyWcf.Configuration/Internal/`
- diagnostics are attached to both the read result and returned configuration for successful reads
- preserve existing read-failure behaviour and diagnostic codes `LWC0001` to `LWC0005`
- use warning or informational diagnostics for validation issues unless an issue prevents a usable configuration from being returned

Implemented Phase 4 validation codes:

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


## Completed Phase 2 Stage 6 slice

**Phase 2 Stage 6: typed serviceHostingEnvironment support is implemented.**

Stage 6 adds typed support for WCF `serviceHostingEnvironment` only:

- `LegacyWcfServiceHostingEnvironment`
- `LegacyWcfConfiguration.ServiceHostingEnvironment`
- parsing the first direct `<serviceHostingEnvironment>` child under `<system.serviceModel>` from the preserved raw `LegacyWcfElement` tree
- raw XML preservation for the typed `serviceHostingEnvironment` object
- tests for common attributes, missing attributes, missing element, unknown attributes, unknown child elements, duplicate direct elements, and raw XML preservation

Required Stage 6 public model shape:

```text
LegacyWcfServiceHostingEnvironment
- string? AspNetCompatibilityEnabled
- string? MultipleSiteBindingsEnabled
- IReadOnlyDictionary<string, string> Attributes
- LegacyWcfElement RawElement

LegacyWcfConfiguration
- LegacyWcfServiceHostingEnvironment? ServiceHostingEnvironment
```

Stage 6 parsing rules:

- `AspNetCompatibilityEnabled` comes from `aspNetCompatibilityEnabled`.
- `MultipleSiteBindingsEnabled` comes from `multipleSiteBindingsEnabled`.
- missing optional attributes become `null` typed property values.
- `Attributes` preserves all source attributes, including unknown attributes.
- `RawElement` points to the preserved raw `<serviceHostingEnvironment>` element.
- `config.ServiceHostingEnvironment` is `null` when the element is missing.
- unknown child elements remain available through `ServiceHostingEnvironment.RawElement.Children`.
- do not parse boolean values into `bool`; preserve values as strings exactly as they appear in XML.
- do not validate whether `aspNetCompatibilityEnabled` or `multipleSiteBindingsEnabled` values are valid boolean strings.
- if more than one direct `<serviceHostingEnvironment>` element exists, parse the first direct element and preserve all elements in `RawSystemServiceModel`; duplicate diagnostics are emitted by Phase 4 validation.

Stage 6 does not implement:

- `Find(...)`
- `GetRequired(...)`
- service lookup helpers
- service endpoint lookup helpers
- client endpoint lookup helpers
- binding lookup helpers
- behaviour lookup helpers
- validation diagnostics for duplicate services, bindings, behaviours, endpoints, or serviceHostingEnvironment elements
- validation diagnostics for missing binding references
- validation diagnostics for missing behaviour references
- CoreWCF mapping
- code generation
- CLI tooling

Implementation notes for Stage 6:

- `src/LegacyWcf.Configuration/Internal/LegacyWcfTypedModelBuilder.cs` is extended
- typed serviceHostingEnvironment is built from `LegacyWcfElement` only
- serviceHostingEnvironment is not parsed from `XDocument` or `XElement`
- files are not read inside the typed model builder
- public API files remain directly under `src/LegacyWcf.Configuration/`
- implementation-only helpers remain under `src/LegacyWcf.Configuration/Internal/`
- public API types use the root namespace `LegacyWcf.Configuration`
- internal implementation types use `LegacyWcf.Configuration.Internal`

## Completed Phase 2 Stage 5 slice

**Phase 2 Stage 5: typed client endpoints is implemented.**

Stage 5 adds typed support for WCF client endpoints only:

- `LegacyWcfClient`
- `LegacyWcfClientEndpoint`
- `LegacyWcfClientEndpoints`
- `LegacyWcfConfiguration.Client`
- parsing direct `<client>/<endpoint>` children from the preserved raw `LegacyWcfElement` tree
- raw XML preservation for the `<client>` element and every typed client endpoint

Required Stage 5 public model shape:

```text
LegacyWcfClient
- LegacyWcfClientEndpoints Endpoints
- LegacyWcfElement RawElement

LegacyWcfClientEndpoint
- string? Name
- string? Address
- string? Binding
- string? BindingConfiguration
- string? Contract
- string? BehaviorConfiguration
- IReadOnlyDictionary<string, string> Attributes
- LegacyWcfElement RawElement

LegacyWcfClientEndpoints
- Count
- indexer
- foreach support
- LINQ support through IEnumerable/IReadOnlyList
- Empty static instance

LegacyWcfConfiguration
- LegacyWcfClient? Client
```

Stage 5 parsing rules:

- if `<client>` is missing, `config.Client` should be `null`
- if `<client>` exists with no direct `<endpoint>` children, `config.Client` should be non-null and `config.Client.Endpoints.Count` should be `0`
- client endpoints should preserve source XML order
- missing optional endpoint attributes should become `null` typed property values
- all source endpoint attributes, including unknown attributes, should be preserved through `endpoint.Attributes`
- every typed client endpoint should retain its source `LegacyWcfElement` through `RawElement`
- unknown child elements under `<client>` should remain available through `config.Client.RawElement.Children` but should not be typed in Stage 5

Implementation guidance:

- `src/LegacyWcf.Configuration/Internal/LegacyWcfTypedModelBuilder.cs` is extended
- build typed client endpoints from the preserved `LegacyWcfElement` tree only
- do not parse client endpoints directly from `XDocument` or `XElement`
- keep public API files directly under `src/LegacyWcf.Configuration/`
- keep implementation-only helpers under `src/LegacyWcf.Configuration/Internal/`

Stage 5 does not implement:

- serviceHostingEnvironment
- `Find(...)`
- `GetRequired(...)`
- service lookup helpers
- service endpoint lookup helpers
- client endpoint lookup helpers
- binding lookup helpers
- behaviour lookup helpers
- validation diagnostics for duplicate client endpoints
- validation diagnostics for client endpoints referencing missing bindings
- validation diagnostics for client endpoints referencing missing behaviours
- validation diagnostics for duplicate bindings or behaviours
- CoreWCF mapping
- code generation
- CLI tooling

Phase 2 should still avoid CoreWCF mapping, code generation, and CLI tooling.

## Raw model

The raw model should preserve the full XML tree under `<system.serviceModel>`.

A possible shape:

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

The exact API may change, but the preservation principle should not.

## Typed model

The typed model should sit on top of the raw model.

Every typed object should retain a link back to the raw XML element.

The implemented Phase 2 Stage 2 public model shape currently includes:

```text
LegacyWcfService
- string Name
- string? BehaviorConfiguration
- LegacyWcfServiceEndpoints Endpoints
- LegacyWcfHost? Host
- LegacyWcfElement RawElement

LegacyWcfServiceEndpoint
- string? Name
- string? Address
- string? Binding
- string? BindingConfiguration
- string? Contract
- string? BehaviorConfiguration
- LegacyWcfElement RawElement

LegacyWcfServices
- Count
- indexer
- foreach support
- LINQ support through IEnumerable/IReadOnlyList

LegacyWcfServiceEndpoints
- Count
- indexer
- foreach support
- LINQ support through IEnumerable/IReadOnlyList

LegacyWcfHost
- IReadOnlyList<string> BaseAddresses
- LegacyWcfHostTimeouts? Timeouts
- LegacyWcfElement RawElement

LegacyWcfHostTimeouts
- string? CloseTimeout
- string? OpenTimeout
- LegacyWcfElement RawElement
```

`LegacyWcfConfiguration` currently exposes `LegacyWcfServices Services`, defaulting to an empty collection when no `<services>` element exists. It also exposes `LegacyWcfBindings Bindings`, defaulting to `LegacyWcfBindings.Empty` when no `<bindings>` element exists, `LegacyWcfBehaviors Behaviors`, defaulting to `LegacyWcfBehaviors.Empty` when no `<behaviors>` or `<behaviours>` element exists, and `LegacyWcfClient? Client`, which remains `null` when no `<client>` element exists.

A possible shape:

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

## Typed collections

Major collections should be typed and enumerable.

For example, `config.Services` should:

- be an enumerable collection of `LegacyWcfService`
- support `foreach`
- support LINQ
- support `Find(...)`
- support `GetRequired(...)`

Example:

```csharp
foreach (var service in config.Services)
{
    Console.WriteLine(service.Name);
}

var service = config.Services.GetRequired(
    "MyCompany.Services.CustomerService");
```

This pattern should also be considered for endpoints, bindings, service behaviours, endpoint behaviours, and client endpoints.

## Diagnostics philosophy

The reader should be permissive by default.

Guiding rule:

```text
Read what can be read.
Preserve what is unknown.
Report diagnostics.
Only fail when the file cannot be read or the XML cannot be loaded.
```

Examples of diagnostics:

- file not found
- malformed XML
- missing `<configuration>`
- missing `<system.serviceModel>`
- unknown elements preserved
- unknown attributes preserved
- duplicate named services
- duplicate named bindings
- endpoint references missing binding configuration
- endpoint references missing behaviour configuration
- service references missing behaviour configuration
- unsupported or partially understood elements
- likely CoreWCF migration concerns


## License

The project should use the Apache License, Version 2.0, January 2004.

The repository should include a root-level `LICENSE` file containing the Apache 2.0 license text. Package metadata should use the SPDX license expression `Apache-2.0`.

## Important decisions already made

- The project/package name is `LegacyWcf.Configuration`.
- The core package should target `netstandard2.0;net8.0`.
- The core package should not depend on CoreWCF.
- The first package is a reader/model package, not a migration package.
- Raw XML preservation is essential.
- Unknown elements and attributes must not be silently discarded.
- Typed models should retain access to their raw XML element.
- Diagnostics should be permissive and useful.
- Optional CoreWCF mapping helpers should come later as a separate package.

## Commit message convention

Use Conventional Commit-style messages for this repository.

Format:

```text
<type>(<scope>): <imperative description>
```

Examples:

```text
feat(parser): preserve raw system.serviceModel xml
fix(parser): handle missing serviceModel section
docs(usage): add basic loading example
test(parser): cover empty serviceModel section
refactor(api): extract service lookup helpers
build(project): add nuget package metadata
```

The commit message should clearly answer:

```text
What kind of change is it? -> type
Where did it happen?       -> scope
What changed?              -> description
```

### Commit types

Use these commit types consistently:

| Type | Use when |
|---|---|
| `feat` | Adding a new capability, public API, parser behaviour, model, diagnostic, or supported WCF configuration concept. |
| `fix` | Correcting broken, incorrect, or unintended behaviour. |
| `docs` | Changing documentation only. |
| `refactor` | Improving internal structure without intentionally changing public behaviour. |
| `test` | Adding, updating, or fixing tests only. |
| `build` | Changing project files, target frameworks, package metadata, CI, publishing, or release behaviour. |
| `chore` | Repository maintenance that is not product code, tests, docs, or build behaviour. |
| `style` | Formatting, whitespace, naming cleanup, or code style changes with no behaviour change. |
| `perf` | Improving performance, reducing repeated XML traversal, reducing allocations, or avoiding unnecessary IO. |
| `revert` | Reverting a previous commit. |

### Commit scopes

Prefer scopes that match the main architectural and domain areas of `LegacyWcf.Configuration`.

| Scope | Use for |
|---|---|
| `parser` | XML loading, XML traversal, `<system.serviceModel>` reading, and raw XML preservation. |
| `model` | Typed configuration model classes and raw model classes. |
| `services` | WCF service definitions under `<services>`. |
| `endpoints` | Service endpoints and client endpoints. |
| `bindings` | Binding collections and binding configuration such as `basicHttpBinding`, `wsHttpBinding`, `netTcpBinding`, and `customBinding`. |
| `behaviours` | Service behaviours and endpoint behaviours. |
| `validation` | Validation rules for missing, duplicate, malformed, or inconsistent configuration. |
| `diagnostics` | Warnings, informational messages, unsupported configuration notes, and migration concern reporting. |
| `api` | Public API shape, extension methods, typed collection helpers, lookup methods, and consumer-facing convenience methods. |
| `docs` | General documentation structure. |
| `usage` | Usage examples and consumer guidance. |
| `architecture` | Internal design documentation. |
| `tests` | Test project structure, shared test helpers, and test data. |
| `build` | `.csproj`, `.sln`, `.slnx`, package metadata, CI, and publishing configuration. |
| `repo` | Repository housekeeping such as `.gitignore`, license files, and root-level maintenance files. |

### Description style

Write the description in imperative style.

Good:

```text
feat(parser): preserve raw system.serviceModel xml
fix(bindings): handle unnamed binding configurations
docs(architecture): explain raw and typed model relationship
test(endpoints): cover endpoint binding configuration lookup
refactor(parser): extract service model section reader
```

Avoid:

```text
feat(parser): added raw xml preservation
fix(bindings): fixed unnamed binding configs
docs(architecture): updated architecture docs
```

The description should read naturally as:

```text
This commit will preserve raw system.serviceModel xml.
```

### Choosing the right type

Use this decision guide:

| Question | Type |
|---|---|
| Does it add something consumers can use or observe? | `feat` |
| Does it correct wrong behaviour? | `fix` |
| Does it only change documentation? | `docs` |
| Does it only change tests? | `test` |
| Does it improve internal design without changing intended behaviour? | `refactor` |
| Does it make parsing, modelling, or querying faster or cheaper? | `perf` |
| Does it change `.csproj`, `.slnx`, CI, packaging, or publishing? | `build` |
| Is it general repository maintenance? | `chore` |
| Is it only formatting or code style? | `style` |
| Does it undo a previous commit? | `revert` |


## Things AI assistants should avoid

Do not:

- add a CoreWCF dependency to the core package
- make the parser strict by default
- throw for unknown WCF elements
- silently discard unknown XML
- require the legacy solution to build
- require the legacy WCF runtime to be available
- turn the MVP into a code-generation project
- target only `net8.0` unless the project direction changes
- overfit the design to only one sample config file

## Current repository structure

```text
LegacyWcf.Configuration/
│
├── docs/
│   ├── ai-context.md
│   ├── architecture.md
│   ├── configuration-spec.md
│   ├── roadmap.md
│   └── usage.md
│
├── src/
│   └── LegacyWcf.Configuration/
│       ├── LegacyWcf.Configuration.csproj
│       ├── LegacyWcfConfiguration.cs
│       ├── LegacyWcfConfigurationReader.cs
│       ├── LegacyWcfConfigurationReadResult.cs
│       ├── LegacyWcfDiagnostic.cs
│       ├── LegacyWcfDiagnosticSeverity.cs
│       ├── LegacyWcfElement.cs
│       ├── LegacyWcfHost.cs
│       ├── LegacyWcfHostTimeouts.cs
│       ├── LegacyWcfBinding.cs
│       ├── LegacyWcfBindingCollection.cs
│       ├── LegacyWcfBindings.cs
│       ├── LegacyWcfBehavior.cs
│       ├── LegacyWcfBehaviorCollection.cs
│       ├── LegacyWcfBehaviors.cs
│       ├── LegacyWcfClient.cs
│       ├── LegacyWcfClientEndpoint.cs
│       ├── LegacyWcfClientEndpoints.cs
│       ├── LegacyWcfServiceHostingEnvironment.cs
│       ├── LegacyWcfService.cs
│       ├── LegacyWcfServiceEndpoint.cs
│       ├── LegacyWcfServiceEndpoints.cs
│       ├── LegacyWcfServices.cs
│       └── Internal/
│           ├── LegacyWcfRawElementBuilder.cs
│           └── LegacyWcfTypedModelBuilder.cs
│
├── tests/
│   └── LegacyWcf.Configuration.Tests/
│       ├── LegacyWcf.Configuration.Tests.csproj
│       └── LegacyWcfConfigurationReaderTests.cs
│
├── .gitignore
├── CHANGELOG.md
├── LegacyWcf.Configuration.slnx
├── LICENSE
├── README.md
├── merge.ps1
└── test.ps1
```


## Useful implementation prompt starter

Use this when starting a new implementation chat:

```text
You must read the attached files for context and reference. This project is LegacyWcf.Configuration, a modern .NET library for reading, preserving, modelling, and querying legacy WCF <system.serviceModel> configuration. It is not LegacyLens.NET.

Implement Phase 4 validation diagnostics, starting from the current post-Phase-3 implementation while preserving the design decisions in docs/ai-context.md and docs/architecture.md. The core package must target netstandard2.0;net8.0 and must not depend on CoreWCF. Unknown XML must be preserved and diagnostics should be permissive. Phase 4 should report duplicates and unresolved references without making well-formed but imperfect WCF XML unreadable. Public API files currently live at the project root, implementation-only helpers live under Internal/, nullable is enabled at project level, implicit usings are disabled, and explicit usings should be kept.
```
