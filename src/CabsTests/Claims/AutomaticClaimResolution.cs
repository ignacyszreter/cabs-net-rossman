using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using LegacyFighter.Cabs.Config;
using LegacyFighter.Cabs.Entity;
using LegacyFighter.CabsTests.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NodaTime;
using NUnit.Framework;

namespace LegacyFighter.CabsTests.Claims;

[TestFixture("/claims")]
[TestFixture("/bubble/claims")]
public class AutomaticClaimResolution
{
  private const int RefundThreshold = 10000;
  private const int TransitsForAutomaticRefund = 3;

  private static readonly DateTimeZone Warsaw = DateTimeZoneProviders.Tzdb["Europe/Warsaw"];

  // The hour of the order picks the tariff, and the tariff is what makes the fare cheap or expensive:
  // 42 km on Standard costs 5100, on Weekend+ 11500.
  private static readonly Instant WednesdayNoon =
    new LocalDateTime(2026, 9, 16, 12, 0).InZoneStrictly(Warsaw).ToInstant();

  private static readonly Instant FridayEvening =
    new LocalDateTime(2026, 9, 18, 18, 0).InZoneStrictly(Warsaw).ToInstant();

  private readonly string _claims;

  private CabsApp _app = default!;
  private HttpClient _http = default!;
  private long _driverId;

  public AutomaticClaimResolution(string claims)
  {
    _claims = claims;
  }

  [SetUp]
  public async Task Start()
  {
    // Cabs has no entry for the claim thresholds, so the test replaces the whole IAppProperties.
    _app = CabsApp.CreateInstance(services =>
    {
      services.RemoveAll<IAppProperties>();
      services.AddSingleton<IAppProperties>(new AppProperties
      {
        AutomaticRefundForVipThreshold = RefundThreshold,
        NoOfTransitsForClaimAutomaticRefund = TransitsForAutomaticRefund
      });
    });
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
  public async Task EscalatesASecondClaimOnTheSameTransit()
  {
    var claimant = await AClaimant(Client.Types.Normal);
    var transit = await ACheapTransitFor(claimant);
    await SendClaim(claimant, transit);

    var outcome = await ResolveNewClaim(claimant, transit);

    Assert.That(outcome, Is.EqualTo(Escalated));
  }

  [Test]
  public async Task RefundsTheThirdClaimWhateverTheFare()
  {
    var claimant = await AClaimant(Client.Types.Normal);
    await ClaimsOnNewTransits(claimant, 2);
    var transit = await AnExpensiveTransitFor(claimant);

    var outcome = await ResolveNewClaim(claimant, transit);

    Assert.That(outcome, Is.EqualTo(Refunded()));
  }

  [Test]
  public async Task RefundsAFrequentVipOnACheapTransitWithTenMiles()
  {
    var claimant = await AClaimant(Client.Types.Vip);
    await ClaimsOnNewTransits(claimant, 3);
    var transit = await ACheapTransitFor(claimant);

    var outcome = await ResolveNewClaim(claimant, transit);

    Assert.That(outcome, Is.EqualTo(Refunded(miles: 10)));
  }

  [Test]
  public async Task EscalatesAFrequentVipOnAnExpensiveTransit()
  {
    var claimant = await AClaimant(Client.Types.Vip);
    await ClaimsOnNewTransits(claimant, 3);
    var transit = await AnExpensiveTransitFor(claimant);

    var outcome = await ResolveNewClaim(claimant, transit);

    Assert.That(outcome, Is.EqualTo(Escalated));
  }

  [Test]
  public async Task RefundsAFrequentClaimantWithThreeOrderedTransitsOnACheapTransit()
  {
    var claimant = await AClaimant(Client.Types.Normal);
    var twiceClaimed = await ACheapTransitFor(claimant);
    await SendClaims(claimant, twiceClaimed, 2);
    await ClaimsOnNewTransits(claimant, 1);
    var transit = await ACheapTransitFor(claimant);

    var outcome = await ResolveNewClaim(claimant, transit);

    Assert.That(outcome, Is.EqualTo(Refunded()));
  }

  [Test]
  public async Task EscalatesAFrequentClaimantWithThreeOrderedTransitsOnAnExpensiveTransit()
  {
    var claimant = await AClaimant(Client.Types.Normal);
    var twiceClaimed = await ACheapTransitFor(claimant);
    await SendClaims(claimant, twiceClaimed, 2);
    await ClaimsOnNewTransits(claimant, 1);
    var transit = await AnExpensiveTransitFor(claimant);

    var outcome = await ResolveNewClaim(claimant, transit);

    Assert.That(outcome, Is.EqualTo(Escalated));
  }

  [Test]
  public async Task EscalatesAFrequentClaimantWithTwoOrderedTransits()
  {
    var claimant = await AClaimant(Client.Types.Normal);
    var other = await ACheapTransitFor(claimant);
    await SendClaims(claimant, other, 3);
    var transit = await ACheapTransitFor(claimant);

    var outcome = await ResolveNewClaim(claimant, transit);

    Assert.That(outcome, Is.EqualTo(Escalated));
  }

  private sealed record Outcome(Claim.Statuses? Status, Claim.CompletionModes? CompletionMode, int AddedMiles);

  private static Outcome Refunded(int miles = 0) =>
    new(Claim.Statuses.Refunded, Claim.CompletionModes.Automatic, miles);

  private static readonly Outcome Escalated =
    new(Claim.Statuses.Escalated, Claim.CompletionModes.Manual, 0);

  private async Task<Outcome> ResolveNewClaim(long claimantId, long transitId)
  {
    var claim = await SendClaim(claimantId, transitId);
    var milesBefore = await AwardsBalance(claimantId);
    var response = await Ok(await _http.PostAsync($"{_claims}/{claim.ClaimId}", null));
    var resolved = (await response.Content.ReadFromJsonAsync<ClaimView>())!;
    return new Outcome(resolved.Status, resolved.CompletionMode, await AwardsBalance(claimantId) - milesBefore);
  }

  private async Task ClaimsOnNewTransits(long claimantId, int count)
  {
    for (var claim = 0; claim < count; claim++)
    {
      await SendClaim(claimantId, await ACheapTransitFor(claimantId));
    }
  }

  private async Task SendClaims(long claimantId, long transitId, int count)
  {
    for (var claim = 0; claim < count; claim++)
    {
      await SendClaim(claimantId, transitId);
    }
  }

  private async Task<long> AClaimant(Client.Types type)
  {
    var clientId = await _app.Fixtures.ARegisteredClient();
    if (type == Client.Types.Vip)
    {
      await Ok(await _http.PostAsync($"/clients/{clientId}/upgrade", null));
    }

    await Ok(await _http.PostAsync($"/clients/{clientId}/awards", null));
    await Ok(await _http.PostAsync($"/clients/{clientId}/awards/activate", null));
    return clientId;
  }

  private Task<long> ACheapTransitFor(long clientId)
  {
    return _app.Fixtures.ACompletedTransitFor(clientId, _driverId, WednesdayNoon);
  }

  private Task<long> AnExpensiveTransitFor(long clientId)
  {
    return _app.Fixtures.ACompletedTransitFor(clientId, _driverId, FridayEvening);
  }

  private async Task<ClaimView> SendClaim(long clientId, long transitId)
  {
    var response = await Ok(await _http.PostAsJsonAsync(
      $"{_claims}/send",
      new { clientId, transitId, reason = "za drogo", incidentDescription = "za drogo" }));
    return (await response.Content.ReadFromJsonAsync<ClaimView>())!;
  }

  private async Task<int> AwardsBalance(long clientId)
  {
    var response = await Ok(await _http.GetAsync($"/clients/{clientId}/awards/balance"));
    return await response.Content.ReadFromJsonAsync<int>();
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

  private sealed class ClaimView
  {
    public long ClaimId { get; set; }
    public Claim.Statuses? Status { get; set; }
    public Claim.CompletionModes? CompletionMode { get; set; }
  }
}
