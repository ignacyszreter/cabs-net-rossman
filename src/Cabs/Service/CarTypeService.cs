using System.Linq;
using LegacyFighter.Cabs.Dto;
using LegacyFighter.Cabs.Entity;
using LegacyFighter.Cabs.Repository;

namespace LegacyFighter.Cabs.Service;

public class CarTypeService : ICarTypeService
{
  private readonly ICarTypeRepository _carTypeRepository;

  public CarTypeService(ICarTypeRepository carTypeRepository)
  {
    _carTypeRepository = carTypeRepository;
  }

  public async Task<CarType> Load(long? id)
  {
    var carType = await _carTypeRepository.Find(id);
    if (carType == null)
    {
      throw new InvalidOperationException("Cannot find car type");
    }

    return carType;
  }

  public async Task<CarTypeDto> LoadDto(long? id)
  {
    return new CarTypeDto(await Load(id));
  }

  public async Task<CarType> Create(CarTypeDto carTypeDto)
  {
    var byCarClass = await _carTypeRepository.FindByCarClass(carTypeDto.CarClass);
    if (byCarClass == null)
    {
      var type = new CarType(carTypeDto.CarClass, carTypeDto.Description);
      return await _carTypeRepository.Save(type);
    }
    else
    {
      return byCarClass;
    }
  }

  public async Task Activate(long? id)
  {
    var carType = await Load(id);
    carType.Activate();
  }

  public async Task Deactivate(long? id)
  {
    var carType = await Load(id);
    carType.Deactivate();
  }

  public async Task<List<CarType.CarClasses>> FindActiveCarClasses()
  {
    return (await _carTypeRepository.FindByStatus(CarType.Statuses.Active))
      .Select(type => type.CarClass)
      .ToList();
  }

  private async Task<CarType> FindByCarClass(CarType.CarClasses? carClass)
  {
    var byCarClass = await _carTypeRepository.FindByCarClass(carClass);
    if (byCarClass == null)
    {
      throw new ArgumentException("Car class does not exist: " + carClass);
    }

    return byCarClass;
  }
}