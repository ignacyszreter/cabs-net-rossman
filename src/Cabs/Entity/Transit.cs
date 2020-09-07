using LegacyFighter.Cabs.Common;
using NodaTime;

namespace LegacyFighter.Cabs.Entity;

public class Transit : BaseEntity
{


  public Transit()
  {
  }

  public enum Statuses
  {
    Draft,
    Completed
  }

  public Instant? Date { get; private set; }
  public const int BaseFee = 9;

  public virtual Driver Driver { get; set; }

  // https://stackoverflow.com/questions/37107123/sould-i-store-price-as-decimal-or-integer-in-mysql
  public int? Price
  {
    get;
    set; //just for testing
  }

  public Statuses? Status { get; set; }

  public Instant? CompleteAt { get; private set; }

  public virtual Client Client { get; set; }

  public int CalculateFinalCosts()
  {
    if (Status == Statuses.Completed)
    {
      return CalculateCost();
    }
    else
    {
      throw new InvalidOperationException("Cannot calculate final cost if the transit is not completed");
    }
  }

  private int CalculateCost()
  {
    var baseFee = BaseFee;
    float kmRate = 1.0f;

    var finalPrice = (int) Math.Round(Km * kmRate + baseFee);
    Price = finalPrice;
    return finalPrice;
  }

  public Instant? DateTime { set; get; }

  public float Km { get; set; }

  public virtual Address From { get; set; }
  public virtual Address To { get; set; }

  public override bool Equals(object obj)
  {
    if (ReferenceEquals(this, obj)) return true;
    return obj != null && Id != null && Id == (obj as Transit)?.Id;
  }

  public static bool operator ==(Transit left, Transit right)
  {
    return Equals(left, right);
  }

  public static bool operator !=(Transit left, Transit right)
  {
    return !Equals(left, right);
  }

  public void CompleteTransitAt(Instant when)
  {
    CompleteAt = when;
  }

}