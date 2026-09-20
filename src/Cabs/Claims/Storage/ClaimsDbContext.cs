using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NodaTime;

namespace LegacyFighter.Cabs.Claims.Storage;

public class ClaimsDbContext : DbContext
{
  private readonly ClaimsDatabase _database;

  public ClaimsDbContext(ClaimsDatabase database)
  {
    _database = database;
  }

  public DbSet<ClaimRecord> Claims { get; set; } = default!;
  public DbSet<ClaimantRecord> Claimants { get; set; } = default!;
  public DbSet<ClaimedTransitRecord> ClaimedTransits { get; set; } = default!;

  protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
  {
    optionsBuilder.UseSqlite(_database.Connection);
    base.OnConfiguring(optionsBuilder);
  }

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    var instantConverter = new ValueConverter<Instant, long>(
      instant => instant.ToUnixTimeTicks(),
      ticks => Instant.FromUnixTimeTicks(ticks));

    modelBuilder.Entity<ClaimRecord>(builder =>
    {
      builder.ToTable("Claims");
      builder.HasKey(claim => claim.ClaimId);
      builder.Property(claim => claim.ClaimId).ValueGeneratedNever();
      builder.Property(claim => claim.Status).HasConversion<string>().IsRequired();
      builder.Property(claim => claim.CompletionMode).HasConversion<string>();
      builder.Property(claim => claim.CreatedAt).HasConversion(instantConverter).IsRequired();
      builder.Property(claim => claim.CompletedAt).HasConversion(instantConverter);
    });
    modelBuilder.Entity<ClaimantRecord>(builder =>
    {
      builder.ToTable("Claimants");
      builder.HasKey(claimant => claimant.ClientId);
      builder.Property(claimant => claimant.ClientId).ValueGeneratedNever();
    });
    modelBuilder.Entity<ClaimedTransitRecord>(builder =>
    {
      builder.ToTable("ClaimedTransits");
      builder.HasKey(transit => transit.TransitId);
      builder.Property(transit => transit.TransitId).ValueGeneratedNever();
    });
  }
}
