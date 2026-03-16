using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RMMSoft.Mediator.Abstractions;

namespace RMMSoft.Mediator.Implementation;


public class AppMediator(IServiceProvider services, ILogger<AppMediator> logger) : IAppMediator
{
    private readonly ILogger<AppMediator> _logger = logger;
    public async Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : IRequest
    {
        // Envuelve el handler en un pipeline TResponse = Unit
        var handler = services.GetRequiredService<IRequestHandler<TRequest>>();
        var behaviors = services.GetServices<IPipelineBehavior<TRequest, Unit>>().Reverse();

        Func<Task<Unit>> pipeline = async () =>
        {
            await handler.Handle(request, cancellationToken);
            return Unit.Value;
        };

        foreach (var behavior in behaviors)
        {
            var next = pipeline;
            var b = behavior;
            pipeline = () => b.Handle(request, next, cancellationToken);
        }

        await pipeline();
    }

    public async Task<TResponse> Send<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : IRequest<TResponse>
    {
        var handler = services.GetRequiredService<IRequestHandler<TRequest, TResponse>>();
        var behaviors = services.GetServices<IPipelineBehavior<TRequest, TResponse>>();

        Func<Task<TResponse>> pipeline = () => handler.Handle(request, cancellationToken);

        foreach (var behavior in behaviors)
        {
            var currentBehavior = behavior;
            var nextPipeline = pipeline;
            pipeline = () => currentBehavior.Handle(request, nextPipeline, cancellationToken);
        }

        return await pipeline();
    }

    public async Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        var handlers = services.GetServices<INotificationHandler<TNotification>>();
        var behaviors = services.GetServices<INotificationBehavior<TNotification>>().Reverse();

        if (!handlers.Any())
        {
            string warningMessage = $"No handlers found for notification {typeof(TNotification).Name}";
            _logger.LogWarning(warningMessage);
            return;
        }

        foreach (var handler in handlers)
        {
            Func<Task> pipeline = async () =>
            {
                try
                {
                    string messageInfo = $"Handling notification: {typeof(TNotification).Name} with handler {handler.GetType().Name}";
                    _logger.LogInformation(messageInfo);
                    await handler.Handle(notification, cancellationToken);
                    string handledMessage = $"Handled notification: {typeof(TNotification).Name} with handler {handler.GetType().Name}";
                    _logger.LogInformation(handledMessage);
                }
                catch (Exception ex)
                {
                    string errorMessage = $"Error handling notification {typeof(TNotification).Name} with handler {handler.GetType().Name}: {ex.Message}";
                    _logger.LogError(errorMessage, ex);
                }
            };

            foreach (var behavior in behaviors)
            {
                var current = behavior;
                var next = pipeline;
                pipeline = () => current.Handle(notification, next, cancellationToken);
            }

            await pipeline();
        }
    }
}

