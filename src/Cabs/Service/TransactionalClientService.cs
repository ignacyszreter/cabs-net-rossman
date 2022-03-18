using System.Linq;
using LegacyFighter.Cabs.Common;
using LegacyFighter.Cabs.Dto;
using LegacyFighter.Cabs.Entity;

namespace LegacyFighter.Cabs.Service;

public class TransactionalClientService : IClientService
{
  private readonly IClientService _inner;
  private readonly ITransactions _transactions;

  public TransactionalClientService(IClientService inner, ITransactions transactions)
  {
    _inner = inner;
    _transactions = transactions;
  }

  public async Task<Client> RegisterClient(string name, string lastName)
  {
    await using var tx = await _transactions.BeginTransaction();
    var client = await _inner.RegisterClient(name, lastName);
    await tx.Commit();
    return client;
  }

  public async Task<ClientDto> Load(long? id)
  {
    await using var tx = await _transactions.BeginTransaction();
    var clientDto = await _inner.Load(id);
    await tx.Commit();
    return clientDto;
  }
}