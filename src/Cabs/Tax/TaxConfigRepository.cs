using LegacyFighter.Cabs.Repository;
using Microsoft.EntityFrameworkCore;

namespace LegacyFighter.Cabs.Tax;

public interface ITaxConfigRepository
{
  Task<TaxConfig> Find(long? id);
  Task<TaxConfig> FindByCountry(Country country);
  Task<List<TaxConfig>> FindAll();
  Task<TaxConfig> Save(TaxConfig taxConfig);
  Task Delete(TaxConfig taxConfig);
}

internal class EfCoreTaxConfigRepository : ITaxConfigRepository
{
  private readonly SqLiteDbContext _context;

  public EfCoreTaxConfigRepository(SqLiteDbContext context)
  {
    _context = context;
  }

  public async Task<TaxConfig> Find(long? id)
  {
    return await _context.TaxConfigs.Include(c => c.TaxRules).FirstOrDefaultAsync(c => c.Id == id);
  }

  public async Task<TaxConfig> FindByCountry(Country country)
  {
    return await _context.TaxConfigs.Include(c => c.TaxRules).FirstOrDefaultAsync(c => c.Country == country);
  }

  public async Task<List<TaxConfig>> FindAll()
  {
    return await _context.TaxConfigs.Include(c => c.TaxRules).ToListAsync();
  }

  public async Task<TaxConfig> Save(TaxConfig taxConfig)
  {
    _context.TaxConfigs.Update(taxConfig);
    await _context.SaveChangesAsync();
    return taxConfig;
  }

  public async Task Delete(TaxConfig taxConfig)
  {
    _context.TaxConfigs.Remove(taxConfig);
    await _context.SaveChangesAsync();
  }
}
