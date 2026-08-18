using System.Threading;
using ClickerTycoon.Application.Common;
using ClickerTycoon.Application.Common.Cqrs;
using ClickerTycoon.Application.Common.Dto;
using ClickerTycoon.Application.Common.Interfaces;
using ClickerTycoon.Domain.Exceptions;
using ClickerTycoon.Domain.Services;

namespace ClickerTycoon.Application.Features.GameStateQuery;

public record GetGameStateQuery(Guid PlayerId) : IQuery<GameStateDto>;

public class GetGameStateQueryHandler : IQueryHandler<GetGameStateQuery, GameStateDto>
{
    private readonly IGameStateRepository _repository;
    private readonly IGameConfigProvider _configProvider;
    private readonly IClock _clock;

    public GetGameStateQueryHandler(IGameStateRepository repository, IGameConfigProvider configProvider, IClock clock)
    {
        _repository = repository;
        _configProvider = configProvider;
        _clock = clock;
    }

    public async Task<GameStateDto> HandleAsync(GetGameStateQuery query, CancellationToken ct = default)
    {
        var state = await _repository.FindAsync(query.PlayerId, ct)
                    ?? throw new PlayerNotFoundException(query.PlayerId);

        var config = _configProvider.GetConfig();
        var now = _clock.UtcNow;

        GameCalculationEngine.ApplyAutomationTick(state, config, now);
        GameCalculationEngine.TryAdvanceStage(state, config);

        await _repository.SaveAsync(state, ct);

        return GameStateMapper.ToDto(state, config, now);
    }
}
