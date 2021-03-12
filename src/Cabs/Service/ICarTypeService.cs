using System.Linq;
using LegacyFighter.Cabs.Dto;
using LegacyFighter.Cabs.Entity;

namespace LegacyFighter.Cabs.Service;

public interface ICarTypeService
{
  Task<CarType> Load(long? id);
  Task<CarTypeDto> LoadDto(long? id);
  Task<CarType> Create(CarTypeDto carTypeDto);
  Task Activate(long? id);
  Task Deactivate(long? id);
  Task<List<CarType.CarClasses>> FindActiveCarClasses();
}