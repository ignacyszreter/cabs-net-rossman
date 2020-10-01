using System.Linq;
using LegacyFighter.Cabs.Entity;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace LegacyFighter.Cabs.Repository;

public interface IDriverPositionRepository
{
  Task<List<DriverPosition>> FindByDriverAndSeenAtBetweenOrderBySeenAtAsc(Driver driver, Instant @from, Instant to);
  Task<DriverPosition> Save(DriverPosition position);
}

internal class EfCoreDriverPositionRepository : IDriverPositionRepository
{
  private readonly SqLiteDbContext _context;

  public EfCoreDriverPositionRepository(SqLiteDbContext context)
  {
    _context = context;
  }

  public async Task<List<DriverPosition>> FindByDriverAndSeenAtBetweenOrderBySeenAtAsc(Driver driver, Instant @from,
    Instant to)
  {
    return await _context.DriverPositions.Where(
        d => d.Driver == driver && 
             d.SeenAt >= from && 
             d.SeenAt <= to)
      .ToListAsync();
  }

  public async Task<DriverPosition> Save(DriverPosition position)
  {
    _context.DriverPositions.Update(position);
    await _context.SaveChangesAsync();
    return position;
  }
}