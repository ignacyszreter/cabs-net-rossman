using System;
using LegacyFighter.Cabs.Tax;
using NodaTime;

namespace LegacyFighter.CabsTests.Tax;

public class TaxConfigTest
{
  private static readonly Instant Now = SystemClock.Instance.GetCurrentInstant();

  [Test]
  public void CanAddLimitedRulesToCountry()
  {
    //given
    var config = NewConfigWithRuleAndMaxRules("Polska", 2, TaxRule.LinearRule(8, 0, "VAT-8"));

    //when
    config.Add(TaxRule.LinearRule(2, 400, "OPLATA-LOTNISKOWA"), Now);

    //then
    Assert.AreEqual(2, config.CurrentRulesCount);
  }

  [Test]
  public void RemovingRuleShouldBeTakenIntoAccount()
  {
    //given
    var config = NewConfigWithRuleAndMaxRules("Polska", 2, TaxRule.LinearRule(8, 0, "VAT-8"));
    //and
    var airportFee = TaxRule.LinearRule(2, 400, "OPLATA-LOTNISKOWA");
    config.Add(airportFee, Now);
    //and
    config.Remove(airportFee, Now);

    //then
    Assert.AreEqual(1, config.CurrentRulesCount);

    //when
    config.Add(TaxRule.LinearRule(1, 200, "OPLATA-MIEJSKA"), Now);

    //then
    Assert.AreEqual(2, config.CurrentRulesCount);
  }

  [Test]
  public void CannotAddMoreThanLimitedRulesToCountry()
  {
    //given
    var config = NewConfigWithRuleAndMaxRules("Polska", 2, TaxRule.LinearRule(8, 0, "VAT-8"));
    //and
    config.Add(TaxRule.LinearRule(2, 400, "OPLATA-LOTNISKOWA"), Now);

    //expect
    config.Invoking(c => c.Add(TaxRule.LinearRule(1, 200, "OPLATA-MIEJSKA"), Now))
      .Should().ThrowExactly<InvalidOperationException>();
  }

  [Test]
  public void CannotAddMoreThanLimitedSquareRulesToCountry()
  {
    //given
    var config = NewConfigWithRuleAndMaxRules("Polska", 2, TaxRule.LinearRule(8, 0, "VAT-8"));
    //and
    config.Add(TaxRule.LinearRule(2, 400, "OPLATA-LOTNISKOWA"), Now);

    //expect
    config.Invoking(c => c.Add(TaxRule.SquareRule(1, 2, 300, "OPLATA-NOCNA"), Now))
      .Should().ThrowExactly<InvalidOperationException>();
  }

  [Test]
  public void CountryConfigHasAtLeastOneRule()
  {
    //given
    var vat = TaxRule.LinearRule(8, 0, "VAT-8");
    var config = NewConfigWithRuleAndMaxRules("Polska", 2, vat);

    //expect
    config.Invoking(c => c.Remove(vat, Now))
      .Should().ThrowExactly<InvalidOperationException>();
  }

  [Test]
  public void CountryIsAlwaysValid()
  {
    //expect
    new Func<Country>(() => Country.Of(null)).Should().ThrowExactly<InvalidOperationException>();
    new Func<Country>(() => Country.Of("")).Should().ThrowExactly<InvalidOperationException>();
    new Func<Country>(() => Country.Of("P")).Should().ThrowExactly<InvalidOperationException>();
  }

  private static TaxConfig NewConfigWithRuleAndMaxRules(string country, int maxRules, TaxRule taxRule)
  {
    return new TaxConfig(country, maxRules, taxRule, Now);
  }
}
