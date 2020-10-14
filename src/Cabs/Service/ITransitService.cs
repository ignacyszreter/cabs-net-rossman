using System.Linq;
using LegacyFighter.Cabs.Dto;
using LegacyFighter.Cabs.Entity;

namespace LegacyFighter.Cabs.Service;

public interface ITransitService
{
  Task<Transit> CreateTransit(TransitDto transitDto);
  Task<Transit> CreateTransit(long? clientId, Address from, Address to);
  Task CancelTransit(long? transitId);
  Task<Transit> PublishTransit(long? transitId);
  Task<Transit> FindDriversForTransit(long? transitId);
  Task<TransitDto> LoadTransit(long? id);
}