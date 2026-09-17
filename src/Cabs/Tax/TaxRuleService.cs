using LegacyFighter.Cabs.MoneyValue;
using NodaTime;

namespace LegacyFighter.Cabs.Tax;

public class TaxRuleService : ITaxRuleService
{
  private readonly ITaxRuleRepository _taxRuleRepository;
  private readonly ITaxConfigRepository _taxConfigRepository;
  private readonly IClock _clock;

  public TaxRuleService(
    ITaxRuleRepository taxRuleRepository,
    ITaxConfigRepository taxConfigRepository,
    IClock clock)
  {
    _taxRuleRepository = taxRuleRepository;
    _taxConfigRepository = taxConfigRepository;
    _clock = clock;
  }

  public async Task AddTaxRuleToCountry(string country, int aFactor, int bFactor, string taxCode)
  {
    if (aFactor == 0)
    {
      throw new InvalidOperationException("Invalid aFactor");
    }

    var taxRule = new TaxRule
    {
      AFactor = aFactor,
      BFactor = bFactor,
      IsLinear = true,
      TaxCode = "Dz.U. " + _clock.GetCurrentInstant().InUtc().Year + " " + taxCode
    };

    var taxConfig = await _taxConfigRepository.FindByCountry(Country.Of(country));
    if (taxConfig == null)
    {
      taxConfig = await CreateTaxConfigWithRule(country, taxRule);
      return;
    }

    if (taxConfig.MaxRulesCount <= taxConfig.TaxRules.Count)
    {
      throw new InvalidOperationException("Too many rules");
    }

    taxConfig.TaxRules.Add(taxRule);
    taxConfig.CurrentRulesCount += 1;
    taxConfig.LastModifiedDate = _clock.GetCurrentInstant();
  }

  public async Task<TaxConfig> CreateTaxConfigWithRule(string country, TaxRule taxRule)
  {
    var taxConfig = new TaxConfig
    {
      Country = Country.Of(country),
      TaxRules = new List<TaxRule>()
    };

    taxConfig.TaxRules.Add(taxRule);
    taxConfig.CurrentRulesCount = taxConfig.TaxRules.Count;
    taxConfig.MaxRulesCount = 10;
    taxConfig.LastModifiedDate = _clock.GetCurrentInstant();

    return await _taxConfigRepository.Save(taxConfig);
  }

  public async Task<TaxConfig> CreateTaxConfigWithRule(string country, int maxRulesCount, TaxRule taxRule)
  {
    var taxConfig = new TaxConfig
    {
      Country = Country.Of(country),
      TaxRules = new List<TaxRule>()
    };

    taxConfig.TaxRules.Add(taxRule);
    taxConfig.CurrentRulesCount = taxConfig.TaxRules.Count;
    taxConfig.MaxRulesCount = maxRulesCount;
    taxConfig.LastModifiedDate = _clock.GetCurrentInstant();

    return await _taxConfigRepository.Save(taxConfig);
  }

  public async Task AddTaxRuleToCountry(string country, int aFactor, int bFactor, int cFactor, string taxCode)
  {
    if (aFactor == 0)
    {
      throw new InvalidOperationException("Invalid aFactor");
    }

    var taxRule = new TaxRule
    {
      ASquareFactor = aFactor,
      BSquareFactor = bFactor,
      CSquareFactor = cFactor,
      IsSquare = true,
      TaxCode = "Dz.U. " + _clock.GetCurrentInstant().InUtc().Year + " " + taxCode
    };

    var taxConfig = await _taxConfigRepository.FindByCountry(Country.Of(country));
    if (taxConfig == null)
    {
      await CreateTaxConfigWithRule(country, taxRule);
      return;
    }

    if (taxConfig.MaxRulesCount <= taxConfig.TaxRules.Count)
    {
      throw new InvalidOperationException("Too many rules");
    }

    taxConfig.TaxRules.Add(taxRule);
    taxConfig.CurrentRulesCount += 1;
    taxConfig.LastModifiedDate = _clock.GetCurrentInstant();
  }

  public async Task DeleteRule(long? taxRuleId, long? configId)
  {
    var taxRule = await _taxRuleRepository.Find(taxRuleId);
    var taxConfig = await _taxConfigRepository.Find(configId);
    if (taxConfig.TaxRules.Contains(taxRule))
    {
      if (taxConfig.TaxRules.Count == 1)
      {
        throw new InvalidOperationException("Last rule in country config");
      }

      await _taxRuleRepository.Delete(taxRule);
      taxConfig.TaxRules.Remove(taxRule);
      taxConfig.CurrentRulesCount -= 1;
      taxConfig.LastModifiedDate = _clock.GetCurrentInstant();
    }
  }

  public async Task<List<TaxRule>> FindRules(string country)
  {
    return (await _taxConfigRepository.FindByCountry(Country.Of(country))).TaxRules;
  }

  public async Task<int> RulesCount(string country)
  {
    return (await _taxConfigRepository.FindByCountry(Country.Of(country))).CurrentRulesCount;
  }

  public async Task<List<TaxConfig>> FindAllConfigs()
  {
    return await _taxConfigRepository.FindAll();
  }

  public async Task<Money> CalculateTax(string country, Money price)
  {
    var taxConfig = await _taxConfigRepository.FindByCountry(Country.Of(country));
    var tax = 0;
    foreach (var rule in taxConfig.TaxRules)
    {
      if (rule.IsLinear)
      {
        tax += price.IntValue * rule.AFactor / 100 + rule.BFactor;
      }

      if (rule.IsSquare)
      {
        var priceInZloty = price.IntValue / 100;
        tax += rule.ASquareFactor * priceInZloty * priceInZloty + rule.BSquareFactor * priceInZloty +
               rule.CSquareFactor;
      }
    }

    return new Money(tax);
  }
}
