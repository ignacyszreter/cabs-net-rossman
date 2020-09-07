using System.Linq;
using LegacyFighter.Cabs.Entity;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace LegacyFighter.Cabs.Repository;

public interface ITransitRepository
{
  Task<Transit> Find(long? transitId);
  Task<Transit> Save(Transit transit);
}

internal class EfCoreTransitRepository : ITransitRepository
{
  private readonly SqLiteDbContext _context;

  public EfCoreTransitRepository(SqLiteDbContext context)
  {
    _context = context;
  }

  public async Task<Transit> Find(long? transitId)
  {
    return await _context.Transits.FindAsync(transitId);
  }

  public async Task<Transit> Save(Transit transit)
  {
    _context.Transits.Update(transit);
    await _context.SaveChangesAsync();
    return transit;
  }
}