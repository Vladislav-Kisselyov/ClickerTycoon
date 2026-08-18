using System.Threading;
using ClickerTycoon.Application.Common.Interfaces;
using ClickerTycoon.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClickerTycoon.Infrastructure.Persistence;

public class GameStateRepository : IGameStateRepository
{
    private readonly GameDbContext _context;

    public GameStateRepository(GameDbContext context)
    {
        _context = context;
    }

    public Task<PlayerGameState?> FindAsync(Guid playerId, CancellationToken ct = default)
    {
        return _context.GameSaves.FirstOrDefaultAsync(x => x.PlayerId == playerId, ct);
    }

    public async Task<PlayerGameState> CreateAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var state = new PlayerGameState
        {
            PlayerId = Guid.NewGuid(),
            Resource = 0,
            TotalEarned = 0,
            TotalClicks = 0,
            ClicksSinceLastEvent = 0,
            CurrentStage = 1,
            CreatedUtc = now,
            LastTickUtc = now,
            CrisisActionsRemaining = 0
        };

        _context.GameSaves.Add(state);
        await _context.SaveChangesAsync(ct);
        return state;
    }

    public async Task SaveAsync(PlayerGameState state, CancellationToken ct = default)
    {
        if (_context.Entry(state).State == EntityState.Detached)
        {
            _context.GameSaves.Update(state);
        }
        await _context.SaveChangesAsync(ct);
    }
}
