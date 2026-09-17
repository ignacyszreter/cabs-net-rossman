using LegacyFighter.Cabs.MoneyValue;

namespace LegacyFighter.Cabs.Tax;

public interface ITaxRuleService
{
  Task AddTaxRuleToCountry(string country, int aFactor, int bFactor, string taxCode);
  Task AddTaxRuleToCountry(string country, int aFactor, int bFactor, int cFactor, string taxCode);
  Task<TaxConfig> CreateTaxConfigWithRule(string country, TaxRule taxRule);
  Task<TaxConfig> CreateTaxConfigWithRule(string country, int maxRulesCount, TaxRule taxRule);
  Task DeleteRule(long? taxRuleId, long? configId);
  Task<List<TaxRule>> FindRules(string country);
  Task<int> RulesCount(string country);
  Task<List<TaxConfig>> FindAllConfigs();
  Task<Money> CalculateTax(string country, Money price);
}
