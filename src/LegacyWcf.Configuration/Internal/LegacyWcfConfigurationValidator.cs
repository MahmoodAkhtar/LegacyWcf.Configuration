using System;
using System.Collections.Generic;
using System.Linq;

namespace LegacyWcf.Configuration.Internal;

internal static class LegacyWcfConfigurationValidator
{
    public static IReadOnlyList<LegacyWcfDiagnostic> Validate(
        LegacyWcfElement rawSystemServiceModel,
        LegacyWcfServices services,
        LegacyWcfBindings bindings,
        LegacyWcfBehaviors behaviors,
        LegacyWcfClient? client,
        LegacyWcfServiceHostingEnvironment? serviceHostingEnvironment)
    {
        if (rawSystemServiceModel is null)
        {
            throw new ArgumentNullException(nameof(rawSystemServiceModel));
        }

        if (services is null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (bindings is null)
        {
            throw new ArgumentNullException(nameof(bindings));
        }

        if (behaviors is null)
        {
            throw new ArgumentNullException(nameof(behaviors));
        }

        var diagnostics = new List<LegacyWcfDiagnostic>();

        AddUnknownElementDiagnostics(rawSystemServiceModel, diagnostics);
        AddDuplicateServiceDiagnostics(services, diagnostics);
        AddDuplicateBindingDiagnostics(bindings.BasicHttp, diagnostics);
        AddDuplicateBindingDiagnostics(bindings.WsHttp, diagnostics);
        AddDuplicateBindingDiagnostics(bindings.NetTcp, diagnostics);
        AddDuplicateBindingDiagnostics(bindings.Custom, diagnostics);
        AddDuplicateBehaviorDiagnostics(behaviors.ServiceBehaviors, "service behaviour", "LWC1004", diagnostics);
        AddDuplicateBehaviorDiagnostics(behaviors.EndpointBehaviors, "endpoint behaviour", "LWC1005", diagnostics);
        AddDuplicateServiceHostingEnvironmentDiagnostics(rawSystemServiceModel, diagnostics);
        AddMissingServiceBehaviorDiagnostics(services, behaviors, diagnostics);
        AddMissingEndpointReferenceDiagnostics(services, bindings, behaviors, diagnostics);
        AddMissingClientEndpointReferenceDiagnostics(client, bindings, behaviors, diagnostics);

        return diagnostics;
    }

    private static void AddUnknownElementDiagnostics(
        LegacyWcfElement element,
        ICollection<LegacyWcfDiagnostic> diagnostics)
    {
        if (!element.IsKnownElement)
        {
            diagnostics.Add(CreateDiagnostic(
                LegacyWcfDiagnosticSeverity.Info,
                "The WCF configuration element <" + element.Name + "> is preserved in the raw model but is not recognised by the current typed model.",
                "LWC1001",
                element));
        }

        foreach (var child in element.Children)
        {
            AddUnknownElementDiagnostics(child, diagnostics);
        }
    }

    private static void AddDuplicateServiceDiagnostics(
        IEnumerable<LegacyWcfService> services,
        ICollection<LegacyWcfDiagnostic> diagnostics)
    {
        foreach (var duplicate in services
            .Where(service => !string.IsNullOrWhiteSpace(service.Name))
            .GroupBy(service => service.Name, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .SelectMany(group => group.Skip(1)))
        {
            diagnostics.Add(CreateDiagnostic(
                LegacyWcfDiagnosticSeverity.Warning,
                "Duplicate WCF service name '" + duplicate.Name + "' was found. Lookup helpers will return the first matching service.",
                "LWC1002",
                duplicate.RawElement));
        }
    }

    private static void AddDuplicateBindingDiagnostics(
        IEnumerable<LegacyWcfBinding> bindings,
        ICollection<LegacyWcfDiagnostic> diagnostics)
    {
        foreach (var duplicate in bindings
            .Where(binding => !string.IsNullOrWhiteSpace(binding.Name))
            .GroupBy(binding => binding.Name, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .SelectMany(group => group.Skip(1)))
        {
            diagnostics.Add(CreateDiagnostic(
                LegacyWcfDiagnosticSeverity.Warning,
                "Duplicate WCF binding name '" + duplicate.Name + "' was found for binding type '" + duplicate.BindingType + "'. Lookup helpers will return the first matching binding.",
                "LWC1003",
                duplicate.RawElement));
        }
    }

    private static void AddDuplicateBehaviorDiagnostics(
        IEnumerable<LegacyWcfBehavior> behaviors,
        string description,
        string code,
        ICollection<LegacyWcfDiagnostic> diagnostics)
    {
        foreach (var duplicate in behaviors
            .Where(behavior => !string.IsNullOrWhiteSpace(behavior.Name))
            .GroupBy(behavior => behavior.Name, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .SelectMany(group => group.Skip(1)))
        {
            diagnostics.Add(CreateDiagnostic(
                LegacyWcfDiagnosticSeverity.Warning,
                "Duplicate WCF " + description + " name '" + duplicate.Name + "' was found. Lookup helpers will return the first matching behaviour.",
                code,
                duplicate.RawElement));
        }
    }

    private static void AddDuplicateServiceHostingEnvironmentDiagnostics(
        LegacyWcfElement rawSystemServiceModel,
        ICollection<LegacyWcfDiagnostic> diagnostics)
    {
        var serviceHostingEnvironmentElements = rawSystemServiceModel.Children
            .Where(child => IsNamed(child, "serviceHostingEnvironment"))
            .ToList();

        foreach (var duplicate in serviceHostingEnvironmentElements.Skip(1))
        {
            diagnostics.Add(CreateDiagnostic(
                LegacyWcfDiagnosticSeverity.Warning,
                "Multiple direct <serviceHostingEnvironment> elements were found. The typed model uses the first element and preserves all elements in raw XML.",
                "LWC1006",
                duplicate));
        }
    }

    private static void AddMissingServiceBehaviorDiagnostics(
        IEnumerable<LegacyWcfService> services,
        LegacyWcfBehaviors behaviors,
        ICollection<LegacyWcfDiagnostic> diagnostics)
    {
        foreach (var service in services.Where(service => !string.IsNullOrWhiteSpace(service.BehaviorConfiguration)))
        {
            if (behaviors.ServiceBehaviors.Find(service.BehaviorConfiguration) is null)
            {
                diagnostics.Add(CreateDiagnostic(
                    LegacyWcfDiagnosticSeverity.Warning,
                    "Service '" + service.Name + "' references missing service behaviour configuration '" + service.BehaviorConfiguration + "'.",
                    "LWC1009",
                    service.RawElement));
            }
        }
    }

    private static void AddMissingEndpointReferenceDiagnostics(
        IEnumerable<LegacyWcfService> services,
        LegacyWcfBindings bindings,
        LegacyWcfBehaviors behaviors,
        ICollection<LegacyWcfDiagnostic> diagnostics)
    {
        foreach (var service in services)
        {
            foreach (var endpoint in service.Endpoints)
            {
                AddMissingBindingDiagnostic(
                    endpoint.Binding,
                    endpoint.BindingConfiguration,
                    "Service endpoint" + FormatEndpointContext(endpoint) + " on service '" + service.Name + "'",
                    endpoint.RawElement,
                    bindings,
                    diagnostics);

                AddMissingEndpointBehaviorDiagnostic(
                    endpoint.BehaviorConfiguration,
                    "Service endpoint" + FormatEndpointContext(endpoint) + " on service '" + service.Name + "'",
                    endpoint.RawElement,
                    behaviors,
                    diagnostics);
            }
        }
    }

    private static void AddMissingClientEndpointReferenceDiagnostics(
        LegacyWcfClient? client,
        LegacyWcfBindings bindings,
        LegacyWcfBehaviors behaviors,
        ICollection<LegacyWcfDiagnostic> diagnostics)
    {
        if (client is null)
        {
            return;
        }

        foreach (var endpoint in client.Endpoints)
        {
            AddMissingBindingDiagnostic(
                endpoint.Binding,
                endpoint.BindingConfiguration,
                "Client endpoint" + FormatEndpointContext(endpoint),
                endpoint.RawElement,
                bindings,
                diagnostics);

            AddMissingEndpointBehaviorDiagnostic(
                endpoint.BehaviorConfiguration,
                "Client endpoint" + FormatEndpointContext(endpoint),
                endpoint.RawElement,
                behaviors,
                diagnostics);
        }
    }

    private static void AddMissingBindingDiagnostic(
        string? bindingType,
        string? bindingConfiguration,
        string context,
        LegacyWcfElement rawElement,
        LegacyWcfBindings bindings,
        ICollection<LegacyWcfDiagnostic> diagnostics)
    {
        if (string.IsNullOrWhiteSpace(bindingConfiguration))
        {
            return;
        }

        if (bindings.Find(bindingType, bindingConfiguration) is not null)
        {
            return;
        }

        diagnostics.Add(CreateDiagnostic(
            LegacyWcfDiagnosticSeverity.Warning,
            context + " references missing binding configuration '" + bindingConfiguration + "' for binding type '" + FormatValue(bindingType) + "'.",
            "LWC1007",
            rawElement));
    }

    private static void AddMissingEndpointBehaviorDiagnostic(
        string? behaviorConfiguration,
        string context,
        LegacyWcfElement rawElement,
        LegacyWcfBehaviors behaviors,
        ICollection<LegacyWcfDiagnostic> diagnostics)
    {
        if (string.IsNullOrWhiteSpace(behaviorConfiguration))
        {
            return;
        }

        if (behaviors.EndpointBehaviors.Find(behaviorConfiguration) is not null)
        {
            return;
        }

        diagnostics.Add(CreateDiagnostic(
            LegacyWcfDiagnosticSeverity.Warning,
            context + " references missing endpoint behaviour configuration '" + behaviorConfiguration + "'.",
            "LWC1008",
            rawElement));
    }

    private static LegacyWcfDiagnostic CreateDiagnostic(
        LegacyWcfDiagnosticSeverity severity,
        string message,
        string code,
        LegacyWcfElement element)
    {
        return new LegacyWcfDiagnostic(
            severity,
            message,
            code,
            element.SourceFilePath,
            element.LineNumber);
    }

    private static string FormatEndpointContext(LegacyWcfServiceEndpoint endpoint)
    {
        if (!string.IsNullOrWhiteSpace(endpoint.Name))
        {
            return " '" + endpoint.Name + "'";
        }

        if (!string.IsNullOrWhiteSpace(endpoint.Contract))
        {
            return " with contract '" + endpoint.Contract + "'";
        }

        return string.Empty;
    }

    private static string FormatEndpointContext(LegacyWcfClientEndpoint endpoint)
    {
        if (!string.IsNullOrWhiteSpace(endpoint.Name))
        {
            return " '" + endpoint.Name + "'";
        }

        if (!string.IsNullOrWhiteSpace(endpoint.Contract))
        {
            return " with contract '" + endpoint.Contract + "'";
        }

        return string.Empty;
    }

    private static string FormatValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "<missing>" : value;
    }

    private static bool IsNamed(LegacyWcfElement element, string name)
    {
        return string.Equals(element.Name, name, StringComparison.OrdinalIgnoreCase);
    }
}
