namespace LegacyFighter.Cabs.Claims;

public enum ClaimStatus
{
  Draft,
  New,
  InProcess,
  Refunded,
  Escalated
}

// Ordinals must match Claim.CompletionModes: the resolution Golden Master reads both /claims and
// /bubble/claims into one type, and both serialize the enum as a number.
public enum ClaimCompletionMode
{
  Manual,
  Automatic
}
