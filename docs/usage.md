# Usage

This document shows how LegacyWcf.Configuration is intended to be used by application developers.

Phase 1 raw-reader APIs are implemented. Phase 2 Stage 1 typed service and endpoint APIs are implemented. Phase 2 Stage 2 typed service host, host base address, and host timeout APIs are implemented. Phase 2 Stage 3 initial typed binding APIs are implemented. Phase 2 Stage 4 initial typed behaviour APIs are implemented. Phase 2 Stage 5 typed client endpoint APIs are implemented. Phase 2 Stage 6 typed `serviceHostingEnvironment` APIs are implemented. Validation and lookup examples describe the intended developer experience for later phases.

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

Raw XML preservation remains the trust boundary for the library. Typed service, endpoint, and host models are additive views over the preserved raw tree.

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

## Phase 2 Stage 6 typed service, endpoint, host, binding, behaviour, client endpoint, and serviceHostingEnvironment usage

The following APIs are implemented. The current typed model includes services, service endpoints, service hosts, host base addresses, host timeouts, initial typed binding collections, initial typed behaviour collections, typed client endpoints, typed enumerable collections, typed service hosting environment settings, and raw XML fallback from those typed objects.

Stage 6 does not include `Find(...)`, `GetRequired(...)`, endpoint lookup helpers, binding lookup helpers, behaviour lookup helpers, client endpoint lookup helpers, validation diagnostics, CoreWCF mapping, code generation, or CLI tooling. Targeted lookup remains a later retrieval API concern.

## Enumerate services

```csharp
foreach (var service in config.Services)
{
    Console.WriteLine($"Service: {service.Name}");
    Console.WriteLine($"Behaviour: {service.BehaviorConfiguration}");
}
```

`config.Services` is a typed enumerable collection.

It supports:

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
foreach (var service in config.Services)
{
    Console.WriteLine($"Service: {service.Name}");

    if (service.Host is null)
    {
        continue;
    }

    foreach (var baseAddress in service.Host.BaseAddresses)
    {
        Console.WriteLine($"  Base address: {baseAddress}");
    }
}
```

The `Host` property is `null` when the source `<service>` element has no direct `<host>` child. If `<host>` exists but `<baseAddresses>` is missing, `service.Host` is populated and `service.Host.BaseAddresses.Count` is `0`.

Entries such as `<add />` without a `baseAddress` attribute are ignored by the typed `BaseAddresses` collection, but remain preserved through `service.Host.RawElement`.

## Read service host timeouts

Host timeout values are exposed as strings so the library preserves the original configuration values without enforcing WCF runtime parsing rules.

```csharp
foreach (var service in config.Services)
{
    var timeouts = service.Host?.Timeouts;

    if (timeouts is null)
    {
        continue;
    }

    Console.WriteLine($"Open timeout: {timeouts.OpenTimeout}");
    Console.WriteLine($"Close timeout: {timeouts.CloseTimeout}");
}
```

Unknown host child elements are not modelled as first-class typed objects in Stage 2, but they remain available through `service.Host.RawElement.Children`.

## Phase 2 Stage 3 usage: enumerate typed bindings

Phase 2 Stage 3 adds initial typed binding support without adding lookup helpers yet.

The implemented Stage 3 API exposes:

```text
config.Bindings.BasicHttp
config.Bindings.WsHttp
config.Bindings.NetTcp
config.Bindings.Custom
```

Each collection should support `Count`, indexed access, `foreach`, and LINQ through `IEnumerable`/`IReadOnlyList`.

```csharp
foreach (var binding in config.Bindings.BasicHttp)
{
    Console.WriteLine($"Binding type: {binding.BindingType}");
    Console.WriteLine($"Name: {binding.Name}");
    Console.WriteLine($"Max received message size: {binding.Attributes["maxReceivedMessageSize"]}");
    Console.WriteLine(binding.RawElement.RawXml);
}
```

```csharp
foreach (var binding in config.Bindings.WsHttp)
{
    Console.WriteLine($"Binding type: {binding.BindingType}");
    Console.WriteLine($"Name: {binding.Name}");
}
```

```csharp
foreach (var binding in config.Bindings.NetTcp)
{
    Console.WriteLine($"Binding type: {binding.BindingType}");
    Console.WriteLine($"Name: {binding.Name}");
    Console.WriteLine($"Port sharing: {binding.Attributes["portSharingEnabled"]}");
}
```

```csharp
foreach (var binding in config.Bindings.Custom)
{
    Console.WriteLine($"Binding type: {binding.BindingType}");
    Console.WriteLine($"Name: {binding.Name}");

    foreach (var child in binding.RawElement.Children)
    {
        Console.WriteLine($"  Raw child: {child.Name}");
    }
}
```

The Stage 3 binding model preserves all attributes from each `<binding>` element through `binding.Attributes`, including `name`, and preserves child elements such as `<security>`, `<textMessageEncoding>`, and `<httpTransport>` through `binding.RawElement.Children`.

If `<bindings>` is missing, `config.Bindings` should still be non-null and all known binding collections should be empty. If a known binding group exists but has no `<binding>` children, the corresponding collection should be empty.

If a `<binding>` is missing a `name` attribute, the binding should still be included in the typed collection with `Name == null`. Unknown binding groups should remain available through `config.RawSystemServiceModel` but should not be surfaced through Stage 3 typed binding collections.

## Future targeted binding lookup

A service endpoint usually references a binding type and optional named binding configuration. Lookup helpers are planned for a later retrieval API phase, not Phase 2 Stage 3.

```csharp
var endpoint = service.Endpoints.GetRequiredByContract(
    "MyCompany.Services.ICustomerService");

var binding = config.Bindings.GetRequired(
    endpoint.Binding,
    endpoint.BindingConfiguration);
```

## Phase 2 Stage 4 usage: enumerate typed behaviours

Services and endpoints can reference named behaviours through `behaviorConfiguration`. Phase 2 Stage 4 adds typed behaviour enumeration without adding lookup helpers yet.

The implemented Stage 4 API exposes:

```text
config.Behaviors.ServiceBehaviors
config.Behaviors.EndpointBehaviors
```

Each collection should support `Count`, indexed access, `foreach`, and LINQ through `IEnumerable`/`IReadOnlyList`.

```csharp
foreach (var behavior in config.Behaviors.ServiceBehaviors)
{
    Console.WriteLine($"Behaviour type: {behavior.BehaviorType}");
    Console.WriteLine($"Name: {behavior.Name}");

    foreach (var child in behavior.RawElement.Children)
    {
        Console.WriteLine($"  Raw child: {child.Name}");
    }
}
```

```csharp
foreach (var behavior in config.Behaviors.EndpointBehaviors)
{
    Console.WriteLine($"Behaviour type: {behavior.BehaviorType}");
    Console.WriteLine($"Name: {behavior.Name}");
    Console.WriteLine(behavior.RawElement.RawXml);
}
```

The Stage 4 behaviour model preserves all attributes from each `<behavior>` or `<behaviour>` element through `behavior.Attributes`, including `name`, and preserves child elements such as `<serviceMetadata>`, `<serviceDebug>`, and `<clientCredentials>` through `behavior.RawElement.Children`.

If `<behaviors>` or `<behaviours>` is missing, `config.Behaviors` should still be non-null and service and endpoint behaviour collections should be empty. If a behaviour is missing a `name` attribute, it should still be included in the typed collection with `Name == null`.

Lookup helpers such as `Find(...)` and `GetRequired(...)` are planned for a later retrieval API phase, not Stage 4.

## Phase 2 Stage 5 usage: read client endpoints

Legacy applications may contain WCF client configuration:

```xml
<client>
  <endpoint
    name="CustomerClient"
    address="http://localhost:8080/CustomerService"
    binding="basicHttpBinding"
    bindingConfiguration="CustomerBinding"
    contract="MyCompany.Services.ICustomerService"
    behaviorConfiguration="CustomerEndpointBehavior" />
</client>
```

Phase 2 Stage 5 exposes this through `config.Client` and a typed endpoint collection. `config.Client` should be `null` when `<client>` is missing. If `<client>` exists but contains no endpoint children, `config.Client` should be non-null and `config.Client.Endpoints.Count` should be `0`.

Usage:

```csharp
if (config.Client is not null)
{
    foreach (var endpoint in config.Client.Endpoints)
    {
        Console.WriteLine(endpoint.Name);
        Console.WriteLine(endpoint.Address);
        Console.WriteLine(endpoint.Binding);
        Console.WriteLine(endpoint.BindingConfiguration);
        Console.WriteLine(endpoint.Contract);
        Console.WriteLine(endpoint.BehaviorConfiguration);

        foreach (var attribute in endpoint.Attributes)
        {
            Console.WriteLine($"{attribute.Key}: {attribute.Value}");
        }

        Console.WriteLine(endpoint.RawElement.RawXml);
    }
}
```

The Stage 5 client endpoint model preserves all endpoint attributes through `endpoint.Attributes`, including unknown attributes. Missing optional attributes should produce `null` typed properties rather than a read failure. Unknown child elements under `<client>` should remain available through `config.Client.RawElement.Children` but should not be modelled as typed client endpoints in Stage 5.


## Phase 2 Stage 6 usage: read serviceHostingEnvironment

Phase 2 Stage 6 adds typed access to the first direct `<serviceHostingEnvironment>` element under `<system.serviceModel>`.

Example source XML:

```xml
<serviceHostingEnvironment
  aspNetCompatibilityEnabled="true"
  multipleSiteBindingsEnabled="true" />
```

Usage:

```csharp
var hosting = config.ServiceHostingEnvironment;

if (hosting is not null)
{
    Console.WriteLine(hosting.AspNetCompatibilityEnabled);
    Console.WriteLine(hosting.MultipleSiteBindingsEnabled);

    foreach (var attribute in hosting.Attributes)
    {
        Console.WriteLine($"{attribute.Key}: {attribute.Value}");
    }

    Console.WriteLine(hosting.RawElement.RawXml);
}
```

`config.ServiceHostingEnvironment` should be `null` when no direct `<serviceHostingEnvironment>` element exists. Attribute values should be preserved as strings exactly as they appear in the source XML. Unknown attributes should remain available through `hosting.Attributes`, and unknown child elements should remain available through `hosting.RawElement.Children`. Stage 6 does not validate boolean values and does not emit diagnostics for unknown attributes, unknown child elements, or duplicate `serviceHostingEnvironment` elements.

## Access raw XML fallback

Every typed object should keep a link to the raw XML element it came from.

```csharp
foreach (var service in config.Services)
{
    Console.WriteLine(service.RawElement.RawXml);

    if (service.Host is not null)
    {
        Console.WriteLine(service.Host.RawElement.RawXml);
    }
}
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
