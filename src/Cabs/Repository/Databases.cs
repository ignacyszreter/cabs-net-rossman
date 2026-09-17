using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace LegacyFighter.Cabs.Repository;

public interface IDatabase
{
  void ApplyTo(DbContextOptionsBuilder options);
  Task Create(DatabaseFacade database);
}

public static class Databases
{
  public const string ConnectionStringName = "Cabs";

  public static IDatabase From(IConfiguration configuration)
  {
    var connectionString = configuration.GetConnectionString(ConnectionStringName);
    return string.IsNullOrWhiteSpace(connectionString)
      ? new InMemorySqliteDatabase()
      : new SqlServerDatabase(connectionString);
  }
}

public class InMemorySqliteDatabase : IDatabase
{
  private readonly DbConnection _connection = OpenSharedConnection();

  private static DbConnection OpenSharedConnection()
  {
    var connection = new SqliteConnection("Filename=:memory:");
    connection.Open();
    return connection;
  }

  public void ApplyTo(DbContextOptionsBuilder options)
  {
    options.UseSqlite(_connection);
  }

  public async Task Create(DatabaseFacade database)
  {
    await database.EnsureCreatedAsync();
  }
}

public class SqlServerDatabase : IDatabase
{
  private readonly string _connectionString;

  public SqlServerDatabase(string connectionString)
  {
    _connectionString = connectionString;
  }

  public void ApplyTo(DbContextOptionsBuilder options)
  {
    options.UseSqlServer(_connectionString);
  }

  public async Task Create(DatabaseFacade database)
  {
    await database.EnsureCreatedAsync();
    foreach (var script in Scripts())
    {
      await database.ExecuteSqlRawAsync(script);
    }
  }

  private static IEnumerable<string> Scripts()
  {
    var assembly = typeof(SqlServerDatabase).Assembly;
    foreach (var name in assembly.GetManifestResourceNames().Where(n => n.EndsWith(".sql")).Order())
    {
      using var reader = new StreamReader(assembly.GetManifestResourceStream(name)!);
      yield return reader.ReadToEnd();
    }
  }
}
