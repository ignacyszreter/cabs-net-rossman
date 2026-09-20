using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using LegacyFighter.Cabs.Claims;
using LegacyFighter.Cabs.Claims.Storage;
using LegacyFighter.Cabs.Dto;
using LegacyFighter.Cabs.Entity;
using LegacyFighter.CabsTests.Common;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using NUnit.Framework;

namespace LegacyFighter.CabsTests.Claims;

public class ClaimsCopyTest
{
  private static readonly DateTimeZone Warsaw = DateTimeZoneProviders.Tzdb["Europe/Warsaw"];

  private static readonly Instant When =
    new LocalDateTime(2026, 9, 16, 12, 0).InZoneStrictly(Warsaw).ToInstant();

  private CabsApp _app = default!;
  private HttpClient _http = default!;
  private long _driverId;

  [SetUp]
  public async Task Start()
  {
    _app = CabsApp.CreateInstance();
    _http = _app.CreateClient();
    _driverId = await _app.Fixtures.ADriverOnDuty("WU1212");
  }

  [TearDown]
  public void TearDown()
  {
    _http.Dispose();
    _app.Dispose();
  }

  [Test]
  public async Task LearnsThatAClientBecameVip()
  {
    var clientId = await ARegularClient();

    await _http.PostAsync($"/clients/{clientId}/upgrade", null);

    Assert.That((await Claimant(clientId))?.IsVip, Is.True);
  }

  [Test]
  public async Task LearnsAboutAClaimRegisteredTheOldWay()
  {
    var clientId = await ARegularClient();
    var transitId = await ATransitFor(clientId);

    var claimId = await SendClaim(clientId, transitId);

    var claim = await Claim(claimId);
    Assert.That(claim?.ClaimantId, Is.EqualTo(clientId));
    Assert.That(claim?.TransitId, Is.EqualTo(transitId));
    Assert.That(claim?.Status, Is.EqualTo(ClaimStatus.New));
  }

  private async Task<long> ARegularClient()
  {
    var response = await _http.PostAsJsonAsync("/clients", new ClientDto
    {
      Name = "Jan",
      LastName = "Kowalski",
      Type = Client.Types.Normal,
      DefaultPaymentType = Client.PaymentTypes.MonthlyInvoice
    });
    return (await response.Content.ReadFromJsonAsync<ClientDto>())!.Id!.Value;
  }

  private Task<long> ATransitFor(long clientId)
  {
    return _app.Fixtures.ACompletedTransitFor(clientId, _driverId, When);
  }

  private async Task<long> SendClaim(long clientId, long transitId)
  {
    var response = await _http.PostAsJsonAsync(
      "/claims/send",
      new { clientId, transitId, reason = "za drogo", incidentDescription = "trasa dłuższa o 3 km" });
    return (await response.Content.ReadFromJsonAsync<ClaimDto>())!.ClaimId!.Value;
  }

  private async Task<ClaimantRecord?> Claimant(long clientId)
  {
    using var scope = _app.Services.CreateScope();
    return await scope.ServiceProvider.GetRequiredService<ClaimsDbContext>().Claimants.FindAsync(clientId);
  }

  private async Task<ClaimRecord?> Claim(long claimId)
  {
    using var scope = _app.Services.CreateScope();
    return await scope.ServiceProvider.GetRequiredService<ClaimsDbContext>().Claims.FindAsync(claimId);
  }
}
