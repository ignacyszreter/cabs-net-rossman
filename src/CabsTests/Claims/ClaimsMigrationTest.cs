using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using LegacyFighter.Cabs.Claims;
using LegacyFighter.Cabs.Claims.Storage;
using LegacyFighter.Cabs.Claims.Sync;
using LegacyFighter.Cabs.Dto;
using LegacyFighter.Cabs.Entity;
using LegacyFighter.CabsTests.Common;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NodaTime;
using NUnit.Framework;

namespace LegacyFighter.CabsTests.Claims;

public class ClaimsMigrationTest
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
    // Everything here happens before the module starts listening, so the app announces nothing.
    _app = CabsApp.CreateInstance(services =>
    {
      services.RemoveAll<IClaimsEvents>();
      services.AddSingleton<IClaimsEvents>(new NobodyListens());
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
  public async Task MovesAVipTheModuleNeverHeardAbout()
  {
    var clientId = await AVipNobodyAnnounced();

    await Migrate();

    Assert.That((await Claimant(clientId))?.IsVip, Is.True);
  }

  [Test]
  public async Task MovesAnOldClaimIntoTheModuleDatabase()
  {
    var claimId = await AClaimNobodyAnnounced();

    await Migrate();

    var view = await _http.GetFromJsonAsync<ClaimView>($"/bubble/claims/{claimId}");
    Assert.That(view!.ClaimId, Is.EqualTo(claimId));
    Assert.That(view.Status, Is.EqualTo(ClaimStatus.New));
    Assert.That(view.Reason, Is.EqualTo("za drogo"));
  }

  [Test]
  public async Task RunsAgainWithoutMovingAnything()
  {
    await AClaimNobodyAnnounced();
    await Migrate();

    Assert.That(await Migrate(), Is.EqualTo(0));
    Assert.That(await CopiedClaimants(), Is.EqualTo(1));
    Assert.That(await CopiedClaims(), Is.EqualTo(1));
  }

  private async Task<int> Migrate()
  {
    using var scope = _app.Services.CreateScope();
    return await scope.ServiceProvider.GetRequiredService<ClaimsMigration>().Run();
  }

  private async Task<ClaimantRecord?> Claimant(long clientId)
  {
    using var scope = _app.Services.CreateScope();
    return await scope.ServiceProvider.GetRequiredService<ClaimsDbContext>().Claimants.FindAsync(clientId);
  }

  private async Task<int> CopiedClaimants()
  {
    using var scope = _app.Services.CreateScope();
    return scope.ServiceProvider.GetRequiredService<ClaimsDbContext>().Claimants.Count();
  }

  private async Task<int> CopiedClaims()
  {
    using var scope = _app.Services.CreateScope();
    return scope.ServiceProvider.GetRequiredService<ClaimsDbContext>().Claims.Count();
  }

  private async Task<long> AVipNobodyAnnounced()
  {
    return await AClient(Client.Types.Vip);
  }

  private async Task<long> AClaimNobodyAnnounced()
  {
    var clientId = await AClient(Client.Types.Normal);
    var transitId = await _app.Fixtures.ACompletedTransitFor(clientId, _driverId, When);
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

  private sealed class NobodyListens : IClaimsEvents
  {
    public Task Publish(INotification @event)
    {
      return Task.CompletedTask;
    }
  }
}
