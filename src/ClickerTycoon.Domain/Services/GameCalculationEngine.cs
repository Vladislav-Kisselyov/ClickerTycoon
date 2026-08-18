using ClickerTycoon.Domain.Configuration;
using ClickerTycoon.Domain.Entities;
using ClickerTycoon.Domain.Exceptions;

namespace ClickerTycoon.Domain.Services;

public record ClickOutcome(
    decimal AmountGained,
    bool IsCritical,
    bool CrisisActive,
    string? TriggeredEventType,
    string? TriggeredEventMessage,
    bool StageAdvanced);

/// <summary>
/// Pure, stateless domain service. All randomness and "current time" are passed
/// in explicitly so the engine is trivially unit-testable and has no hidden
/// dependencies on the clock or on System.Random.
/// </summary>
public static class GameCalculationEngine
{
    // ---------- Automation / idle income ----------

    public static double GetAutomationRatePerSecond(PlayerGameState state, GameConfig config)
    {
        double flat = 0;
        double percentBonus = 0;

        foreach (var owned in state.Upgrades)
        {
            var def = config.Upgrades.FirstOrDefault(u => u.Id == owned.UpgradeId);
            if (def is null) continue;

            switch (def.EffectType)
            {
                case UpgradeEffectType.AutomationFlat:
                    flat += def.EffectValuePerLevel * owned.Level;
                    break;
                case UpgradeEffectType.AutomationPercent:
                    percentBonus += def.EffectValuePerLevel * owned.Level;
                    break;
            }
        }

        if (flat <= 0) return 0;

        double effectMultiplier = GetActiveEffectMultiplier(state);
        return flat * (1 + percentBonus) * effectMultiplier;
    }

    /// <summary>
    /// Applies automatically-generated resource for the time elapsed since the last tick.
    /// Must be called before any click/purchase logic so the player never "loses" idle income.
    /// </summary>
    public static void ApplyAutomationTick(PlayerGameState state, GameConfig config, DateTime nowUtc)
    {
        ExpireEffects(state, nowUtc);

        var elapsed = nowUtc - state.LastTickUtc;
        if (elapsed <= TimeSpan.Zero)
        {
            state.LastTickUtc = nowUtc;
            return;
        }

        var cappedSeconds = Math.Min(elapsed.TotalSeconds, config.MaxOfflineSeconds);
        var rate = GetAutomationRatePerSecond(state, config);

        if (rate > 0 && cappedSeconds > 0)
        {
            var gained = (decimal)(rate * cappedSeconds);
            state.Resource += gained;
            state.TotalEarned += gained;
        }

        state.LastTickUtc = nowUtc;
    }

    private static void ExpireEffects(PlayerGameState state, DateTime nowUtc)
    {
        state.ActiveEffects.RemoveAll(e => e.ExpiresUtc <= nowUtc);
        if (state.AdBoostExpiresUtc.HasValue && state.AdBoostExpiresUtc.Value <= nowUtc)
        {
            state.AdBoostExpiresUtc = null;
        }
    }

    private static double GetActiveEffectMultiplier(PlayerGameState state)
    {
        double multiplier = 1.0;
        foreach (var effect in state.ActiveEffects)
        {
            multiplier *= effect.Multiplier;
        }
        return multiplier;
    }

    // ---------- Clicking ----------

    public static double GetClickCritChance(PlayerGameState state, GameConfig config)
    {
        double bonus = 0;
        foreach (var owned in state.Upgrades)
        {
            var def = config.Upgrades.FirstOrDefault(u => u.Id == owned.UpgradeId);
            if (def?.EffectType == UpgradeEffectType.CritChance)
            {
                bonus += def.EffectValuePerLevel * owned.Level;
            }
        }
        return Math.Clamp(config.BaseCritChance + bonus, 0, 0.95);
    }

    public static decimal GetBaseClickValue(PlayerGameState state, GameConfig config)
    {
        double flat = config.BaseClickValue;
        double percent = 0;

        foreach (var owned in state.Upgrades)
        {
            var def = config.Upgrades.FirstOrDefault(u => u.Id == owned.UpgradeId);
            if (def is null) continue;

            if (def.EffectType == UpgradeEffectType.ClickFlat)
                flat += def.EffectValuePerLevel * owned.Level;
            else if (def.EffectType == UpgradeEffectType.ClickPercent)
                percent += def.EffectValuePerLevel * owned.Level;
        }

        if (state.PremiumCameraPurchased)
            percent += config.Monetization.PremiumCameraClickPercentBonus;

        double effectMultiplier = GetActiveEffectMultiplier(state);
        double crisisMultiplier = state.CrisisActionsRemaining > 0 ? config.Events.CrisisIncomeMultiplier : 1.0;

        return (decimal)(flat * (1 + percent) * effectMultiplier * crisisMultiplier);
    }

    /// <summary>
    /// Performs one manual click: resolves crit, applies the gain, resolves an in-progress
    /// crisis by one step, rolls for a new random event, and checks for stage advancement.
    /// Caller must have already invoked <see cref="ApplyAutomationTick"/> for this request.
    /// </summary>
    public static ClickOutcome PerformClick(
        PlayerGameState state,
        GameConfig config,
        DateTime nowUtc,
        Func<double> nextRandom)
    {
        double critChance = GetClickCritChance(state, config);
        bool isCrit = nextRandom() < critChance;

        decimal amount = GetBaseClickValue(state, config);
        if (isCrit) amount *= (decimal)config.CritMultiplier;
        amount = Math.Round(amount, 0, MidpointRounding.AwayFromZero);
        if (amount < 1) amount = 1;

        state.Resource += amount;
        state.TotalEarned += amount;
        state.TotalClicks += 1;
        state.ClicksSinceLastEvent += 1;

        bool crisisWasActive = state.CrisisActionsRemaining > 0;
        if (crisisWasActive)
        {
            state.CrisisActionsRemaining = Math.Max(0, state.CrisisActionsRemaining - 1);
        }

        string? eventType = null;
        string? eventMessage = null;

        if (state.ClicksSinceLastEvent >= config.Events.MinClicksBetweenEvents && state.CrisisActionsRemaining == 0)
        {
            (eventType, eventMessage) = TryTriggerEvent(state, config, nowUtc, nextRandom);
            if (eventType is not null)
            {
                state.ClicksSinceLastEvent = 0;
            }
        }

        bool stageAdvanced = TryAdvanceStage(state, config);

        return new ClickOutcome(amount, isCrit, state.CrisisActionsRemaining > 0, eventType, eventMessage, stageAdvanced);
    }

    private static double GetViralChanceBonus(PlayerGameState state, GameConfig config)
    {
        double bonus = 0;
        foreach (var owned in state.Upgrades)
        {
            var def = config.Upgrades.FirstOrDefault(u => u.Id == owned.UpgradeId);
            if (def?.EffectType == UpgradeEffectType.ViralChanceBonus)
                bonus += def.EffectValuePerLevel * owned.Level;
        }
        return bonus;
    }

    private static (string? type, string? message) TryTriggerEvent(
        PlayerGameState state, GameConfig config, DateTime nowUtc, Func<double> nextRandom)
    {
        var ev = config.Events;

        double viralChance = ev.ViralEventChancePerClick + GetViralChanceBonus(state, config);
        if (nextRandom() < viralChance)
        {
            state.ActiveEffects.Add(new ActiveEffect
            {
                Type = ActiveEffectType.ViralBoost,
                ExpiresUtc = nowUtc.AddSeconds(ev.ViralEventDurationSeconds),
                Multiplier = ev.ViralEventMultiplier
            });
            return ("viral", "Мем завирусился! Доход x3 на 30 секунд.");
        }

        if (nextRandom() < ev.CatStoleEventChancePerClick)
        {
            var loss = state.Resource * (decimal)ev.CatStoleLossPercent;
            state.Resource = Math.Max(0, state.Resource - loss);
            state.ActiveEffects.Add(new ActiveEffect
            {
                Type = ActiveEffectType.CatStoleBoost,
                ExpiresUtc = nowUtc.AddSeconds(ev.CatStoleBoostDurationSeconds),
                Multiplier = ev.CatStoleBoostMultiplier
            });
            return ("cat-stole", $"Кот украл аккаунт! Потеряно {loss:0} ресурса, но временный бонус x{ev.CatStoleBoostMultiplier:0.#} активирован.");
        }

        if (nextRandom() < ev.CrisisEventChancePerClick)
        {
            state.CrisisActionsRemaining = ev.CrisisClicksToResolve;
            return ("crisis", $"Алгоритмы решили, что ты больше не в тренде. Доход снижен на {ev.CrisisClicksToResolve} действий.");
        }

        return (null, null);
    }

    // ---------- Upgrades ----------

    public static decimal PurchaseUpgrade(PlayerGameState state, GameConfig config, string upgradeId, DateTime nowUtc)
    {
        var def = config.Upgrades.FirstOrDefault(u => u.Id == upgradeId)
                  ?? throw new UpgradeNotFoundException(upgradeId);

        if (state.CurrentStage < def.RequiredStage)
            throw new UpgradeNotAvailableException(upgradeId, def.RequiredStage);

        var owned = state.Upgrades.FirstOrDefault(u => u.UpgradeId == upgradeId);
        int currentLevel = owned?.Level ?? 0;

        if (currentLevel >= def.MaxLevel)
            throw new UpgradeMaxLevelReachedException(upgradeId);

        decimal cost = (decimal)def.CostAtLevel(currentLevel);
        if (state.Resource < cost)
            throw new InsufficientResourceException(cost, state.Resource);

        state.Resource -= cost;

        if (owned is null)
        {
            state.Upgrades.Add(new OwnedUpgrade { UpgradeId = upgradeId, Level = 1 });
        }
        else
        {
            owned.Level += 1;
        }

        TryAdvanceStage(state, config);

        return cost;
    }

    // ---------- Stage progression ----------

    public static bool TryAdvanceStage(PlayerGameState state, GameConfig config)
    {
        bool advancedAny = false;

        while (true)
        {
            var next = config.Stages.FirstOrDefault(s => s.Id == state.CurrentStage + 1);
            if (next is null) break;

            bool resourceOk = state.Resource >= next.ResourceRequired;
            bool upgradeOk = next.RequiredUpgradeId is null || state.GetUpgradeLevel(next.RequiredUpgradeId) > 0;

            if (resourceOk && upgradeOk)
            {
                state.CurrentStage = next.Id;
                advancedAny = true;
            }
            else
            {
                break;
            }
        }

        return advancedAny;
    }

    // ---------- Monetization ----------

    public static void ActivateAdBoost(PlayerGameState state, GameConfig config, DateTime nowUtc)
    {
        state.ActiveEffects.RemoveAll(e => e.Type == ActiveEffectType.AdBoost);
        state.ActiveEffects.Add(new ActiveEffect
        {
            Type = ActiveEffectType.AdBoost,
            ExpiresUtc = nowUtc.AddSeconds(config.Monetization.AdBoostDurationSeconds),
            Multiplier = config.Monetization.AdBoostMultiplier
        });
        state.AdBoostExpiresUtc = nowUtc.AddSeconds(config.Monetization.AdBoostDurationSeconds);
    }

    public static void PurchasePremiumCamera(PlayerGameState state)
    {
        state.PremiumCameraPurchased = true;
    }

    public static void PurchaseStarterPack(PlayerGameState state, GameConfig config, DateTime nowUtc)
    {
        state.Resource += config.Monetization.StarterPackResourceAmount;
        state.TotalEarned += config.Monetization.StarterPackResourceAmount;
        state.StarterPackPurchased = true;

        state.ActiveEffects.RemoveAll(e => e.Type == ActiveEffectType.StarterPackBoost);
        state.ActiveEffects.Add(new ActiveEffect
        {
            Type = ActiveEffectType.StarterPackBoost,
            ExpiresUtc = nowUtc.AddSeconds(config.Monetization.StarterPackBoostDurationSeconds),
            Multiplier = config.Monetization.StarterPackBoostMultiplier
        });
    }

    // ---------- Reset ----------

    /// <summary>Wipes a save back to a brand-new game, keeping the same PlayerId.</summary>
    public static void ResetProgress(PlayerGameState state, DateTime nowUtc)
    {
        state.Resource = 0;
        state.TotalEarned = 0;
        state.TotalClicks = 0;
        state.ClicksSinceLastEvent = 0;
        state.CurrentStage = 1;
        state.LastTickUtc = nowUtc;
        state.CrisisActionsRemaining = 0;

        state.Upgrades.Clear();
        state.ActiveEffects.Clear();

        state.PremiumCameraPurchased = false;
        state.StarterPackPurchased = false;
        state.AdBoostExpiresUtc = null;
    }
}
