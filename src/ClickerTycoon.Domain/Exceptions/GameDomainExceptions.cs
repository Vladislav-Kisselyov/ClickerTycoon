namespace ClickerTycoon.Domain.Exceptions;

/// <summary>
/// Base type for all expected, "business" errors. These are caught by the API
/// exception-handling middleware and translated into clean HTTP responses -
/// they are never allowed to surface as raw exceptions or HTTP 500s.
/// </summary>
public abstract class GameDomainException : Exception
{
    protected GameDomainException(string message) : base(message) { }
}

public class PlayerNotFoundException : GameDomainException
{
    public PlayerNotFoundException(Guid playerId)
        : base($"Игрок с идентификатором '{playerId}' не найден.") { }
}

public class InsufficientResourceException : GameDomainException
{
    public InsufficientResourceException(decimal required, decimal available)
        : base($"Недостаточно ресурса: требуется {required:0}, доступно {available:0}.") { }
}

public class UpgradeNotFoundException : GameDomainException
{
    public UpgradeNotFoundException(string upgradeId)
        : base($"Улучшение '{upgradeId}' не существует.") { }
}

public class UpgradeNotAvailableException : GameDomainException
{
    public UpgradeNotAvailableException(string upgradeId, int requiredStage)
        : base($"Улучшение '{upgradeId}' станет доступно на этапе {requiredStage}.") { }
}

public class UpgradeMaxLevelReachedException : GameDomainException
{
    public UpgradeMaxLevelReachedException(string upgradeId)
        : base($"Улучшение '{upgradeId}' уже прокачано до максимального уровня.") { }
}

public class InvalidGameStateException : GameDomainException
{
    public InvalidGameStateException(string message) : base(message) { }
}

public class MonetizationOfferNotFoundException : GameDomainException
{
    public MonetizationOfferNotFoundException(string offerId)
        : base($"Предложение монетизации '{offerId}' не найдено.") { }
}

public class MonetizationOfferAlreadyPurchasedException : GameDomainException
{
    public MonetizationOfferAlreadyPurchasedException(string offerId)
        : base($"Предложение '{offerId}' уже приобретено.") { }
}
