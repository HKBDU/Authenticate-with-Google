namespace AuthGG.Api.Models;

public sealed class GoogleLoginRequest
{
    public string IdToken { get; init; } = string.Empty;
}
