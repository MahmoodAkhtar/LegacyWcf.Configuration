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

## Suggested project structure

```text
src/
└── LegacyWcf.Configuration/
    ├── Reading/
    ├── Raw/
    ├── Model/
    ├── Parsing/
    ├── Diagnostics/
    └── Internal/
```

Suggested responsibilities:

```text
Reading
- public entry point
- file loading
- read result
- options
- exceptions

Raw
- full-fidelity XML model
- raw element representation
- source location

Model
- typed WCF configuration model
- services
- endpoints
- bindings
- behaviours
- client endpoints
- service hosting environment

Parsing
- XML to raw model
- raw model to typed model
- focused parsers for services, bindings, behaviours, client

Diagnostics
- diagnostic model
- severity
- codes
- diagnostic helpers

Internal
- XML helpers
- path helpers
- string comparers
```

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

Recommended flow:

```text
File path
  -> load XML document
  -> locate <configuration>
  -> locate <system.serviceModel>
  -> build full raw LegacyWcfElement tree
  -> parse typed model from raw tree
  -> attach diagnostics
  -> return result
```

The raw model should be created before typed parsing so that preservation does not depend on typed support.


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
