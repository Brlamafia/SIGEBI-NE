namespace SIGEBI.Application.Options;

public sealed class AuthenticationOptions
{
    public int MaxFailedAttempts { get; init; } = 5;
    public int LockoutMinutes { get; init; } = 15;
}
