namespace ClickerTycoon.Domain.Entities;

/// <summary>
/// Aggregate root holding the full persisted state of a single player's game.
/// Every mutation to this object must go through the Application layer command
/// handlers, which route through <see cref="Services.GameCalculationEngine"/>.
/// </summary>
public class PlayerGameState
{
    public Guid PlayerId { get; set; }

    public decimal Resource { get; set; }
    public decimal TotalEarned { get; set; }
    public long TotalClicks { get; set; }
    public int ClicksSinceLastEvent { get; set; }

    public int CurrentStage { get; set; } = 1;

    public DateTime CreatedUtc { get; set; }
    public DateTime LastTickUtc { get; set; }

    public List<OwnedUpgrade> Upgrades { get; set; } = new();
    public List<ActiveEffect> ActiveEffects { get; set; } = new();

    public int CrisisActionsRemaining { get; set; }

    // Monetization (simulated purchases / boosts)
    public bool PremiumCameraPurchased { get; set; }
    public bool StarterPackPurchased { get; set; }
    public DateTime? AdBoostExpiresUtc { get; set; }

    public int GetUpgradeLevel(string upgradeId)
    {
        return Upgrades.FirstOrDefault(u => u.UpgradeId == upgradeId)?.Level ?? 0;
    }
}
