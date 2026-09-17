using System.Globalization;
using System.Net.Mail;
using System.Text;
using LegacyFighter.Cabs.DriverSettlements;
using LegacyFighter.Cabs.Entity;
using LegacyFighter.Cabs.Repository;

namespace LegacyFighter.Cabs.Service;

public class DriverSettlementJob : BackgroundService
{
  private const string AccountingEmail = "ksiegowosc@cabs.pl";
  private static DateTime? _lastRun;

  private readonly IServiceProvider _serviceProvider;
  private readonly IConfiguration _configuration;

  public DriverSettlementJob(IServiceProvider serviceProvider, IConfiguration configuration)
  {
    _serviceProvider = serviceProvider;
    _configuration = configuration;
  }

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    while (!stoppingToken.IsCancellationRequested)
    {
      var now = DateTime.Now;
      if (now.Month == 1 && now.Day == 2 && now.Hour == 6 && _lastRun?.Date != now.Date)
      {
        SettleAllDrivers();
        _lastRun = now;
      }

      await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
    }
  }

  public void SettleAllDrivers()
  {
    var year = DateTime.Now.Year - 1;
    using var scope = _serviceProvider.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<SqLiteDbContext>();
    var driverSettlement = scope.ServiceProvider.GetRequiredService<IDriverSettlement>();
    var pl = new CultureInfo("pl-PL");

    var drivers = dbContext.Drivers.Where(d => d.Status == Driver.Statuses.Active).ToList();

    var body = new StringBuilder();
    body.Append("<h2>Rozliczenia kierowców za " + year + "</h2>");
    body.Append("<table border=\"1\">");
    body.Append("<tr><th>Kierowca</th><th>Suma PLN</th><th>Kurs EUR</th><th>Suma EUR</th><th>Data wypłaty</th></tr>");
    var total = 0;
    foreach (var driver in drivers)
    {
      try
      {
        var settlement = driverSettlement.Settle(driver.Id.Value, year).Result;
        body.Append("<tr>");
        body.Append("<td>" + driver.FirstName + " " + driver.LastName + "</td>");
        body.Append("<td>" + settlement.Total.ToString("N0", pl) + " zł</td>");
        body.Append("<td>" + settlement.EurRate.ToString(pl) + "</td>");
        body.Append("<td>" + settlement.TotalInEur.ToString("N2", pl) + " €</td>");
        body.Append("<td>" + settlement.PayoutDate.ToString("dd.MM.yyyy") + "</td>");
        body.Append("</tr>");
        total += settlement.Total;
      }
      catch (Exception e)
      {
        Console.WriteLine("Blad rozliczenia kierowcy " + driver.Id + ": " + e.Message);
      }

      Thread.Sleep(200);
    }

    body.Append("</table>");
    body.Append("<p>Razem do wypłaty: " + total.ToString("N0", pl) + " zł</p>");

    var pickupDirectory = _configuration["Mail:PickupDirectory"] ?? Path.Combine(Path.GetTempPath(), "cabs-mail");
    Directory.CreateDirectory(pickupDirectory);
    using var smtp = new SmtpClient
    {
      DeliveryMethod = SmtpDeliveryMethod.SpecifiedPickupDirectory,
      PickupDirectoryLocation = pickupDirectory
    };
    using var message = new MailMessage("noreply@cabs.pl", AccountingEmail)
    {
      Subject = "Rozliczenia kierowców za " + year,
      Body = body.ToString(),
      IsBodyHtml = true
    };
    smtp.Send(message);
  }
}
