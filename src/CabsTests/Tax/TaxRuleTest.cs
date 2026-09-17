using System;
using LegacyFighter.Cabs.Tax;

namespace LegacyFighter.CabsTests.Tax;

public class TaxRuleTest
{
  [Test]
  public void AFactorIsAlwaysNotZero()
  {
    //expect
    new Func<TaxRule>(() => TaxRule.LinearRule(0, 400, "OPLATA-MIEJSKA"))
      .Should().ThrowExactly<InvalidOperationException>();
    new Func<TaxRule>(() => TaxRule.SquareRule(0, 4, 5, "OPLATA-NOCNA"))
      .Should().ThrowExactly<InvalidOperationException>();
  }
}
