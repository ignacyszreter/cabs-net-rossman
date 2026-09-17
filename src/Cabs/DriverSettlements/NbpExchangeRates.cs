using System.Text.Json;

namespace LegacyFighter.Cabs.DriverSettlements;

public class NbpExchangeRates : IExchangeRates
{
  private readonly IHttpClientFactory _httpClientFactory;

  public NbpExchangeRates(IHttpClientFactory httpClientFactory)
  {
    _httpClientFactory = httpClientFactory;
  }

  public async Task<decimal> EurRateAtEndOf(int year)
  {
    var nbp = _httpClientFactory.CreateClient("Nbp");
    var ratesJson = await nbp.GetStringAsync($"exchangerates/rates/a/eur/{year}-12-20/{year}-12-31/?format=json");
    using var rates = JsonDocument.Parse(ratesJson);
    return rates.RootElement.GetProperty("rates").EnumerateArray().Last().GetProperty("mid").GetDecimal();
  }
}
