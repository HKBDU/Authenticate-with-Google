namespace AuthGG.Api.Models;

public sealed record AppUser(
    string GoogleSubject,
    string Email,
    string Name,
    string? Picture,
    DateTimeOffset LastLoginAt);
