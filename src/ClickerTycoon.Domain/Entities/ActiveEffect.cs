namespace ClickerTycoon.Domain.Entities;

public enum ActiveEffectType
{
    ViralBoost,
    CatStoleBoost,
    AdBoost,
    StarterPackBoost
}

/// <summary>
/// A temporary multiplier affecting click and/or automation income.
/// Crisis is intentionally NOT modeled here because it is resolved by
/// player action (a number of clicks) rather than by elapsed time -
/// see <see cref="PlayerGameState.CrisisActionsRemaining"/>.
/// </summary>
public class ActiveEffect
{
    public ActiveEffectType Type { get; set; }
    public DateTime ExpiresUtc { get; set; }
    public double Multiplier { get; set; } = 1.0;
}
