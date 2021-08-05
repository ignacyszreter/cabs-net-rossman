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
    Cancelled,
    WaitingForDriverAssignment,
    DriverAssignmentFailed,
    TransitToPassenger,
    InTransit,
    Completed
  }

  public Instant? Date { get; private set; }
  private float _km;
  public const int BaseFee = 9;

  public CarType.CarClasses? CarType { get; set; }
  public virtual Driver Driver { get; set; }

  // https://stackoverflow.com/questions/37107123/sould-i-store-price-as-decimal-or-integer-in-mysql
  public int? Price
  {
    get;
    set; //just for testing
  }

  public Statuses? Status { get; set; }

  public Instant? CompleteAt { get; private set; }

  public int EstimateCost()
  {
    var estimated = CalculateCost();

    EstimatedPrice = estimated;

    return estimated;
  }

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

    var finalPrice = (int) Math.Round(_km * kmRate + baseFee);
    Price = finalPrice;
    return finalPrice;
  }

  public Instant? DateTime { set; get; }

  public Instant? Published { get; set; }

  public float Km 
  {
    get => _km;
    set
    {
      _km = value;
      EstimateCost();
    }
  }

  public int AwaitingDriversResponses { get; set; } = 0;
  public virtual ISet<Driver> DriversRejections { get; set; } = new HashSet<Driver>();
  public virtual ISet<Driver> ProposedDrivers { get; set; } = new HashSet<Driver>();
  public Instant? AcceptedAt { get; set; }
  public Instant? Started { get; set; }
  public virtual Address From { get; set; }
  public virtual Address To { get; set; }

  public int PickupAddressChangeCounter { get; set; } = 0;

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

  public int? EstimatedPrice { get; set; }
}