# rmm-soft.Mediator

Lightweight mediator implementation for Clean Architecture in .NET. Supports CQRS, pipeline behaviors, and notifications — with no external dependencies beyond `Microsoft.Extensions`.

## Features

- ✅ **CQRS** — Commands and Queries with `IRequest<TResponse>`
- ✅ **Pipeline Behaviors** — cross-cutting concerns (logging, validation, exception handling)
- ✅ **Notifications** — publish/subscribe pattern with `INotification`
- ✅ **Notification Behaviors** — pipeline for notifications
- ✅ **Zero magic** — explicit, readable, no reflection tricks
- ✅ **DI friendly** — works with `Microsoft.Extensions.DependencyInjection`

## Installation

```bash
dotnet add package rmm-soft.Mediator
```

## Quick Start

### 1. Register in DI

```csharp
// Program.cs
builder.Services.AddRmmSoftMediator(Assembly.GetExecutingAssembly());

// Register your pipeline behaviors (in order)
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
```

### 2. Define a Command

```csharp
public record CreateProductCommand : IRequest<int>
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}
```

### 3. Implement the Handler

```csharp
public record CreateProductCommandHandler(
    IProductRepository repository) : IRequestHandler<CreateProductCommand, int>
{
    public async Task<int> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var product = new Product { Name = request.Name, Price = request.Price };
        await repository.AddAsync(product);
        return product.Id;
    }
}
```

### 4. Send the Command

```csharp
public class ProductController(IAppMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductCommand command)
    {
        var id = await mediator.Send<CreateProductCommand, int>(command);
        return CreatedAtAction(nameof(GetById), new { id }, null);
    }
}
```

## Pipeline Behaviors

Pipeline behaviors allow you to add cross-cutting concerns to all commands and queries.

```csharp
public class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        Func<Task<TResponse>> next,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Handling {RequestType}", typeof(TRequest).Name);
        var response = await next();
        logger.LogInformation("Handled {RequestType}", typeof(TRequest).Name);
        return response;
    }
}
```

## Notifications

Use notifications for domain events and pub/sub patterns.

```csharp
// Define notification
public record OrderCreatedNotification(int OrderId, string UserId) : INotification;

// Implement handler
public class SendEmailOnOrderCreated : INotificationHandler<OrderCreatedNotification>
{
    public async Task Handle(OrderCreatedNotification notification, CancellationToken cancellationToken)
    {
        // send email...
    }
}

// Publish
await mediator.Publish(new OrderCreatedNotification(order.Id, userId));
```

## Void Commands (no return value)

```csharp
// Command with no response
public record DeleteProductCommand(int Id) : IRequest;

// Handler
public record DeleteProductCommandHandler(IProductRepository repository)
    : IRequestHandler<DeleteProductCommand>
{
    public async Task Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        await repository.DeleteAsync(request.Id);
    }
}

// Send
await mediator.Send(new DeleteProductCommand(id));
```

## DI Registration Options

```csharp
// Register handlers from multiple assemblies
services.AddRmmSoftMediator(
    Assembly.GetExecutingAssembly(),
    typeof(SomeOtherHandler).Assembly
);
```

## License
MIT — see [LICENSE](LICENSE) for details.

