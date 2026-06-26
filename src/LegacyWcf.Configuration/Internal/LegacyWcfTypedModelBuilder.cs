using System;
using System.Collections.Generic;
using System.Linq;

namespace LegacyWcf.Configuration.Internal;

internal static class LegacyWcfTypedModelBuilder
{
    public static LegacyWcfServices BuildServices(LegacyWcfElement rawSystemServiceModel)
    {
        if (rawSystemServiceModel is null)
        {
            throw new ArgumentNullException(nameof(rawSystemServiceModel));
        }

        var services = rawSystemServiceModel.Children
            .Where(child => IsNamed(child, "services"))
            .SelectMany(servicesElement => servicesElement.Children)
            .Where(child => IsNamed(child, "service"))
            .Select(BuildService)
            .ToList();

        return services.Count == 0
            ? LegacyWcfServices.Empty
            : new LegacyWcfServices(services);
    }

    private static LegacyWcfService BuildService(LegacyWcfElement serviceElement)
    {
        var endpoints = serviceElement.Children
            .Where(child => IsNamed(child, "endpoint"))
            .Select(BuildEndpoint)
            .ToList();

        var host = BuildHost(serviceElement);

        return new LegacyWcfService(
            name: GetAttributeOrDefault(serviceElement, "name", string.Empty),
            behaviorConfiguration: GetAttributeOrNull(serviceElement, "behaviorConfiguration"),
            endpoints: endpoints.Count == 0 ? LegacyWcfServiceEndpoints.Empty : new LegacyWcfServiceEndpoints(endpoints),
            rawElement: serviceElement,
            host: host);
    }

    private static LegacyWcfHost? BuildHost(LegacyWcfElement serviceElement)
    {
        var hostElement = serviceElement.Children.FirstOrDefault(child => IsNamed(child, "host"));

        if (hostElement is null)
        {
            return null;
        }

        var baseAddressesElement = hostElement.Children.FirstOrDefault(child => IsNamed(child, "baseAddresses"));
        var baseAddresses = baseAddressesElement is null
            ? new List<string>()
            : baseAddressesElement.Children
                .Where(child => IsNamed(child, "add"))
                .Select(child => GetAttributeOrNull(child, "baseAddress"))
                .Where(baseAddress => baseAddress is not null)
                .Select(baseAddress => baseAddress!)
                .ToList();

        var timeouts = BuildHostTimeouts(hostElement);

        return new LegacyWcfHost(
            baseAddresses: baseAddresses,
            timeouts: timeouts,
            rawElement: hostElement);
    }

    private static LegacyWcfHostTimeouts? BuildHostTimeouts(LegacyWcfElement hostElement)
    {
        var timeoutsElement = hostElement.Children.FirstOrDefault(child => IsNamed(child, "timeouts"));

        if (timeoutsElement is null)
        {
            return null;
        }

        return new LegacyWcfHostTimeouts(
            closeTimeout: GetAttributeOrNull(timeoutsElement, "closeTimeout"),
            openTimeout: GetAttributeOrNull(timeoutsElement, "openTimeout"),
            rawElement: timeoutsElement);
    }

    private static LegacyWcfServiceEndpoint BuildEndpoint(LegacyWcfElement endpointElement)
    {
        return new LegacyWcfServiceEndpoint(
            name: GetAttributeOrNull(endpointElement, "name"),
            address: GetAttributeOrNull(endpointElement, "address"),
            binding: GetAttributeOrNull(endpointElement, "binding"),
            bindingConfiguration: GetAttributeOrNull(endpointElement, "bindingConfiguration"),
            contract: GetAttributeOrNull(endpointElement, "contract"),
            behaviorConfiguration: GetAttributeOrNull(endpointElement, "behaviorConfiguration"),
            rawElement: endpointElement);
    }

    private static string GetAttributeOrDefault(LegacyWcfElement element, string name, string defaultValue)
    {
        return element.Attributes.TryGetValue(name, out var value)
            ? value
            : defaultValue;
    }

    private static string? GetAttributeOrNull(LegacyWcfElement element, string name)
    {
        return element.Attributes.TryGetValue(name, out var value)
            ? value
            : null;
    }

    private static bool IsNamed(LegacyWcfElement element, string name)
    {
        return string.Equals(element.Name, name, StringComparison.OrdinalIgnoreCase);
    }
}
