namespace RMMSoft.Mediator.Abstractions;

public interface IAppMediator
{
  Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
      where TRequest : IRequest;

  Task<TResponse> Send<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default)
      where TRequest : IRequest<TResponse>;

  Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
  where TNotification : INotification;
}
