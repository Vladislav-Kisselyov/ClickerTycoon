using System.Threading;
using ClickerTycoon.Application.Common;
using ClickerTycoon.Application.Common.Cqrs;
using ClickerTycoon.Application.Common.Dto;
using ClickerTycoon.Application.Common.Interfaces;
using ClickerTycoon.Domain.Exceptions;
using ClickerTycoon.Domain.Services;

namespace ClickerTycoon.Application.Features.PerformClick;

public record PerformClickCommand(Guid PlayerId) : ICommand<ClickResultDto>;

public class PerformClickCommandHandler : ICommandHandler<PerformClickCommand, ClickResultDto>
{
    private readonly IGameStateRepository _repository;
    private readonly IGameConfigProvider _configProvider;
    private readonly IClock _clock;
    private readonly IRandomProvider _random;

    public PerformClickCommandHandler(
        IGameStateRepository repository,
        IGameConfigProvider configProvider,
        IClock clock,
        IRandomProvider random)
    {
        _repository = repository;
        _configProvider = configProvider;
        _clock = clock;
        _random = random;
    }

    public async Task<ClickResultDto> HandleAsync(PerformClickCommand command, CancellationToken ct = default)
    {
        var state = await _repository.FindAsync(command.PlayerId, ct)
                    ?? throw new PlayerNotFoundException(command.PlayerId);

        var config = _configProvider.GetConfig();
        var now = _clock.UtcNow;

        GameCalculationEngine.ApplyAutomationTick(state, config, now);
        var outcome = GameCalculationEngine.PerformClick(state, config, now, _random.NextDouble);

        await _repository.SaveAsync(state, ct);

        var dto = GameStateMapper.ToDto(state, config, now);

        return new ClickResultDto(
            outcome.AmountGained,
            outcome.IsCritical,
            state.Resource,
            outcome.TriggeredEventType,
            outcome.TriggeredEventMessage,
            outcome.StageAdvanced,
            dto);
    }
}
