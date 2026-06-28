# LegacyWcf.Configuration

LegacyWcf.Configuration is a modern .NET library for reading, preserving, modelling, and querying legacy Windows Communication Foundation (WCF) `<system.serviceModel>` configuration from `app.config`, `web.config`, and external `.config` files.

The first goal is not automatic migration. The first goal is to become a trusted reader for legacy WCF configuration so that developers can reliably retrieve the values that exist in old configuration files using a modern .NET API.

## Why this exists

Many legacy WCF applications contain important runtime behaviour in XML configuration:

```xml
<configuration>
  <system.serviceModel>
    <!-- services, endpoints, bindings, behaviours, host settings, client endpoints, service hosting environment settings, etc. -->
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

High-value typed model and reader areas include:

- services
- service endpoints
- service host settings
- host base addresses
- bindings
- service behaviours
- endpoint behaviours
- client endpoints
- serviceHostingEnvironment
- retrieval APIs for common lookups
- raw XML fallback
- permissive diagnostics

Other sections such as `<appSettings>`, `<connectionStrings>`, `<system.web>`, and `<system.webServer>` may be useful application-hosting context later, but they are not the first focus of the core package.

## Target frameworks

The intended core package target frameworks are:

```xml
<TargetFrameworks>netstandard2.0;net8.0</TargetFrameworks>
```

This keeps the package usable from .NET Framework 4.8 applications through `netstandard2.0`, while also providing a native .NET 8 target for modern applications.

## Intended usage

The currently implemented API reads a config file, preserves the raw `<system.serviceModel>` XML tree, and exposes typed services, service endpoints, service hosts, host base addresses, host timeouts, initial typed binding collections, initial typed behaviour collections, typed client endpoints, and typed service hosting environment settings:

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

foreach (var behavior in config.Behaviors.ServiceBehaviors)
{
    Console.WriteLine($"Service behaviour: {behavior.Name}");

    foreach (var child in behavior.RawElement.Children)
    {
        Console.WriteLine($"  Raw child: {child.Name}");
    }
}

foreach (var behavior in config.Behaviors.EndpointBehaviors)
{
    Console.WriteLine($"Endpoint behaviour: {behavior.Name}");
}

if (config.Client is not null)
{
    foreach (var endpoint in config.Client.Endpoints)
    {
        Console.WriteLine($"Client endpoint: {endpoint.Name}");
        Console.WriteLine($"  Contract: {endpoint.Contract}");
        Console.WriteLine($"  Binding: {endpoint.Binding}");
        Console.WriteLine($"  Binding configuration: {endpoint.BindingConfiguration}");
        Console.WriteLine($"  Address: {endpoint.Address}");
    }
}

if (config.ServiceHostingEnvironment is not null)
{
    Console.WriteLine($"ASP.NET compatibility: {config.ServiceHostingEnvironment.AspNetCompatibilityEnabled}");
    Console.WriteLine($"Multiple site bindings: {config.ServiceHostingEnvironment.MultipleSiteBindingsEnabled}");
}
```

Common WCF concepts are now directly queryable through Phase 3 retrieval APIs:

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

Targeted lookup is supported by the Phase 3 retrieval APIs:

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


## Current implementation slice: Phase 4 validation diagnostics

**Phase 1: Full-fidelity reader is implemented.**

**Phase 2 Stage 1: Typed services and service endpoints are implemented.**

**Phase 2 Stage 2: Typed service hosts, host base addresses, and host timeouts are implemented.**

**Phase 2 Stage 3: Initial typed binding support is implemented.**

**Phase 2 Stage 4: Initial typed behaviour support is implemented.**

**Phase 2 Stage 5: Typed client endpoint support is implemented.**

**Phase 2 Stage 6: Typed `serviceHostingEnvironment` support is implemented.**

**Phase 3: Retrieval APIs are implemented.**

**Phase 4: Validation and diagnostics are implemented.**

The reader now preserves the full raw `<system.serviceModel>` XML tree and builds additive typed models for the currently supported WCF concepts. Raw XML remains the source of truth, and every typed service, endpoint, host, host timeout, binding, behaviour, client, client endpoint, and service hosting environment object keeps access to its source `LegacyWcfElement`.

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

    if (config.Client is not null)
    {
        foreach (var endpoint in config.Client.Endpoints)
        {
            Console.WriteLine(endpoint.Contract);
            Console.WriteLine(endpoint.Binding);
            Console.WriteLine(endpoint.BindingConfiguration);
            Console.WriteLine(endpoint.Address);
        }
    }
}
```

The current package/version context is:

- current NuGet package version: `0.4.0`
- latest provided test run: 78 total, 78 passed, 0 failed, 0 skipped

The current implementation provides:

- a simple `LegacyWcfConfigurationReader.Read(string filePath)` entry point
- a read result with `Success`, `Configuration`, and `Diagnostics`
- a recursive raw XML model that preserves element names, paths, attributes, children, values, raw XML, source file paths, and line numbers where practical
- diagnostics for missing files, unreadable files, malformed XML, missing `<configuration>`, and missing `<system.serviceModel>`
- permissive validation diagnostics for duplicate names, unresolved references, duplicate `serviceHostingEnvironment`, and unknown preserved elements
- typed enumerable services through `config.Services`
- typed enumerable service endpoints through `service.Endpoints`
- typed service host access through `service.Host`
- typed host base addresses through `service.Host.BaseAddresses`
- typed host timeouts through `service.Host.Timeouts`
- preservation of unknown custom elements and attributes
- typed binding access through `config.Bindings`
- typed binding groups for `BasicHttp`, `WsHttp`, `NetTcp`, and `Custom`
- typed behaviour access through `config.Behaviors`
- typed service behaviour and endpoint behaviour collections through `ServiceBehaviors` and `EndpointBehaviors`
- typed client access through `config.Client`
- typed client endpoint enumeration through `config.Client.Endpoints`
- typed service hosting environment access through `config.ServiceHostingEnvironment`
- no CoreWCF dependency in the core package

The implemented Stage 3 binding API adds typed binding models, typed binding collections, and a top-level `config.Bindings` container for the common binding groups `basicHttpBinding`, `wsHttpBinding`, `netTcpBinding`, and `customBinding`. Binding values are parsed from the preserved raw `LegacyWcfElement` tree, each typed binding retains its source raw `<binding>` element, and unknown binding groups or child elements remain preserved in raw XML.

The implemented Stage 4 behaviour API adds typed behaviour models, typed behaviour collections, and a top-level `config.Behaviors` container for service behaviours and endpoint behaviours. Behaviour values are parsed from the preserved raw `LegacyWcfElement` tree, each typed behaviour retains its source raw `<behavior>` or `<behaviour>` element, and unknown behaviour groups or child elements remain preserved in raw XML.

The implemented Stage 5 client endpoint API adds typed client models, typed client endpoint models, typed client endpoint collections, and a top-level `config.Client` property. Client endpoint values are parsed from the preserved raw `LegacyWcfElement` tree, each typed client endpoint retains its source raw `<endpoint>` element, all source endpoint attributes are preserved through `endpoint.Attributes`, and unknown child elements under `<client>` remain preserved in raw XML.

The implemented Stage 6 service hosting environment API adds a typed `LegacyWcfServiceHostingEnvironment` model and a top-level `config.ServiceHostingEnvironment` property. Values are parsed from the preserved raw `LegacyWcfElement` tree, `aspNetCompatibilityEnabled` and `multipleSiteBindingsEnabled` are preserved as strings, all source attributes are preserved through `Attributes`, and unknown child elements remain preserved through `RawElement.Children`. Stage 6 did not add lookup APIs, validation diagnostics, CoreWCF mapping, code generation, or CLI tooling. Phase 3 now adds lookup APIs only.

## Current retrieval APIs: Phase 3

Phase 3 retrieval APIs are implemented. This is an additive API phase on top of the existing typed model. It makes common WCF lookups easy without changing reader behaviour, parser flow, raw XML preservation, diagnostics, or CoreWCF boundaries.

Phase 3 adds lookup helpers where the existing typed collections already expose enough data to support them:

```csharp
var service = config.Services.GetRequired(
    "MyCompany.Services.CustomerService");

var serviceEndpoint = service.Endpoints.GetRequiredByContract(
    "MyCompany.Services.ICustomerService");

var binding = config.Bindings.GetRequired(
    serviceEndpoint.Binding,
    serviceEndpoint.BindingConfiguration);

var serviceBehavior = config.Behaviors.ServiceBehaviors.GetRequired(
    service.BehaviorConfiguration);

var clientEndpoint = config.Client?.Endpoints.GetRequiredByContract(
    "MyCompany.Services.ICustomerService");
```

Implemented Phase 3 lookup helpers include:

- `config.Services.Find(name)` and `config.Services.GetRequired(name)`
- `service.Endpoints.FindByName(name)` and `service.Endpoints.GetRequiredByName(name)`
- `service.Endpoints.FindByContract(contract)` and `service.Endpoints.GetRequiredByContract(contract)`
- `config.Bindings.Find(bindingType, name)` and `config.Bindings.GetRequired(bindingType, name)`
- `config.Bindings.BasicHttp.Find(name)` and `config.Bindings.BasicHttp.GetRequired(name)`, with the same collection-level pattern for other known binding groups
- `config.Behaviors.ServiceBehaviors.Find(name)` and `config.Behaviors.ServiceBehaviors.GetRequired(name)`
- `config.Behaviors.EndpointBehaviors.Find(name)` and `config.Behaviors.EndpointBehaviors.GetRequired(name)`
- `config.Client?.Endpoints.FindByName(name)` and `config.Client?.Endpoints.GetRequiredByName(name)`
- `config.Client?.Endpoints.FindByContract(contract)` and `config.Client?.Endpoints.GetRequiredByContract(contract)`

Find-style helpers return `null` when no match exists. Required helpers throw a clear `InvalidOperationException` when the requested object cannot be found. Matching is case-insensitive for WCF names and identifiers. If duplicates exist, Phase 3 returns the first matching object and leaves duplicate diagnostics to Phase 4 validation.

## Current validation diagnostics: Phase 4

Phase 4 adds permissive validation diagnostics on top of the existing raw and typed model. It does not weaken XML preservation, remove existing typed values, or make valid-but-imperfect legacy WCF configuration unreadable.

The intended Phase 4 behaviour is:

```text
Read what can be read.
Preserve what is unknown.
Report diagnostics.
Only fail when the file cannot be read, the XML cannot be parsed, <configuration> is missing, or <system.serviceModel> is missing.
```

Implemented Phase 4 diagnostics cover duplicate named services, duplicate named bindings within the same binding type, duplicate named service behaviours, duplicate named endpoint behaviours, endpoint references to missing binding configurations, endpoint references to missing endpoint behaviours, service references to missing service behaviours, duplicate direct `serviceHostingEnvironment` elements, and unknown or unsupported raw elements preserved for review.

Phase 4 remains additive. Existing Phase 1, Phase 2, and Phase 3 public APIs continue to work. Lookup helpers still return the first matching object where duplicates exist; duplicate reporting belongs in diagnostics rather than lookup behaviour.

Phase 4 validation diagnostic codes are:

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
│           ├── LegacyWcfTypedModelBuilder.cs
│           └── LegacyWcfConfigurationValidator.cs
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

Phase 2 Stage 4 initial typed behaviour support is implemented and covered by tests.

Phase 2 Stage 5 typed client endpoint support is implemented and covered by tests.

Phase 2 Stage 6 typed `serviceHostingEnvironment` support is implemented and covered by tests.

Phase 3 retrieval APIs are implemented and covered by tests.

Phase 4 validation diagnostics are implemented and covered by tests.

Current package/version context:

- current NuGet package version: `0.4.0`
- latest provided full test run: 78 total, 78 passed, 0 failed, 0 skipped

The next implementation step should be decided after the `v0.4.0` release is reviewed. The project should continue to prioritise:

- full-fidelity XML preservation
- typed access to common WCF values
- retrieval APIs for common lookups
- permissive diagnostics
- low dependency weight
- clear tests
- no CoreWCF dependency in the core package

## License

This project is licensed under the Apache License, Version 2.0, January 2004.

See [LICENSE](LICENSE) for the full license text.
