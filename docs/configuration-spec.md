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

## Scenario 1: Simple service with endpoint

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

## Scenario 3: Service with multiple endpoints

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

binding.Type == "basicHttpBinding"
binding.Name == "CustomerBinding"
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
```

### Expected diagnostics

```text
No diagnostics expected.
```

## Scenario 5: Named netTcpBinding

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

binding.Type == "netTcpBinding"
binding.Name == "CustomerTcpBinding"
binding.Attributes["portSharingEnabled"] == "true"
binding.Attributes["maxReceivedMessageSize"] == "1048576"
```

### Raw XML preservation

```text
Raw XML for the netTcpBinding binding and child elements is preserved.
```

### Expected diagnostics

```text
No diagnostics expected.
```

## Scenario 6: Service behaviour

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

behavior.Name == "CustomerServiceBehavior"
behavior.RawElement is not null
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

behavior.Name == "CustomerEndpointBehavior"
behavior.RawElement is not null
```

### Raw XML preservation

```text
Raw XML for endpoint behaviour child elements is preserved.
```

### Expected diagnostics

```text
No diagnostics expected.
```

## Scenario 8: Client endpoint

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
```

### Raw XML preservation

```text
Raw XML for <client> and <endpoint> is preserved.
```

### Expected diagnostics

```text
No diagnostics expected.
```

## Scenario 9: serviceHostingEnvironment

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
```

The exact property types may evolve. The important point is that common attributes should be retrievable.

### Raw XML preservation

```text
Raw XML for <serviceHostingEnvironment> is preserved.
```

### Expected diagnostics

```text
No diagnostics expected.
```

## Scenario 10: Unknown custom element preserved

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
A warning or informational diagnostic may be emitted to say that an unknown element was preserved.
The reader should not fail solely because this unknown element exists.
```

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
No LegacyWcfConfiguration is available, or an empty configuration is returned depending on final API design.
```

### Raw XML preservation

```text
No RawSystemServiceModel exists because <system.serviceModel> is absent.
```

### Expected diagnostics

```text
Diagnostic should indicate that <system.serviceModel> was not found.
```

Whether this makes result.Success false is an API decision. For the MVP, missing `<system.serviceModel>` should be reported clearly and should not be confused with malformed XML.

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
A warning diagnostic should indicate that the endpoint references a binding configuration that was not found.
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
The typed service collection should define clear behaviour for duplicate names.
```

Possible collection behaviour:

```text
Enumeration returns both services.
Find/GetRequired by name reports duplicate match clearly.
```

### Raw XML preservation

```text
Both service RawElement values are preserved.
```

### Expected diagnostics

```text
A warning or error diagnostic should indicate duplicate service names.
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
- diagnostics

Priority 2:

- more binding-specific typed properties
- more behaviour-specific typed properties
- serviceHostingEnvironment details
- validation of cross-references

Priority 3:

- migration concern classification
- CoreWCF mapping helpers
- code generation
