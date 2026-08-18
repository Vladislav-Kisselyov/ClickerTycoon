namespace ClickerTycoon.Application.Common.Dto;

public record UpgradeDto(
    string Id,
    string Name,
    string Description,
    string Icon,
    int Level,
    int MaxLevel,
    decimal NextCost,
    bool MaxedOut,
    bool Unlocked,
    int RequiredStage,
    bool ChangesMechanic,
    string EffectSummary);

public record ActiveEffectDto(string Type, string Label, int SecondsRemaining, double Multiplier);

public record MonetizationOfferDto(
    string Id,
    string Name,
    string Description,
    string Kind, // "ad" | "premium" | "starter-pack"
    bool Purchased,
    bool Active,
    int? SecondsRemaining);

public record StageInfoDto(
    int Id,
    string Name,
    string Description,
    List<string> Unlocks,
    bool IsCurrent);

public record NextStageDto(int Id, string Name, decimal ResourceRequired, string? RequiredUpgradeName);

public record GameStateDto(
    Guid PlayerId,
    decimal Resource,
    decimal TotalEarned,
    long TotalClicks,
    int CurrentStage,
    string StageName,
    string StageDescription,
    List<string> StageUnlocks,
    NextStageDto? NextStage,
    decimal ClickValuePreview,
    double CritChancePercent,
    double AutomationRatePerSecond,
    int CrisisActionsRemaining,
    List<UpgradeDto> Upgrades,
    List<ActiveEffectDto> ActiveEffects,
    List<MonetizationOfferDto> MonetizationOffers);

public record ClickResultDto(
    decimal AmountGained,
    bool IsCritical,
    decimal NewResource,
    string? TriggeredEventType,
    string? TriggeredEventMessage,
    bool StageAdvanced,
    GameStateDto State);

public record PurchaseUpgradeResultDto(string UpgradeId, decimal CostPaid, int NewLevel, GameStateDto State);

public record MonetizationResultDto(string OfferId, string Message, GameStateDto State);
