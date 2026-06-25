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
        Assert.Equal(0, result.Configuration!.Services.Count);
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

    private string WriteConfig(string xml)
    {
        var filePath = Path.Combine(_tempDirectory, $"{Guid.NewGuid():N}.config");
        File.WriteAllText(filePath, xml);

        return filePath;
    }
}
