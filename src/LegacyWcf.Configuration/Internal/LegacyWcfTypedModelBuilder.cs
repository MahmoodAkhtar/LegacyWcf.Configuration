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

        return new LegacyWcfService(
            name: GetAttributeOrDefault(serviceElement, "name", string.Empty),
            behaviorConfiguration: GetAttributeOrNull(serviceElement, "behaviorConfiguration"),
            endpoints: endpoints.Count == 0 ? LegacyWcfServiceEndpoints.Empty : new LegacyWcfServiceEndpoints(endpoints),
            rawElement: serviceElement);
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
