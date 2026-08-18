using ClickerTycoon.Domain.Configuration;

namespace ClickerTycoon.Application.Common.Interfaces;

/// <summary>Provides the game's economic/balance configuration, loaded from gameconfig.json.</summary>
public interface IGameConfigProvider
{
    GameConfig GetConfig();
}

/// <summary>Abstraction over the system clock, so handlers are unit-testable.</summary>
public interface IClock
{
    DateTime UtcNow { get; }
}

/// <summary>Abstraction over randomness, so event/crit rolls are unit-testable.</summary>
public interface IRandomProvider
{
    /// <summary>Returns a double in [0, 1).</summary>
    double NextDouble();
}
