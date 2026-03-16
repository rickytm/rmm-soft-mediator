using Microsoft.Extensions.DependencyInjection;
using RMMSoft.Mediator.Abstractions;
using RMMSoft.Mediator.Implementation;
using System.Reflection;

namespace RMMSoft.Mediator.Extensions;

public static class MediatorServiceExtensions
{
    /// <summary>
    /// Registers the RMMSoft.Mediator services and handlers from the specified assemblies.
    /// </summary>
    /// <param name="services">The IServiceCollection to add the services to.</param>
    /// <param name="assemblies">The assemblies to scan for handlers and behaviors.</param>
    /// <returns>The IServiceCollection with the registered services.</returns>
    public static IServiceCollection AddRMMSoftMediator(this IServiceCollection services, params Assembly[] assemblies)
    {
        services.AddScoped<IAppMediator, AppMediator>();

        foreach (var assembly in assemblies)
        {
            // Request handlers
            var handlerTypes = assembly.GetTypes()
                .Where(t => t.GetInterfaces().Any(i =>
                    i.IsGenericType &&
                    (i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>) ||
                     i.GetGenericTypeDefinition() == typeof(IRequestHandler<>))));

            foreach (var handler in handlerTypes)
            {
                foreach (var iface in handler.GetInterfaces()
                    .Where(i => i.IsGenericType &&
                        (i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>) ||
                         i.GetGenericTypeDefinition() == typeof(IRequestHandler<>))))
                {
                    services.AddTransient(iface, handler);
                }
            }

            // Notification handlers
            services.Scan(scan => scan
                .FromAssemblies(assembly)
                .AddClasses(classes => classes.AssignableTo(typeof(INotificationHandler<>)))
                .AsImplementedInterfaces()
                .WithScopedLifetime());

            // Notification behaviors
            services.Scan(scan => scan
                .FromAssemblies(assembly)
                .AddClasses(classes => classes.AssignableTo(typeof(INotificationBehavior<>)))
                .AsImplementedInterfaces()
                .WithScopedLifetime());
        }

        return services;
    }

    /// <summary>
    /// Registers a pipeline behavior for all request handlers. The behavior must implement IPipelineBehavior<TRequest, TResponse> for the appropriate request and response types.
    /// </summary>
    /// <typeparam name="TBehavior">The type of the pipeline behavior to register.</typeparam>
    /// <param name="services">The IServiceCollection to add the behavior to.</param>
    /// <returns>The IServiceCollection with the registered behavior.</returns>
    public static IServiceCollection AddMediatorBehavior<TBehavior>(this IServiceCollection services) where TBehavior : class
    {
        var behaviorInterfaces = typeof(TBehavior).GetInterfaces()
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>));

        foreach (var iface in behaviorInterfaces)
        {
            services.AddTransient(iface, typeof(TBehavior));
        }

        return services;
    }
}


