using LegacyFighter.Cabs.Entity;

namespace LegacyFighter.Cabs.Dto;

public class CarTypeDto
{
  public CarTypeDto(CarType carType)
  {
    Id = carType.Id;
    CarClass = carType.CarClass;
    Status = carType.Status;
    Description = carType.Description;
  }

  public CarTypeDto()
  {

  }

  public long? Id { get; }
  public CarType.CarClasses CarClass { get; set; }
  public CarType.Statuses? Status { get; set; }
  public string Description { get; set; }
}