using ClickerTycoon.Application.Common.Dto;
using ClickerTycoon.Domain.Configuration;
using ClickerTycoon.Domain.Entities;
using ClickerTycoon.Domain.Services;

namespace ClickerTycoon.Application.Common;

public static class GameStateMapper
{
    public static GameStateDto ToDto(PlayerGameState state, GameConfig config, DateTime nowUtc)
    {
        var stageDef = config.Stages.First(s => s.Id == state.CurrentStage);
        var nextStageDef = config.Stages.FirstOrDefault(s => s.Id == state.CurrentStage + 1);

        NextStageDto? nextStageDto = null;
        if (nextStageDef is not null)
        {
            string? requiredUpgradeName = null;
            if (nextStageDef.RequiredUpgradeId is not null)
            {
                requiredUpgradeName = config.Upgrades.FirstOrDefault(u => u.Id == nextStageDef.RequiredUpgradeId)?.Name;
            }
            nextStageDto = new NextStageDto(nextStageDef.Id, nextStageDef.Name, nextStageDef.ResourceRequired, requiredUpgradeName);
        }

        var upgrades = config.Upgrades
            .OrderBy(u => u.RequiredStage)
            .ThenBy(u => u.BaseCost)
            .Select(def =>
            {
                int level = state.GetUpgradeLevel(def.Id);
                bool maxed = level >= def.MaxLevel;
                bool unlocked = state.CurrentStage >= def.RequiredStage;
                decimal nextCost = maxed ? 0 : (decimal)def.CostAtLevel(level);

                return new UpgradeDto(
                    def.Id, def.Name, def.Description, def.Icon,
                    level, def.MaxLevel, nextCost, maxed, unlocked, def.RequiredStage,
                    def.ChangesMechanic, DescribeEffect(def));
            })
            .ToList();

        var activeEffects = state.ActiveEffects
            .Select(e => new ActiveEffectDto(
                e.Type.ToString(),
                DescribeEffectType(e.Type),
                Math.Max(0, (int)Math.Ceiling((e.ExpiresUtc - nowUtc).TotalSeconds)),
                e.Multiplier))
            .Where(e => e.SecondsRemaining > 0)
            .ToList();

        var monetization = BuildMonetizationOffers(state, config, nowUtc);

        return new GameStateDto(
            state.PlayerId,
            state.Resource,
            state.TotalEarned,
            state.TotalClicks,
            state.CurrentStage,
            stageDef.Name,
            stageDef.Description,
            stageDef.Unlocks,
            nextStageDto,
            GameCalculationEngine.GetBaseClickValue(state, config),
            Math.Round(GameCalculationEngine.GetClickCritChance(state, config) * 100, 1),
            Math.Round(GameCalculationEngine.GetAutomationRatePerSecond(state, config), 2),
            state.CrisisActionsRemaining,
            upgrades,
            activeEffects,
            monetization);
    }

    private static List<MonetizationOfferDto> BuildMonetizationOffers(PlayerGameState state, GameConfig config, DateTime nowUtc)
    {
        var offers = new List<MonetizationOfferDto>();

        bool adActive = state.AdBoostExpiresUtc.HasValue && state.AdBoostExpiresUtc.Value > nowUtc;
        int? adSeconds = adActive ? (int)Math.Ceiling((state.AdBoostExpiresUtc!.Value - nowUtc).TotalSeconds) : null;
        offers.Add(new MonetizationOfferDto(
            "ad-boost",
            "Посмотреть рекламу",
            $"Реклама (симулированная) → доход x{config.Monetization.AdBoostMultiplier:0.#} на {config.Monetization.AdBoostDurationSeconds} секунд.",
            "ad",
            Purchased: false,
            Active: adActive,
            SecondsRemaining: adSeconds));

        offers.Add(new MonetizationOfferDto(
            "premium-camera",
            "Premium Camera",
            $"Разовая покупка (симулированная): постоянный бонус +{config.Monetization.PremiumCameraClickPercentBonus * 100:0}% к доходу за клик.",
            "premium",
            Purchased: state.PremiumCameraPurchased,
            Active: state.PremiumCameraPurchased,
            SecondsRemaining: null));

        var starterEffect = state.ActiveEffects.FirstOrDefault(e => e.Type == ActiveEffectType.StarterPackBoost && e.ExpiresUtc > nowUtc);
        offers.Add(new MonetizationOfferDto(
            "starter-pack",
            "Мемный стартовый набор",
            $"Разовая покупка (симулированная): мгновенно +{config.Monetization.StarterPackResourceAmount:0} ресурса и x{config.Monetization.StarterPackBoostMultiplier:0.#} доход на {config.Monetization.StarterPackBoostDurationSeconds} секунд.",
            "starter-pack",
            Purchased: state.StarterPackPurchased,
            Active: starterEffect is not null,
            SecondsRemaining: starterEffect is not null ? (int)Math.Ceiling((starterEffect.ExpiresUtc - nowUtc).TotalSeconds) : null));

        return offers;
    }

    private static string DescribeEffectType(ActiveEffectType type) => type switch
    {
        ActiveEffectType.ViralBoost => "Мем завирусился",
        ActiveEffectType.CatStoleBoost => "Бонус после кражи аккаунта",
        ActiveEffectType.AdBoost => "Рекламный буст",
        ActiveEffectType.StarterPackBoost => "Стартовый набор",
        _ => type.ToString()
    };

    private static string DescribeEffect(UpgradeDefinition def) => def.EffectType switch
    {
        UpgradeEffectType.ClickFlat => $"+{def.EffectValuePerLevel:0.#} к клику за уровень",
        UpgradeEffectType.ClickPercent => $"+{def.EffectValuePerLevel * 100:0}% к доходу за клик за уровень",
        UpgradeEffectType.AutomationFlat => $"+{def.EffectValuePerLevel:0.#} ресурса/сек автоматически за уровень",
        UpgradeEffectType.AutomationPercent => $"+{def.EffectValuePerLevel * 100:0}% к авто-доходу за уровень",
        UpgradeEffectType.CritChance => $"+{def.EffectValuePerLevel * 100:0}% к шансу крита за уровень",
        UpgradeEffectType.ViralChanceBonus => $"+{def.EffectValuePerLevel * 100:0.#}% к шансу вирусного события за уровень",
        _ => string.Empty
    };
}
