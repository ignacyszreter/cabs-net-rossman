using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using LegacyFighter.Cabs.Claims;
using LegacyFighter.Cabs.Claims.Storage;
using LegacyFighter.Cabs.Claims.Strangler;
using LegacyFighter.Cabs.Dto;
using LegacyFighter.Cabs.Entity;
using LegacyFighter.CabsTests.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NodaTime;
using NUnit.Framework;

namespace LegacyFighter.CabsTests.Claims;

public class ClaimsSliceTest
{
  private static readonly DateTimeZone Warsaw = DateTimeZoneProviders.Tzdb["Europe/Warsaw"];

  private static readonly Instant When =
    new LocalDateTime(2026, 9, 16, 12, 0).InZoneStrictly(Warsaw).ToInstant();

  private CabsApp _app = default!;
  private HttpClient _http = default!;
  private long _driverId;

  [TearDown]
  public void TearDown()
  {
    _http.Dispose();
    _app.Dispose();
  }

  [Test]
  public async Task LeavesTheClaimWithTheOldCodeWhenTheSliceIsOff()
  {
    await AnAppWithTheSlice(false);
    var claimId = await AClaimOf(Client.Types.Normal);

    Assert.That(await Resolve(claimId), Is.EqualTo(ClaimStatus.Refunded));

    Assert.That((await Claim(claimId))!.Status, Is.EqualTo(ClaimStatus.New));
  }

  [Test]
  public async Task SendsTheClaimOfARegularClientToTheModuleWhenTheSliceIsOn()
  {
    await AnAppWithTheSlice(true);
    var claimId = await AClaimOf(Client.Types.Normal);

    Assert.That(await Resolve(claimId), Is.EqualTo(ClaimStatus.Refunded));

    Assert.That((await Claim(claimId))!.Status, Is.EqualTo(ClaimStatus.Refunded));
  }

  [Test]
  public async Task LeavesTheClaimOfAVipWithTheOldCodeWhenTheSliceIsOn()
  {
    await AnAppWithTheSlice(true);
    var claimId = await AClaimOf(Client.Types.Vip);

    await Resolve(claimId);

    Assert.That((await Claim(claimId))!.Status, Is.EqualTo(ClaimStatus.New));
  }

  private async Task AnAppWithTheSlice(bool on)
  {
    _app = CabsApp.CreateInstance(services =>
    {
      services.RemoveAll<StranglerFlags>();
      services.AddSingleton(new StranglerFlags { ResolveClaimsOfRegularClients = on });
    });
    _http = _app.CreateClient();
    _driverId = await _app.Fixtures.ADriverOnDuty("WU1212");
  }

  private async Task<ClaimStatus?> Resolve(long claimId)
  {
    var response = await _http.PostAsync($"/claims/{claimId}", null);
    return (await response.Content.ReadFromJsonAsync<ResolvedClaim>())!.Status;
  }

  private async Task<long> AClaimOf(Client.Types type)
  {
    var clientId = await AClient(type);
    var transitId = await ATransitFor(clientId);
    var response = await _http.PostAsJsonAsync(
      "/claims/send",
      new { clientId, transitId, reason = "za drogo", incidentDescription = "trasa dłuższa o 3 km" });
    return (await response.Content.ReadFromJsonAsync<ClaimDto>())!.ClaimId!.Value;
  }

  private async Task<long> AClient(Client.Types type)
  {
    var response = await _http.PostAsJsonAsync("/clients", new ClientDto
    {
      Name = "Jan",
      LastName = "Kowalski",
      Type = type,
      DefaultPaymentType = Client.PaymentTypes.MonthlyInvoice
    });
    return (await response.Content.ReadFromJsonAsync<ClientDto>())!.Id!.Value;
  }

  private Task<long> ATransitFor(long clientId)
  {
    return _app.Fixtures.ACompletedTransitFor(clientId, _driverId, When);
  }

  private async Task<ClaimRecord?> Claim(long claimId)
  {
    using var scope = _app.Services.CreateScope();
    return await scope.ServiceProvider.GetRequiredService<ClaimsDbContext>().Claims.FindAsync(claimId);
  }

  private sealed class ResolvedClaim
  {
    public ClaimStatus? Status { get; set; }
  }
}
