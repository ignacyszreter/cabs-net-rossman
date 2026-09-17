using System;
using System.Linq;
using LegacyFighter.Cabs.Entity;
using LegacyFighter.Cabs.MoneyValue;
using LegacyFighter.Cabs.Tax;
using LegacyFighter.CabsTests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LegacyFighter.CabsTests.Integration;

public class ManageCountryTaxRulesIntegrationTest
{
  private ITaxRuleService TaxRuleService => _app.TaxRuleService;
  private Fixtures Fixtures => _app.Fixtures;
  private CabsApp _app = default!;

  [SetUp]
  public void InitializeApp()
  {
    _app = CabsApp.CreateInstance();
  }

  [TearDown]
  public async Task DisposeOfApp()
  {
    await _app.DisposeAsync();
  }

  [Test]
  public async Task CountryIsAlwaysValid()
  {
    //expect
    await this.Awaiting(_ => CreateConfigWithInitialRule("", 2, TaxRule.LinearRule(23, 0, "VAT-23")))
      .Should().ThrowExactlyAsync<InvalidOperationException>();
    await this.Awaiting(_ => CreateConfigWithInitialRule(null, 2, TaxRule.LinearRule(23, 0, "VAT-23")))
      .Should().ThrowExactlyAsync<InvalidOperationException>();
    await this.Awaiting(_ => CreateConfigWithInitialRule("P", 2, TaxRule.LinearRule(23, 0, "VAT-23")))
      .Should().ThrowExactlyAsync<InvalidOperationException>();
  }

  [Test]
  public async Task AFactorIsNotZero()
  {
    //given
    await CreateConfigWithInitialRule("Czechy", 2, TaxRule.LinearRule(21, 0, "DPH-21"));

    //expect
    await this.Awaiting(_ => TaxRuleService.AddTaxRuleToCountry("Czechy", 0, 400, "OPLATA-MIEJSKA"))
      .Should().ThrowExactlyAsync<InvalidOperationException>();
    await this.Awaiting(_ => TaxRuleService.AddTaxRuleToCountry("Czechy", 0, 4, 5, "OPLATA-NOCNA"))
      .Should().ThrowExactlyAsync<InvalidOperationException>();
  }

  [Test]
  public async Task CannotHaveMoreThanMaximumNumberOfRules()
  {
    //given
    await CreateConfigWithInitialRule("Polska", 2, TaxRule.LinearRule(8, 0, "VAT-8"));
    //and
    await TaxRuleService.AddTaxRuleToCountry("Polska", 2, 400, "OPLATA-LOTNISKOWA");

    //expect
    await this.Awaiting(_ => TaxRuleService.AddTaxRuleToCountry("Polska", 1, 200, "OPLATA-MIEJSKA"))
      .Should().ThrowExactlyAsync<InvalidOperationException>();
  }

  [Test]
  public async Task CannotAddSquareRuleAboveMaximumNumberOfRules()
  {
    //given
    await CreateConfigWithInitialRule("Polska", 2, TaxRule.LinearRule(8, 0, "VAT-8"));
    //and
    await TaxRuleService.AddTaxRuleToCountry("Polska", 2, 400, "OPLATA-LOTNISKOWA");

    //expect
    await this.Awaiting(_ => TaxRuleService.AddTaxRuleToCountry("Polska", 1, 2, 300, "OPLATA-NOCNA"))
      .Should().ThrowExactlyAsync<InvalidOperationException>();
  }

  [Test]
  public async Task CanAddRule()
  {
    //given
    await CreateConfigWithInitialRule("Polska", 2, TaxRule.LinearRule(8, 0, "VAT-8"));

    //when
    await TaxRuleService.AddTaxRuleToCountry("Polska", 2, 400, "OPLATA-LOTNISKOWA");

    //then
    Assert.AreEqual(2, await TaxRuleService.RulesCount("Polska"));
  }

  [Test]
  public async Task CanDeleteRule()
  {
    //given
    var config = await CreateConfigWithInitialRule("Polska", 2, TaxRule.LinearRule(8, 0, "VAT-8"));
    //and
    await TaxRuleService.AddTaxRuleToCountry("Polska", 2, 400, "OPLATA-LOTNISKOWA");
    //and
    var ruleId = await RuleIdByTaxCode("Polska", "OPLATA-LOTNISKOWA");

    //when
    await TaxRuleService.DeleteRule(ruleId, config.Id);

    //then
    Assert.AreEqual(1, await TaxRuleService.RulesCount("Polska"));
  }

  [Test]
  public async Task CannotDeleteLastRule()
  {
    //given
    var config = await CreateConfigWithInitialRule("Polska", 2, TaxRule.LinearRule(8, 0, "VAT-8"));
    //and
    var ruleId = await RuleIdByTaxCode("Polska", "VAT-8");

    //expect
    await this.Awaiting(_ => TaxRuleService.DeleteRule(ruleId, config.Id))
      .Should().ThrowExactlyAsync<InvalidOperationException>();
  }

  [Test]
  public async Task CalculatesTaxFromAllRulesOfCountry()
  {
    //given
    await CreateConfigWithInitialRule("Polska", 10, TaxRule.LinearRule(8, 0, "VAT-8"));

    //when
    await TaxRuleService.AddTaxRuleToCountry("Polska", 5, 200, "OPLATA-LOTNISKOWA");

    //then
    Assert.AreEqual(new Money(1500), await TaxRuleService.CalculateTax("Polska", new Money(10000)));
  }

  [Test]
  public async Task ParallelDeleteKeepsAtLeastOneRule()
  {
    //given
    var config = await CreateConfigWithInitialRule("Polska", 5, TaxRule.LinearRule(8, 0, "VAT-8"));
    //and
    await TaxRuleService.AddTaxRuleToCountry("Polska", 2, 400, "OPLATA-LOTNISKOWA");
    //and
    using var firstRequest = _app.NewConcurrentRequestScope();
    using var secondRequest = _app.NewConcurrentRequestScope();
    //and
    var configOfFirstRequest = await ConfigIn(firstRequest, config.Id);
    var configOfSecondRequest = await ConfigIn(secondRequest, config.Id);

    //when
    configOfFirstRequest.Remove(configOfFirstRequest.TaxRules.First(), _app.Clock.GetCurrentInstant());
    configOfSecondRequest.Remove(configOfSecondRequest.TaxRules.Last(), _app.Clock.GetCurrentInstant());
    //and
    await SaveIn(firstRequest, configOfFirstRequest);

    //then
    await this.Awaiting(_ => SaveIn(secondRequest, configOfSecondRequest))
      .Should().ThrowExactlyAsync<DbUpdateConcurrencyException>();
    //and
    Assert.AreEqual(1, await TaxRuleService.RulesCount("Polska"));
  }

  [Test]
  public async Task ParallelAddRespectsMaximumNumberOfRules()
  {
    //given
    var config = await CreateConfigWithInitialRule("Polska", 2, TaxRule.LinearRule(8, 0, "VAT-8"));
    //and
    using var firstRequest = _app.NewConcurrentRequestScope();
    using var secondRequest = _app.NewConcurrentRequestScope();
    //and
    var configOfFirstRequest = await ConfigIn(firstRequest, config.Id);
    var configOfSecondRequest = await ConfigIn(secondRequest, config.Id);

    //when
    configOfFirstRequest.Add(TaxRule.LinearRule(2, 400, "OPLATA-LOTNISKOWA"), _app.Clock.GetCurrentInstant());
    configOfSecondRequest.Add(TaxRule.LinearRule(1, 300, "OPLATA-NOCNA"), _app.Clock.GetCurrentInstant());
    //and
    await SaveIn(firstRequest, configOfFirstRequest);

    //then
    await this.Awaiting(_ => SaveIn(secondRequest, configOfSecondRequest))
      .Should().ThrowExactlyAsync<DbUpdateConcurrencyException>();
    //and
    Assert.AreEqual(2, await TaxRuleService.RulesCount("Polska"));
  }

  private async Task<TaxConfig> CreateConfigWithInitialRule(string country, int maxRules, TaxRule rule)
  {
    return await TaxRuleService.CreateTaxConfigWithRule(country, maxRules, rule);
  }

  private async Task<long?> RuleIdByTaxCode(string country, string taxCode)
  {
    return (await TaxRuleService.FindRules(country)).Single(r => r.TaxCode.EndsWith(taxCode)).Id;
  }

  private static async Task<TaxConfig> ConfigIn(IServiceScope request, long? configId)
  {
    return await request.ServiceProvider.GetRequiredService<ITaxConfigRepository>().Find(configId);
  }

  private static async Task SaveIn(IServiceScope request, TaxConfig taxConfig)
  {
    await request.ServiceProvider.GetRequiredService<ITaxConfigRepository>().Save(taxConfig);
  }
}
