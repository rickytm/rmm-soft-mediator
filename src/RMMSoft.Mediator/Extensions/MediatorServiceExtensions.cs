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
            // 1. FILTRADO ESTRICTO: Solo clases concretas (omitimos abstractas, interfaces y genéricos abiertos puros)
            var handlerTypes = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && !t.IsGenericTypeDefinition &&
                            t.GetInterfaces().Any(i =>
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
                    // 2. CICLO DE VIDA OPTIMIZADO: Cambiado a Scoped para unificar con DbContext y Repositorios
                    services.AddScoped(iface, handler);
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
    /// Registers a pipeline behavior for all request handlers. Supports open generic behaviors.
    /// </summary>
    /// <typeparam name="TBehavior">The type of the pipeline behavior to register.</typeparam>
    /// <param name="services">The IServiceCollection to add the behavior to.</param>
    /// <returns>The IServiceCollection with the registered behavior.</returns>
    public static IServiceCollection AddMediatorBehavior<TBehavior>(this IServiceCollection services) where TBehavior : class
    {
        var behaviorType = typeof(TBehavior);
        var behaviorInterfaces = behaviorType.GetInterfaces()
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>));

        foreach (var iface in behaviorInterfaces)
        {
            // 3. SOPORTE DE GENÉRICOS ABIERTOS POR REFLEXIÓN: 
            // Si el comportamiento es genérico abierto (ej: LoggingBehavior<TRequest, TResponse>),
            // usamos la definición abierta tanto de la interfaz como de la clase mediante ServiceDescriptor.
            if (behaviorType.IsGenericTypeDefinition)
            {
                var openInterface = iface.GetGenericTypeDefinition();
                services.Add(ServiceDescriptor.Scoped(openInterface, behaviorType));
            }
            else
            {
                services.AddScoped(iface, behaviorType);
            }
        }

        return services;
    }
}


