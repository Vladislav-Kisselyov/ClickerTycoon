using System.Threading;
using ClickerTycoon.Application.Common;
using ClickerTycoon.Application.Common.Cqrs;
using ClickerTycoon.Application.Common.Dto;
using ClickerTycoon.Application.Common.Interfaces;
using ClickerTycoon.Domain.Exceptions;
using ClickerTycoon.Domain.Services;

namespace ClickerTycoon.Application.Features.ResetProgress;

public record ResetProgressCommand(Guid PlayerId) : ICommand<GameStateDto>;

public class ResetProgressCommandHandler : ICommandHandler<ResetProgressCommand, GameStateDto>
{
    private readonly IGameStateRepository _repository;
    private readonly IGameConfigProvider _configProvider;
    private readonly IClock _clock;

    public ResetProgressCommandHandler(IGameStateRepository repository, IGameConfigProvider configProvider, IClock clock)
    {
        _repository = repository;
        _configProvider = configProvider;
        _clock = clock;
    }

    public async Task<GameStateDto> HandleAsync(ResetProgressCommand command, CancellationToken ct = default)
    {
        var state = await _repository.FindAsync(command.PlayerId, ct)
                    ?? throw new PlayerNotFoundException(command.PlayerId);

        var config = _configProvider.GetConfig();
        var now = _clock.UtcNow;

        GameCalculationEngine.ResetProgress(state, now);

        await _repository.SaveAsync(state, ct);

        return GameStateMapper.ToDto(state, config, now);
    }
}
