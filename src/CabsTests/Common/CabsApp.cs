using System;
using LegacyFighter.Cabs.Controllers;
using LegacyFighter.Cabs.Service;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NodaTime;
using NodaTime.Testing;

namespace LegacyFighter.CabsTests.Common;

internal class CabsApp : WebApplicationFactory<Program>
{
  private readonly Action<IServiceCollection> _customization;
  private readonly FakeClock _clock = new(SystemClock.Instance.GetCurrentInstant());
  private IServiceScope _scope;
  private CabsApi? _api;

  private CabsApp(Action<IServiceCollection> customization)
  {
    _customization = customization;
    _scope = base.Services.CreateAsyncScope();
  }

  public static CabsApp CreateInstance()
  {
    return new CabsApp(_ => { });
  }

  public static CabsApp CreateInstance(Action<IServiceCollection> customization)
  {
    return new CabsApp(customization);
  }

  protected override void ConfigureWebHost(IWebHostBuilder builder)
  {
    builder.ConfigureServices(collection =>
    {
      collection.RemoveAll<IClock>();
      collection.AddSingleton<IClock>(_clock);
      collection.AddTransient<Fixtures>();
    });
    builder.ConfigureServices(_customization);
  }

  protected override void Dispose(bool disposing)
  {
    _scope.Dispose();
    base.Dispose(disposing);
  }

  private IServiceScope NewRequestScope()
  {
    _scope.Dispose();
    _scope = Services.CreateAsyncScope();
    return _scope;
  }

  public FakeClock Clock => _clock;

  public IClientService ClientService
    => NewRequestScope().ServiceProvider.GetRequiredService<IClientService>();

  public IDriverFeeService DriverFeeService
    => NewRequestScope().ServiceProvider.GetRequiredService<IDriverFeeService>();

  public IDriverService DriverService
    => NewRequestScope().ServiceProvider.GetRequiredService<IDriverService>();

  public IDriverSessionService DriverSessionService
    => NewRequestScope().ServiceProvider.GetRequiredService<IDriverSessionService>();

  public IDriverTrackingService DriverTrackingService
    => NewRequestScope().ServiceProvider.GetRequiredService<IDriverTrackingService>();

  public ICarTypeService CarTypeService
    => NewRequestScope().ServiceProvider.GetRequiredService<ICarTypeService>();

  public ITransitService TransitService
    => NewRequestScope().ServiceProvider.GetRequiredService<ITransitService>();

  public TransitController TransitController
    => NewRequestScope().ServiceProvider.GetRequiredService<TransitController>();

  public CabsApi Api => _api ??= new CabsApi(this);

  public Fixtures Fixtures
    => NewRequestScope().ServiceProvider.GetRequiredService<Fixtures>().WithApi(Api, _clock, Services);
}
