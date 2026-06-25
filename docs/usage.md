# Usage

This document shows how LegacyWcf.Configuration is intended to be used by application developers.

Phase 1 raw-reader APIs are implemented. Typed service, endpoint, binding, behaviour, and lookup examples later in this document describe the intended developer experience for later phases.

## Install

When published to NuGet, the package should be installed as:

```powershell
dotnet add package LegacyWcf.Configuration
```

## Read a config file

```csharp
using LegacyWcf.Configuration;

var result = LegacyWcfConfigurationReader.Read("web.config");

if (!result.Success)
{
    foreach (var diagnostic in result.Diagnostics)
    {
        Console.WriteLine($"{diagnostic.Code}: {diagnostic.Severity}: {diagnostic.Message}");
    }

    return;
}

var config = result.Configuration!;
```

The reader supports `app.config`, `web.config`, and external `.config` files.


## Phase 1 usage: read the raw system.serviceModel tree

The implemented Phase 1 API supports reading the raw `<system.serviceModel>` tree before any typed WCF model is added.

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

var raw = result.Configuration!.RawSystemServiceModel;

Console.WriteLine(raw.Name);
Console.WriteLine(raw.Path);
Console.WriteLine(raw.RawXml);
```

During Phase 1, the main consumer-facing value is raw XML preservation. Typed service, endpoint, binding, behaviour, and lookup examples later in this document describe the intended developer experience for later phases.

## Phase 1 diagnostics

Phase 1 returns diagnostics instead of throwing for normal read outcomes such as:

- file not found
- file cannot be read
- malformed XML
- missing `<configuration>`
- missing `<system.serviceModel>`

Malformed XML and unreadable files return `Success == false`. Missing `<system.serviceModel>` also returns `Success == false`, but is reported clearly and is not confused with malformed XML.

## Inspect diagnostics

The reader should be permissive. It should preserve what it can and report diagnostics for issues that need attention.

```csharp
foreach (var diagnostic in result.Diagnostics)
{
    Console.WriteLine($"{diagnostic.Severity}: {diagnostic.Message}");
}
```

Phase 1 diagnostic codes are:

| Code | Meaning |
|---|---|
| `LWC0001` | Missing/blank path or file not found. |
| `LWC0002` | File could not be read. |
| `LWC0003` | XML could not be loaded or parsed. |
| `LWC0004` | Root `<configuration>` element was not found. |
| `LWC0005` | `<system.serviceModel>` was not found under `<configuration>`. |

Future diagnostics may include:

- duplicate named services
- duplicate named bindings
- unknown elements preserved
- unknown attributes preserved
- references to missing binding configurations
- references to missing behaviour configurations

## Phase 2 Stage 1 planned typed service and endpoint usage

The following examples describe the intended developer experience once Phase 2 Stage 1 is implemented. Stage 1 is limited to typed services, typed service endpoints, typed enumerable collections, and raw XML fallback from those typed objects.

Stage 1 does not include `Find(...)`, `GetRequired(...)`, endpoint lookup helpers, host/base address models, bindings, behaviours, client endpoints, validation diagnostics, CoreWCF mapping, code generation, or CLI tooling.

## Enumerate services

```csharp
foreach (var service in config.Services)
{
    Console.WriteLine($"Service: {service.Name}");
    Console.WriteLine($"Behaviour: {service.BehaviorConfiguration}");
}
```

`config.Services` should be a typed enumerable collection.

For Phase 2 Stage 1 it should support:

- `Count`
- indexed access
- `foreach`
- LINQ through `IEnumerable`/`IReadOnlyList`

Lookup helpers such as `Find(...)` and `GetRequired(...)` are planned for a later retrieval API phase.

## Enumerate service endpoints

```csharp
foreach (var service in config.Services)
{
    Console.WriteLine($"Service: {service.Name}");

    foreach (var endpoint in service.Endpoints)
    {
        Console.WriteLine($"  Name: {endpoint.Name}");
        Console.WriteLine($"  Address: {endpoint.Address}");
        Console.WriteLine($"  Binding: {endpoint.Binding}");
        Console.WriteLine($"  Binding configuration: {endpoint.BindingConfiguration}");
        Console.WriteLine($"  Contract: {endpoint.Contract}");
        Console.WriteLine($"  Behaviour configuration: {endpoint.BehaviorConfiguration}");
    }
}
```

## Future targeted service lookup

```csharp
var service = config.Services.GetRequired(
    "MyCompany.Services.CustomerService");
```

`GetRequired(...)` should return the typed object and fail clearly if the requested item is not present.

```csharp
var service = config.Services.Find(
    "MyCompany.Services.CustomerService");

if (service is null)
{
    Console.WriteLine("Service was not found.");
}
```

`Find(...)` should return `null` when the item does not exist.

## Future targeted endpoint lookup

A service endpoint collection should support common WCF lookup patterns.

```csharp
var endpoint = service.Endpoints.GetRequiredByContract(
    "MyCompany.Services.ICustomerService");
```

Possible endpoint lookup helpers:

```csharp
service.Endpoints.FindByName("CustomerEndpoint");
service.Endpoints.FindByContract("MyCompany.Services.ICustomerService");
service.Endpoints.GetRequiredByName("CustomerEndpoint");
service.Endpoints.GetRequiredByContract("MyCompany.Services.ICustomerService");
```

The final API should avoid unnecessary complexity, but common WCF identifiers should be easy to query.

## Read service host base addresses

Legacy WCF service configuration may include `<host>` settings.

```csharp
var service = config.Services.GetRequired(
    "MyCompany.Services.CustomerService");

var baseAddresses = service.Host?.BaseAddresses ?? [];

foreach (var baseAddress in baseAddresses)
{
    Console.WriteLine(baseAddress);
}
```

Even if later CoreWCF mapping does not directly consume `<host>` configuration, the core package should still read and preserve it.

## Read bindings

A service endpoint usually references a binding type and optional named binding configuration.

```csharp
var endpoint = service.Endpoints.GetRequiredByContract(
    "MyCompany.Services.ICustomerService");

var binding = config.Bindings.GetRequired(
    endpoint.Binding,
    endpoint.BindingConfiguration);
```

The binding model should preserve both typed high-value attributes and raw XML.

## Read behaviours

Services and endpoints can reference named behaviours.

```csharp
var serviceBehavior = config.Behaviors.ServiceBehaviors.Find(
    service.BehaviorConfiguration);

var endpointBehavior = config.Behaviors.EndpointBehaviors.Find(
    endpoint.BehaviorConfiguration);
```

Behaviour elements may contain many child elements. The typed model should expose common identifiers while retaining raw XML fallback.

## Read client endpoints

Legacy applications may contain WCF client configuration:

```xml
<client>
  <endpoint
    name="CustomerClient"
    address="http://localhost:8080/CustomerService"
    binding="basicHttpBinding"
    bindingConfiguration="CustomerBinding"
    contract="MyCompany.Services.ICustomerService" />
</client>
```

Intended usage:

```csharp
foreach (var endpoint in config.Client?.Endpoints ?? [])
{
    Console.WriteLine(endpoint.Name);
    Console.WriteLine(endpoint.Address);
    Console.WriteLine(endpoint.Binding);
    Console.WriteLine(endpoint.BindingConfiguration);
    Console.WriteLine(endpoint.Contract);
}
```

## Access raw XML fallback

Every typed object should keep a link to the raw XML element it came from.

```csharp
var service = config.Services.GetRequired(
    "MyCompany.Services.CustomerService");

Console.WriteLine(service.RawElement.RawXml);
```

This is important because not every WCF element will be strongly modelled in the first version.

The raw fallback lets a developer retrieve values even when the typed model does not yet understand a particular element.

## Use with future CoreWCF mapping

The core package should not depend on CoreWCF.

A later optional package may provide mapping helpers:

```text
LegacyWcf.Configuration.CoreWcf
```

Future usage might look like this:

```csharp
app.UseServiceModel(serviceBuilder =>
{
    serviceBuilder.AddService<CustomerService>();

    serviceBuilder.AddServiceEndpoint<CustomerService, ICustomerService>(
        binding.ToCoreWcfBinding(),
        endpoint.ResolveAddress(service.Host));
});
```

This is not part of the first package. The first package should simply make the legacy values available reliably.
