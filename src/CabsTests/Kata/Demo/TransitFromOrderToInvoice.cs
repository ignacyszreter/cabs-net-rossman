using System.Threading.Tasks;
using LegacyFighter.Cabs.CarFleet;
using LegacyFighter.Cabs.Ride;
using LegacyFighter.Cabs.Ride.Details;
using LegacyFighter.CabsTests.Common;
using NodaTime;

namespace LegacyFighter.CabsTests.Kata.Demo;

public class TransitFromOrderToInvoice
{
  private static readonly DateTimeZone Warsaw = DateTimeZoneProviders.Tzdb["Europe/Warsaw"];
  private static readonly Instant FridayEvening =
    new LocalDateTime(2025, 9, 12, 18, 0).InZoneStrictly(Warsaw).ToInstant();

  private CabsApp _app = default!;
  private Fixtures Fixtures => _app.Fixtures;
  private CabsApi Cabs => _app.Api;

  [SetUp]
  public void Setup()
  {
    _app = CabsApp.CreateInstance();
    _app.Clock.Reset(FridayEvening);
  }

  [TearDown]
  public void TearDown()
  {
    _app.Dispose();
  }

  [Test]
  public async Task FridayEveningTransitOf42KmCosts11500()
  {
    await Fixtures.ARegisteredActiveCarCategory(CarClasses.Van);
    var driver = await Fixtures.ADriverNearby("WU1212");
    await Fixtures.DriverHasFlatFee(driver, 10);
    var (from, to) = Fixtures.AddressesOf42KmDistance();

    var ordered = await Cabs.OrderTransit(await Fixtures.ARegisteredClient(), from, to);
    Assert.AreEqual(11500, (int)ordered.EstimatedPrice);
    Assert.AreEqual("Weekend+", ordered.Tariff);

    await Cabs.PublishTransit(ordered.RequestId);
    await Cabs.AcceptTransit(ordered.RequestId, driver);
    await Cabs.StartTransit(ordered.RequestId, driver);
    var completed = await Cabs.CompleteTransit(ordered.RequestId, driver, to);

    Assert.AreEqual(Statuses.Completed, completed.Status);
    Assert.AreEqual(11500, (int?)completed.Price);
    Assert.AreEqual(11490, (int?)completed.DriverFee);
  }
}
