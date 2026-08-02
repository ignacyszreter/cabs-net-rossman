using System.IO;
using DotNet.Testcontainers.Configurations;
using Testcontainers.Neo4j;

namespace LegacyFighter.CabsTests.Common;

public class TestWithGraphDb
{
  private Neo4jContainer _neo4J = default!;
  private NUnitConsumer _outputConsumer = default!;
  private const int InternalHttpPort = 7474;
  private const int InternalBoltPort = 7687;
  protected string Neo4JBoltUri => $"neo4j://{_neo4J.Hostname}:{_neo4J.GetMappedPublicPort(InternalBoltPort)}";

  [SetUp]
  public async Task SetUp()
  {
    _outputConsumer = new NUnitConsumer();
    _neo4J = new Neo4jBuilder()
      .WithImage("neo4j:5.26")
      .WithEnvironment("NEO4J_AUTH", "none")
      .WithOutputConsumer(_outputConsumer)
      .Build();
    await _neo4J.StartAsync();
  }

  [TearDown]
  public async Task TearDown()
  {
    _outputConsumer.Dispose();
    await _neo4J.DisposeAsync();
  }

  private class NUnitConsumer : IOutputConsumer
  {
    private readonly MemoryStream _stream = new();
  
    public NUnitConsumer()
    {
      Stderr = _stream;
      Stdout = _stream;
    }

    public void Dispose()
    {
      _stream.Position = 0;
      using var reader = new StreamReader(_stream);
      var logs = reader.ReadToEnd();
      TestContext.Out.WriteLine(logs);
      _stream.Close();
    }

    public bool Enabled => true;

    public Stream Stdout { get; }
    public Stream Stderr { get; }
  }

}