namespace ProfanityService;

public sealed record ValidateTextRequest(string Text);
public sealed record ProfanityResult(bool IsProfane, IReadOnlyList<string> Matches);
