using LegacyFighter.Cabs.Common;
using LegacyFighter.Cabs.MoneyValue;

namespace LegacyFighter.Cabs.Tax;

public class TransactionalTaxRuleService : ITaxRuleService
{
  private readonly ITaxRuleService _inner;
  private readonly ITransactions _transactions;

  public TransactionalTaxRuleService(
    ITaxRuleService inner,
    ITransactions transactions)
  {
    _inner = inner;
    _transactions = transactions;
  }

  public async Task AddTaxRuleToCountry(string country, int aFactor, int bFactor, string taxCode)
  {
    await using var tx = await _transactions.BeginTransaction();
    await _inner.AddTaxRuleToCountry(country, aFactor, bFactor, taxCode);
    await tx.Commit();
  }

  public async Task AddTaxRuleToCountry(string country, int aFactor, int bFactor, int cFactor, string taxCode)
  {
    await using var tx = await _transactions.BeginTransaction();
    await _inner.AddTaxRuleToCountry(country, aFactor, bFactor, cFactor, taxCode);
    await tx.Commit();
  }

  public async Task<TaxConfig> CreateTaxConfigWithRule(string country, TaxRule taxRule)
  {
    await using var tx = await _transactions.BeginTransaction();
    var taxConfig = await _inner.CreateTaxConfigWithRule(country, taxRule);
    await tx.Commit();
    return taxConfig;
  }

  public async Task<TaxConfig> CreateTaxConfigWithRule(string country, int maxRulesCount, TaxRule taxRule)
  {
    await using var tx = await _transactions.BeginTransaction();
    var taxConfig = await _inner.CreateTaxConfigWithRule(country, maxRulesCount, taxRule);
    await tx.Commit();
    return taxConfig;
  }

  public async Task DeleteRule(long? taxRuleId, long? configId)
  {
    await using var tx = await _transactions.BeginTransaction();
    await _inner.DeleteRule(taxRuleId, configId);
    await tx.Commit();
  }

  public async Task<IReadOnlyCollection<TaxRule>> FindRules(string country)
  {
    await using var tx = await _transactions.BeginTransaction();
    var taxRules = await _inner.FindRules(country);
    await tx.Commit();
    return taxRules;
  }

  public async Task<int> RulesCount(string country)
  {
    await using var tx = await _transactions.BeginTransaction();
    var rulesCount = await _inner.RulesCount(country);
    await tx.Commit();
    return rulesCount;
  }

  public async Task<List<TaxConfig>> FindAllConfigs()
  {
    await using var tx = await _transactions.BeginTransaction();
    var taxConfigs = await _inner.FindAllConfigs();
    await tx.Commit();
    return taxConfigs;
  }

  public async Task<Money> CalculateTax(string country, Money price)
  {
    await using var tx = await _transactions.BeginTransaction();
    var tax = await _inner.CalculateTax(country, price);
    await tx.Commit();
    return tax;
  }
}
