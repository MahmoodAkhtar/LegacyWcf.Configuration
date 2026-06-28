# Roadmap

This roadmap describes the intended delivery path for LegacyWcf.Configuration.

The project should prioritise trust, preservation, typed access, and clear diagnostics before attempting CoreWCF mapping or code generation.

## Phase 1: Full-fidelity reader

Status: implemented.

Goal:

```text
Read and preserve the full <system.serviceModel> XML tree.
```

The implemented reader:

- load `app.config`, `web.config`, or external `.config` files
- locate `<configuration>`
- locate `<system.serviceModel>`
- read every descendant element under `<system.serviceModel>`
- preserve unknown elements
- preserve unknown attributes
- preserve raw XML
- expose source file path
- expose line number if practical
- provide diagnostics for missing or malformed configuration
- avoid silently discarding information

Expected first useful output:

```csharp
var result = LegacyWcfConfigurationReader.Read("web.config");
var raw = result.Configuration.RawSystemServiceModel;
```


### Phase 1 implementation boundary

Phase 1 has been implemented as a small raw-reader slice only.

Include:

- `LegacyWcfConfigurationReader.Read(string filePath)`
- `LegacyWcfConfigurationReadResult`
- `LegacyWcfConfiguration` with `RawSystemServiceModel`
- `LegacyWcfElement`
- `LegacyWcfDiagnostic`
- `LegacyWcfDiagnosticSeverity`
- an internal recursive raw element builder
- tests for valid XML, missing files, malformed XML, missing `<configuration>`, missing `<system.serviceModel>`, and unknown XML preservation

Current Phase 1 source layout keeps public API files at the project root and implementation-only helpers under `Internal/`:

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

Phase 1 does not include typed services, endpoints, bindings, behaviours, lookup APIs, CoreWCF mapping, code generation, or CLI tooling.

### Current overall test status

The latest provided full test run after Phase 4 passed:

- total tests: 78
- passed: 78
- failed: 0
- skipped: 0

## Phase 2: Typed WCF model

Goal:

```text
Add typed models for high-value WCF concepts.
```

### Phase 2 Stage 1: typed services and service endpoints

Status: implemented.

Stage 1 should add:

- `LegacyWcfService`
- `LegacyWcfServiceEndpoint`
- `LegacyWcfServices`
- `LegacyWcfServiceEndpoints`
- `LegacyWcfConfiguration.Services`
- typed parsing from the preserved raw `LegacyWcfElement` tree
- tests for simple services, service endpoints, multiple endpoints, missing `<services>`, and unknown service children

Stage 1 did not add:

- host model
- host base addresses
- host timeouts
- bindings
- behaviours
- client endpoints
- serviceHostingEnvironment
- `Find(...)` or `GetRequired(...)` lookup APIs
- endpoint lookup helpers
- validation diagnostics for duplicates or missing references
- CoreWCF mapping
- code generation
- CLI tooling

Every typed object should retain access to its raw XML element.

### Phase 2 Stage 2: typed service host and base addresses

Status: implemented.

Stage 2 adds:

- `LegacyWcfHost`
- `LegacyWcfHostTimeouts`
- `LegacyWcfService.Host`
- typed host base addresses in source XML order
- typed host timeout values as strings
- raw XML fallback for host and timeout elements
- tests for single base address, multiple base addresses, missing host, empty host, missing `baseAddress`, and unknown host children

Stage 2 does not add bindings, behaviours, client endpoints, lookup helpers, validation diagnostics, CoreWCF mapping, code generation, or CLI tooling.

### Phase 2 Stage 3: initial typed bindings

Status: implemented.

Stage 3 adds initial typed support for WCF binding configuration only:

- `LegacyWcfBinding`
- `LegacyWcfBindingCollection`
- `LegacyWcfBindings`
- `LegacyWcfConfiguration.Bindings`
- `basicHttpBinding`
- `wsHttpBinding`
- `netTcpBinding`
- `customBinding`
- parsing from the preserved raw `LegacyWcfElement` tree
- raw XML fallback for every typed binding
- tests for named binding parsing, unnamed binding preservation, unknown child preservation, missing `<bindings>`, and unknown binding groups

Stage 3 does not add:

- behaviours
- service behaviours
- endpoint behaviours
- client endpoints
- serviceHostingEnvironment
- `Find(...)` or `GetRequired(...)` lookup APIs
- endpoint lookup helpers
- binding lookup helpers
- validation diagnostics for duplicate bindings
- validation diagnostics for endpoints referencing missing bindings
- CoreWCF mapping
- code generation
- CLI tooling

Example Stage 3 usage:

```csharp
foreach (var binding in config.Bindings.BasicHttp)
{
    Console.WriteLine(binding.BindingType);
    Console.WriteLine(binding.Name);
    Console.WriteLine(binding.RawElement.RawXml);
}

foreach (var binding in config.Bindings.NetTcp)
{
    Console.WriteLine(binding.Attributes["portSharingEnabled"]);
}
```


### Phase 2 Stage 4: initial typed behaviours

Status: implemented.

Stage 4 adds initial typed support for WCF behaviour configuration only:

- `LegacyWcfBehavior`
- `LegacyWcfBehaviorCollection`
- `LegacyWcfBehaviors`
- `LegacyWcfConfiguration.Behaviors`
- service behaviours under `<behaviors>/<serviceBehaviors>/<behavior>`
- endpoint behaviours under `<behaviors>/<endpointBehaviors>/<behavior>`
- British legacy/custom aliases: `<behaviours>`, `<serviceBehaviours>`, `<endpointBehaviours>`, and `<behaviour>`
- parsing from the preserved raw `LegacyWcfElement` tree
- raw XML fallback for every typed behaviour
- tests for named behaviours, unnamed behaviours, unknown child preservation, unknown group preservation, missing behaviours, and British spelling aliases

Stage 4 uses normalized behaviour kind values:

```text
serviceBehavior
endpointBehavior
```

Stage 4 does not add:

- client endpoints
- serviceHostingEnvironment
- `Find(...)` or `GetRequired(...)` lookup APIs
- behaviour lookup helpers
- validation diagnostics for duplicate behaviours
- validation diagnostics for services or endpoints referencing missing behaviours
- CoreWCF mapping
- code generation
- CLI tooling


### Phase 2 Stage 5: typed client endpoints

Status: implemented.

Stage 5 adds typed support for WCF client endpoint configuration only:

- `LegacyWcfClient`
- `LegacyWcfClientEndpoint`
- `LegacyWcfClientEndpoints`
- `LegacyWcfConfiguration.Client`
- endpoint attributes: `name`, `address`, `binding`, `bindingConfiguration`, `contract`, and `behaviorConfiguration`
- all source endpoint attributes preserved through `LegacyWcfClientEndpoint.Attributes`
- raw XML fallback for the `<client>` element and every typed client endpoint
- tests for named endpoints, behaviour configuration, multiple endpoints in order, missing `<client>`, empty `<client>`, missing optional endpoint attributes, unknown endpoint attributes, and unknown client child elements

Stage 5 parses only from the preserved `LegacyWcfElement` tree. It does not parse directly from `XDocument` or `XElement`, validate binding or behaviour references, add lookup helpers, add CoreWCF mapping, generate code, add CLI tooling, or implement `serviceHostingEnvironment`.

### Phase 2 Stage 6: typed serviceHostingEnvironment

Status: implemented.

Stage 6 adds typed support for WCF `serviceHostingEnvironment` configuration only:

- `LegacyWcfServiceHostingEnvironment`
- `LegacyWcfConfiguration.ServiceHostingEnvironment`
- attributes `aspNetCompatibilityEnabled` and `multipleSiteBindingsEnabled` as string values
- all source attributes preserved through `LegacyWcfServiceHostingEnvironment.Attributes`
- raw XML fallback through `LegacyWcfServiceHostingEnvironment.RawElement`
- tests for common attributes, missing attributes, missing element, unknown attributes, unknown child elements, duplicate direct elements, and raw XML preservation

Stage 6 parses only from the preserved `LegacyWcfElement` tree. It does not parse directly from `XDocument` or `XElement`, parse boolean values into `bool`, validate boolean strings, emit duplicate diagnostics, add lookup helpers, add CoreWCF mapping, generate code, or add CLI tooling.

## Phase 3: Retrieval APIs

Status: implemented.

Goal:

```text
Make common WCF lookups easy.
```

Phase 3 adds additive lookup helpers on top of the existing typed collections. It should not change parsing, raw XML preservation, diagnostics, or CoreWCF boundaries.

Planned APIs:

```csharp
config.Services.Find(serviceName);
config.Services.GetRequired(serviceName);

service.Endpoints.FindByName(endpointName);
service.Endpoints.FindByContract(contractName);
service.Endpoints.GetRequiredByName(endpointName);
service.Endpoints.GetRequiredByContract(contractName);

config.Bindings.BasicHttp.Find(bindingConfigurationName);
config.Bindings.BasicHttp.GetRequired(bindingConfigurationName);

config.Bindings.Find(bindingType, bindingConfigurationName);
config.Bindings.GetRequired(bindingType, bindingConfigurationName);

config.Behaviors.ServiceBehaviors.Find(name);
config.Behaviors.ServiceBehaviors.GetRequired(name);

config.Behaviors.EndpointBehaviors.Find(name);
config.Behaviors.EndpointBehaviors.GetRequired(name);

config.Client?.Endpoints.FindByName(endpointName);
config.Client?.Endpoints.FindByContract(contractName);
config.Client?.Endpoints.GetRequiredByName(endpointName);
config.Client?.Endpoints.GetRequiredByContract(contractName);
```

Phase 3 uses case-insensitive matching for WCF names and identifiers. `Find...` methods return `null` when no item matches. `GetRequired...` methods throw a clear `InvalidOperationException` when no item matches. If duplicates exist, Phase 3 returns the first matching item and leave duplicate diagnostics to Phase 4 validation.

Phase 3 includes tests for service, service endpoint, binding, behaviour, and client endpoint lookup helpers, including missing lookup values and required-lookup exception messages. Existing Phase 1 and Phase 2 behaviour remains unchanged.

This phase gives developers the first major practical payoff: they can read old config files and retrieve the values they need in code.

Phase 3 should not include validation diagnostics, duplicate detection, missing binding reference diagnostics, missing behaviour reference diagnostics, CoreWCF mapping, code generation, CLI tooling, richer binding-specific models, or richer behaviour-specific models.

## Phase 4: Validation and diagnostics

Status: implemented.

Goal:

```text
Add permissive validation and useful diagnostics.
```

Diagnostics include the existing read-failure diagnostics plus new permissive validation diagnostics for successful reads:

- malformed XML
- missing `<configuration>`
- missing `<system.serviceModel>`
- unknown elements
- unknown attributes where useful
- duplicate named services
- duplicate named bindings
- duplicate named service behaviours
- duplicate named endpoint behaviours
- duplicate direct `serviceHostingEnvironment` elements
- service endpoint references missing binding configuration
- client endpoint references missing binding configuration
- service endpoint references missing endpoint behaviour configuration
- client endpoint references missing endpoint behaviour configuration
- service references missing service behaviour configuration
- unsupported or partially understood elements
- likely CoreWCF migration concerns

Default mode should remain permissive:

```text
Read what can be read.
Preserve what is unknown.
Report diagnostics.
Only fail when the XML cannot be loaded or the requested file cannot be read.
```

Phase 4 is additive and does not break existing public APIs. It builds diagnostics from the preserved raw XML tree and the current typed model. It does not introduce CoreWCF mapping, code generation, CLI tooling, automatic migration, strict schema enforcement, or richer binding/behaviour-specific models.

Representative Phase 4 tests include duplicate services, duplicate bindings, duplicate behaviours, missing binding references from service and client endpoints, missing behaviour references from services and endpoints, duplicate direct `serviceHostingEnvironment`, unknown raw elements, and confirmation that successful reads remain successful when warnings are emitted.

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



## Phase 5: Optional CoreWCF mapping helpers

Goal:

```text
Add a separate optional package that depends on CoreWCF.
```

Possible package name:

```text
LegacyWcf.Configuration.CoreWcf
```

Responsibilities may include:

- map supported legacy bindings to CoreWCF binding objects
- map endpoint information to CoreWCF endpoint setup
- resolve endpoint addresses using host base addresses
- classify elements as directly mappable, mappable in code, partially mappable, unsupported, or requiring manual review
- provide migration recommendations

Possible classification enum:

```csharp
public enum CoreWcfMigrationStatus
{
    DirectlyMappable,
    MappableInCode,
    PartiallyMappable,
    Unsupported,
    RequiresManualReview,
    Informational
}
```

For example, legacy `<host>` data may be classified as:

```text
MappableInCode
```

because the values can inform ASP.NET Core/CoreWCF hosting setup even if they are not directly consumed from legacy XML configuration.

## Phase 6: Optional code generation

Goal:

```text
Generate suggested CoreWCF setup snippets and migration notes.
```

Possible package name:

```text
LegacyWcf.Configuration.CodeGeneration
```

Responsibilities may include:

- generate suggested CoreWCF `Program.cs` or `Startup.cs` snippets
- generate service registration snippets
- generate endpoint setup snippets
- generate binding setup snippets
- generate TODO comments for unsupported elements
- generate migration notes

Generated code should be treated as a migration aid, not as a guaranteed production-ready conversion.

## MVP definition

The MVP should include:

- `netstandard2.0;net8.0` target frameworks
- no CoreWCF dependency
- full-fidelity raw reader (implemented)
- Phase 1 raw-reader implementation before typed parsing
- typed model for services and endpoints
- host/baseAddresses support
- initial binding support
- initial behaviour support
- client endpoint support (implemented)
- serviceHostingEnvironment support (implemented)
- typed enumerable collections
- `Find(...)` and `GetRequired(...)` where useful (implemented)
- permissive diagnostics (implemented)
- representative test config files

The MVP should avoid:

- automatic migration promises
- strict schema enforcement by default
- code generation
- CoreWCF dependency in the core package
- discarding unknown XML
- requiring the legacy solution to build

## Future ideas

Possible later capabilities:

- richer binding-specific models
- richer behaviour-specific models
- CoreWCF migration report
- code generation package
- command-line inspection tool
- JSON export of parsed configuration
- comparison between two config files
- documentation generation from a config file
- analyzers for risky legacy WCF settings
