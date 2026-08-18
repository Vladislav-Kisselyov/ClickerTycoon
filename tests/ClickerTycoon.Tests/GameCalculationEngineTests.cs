using ClickerTycoon.Domain.Entities;
using ClickerTycoon.Domain.Exceptions;
using ClickerTycoon.Domain.Services;
using Xunit;

namespace ClickerTycoon.Tests;

public class GameCalculationEngineTests
{
    private static readonly DateTime Now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    private static Func<double> AlwaysReturn(double value) => () => value;

    [Fact]
    public void GetBaseClickValue_WithNoUpgrades_ReturnsConfigBaseValue()
    {
        var config = TestGameConfigFactory.CreateConfig();
        var state = TestGameConfigFactory.CreateState(Now);

        var value = GameCalculationEngine.GetBaseClickValue(state, config);

        Assert.Equal(1m, value);
    }

    [Fact]
    public void GetBaseClickValue_WithPercentUpgrade_AppliesBonus()
    {
        var config = TestGameConfigFactory.CreateConfig();
        var state = TestGameConfigFactory.CreateState(Now);
        state.Resource = 1000;

        GameCalculationEngine.PurchaseUpgrade(state, config, "click-percent", Now);

        // base 1 * (1 + 0.5) = 1.5
        var value = GameCalculationEngine.GetBaseClickValue(state, config);
        Assert.Equal(1.5m, value);
    }

    [Fact]
    public void PerformClick_WhenRandomBelowCritChance_IsCritical()
    {
        var config = TestGameConfigFactory.CreateConfig(); // BaseCritChance = 0.1
        var state = TestGameConfigFactory.CreateState(Now);

        var outcome = GameCalculationEngine.PerformClick(state, config, Now, AlwaysReturn(0.0));

        Assert.True(outcome.IsCritical);
        // 1 (base) * 5 (crit multiplier) = 5
        Assert.Equal(5m, outcome.AmountGained);
    }

    [Fact]
    public void PerformClick_WhenRandomAboveCritChance_IsNotCritical()
    {
        var config = TestGameConfigFactory.CreateConfig();
        var state = TestGameConfigFactory.CreateState(Now);

        var outcome = GameCalculationEngine.PerformClick(state, config, Now, AlwaysReturn(0.99));

        Assert.False(outcome.IsCritical);
        Assert.Equal(1m, outcome.AmountGained);
    }

    [Fact]
    public void PerformClick_IncreasesResourceAndTotals()
    {
        var config = TestGameConfigFactory.CreateConfig();
        var state = TestGameConfigFactory.CreateState(Now);

        GameCalculationEngine.PerformClick(state, config, Now, AlwaysReturn(0.99));
        GameCalculationEngine.PerformClick(state, config, Now, AlwaysReturn(0.99));

        Assert.Equal(2m, state.Resource);
        Assert.Equal(2m, state.TotalEarned);
        Assert.Equal(2, state.TotalClicks);
    }

    [Fact]
    public void PurchaseUpgrade_DeductsCostAndIncrementsLevel()
    {
        var config = TestGameConfigFactory.CreateConfig();
        var state = TestGameConfigFactory.CreateState(Now);
        state.Resource = 10;

        var cost = GameCalculationEngine.PurchaseUpgrade(state, config, "click-percent", Now);

        Assert.Equal(10m, cost);
        Assert.Equal(0m, state.Resource);
        Assert.Equal(1, state.GetUpgradeLevel("click-percent"));
    }

    [Fact]
    public void PurchaseUpgrade_SecondLevel_UsesGrowthCurve()
    {
        var config = TestGameConfigFactory.CreateConfig(); // baseCost 10, growth 2
        var state = TestGameConfigFactory.CreateState(Now);
        state.Resource = 100;

        GameCalculationEngine.PurchaseUpgrade(state, config, "click-percent", Now); // cost 10
        var secondCost = GameCalculationEngine.PurchaseUpgrade(state, config, "click-percent", Now); // cost 10*2=20

        Assert.Equal(20m, secondCost);
        Assert.Equal(2, state.GetUpgradeLevel("click-percent"));
    }

    [Fact]
    public void PurchaseUpgrade_WithInsufficientResource_Throws()
    {
        var config = TestGameConfigFactory.CreateConfig();
        var state = TestGameConfigFactory.CreateState(Now);
        state.Resource = 5;

        Assert.Throws<InsufficientResourceException>(() =>
            GameCalculationEngine.PurchaseUpgrade(state, config, "click-percent", Now));
    }

    [Fact]
    public void PurchaseUpgrade_LockedByStage_Throws()
    {
        var config = TestGameConfigFactory.CreateConfig();
        var state = TestGameConfigFactory.CreateState(Now);
        state.Resource = 1000;

        Assert.Throws<UpgradeNotAvailableException>(() =>
            GameCalculationEngine.PurchaseUpgrade(state, config, "stage2-only", Now));
    }

    [Fact]
    public void PurchaseUpgrade_UnknownId_Throws()
    {
        var config = TestGameConfigFactory.CreateConfig();
        var state = TestGameConfigFactory.CreateState(Now);
        state.Resource = 1000;

        Assert.Throws<UpgradeNotFoundException>(() =>
            GameCalculationEngine.PurchaseUpgrade(state, config, "does-not-exist", Now));
    }

    [Fact]
    public void PurchaseUpgrade_BeyondMaxLevel_Throws()
    {
        var config = TestGameConfigFactory.CreateConfig(); // max level 3
        var state = TestGameConfigFactory.CreateState(Now);
        state.Resource = 100_000;

        GameCalculationEngine.PurchaseUpgrade(state, config, "click-percent", Now);
        GameCalculationEngine.PurchaseUpgrade(state, config, "click-percent", Now);
        GameCalculationEngine.PurchaseUpgrade(state, config, "click-percent", Now);

        Assert.Throws<UpgradeMaxLevelReachedException>(() =>
            GameCalculationEngine.PurchaseUpgrade(state, config, "click-percent", Now));
    }

    [Fact]
    public void TryAdvanceStage_AdvancesWhenResourceAndUpgradeRequirementsMet()
    {
        var config = TestGameConfigFactory.CreateConfig();
        var state = TestGameConfigFactory.CreateState(Now);
        state.Resource = 500;
        state.Upgrades.Add(new() { UpgradeId = "auto-feeder", Level = 1 });

        var advanced = GameCalculationEngine.TryAdvanceStage(state, config);

        Assert.True(advanced);
        Assert.Equal(2, state.CurrentStage);
    }

    [Fact]
    public void TryAdvanceStage_DoesNotAdvanceWithoutRequiredUpgrade()
    {
        var config = TestGameConfigFactory.CreateConfig();
        var state = TestGameConfigFactory.CreateState(Now);
        state.Resource = 500; // enough resource, but "auto-feeder" not owned

        var advanced = GameCalculationEngine.TryAdvanceStage(state, config);

        Assert.False(advanced);
        Assert.Equal(1, state.CurrentStage);
    }

    [Fact]
    public void ApplyAutomationTick_WithAutomationUpgrade_AddsResourceForElapsedTime()
    {
        var config = TestGameConfigFactory.CreateConfig();
        var state = TestGameConfigFactory.CreateState(Now);
        state.Resource = 1000;
        GameCalculationEngine.PurchaseUpgrade(state, config, "auto-feeder", Now); // 2 resource/sec, costs 50
        state.LastTickUtc = Now;

        var later = Now.AddSeconds(10);
        GameCalculationEngine.ApplyAutomationTick(state, config, later);

        // 950 (after purchase) + 2/sec * 10s = 970
        Assert.Equal(970m, state.Resource);
        Assert.Equal(later, state.LastTickUtc);
    }

    [Fact]
    public void ApplyAutomationTick_CapsElapsedTimeAtMaxOfflineSeconds()
    {
        var config = TestGameConfigFactory.CreateConfig();
        config.MaxOfflineSeconds = 10;
        var state = TestGameConfigFactory.CreateState(Now);
        state.Resource = 1000;
        GameCalculationEngine.PurchaseUpgrade(state, config, "auto-feeder", Now); // -> 950 resource, 2/sec

        var muchLater = Now.AddHours(5);
        GameCalculationEngine.ApplyAutomationTick(state, config, muchLater);

        // capped at 10s of income: 950 + 2*10 = 970
        Assert.Equal(970m, state.Resource);
    }

    [Fact]
    public void PerformClick_CanTriggerViralEvent_WhenChanceIsHigh()
    {
        var config = TestGameConfigFactory.CreateConfig(); // ViralEventChancePerClick = 0.5, MinClicksBetweenEvents = 1
        var state = TestGameConfigFactory.CreateState(Now);
        state.ClicksSinceLastEvent = 5;

        var outcome = GameCalculationEngine.PerformClick(state, config, Now, AlwaysReturn(0.0));

        Assert.Equal("viral", outcome.TriggeredEventType);
        Assert.Single(state.ActiveEffects);
        Assert.Equal(3.0, state.ActiveEffects[0].Multiplier);
    }

    [Fact]
    public void PurchaseUpgrade_TriggersStageAdvance_WhenAffordingCrossesThreshold()
    {
        var config = TestGameConfigFactory.CreateConfig();
        var state = TestGameConfigFactory.CreateState(Now);
        state.Resource = 150; // enough to buy auto-feeder (50) and remain above stage-2 threshold (100)

        GameCalculationEngine.PurchaseUpgrade(state, config, "auto-feeder", Now);

        Assert.Equal(2, state.CurrentStage);
    }

    [Fact]
    public void ResetProgress_ClearsResourceUpgradesEffectsAndStage()
    {
        var config = TestGameConfigFactory.CreateConfig();
        var state = TestGameConfigFactory.CreateState(Now);
        state.Resource = 5000;
        state.TotalEarned = 5000;
        state.TotalClicks = 42;
        state.CurrentStage = 2;
        state.Upgrades.Add(new() { UpgradeId = "click-percent", Level = 3 });
        state.ActiveEffects.Add(new() { Type = ActiveEffectType.ViralBoost, ExpiresUtc = Now.AddSeconds(30), Multiplier = 3 });
        state.CrisisActionsRemaining = 4;
        state.PremiumCameraPurchased = true;
        state.StarterPackPurchased = true;
        state.AdBoostExpiresUtc = Now.AddSeconds(60);

        GameCalculationEngine.ResetProgress(state, Now);

        Assert.Equal(0m, state.Resource);
        Assert.Equal(0m, state.TotalEarned);
        Assert.Equal(0, state.TotalClicks);
        Assert.Equal(1, state.CurrentStage);
        Assert.Empty(state.Upgrades);
        Assert.Empty(state.ActiveEffects);
        Assert.Equal(0, state.CrisisActionsRemaining);
        Assert.False(state.PremiumCameraPurchased);
        Assert.False(state.StarterPackPurchased);
        Assert.Null(state.AdBoostExpiresUtc);
    }

    [Fact]
    public void ResetProgress_KeepsSamePlayerId()
    {
        var state = TestGameConfigFactory.CreateState(Now);
        var originalId = state.PlayerId;

        GameCalculationEngine.ResetProgress(state, Now);

        Assert.Equal(originalId, state.PlayerId);
    }
}
