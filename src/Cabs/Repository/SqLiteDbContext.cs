using System.Linq;
using System.Data.Common;
using LegacyFighter.Cabs.Common;
using LegacyFighter.Cabs.Entity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NodaTime;

namespace LegacyFighter.Cabs.Repository;

public class SqLiteDbContext : DbContext
{
  private readonly DbConnection _connection;
  public DbSet<Address> Addresses { get; set; }
  public DbSet<Client> Clients { get; set; }
  public DbSet<Driver> Drivers { get; set; }
  public DbSet<DriverPosition> DriverPositions { get; set; }
  public DbSet<DriverSession> DriverSessions { get; set; }
  public DbSet<Transit> Transits { get; set; }

  public static DbConnection CreateInMemoryDatabase()
  {
    var connection = new SqliteConnection("Filename=:memory:");
    connection.Open();
    return connection;
  }

  public SqLiteDbContext(DbConnection connection)
  {
    _connection = connection;
  }

  protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
  {
    optionsBuilder.UseLazyLoadingProxies().UseSqlite(_connection);
    base.OnConfiguring(optionsBuilder);
  }

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    var instantConverter = new ValueConverter<Instant,long>(
        instant => instant.ToUnixTimeTicks(),
        ticks => Instant.FromUnixTimeTicks(ticks));

    base.OnModelCreating(modelBuilder);

    modelBuilder.Entity<Address>(builder =>
    {
      builder.MapBaseEntityProperties();
      builder.HasIndex(u => u.Hash).IsUnique();
    });
    modelBuilder.Entity<Client>(builder =>
    {
      builder.MapBaseEntityProperties();
    });
    modelBuilder.Entity<Driver>(e =>
    {
      e.MapBaseEntityProperties();
      e.Property(d => d.Status).HasConversion<string>().IsRequired();
      e.Property(d => d.Type).HasConversion<string>();
      e.Property(d => d.DriverLicense).IsRequired();
      e.HasMany(d => d.Transits).WithOne(t => t.Driver);
    });
    modelBuilder.Entity<DriverPosition>(builder =>
    {
      builder.MapBaseEntityProperties();
      builder.HasOne(p => p.Driver);
      builder.Property(p => p.Latitude).IsRequired();
      builder.Property(p => p.Longitude).IsRequired();
      builder.Property(p => p.SeenAt).HasConversion(instantConverter).IsRequired();
    });
    modelBuilder.Entity<DriverSession>(builder =>
    {
      builder.MapBaseEntityProperties();
      builder.Property(x => x.LoggedAt).HasConversion(instantConverter).IsRequired();
      builder.Property(x => x.LoggedOutAt).HasConversion(instantConverter);
      builder.Property(x => x.PlatesNumber).IsRequired();
      builder.HasOne(s => s.Driver);
    });
    modelBuilder.Entity<Transit>(builder =>
    {
      builder.MapBaseEntityProperties();
      builder.Property(x => x.CompleteAt).HasConversion(instantConverter);
      builder.Property(x => x.DateTime).HasConversion(instantConverter);
      builder.Property(x => x.Date).HasConversion(instantConverter);
      builder.HasOne(t => t.To);
      builder.HasOne(t => t.Client);
      builder.HasOne(t => t.Driver).WithMany(d => d.Transits);
    });
  }
}

public static class EfCoreExtensions
{
  public static void MapBaseEntityProperties<T>(this EntityTypeBuilder<T> builder) where T : BaseEntity
  {
    builder.HasKey(e => e.Id);
  }
}