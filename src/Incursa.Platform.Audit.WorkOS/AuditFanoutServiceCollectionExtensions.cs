namespace Incursa.Platform.Audit.WorkOS;

using Incursa.Platform.Audit;
using Incursa.Platform.Audit.WorkOS.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

public static class AuditFanoutServiceCollectionExtensions
{
    public static IServiceCollection AddAuditSinkFanout(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var existingDescriptor = services.LastOrDefault(static descriptor => descriptor.ServiceType == typeof(IAuditEventWriter));
        if (existingDescriptor is null)
        {
            throw new InvalidOperationException("IAuditEventWriter must be registered before calling AddAuditSinkFanout.");
        }

        if (existingDescriptor.ImplementationType == typeof(AuditFanoutWriter))
        {
            return services;
        }

        services.Remove(existingDescriptor);

        services.Add(new ServiceDescriptor(
            typeof(IPrimaryAuditEventWriter),
            serviceProvider =>
            {
                var writer = MaterializeWriter(serviceProvider, existingDescriptor);
                return new PrimaryAuditEventWriter(writer);
            },
            existingDescriptor.Lifetime));

        services.Add(new ServiceDescriptor(
            typeof(IAuditEventWriter),
            typeof(AuditFanoutWriter),
            existingDescriptor.Lifetime));

        return services;
    }

    private static IAuditEventWriter MaterializeWriter(IServiceProvider serviceProvider, ServiceDescriptor descriptor)
    {
        if (descriptor.ImplementationInstance is IAuditEventWriter instance)
        {
            return instance;
        }

        if (descriptor.ImplementationFactory is not null)
        {
            var materialized = descriptor.ImplementationFactory(serviceProvider);
            if (materialized is not IAuditEventWriter writerFromFactory)
            {
                throw new InvalidOperationException("IAuditEventWriter factory did not return IAuditEventWriter.");
            }

            return writerFromFactory;
        }

        if (descriptor.ImplementationType is not null)
        {
            var materialized = ActivatorUtilities.GetServiceOrCreateInstance(serviceProvider, descriptor.ImplementationType);
            if (materialized is not IAuditEventWriter writerFromType)
            {
                throw new InvalidOperationException("IAuditEventWriter implementation type did not resolve to IAuditEventWriter.");
            }

            return writerFromType;
        }

        throw new InvalidOperationException("Unsupported IAuditEventWriter registration descriptor.");
    }
}
