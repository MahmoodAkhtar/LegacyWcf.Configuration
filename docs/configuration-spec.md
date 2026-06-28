# Configuration Spec

This document is a living behavioural specification for how LegacyWcf.Configuration should read representative legacy WCF XML configuration shapes.

It is not a replacement for the official Microsoft WCF configuration schema documentation. The official schema describes WCF configuration elements and their WCF meaning. This document describes how this library should behave when it reads selected configuration shapes.

Each scenario should answer:

```text
Given this WCF XML,
what typed model should the library produce?
what raw XML must be preserved?
what diagnostics should be emitted?
```

## General rules

The reader should:

- locate `<system.serviceModel>` under `<configuration>`
- preserve all descendants of `<system.serviceModel>` in the raw model
- expose common WCF concepts through typed models
- preserve unknown elements and attributes
- add diagnostics where useful
- avoid failing for unsupported but well-formed XML


## Phase 1 behaviour contract: raw reader only

Phase 1 implements the full-fidelity raw reader only. This phase is now implemented in the current codebase.

For Phase 1, scenarios in this document are used primarily to prove that the raw `<system.serviceModel>` tree is preserved. Typed model expectations describe later behaviour unless explicitly stated otherwise.

The implemented public API is:

```csharp
var result = LegacyWcfConfigurationReader.Read("web.config");
var raw = result.Configuration!.RawSystemServiceModel;
```

Phase 1 should produce:

```text
result.Success == true
result.Configuration is not null
result.Configuration.RawSystemServiceModel.Name == "system.serviceModel"
```

Phase 1 should preserve:

- all descendant elements under `<system.serviceModel>`
- all attributes on those elements
- raw XML for each element
- source file path on raw elements
- line number where practical
- unknown custom elements and attributes

Phase 1 should not yet require:

- typed services
- typed endpoints
- typed bindings
- typed behaviours
- typed client endpoint collections
- `Find(...)` or `GetRequired(...)` lookup APIs
- CoreWCF mapping
- code generation

## Phase 1 diagnostics contract

The Phase 1 reader returns diagnostics for:

- missing or blank file path
- file not found
- file cannot be read
- malformed XML
- missing `<configuration>`
- missing `<system.serviceModel>`

Current diagnostic codes are:

| Code | Meaning |
|---|---|
| `LWC0001` | Missing/blank path or file not found. |
| `LWC0002` | File could not be read. |
| `LWC0003` | XML could not be loaded or parsed. |
| `LWC0004` | Root `<configuration>` element was not found. |
| `LWC0005` | `<system.serviceModel>` was not found under `<configuration>`. |

Malformed XML returns `Success == false` and no typed or raw configuration. Missing `<system.serviceModel>` also returns `Success == false`, but it is reported clearly and is not treated as malformed XML.

## Phase 2 Stage 1 typed model contract

Phase 2 Stage 1 should add typed models for services and service endpoints only. It should be additive on top of the Phase 1 raw reader and must not weaken raw XML preservation.

Implemented Stage 1 behaviour should include:

- `config.Services` as a typed enumerable collection
- `service.Endpoints` as a typed enumerable collection
- `LegacyWcfService.Name`
- `LegacyWcfService.BehaviorConfiguration`
- `LegacyWcfService.RawElement`
- `LegacyWcfServiceEndpoint.Name`
- `LegacyWcfServiceEndpoint.Address`
- `LegacyWcfServiceEndpoint.Binding`
- `LegacyWcfServiceEndpoint.BindingConfiguration`
- `LegacyWcfServiceEndpoint.Contract`
- `LegacyWcfServiceEndpoint.BehaviorConfiguration`
- `LegacyWcfServiceEndpoint.RawElement`

If `<services>` is missing, `config.Services.Count` should be `0` while the raw `<system.serviceModel>` model remains available.

If a `<service>` is missing a `name` attribute, the typed service should be preserved with `Name == ""` rather than failing.

If an `<endpoint>` is missing optional attributes, the corresponding typed properties should be `null`.

Unknown service child elements should be preserved through `service.RawElement`. Stage 1 did not expose typed host, binding, behaviour, client endpoint, or lookup APIs.

## Phase 2 Stage 2 typed host model contract

Phase 2 Stage 2 adds typed service host support on top of the Stage 1 service model. It is additive and must not weaken raw XML preservation.

Implemented Stage 2 behaviour includes:

- `LegacyWcfService.Host`
- `LegacyWcfHost.BaseAddresses`
- `LegacyWcfHost.Timeouts`
- `LegacyWcfHost.RawElement`
- `LegacyWcfHostTimeouts.OpenTimeout`
- `LegacyWcfHostTimeouts.CloseTimeout`
- `LegacyWcfHostTimeouts.RawElement`

If `<host>` is missing, `service.Host` should be `null`.

If `<host>` exists but `<baseAddresses>` is missing, `service.Host` should be populated and `service.Host.BaseAddresses.Count` should be `0`.

If `<baseAddresses>` contains an `<add>` element without a `baseAddress` attribute, that entry should be ignored by the typed `BaseAddresses` collection while the raw `<add>` element remains preserved through `service.Host.RawElement`.

Base address values should be preserved as strings. Stage 2 should not validate URI format.

Unknown host child elements should be preserved through `service.Host.RawElement.Children` and should not cause a read failure.

Stage 2 does not add typed bindings, behaviours, client endpoints, serviceHostingEnvironment, lookup APIs, validation diagnostics, CoreWCF mapping, code generation, or CLI tooling.


## Phase 2 Stage 3 typed binding model contract

Phase 2 Stage 3 adds initial typed models for common WCF binding groups only. It is implemented and remains additive on top of the preserved raw model.

Stage 3 adds:

- `config.Bindings`
- `LegacyWcfBinding`
- `LegacyWcfBindingCollection`
- `LegacyWcfBindings`
- `config.Bindings.BasicHttp`
- `config.Bindings.WsHttp`
- `config.Bindings.NetTcp`
- `config.Bindings.Custom`

`LegacyWcfBinding` should expose:

```text
BindingType
Name
Attributes
RawElement
```

`BindingType` should contain the parent binding group name, such as `basicHttpBinding`, `wsHttpBinding`, `netTcpBinding`, or `customBinding`. `Name` should come from the `<binding name="..." />` attribute and should be `null` when the attribute is missing. `Attributes` preserves all attributes from the `<binding>` element, including `name`. `RawElement` points to the preserved raw `<binding>` element.

If `<bindings>` is missing, `config.Bindings` should be non-null and all known binding collections should be empty. If a known binding group exists but has no direct `<binding>` children, the corresponding typed collection should be empty.

Unknown binding groups should be preserved in `config.RawSystemServiceModel` but should not be surfaced through Stage 3 typed binding collections. Unknown child elements inside a `<binding>` should be preserved through `binding.RawElement.Children`.

Stage 3 does not emit diagnostics for unnamed bindings, unknown binding groups, unknown binding child elements, duplicate bindings, or endpoints referencing missing binding configurations. Validation belongs to a later phase.


## Phase 2 Stage 4 typed behaviour model contract

Phase 2 Stage 4 adds initial typed models for WCF service and endpoint behaviours only. It is implemented and remains additive on top of the preserved raw model.

Stage 4 adds:

- `config.Behaviors`
- `LegacyWcfBehavior`
- `LegacyWcfBehaviorCollection`
- `LegacyWcfBehaviors`
- `config.Behaviors.ServiceBehaviors`
- `config.Behaviors.EndpointBehaviors`

`LegacyWcfBehavior` exposes:

```text
BehaviorType
Name
Attributes
RawElement
```

`BehaviorType` uses the normalized singular values `serviceBehavior` and `endpointBehavior`. `Name` should come from the `<behavior name="..." />` or `<behaviour name="..." />` attribute and should be `null` when the attribute is missing. `Attributes` preserves all attributes from the source behaviour element, including `name`. `RawElement` points to the preserved raw behaviour element.

If `<behaviors>` or `<behaviours>` is missing, `config.Behaviors` is non-null and both known behaviour collections should be empty. If a known behaviour group exists but has no direct behaviour children, the corresponding typed collection should be empty.

Stage 4 supports British legacy/custom spelling aliases for raw XML where they appear:

```text
behaviors / behaviours
serviceBehaviors / serviceBehaviours
endpointBehaviors / endpointBehaviours
behavior / behaviour
```

The public API should keep American spelling: `LegacyWcfBehavior`, `LegacyWcfBehaviorCollection`, `LegacyWcfBehaviors`, `ServiceBehaviors`, and `EndpointBehaviors`.

Unknown behaviour groups are preserved in `config.RawSystemServiceModel` but should not be surfaced through Stage 4 typed behaviour collections. Unknown child elements inside a behaviour should be preserved through `behavior.RawElement.Children`.

Stage 4 does not emit diagnostics for unnamed behaviours, unknown behaviour groups, unknown behaviour child elements, duplicate behaviours, or services/endpoints referencing missing behaviour configurations. Validation belongs to a later phase.

## Phase 2 Stage 5 typed client endpoint model contract

Phase 2 Stage 5 is implemented and adds typed models for WCF client endpoints only. It remains additive on top of the preserved raw model and must not weaken raw XML preservation.

Stage 5 adds:

- `config.Client`
- `LegacyWcfClient`
- `LegacyWcfClientEndpoint`
- `LegacyWcfClientEndpoints`

`LegacyWcfClient` exposes:

```text
Endpoints
RawElement
```

If `<client>` is missing, `config.Client` should be `null`. If `<client>` exists but has no direct `<endpoint>` children, `config.Client` should be non-null, `config.Client.Endpoints.Count` should be `0`, and `config.Client.RawElement` should preserve the source `<client>` element.

`LegacyWcfClientEndpoint` exposes:

```text
Name
Address
Binding
BindingConfiguration
Contract
BehaviorConfiguration
Attributes
RawElement
```

The typed properties should come from the same-named source attributes, except `BindingConfiguration` should come from `bindingConfiguration` and `BehaviorConfiguration` should come from `behaviorConfiguration`. Missing optional attributes produce `null` typed property values. `Attributes` preserves all source endpoint attributes, including unknown attributes. `RawElement` points to the preserved raw `<endpoint>` element.

Stage 5 parses only direct `<client>/<endpoint>` children. Unknown child elements under `<client>` should remain preserved through `config.Client.RawElement.Children` but should not be typed in Stage 5. Stage 5 should not emit diagnostics for missing optional endpoint attributes, unknown endpoint attributes, unknown client child elements, duplicate client endpoints, or missing binding/behaviour references. Validation belongs to a later phase.


## Phase 2 Stage 6 typed serviceHostingEnvironment model contract

Phase 2 Stage 6 is implemented and adds typed support for WCF `serviceHostingEnvironment` only. It should remain additive on top of the preserved raw model and must not weaken raw XML preservation.

Stage 6 adds:

- `config.ServiceHostingEnvironment`
- `LegacyWcfServiceHostingEnvironment`

`LegacyWcfServiceHostingEnvironment` exposes:

```text
AspNetCompatibilityEnabled
MultipleSiteBindingsEnabled
Attributes
RawElement
```

`AspNetCompatibilityEnabled` should come from the source `aspNetCompatibilityEnabled` attribute. `MultipleSiteBindingsEnabled` should come from the source `multipleSiteBindingsEnabled` attribute. Missing optional attributes produce `null` typed property values. `Attributes` preserves all source attributes, including unknown attributes. `RawElement` points to the preserved raw `<serviceHostingEnvironment>` element.

Stage 6 parses only the first direct `<serviceHostingEnvironment>` child under `<system.serviceModel>`. If no direct `<serviceHostingEnvironment>` element exists, `config.ServiceHostingEnvironment` should be `null`. If more than one direct `<serviceHostingEnvironment>` element exists, Stage 6 parses the first direct element, preserves all elements in `RawSystemServiceModel`, and emit no duplicate diagnostic. Duplicate diagnostics are emitted by Phase 4 validation.

Stage 6 does not parse boolean values into `bool`. Values remain strings exactly as they appear in XML. Stage 6 does not validate whether `aspNetCompatibilityEnabled` or `multipleSiteBindingsEnabled` values are valid boolean strings. Unknown attributes and unknown child elements are preserved and do not cause a read failure or diagnostic in Stage 6.

## Phase 3 retrieval API contract

Phase 3 is implemented. It adds lookup helpers to existing typed collections only. It does not change the XML scenarios that are parsed, the typed values produced from XML, or raw XML preservation.

Phase 3 adds these lookup behaviours:

| Area | Planned helpers | Match source | Missing `Find` result | Missing `GetRequired` result |
|---|---|---|---|---|
| Services | `Find(name)`, `GetRequired(name)` | `LegacyWcfService.Name` | `null` | `InvalidOperationException` |
| Service endpoints | `FindByName(name)`, `GetRequiredByName(name)` | `LegacyWcfServiceEndpoint.Name` | `null` | `InvalidOperationException` |
| Service endpoints | `FindByContract(contract)`, `GetRequiredByContract(contract)` | `LegacyWcfServiceEndpoint.Contract` | `null` | `InvalidOperationException` |
| Binding collection | `Find(name)`, `GetRequired(name)` | `LegacyWcfBinding.Name` | `null` | `InvalidOperationException` |
| Top-level bindings | `Find(bindingType, name)`, `GetRequired(bindingType, name)` | known binding group plus `LegacyWcfBinding.Name` | `null` | `InvalidOperationException` |
| Behaviour collection | `Find(name)`, `GetRequired(name)` | `LegacyWcfBehavior.Name` | `null` | `InvalidOperationException` |
| Client endpoints | `FindByName(name)`, `GetRequiredByName(name)` | `LegacyWcfClientEndpoint.Name` | `null` | `InvalidOperationException` |
| Client endpoints | `FindByContract(contract)`, `GetRequiredByContract(contract)` | `LegacyWcfClientEndpoint.Contract` | `null` | `InvalidOperationException` |

General Phase 3 lookup behaviour:

- WCF names and identifiers are matched case-insensitively.
- Service, service endpoint, and client endpoint lookup values that are `null`, blank, or whitespace do not match; `Find...` returns `null`, and `GetRequired...` throws a clear `InvalidOperationException`.
- Binding and behaviour collection lookup names may be `null` because unnamed bindings and behaviours are preserved in the typed model. A `null` lookup should match an item whose `Name` is `null`. An empty string should remain an empty-string lookup value.
- Top-level binding lookup supports `basicHttpBinding`, `wsHttpBinding`, `netTcpBinding`, and `customBinding`. Unknown, blank, or `null` binding types should return `null` from `Find(...)` and throw a clear `InvalidOperationException` from `GetRequired(...)`.
- If multiple items match, Phase 3 returns the first matching item and emit no diagnostics. Duplicate diagnostics belong to Phase 4 validation.
- Lookup helpers do not validate endpoint binding references, endpoint behaviour references, or service behaviour references. They only retrieve objects already present in the typed model.
- Required lookup exception messages include the missing lookup value and enough context to identify whether the lookup was for a service, service endpoint, client endpoint, binding, service behaviour, or endpoint behaviour.

Phase 3 does not add new WCF XML element support. It adds consumer-facing retrieval behaviour over the Phase 2 typed model.


## Phase 4 validation diagnostics contract

Phase 4 adds permissive validation diagnostics while preserving the existing raw and typed model behaviour. It does not make well-formed but imperfect legacy WCF configuration unreadable.

General Phase 4 rules:

- raw XML remains the source of truth
- typed models remain additive views over the raw tree
- validation diagnostics should be emitted without discarding unknown XML
- `Success` should remain `true` for well-formed configuration that contains validation warnings
- `Success` should remain `false` for missing or blank paths, missing files, unreadable files, malformed XML, missing `<configuration>`, and missing `<system.serviceModel>`
- duplicate detection should not change enumeration order
- lookup helpers should continue to return the first matching object when duplicates exist
- validation should report duplicates and unresolved references through diagnostics rather than by changing lookup semantics

Implemented Phase 4 diagnostics include:

| Area | Diagnostic expectation | Success impact |
|---|---|---|
| Unknown raw element | Report preserved element that is not currently recognised or is unsupported for typed modelling. | Success remains `true`. |
| Duplicate service name | Report duplicate non-blank service names. | Success remains `true`. |
| Duplicate binding name | Report duplicate names within the same binding type. | Success remains `true`. |
| Duplicate behaviour name | Report duplicate names within service behaviours or endpoint behaviours separately. | Success remains `true`. |
| Duplicate `serviceHostingEnvironment` | Report more than one direct element under `<system.serviceModel>`. | Success remains `true`; typed model still uses the first element. |
| Missing binding reference | Report service or client endpoint `bindingConfiguration` values that do not resolve to a binding of the endpoint `binding` type. | Success remains `true`. |
| Missing endpoint behaviour reference | Report service or client endpoint `behaviorConfiguration` values that do not resolve to an endpoint behaviour. | Success remains `true`. |
| Missing service behaviour reference | Report service `behaviorConfiguration` values that do not resolve to a service behaviour. | Success remains `true`. |

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


Phase 4 should not validate by fully enforcing the WCF XML schema. It should focus on useful diagnostics for common modernization and inspection problems.

## Phase 4 validation diagnostic scenarios

Phase 4 validation is additive. The following scenarios should all keep `result.Success == true`, preserve raw XML, keep typed models available, and attach diagnostics to both `result.Diagnostics` and `result.Configuration.Diagnostics`.

### Duplicate service name

Two or more non-blank service names that match case-insensitively should produce `LWC1002`. Lookup helpers still return the first matching service.

### Duplicate binding name

Two or more non-blank binding names within the same binding type that match case-insensitively should produce `LWC1003`. The duplicate rule is scoped to each binding type, so a `basicHttpBinding` and `netTcpBinding` may use the same name without being duplicates of each other.

### Duplicate behaviour name

Duplicate non-blank service behaviour names should produce `LWC1004`. Duplicate non-blank endpoint behaviour names should produce `LWC1005`. Service behaviours and endpoint behaviours are separate duplicate scopes.

### Duplicate serviceHostingEnvironment

More than one direct `<serviceHostingEnvironment>` child under `<system.serviceModel>` should produce `LWC1006`. The typed model still uses the first direct element and raw XML preserves all elements.

### Missing references

A service or client endpoint with a non-blank `bindingConfiguration` that cannot be resolved through the endpoint `binding` type should produce `LWC1007`. A service or client endpoint with a non-blank `behaviorConfiguration` that cannot be resolved to an endpoint behaviour should produce `LWC1008`. A service with a non-blank `behaviorConfiguration` that cannot be resolved to a service behaviour should produce `LWC1009`.

### Unknown raw element

A raw element not recognised by the current reader should produce `LWC1001` as an informational diagnostic. The element and its attributes should remain available through the raw model.

## Scenario 1: Simple service with endpoint

Stage 1 typed model target: yes.

### Input XML

```xml
<configuration>
  <system.serviceModel>
    <services>
      <service name="MyCompany.Services.CustomerService">
        <endpoint
          address=""
          binding="basicHttpBinding"
          contract="MyCompany.Services.ICustomerService" />
      </service>
    </services>
  </system.serviceModel>
</configuration>
```

### Expected typed model

```text
config.Services.Count == 1

service.Name == "MyCompany.Services.CustomerService"
service.BehaviorConfiguration == null
service.Endpoints.Count == 1

endpoint.Address == ""
endpoint.Binding == "basicHttpBinding"
endpoint.BindingConfiguration == null
endpoint.Contract == "MyCompany.Services.ICustomerService"
endpoint.BehaviorConfiguration == null
```

### Raw XML preservation

```text
config.RawSystemServiceModel is not null
service.RawElement is not null
endpoint.RawElement is not null
Raw XML for <system.serviceModel>, <services>, <service>, and <endpoint> is preserved
```

### Expected diagnostics

```text
No diagnostics expected.
```

## Scenario 2: Service with host base addresses

Stage 2 typed model target: yes.

### Input XML

```xml
<configuration>
  <system.serviceModel>
    <services>
      <service name="MyCompany.Services.CustomerService">
        <host>
          <baseAddresses>
            <add baseAddress="http://localhost:8080/CustomerService" />
          </baseAddresses>
        </host>
        <endpoint
          address=""
          binding="basicHttpBinding"
          bindingConfiguration="CustomerBinding"
          contract="MyCompany.Services.ICustomerService" />
      </service>
    </services>
  </system.serviceModel>
</configuration>
```

### Expected typed model

```text
config.Services.Count == 1

service.Name == "MyCompany.Services.CustomerService"
service.Host is not null
service.Host.BaseAddresses contains "http://localhost:8080/CustomerService"

service.Endpoints.Count == 1
endpoint.Address == ""
endpoint.Binding == "basicHttpBinding"
endpoint.BindingConfiguration == "CustomerBinding"
endpoint.Contract == "MyCompany.Services.ICustomerService"
```

### Raw XML preservation

```text
service.RawElement is not null
service.Host.RawElement is not null
endpoint.RawElement is not null
Raw XML for <host>, <baseAddresses>, <add>, and <endpoint> is preserved
```

### Expected diagnostics

```text
No diagnostics expected.
```

## Scenario 2a: Service with host timeouts

Stage 2 typed model target: yes.

### Input XML

```xml
<configuration>
  <system.serviceModel>
    <services>
      <service name="MyCompany.Services.CustomerService">
        <host>
          <timeouts openTimeout="00:01:00" closeTimeout="00:02:00" />
        </host>
      </service>
    </services>
  </system.serviceModel>
</configuration>
```

### Expected typed model

```text
config.Services.Count == 1

service.Host is not null
service.Host.Timeouts is not null
service.Host.Timeouts.OpenTimeout == "00:01:00"
service.Host.Timeouts.CloseTimeout == "00:02:00"
```

### Raw XML preservation

```text
service.Host.RawElement is not null
service.Host.Timeouts.RawElement is not null
Raw XML for <host> and <timeouts> is preserved
```

### Expected diagnostics

```text
No diagnostics expected.
```

## Scenario 3: Service with multiple endpoints

Stage 1 typed model target: yes.

### Input XML

```xml
<configuration>
  <system.serviceModel>
    <services>
      <service name="MyCompany.Services.CustomerService">
        <endpoint
          address="basic"
          binding="basicHttpBinding"
          bindingConfiguration="CustomerBasicBinding"
          contract="MyCompany.Services.ICustomerService" />
        <endpoint
          address="mex"
          binding="mexHttpBinding"
          contract="IMetadataExchange" />
      </service>
    </services>
  </system.serviceModel>
</configuration>
```

### Expected typed model

```text
config.Services.Count == 1
service.Endpoints.Count == 2

First endpoint:
endpoint.Address == "basic"
endpoint.Binding == "basicHttpBinding"
endpoint.BindingConfiguration == "CustomerBasicBinding"
endpoint.Contract == "MyCompany.Services.ICustomerService"

Second endpoint:
endpoint.Address == "mex"
endpoint.Binding == "mexHttpBinding"
endpoint.BindingConfiguration == null
endpoint.Contract == "IMetadataExchange"
```

### Raw XML preservation

```text
Both endpoint RawElement values are preserved separately.
```

### Expected diagnostics

```text
No diagnostics expected.
```

## Scenario 4: Named basicHttpBinding

Stage 3 typed model target: yes, implemented.

### Input XML

```xml
<configuration>
  <system.serviceModel>
    <bindings>
      <basicHttpBinding>
        <binding
          name="CustomerBinding"
          maxReceivedMessageSize="65536"
          openTimeout="00:01:00"
          closeTimeout="00:01:00"
          sendTimeout="00:02:00"
          receiveTimeout="00:10:00">
          <security mode="Transport" />
        </binding>
      </basicHttpBinding>
    </bindings>
  </system.serviceModel>
</configuration>
```

### Expected typed model

```text
config.Bindings.BasicHttp.Count == 1

binding.BindingType == "basicHttpBinding"
binding.Name == "CustomerBinding"
binding.Attributes["name"] == "CustomerBinding"
binding.Attributes["maxReceivedMessageSize"] == "65536"
binding.Attributes["openTimeout"] == "00:01:00"
binding.Attributes["closeTimeout"] == "00:01:00"
binding.Attributes["sendTimeout"] == "00:02:00"
binding.Attributes["receiveTimeout"] == "00:10:00"
```

The exact binding model may evolve, but the binding element and its attributes must be queryable.

### Raw XML preservation

```text
Raw XML for <bindings>, <basicHttpBinding>, <binding>, and <security> is preserved.
`binding.RawElement.Name == "binding"`.
`binding.RawElement.Children` contains the preserved `<security>` element.
```

### Expected diagnostics

```text
No diagnostics expected.
```

## Scenario 5: Named netTcpBinding

Stage 3 typed model target: yes, implemented.

### Input XML

```xml
<configuration>
  <system.serviceModel>
    <bindings>
      <netTcpBinding>
        <binding
          name="CustomerTcpBinding"
          portSharingEnabled="true"
          maxReceivedMessageSize="1048576">
          <security mode="Transport" />
        </binding>
      </netTcpBinding>
    </bindings>
  </system.serviceModel>
</configuration>
```

### Expected typed model

```text
config.Bindings.NetTcp.Count == 1

binding.BindingType == "netTcpBinding"
binding.Name == "CustomerTcpBinding"
binding.Attributes["portSharingEnabled"] == "true"
binding.Attributes["maxReceivedMessageSize"] == "1048576"
```

### Raw XML preservation

```text
Raw XML for the netTcpBinding binding and child elements is preserved.
`binding.RawElement.Children` contains the preserved `<security>` element.
```

### Expected diagnostics

```text
No diagnostics expected.
```


## Scenario 5a: Named wsHttpBinding

Stage 3 typed model target: yes, implemented.

### Input XML

```xml
<configuration>
  <system.serviceModel>
    <bindings>
      <wsHttpBinding>
        <binding
          name="CustomerWsBinding"
          maxReceivedMessageSize="65536">
          <security mode="Message" />
        </binding>
      </wsHttpBinding>
    </bindings>
  </system.serviceModel>
</configuration>
```

### Expected typed model

```text
config.Bindings.WsHttp.Count == 1

binding.BindingType == "wsHttpBinding"
binding.Name == "CustomerWsBinding"
binding.Attributes["name"] == "CustomerWsBinding"
binding.Attributes["maxReceivedMessageSize"] == "65536"
```

### Raw XML preservation

```text
binding.RawElement is not null
binding.RawElement.Name == "binding"
binding.RawElement.Children contains <security>
```

### Expected diagnostics

```text
No diagnostics expected in Stage 3.
```

## Scenario 5b: Named customBinding

Stage 3 typed model target: yes, implemented.

### Input XML

```xml
<configuration>
  <system.serviceModel>
    <bindings>
      <customBinding>
        <binding name="CustomerCustomBinding">
          <textMessageEncoding messageVersion="Soap12" />
          <httpTransport maxReceivedMessageSize="65536" />
        </binding>
      </customBinding>
    </bindings>
  </system.serviceModel>
</configuration>
```

### Expected typed model

```text
config.Bindings.Custom.Count == 1

binding.BindingType == "customBinding"
binding.Name == "CustomerCustomBinding"
binding.Attributes["name"] == "CustomerCustomBinding"
```

### Raw XML preservation

```text
binding.RawElement is not null
binding.RawElement.Children contains <textMessageEncoding>
binding.RawElement.Children contains <httpTransport>
```

### Expected diagnostics

```text
No diagnostics expected in Stage 3.
```

## Scenario 5c: Missing bindings element

Stage 3 typed model target: yes, implemented.

### Input XML

```xml
<configuration>
  <system.serviceModel>
  </system.serviceModel>
</configuration>
```

### Expected typed model

```text
config.Bindings is not null
config.Bindings.BasicHttp.Count == 0
config.Bindings.WsHttp.Count == 0
config.Bindings.NetTcp.Count == 0
config.Bindings.Custom.Count == 0
```

### Raw XML preservation

```text
config.RawSystemServiceModel is preserved.
```

### Expected diagnostics

```text
No diagnostics expected in Stage 3.
```

## Scenario 5d: Unnamed binding is preserved

Stage 3 typed model target: yes, implemented.

### Input XML

```xml
<configuration>
  <system.serviceModel>
    <bindings>
      <basicHttpBinding>
        <binding maxReceivedMessageSize="65536" />
      </basicHttpBinding>
    </bindings>
  </system.serviceModel>
</configuration>
```

### Expected typed model

```text
config.Bindings.BasicHttp.Count == 1

binding.BindingType == "basicHttpBinding"
binding.Name == null
binding.Attributes["maxReceivedMessageSize"] == "65536"
```

### Raw XML preservation

```text
binding.RawElement is not null
binding.RawElement.Name == "binding"
```

### Expected diagnostics

```text
No diagnostics expected in Stage 3.
```

## Scenario 5e: Unknown binding group is raw-only

Stage 3 typed model target: yes for raw preservation, no for typed modelling; implemented.

### Input XML

```xml
<configuration>
  <system.serviceModel>
    <bindings>
      <unknownLegacyBinding>
        <binding name="LegacyBinding" />
      </unknownLegacyBinding>
    </bindings>
  </system.serviceModel>
</configuration>
```

### Expected typed model

```text
config.Bindings.BasicHttp.Count == 0
config.Bindings.WsHttp.Count == 0
config.Bindings.NetTcp.Count == 0
config.Bindings.Custom.Count == 0
```

### Raw XML preservation

```text
The <unknownLegacyBinding> element is preserved under config.RawSystemServiceModel.
The nested <binding name="LegacyBinding" /> element is preserved in raw XML.
```

### Expected diagnostics

```text
No diagnostics expected in Stage 3.
```

## Scenario 6: Service behaviour

Stage 4 typed model target: yes, implemented.

### Input XML

```xml
<configuration>
  <system.serviceModel>
    <behaviors>
      <serviceBehaviors>
        <behavior name="CustomerServiceBehavior">
          <serviceMetadata httpGetEnabled="true" />
          <serviceDebug includeExceptionDetailInFaults="false" />
        </behavior>
      </serviceBehaviors>
    </behaviors>
  </system.serviceModel>
</configuration>
```

### Expected typed model

```text
config.Behaviors.ServiceBehaviors.Count == 1

behavior.BehaviorType == "serviceBehavior"
behavior.Name == "CustomerServiceBehavior"
behavior.Attributes["name"] == "CustomerServiceBehavior"
behavior.RawElement is not null
behavior.RawElement.Name == "behavior"
behavior.RawElement.Children contains <serviceMetadata>
behavior.RawElement.Children contains <serviceDebug>
```

The first typed model does not need to strongly model every behaviour child element, but those child elements must remain available through raw XML.

### Raw XML preservation

```text
Raw XML for <behavior>, <serviceMetadata>, and <serviceDebug> is preserved.
```

### Expected diagnostics

```text
No diagnostics expected.
```

## Scenario 7: Endpoint behaviour

Stage 4 typed model target: yes, implemented.

### Input XML

```xml
<configuration>
  <system.serviceModel>
    <behaviors>
      <endpointBehaviors>
        <behavior name="CustomerEndpointBehavior">
          <clientCredentials />
        </behavior>
      </endpointBehaviors>
    </behaviors>
  </system.serviceModel>
</configuration>
```

### Expected typed model

```text
config.Behaviors.EndpointBehaviors.Count == 1

behavior.BehaviorType == "endpointBehavior"
behavior.Name == "CustomerEndpointBehavior"
behavior.Attributes["name"] == "CustomerEndpointBehavior"
behavior.RawElement is not null
behavior.RawElement.Name == "behavior"
behavior.RawElement.Children contains <clientCredentials>
```

### Raw XML preservation

```text
Raw XML for endpoint behaviour child elements is preserved.
```

### Expected diagnostics

```text
No diagnostics expected.
```

## Scenario 7a: Unnamed service behaviour is preserved

Stage 4 typed model target: yes, implemented.

### Input XML

```xml
<configuration>
  <system.serviceModel>
    <behaviors>
      <serviceBehaviors>
        <behavior>
          <serviceMetadata httpGetEnabled="true" />
        </behavior>
      </serviceBehaviors>
    </behaviors>
  </system.serviceModel>
</configuration>
```

### Expected typed model

```text
config.Behaviors.ServiceBehaviors.Count == 1

behavior.BehaviorType == "serviceBehavior"
behavior.Name == null
behavior.RawElement is not null
```

### Raw XML preservation

```text
The unnamed <behavior> element and its <serviceMetadata> child are preserved.
```

### Expected diagnostics

```text
No diagnostics expected in Stage 4.
```

## Scenario 7b: Unknown behaviour group is raw-only

Stage 4 typed model target: yes for raw preservation, no for typed modelling; implemented.

### Input XML

```xml
<configuration>
  <system.serviceModel>
    <behaviors>
      <unknownLegacyBehaviors>
        <behavior name="LegacyBehavior" />
      </unknownLegacyBehaviors>
    </behaviors>
  </system.serviceModel>
</configuration>
```

### Expected typed model

```text
config.Behaviors.ServiceBehaviors.Count == 0
config.Behaviors.EndpointBehaviors.Count == 0
```

### Raw XML preservation

```text
The <unknownLegacyBehaviors> element is preserved under config.RawSystemServiceModel.
The nested <behavior name="LegacyBehavior" /> element is preserved in raw XML.
```

### Expected diagnostics

```text
No diagnostics expected in Stage 4.
```

## Scenario 7c: British spelling behaviour aliases

Stage 4 typed model target: yes, implemented.

### Input XML

```xml
<configuration>
  <system.serviceModel>
    <behaviours>
      <serviceBehaviours>
        <behaviour name="BritishServiceBehaviour">
          <serviceMetadata httpGetEnabled="true" />
        </behaviour>
      </serviceBehaviours>
      <endpointBehaviours>
        <behaviour name="BritishEndpointBehaviour">
          <clientCredentials />
        </behaviour>
      </endpointBehaviours>
    </behaviours>
  </system.serviceModel>
</configuration>
```

### Expected typed model

```text
config.Behaviors.ServiceBehaviors.Count == 1
config.Behaviors.EndpointBehaviors.Count == 1

service behavior BehaviorType == "serviceBehavior"
service behavior Name == "BritishServiceBehaviour"
endpoint behavior BehaviorType == "endpointBehavior"
endpoint behavior Name == "BritishEndpointBehaviour"
```

### Raw XML preservation

```text
Both RawElement values are preserved.
Raw element names may be "behaviour" because the source XML used British spelling.
```

### Expected diagnostics

```text
No diagnostics expected in Stage 4.
```

## Scenario 8: Client endpoint

Stage 5 typed model target: yes.

### Input XML

```xml
<configuration>
  <system.serviceModel>
    <client>
      <endpoint
        name="CustomerClient"
        address="http://localhost:8080/CustomerService"
        binding="basicHttpBinding"
        bindingConfiguration="CustomerBinding"
        contract="MyCompany.Services.ICustomerService" />
    </client>
  </system.serviceModel>
</configuration>
```

### Expected typed model

```text
config.Client is not null
config.Client.Endpoints.Count == 1

endpoint.Name == "CustomerClient"
endpoint.Address == "http://localhost:8080/CustomerService"
endpoint.Binding == "basicHttpBinding"
endpoint.BindingConfiguration == "CustomerBinding"
endpoint.Contract == "MyCompany.Services.ICustomerService"
endpoint.BehaviorConfiguration == null
endpoint.Attributes["name"] == "CustomerClient"
endpoint.Attributes["address"] == "http://localhost:8080/CustomerService"
endpoint.Attributes["binding"] == "basicHttpBinding"
endpoint.Attributes["bindingConfiguration"] == "CustomerBinding"
endpoint.Attributes["contract"] == "MyCompany.Services.ICustomerService"
endpoint.RawElement is not null
endpoint.RawElement.Name == "endpoint"
```

### Raw XML preservation

```text
Raw XML for <client> and <endpoint> is preserved.
config.Client.RawElement.Name == "client"
endpoint.RawElement.Name == "endpoint"
```

### Expected diagnostics

```text
No diagnostics expected in Stage 5.
```

## Scenario 8a: Client endpoint with behaviour configuration

Stage 5 typed model target: yes.

### Input XML

```xml
<configuration>
  <system.serviceModel>
    <client>
      <endpoint
        name="CustomerClient"
        address="http://localhost:8080/CustomerService"
        binding="basicHttpBinding"
        bindingConfiguration="CustomerBinding"
        contract="MyCompany.Services.ICustomerService"
        behaviorConfiguration="CustomerEndpointBehavior" />
    </client>
  </system.serviceModel>
</configuration>
```

### Expected typed model

```text
config.Client is not null
config.Client.Endpoints.Count == 1
endpoint.BehaviorConfiguration == "CustomerEndpointBehavior"
endpoint.Attributes["behaviorConfiguration"] == "CustomerEndpointBehavior"
```

### Raw XML preservation

```text
The endpoint RawElement is preserved.
```

### Expected diagnostics

```text
No diagnostics expected in Stage 5.
```

## Scenario 8b: Multiple client endpoints preserve order

Stage 5 typed model target: yes.

### Input XML

```xml
<configuration>
  <system.serviceModel>
    <client>
      <endpoint
        name="CustomerClient"
        address="http://localhost:8080/CustomerService"
        binding="basicHttpBinding"
        bindingConfiguration="CustomerBinding"
        contract="MyCompany.Services.ICustomerService" />
      <endpoint
        name="OrderClient"
        address="http://localhost:8080/OrderService"
        binding="wsHttpBinding"
        bindingConfiguration="OrderBinding"
        contract="MyCompany.Services.IOrderService" />
    </client>
  </system.serviceModel>
</configuration>
```

### Expected typed model

```text
config.Client is not null
config.Client.Endpoints.Count == 2
config.Client.Endpoints[0].Name == "CustomerClient"
config.Client.Endpoints[1].Name == "OrderClient"
config.Client.Endpoints[0].RawElement is not config.Client.Endpoints[1].RawElement
```

### Raw XML preservation

```text
Both endpoint RawElement values are preserved separately and in source order.
```

### Expected diagnostics

```text
No diagnostics expected in Stage 5.
```

## Scenario 8c: Missing client element

Stage 5 typed model target: yes.

### Input XML

```xml
<configuration>
  <system.serviceModel>
  </system.serviceModel>
</configuration>
```

### Expected typed model

```text
config.Client == null
```

### Raw XML preservation

```text
config.RawSystemServiceModel is preserved.
```

### Expected diagnostics

```text
No diagnostics expected in Stage 5.
```

## Scenario 8d: Client exists with no endpoints

Stage 5 typed model target: yes.

### Input XML

```xml
<configuration>
  <system.serviceModel>
    <client>
    </client>
  </system.serviceModel>
</configuration>
```

### Expected typed model

```text
config.Client is not null
config.Client.Endpoints.Count == 0
config.Client.RawElement.Name == "client"
```

### Raw XML preservation

```text
The raw <client> element is preserved.
```

### Expected diagnostics

```text
No diagnostics expected in Stage 5.
```

## Scenario 8e: Client endpoint with missing optional attributes and unknown attributes

Stage 5 typed model target: yes.

### Input XML

```xml
<configuration>
  <system.serviceModel>
    <client>
      <endpoint
        contract="MyCompany.Services.ICustomerService"
        customAttribute="abc" />
    </client>
  </system.serviceModel>
</configuration>
```

### Expected typed model

```text
config.Client is not null
config.Client.Endpoints.Count == 1
endpoint.Name == null
endpoint.Address == null
endpoint.Binding == null
endpoint.BindingConfiguration == null
endpoint.Contract == "MyCompany.Services.ICustomerService"
endpoint.BehaviorConfiguration == null
endpoint.Attributes["contract"] == "MyCompany.Services.ICustomerService"
endpoint.Attributes["customAttribute"] == "abc"
```

### Raw XML preservation

```text
The endpoint RawElement is preserved.
Unknown attributes remain available through endpoint.Attributes.
```

### Expected diagnostics

```text
No diagnostics expected in Stage 5.
```

## Scenario 8f: Unknown client child is raw-only

Stage 5 typed model target: yes for raw preservation, no for typed modelling.

### Input XML

```xml
<configuration>
  <system.serviceModel>
    <client>
      <customClientChild value="abc" />
    </client>
  </system.serviceModel>
</configuration>
```

### Expected typed model

```text
config.Client is not null
config.Client.Endpoints.Count == 0
config.Client.RawElement.Children contains customClientChild
```

### Raw XML preservation

```text
The unknown <customClientChild> element is preserved under config.Client.RawElement.Children.
```

### Expected diagnostics

```text
No diagnostics expected in Stage 5.
```

## Scenario 9: serviceHostingEnvironment

Stage 6 typed model target: yes.

### Input XML

```xml
<configuration>
  <system.serviceModel>
    <serviceHostingEnvironment
      aspNetCompatibilityEnabled="true"
      multipleSiteBindingsEnabled="true" />
  </system.serviceModel>
</configuration>
```

### Expected typed model

```text
config.ServiceHostingEnvironment is not null
config.ServiceHostingEnvironment.AspNetCompatibilityEnabled == "true"
config.ServiceHostingEnvironment.MultipleSiteBindingsEnabled == "true"
config.ServiceHostingEnvironment.Attributes["aspNetCompatibilityEnabled"] == "true"
config.ServiceHostingEnvironment.Attributes["multipleSiteBindingsEnabled"] == "true"
config.ServiceHostingEnvironment.RawElement is not null
config.ServiceHostingEnvironment.RawElement.Name == "serviceHostingEnvironment"
```

### Raw XML preservation

```text
Raw XML for <serviceHostingEnvironment> is preserved.
Unknown attributes and child elements remain available through the raw model.
```

### Expected diagnostics

```text
No diagnostics expected in Stage 6.
```

## Scenario 9a: serviceHostingEnvironment with only aspNetCompatibilityEnabled

Stage 6 typed model target: yes.

### Input XML

```xml
<configuration>
  <system.serviceModel>
    <serviceHostingEnvironment aspNetCompatibilityEnabled="false" />
  </system.serviceModel>
</configuration>
```

### Expected typed model

```text
config.ServiceHostingEnvironment is not null
config.ServiceHostingEnvironment.AspNetCompatibilityEnabled == "false"
config.ServiceHostingEnvironment.MultipleSiteBindingsEnabled == null
config.ServiceHostingEnvironment.Attributes["aspNetCompatibilityEnabled"] == "false"
```

### Expected diagnostics

```text
No diagnostics expected in Stage 6.
```

## Scenario 9b: serviceHostingEnvironment with no attributes

Stage 6 typed model target: yes.

### Input XML

```xml
<configuration>
  <system.serviceModel>
    <serviceHostingEnvironment />
  </system.serviceModel>
</configuration>
```

### Expected typed model

```text
config.ServiceHostingEnvironment is not null
config.ServiceHostingEnvironment.AspNetCompatibilityEnabled == null
config.ServiceHostingEnvironment.MultipleSiteBindingsEnabled == null
config.ServiceHostingEnvironment.Attributes.Count == 0
config.ServiceHostingEnvironment.RawElement.Name == "serviceHostingEnvironment"
```

### Expected diagnostics

```text
No diagnostics expected in Stage 6.
```

## Scenario 9c: missing serviceHostingEnvironment

Stage 6 typed model target: yes.

### Input XML

```xml
<configuration>
  <system.serviceModel>
  </system.serviceModel>
</configuration>
```

### Expected typed model

```text
config.ServiceHostingEnvironment == null
```

### Expected diagnostics

```text
No diagnostics expected in Stage 6.
```

## Scenario 9d: serviceHostingEnvironment with unknown attribute

Stage 6 typed model target: yes.

### Input XML

```xml
<configuration>
  <system.serviceModel>
    <serviceHostingEnvironment
      aspNetCompatibilityEnabled="true"
      customAttribute="abc" />
  </system.serviceModel>
</configuration>
```

### Expected typed model

```text
config.ServiceHostingEnvironment is not null
config.ServiceHostingEnvironment.AspNetCompatibilityEnabled == "true"
config.ServiceHostingEnvironment.Attributes["customAttribute"] == "abc"
```

### Expected diagnostics

```text
No diagnostics expected in Stage 6.
```

## Scenario 9e: serviceHostingEnvironment with unknown child

Stage 6 typed model target: yes for raw preservation, no for typed child modelling.

### Input XML

```xml
<configuration>
  <system.serviceModel>
    <serviceHostingEnvironment aspNetCompatibilityEnabled="true">
      <customHostingChild value="abc" />
    </serviceHostingEnvironment>
  </system.serviceModel>
</configuration>
```

### Expected typed model

```text
config.ServiceHostingEnvironment is not null
config.ServiceHostingEnvironment.AspNetCompatibilityEnabled == "true"
config.ServiceHostingEnvironment.RawElement.Children contains customHostingChild
```

### Expected diagnostics

```text
No diagnostics expected in Stage 6.
```

## Scenario 9f: duplicate serviceHostingEnvironment elements

Stage 6 typed model target: yes for first direct element, yes for raw preservation of all elements.

### Input XML

```xml
<configuration>
  <system.serviceModel>
    <serviceHostingEnvironment aspNetCompatibilityEnabled="true" />
    <serviceHostingEnvironment aspNetCompatibilityEnabled="false" />
  </system.serviceModel>
</configuration>
```

### Expected typed model

```text
config.ServiceHostingEnvironment is not null
config.ServiceHostingEnvironment.AspNetCompatibilityEnabled == "true"
config.RawSystemServiceModel.Children contains both serviceHostingEnvironment elements
```

### Expected diagnostics

```text
No duplicate diagnostic expected in Stage 6.
Duplicate diagnostics are emitted by Phase 4 validation.
```

## Scenario 10: Unknown custom element preserved

Stage 1 typed model target: yes for service parsing and raw child preservation. The unknown element remains raw-only.

### Input XML

```xml
<configuration>
  <system.serviceModel>
    <services>
      <service name="MyCompany.Services.CustomerService">
        <customLegacyElement customAttribute="abc">
          <nested value="123" />
        </customLegacyElement>
      </service>
    </services>
  </system.serviceModel>
</configuration>
```

### Expected typed model

```text
config.Services.Count == 1
service.Name == "MyCompany.Services.CustomerService"
```

The unknown element does not need to have a first-class typed model.

### Raw XML preservation

```text
The <customLegacyElement> element is preserved in the raw tree.
The customAttribute attribute is preserved.
The <nested> child element is preserved.
```

### Expected diagnostics

```text
Phase 1 does not need to emit a diagnostic for this scenario.
The reader should not fail solely because this unknown element exists.
Phase 4 emits an informational diagnostic to say that an unknown element was preserved.
```


## Phase 1 test scenario: Raw reader preserves valid service XML

### Input XML

```xml
<configuration>
  <system.serviceModel>
    <services>
      <service name="MyCompany.Services.CustomerService">
        <endpoint
          address=""
          binding="basicHttpBinding"
          contract="MyCompany.Services.ICustomerService" />
      </service>
    </services>
  </system.serviceModel>
</configuration>
```

### Expected Phase 1 result

```text
result.Success == true
result.Configuration is not null
RawSystemServiceModel.Name == "system.serviceModel"
RawSystemServiceModel.Children contains services
service element is preserved
endpoint element is preserved
endpoint attributes are preserved
RawXml is populated
SourceFilePath is populated
LineNumber is populated if practical
```

No typed service or endpoint model is required in Phase 1.

## Scenario 11: Missing system.serviceModel

### Input XML

```xml
<configuration>
  <appSettings>
    <add key="Example" value="Value" />
  </appSettings>
</configuration>
```

### Expected typed model

```text
result.Success == false
result.Configuration == null
```

### Raw XML preservation

```text
No RawSystemServiceModel exists because <system.serviceModel> is absent.
```

### Expected diagnostics

```text
Diagnostic should indicate that <system.serviceModel> was not found.
Diagnostic severity should be Error.
Diagnostic code should be LWC0005.
```

Missing `<system.serviceModel>` is an expected read outcome. It should be reported clearly and should not be confused with malformed XML.

## Scenario 12: Malformed XML

### Input XML

```xml
<configuration>
  <system.serviceModel>
    <services>
      <service name="Broken">
    </services>
  </system.serviceModel>
</configuration>
```

### Expected typed model

```text
No typed configuration should be returned.
```

### Raw XML preservation

```text
No reliable raw model can be built.
```

### Expected diagnostics

```text
Diagnostic severity should be Error.
Diagnostic code should be LWC0003.
result.Success should be false.
```

Malformed XML is one of the cases where reading should fail.

## Scenario 13: Endpoint references missing named binding

### Input XML

```xml
<configuration>
  <system.serviceModel>
    <services>
      <service name="MyCompany.Services.CustomerService">
        <endpoint
          address=""
          binding="basicHttpBinding"
          bindingConfiguration="MissingBinding"
          contract="MyCompany.Services.ICustomerService" />
      </service>
    </services>
    <bindings>
      <basicHttpBinding />
    </bindings>
  </system.serviceModel>
</configuration>
```

### Expected typed model

```text
service and endpoint should still be parsed.
endpoint.Binding == "basicHttpBinding"
endpoint.BindingConfiguration == "MissingBinding"
```

### Raw XML preservation

```text
All XML should be preserved.
```

### Expected diagnostics

```text
Phase 4 emits warning diagnostic `LWC1007` indicating that the endpoint references a binding configuration that was not found.
The reader should not fail solely because the reference is unresolved.
```

## Scenario 14: Duplicate service names

### Input XML

```xml
<configuration>
  <system.serviceModel>
    <services>
      <service name="MyCompany.Services.CustomerService" />
      <service name="MyCompany.Services.CustomerService" />
    </services>
  </system.serviceModel>
</configuration>
```

### Expected typed model

```text
Both service elements should be preserved.
Enumeration returns both services in source order.
Phase 3 lookup helpers continue to return the first matching service.
Phase 4 reports the duplicate through diagnostics.
```

### Raw XML preservation

```text
Both service RawElement values are preserved.
```

### Expected diagnostics

```text
A warning diagnostic should indicate duplicate service names. The read should remain successful because both raw service elements are preserved.
```


## Scenario 15: Service endpoint references missing binding configuration

Phase 4 validation target: yes.

### Input XML

```xml
<configuration>
  <system.serviceModel>
    <services>
      <service name="MyCompany.Services.CustomerService">
        <endpoint
          address=""
          binding="basicHttpBinding"
          bindingConfiguration="MissingBinding"
          contract="MyCompany.Services.ICustomerService" />
      </service>
    </services>
    <bindings>
      <basicHttpBinding>
        <binding name="ExistingBinding" />
      </basicHttpBinding>
    </bindings>
  </system.serviceModel>
</configuration>
```

### Expected typed model

```text
The service endpoint is preserved.
endpoint.Binding == "basicHttpBinding"
endpoint.BindingConfiguration == "MissingBinding"
config.Bindings.BasicHttp contains "ExistingBinding" only.
```

### Raw XML preservation

```text
The endpoint and binding XML are preserved.
```

### Expected diagnostics

```text
A warning diagnostic should indicate that the endpoint references a missing binding configuration.
The read should remain successful.
```

## Scenario 16: Client endpoint references missing endpoint behaviour

Phase 4 validation target: yes.

### Input XML

```xml
<configuration>
  <system.serviceModel>
    <client>
      <endpoint
        name="CustomerClient"
        address="http://localhost/customer"
        binding="basicHttpBinding"
        contract="MyCompany.Services.ICustomerService"
        behaviorConfiguration="MissingEndpointBehavior" />
    </client>
    <behaviors>
      <endpointBehaviors>
        <behavior name="ExistingEndpointBehavior" />
      </endpointBehaviors>
    </behaviors>
  </system.serviceModel>
</configuration>
```

### Expected typed model

```text
The client endpoint is preserved.
endpoint.BehaviorConfiguration == "MissingEndpointBehavior"
config.Behaviors.EndpointBehaviors contains "ExistingEndpointBehavior" only.
```

### Raw XML preservation

```text
The client endpoint and endpoint behaviour XML are preserved.
```

### Expected diagnostics

```text
A warning diagnostic should indicate that the client endpoint references a missing endpoint behaviour configuration.
The read should remain successful.
```

## Scenario 17: Duplicate serviceHostingEnvironment elements

Phase 4 validation target: yes.

### Input XML

```xml
<configuration>
  <system.serviceModel>
    <serviceHostingEnvironment aspNetCompatibilityEnabled="true" />
    <serviceHostingEnvironment multipleSiteBindingsEnabled="true" />
  </system.serviceModel>
</configuration>
```

### Expected typed model

```text
config.ServiceHostingEnvironment is built from the first direct serviceHostingEnvironment element.
All direct serviceHostingEnvironment elements are preserved in RawSystemServiceModel.
```

### Raw XML preservation

```text
Both serviceHostingEnvironment elements are preserved.
```

### Expected diagnostics

```text
A warning diagnostic should indicate that multiple direct serviceHostingEnvironment elements were found.
The read should remain successful.
```

## MVP support priority

Priority 1:

- raw `<system.serviceModel>` preservation
- services
- service endpoints
- host/baseAddresses
- basic named bindings
- behaviours by name
- client endpoints
- retrieval APIs
- permissive validation diagnostics

Priority 2:

- more binding-specific typed properties
- more behaviour-specific typed properties
- deeper serviceHostingEnvironment details
- broader validation coverage

Priority 3:

- migration concern classification
- CoreWCF mapping helpers
- code generation
