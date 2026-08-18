using System.Threading;
namespace ClickerTycoon.Application.Common.Cqrs;

/// <summary>Marker for an operation that mutates state.</summary>
public interface ICommand<TResult> { }

/// <summary>Marker for an operation that only reads state.</summary>
public interface IQuery<TResult> { }

public interface ICommandHandler<in TCommand, TResult> where TCommand : ICommand<TResult>
{
    Task<TResult> HandleAsync(TCommand command, CancellationToken ct = default);
}

public interface IQueryHandler<in TQuery, TResult> where TQuery : IQuery<TResult>
{
    Task<TResult> HandleAsync(TQuery query, CancellationToken ct = default);
}

/// <summary>
/// Thin dispatcher that resolves the matching handler from DI. This gives us the
/// same "single entry point" ergonomics as MediatR, split cleanly into commands
/// (writes) and queries (reads), without pulling in a third-party dependency.
/// </summary>
public interface IDispatcher
{
    Task<TResult> SendAsync<TResult>(ICommand<TResult> command, CancellationToken ct = default);
    Task<TResult> SendAsync<TResult>(IQuery<TResult> query, CancellationToken ct = default);
}

public class Dispatcher : IDispatcher
{
    private readonly IServiceProvider _serviceProvider;

    public Dispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Task<TResult> SendAsync<TResult>(ICommand<TResult> command, CancellationToken ct = default)
    {
        var handlerType = typeof(ICommandHandler<,>).MakeGenericType(command.GetType(), typeof(TResult));
        var handler = _serviceProvider.GetService(handlerType)
            ?? throw new InvalidOperationException($"Не найден обработчик команды {command.GetType().Name}.");
        var method = handlerType.GetMethod("HandleAsync")
            ?? throw new InvalidOperationException("Метод HandleAsync не найден.");
        return (Task<TResult>)method.Invoke(handler, new object[] { command, ct })!;
    }

    public Task<TResult> SendAsync<TResult>(IQuery<TResult> query, CancellationToken ct = default)
    {
        var handlerType = typeof(IQueryHandler<,>).MakeGenericType(query.GetType(), typeof(TResult));
        var handler = _serviceProvider.GetService(handlerType)
            ?? throw new InvalidOperationException($"Не найден обработчик запроса {query.GetType().Name}.");
        var method = handlerType.GetMethod("HandleAsync")
            ?? throw new InvalidOperationException("Метод HandleAsync не найден.");
        return (Task<TResult>)method.Invoke(handler, new object[] { query, ct })!;
    }
}
