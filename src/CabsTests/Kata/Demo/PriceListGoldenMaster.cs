using System.Globalization;
using System.Threading.Tasks;
using LegacyFighter.CabsTests.Common;
using NodaTime;
using static VerifyNUnit.Verifier;

namespace LegacyFighter.CabsTests.Kata.Demo;

public class PriceListGoldenMaster
{
  private static readonly DateTimeZone Warsaw = DateTimeZoneProviders.Tzdb["Europe/Warsaw"];

  private sealed record Day(string Label, LocalDate Date)
  {
    public override string ToString() => Label;
  }

  private static readonly Day[] Days =
  {
    new("workday  Mon 2019-01-07", new LocalDate(2019, 1, 7)),
    new("weekend  Fri 2019-01-11", new LocalDate(2019, 1, 11)),
    new("weekend  Sat 2019-01-12", new LocalDate(2019, 1, 12)),
    new("weekend  Sun 2019-01-13", new LocalDate(2019, 1, 13)),
    new("new year Tue 2019-12-31", new LocalDate(2019, 12, 31)),
    new("new year Wed 2020-01-01", new LocalDate(2020, 1, 1)),
    new("leap day Sat 2020-02-29", new LocalDate(2020, 2, 29)),
    new("old list Fri 2018-06-15", new LocalDate(2018, 6, 15)),
    new("old list Mon 2018-12-31", new LocalDate(2018, 12, 31)),
    // new("leap end Wed 2020-12-30", new LocalDate(2020, 12, 30))
  };

  private static readonly int[] Hours = { 0, 3, 5, 6, 7, 12, 17, 18 };

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
  public Task PriceList()
  {
    return Combination().Verify(Quote, Days, Hours);
  }

  private async Task<string> Quote(Day day, int hour)
  {
    var when = day.Date.At(new LocalTime(hour, 0)).InZoneStrictly(Warsaw).ToInstant();
    var (from, to) = _app.Fixtures.AddressesOf42KmDistance();
    var transit = await _app.Fixtures.ADraftTransitAt(when, from, to);
    var quoted = await _app.Api.FindTransit(transit);
    return string.Format(
      CultureInfo.InvariantCulture,
      "{0} gr | {1} | {2:0.00}/km",
      quoted.EstimatedPrice, quoted.Tariff, quoted.KmRate);
  }
}
