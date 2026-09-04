using System;
using NSubstitute;

namespace LegacyFighter.CabsTests.Common;

public static class Arg<T>
{
  public static T That(Action<T> assertion)
  {
    return NSubstitute.Arg.Is<T>(value => Satisfies(value, assertion));
  }

  private static bool Satisfies(T value, Action<T> assertion)
  {
    try
    {
      assertion(value);
      return true;
    }
    catch
    {
      return false;
    }
  }
}
