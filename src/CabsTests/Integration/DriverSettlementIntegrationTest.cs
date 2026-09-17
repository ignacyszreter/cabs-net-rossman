using LegacyFighter.Cabs.Entity;
using LegacyFighter.CabsTests.Common;
using NodaTime;
using VerifyTests;
using static VerifyNUnit.Verifier;

namespace LegacyFighter.CabsTests.Integration;

[Category("SqlServer")]
public class DriverSettlementIntegrationTest
{
  private CabsApp _app = default!;

  [SetUp]
  public void InitializeApp()
  {
    _app = CabsApp.CreateInstanceOnSqlServer(_ => { });
  }

  [TearDown]
  public async Task DisposeOfApp()
  {
    await _app.DisposeAsync();
  }

  private async Task<long> ADriverWithTransits(params int[] years)
  {
    _app.StartReuseRequestScope();
    var driver = await _app.Fixtures.ADriver();
    foreach (var year in years)
    {
      await _app.Fixtures.ATransit(driver, 60, new LocalDateTime(year, 10, 1, 6, 30));
      await _app.Fixtures.ATransit(driver, 70, new LocalDateTime(year, 10, 10, 2, 30));
      await _app.Fixtures.ATransit(driver, 80, new LocalDateTime(year, 10, 30, 6, 30));
      await _app.Fixtures.ATransit(driver, 60, new LocalDateTime(year, 11, 10, 1, 30));
      await _app.Fixtures.ATransit(driver, 30, new LocalDateTime(year, 11, 10, 1, 30));
      await _app.Fixtures.ATransit(driver, 15, new LocalDateTime(year, 12, 10, 2, 30));
    }
    await _app.Fixtures.DriverHasFee(driver, DriverFee.FeeTypes.Flat, 10);
    _app.EndReuseRequestScope();
    return driver.Id!.Value;
  }

  private static SettingsTask VerifySettlement(object settlement)
  {
    return Verify(settlement)
      .DontScrubDateTimes()
      .AddExtraSettings(settings => settings.DefaultValueHandling = Argon.DefaultValueHandling.Include);
  }
}
