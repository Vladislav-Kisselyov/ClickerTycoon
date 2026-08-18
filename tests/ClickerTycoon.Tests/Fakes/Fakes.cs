using System.Threading;
using ClickerTycoon.Application.Common.Interfaces;
using ClickerTycoon.Domain.Configuration;
using ClickerTycoon.Domain.Entities;

namespace ClickerTycoon.Tests.Fakes;

internal class InMemoryGameStateRepository : IGameStateRepository
{
    private readonly Dictionary<Guid, PlayerGameState> _store = new();

    public Task<PlayerGameState?> FindAsync(Guid playerId, CancellationToken ct = default)
    {
        _store.TryGetValue(playerId, out var state);
        return Task.FromResult(state);
    }

    public Task<PlayerGameState> CreateAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var state = new PlayerGameState
        {
            PlayerId = Guid.NewGuid(),
            CreatedUtc = now,
            LastTickUtc = now,
            CurrentStage = 1
        };
        _store[state.PlayerId] = state;
        return Task.FromResult(state);
    }

    public Task SaveAsync(PlayerGameState state, CancellationToken ct = default)
    {
        _store[state.PlayerId] = state;
        return Task.CompletedTask;
    }
}

internal class FixedClock : IClock
{
    public FixedClock(DateTime now) => UtcNow = now;
    public DateTime UtcNow { get; set; }
}

internal class FixedRandomProvider : IRandomProvider
{
    private readonly double _value;
    public FixedRandomProvider(double value) => _value = value;
    public double NextDouble() => _value;
}

internal class StaticGameConfigProvider : IGameConfigProvider
{
    private readonly GameConfig _config;
    public StaticGameConfigProvider(GameConfig config) => _config = config;
    public GameConfig GetConfig() => _config;
}
