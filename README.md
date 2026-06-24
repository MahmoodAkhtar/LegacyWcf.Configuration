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

var config = result.Configuration;

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

Targeted lookup should also be supported:

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

Suggested initial repository structure:

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

## Status

This project is at the design/startup stage. The initial implementation should prioritise:

- full-fidelity XML preservation
- typed access to common WCF values
- permissive diagnostics
- low dependency weight
- clear tests
- no CoreWCF dependency in the core package

## License

This project is licensed under the Apache License, Version 2.0, January 2004.

See [LICENSE](LICENSE) for the full license text.
