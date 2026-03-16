namespace RMMSoft.Mediator.Abstractions;

public interface INotificationBehavior<TNotification> where TNotification : INotification
{
  Task Handle(TNotification notification, Func<Task> next, CancellationToken cancellationToken);
}
