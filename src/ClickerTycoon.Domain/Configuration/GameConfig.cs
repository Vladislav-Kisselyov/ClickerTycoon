namespace ClickerTycoon.Domain.Configuration;

public enum UpgradeEffectType
{
    /// <summary>Flat amount added to every click.</summary>
    ClickFlat,

    /// <summary>Percentage bonus applied to click value (0.2 = +20%).</summary>
    ClickPercent,

    /// <summary>Flat resource-per-second generated automatically. Introduces automation.</summary>
    AutomationFlat,

    /// <summary>Percentage bonus applied to total automation output.</summary>
    AutomationPercent,

    /// <summary>Adds to the chance (0..1) that a click is a critical hit.</summary>
    CritChance,

    /// <summary>Adds to the chance (0..1) that the "viral" event triggers.</summary>
    ViralChanceBonus
}

public class UpgradeDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = "⭐";
    public UpgradeEffectType EffectType { get; set; }
    public double EffectValuePerLevel { get; set; }
    public double BaseCost { get; set; }
    public double CostGrowth { get; set; } = 1.15;
    public int MaxLevel { get; set; } = 999;
    public int RequiredStage { get; set; } = 1;

    /// <summary>True for upgrades that change *how* the game is played rather than just scaling numbers.</summary>
    public bool ChangesMechanic { get; set; }

    public double CostAtLevel(int currentLevel) => BaseCost * Math.Pow(CostGrowth, currentLevel);
}

public class StageDefinition
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal ResourceRequired { get; set; }
    public string? RequiredUpgradeId { get; set; }
    public List<string> Unlocks { get; set; } = new();
}

public class EventsConfig
{
    public double ViralEventChancePerClick { get; set; } = 0.01;
    public int ViralEventDurationSeconds { get; set; } = 30;
    public double ViralEventMultiplier { get; set; } = 3.0;

    public double CrisisEventChancePerClick { get; set; } = 0.006;
    public int CrisisClicksToResolve { get; set; } = 8;
    public double CrisisIncomeMultiplier { get; set; } = 0.4;

    public double CatStoleEventChancePerClick { get; set; } = 0.004;
    public double CatStoleLossPercent { get; set; } = 0.15;
    public int CatStoleBoostDurationSeconds { get; set; } = 20;
    public double CatStoleBoostMultiplier { get; set; } = 2.0;

    /// <summary>Minimum clicks that must occur between two triggered events (cooldown).</summary>
    public int MinClicksBetweenEvents { get; set; } = 15;
}

public class MonetizationConfig
{
    public int AdBoostDurationSeconds { get; set; } = 60;
    public double AdBoostMultiplier { get; set; } = 2.0;

    public double PremiumCameraClickPercentBonus { get; set; } = 0.25;

    public decimal StarterPackResourceAmount { get; set; } = 500;
    public int StarterPackBoostDurationSeconds { get; set; } = 60;
    public double StarterPackBoostMultiplier { get; set; } = 1.5;
}

public class GameConfig
{
    public double BaseClickValue { get; set; } = 1.0;
    public double BaseCritChance { get; set; } = 0.05;
    public double CritMultiplier { get; set; } = 5.0;

    /// <summary>Automation income is only ever applied for elapsed time up to this cap, to avoid absurd offline gains.</summary>
    public int MaxOfflineSeconds { get; set; } = 3600;

    public List<UpgradeDefinition> Upgrades { get; set; } = new();
    public List<StageDefinition> Stages { get; set; } = new();
    public EventsConfig Events { get; set; } = new();
    public MonetizationConfig Monetization { get; set; } = new();
}
