using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LegacyFighter.Cabs.Repository;

public interface IDatabase
{
  void ApplyTo(DbContextOptionsBuilder options);
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
}
