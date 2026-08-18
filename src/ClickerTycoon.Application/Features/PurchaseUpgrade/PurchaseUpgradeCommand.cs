using System.Threading;
using ClickerTycoon.Application.Common;
using ClickerTycoon.Application.Common.Cqrs;
using ClickerTycoon.Application.Common.Dto;
using ClickerTycoon.Application.Common.Interfaces;
using ClickerTycoon.Domain.Exceptions;
using ClickerTycoon.Domain.Services;

namespace ClickerTycoon.Application.Features.PurchaseUpgrade;

public record PurchaseUpgradeCommand(Guid PlayerId, string UpgradeId) : ICommand<PurchaseUpgradeResultDto>;

public class PurchaseUpgradeCommandHandler : ICommandHandler<PurchaseUpgradeCommand, PurchaseUpgradeResultDto>
{
    private readonly IGameStateRepository _repository;
    private readonly IGameConfigProvider _configProvider;
    private readonly IClock _clock;

    public PurchaseUpgradeCommandHandler(IGameStateRepository repository, IGameConfigProvider configProvider, IClock clock)
    {
        _repository = repository;
        _configProvider = configProvider;
        _clock = clock;
    }

    public async Task<PurchaseUpgradeResultDto> HandleAsync(PurchaseUpgradeCommand command, CancellationToken ct = default)
    {
        var state = await _repository.FindAsync(command.PlayerId, ct)
                    ?? throw new PlayerNotFoundException(command.PlayerId);

        var config = _configProvider.GetConfig();
        var now = _clock.UtcNow;

        GameCalculationEngine.ApplyAutomationTick(state, config, now);
        var cost = GameCalculationEngine.PurchaseUpgrade(state, config, command.UpgradeId, now);

        await _repository.SaveAsync(state, ct);

        var dto = GameStateMapper.ToDto(state, config, now);
        int newLevel = state.GetUpgradeLevel(command.UpgradeId);

        return new PurchaseUpgradeResultDto(command.UpgradeId, cost, newLevel, dto);
    }
}
