using System.Threading;
using ClickerTycoon.Application.Common.Cqrs;
using ClickerTycoon.Application.Common.Interfaces;

namespace ClickerTycoon.Application.Features.Players;

public record CreatePlayerCommand : ICommand<Guid>;

public class CreatePlayerCommandHandler : ICommandHandler<CreatePlayerCommand, Guid>
{
    private readonly IGameStateRepository _repository;

    public CreatePlayerCommandHandler(IGameStateRepository repository)
    {
        _repository = repository;
    }

    public async Task<Guid> HandleAsync(CreatePlayerCommand command, CancellationToken ct = default)
    {
        var state = await _repository.CreateAsync(ct);
        return state.PlayerId;
    }
}
