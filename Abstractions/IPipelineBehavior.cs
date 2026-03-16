namespace RMMSoft.Mediator.Abstractions;

public interface IPipelineBehavior<TRequest, TResponse>
{
  Task<TResponse> Handle(TRequest request, Func<Task<TResponse>> next, CancellationToken cancellationToken);
}
