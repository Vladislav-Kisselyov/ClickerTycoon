using System.Text.Json;
using ClickerTycoon.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ClickerTycoon.Infrastructure.Persistence;

public class GameDbContext : DbContext
{
    public DbSet<PlayerGameState> GameSaves => Set<PlayerGameState>();

    public GameDbContext(DbContextOptions<GameDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<PlayerGameState>(b =>
        {
            b.ToTable("GameSaves");
            b.HasKey(x => x.PlayerId);

            b.Property(x => x.Resource).HasColumnType("TEXT");
            b.Property(x => x.TotalEarned).HasColumnType("TEXT");

            var upgradesConverter = new ValueConverter<List<OwnedUpgrade>, string>(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<OwnedUpgrade>>(v, (JsonSerializerOptions?)null) ?? new List<OwnedUpgrade>());

            var upgradesComparer = new ValueComparer<List<OwnedUpgrade>>(
                (a, c) => JsonSerializer.Serialize(a, (JsonSerializerOptions?)null) == JsonSerializer.Serialize(c, (JsonSerializerOptions?)null),
                a => JsonSerializer.Serialize(a, (JsonSerializerOptions?)null).GetHashCode(),
                a => JsonSerializer.Deserialize<List<OwnedUpgrade>>(JsonSerializer.Serialize(a, (JsonSerializerOptions?)null), (JsonSerializerOptions?)null)!);

            b.Property(x => x.Upgrades)
                .HasConversion(upgradesConverter)
                .Metadata.SetValueComparer(upgradesComparer);

            var effectsConverter = new ValueConverter<List<ActiveEffect>, string>(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<ActiveEffect>>(v, (JsonSerializerOptions?)null) ?? new List<ActiveEffect>());

            var effectsComparer = new ValueComparer<List<ActiveEffect>>(
                (a, c) => JsonSerializer.Serialize(a, (JsonSerializerOptions?)null) == JsonSerializer.Serialize(c, (JsonSerializerOptions?)null),
                a => JsonSerializer.Serialize(a, (JsonSerializerOptions?)null).GetHashCode(),
                a => JsonSerializer.Deserialize<List<ActiveEffect>>(JsonSerializer.Serialize(a, (JsonSerializerOptions?)null), (JsonSerializerOptions?)null)!);

            b.Property(x => x.ActiveEffects)
                .HasConversion(effectsConverter)
                .Metadata.SetValueComparer(effectsComparer);
        });
    }
}
