using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace LegacyFighter.CabsTests.Common;

internal class CabsApp : WebApplicationFactory<Program>
{
  private readonly Action<IServiceCollection> _customization;

  private CabsApp(Action<IServiceCollection> customization)
  {
    _customization = customization;
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
}
