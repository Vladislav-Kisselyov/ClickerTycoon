using ClickerTycoon.Domain.Configuration;
using ClickerTycoon.Domain.Entities;

namespace ClickerTycoon.Tests;

internal static class TestGameConfigFactory {

    public static GameConfig CreateConfig()
    {
        return new GameConfig
        {
            BaseClickValue = 1,
            BaseCritChance = 0.1,
            CritMultiplier = 5,
            MaxOfflineSeconds = 3600,
            Upgrades = new List<UpgradeDefinition>
            {
                new()
                {
                    Id = "click-percent",
                    Name = "Test Click %",
                    EffectType = UpgradeEffectType.ClickPercent,
                    EffectValuePerLevel = 0.5,
                    BaseCost = 10,
                    CostGrowth = 2,
                    MaxLevel = 3,
                    RequiredStage = 1
                },
                new()
                {
                    Id = "auto-feeder",
                    Name = "Test Automation",
                    EffectType = UpgradeEffectType.AutomationFlat,
                    EffectValuePerLevel = 2,
                    BaseCost = 50,
                    CostGrowth = 1.5,
                    MaxLevel = 10,
                    RequiredStage = 1,
                    ChangesMechanic = true
                },
                new()
                {
                    Id = "stage2-only",
                    Name = "Test Stage 2 Upgrade",
                    EffectType = UpgradeEffectType.ClickFlat,
                    EffectValuePerLevel = 1,
                    BaseCost = 5,
                    CostGrowth = 1.1,
                    MaxLevel = 5,
                    RequiredStage = 2
                }
            },
            Stages = new List<StageDefinition>
            {
                new() { Id = 1, Name = "Stage 1", Description = "d1", ResourceRequired = 0 },
                new() { Id = 2, Name = "Stage 2", Description = "d2", ResourceRequired = 100, RequiredUpgradeId = "auto-feeder" },
                new() { Id = 3, Name = "Stage 3", Description = "d3", ResourceRequired = 1000 }
            },
            Events = new EventsConfig
            {
                ViralEventChancePerClick = 0.5,
                ViralEventDurationSeconds = 30,
                ViralEventMultiplier = 3,
                CrisisEventChancePerClick = 0,
                CatStoleEventChancePerClick = 0,
                MinClicksBetweenEvents = 1
            },
            Monetization = new MonetizationConfig()
        };
    }

    public static PlayerGameState CreateState(DateTime nowUtc)
    {
        return new PlayerGameState
        {
            PlayerId = Guid.NewGuid(),
            Resource = 0,
            TotalEarned = 0,
            TotalClicks = 0,
            CurrentStage = 1,
            CreatedUtc = nowUtc,
            LastTickUtc = nowUtc
        };
    }
}
