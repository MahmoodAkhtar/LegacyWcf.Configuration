using System;
using System.IO;
using System.Linq;
using LegacyWcf.Configuration;
using Xunit;

namespace LegacyWcf.Configuration.Tests;

public sealed class LegacyWcfConfigurationReaderTests : IDisposable
{
    private readonly string _tempDirectory;

    public LegacyWcfConfigurationReaderTests()
    {
        _tempDirectory = Path.Combine(
            Path.GetTempPath(),
            "LegacyWcf.Configuration.Tests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Read_WhenSystemServiceModelExists_PreservesRawTree()
    {
        var filePath = WriteConfig("""
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
""");

        var result = LegacyWcfConfigurationReader.Read(filePath);

        Assert.True(result.Success);
        Assert.NotNull(result.Configuration);

        var raw = result.Configuration!.RawSystemServiceModel;
        Assert.Equal("system.serviceModel", raw.Name);
        Assert.Equal("configuration/system.serviceModel", raw.Path);
        Assert.NotNull(raw.RawXml);
        Assert.Contains("<services>", raw.RawXml!, StringComparison.Ordinal);
        Assert.Equal(Path.GetFullPath(filePath), raw.SourceFilePath);
        Assert.NotNull(raw.LineNumber);

        var services = Assert.Single(raw.Children, child => child.Name == "services");
        var service = Assert.Single(services.Children, child => child.Name == "service");
        Assert.Equal("MyCompany.Services.CustomerService", service.Attributes["name"]);

        var endpoint = Assert.Single(service.Children, child => child.Name == "endpoint");
        Assert.Equal(string.Empty, endpoint.Attributes["address"]);
        Assert.Equal("basicHttpBinding", endpoint.Attributes["binding"]);
        Assert.Equal("MyCompany.Services.ICustomerService", endpoint.Attributes["contract"]);
        Assert.NotNull(endpoint.RawXml);
        Assert.Equal(Path.GetFullPath(filePath), endpoint.SourceFilePath);
    }

    [Fact]
    public void Read_WhenFileIsMissing_ReturnsErrorDiagnostic()
    {
        var filePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "missing.config");

        var result = LegacyWcfConfigurationReader.Read(filePath);

        Assert.False(result.Success);
        Assert.Null(result.Configuration);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(LegacyWcfDiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("could not be found", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Read_WhenXmlIsMalformed_ReturnsErrorDiagnostic()
    {
        var filePath = WriteConfig("""
<configuration>
  <system.serviceModel>
    <services>
      <service name="Broken">
    </services>
  </system.serviceModel>
</configuration>
""");

        var result = LegacyWcfConfigurationReader.Read(filePath);

        Assert.False(result.Success);
        Assert.Null(result.Configuration);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(LegacyWcfDiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("could not be loaded or parsed", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(diagnostic.LineNumber);
    }

    [Fact]
    public void Read_WhenConfigurationElementIsMissing_ReturnsErrorDiagnostic()
    {
        var filePath = WriteConfig("""
<notConfiguration>
  <system.serviceModel />
</notConfiguration>
""");

        var result = LegacyWcfConfigurationReader.Read(filePath);

        Assert.False(result.Success);
        Assert.Null(result.Configuration);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(LegacyWcfDiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("<configuration>", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not found", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Read_WhenSystemServiceModelElementIsMissing_ReturnsErrorDiagnostic()
    {
        var filePath = WriteConfig("""
<configuration>
  <appSettings>
    <add key="Example" value="Value" />
  </appSettings>
</configuration>
""");

        var result = LegacyWcfConfigurationReader.Read(filePath);

        Assert.False(result.Success);
        Assert.Null(result.Configuration);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(LegacyWcfDiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("<system.serviceModel>", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not found", diagnostic.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Read_WhenUnknownCustomElementExists_PreservesUnknownElementAndAttributes()
    {
        var filePath = WriteConfig("""
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
""");

        var result = LegacyWcfConfigurationReader.Read(filePath);

        Assert.True(result.Success);
        Assert.NotNull(result.Configuration);

        var service = result.Configuration!.RawSystemServiceModel
            .Children.Single(child => child.Name == "services")
            .Children.Single(child => child.Name == "service");

        var custom = Assert.Single(service.Children, child => child.Name == "customLegacyElement");
        Assert.False(custom.IsKnownElement);
        Assert.Equal("abc", custom.Attributes["customAttribute"]);
        Assert.Contains("customLegacyElement", custom.RawXml!, StringComparison.Ordinal);

        var nested = Assert.Single(custom.Children, child => child.Name == "nested");
        Assert.False(nested.IsKnownElement);
        Assert.Equal("123", nested.Attributes["value"]);
        Assert.NotNull(nested.RawXml);
    }


    [Fact]
    public void Read_WhenServiceExists_PopulatesTypedService()
    {
        var filePath = WriteConfig("""
<configuration>
  <system.serviceModel>
    <services>
      <service name="MyCompany.Services.CustomerService" behaviorConfiguration="CustomerServiceBehavior" />
    </services>
  </system.serviceModel>
</configuration>
""");

        var result = LegacyWcfConfigurationReader.Read(filePath);

        Assert.True(result.Success);
        Assert.NotNull(result.Configuration);

        var service = Assert.Single(result.Configuration!.Services);
        Assert.Equal("MyCompany.Services.CustomerService", service.Name);
        Assert.Equal("CustomerServiceBehavior", service.BehaviorConfiguration);
        Assert.NotNull(service.RawElement);
        Assert.Equal("service", service.RawElement.Name);
    }

    [Fact]
    public void Read_WhenServiceEndpointExists_PopulatesTypedEndpoint()
    {
        var filePath = WriteConfig("""
<configuration>
  <system.serviceModel>
    <services>
      <service name="MyCompany.Services.CustomerService">
        <endpoint
          name="CustomerEndpoint"
          address=""
          binding="basicHttpBinding"
          bindingConfiguration="CustomerBinding"
          contract="MyCompany.Services.ICustomerService"
          behaviorConfiguration="CustomerEndpointBehavior" />
      </service>
    </services>
  </system.serviceModel>
</configuration>
""");

        var result = LegacyWcfConfigurationReader.Read(filePath);

        Assert.True(result.Success);
        Assert.NotNull(result.Configuration);

        var service = Assert.Single(result.Configuration!.Services);
        var endpoint = Assert.Single(service.Endpoints);

        Assert.Equal("CustomerEndpoint", endpoint.Name);
        Assert.Equal(string.Empty, endpoint.Address);
        Assert.Equal("basicHttpBinding", endpoint.Binding);
        Assert.Equal("CustomerBinding", endpoint.BindingConfiguration);
        Assert.Equal("MyCompany.Services.ICustomerService", endpoint.Contract);
        Assert.Equal("CustomerEndpointBehavior", endpoint.BehaviorConfiguration);
        Assert.NotNull(endpoint.RawElement);
        Assert.Equal("endpoint", endpoint.RawElement.Name);
    }

    [Fact]
    public void Read_WhenServiceHasMultipleEndpoints_PopulatesAllTypedEndpoints()
    {
        var filePath = WriteConfig("""
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
""");

        var result = LegacyWcfConfigurationReader.Read(filePath);

        Assert.True(result.Success);
        Assert.NotNull(result.Configuration);

        var service = Assert.Single(result.Configuration!.Services);
        Assert.Equal(2, service.Endpoints.Count);
        Assert.Equal("basic", service.Endpoints[0].Address);
        Assert.Equal("mex", service.Endpoints[1].Address);
        Assert.NotSame(service.Endpoints[0].RawElement, service.Endpoints[1].RawElement);
    }

    [Fact]
    public void Read_WhenServicesElementIsMissing_ServicesCollectionIsEmpty()
    {
        var filePath = WriteConfig("""
<configuration>
  <system.serviceModel>
  </system.serviceModel>
</configuration>
""");

        var result = LegacyWcfConfigurationReader.Read(filePath);

        Assert.True(result.Success);
        Assert.NotNull(result.Configuration);
        Assert.Empty(result.Configuration!.Services);
        Assert.NotNull(result.Configuration.RawSystemServiceModel);
        Assert.Equal("system.serviceModel", result.Configuration.RawSystemServiceModel.Name);
    }

    [Fact]
    public void Read_WhenServiceContainsUnknownChild_PreservesRawChild()
    {
        var filePath = WriteConfig("""
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
""");

        var result = LegacyWcfConfigurationReader.Read(filePath);

        Assert.True(result.Success);
        Assert.NotNull(result.Configuration);

        var service = Assert.Single(result.Configuration!.Services);
        Assert.Equal("MyCompany.Services.CustomerService", service.Name);

        var custom = Assert.Single(service.RawElement.Children, child => child.Name == "customLegacyElement");
        Assert.Equal("abc", custom.Attributes["customAttribute"]);
        Assert.False(custom.IsKnownElement);
    }

    [Fact]
    public void Read_WhenServiceNameIsMissing_PopulatesServiceWithEmptyName()
    {
        var filePath = WriteConfig("""
<configuration>
  <system.serviceModel>
    <services>
      <service>
        <endpoint contract="MyCompany.Services.ICustomerService" />
      </service>
    </services>
  </system.serviceModel>
</configuration>
""");

        var result = LegacyWcfConfigurationReader.Read(filePath);

        Assert.True(result.Success);
        Assert.NotNull(result.Configuration);

        var service = Assert.Single(result.Configuration!.Services);
        Assert.Equal(string.Empty, service.Name);
        Assert.Null(service.BehaviorConfiguration);
        var endpoint = Assert.Single(service.Endpoints);
        Assert.Null(endpoint.Name);
        Assert.Null(endpoint.Address);
        Assert.Null(endpoint.Binding);
        Assert.Null(endpoint.BindingConfiguration);
        Assert.Equal("MyCompany.Services.ICustomerService", endpoint.Contract);
        Assert.Null(endpoint.BehaviorConfiguration);
    }


    [Fact]
    public void Read_WhenServiceHasHostBaseAddress_PopulatesTypedHostBaseAddress()
    {
        var filePath = WriteConfig("""
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
""");

        var result = LegacyWcfConfigurationReader.Read(filePath);

        Assert.True(result.Success);
        Assert.NotNull(result.Configuration);

        var service = Assert.Single(result.Configuration!.Services);
        Assert.NotNull(service.Host);
        Assert.Single(service.Host!.BaseAddresses);
        Assert.Equal("http://localhost:8080/CustomerService", service.Host.BaseAddresses[0]);
        Assert.NotNull(service.Host.RawElement);
        Assert.Equal("host", service.Host.RawElement.Name);

        var endpoint = Assert.Single(service.Endpoints);
        Assert.Equal(string.Empty, endpoint.Address);
        Assert.Equal("basicHttpBinding", endpoint.Binding);
        Assert.Equal("CustomerBinding", endpoint.BindingConfiguration);
        Assert.Equal("MyCompany.Services.ICustomerService", endpoint.Contract);
    }

    [Fact]
    public void Read_WhenServiceHasMultipleHostBaseAddresses_PopulatesAllBaseAddressesInOrder()
    {
        var filePath = WriteConfig("""
<configuration>
  <system.serviceModel>
    <services>
      <service name="Microsoft.ServiceModel.Samples.CalculatorService">
        <host>
          <baseAddresses>
            <add baseAddress="http://localhost:8000/ServiceModelSamples/service" />
            <add baseAddress="net.tcp://localhost:8000/ServiceModelSamples/service2" />
          </baseAddresses>
        </host>
      </service>
    </services>
  </system.serviceModel>
</configuration>
""");

        var result = LegacyWcfConfigurationReader.Read(filePath);

        Assert.True(result.Success);
        Assert.NotNull(result.Configuration);

        var service = Assert.Single(result.Configuration!.Services);
        Assert.NotNull(service.Host);
        Assert.Equal(2, service.Host!.BaseAddresses.Count);
        Assert.Equal("http://localhost:8000/ServiceModelSamples/service", service.Host.BaseAddresses[0]);
        Assert.Equal("net.tcp://localhost:8000/ServiceModelSamples/service2", service.Host.BaseAddresses[1]);
    }

    [Fact]
    public void Read_WhenServiceHasNoHost_HostIsNull()
    {
        var filePath = WriteConfig("""
<configuration>
  <system.serviceModel>
    <services>
      <service name="MyCompany.Services.CustomerService">
        <endpoint contract="MyCompany.Services.ICustomerService" />
      </service>
    </services>
  </system.serviceModel>
</configuration>
""");

        var result = LegacyWcfConfigurationReader.Read(filePath);

        Assert.True(result.Success);
        Assert.NotNull(result.Configuration);

        var service = Assert.Single(result.Configuration!.Services);
        Assert.Null(service.Host);
        var endpoint = Assert.Single(service.Endpoints);
        Assert.Equal("MyCompany.Services.ICustomerService", endpoint.Contract);
    }

    [Fact]
    public void Read_WhenHostHasNoBaseAddresses_HostExistsWithEmptyBaseAddressCollection()
    {
        var filePath = WriteConfig("""
<configuration>
  <system.serviceModel>
    <services>
      <service name="MyCompany.Services.CustomerService">
        <host>
        </host>
      </service>
    </services>
  </system.serviceModel>
</configuration>
""");

        var result = LegacyWcfConfigurationReader.Read(filePath);

        Assert.True(result.Success);
        Assert.NotNull(result.Configuration);

        var service = Assert.Single(result.Configuration!.Services);
        Assert.NotNull(service.Host);
        Assert.Empty(service.Host!.BaseAddresses);
        Assert.Equal("host", service.Host.RawElement.Name);
    }

    [Fact]
    public void Read_WhenHostBaseAddressAddIsMissingBaseAddress_IgnoresTypedAddressButPreservesRawAdd()
    {
        var filePath = WriteConfig("""
<configuration>
  <system.serviceModel>
    <services>
      <service name="MyCompany.Services.CustomerService">
        <host>
          <baseAddresses>
            <add />
          </baseAddresses>
        </host>
      </service>
    </services>
  </system.serviceModel>
</configuration>
""");

        var result = LegacyWcfConfigurationReader.Read(filePath);

        Assert.True(result.Success);
        Assert.NotNull(result.Configuration);

        var service = Assert.Single(result.Configuration!.Services);
        Assert.NotNull(service.Host);
        Assert.Empty(service.Host!.BaseAddresses);

        var baseAddresses = Assert.Single(service.Host.RawElement.Children, child => child.Name == "baseAddresses");
        var add = Assert.Single(baseAddresses.Children, child => child.Name == "add");
        Assert.Empty(add.Attributes);
    }

    [Fact]
    public void Read_WhenHostContainsUnknownChild_PreservesUnknownChild()
    {
        var filePath = WriteConfig("""
<configuration>
  <system.serviceModel>
    <services>
      <service name="MyCompany.Services.CustomerService">
        <host>
          <customHostSetting value="abc" />
        </host>
      </service>
    </services>
  </system.serviceModel>
</configuration>
""");

        var result = LegacyWcfConfigurationReader.Read(filePath);

        Assert.True(result.Success);
        Assert.NotNull(result.Configuration);
        Assert.Empty(result.Diagnostics);

        var service = Assert.Single(result.Configuration!.Services);
        Assert.NotNull(service.Host);
        var customHostSetting = Assert.Single(service.Host!.RawElement.Children, child => child.Name == "customHostSetting");
        Assert.Equal("abc", customHostSetting.Attributes["value"]);
        Assert.False(customHostSetting.IsKnownElement);
    }


    [Fact]
    public void Read_WhenBasicHttpBindingExists_PopulatesTypedBasicHttpBinding()
    {
        var filePath = WriteConfig("""
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
""");

        var result = LegacyWcfConfigurationReader.Read(filePath);

        Assert.True(result.Success);
        Assert.NotNull(result.Configuration);

        var binding = Assert.Single(result.Configuration!.Bindings.BasicHttp);
        Assert.Equal("basicHttpBinding", binding.BindingType);
        Assert.Equal("CustomerBinding", binding.Name);
        Assert.Equal("CustomerBinding", binding.Attributes["name"]);
        Assert.Equal("65536", binding.Attributes["maxReceivedMessageSize"]);
        Assert.Equal("00:01:00", binding.Attributes["openTimeout"]);
        Assert.Equal("00:01:00", binding.Attributes["closeTimeout"]);
        Assert.Equal("00:02:00", binding.Attributes["sendTimeout"]);
        Assert.Equal("00:10:00", binding.Attributes["receiveTimeout"]);
        Assert.NotNull(binding.RawElement);
        Assert.Equal("binding", binding.RawElement.Name);
        Assert.Single(binding.RawElement.Children, child => child.Name == "security");
    }

    [Fact]
    public void Read_WhenNetTcpBindingExists_PopulatesTypedNetTcpBinding()
    {
        var filePath = WriteConfig("""
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
""");

        var result = LegacyWcfConfigurationReader.Read(filePath);

        Assert.True(result.Success);
        Assert.NotNull(result.Configuration);

        var binding = Assert.Single(result.Configuration!.Bindings.NetTcp);
        Assert.Equal("netTcpBinding", binding.BindingType);
        Assert.Equal("CustomerTcpBinding", binding.Name);
        Assert.Equal("true", binding.Attributes["portSharingEnabled"]);
        Assert.Equal("1048576", binding.Attributes["maxReceivedMessageSize"]);
        Assert.Single(binding.RawElement.Children, child => child.Name == "security");
    }

    [Fact]
    public void Read_WhenWsHttpBindingExists_PopulatesTypedWsHttpBinding()
    {
        var filePath = WriteConfig("""
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
""");

        var result = LegacyWcfConfigurationReader.Read(filePath);

        Assert.True(result.Success);
        Assert.NotNull(result.Configuration);

        var binding = Assert.Single(result.Configuration!.Bindings.WsHttp);
        Assert.Equal("wsHttpBinding", binding.BindingType);
        Assert.Equal("CustomerWsBinding", binding.Name);
        Assert.Equal("65536", binding.Attributes["maxReceivedMessageSize"]);
        Assert.NotNull(binding.RawElement);
        Assert.Equal("binding", binding.RawElement.Name);
        Assert.Single(binding.RawElement.Children, child => child.Name == "security");
    }

    [Fact]
    public void Read_WhenCustomBindingExists_PopulatesTypedCustomBinding()
    {
        var filePath = WriteConfig("""
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
""");

        var result = LegacyWcfConfigurationReader.Read(filePath);

        Assert.True(result.Success);
        Assert.NotNull(result.Configuration);

        var binding = Assert.Single(result.Configuration!.Bindings.Custom);
        Assert.Equal("customBinding", binding.BindingType);
        Assert.Equal("CustomerCustomBinding", binding.Name);
        Assert.Single(binding.RawElement.Children, child => child.Name == "textMessageEncoding");
        Assert.Single(binding.RawElement.Children, child => child.Name == "httpTransport");
    }

    [Fact]
    public void Read_WhenBindingsElementIsMissing_BindingsCollectionsAreEmpty()
    {
        var filePath = WriteConfig("""
<configuration>
  <system.serviceModel>
    <services>
      <service name="MyCompany.Services.CustomerService">
        <host>
          <baseAddresses>
            <add baseAddress="http://localhost:8080/CustomerService" />
          </baseAddresses>
        </host>
        <endpoint contract="MyCompany.Services.ICustomerService" />
      </service>
    </services>
  </system.serviceModel>
</configuration>
""");

        var result = LegacyWcfConfigurationReader.Read(filePath);

        Assert.True(result.Success);
        Assert.NotNull(result.Configuration);
        Assert.Empty(result.Configuration!.Bindings.BasicHttp);
        Assert.Empty(result.Configuration.Bindings.WsHttp);
        Assert.Empty(result.Configuration.Bindings.NetTcp);
        Assert.Empty(result.Configuration.Bindings.Custom);

        var service = Assert.Single(result.Configuration.Services);
        Assert.NotNull(service.Host);
        Assert.Single(service.Host!.BaseAddresses);
        Assert.Single(service.Endpoints);
    }

    [Fact]
    public void Read_WhenBindingNameIsMissing_PreservesUnnamedBinding()
    {
        var filePath = WriteConfig("""
<configuration>
  <system.serviceModel>
    <bindings>
      <basicHttpBinding>
        <binding maxReceivedMessageSize="65536" />
      </basicHttpBinding>
    </bindings>
  </system.serviceModel>
</configuration>
""");

        var result = LegacyWcfConfigurationReader.Read(filePath);

        Assert.True(result.Success);
        Assert.NotNull(result.Configuration);

        var binding = Assert.Single(result.Configuration!.Bindings.BasicHttp);
        Assert.Equal("basicHttpBinding", binding.BindingType);
        Assert.Null(binding.Name);
        Assert.Equal("65536", binding.Attributes["maxReceivedMessageSize"]);
        Assert.NotNull(binding.RawElement);
        Assert.Equal("binding", binding.RawElement.Name);
    }

    [Fact]
    public void Read_WhenBindingContainsUnknownChild_PreservesRawChild()
    {
        var filePath = WriteConfig("""
<configuration>
  <system.serviceModel>
    <bindings>
      <basicHttpBinding>
        <binding name="CustomerBinding">
          <customBindingChild value="abc" />
        </binding>
      </basicHttpBinding>
    </bindings>
  </system.serviceModel>
</configuration>
""");

        var result = LegacyWcfConfigurationReader.Read(filePath);

        Assert.True(result.Success);
        Assert.NotNull(result.Configuration);
        Assert.Empty(result.Diagnostics);

        var binding = Assert.Single(result.Configuration!.Bindings.BasicHttp);
        var customChild = Assert.Single(binding.RawElement.Children, child => child.Name == "customBindingChild");
        Assert.Equal("abc", customChild.Attributes["value"]);
        Assert.False(customChild.IsKnownElement);
    }

    [Fact]
    public void Read_WhenUnknownBindingGroupExists_PreservesRawXmlButDoesNotTypedModelIt()
    {
        var filePath = WriteConfig("""
<configuration>
  <system.serviceModel>
    <bindings>
      <unknownLegacyBinding>
        <binding name="LegacyBinding" />
      </unknownLegacyBinding>
    </bindings>
  </system.serviceModel>
</configuration>
""");

        var result = LegacyWcfConfigurationReader.Read(filePath);

        Assert.True(result.Success);
        Assert.NotNull(result.Configuration);
        Assert.Empty(result.Diagnostics);
        Assert.Empty(result.Configuration!.Bindings.BasicHttp);
        Assert.Empty(result.Configuration.Bindings.WsHttp);
        Assert.Empty(result.Configuration.Bindings.NetTcp);
        Assert.Empty(result.Configuration.Bindings.Custom);

        var bindings = Assert.Single(result.Configuration.RawSystemServiceModel.Children, child => child.Name == "bindings");
        var unknownBinding = Assert.Single(bindings.Children, child => child.Name == "unknownLegacyBinding");
        var rawBinding = Assert.Single(unknownBinding.Children, child => child.Name == "binding");
        Assert.Equal("LegacyBinding", rawBinding.Attributes["name"]);
    }


    [Fact]
    public void Read_WhenServiceBehaviorExists_PopulatesTypedServiceBehavior()
    {
        var filePath = WriteConfig("""
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
""");

        var result = LegacyWcfConfigurationReader.Read(filePath);

        Assert.True(result.Success);
        Assert.NotNull(result.Configuration);
        Assert.NotNull(result.Configuration!.Behaviors);

        var behavior = Assert.Single(result.Configuration.Behaviors.ServiceBehaviors);
        Assert.Equal("serviceBehavior", behavior.BehaviorType);
        Assert.Equal("CustomerServiceBehavior", behavior.Name);
        Assert.Equal("CustomerServiceBehavior", behavior.Attributes["name"]);
        Assert.NotNull(behavior.RawElement);
        Assert.Equal("behavior", behavior.RawElement.Name);
        Assert.Single(behavior.RawElement.Children, child => child.Name == "serviceMetadata");
        Assert.Single(behavior.RawElement.Children, child => child.Name == "serviceDebug");
    }

    [Fact]
    public void Read_WhenEndpointBehaviorExists_PopulatesTypedEndpointBehavior()
    {
        var filePath = WriteConfig("""
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
""");

        var result = LegacyWcfConfigurationReader.Read(filePath);

        Assert.True(result.Success);
        Assert.NotNull(result.Configuration);

        var behavior = Assert.Single(result.Configuration!.Behaviors.EndpointBehaviors);
        Assert.Equal("endpointBehavior", behavior.BehaviorType);
        Assert.Equal("CustomerEndpointBehavior", behavior.Name);
        Assert.Equal("CustomerEndpointBehavior", behavior.Attributes["name"]);
        Assert.NotNull(behavior.RawElement);
        Assert.Equal("behavior", behavior.RawElement.Name);
        Assert.Single(behavior.RawElement.Children, child => child.Name == "clientCredentials");
    }

    [Fact]
    public void Read_WhenBehaviorsElementIsMissing_BehaviorCollectionsAreEmpty()
    {
        var filePath = WriteConfig("""
<configuration>
  <system.serviceModel>
    <services>
      <service name="MyCompany.Services.CustomerService">
        <host>
          <baseAddresses>
            <add baseAddress="http://localhost:8080/CustomerService" />
          </baseAddresses>
        </host>
        <endpoint contract="MyCompany.Services.ICustomerService" />
      </service>
    </services>
    <bindings>
      <basicHttpBinding>
        <binding name="CustomerBinding" />
      </basicHttpBinding>
    </bindings>
  </system.serviceModel>
</configuration>
""");

        var result = LegacyWcfConfigurationReader.Read(filePath);

        Assert.True(result.Success);
        Assert.NotNull(result.Configuration);
        Assert.NotNull(result.Configuration!.Behaviors);
        Assert.Empty(result.Configuration.Behaviors.ServiceBehaviors);
        Assert.Empty(result.Configuration.Behaviors.EndpointBehaviors);

        var service = Assert.Single(result.Configuration.Services);
        Assert.NotNull(service.Host);
        Assert.Single(service.Host!.BaseAddresses);
        Assert.Single(service.Endpoints);
        Assert.Single(result.Configuration.Bindings.BasicHttp);
    }

    [Fact]
    public void Read_WhenServiceBehaviorNameIsMissing_PreservesUnnamedServiceBehavior()
    {
        var filePath = WriteConfig("""
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
""");

        var result = LegacyWcfConfigurationReader.Read(filePath);

        Assert.True(result.Success);
        Assert.NotNull(result.Configuration);

        var behavior = Assert.Single(result.Configuration!.Behaviors.ServiceBehaviors);
        Assert.Null(behavior.Name);
        Assert.Equal("serviceBehavior", behavior.BehaviorType);
        Assert.NotNull(behavior.RawElement);
        Assert.Equal("behavior", behavior.RawElement.Name);
        Assert.Single(behavior.RawElement.Children, child => child.Name == "serviceMetadata");
    }

    [Fact]
    public void Read_WhenEndpointBehaviorNameIsMissing_PreservesUnnamedEndpointBehavior()
    {
        var filePath = WriteConfig("""
<configuration>
  <system.serviceModel>
    <behaviors>
      <endpointBehaviors>
        <behavior>
          <clientCredentials />
        </behavior>
      </endpointBehaviors>
    </behaviors>
  </system.serviceModel>
</configuration>
""");

        var result = LegacyWcfConfigurationReader.Read(filePath);

        Assert.True(result.Success);
        Assert.NotNull(result.Configuration);

        var behavior = Assert.Single(result.Configuration!.Behaviors.EndpointBehaviors);
        Assert.Null(behavior.Name);
        Assert.Equal("endpointBehavior", behavior.BehaviorType);
        Assert.NotNull(behavior.RawElement);
        Assert.Equal("behavior", behavior.RawElement.Name);
        Assert.Single(behavior.RawElement.Children, child => child.Name == "clientCredentials");
    }

    [Fact]
    public void Read_WhenBehaviorContainsUnknownChild_PreservesRawChild()
    {
        var filePath = WriteConfig("""
<configuration>
  <system.serviceModel>
    <behaviors>
      <serviceBehaviors>
        <behavior name="CustomerServiceBehavior">
          <customBehaviorChild value="abc" />
        </behavior>
      </serviceBehaviors>
    </behaviors>
  </system.serviceModel>
</configuration>
""");

        var result = LegacyWcfConfigurationReader.Read(filePath);

        Assert.True(result.Success);
        Assert.NotNull(result.Configuration);
        Assert.Empty(result.Diagnostics);

        var behavior = Assert.Single(result.Configuration!.Behaviors.ServiceBehaviors);
        var customChild = Assert.Single(behavior.RawElement.Children, child => child.Name == "customBehaviorChild");
        Assert.Equal("abc", customChild.Attributes["value"]);
        Assert.False(customChild.IsKnownElement);
    }

    [Fact]
    public void Read_WhenUnknownBehaviorGroupExists_PreservesRawXmlButDoesNotTypedModelIt()
    {
        var filePath = WriteConfig("""
<configuration>
  <system.serviceModel>
    <behaviors>
      <unknownLegacyBehaviors>
        <behavior name="LegacyBehavior" />
      </unknownLegacyBehaviors>
    </behaviors>
  </system.serviceModel>
</configuration>
""");

        var result = LegacyWcfConfigurationReader.Read(filePath);

        Assert.True(result.Success);
        Assert.NotNull(result.Configuration);
        Assert.Empty(result.Diagnostics);
        Assert.Empty(result.Configuration!.Behaviors.ServiceBehaviors);
        Assert.Empty(result.Configuration.Behaviors.EndpointBehaviors);

        var behaviors = Assert.Single(result.Configuration.RawSystemServiceModel.Children, child => child.Name == "behaviors");
        var unknownBehaviors = Assert.Single(behaviors.Children, child => child.Name == "unknownLegacyBehaviors");
        var rawBehavior = Assert.Single(unknownBehaviors.Children, child => child.Name == "behavior");
        Assert.Equal("LegacyBehavior", rawBehavior.Attributes["name"]);
    }

    [Fact]
    public void Read_WhenBritishSpellingBehavioursExists_PopulatesTypedBehaviours()
    {
        var filePath = WriteConfig("""
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
""");

        var result = LegacyWcfConfigurationReader.Read(filePath);

        Assert.True(result.Success);
        Assert.NotNull(result.Configuration);

        var serviceBehavior = Assert.Single(result.Configuration!.Behaviors.ServiceBehaviors);
        var endpointBehavior = Assert.Single(result.Configuration.Behaviors.EndpointBehaviors);

        Assert.Equal("serviceBehavior", serviceBehavior.BehaviorType);
        Assert.Equal("BritishServiceBehaviour", serviceBehavior.Name);
        Assert.Equal("behaviour", serviceBehavior.RawElement.Name);
        Assert.Single(serviceBehavior.RawElement.Children, child => child.Name == "serviceMetadata");

        Assert.Equal("endpointBehavior", endpointBehavior.BehaviorType);
        Assert.Equal("BritishEndpointBehaviour", endpointBehavior.Name);
        Assert.Equal("behaviour", endpointBehavior.RawElement.Name);
        Assert.Single(endpointBehavior.RawElement.Children, child => child.Name == "clientCredentials");
    }


    [Fact]
    public void Read_WhenClientEndpointExists_PopulatesTypedClientEndpoint()
    {
        var filePath = WriteConfig("""
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
""");

        var result = LegacyWcfConfigurationReader.Read(filePath);

        Assert.True(result.Success);
        Assert.NotNull(result.Configuration);
        Assert.NotNull(result.Configuration!.Client);

        var endpoint = Assert.Single(result.Configuration.Client!.Endpoints);
        Assert.Equal("CustomerClient", endpoint.Name);
        Assert.Equal("http://localhost:8080/CustomerService", endpoint.Address);
        Assert.Equal("basicHttpBinding", endpoint.Binding);
        Assert.Equal("CustomerBinding", endpoint.BindingConfiguration);
        Assert.Equal("MyCompany.Services.ICustomerService", endpoint.Contract);
        Assert.Null(endpoint.BehaviorConfiguration);
        Assert.Equal("CustomerClient", endpoint.Attributes["name"]);
        Assert.Equal("http://localhost:8080/CustomerService", endpoint.Attributes["address"]);
        Assert.Equal("basicHttpBinding", endpoint.Attributes["binding"]);
        Assert.Equal("CustomerBinding", endpoint.Attributes["bindingConfiguration"]);
        Assert.Equal("MyCompany.Services.ICustomerService", endpoint.Attributes["contract"]);
        Assert.NotNull(endpoint.RawElement);
        Assert.Equal("endpoint", endpoint.RawElement.Name);
    }

    [Fact]
    public void Read_WhenClientEndpointHasBehaviorConfiguration_PopulatesBehaviorConfiguration()
    {
        var filePath = WriteConfig("""
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
""");

        var result = LegacyWcfConfigurationReader.Read(filePath);

        Assert.True(result.Success);
        Assert.NotNull(result.Configuration);
        Assert.NotNull(result.Configuration!.Client);

        var endpoint = Assert.Single(result.Configuration.Client!.Endpoints);
        Assert.Equal("CustomerEndpointBehavior", endpoint.BehaviorConfiguration);
        Assert.Equal("CustomerEndpointBehavior", endpoint.Attributes["behaviorConfiguration"]);
    }

    [Fact]
    public void Read_WhenClientHasMultipleEndpoints_PopulatesAllClientEndpointsInOrder()
    {
        var filePath = WriteConfig("""
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
""");

        var result = LegacyWcfConfigurationReader.Read(filePath);

        Assert.True(result.Success);
        Assert.NotNull(result.Configuration);
        Assert.NotNull(result.Configuration!.Client);
        Assert.Equal(2, result.Configuration.Client!.Endpoints.Count);
        Assert.Equal("CustomerClient", result.Configuration.Client.Endpoints[0].Name);
        Assert.Equal("http://localhost:8080/CustomerService", result.Configuration.Client.Endpoints[0].Address);
        Assert.Equal("MyCompany.Services.ICustomerService", result.Configuration.Client.Endpoints[0].Contract);
        Assert.Equal("OrderClient", result.Configuration.Client.Endpoints[1].Name);
        Assert.Equal("http://localhost:8080/OrderService", result.Configuration.Client.Endpoints[1].Address);
        Assert.Equal("MyCompany.Services.IOrderService", result.Configuration.Client.Endpoints[1].Contract);
        Assert.NotSame(result.Configuration.Client.Endpoints[0].RawElement, result.Configuration.Client.Endpoints[1].RawElement);
    }

    [Fact]
    public void Read_WhenClientElementIsMissing_ClientIsNull()
    {
        var filePath = WriteConfig("""
<configuration>
  <system.serviceModel>
    <services>
      <service name="MyCompany.Services.CustomerService">
        <host>
          <baseAddresses>
            <add baseAddress="http://localhost:8080/CustomerService" />
          </baseAddresses>
        </host>
        <endpoint contract="MyCompany.Services.ICustomerService" />
      </service>
    </services>
    <bindings>
      <basicHttpBinding>
        <binding name="CustomerBinding" />
      </basicHttpBinding>
    </bindings>
    <behaviors>
      <serviceBehaviors>
        <behavior name="CustomerServiceBehavior" />
      </serviceBehaviors>
    </behaviors>
  </system.serviceModel>
</configuration>
""");

        var result = LegacyWcfConfigurationReader.Read(filePath);

        Assert.True(result.Success);
        Assert.NotNull(result.Configuration);
        Assert.Null(result.Configuration!.Client);
        Assert.Single(result.Configuration.Services);
        Assert.NotNull(result.Configuration.Services[0].Host);
        Assert.Single(result.Configuration.Bindings.BasicHttp);
        Assert.Single(result.Configuration.Behaviors.ServiceBehaviors);
    }

    [Fact]
    public void Read_WhenClientHasNoEndpoints_ClientExistsWithEmptyEndpointCollection()
    {
        var filePath = WriteConfig("""
<configuration>
  <system.serviceModel>
    <client>
    </client>
  </system.serviceModel>
</configuration>
""");

        var result = LegacyWcfConfigurationReader.Read(filePath);

        Assert.True(result.Success);
        Assert.NotNull(result.Configuration);
        Assert.NotNull(result.Configuration!.Client);
        Assert.Empty(result.Configuration.Client!.Endpoints);
        Assert.Equal("client", result.Configuration.Client.RawElement.Name);
    }

    [Fact]
    public void Read_WhenClientEndpointAttributesAreMissing_PreservesEndpointWithNullTypedValues()
    {
        var filePath = WriteConfig("""
<configuration>
  <system.serviceModel>
    <client>
      <endpoint contract="MyCompany.Services.ICustomerService" />
    </client>
  </system.serviceModel>
</configuration>
""");

        var result = LegacyWcfConfigurationReader.Read(filePath);

        Assert.True(result.Success);
        Assert.NotNull(result.Configuration);
        Assert.NotNull(result.Configuration!.Client);

        var endpoint = Assert.Single(result.Configuration.Client!.Endpoints);
        Assert.Null(endpoint.Name);
        Assert.Null(endpoint.Address);
        Assert.Null(endpoint.Binding);
        Assert.Null(endpoint.BindingConfiguration);
        Assert.Equal("MyCompany.Services.ICustomerService", endpoint.Contract);
        Assert.Null(endpoint.BehaviorConfiguration);
        Assert.NotNull(endpoint.RawElement);
        Assert.Equal("endpoint", endpoint.RawElement.Name);
    }

    [Fact]
    public void Read_WhenClientEndpointContainsUnknownAttribute_PreservesAttribute()
    {
        var filePath = WriteConfig("""
<configuration>
  <system.serviceModel>
    <client>
      <endpoint
        name="CustomerClient"
        contract="MyCompany.Services.ICustomerService"
        customAttribute="abc" />
    </client>
  </system.serviceModel>
</configuration>
""");

        var result = LegacyWcfConfigurationReader.Read(filePath);

        Assert.True(result.Success);
        Assert.NotNull(result.Configuration);
        Assert.Empty(result.Diagnostics);
        Assert.NotNull(result.Configuration!.Client);

        var endpoint = Assert.Single(result.Configuration.Client!.Endpoints);
        Assert.Equal("CustomerClient", endpoint.Name);
        Assert.Equal("MyCompany.Services.ICustomerService", endpoint.Contract);
        Assert.Equal("abc", endpoint.Attributes["customAttribute"]);
    }

    [Fact]
    public void Read_WhenClientContainsUnknownChild_PreservesRawChild()
    {
        var filePath = WriteConfig("""
<configuration>
  <system.serviceModel>
    <client>
      <customClientChild value="abc" />
    </client>
  </system.serviceModel>
</configuration>
""");

        var result = LegacyWcfConfigurationReader.Read(filePath);

        Assert.True(result.Success);
        Assert.NotNull(result.Configuration);
        Assert.Empty(result.Diagnostics);
        Assert.NotNull(result.Configuration!.Client);
        Assert.Empty(result.Configuration.Client!.Endpoints);

        var customClientChild = Assert.Single(result.Configuration.Client.RawElement.Children, child => child.Name == "customClientChild");
        Assert.Equal("abc", customClientChild.Attributes["value"]);
        Assert.False(customClientChild.IsKnownElement);
    }


    [Fact]
    public void Read_WhenServiceHostingEnvironmentExists_PopulatesTypedServiceHostingEnvironment()
    {
        var filePath = WriteConfig("""
<configuration>
  <system.serviceModel>
    <serviceHostingEnvironment
      aspNetCompatibilityEnabled="true"
      multipleSiteBindingsEnabled="true" />
  </system.serviceModel>
</configuration>
""");

        var result = LegacyWcfConfigurationReader.Read(filePath);

        Assert.True(result.Success);
        Assert.NotNull(result.Configuration);

        var serviceHostingEnvironment = result.Configuration!.ServiceHostingEnvironment;
        Assert.NotNull(serviceHostingEnvironment);
        Assert.Equal("true", serviceHostingEnvironment!.AspNetCompatibilityEnabled);
        Assert.Equal("true", serviceHostingEnvironment.MultipleSiteBindingsEnabled);
        Assert.Equal("true", serviceHostingEnvironment.Attributes["aspNetCompatibilityEnabled"]);
        Assert.Equal("true", serviceHostingEnvironment.Attributes["multipleSiteBindingsEnabled"]);
        Assert.NotNull(serviceHostingEnvironment.RawElement);
        Assert.Equal("serviceHostingEnvironment", serviceHostingEnvironment.RawElement.Name);
    }

    [Fact]
    public void Read_WhenServiceHostingEnvironmentHasOnlyAspNetCompatibilityEnabled_PopulatesAvailableAttribute()
    {
        var filePath = WriteConfig("""
<configuration>
  <system.serviceModel>
    <serviceHostingEnvironment aspNetCompatibilityEnabled="false" />
  </system.serviceModel>
</configuration>
""");

        var result = LegacyWcfConfigurationReader.Read(filePath);

        Assert.True(result.Success);
        Assert.NotNull(result.Configuration);

        var serviceHostingEnvironment = result.Configuration!.ServiceHostingEnvironment;
        Assert.NotNull(serviceHostingEnvironment);
        Assert.Equal("false", serviceHostingEnvironment!.AspNetCompatibilityEnabled);
        Assert.Null(serviceHostingEnvironment.MultipleSiteBindingsEnabled);
        Assert.Equal("false", serviceHostingEnvironment.Attributes["aspNetCompatibilityEnabled"]);
    }

    [Fact]
    public void Read_WhenServiceHostingEnvironmentHasNoAttributes_PreservesEmptyElement()
    {
        var filePath = WriteConfig("""
<configuration>
  <system.serviceModel>
    <serviceHostingEnvironment />
  </system.serviceModel>
</configuration>
""");

        var result = LegacyWcfConfigurationReader.Read(filePath);

        Assert.True(result.Success);
        Assert.NotNull(result.Configuration);

        var serviceHostingEnvironment = result.Configuration!.ServiceHostingEnvironment;
        Assert.NotNull(serviceHostingEnvironment);
        Assert.Null(serviceHostingEnvironment!.AspNetCompatibilityEnabled);
        Assert.Null(serviceHostingEnvironment.MultipleSiteBindingsEnabled);
        Assert.Empty(serviceHostingEnvironment.Attributes);
        Assert.Equal("serviceHostingEnvironment", serviceHostingEnvironment.RawElement.Name);
    }

    [Fact]
    public void Read_WhenServiceHostingEnvironmentIsMissing_ServiceHostingEnvironmentIsNull()
    {
        var filePath = WriteConfig("""
<configuration>
  <system.serviceModel>
    <services>
      <service name="MyCompany.Services.CustomerService">
        <host>
          <baseAddresses>
            <add baseAddress="http://localhost:8080/CustomerService" />
          </baseAddresses>
        </host>
      </service>
    </services>
    <bindings>
      <basicHttpBinding>
        <binding name="CustomerBinding" />
      </basicHttpBinding>
    </bindings>
    <behaviors>
      <serviceBehaviors>
        <behavior name="CustomerServiceBehavior" />
      </serviceBehaviors>
    </behaviors>
    <client>
      <endpoint name="CustomerClient" contract="MyCompany.Services.ICustomerService" />
    </client>
  </system.serviceModel>
</configuration>
""");

        var result = LegacyWcfConfigurationReader.Read(filePath);

        Assert.True(result.Success);
        Assert.NotNull(result.Configuration);
        Assert.Null(result.Configuration!.ServiceHostingEnvironment);
        Assert.Single(result.Configuration.Services);
        Assert.NotNull(result.Configuration.Services[0].Host);
        Assert.Single(result.Configuration.Bindings.BasicHttp);
        Assert.Single(result.Configuration.Behaviors.ServiceBehaviors);
        Assert.NotNull(result.Configuration.Client);
        Assert.Single(result.Configuration.Client!.Endpoints);
    }

    [Fact]
    public void Read_WhenServiceHostingEnvironmentContainsUnknownAttribute_PreservesAttribute()
    {
        var filePath = WriteConfig("""
<configuration>
  <system.serviceModel>
    <serviceHostingEnvironment
      aspNetCompatibilityEnabled="true"
      customAttribute="abc" />
  </system.serviceModel>
</configuration>
""");

        var result = LegacyWcfConfigurationReader.Read(filePath);

        Assert.True(result.Success);
        Assert.NotNull(result.Configuration);
        Assert.Empty(result.Diagnostics);

        var serviceHostingEnvironment = result.Configuration!.ServiceHostingEnvironment;
        Assert.NotNull(serviceHostingEnvironment);
        Assert.Equal("true", serviceHostingEnvironment!.AspNetCompatibilityEnabled);
        Assert.Equal("abc", serviceHostingEnvironment.Attributes["customAttribute"]);
    }

    [Fact]
    public void Read_WhenServiceHostingEnvironmentContainsUnknownChild_PreservesRawChild()
    {
        var filePath = WriteConfig("""
<configuration>
  <system.serviceModel>
    <serviceHostingEnvironment aspNetCompatibilityEnabled="true">
      <customHostingChild value="abc" />
    </serviceHostingEnvironment>
  </system.serviceModel>
</configuration>
""");

        var result = LegacyWcfConfigurationReader.Read(filePath);

        Assert.True(result.Success);
        Assert.NotNull(result.Configuration);
        Assert.Empty(result.Diagnostics);

        var serviceHostingEnvironment = result.Configuration!.ServiceHostingEnvironment;
        Assert.NotNull(serviceHostingEnvironment);

        var customHostingChild = Assert.Single(serviceHostingEnvironment!.RawElement.Children, child => child.Name == "customHostingChild");
        Assert.Equal("abc", customHostingChild.Attributes["value"]);
        Assert.False(customHostingChild.IsKnownElement);
    }

    [Fact]
    public void Read_WhenMultipleServiceHostingEnvironmentElementsExist_UsesFirstTypedElementAndPreservesRawXml()
    {
        var filePath = WriteConfig("""
<configuration>
  <system.serviceModel>
    <serviceHostingEnvironment aspNetCompatibilityEnabled="true" />
    <serviceHostingEnvironment aspNetCompatibilityEnabled="false" multipleSiteBindingsEnabled="true" />
  </system.serviceModel>
</configuration>
""");

        var result = LegacyWcfConfigurationReader.Read(filePath);

        Assert.True(result.Success);
        Assert.NotNull(result.Configuration);
        Assert.Empty(result.Diagnostics);

        var serviceHostingEnvironment = result.Configuration!.ServiceHostingEnvironment;
        Assert.NotNull(serviceHostingEnvironment);
        Assert.Equal("true", serviceHostingEnvironment!.AspNetCompatibilityEnabled);
        Assert.Null(serviceHostingEnvironment.MultipleSiteBindingsEnabled);

        var rawElements = result.Configuration.RawSystemServiceModel.Children
            .Where(child => child.Name == "serviceHostingEnvironment")
            .ToList();

        Assert.Equal(2, rawElements.Count);
    }


    [Fact]
    public void Services_Find_WhenServiceExists_ReturnsService()
    {
        var filePath = WriteConfig("""
<configuration>
  <system.serviceModel>
    <services>
      <service name="MyCompany.Services.CustomerService" />
      <service name="MyCompany.Services.OrderService" />
    </services>
  </system.serviceModel>
</configuration>
""");

        var result = LegacyWcfConfigurationReader.Read(filePath);

        Assert.True(result.Success);
        Assert.NotNull(result.Configuration);

        var service = result.Configuration!.Services.Find("mycompany.services.customerservice");

        Assert.NotNull(service);
        Assert.Equal("MyCompany.Services.CustomerService", service!.Name);
    }

    [Fact]
    public void Services_Find_WhenServiceDoesNotExist_ReturnsNull()
    {
        var filePath = WriteConfig("""
<configuration>
  <system.serviceModel>
    <services>
      <service name="MyCompany.Services.CustomerService" />
    </services>
  </system.serviceModel>
</configuration>
""");

        var result = LegacyWcfConfigurationReader.Read(filePath);

        Assert.True(result.Success);
        Assert.NotNull(result.Configuration);
        Assert.Null(result.Configuration!.Services.Find("Missing.Service"));
    }

    [Fact]
    public void Services_GetRequired_WhenServiceExists_ReturnsService()
    {
        var filePath = WriteConfig("""
<configuration>
  <system.serviceModel>
    <services>
      <service name="MyCompany.Services.CustomerService" />
    </services>
  </system.serviceModel>
</configuration>
""");

        var result = LegacyWcfConfigurationReader.Read(filePath);

        Assert.True(result.Success);
        Assert.NotNull(result.Configuration);

        var service = result.Configuration!.Services.GetRequired("MyCompany.Services.CustomerService");

        Assert.Equal("MyCompany.Services.CustomerService", service.Name);
    }

    [Fact]
    public void Services_GetRequired_WhenServiceDoesNotExist_ThrowsClearException()
    {
        var filePath = WriteConfig("""
<configuration>
  <system.serviceModel>
    <services>
      <service name="MyCompany.Services.CustomerService" />
    </services>
  </system.serviceModel>
</configuration>
""");

        var result = LegacyWcfConfigurationReader.Read(filePath);

        Assert.True(result.Success);
        Assert.NotNull(result.Configuration);

        var exception = Assert.Throws<InvalidOperationException>(
            () => result.Configuration!.Services.GetRequired("Missing.Service"));

        Assert.Contains("Missing.Service", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ServiceEndpoints_FindByName_WhenEndpointExists_ReturnsEndpoint()
    {
        var filePath = WriteConfig("""
<configuration>
  <system.serviceModel>
    <services>
      <service name="MyCompany.Services.CustomerService">
        <endpoint
          name="CustomerEndpoint"
          address="basic"
          binding="basicHttpBinding"
          contract="MyCompany.Services.ICustomerService" />
      </service>
    </services>
  </system.serviceModel>
</configuration>
""");

        var result = LegacyWcfConfigurationReader.Read(filePath);

        Assert.True(result.Success);
        Assert.NotNull(result.Configuration);

        var endpoint = result.Configuration!.Services[0].Endpoints.FindByName("customerendpoint");

        Assert.NotNull(endpoint);
        Assert.Equal("CustomerEndpoint", endpoint!.Name);
    }

    [Fact]
    public void ServiceEndpoints_FindByContract_WhenEndpointExists_ReturnsEndpoint()
    {
        var filePath = WriteConfig("""
<configuration>
  <system.serviceModel>
    <services>
      <service name="MyCompany.Services.CustomerService">
        <endpoint
          name="CustomerEndpoint"
          address="basic"
          binding="basicHttpBinding"
          contract="MyCompany.Services.ICustomerService" />
      </service>
    </services>
  </system.serviceModel>
</configuration>
""");

        var result = LegacyWcfConfigurationReader.Read(filePath);

        Assert.True(result.Success);
        Assert.NotNull(result.Configuration);

        var endpoint = result.Configuration!.Services[0].Endpoints.FindByContract("mycompany.services.icustomerservice");

        Assert.NotNull(endpoint);
        Assert.Equal("MyCompany.Services.ICustomerService", endpoint!.Contract);
    }

    [Fact]
    public void ServiceEndpoints_GetRequiredByName_WhenEndpointMissing_ThrowsClearException()
    {
        var filePath = WriteConfig("""
<configuration>
  <system.serviceModel>
    <services>
      <service name="MyCompany.Services.CustomerService">
        <endpoint name="CustomerEndpoint" contract="MyCompany.Services.ICustomerService" />
      </service>
    </services>
  </system.serviceModel>
</configuration>
""");

        var result = LegacyWcfConfigurationReader.Read(filePath);

        Assert.True(result.Success);
        Assert.NotNull(result.Configuration);

        var exception = Assert.Throws<InvalidOperationException>(
            () => result.Configuration!.Services[0].Endpoints.GetRequiredByName("MissingEndpoint"));

        Assert.Contains("MissingEndpoint", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ServiceEndpoints_GetRequiredByContract_WhenEndpointMissing_ThrowsClearException()
    {
        var filePath = WriteConfig("""
<configuration>
  <system.serviceModel>
    <services>
      <service name="MyCompany.Services.CustomerService">
        <endpoint name="CustomerEndpoint" contract="MyCompany.Services.ICustomerService" />
      </service>
    </services>
  </system.serviceModel>
</configuration>
""");

        var result = LegacyWcfConfigurationReader.Read(filePath);

        Assert.True(result.Success);
        Assert.NotNull(result.Configuration);

        var exception = Assert.Throws<InvalidOperationException>(
            () => result.Configuration!.Services[0].Endpoints.GetRequiredByContract("Missing.Contract"));

        Assert.Contains("Missing.Contract", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Bindings_Find_OnBindingCollection_WhenNamedBindingExists_ReturnsBinding()
    {
        var filePath = WriteConfig("""
<configuration>
  <system.serviceModel>
    <bindings>
      <basicHttpBinding>
        <binding name="CustomerBinding" />
      </basicHttpBinding>
    </bindings>
  </system.serviceModel>
</configuration>
""");

        var result = LegacyWcfConfigurationReader.Read(filePath);

        Assert.True(result.Success);
        Assert.NotNull(result.Configuration);

        var binding = result.Configuration!.Bindings.BasicHttp.Find("customerbinding");

        Assert.NotNull(binding);
        Assert.Equal("CustomerBinding", binding!.Name);
    }

    [Fact]
    public void Bindings_Find_OnBindingCollection_WhenUnnamedBindingExists_ReturnsUnnamedBinding()
    {
        var filePath = WriteConfig("""
<configuration>
  <system.serviceModel>
    <bindings>
      <basicHttpBinding>
        <binding maxReceivedMessageSize="65536" />
      </basicHttpBinding>
    </bindings>
  </system.serviceModel>
</configuration>
""");

        var result = LegacyWcfConfigurationReader.Read(filePath);

        Assert.True(result.Success);
        Assert.NotNull(result.Configuration);

        var binding = result.Configuration!.Bindings.BasicHttp.Find(null);

        Assert.NotNull(binding);
        Assert.Null(binding!.Name);
        Assert.Equal("65536", binding.Attributes["maxReceivedMessageSize"]);
    }

    [Fact]
    public void Bindings_GetRequired_OnBindingCollection_WhenMissing_ThrowsClearException()
    {
        var filePath = WriteConfig("""
<configuration>
  <system.serviceModel>
    <bindings>
      <basicHttpBinding>
        <binding name="CustomerBinding" />
      </basicHttpBinding>
    </bindings>
  </system.serviceModel>
</configuration>
""");

        var result = LegacyWcfConfigurationReader.Read(filePath);

        Assert.True(result.Success);
        Assert.NotNull(result.Configuration);

        var exception = Assert.Throws<InvalidOperationException>(
            () => result.Configuration!.Bindings.BasicHttp.GetRequired("MissingBinding"));

        Assert.Contains("MissingBinding", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Bindings_Find_OnTopLevelBindings_WhenKnownBindingTypeAndNameExist_ReturnsBinding()
    {
        var filePath = WriteConfig("""
<configuration>
  <system.serviceModel>
    <bindings>
      <basicHttpBinding>
        <binding name="CustomerBasicBinding" />
      </basicHttpBinding>
      <wsHttpBinding>
        <binding name="CustomerWsBinding" />
      </wsHttpBinding>
      <netTcpBinding>
        <binding name="CustomerTcpBinding" />
      </netTcpBinding>
      <customBinding>
        <binding name="CustomerCustomBinding" />
      </customBinding>
    </bindings>
  </system.serviceModel>
</configuration>
""");

        var result = LegacyWcfConfigurationReader.Read(filePath);

        Assert.True(result.Success);
        Assert.NotNull(result.Configuration);

        Assert.Equal("CustomerBasicBinding", result.Configuration!.Bindings.Find("basicHttpBinding", "customerbasicbinding")!.Name);
        Assert.Equal("CustomerWsBinding", result.Configuration.Bindings.Find("wsHttpBinding", "customerwsbinding")!.Name);
        Assert.Equal("CustomerTcpBinding", result.Configuration.Bindings.Find("netTcpBinding", "customertcpbinding")!.Name);
        Assert.Equal("CustomerCustomBinding", result.Configuration.Bindings.Find("customBinding", "customercustombinding")!.Name);
    }

    [Fact]
    public void Bindings_Find_OnTopLevelBindings_WhenUnknownBindingType_ReturnsNull()
    {
        var filePath = WriteConfig("""
<configuration>
  <system.serviceModel>
    <bindings>
      <basicHttpBinding>
        <binding name="CustomerBinding" />
      </basicHttpBinding>
    </bindings>
  </system.serviceModel>
</configuration>
""");

        var result = LegacyWcfConfigurationReader.Read(filePath);

        Assert.True(result.Success);
        Assert.NotNull(result.Configuration);
        Assert.Null(result.Configuration!.Bindings.Find("unknownBinding", "CustomerBinding"));
    }

    [Fact]
    public void Bindings_GetRequired_OnTopLevelBindings_WhenMissing_ThrowsClearException()
    {
        var filePath = WriteConfig("""
<configuration>
  <system.serviceModel>
    <bindings>
      <basicHttpBinding>
        <binding name="CustomerBinding" />
      </basicHttpBinding>
    </bindings>
  </system.serviceModel>
</configuration>
""");

        var result = LegacyWcfConfigurationReader.Read(filePath);

        Assert.True(result.Success);
        Assert.NotNull(result.Configuration);

        var exception = Assert.Throws<InvalidOperationException>(
            () => result.Configuration!.Bindings.GetRequired("basicHttpBinding", "MissingBinding"));

        Assert.Contains("basicHttpBinding", exception.Message, StringComparison.Ordinal);
        Assert.Contains("MissingBinding", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Behaviors_Find_WhenNamedBehaviorExists_ReturnsBehavior()
    {
        var filePath = WriteConfig("""
<configuration>
  <system.serviceModel>
    <behaviors>
      <serviceBehaviors>
        <behavior name="CustomerServiceBehavior" />
      </serviceBehaviors>
      <endpointBehaviors>
        <behavior name="CustomerEndpointBehavior" />
      </endpointBehaviors>
    </behaviors>
  </system.serviceModel>
</configuration>
""");

        var result = LegacyWcfConfigurationReader.Read(filePath);

        Assert.True(result.Success);
        Assert.NotNull(result.Configuration);

        Assert.Equal("CustomerServiceBehavior", result.Configuration!.Behaviors.ServiceBehaviors.Find("customerservicebehavior")!.Name);
        Assert.Equal("CustomerEndpointBehavior", result.Configuration.Behaviors.EndpointBehaviors.Find("customerendpointbehavior")!.Name);
    }

    [Fact]
    public void Behaviors_Find_WhenUnnamedBehaviorExists_ReturnsUnnamedBehavior()
    {
        var filePath = WriteConfig("""
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
""");

        var result = LegacyWcfConfigurationReader.Read(filePath);

        Assert.True(result.Success);
        Assert.NotNull(result.Configuration);

        var behavior = result.Configuration!.Behaviors.ServiceBehaviors.Find(null);

        Assert.NotNull(behavior);
        Assert.Null(behavior!.Name);
    }

    [Fact]
    public void Behaviors_GetRequired_WhenMissing_ThrowsClearException()
    {
        var filePath = WriteConfig("""
<configuration>
  <system.serviceModel>
    <behaviors>
      <serviceBehaviors>
        <behavior name="CustomerServiceBehavior" />
      </serviceBehaviors>
    </behaviors>
  </system.serviceModel>
</configuration>
""");

        var result = LegacyWcfConfigurationReader.Read(filePath);

        Assert.True(result.Success);
        Assert.NotNull(result.Configuration);

        var exception = Assert.Throws<InvalidOperationException>(
            () => result.Configuration!.Behaviors.ServiceBehaviors.GetRequired("MissingBehavior"));

        Assert.Contains("MissingBehavior", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ClientEndpoints_FindByName_WhenEndpointExists_ReturnsEndpoint()
    {
        var filePath = WriteConfig("""
<configuration>
  <system.serviceModel>
    <client>
      <endpoint
        name="CustomerClient"
        address="http://localhost:8080/CustomerService"
        binding="basicHttpBinding"
        contract="MyCompany.Services.ICustomerService" />
    </client>
  </system.serviceModel>
</configuration>
""");

        var result = LegacyWcfConfigurationReader.Read(filePath);

        Assert.True(result.Success);
        Assert.NotNull(result.Configuration);
        Assert.NotNull(result.Configuration!.Client);

        var endpoint = result.Configuration.Client!.Endpoints.FindByName("customerclient");

        Assert.NotNull(endpoint);
        Assert.Equal("CustomerClient", endpoint!.Name);
    }

    [Fact]
    public void ClientEndpoints_FindByContract_WhenEndpointExists_ReturnsEndpoint()
    {
        var filePath = WriteConfig("""
<configuration>
  <system.serviceModel>
    <client>
      <endpoint
        name="CustomerClient"
        address="http://localhost:8080/CustomerService"
        binding="basicHttpBinding"
        contract="MyCompany.Services.ICustomerService" />
    </client>
  </system.serviceModel>
</configuration>
""");

        var result = LegacyWcfConfigurationReader.Read(filePath);

        Assert.True(result.Success);
        Assert.NotNull(result.Configuration);
        Assert.NotNull(result.Configuration!.Client);

        var endpoint = result.Configuration.Client!.Endpoints.FindByContract("mycompany.services.icustomerservice");

        Assert.NotNull(endpoint);
        Assert.Equal("MyCompany.Services.ICustomerService", endpoint!.Contract);
    }

    [Fact]
    public void ClientEndpoints_GetRequiredByName_WhenEndpointMissing_ThrowsClearException()
    {
        var filePath = WriteConfig("""
<configuration>
  <system.serviceModel>
    <client>
      <endpoint name="CustomerClient" contract="MyCompany.Services.ICustomerService" />
    </client>
  </system.serviceModel>
</configuration>
""");

        var result = LegacyWcfConfigurationReader.Read(filePath);

        Assert.True(result.Success);
        Assert.NotNull(result.Configuration);
        Assert.NotNull(result.Configuration!.Client);

        var exception = Assert.Throws<InvalidOperationException>(
            () => result.Configuration.Client!.Endpoints.GetRequiredByName("MissingClient"));

        Assert.Contains("MissingClient", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ClientEndpoints_GetRequiredByContract_WhenEndpointMissing_ThrowsClearException()
    {
        var filePath = WriteConfig("""
<configuration>
  <system.serviceModel>
    <client>
      <endpoint name="CustomerClient" contract="MyCompany.Services.ICustomerService" />
    </client>
  </system.serviceModel>
</configuration>
""");

        var result = LegacyWcfConfigurationReader.Read(filePath);

        Assert.True(result.Success);
        Assert.NotNull(result.Configuration);
        Assert.NotNull(result.Configuration!.Client);

        var exception = Assert.Throws<InvalidOperationException>(
            () => result.Configuration.Client!.Endpoints.GetRequiredByContract("Missing.Contract"));

        Assert.Contains("Missing.Contract", exception.Message, StringComparison.Ordinal);
    }

    private string WriteConfig(string xml)
    {
        var filePath = Path.Combine(_tempDirectory, $"{Guid.NewGuid():N}.config");
        File.WriteAllText(filePath, xml);

        return filePath;
    }
}
