using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using LegacyFighter.CabsTests.Common;
using NodaTime;
using NUnit.Framework;

namespace LegacyFighter.CabsTests.Claims;

public class ClaimsControllerTest
{
  private const string Claims = "/bubble/claims";
  private const long Unknown = 999_999;

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
  public async Task RegistersAClaimOnTheClientsTransit()
  {
    var client = await AClient();
    var transit = await ATransitFor(client);

    var claim = await Register(client, transit);

    Assert.That(claim, Is.EqualTo(new RegisteredClaim(
      claim.ClaimId, claim.ClaimNo, client, transit, "za drogo", "trasa dłuższa o 3 km", false)));
  }

  [Test]
  public async Task GivesEveryClaimItsOwnNumber()
  {
    var client = await AClient();

    var first = await Register(client, await ATransitFor(client));
    var second = await Register(client, await ATransitFor(client));

    Assert.That(second.ClaimNo, Is.Not.Empty.And.Not.EqualTo(first.ClaimNo));
  }

  [Test]
  public async Task ShowsARegisteredClaim()
  {
    var client = await AClient();
    var registered = await Register(client, await ATransitFor(client));

    var shown = await Show(registered.ClaimId);

    Assert.That(shown, Is.EqualTo(registered));
  }

  [Test]
  public async Task RejectsAClaimOfAnUnknownClient()
  {
    var client = await AClient();
    var transit = await ATransitFor(client);

    Assert.That(async () => await Register(Unknown, transit), Throws.InvalidOperationException.With.Message.Contains("Client does not exists"));
  }

  [Test]
  public async Task RejectsAClaimOnAnUnknownTransit()
  {
    var client = await AClient();

    Assert.That(async () => await Register(client, Unknown), Throws.InvalidOperationException.With.Message.Contains("Transit does not exists"));
  }

  [Test]
  public void RejectsShowingAnUnknownClaim()
  {
    Assert.That(async () => await Show(Unknown), Throws.InvalidOperationException.With.Message.Contains("Claim does not exists"));
  }

  private sealed record RegisteredClaim(
    long ClaimId,
    string ClaimNo,
    long ClientId,
    long TransitId,
    string Reason,
    string IncidentDescription,
    bool IsDraft);

  private async Task<RegisteredClaim> Register(long clientId, long transitId)
  {
    var response = await Ok(await _http.PostAsJsonAsync(
      $"{Claims}/send",
      new { clientId, transitId, reason = "za drogo", incidentDescription = "trasa dłuższa o 3 km" }));
    return (await response.Content.ReadFromJsonAsync<RegisteredClaim>())!;
  }

  private async Task<RegisteredClaim> Show(long claimId)
  {
    var response = await Ok(await _http.GetAsync($"{Claims}/{claimId}"));
    return (await response.Content.ReadFromJsonAsync<RegisteredClaim>())!;
  }

  private Task<long> AClient()
  {
    return _app.Fixtures.ARegisteredClient();
  }

  private Task<long> ATransitFor(long clientId)
  {
    return _app.Fixtures.ACompletedTransitFor(clientId, _driverId, When);
  }

  private static async Task<HttpResponseMessage> Ok(HttpResponseMessage response)
  {
    if (!response.IsSuccessStatusCode)
    {
      throw new InvalidOperationException(
        $"{(int)response.StatusCode} {response.RequestMessage?.RequestUri}: {await response.Content.ReadAsStringAsync()}");
    }

    return response;
  }
}
