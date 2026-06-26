# LegacyWcf.Configuration

LegacyWcf.Configuration is a modern .NET library for reading, preserving, modelling, and querying legacy Windows Communication Foundation (WCF) `<system.serviceModel>` configuration from `app.config`, `web.config`, and external `.config` files.

The first goal is not automatic migration. The first goal is to become a trusted reader for legacy WCF configuration so that developers can reliably retrieve the values that exist in old configuration files using a modern .NET API.

## Why this exists

Many legacy WCF applications contain important runtime behaviour in XML configuration:

```xml
<configuration>
  <system.serviceModel>
    <!-- services, endpoints, bindings, behaviours, host settings, client endpoints, etc. -->
  </system.serviceModel>
</configuration>
```

When modernising a WCF application, those values still matter. Some values can be mapped to CoreWCF or modern hosting APIs. Some need manual review. Some may be unsupported by a target platform but still need to be preserved and understood.

LegacyWcf.Configuration is intended to help developers inspect and use those values without requiring the legacy application to build, run, or load the original WCF runtime.

## Design principle

The library separates three concerns:

```text
Reading
Understanding
Applying
```

### Reading

Read the XML and preserve what exists.

### Understanding

Expose typed models and diagnostics for known WCF concepts.

### Applying

Optional future packages may help map the neutral model to CoreWCF setup code or generate migration snippets.

The core package focuses on reading and understanding. It should not depend on CoreWCF.

## Scope

The core package focuses on:

```text
configuration/system.serviceModel/**
```

The reader should preserve all descendant XML under `<system.serviceModel>`, including elements that are not yet strongly modelled.

High-value typed model areas include:

- services
- service endpoints
- service host settings
- host base addresses
- bindings
- service behaviours
- endpoint behaviours
- client endpoints
- serviceHostingEnvironment
- raw XML fallback
- diagnostics

Other sections such as `<appSettings>`, `<connectionStrings>`, `<system.web>`, and `<system.webServer>` may be useful application-hosting context later, but they are not the first focus of the core package.

## Target frameworks

The intended core package target frameworks are:

```xml
<TargetFrameworks>netstandard2.0;net8.0</TargetFrameworks>
```

This keeps the package usable from .NET Framework 4.8 applications through `netstandard2.0`, while also providing a native .NET 8 target for modern applications.

## Intended usage

The currently implemented API reads a config file, preserves the raw `<system.serviceModel>` XML tree, and exposes typed services, service endpoints, service hosts, host base addresses, host timeouts, and initial typed binding collections:

```csharp
using LegacyWcf.Configuration;

var result = LegacyWcfConfigurationReader.Read("web.config");

if (!result.Success)
{
    foreach (var diagnostic in result.Diagnostics)
    {
        Console.WriteLine($"{diagnostic.Severity}: {diagnostic.Message}");
    }

    return;
}

var config = result.Configuration!;
var raw = config.RawSystemServiceModel;

Console.WriteLine(raw.Name);
Console.WriteLine(raw.Path);
Console.WriteLine(raw.RawXml);

foreach (var service in config.Services)
{
    Console.WriteLine($"Service: {service.Name}");

    foreach (var endpoint in service.Endpoints)
    {
        Console.WriteLine($"  Contract: {endpoint.Contract}");
        Console.WriteLine($"  Binding: {endpoint.Binding}");
        Console.WriteLine($"  Binding configuration: {endpoint.BindingConfiguration}");
        Console.WriteLine($"  Address: {endpoint.Address}");
    }

    foreach (var baseAddress in service.Host?.BaseAddresses ?? Array.Empty<string>())
    {
        Console.WriteLine($"  Base address: {baseAddress}");
    }
}

foreach (var binding in config.Bindings.BasicHttp)
{
    Console.WriteLine($"Binding: {binding.Name}");
    Console.WriteLine($"Max size: {binding.Attributes["maxReceivedMessageSize"]}");
}
```

Future typed model APIs are intended to make common WCF concepts directly queryable:

```csharp
foreach (var service in config.Services)
{
    Console.WriteLine($"Service: {service.Name}");

    foreach (var endpoint in service.Endpoints)
    {
        Console.WriteLine($"  Contract: {endpoint.Contract}");
        Console.WriteLine($"  Binding: {endpoint.Binding}");
        Console.WriteLine($"  Binding configuration: {endpoint.BindingConfiguration}");
        Console.WriteLine($"  Address: {endpoint.Address}");
    }
}
```

Targeted lookup should also be supported in a later phase:

```csharp
var service = config.Services.GetRequired(
    "MyCompany.Services.CustomerService");

var endpoint = service.Endpoints.GetRequiredByContract(
    "MyCompany.Services.ICustomerService");

var binding = config.Bindings.GetRequired(
    endpoint.Binding,
    endpoint.BindingConfiguration);

var baseAddresses = service.Host?.BaseAddresses ?? [];
```


## Current implementation slice: Phase 2 Stage 2 typed services, endpoints, and hosts

**Phase 1: Full-fidelity reader is implemented.**

**Phase 2 Stage 1: Typed services and service endpoints are implemented.**

**Phase 2 Stage 2: Typed service hosts, host base addresses, and host timeouts are implemented.**

**Phase 2 Stage 3: Initial typed binding support is implemented.**

The reader now preserves the full raw `<system.serviceModel>` XML tree and builds additive typed models for the currently supported WCF concepts. Raw XML remains the source of truth, and every typed service, endpoint, host, and host timeout object keeps access to its source `LegacyWcfElement`.

The implemented API supports:

```csharp
var result = LegacyWcfConfigurationReader.Read("web.config");

if (result.Success)
{
    var config = result.Configuration!;

    foreach (var service in config.Services)
    {
        Console.WriteLine(service.Name);

        foreach (var endpoint in service.Endpoints)
        {
            Console.WriteLine(endpoint.Contract);
            Console.WriteLine(endpoint.Binding);
            Console.WriteLine(endpoint.BindingConfiguration);
            Console.WriteLine(endpoint.Address);
        }

        foreach (var baseAddress in service.Host?.BaseAddresses ?? Array.Empty<string>())
        {
            Console.WriteLine(baseAddress);
        }
    }
}
```

The current implementation provides:

- a simple `LegacyWcfConfigurationReader.Read(string filePath)` entry point
- a read result with `Success`, `Configuration`, and `Diagnostics`
- a recursive raw XML model that preserves element names, paths, attributes, children, values, raw XML, source file paths, and line numbers where practical
- diagnostics for missing files, unreadable files, malformed XML, missing `<configuration>`, and missing `<system.serviceModel>`
- typed enumerable services through `config.Services`
- typed enumerable service endpoints through `service.Endpoints`
- typed service host access through `service.Host`
- typed host base addresses through `service.Host.BaseAddresses`
- typed host timeouts through `service.Host.Timeouts`
- preservation of unknown custom elements and attributes
- no CoreWCF dependency in the core package

The implemented Stage 3 binding API adds typed binding models, typed binding collections, and a top-level `config.Bindings` container for the common binding groups `basicHttpBinding`, `wsHttpBinding`, `netTcpBinding`, and `customBinding`. Binding values are parsed from the preserved raw `LegacyWcfElement` tree, each typed binding retains its source raw `<binding>` element, and unknown binding groups or child elements remain preserved in raw XML. Stage 3 does not add behaviours, client endpoints, lookup APIs, validation diagnostics, CoreWCF mapping, code generation, or CLI tooling.

## Relationship to CoreWCF

LegacyWcf.Configuration is not a CoreWCF hosting package and does not depend on CoreWCF.

The core package reads, preserves, models, and queries legacy WCF `<system.serviceModel>` configuration. Optional CoreWCF mapping helpers may be added later in a separate package, for example:

```text
LegacyWcf.Configuration.CoreWcf
```

That future package could depend on CoreWCF and help map supported legacy configuration values into CoreWCF setup code.

## Documentation

Project documentation is available in the `docs/` folder:

- [AI Context](docs/ai-context.md)
- [Usage](docs/usage.md)
- [Architecture](docs/architecture.md)
- [Configuration Spec](docs/configuration-spec.md)
- [Roadmap](docs/roadmap.md)

## Repository shape

Current source layout:

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

Public API files currently live directly under `src/LegacyWcf.Configuration/`. Implementation-only helpers live under `Internal/`.

## Status

Phase 1 raw reader is implemented and covered by tests.

Phase 2 Stage 1 typed services and service endpoints are implemented and covered by tests.

Phase 2 Stage 2 typed service hosts, host base addresses, and host timeouts are implemented and covered by tests.

Phase 2 Stage 3 initial typed binding support is implemented and covered by tests.

Current test status:

- latest provided test run before Stage 3: 18 total, 18 passed, 0 failed, 0 skipped
- Stage 3 adds 8 binding tests, bringing the expected suite size to 26 tests
- the updated suite should be run locally with the .NET SDK after applying these changes

The completed Phase 2 Stage 3 slice adds `LegacyWcfBinding`, `LegacyWcfBindingCollection`, `LegacyWcfBindings`, and `LegacyWcfConfiguration.Bindings`, while keeping all lookup helpers and validation diagnostics for later phases. The next implementation step should be a later Phase 2 typed model slice, such as behaviours, client endpoints, or serviceHostingEnvironment. The project should continue to prioritise:

- full-fidelity XML preservation
- typed access to common WCF values
- permissive diagnostics
- low dependency weight
- clear tests
- no CoreWCF dependency in the core package

## License

This project is licensed under the Apache License, Version 2.0, January 2004.

See [LICENSE](LICENSE) for the full license text.
