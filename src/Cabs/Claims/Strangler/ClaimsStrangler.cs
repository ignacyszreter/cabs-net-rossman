using LegacyFighter.Cabs.Claims.Storage;

namespace LegacyFighter.Cabs.Claims.Strangler;

public class StranglerFlags
{
  public bool ResolveClaimsOfRegularClients { get; set; }
}

public static class ClaimsStrangler
{
  // Rewrites the path before routing, so the old controller never learns about the new module.
  public static IApplicationBuilder UseClaimsStrangler(this IApplicationBuilder app)
  {
    return app.Use(async (context, next) =>
    {
      if (await GoesToModule(context))
      {
        context.Request.Path = "/bubble" + context.Request.Path;
      }

      await next();
    });
  }

  private static async Task<bool> GoesToModule(HttpContext context)
  {
    if (!context.RequestServices.GetRequiredService<StranglerFlags>().ResolveClaimsOfRegularClients)
    {
      return false;
    }

    if (!ResolutionOfClaim(context, out var claimId))
    {
      return false;
    }

    var claims = context.RequestServices.GetRequiredService<ClaimsDbContext>();
    var claim = await claims.Claims.FindAsync(claimId);
    if (claim is null)
    {
      return false;
    }

    var claimant = await claims.Claimants.FindAsync(claim.ClaimantId);
    return claimant is { IsVip: false };
  }

  private static bool ResolutionOfClaim(HttpContext context, out long claimId)
  {
    claimId = 0;
    return context.Request.Method == HttpMethods.Post
           && context.Request.Path.StartsWithSegments("/claims", out var rest)
           && long.TryParse(rest.Value?.TrimStart('/'), out claimId);
  }
}
