using System.Threading;
using ClickerTycoon.Domain.Entities;

namespace ClickerTycoon.Application.Common.Interfaces;

public interface IGameStateRepository
{
    Task<PlayerGameState?> FindAsync(Guid playerId, CancellationToken ct = default);
    Task<PlayerGameState> CreateAsync(CancellationToken ct = default);
    Task SaveAsync(PlayerGameState state, CancellationToken ct = default);
}
