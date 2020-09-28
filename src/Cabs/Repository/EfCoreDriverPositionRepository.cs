using System.Linq;
using LegacyFighter.Cabs.Entity;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace LegacyFighter.Cabs.Repository;

public interface IDriverPositionRepository
{
  Task<DriverPosition> Save(DriverPosition position);
}

internal class EfCoreDriverPositionRepository : IDriverPositionRepository
{
  private readonly SqLiteDbContext _context;

  public EfCoreDriverPositionRepository(SqLiteDbContext context)
  {
    _context = context;
  }

  public async Task<DriverPosition> Save(DriverPosition position)
  {
    _context.DriverPositions.Update(position);
    await _context.SaveChangesAsync();
    return position;
  }
}