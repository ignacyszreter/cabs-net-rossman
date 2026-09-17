using System;
using Testcontainers.MsSql;

namespace LegacyFighter.CabsTests.Common;

internal static class SqlServer
{
  private static readonly Lazy<MsSqlContainer> Container = new(Start);

  public static string ConnectionString
    => Environment.GetEnvironmentVariable("CABS_SQLSERVER") ?? Container.Value.GetConnectionString();

  private static MsSqlContainer Start()
  {
    var container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
      .WithCreateParameterModifier(parameters => parameters.Platform = "linux/amd64")
      .WithReuse(true)
      .Build();
    container.StartAsync().GetAwaiter().GetResult();
    return container;
  }
}
