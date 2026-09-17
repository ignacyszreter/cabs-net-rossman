using System.Net.Http;
using LegacyFighter.CabsTests.Common;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;

namespace LegacyFighter.CabsTests.Integration;

public class SettlementWorkstationTest
{
  [Test]
  [Category("SqlServer")]
  public async Task SqlServerAnswers()
  {
    await using var connection = new SqlConnection(SqlServer.ConnectionString);
    await connection.OpenAsync();
    await using var command = connection.CreateCommand();
    command.CommandText = "SELECT 1";

    (await command.ExecuteScalarAsync()).Should().Be(1);
  }

  [Test]
  public async Task NbpAnswersWithEurRate()
  {
    var json = await Get("Nbp", "exchangerates/rates/a/eur/2025-12-20/2025-12-31/?format=json");

    json.Should().Contain("\"mid\"");
  }

  [Test]
  public async Task NagerDateAnswersWithPolishHolidays()
  {
    var json = await Get("PublicHolidays", "PublicHolidays/2026/PL");

    json.Should().Contain("\"date\"");
  }

  private static async Task<string> Get(string client, string path)
  {
    await using var app = CabsApp.CreateInstance();
    var http = app.Services.GetRequiredService<IHttpClientFactory>().CreateClient(client);
    return await http.GetStringAsync(path);
  }
}
