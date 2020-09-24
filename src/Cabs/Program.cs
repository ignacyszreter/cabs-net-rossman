using LegacyFighter.Cabs.Repository;
using LegacyFighter.Cabs.Service;
using NodaTime;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddSingleton(_ => SqLiteDbContext.CreateInMemoryDatabase());
builder.Services.AddDbContext<SqLiteDbContext>();
builder.Services.AddTransient<IAddressRepositoryInterface, EfCoreAddressRepository>();
builder.Services.AddTransient<IDriverRepository, EfCoreDriverRepository>();
builder.Services.AddTransient<IDriverSessionRepository, EfCoreDriverSessionRepository>();
builder.Services.AddTransient<IClientRepository, EfCoreClientRepository>();
builder.Services.AddTransient<ITransitRepository, EfCoreTransitRepository>();
builder.Services.AddTransient<IClientService, ClientService>();
builder.Services.AddTransient<IDriverService, DriverService>();
builder.Services.AddTransient<IDriverSessionService, DriverSessionService>();
builder.Services.AddTransient<IGeocodingService, GeocodingService>();
builder.Services.AddTransient<ITransitService, TransitService>();
builder.Services.AddTransient<DistanceCalculator>();
builder.Services.AddSingleton<IClock>(_ => SystemClock.Instance);
builder.Services.AddTransient<AddressRepository>();
builder.Services.AddControllers().AddControllersAsServices();

var app = builder.Build();

using (var serviceScope = app.Services.CreateScope())
{
  var context = serviceScope.ServiceProvider.GetRequiredService<SqLiteDbContext>();
  await context.Database.EnsureCreatedAsync();
}

// Configure the HTTP request pipeline.

app.UseAuthorization();

app.MapControllers();

app.Run();