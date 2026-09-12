using System.Threading.Tasks;
using LegacyFighter.CabsTests.Common;
using NodaTime;

namespace LegacyFighter.CabsTests.Kata.Demo;

public class PriceListCharacterization
{
  private static readonly DateTimeZone Warsaw = DateTimeZoneProviders.Tzdb["Europe/Warsaw"];

  private CabsApp _app = default!;

  [SetUp]
  public void Setup()
  {
    _app = CabsApp.CreateInstance();
  }

  [TearDown]
  public void TearDown()
  {
    _app.Dispose();
  }

  [Test]
  public async Task SaturdayNoonTransitOf42KmCosts7100()
  {
    var when = new LocalDateTime(2019, 1, 12, 12, 0).InZoneStrictly(Warsaw).ToInstant();
    var (from, to) = _app.Fixtures.AddressesOf42KmDistance();
    var transit = await _app.Fixtures.ADraftTransitAt(when, from, to);

    var quoted = await _app.Api.FindTransit(transit);

    ((int)quoted.EstimatedPrice).Should().Be(7100);
  }

  [Test]
  public async Task TransitOf42KmCostsOneOfFourTariffs()
  {
    var (from, to) = _app.Fixtures.AddressesOf42KmDistance();
    var transit = await _app.Fixtures.ADraftTransitNow(from, to);

    var quoted = await _app.Api.FindTransit(transit);

    ((int)quoted.EstimatedPrice).Should().BeOneOf(5100, 7100, 11500, 15800);
  }

  [Test]
  public async Task TwoTransitsOrderedInTheSameRunCostTheSame()
  {
    var (from, to) = _app.Fixtures.AddressesOf42KmDistance();
    var first = await _app.Fixtures.ADraftTransitNow(from, to);
    var second = await _app.Fixtures.ADraftTransitNow(from, to);

    var firstQuoted = await _app.Api.FindTransit(first);
    var secondQuoted = await _app.Api.FindTransit(second);

    secondQuoted.EstimatedPrice.Should().Be(firstQuoted.EstimatedPrice);
  }

  // ANTI-PATTERN - reflects prod logic in tests
  [Test]
  public async Task TransitOf42KmCostsWhatTheTestItselfComputes()
  {
    var now = SystemClock.Instance.GetCurrentInstant()
      .InZone(DateTimeZoneProviders.Bcl.GetSystemDefault()).LocalDateTime;
    var (kmRate, baseFee) = TariffAt(now);
    var (from, to) = _app.Fixtures.AddressesOf42KmDistance();
    var transit = await _app.Fixtures.ADraftTransitNow(from, to);

    var quoted = await _app.Api.FindTransit(transit);

    ((int)quoted.EstimatedPrice).Should().Be((int)((42 * kmRate + baseFee) * 100));
  }

  private static (float KmRate, int BaseFee) TariffAt(LocalDateTime day)
  {
    if ((day.Month == 12 && day.Day == 31) ||
        (day.Month == 1 && day.Day == 1 && day.Hour <= 6))
    {
      return (3.5f, 11);
    }

    if ((day.DayOfWeek == IsoDayOfWeek.Friday && day.Hour >= 17) ||
        (day.DayOfWeek == IsoDayOfWeek.Saturday && day.Hour <= 6) ||
        (day.DayOfWeek == IsoDayOfWeek.Saturday && day.Hour >= 17) ||
        (day.DayOfWeek == IsoDayOfWeek.Sunday && day.Hour <= 6))
    {
      return (2.5f, 10);
    }

    if ((day.DayOfWeek == IsoDayOfWeek.Saturday && day.Hour > 6 && day.Hour < 17) ||
        (day.DayOfWeek == IsoDayOfWeek.Sunday && day.Hour > 6))
    {
      return (1.5f, 8);
    }

    return (1.0f, 9);
  }
}
