using System.Text.Json;
using System.Text.Json.Serialization;
using ClickerTycoon.Application.Common.Interfaces;
using ClickerTycoon.Domain.Configuration;

namespace ClickerTycoon.Infrastructure.Configuration;

/// <summary>
/// Loads all game-balance parameters from a JSON file on disk (gameconfig.json)
/// exactly once at startup, so tuning the economy never requires touching code.
/// </summary>
public class JsonGameConfigProvider : IGameConfigProvider
{
    private readonly GameConfig _config;

    public JsonGameConfigProvider(string configFilePath)
    {
        if (!File.Exists(configFilePath))
            throw new FileNotFoundException($"Файл конфигурации игры не найден: {configFilePath}");

        var json = File.ReadAllText(configFilePath);
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };

        _config = JsonSerializer.Deserialize<GameConfig>(json, options)
                  ?? throw new InvalidOperationException("Не удалось разобрать конфигурацию игры (gameconfig.json).");
    }

    public GameConfig GetConfig() => _config;
}

public class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}

public class RandomProvider : IRandomProvider
{
    public double NextDouble() => Random.Shared.NextDouble();
}
