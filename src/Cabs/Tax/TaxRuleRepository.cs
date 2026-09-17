using LegacyFighter.Cabs.Repository;
using Microsoft.EntityFrameworkCore;

namespace LegacyFighter.Cabs.Tax;

public interface ITaxRuleRepository
{
  Task<TaxRule> Find(long? id);
  Task<TaxRule> FindByTaxCodeContaining(string taxCode);
  Task<TaxRule> Save(TaxRule taxRule);
  Task Delete(TaxRule taxRule);
}

internal class EfCoreTaxRuleRepository : ITaxRuleRepository
{
  private readonly SqLiteDbContext _context;

  public EfCoreTaxRuleRepository(SqLiteDbContext context)
  {
    _context = context;
  }

  public async Task<TaxRule> Find(long? id)
  {
    return await _context.TaxRules.FindAsync(id);
  }

  public async Task<TaxRule> FindByTaxCodeContaining(string taxCode)
  {
    return await _context.TaxRules.FirstOrDefaultAsync(r => r.TaxCode.Contains(taxCode));
  }

  public async Task<TaxRule> Save(TaxRule taxRule)
  {
    _context.TaxRules.Update(taxRule);
    await _context.SaveChangesAsync();
    return taxRule;
  }

  public async Task Delete(TaxRule taxRule)
  {
    _context.TaxRules.Remove(taxRule);
    await _context.SaveChangesAsync();
  }
}
