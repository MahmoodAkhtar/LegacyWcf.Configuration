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

Suggested solution name:

```text
LegacyWcf.Configuration.sln
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

## Suggested initial repository structure

```text
LegacyWcf.Configuration/
├── README.md
├── CHANGELOG.md
├── docs/
│   ├── ai-context.md
│   ├── usage.md
│   ├── architecture.md
│   ├── configuration-spec.md
│   └── roadmap.md
├── src/
│   └── LegacyWcf.Configuration/
├── tests/
│   └── LegacyWcf.Configuration.Tests/
└── samples/
    └── LegacyWcf.Configuration.SampleConsole/
```

## Useful implementation prompt starter

Use this when starting a new implementation chat:

```text
You must read the attached files for context and reference. This project is LegacyWcf.Configuration, a modern .NET library for reading, preserving, modelling, and querying legacy WCF <system.serviceModel> configuration. It is not LegacyLens.NET.

Implement the next slice of the MVP while preserving the design decisions in docs/ai-context.md and docs/architecture.md. The core package must target netstandard2.0;net8.0 and must not depend on CoreWCF. Unknown XML must be preserved and diagnostics should be permissive.
```
