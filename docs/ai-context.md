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
- permissive diagnostics
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

The current code reads and preserves the raw `<system.serviceModel>` XML tree. It does not implement typed services, typed endpoints, typed bindings, typed behaviours, lookup APIs, CoreWCF mapping, code generation, or CLI tooling.

Implemented first useful output:

```csharp
var result = LegacyWcfConfigurationReader.Read("web.config");

if (result.Success)
{
    var raw = result.Configuration!.RawSystemServiceModel;
}
```

Current Phase 1 files:

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

Public API files should remain at the project root for Phase 1. Do not reintroduce public API folders such as `Reading/`, `Raw/`, `Model/`, or `Diagnostics/` unless the project has grown enough that the extra structure clearly improves maintainability.

Current Phase 1 diagnostics cover:

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

Current Phase 1 tests cover:

- valid `<system.serviceModel>` raw tree preservation
- missing file diagnostics
- malformed XML diagnostics
- missing `<configuration>` diagnostics
- missing `<system.serviceModel>` diagnostics
- unknown custom element and attribute preservation

Current test status:

- total tests: 6
- passed: 6
- failed: 0

Future AI implementation chats should preserve this boundary: raw XML preservation first, typed parsing later.

## Current next implementation slice

The next implementation step is **Phase 2 Stage 1: typed services and service endpoints**.

Phase 2 should build typed models on top of the preserved raw tree. Every typed object should retain access to its source `LegacyWcfElement`.

Stage 1 is limited to:

- typed services
- typed service endpoints
- typed enumerable service collection
- typed enumerable endpoint collection
- every typed object retaining access to its source `LegacyWcfElement`
- wiring `LegacyWcfConfiguration.Services` into the configuration model
- tests for typed service and endpoint parsing

Stage 1 should not implement:

- host model
- host base addresses
- host timeouts
- bindings
- behaviours
- client endpoints
- serviceHostingEnvironment
- `Find(...)`
- `GetRequired(...)`
- endpoint lookup helpers
- validation diagnostics for duplicates or missing references
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

For Phase 2 Stage 1, the public model shape should be limited to:

```text
LegacyWcfService
- string Name
- string? BehaviorConfiguration
- LegacyWcfServiceEndpoints Endpoints
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
```

`LegacyWcfConfiguration` should expose `LegacyWcfServices Services`, defaulting to an empty collection when no `<services>` element exists.

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
│       └── Internal/
│           └── LegacyWcfRawElementBuilder.cs
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

Implement the next slice of the MVP, starting from the current post-Phase-1 raw-reader implementation while preserving the design decisions in docs/ai-context.md and docs/architecture.md. The core package must target netstandard2.0;net8.0 and must not depend on CoreWCF. Unknown XML must be preserved and diagnostics should be permissive. Public API files currently live at the project root, implementation-only helpers live under Internal/, nullable is enabled at project level, implicit usings are disabled, and explicit usings should be kept.
```
