# Roadmap

This roadmap describes the intended delivery path for LegacyWcf.Configuration.

The project should prioritise trust, preservation, typed access, and clear diagnostics before attempting CoreWCF mapping or code generation.

## Phase 1: Full-fidelity reader

Goal:

```text
Read and preserve the full <system.serviceModel> XML tree.
```

The reader should:

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

## Phase 2: Typed WCF model

Goal:

```text
Add typed models for high-value WCF concepts.
```

Priority typed models:

- services
- service
- service endpoints
- host
- host base addresses
- host timeouts
- bindings
- binding collections
- basicHttpBinding
- wsHttpBinding
- netTcpBinding
- customBinding
- behaviours
- service behaviours
- endpoint behaviours
- client endpoints
- serviceHostingEnvironment

Every typed object should retain access to its raw XML element.

Example:

```csharp
foreach (var service in config.Services)
{
    Console.WriteLine(service.Name);

    foreach (var endpoint in service.Endpoints)
    {
        Console.WriteLine(endpoint.Contract);
        Console.WriteLine(endpoint.Binding);
    }
}
```

## Phase 3: Retrieval APIs

Goal:

```text
Make common WCF lookups easy.
```

Possible APIs:

```csharp
config.Services.Find(serviceName);
config.Services.GetRequired(serviceName);

service.Endpoints.FindByName(endpointName);
service.Endpoints.FindByContract(contractName);
service.Endpoints.GetRequiredByName(endpointName);
service.Endpoints.GetRequiredByContract(contractName);

config.Bindings.Find(bindingType, bindingConfigurationName);
config.Bindings.GetRequired(bindingType, bindingConfigurationName);

config.Behaviors.ServiceBehaviors.Find(name);
config.Behaviors.ServiceBehaviors.GetRequired(name);

config.Behaviors.EndpointBehaviors.Find(name);
config.Behaviors.EndpointBehaviors.GetRequired(name);
```

This phase gives developers the first major practical payoff: they can read old config files and retrieve the values they need in code.

## Phase 4: Validation and diagnostics

Goal:

```text
Add permissive validation and useful diagnostics.
```

Diagnostics should include:

- malformed XML
- missing `<configuration>`
- missing `<system.serviceModel>`
- unknown elements
- unknown attributes
- duplicate named services
- duplicate named bindings
- endpoint references missing binding configuration
- endpoint references missing behaviour configuration
- service references missing behaviour configuration
- unsupported or partially understood elements
- likely CoreWCF migration concerns

Default mode should remain permissive:

```text
Read what can be read.
Preserve what is unknown.
Report diagnostics.
Only fail when the XML cannot be loaded or the requested file cannot be read.
```

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
- full-fidelity raw reader
- typed model for services and endpoints
- host/baseAddresses support
- initial binding support
- initial behaviour support
- client endpoint support
- typed enumerable collections
- `Find(...)` and `GetRequired(...)` where useful
- permissive diagnostics
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
