using System;
using LegacyFighter.Cabs.Service;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace LegacyFighter.CabsTests.Common;

internal class CabsApp : WebApplicationFactory<Program>
{
  private readonly Action<IServiceCollection> _customization;
  private IServiceScope _scope;
  private CabsApi? _api;
  private Fixtures? _fixtures;

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

  public IClientService ClientService
    => NewRequestScope().ServiceProvider.GetRequiredService<IClientService>();

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

  public CabsApi Api => _api ??= new CabsApi(this);

  public Fixtures Fixtures => _fixtures ??= new Fixtures(Api, Services);
}
