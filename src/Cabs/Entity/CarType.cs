using LegacyFighter.Cabs.Common;

namespace LegacyFighter.Cabs.Entity;

public class CarType : BaseEntity
{
  public enum Statuses
  {
    Inactive,
    Active
  }

  public enum CarClasses
  {
    Eco,
    Regular,
    Van,
    Premium
  }

  public CarType(CarClasses carClass, string description, int minNoOfCarsToActivateClass)
  {
    CarClass = carClass;
    Description = description;
    MinNoOfCarsToActivateClass = minNoOfCarsToActivateClass;
  }

  protected CarType()
  {
  }

  public void RegisterCar()
  {
    CarsCounter++;
  }

  public void UnregisterCar()
  {
    CarsCounter--;
  }

  public void Activate()
  {
    if (CarsCounter < MinNoOfCarsToActivateClass)
    {
      throw new InvalidOperationException();
    }

    Status = Statuses.Active;
  }

  public void Deactivate()
  {
    Status = Statuses.Inactive;
  }

  public CarClasses CarClass { get; set; }
  public string Description { get; set; }
  public Statuses? Status { get; private set; } = Statuses.Inactive;
  public int CarsCounter { get; private set; }
  public int MinNoOfCarsToActivateClass { get; private set; }

  public override bool Equals(object obj)
  {
    if (ReferenceEquals(this, obj)) return true;
    return obj != null && Id != null && Id == (obj as CarType)?.Id;
  }

  public static bool operator ==(CarType left, CarType right)
  {
    return Equals(left, right);
  }

  public static bool operator !=(CarType left, CarType right)
  {
    return !Equals(left, right);
  }
}