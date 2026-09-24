using System.Collections.Concurrent;
using AuthGG.Api.Models;

namespace AuthGG.Api.Services;

public sealed class UserStore
{
    private readonly ConcurrentDictionary<string, AppUser> _users = new();

    public AppUser Upsert(string subject, string email, string name, string? picture)
    {
        return _users.AddOrUpdate(
            subject,
            _ => new AppUser(subject, email, name, picture, DateTimeOffset.UtcNow),
            (_, existing) => existing with
            {
                Email = email,
                Name = name,
                Picture = picture,
                LastLoginAt = DateTimeOffset.UtcNow
            });
    }
}
