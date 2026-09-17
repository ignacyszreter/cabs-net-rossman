using Microsoft.AspNetCore.Mvc;

namespace LegacyFighter.Cabs.Tax;

[ApiController]
[Route("[controller]")]
public class TaxConfigController
{
  private readonly ITaxRuleService _taxRuleService;

  public TaxConfigController(ITaxRuleService taxRuleService)
  {
    _taxRuleService = taxRuleService;
  }

  [HttpGet("/taxconfigs")]
  public async Task<Dictionary<string, List<TaxRuleDto>>> TaxConfigs()
  {
    var taxConfigs = await _taxRuleService.FindAllConfigs();
    var map = new Dictionary<string, List<TaxRuleDto>>();
    foreach (var taxConfig in taxConfigs)
    {
      if (!map.ContainsKey(taxConfig.Country.AsString()))
      {
        map.Add(taxConfig.Country.AsString(), taxConfig.TaxRules.Select(r => new TaxRuleDto(r)).ToList());
      }
      else
      {
        map[taxConfig.Country.AsString()].AddRange(taxConfig.TaxRules.Select(r => new TaxRuleDto(r)));
      }
    }

    return map;
  }
}
