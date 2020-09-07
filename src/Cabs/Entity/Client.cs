using LegacyFighter.Cabs.Common;

namespace LegacyFighter.Cabs.Entity;

public class Client : BaseEntity
{

  public Client()
  {

  }

  public string Name { get; set; }
  public string LastName { get; set; }

  public override bool Equals(object obj)
  {
    if (ReferenceEquals(this, obj)) return true;
    return obj != null && Id != null && Id == (obj as Client)?.Id;
  }

  public static bool operator ==(Client left, Client right)
  {
    return Equals(left, right);
  }

  public static bool operator !=(Client left, Client right)
  {
    return !Equals(left, right);
  }
}