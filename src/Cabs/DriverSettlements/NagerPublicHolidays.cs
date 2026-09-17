using System.Globalization;
using System.Text.Json;

namespace LegacyFighter.Cabs.DriverSettlements;

public class NagerPublicHolidays : IPublicHolidays
{
  private readonly IHttpClientFactory _httpClientFactory;

  public NagerPublicHolidays(IHttpClientFactory httpClientFactory)
  {
    _httpClientFactory = httpClientFactory;
  }

  public async Task<IReadOnlySet<DateOnly>> In(int year)
  {
    var nager = _httpClientFactory.CreateClient("PublicHolidays");
    var holidaysJson = await nager.GetStringAsync($"PublicHolidays/{year}/PL");
    using var holidays = JsonDocument.Parse(holidaysJson);
    return holidays.RootElement.EnumerateArray()
      .Select(h => DateOnly.ParseExact(h.GetProperty("date").GetString()!, "yyyy-MM-dd", CultureInfo.InvariantCulture))
      .ToHashSet();
  }
}
